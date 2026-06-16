using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Services
{
    public enum AuthResult { Authenticated, Guest, ReLoginRequired, NewGuestCreated, NetworkError }

    public class AuthService : MonoBehaviour
    {
        public static AuthService Instance { get; private set; }

        // Removed event duplicate here

        private const string AccessTokenKey = "Auth.AccessToken";
        private const string RefreshTokenKey = "Auth.RefreshToken";
        private const string AccessExpiresAtKey = "Auth.AccessExpiresAt";
        private const string ProviderKey = "Auth.Provider";
        private const string ClientIdKey = "Auth.ClientId";
        private const string PlayerPrefsPidKey = "auth_pid";

        private ITokenStorage _storage;
        private string _accessToken = string.Empty;
        private string _refreshToken = string.Empty;
        private DateTimeOffset _accessExpiresAt;
        private string _provider = string.Empty;
        private bool _refreshInFlight;
        private List<Action<bool, string>> _refreshWaiters;
        private bool _accountSwitched;

        public static string PendingBootMessage { get; set; } = null;

        private string _displayName = string.Empty;
        private int _avatarId = 1;

        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);
        public bool IsGuest => _provider == "guest";
        public string UserId => PlayerPrefs.GetString(ClientIdKey, string.Empty);
        public string Pid => PlayerPrefs.GetString(PlayerPrefsPidKey, string.Empty);
        public string AccessToken => _accessToken;
        public string DisplayName => _displayName;
        public int AvatarId => _avatarId;

        public event Action OnProfileChanged;
        public event Action<bool, string> OnAuthStateChanged; // (isAuthenticated, provider)

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _storage = new SecureTokenStorage();
            LoadSession();
        }

        public void Initialize(Action<AuthResult> onComplete)
        {
            _accountSwitched = false;
            Debug.Log($"[AuthService] Initialize: IsAuthenticated={IsAuthenticated}, Provider={_provider}, HasToken={!string.IsNullOrEmpty(_accessToken)}");

            if (HasUsableAccessToken())
            {
                Debug.Log("[AuthService] Using existing valid session.");
                SyncServiceTokens();
                onComplete?.Invoke(IsGuest ? AuthResult.Guest : AuthResult.Authenticated);
                return;
            }

            if (!string.IsNullOrEmpty(_refreshToken) && !IsOfflineToken(_refreshToken))
            {
                Debug.Log("[AuthService] Attempting Token Refresh...");
                Refresh((success, error) =>
                {
                    if (success)
                    {
                        onComplete?.Invoke(IsGuest ? AuthResult.Guest : AuthResult.Authenticated);
                    }
                    else if (IsRateLimitedError(error))
                    {
                        // Temporary rate limit — token preserved, surface as transient network error
                        onComplete?.Invoke(AuthResult.NetworkError);
                    }
                    else
                    {
                        onComplete?.Invoke(AuthResult.ReLoginRequired);
                    }
                });
                return;
            }

            Debug.Log("[AuthService] No valid session found. Attempting Guest Login...");
            LoginGuest((success, error) =>
            {
                if (success)
                {
                    onComplete?.Invoke(_accountSwitched ? AuthResult.NewGuestCreated : AuthResult.Guest);
                }
                else
                {
                    Debug.LogWarning($"[AuthService] Guest login failed (network unreachable): {error}");
                    onComplete?.Invoke(AuthResult.NetworkError);
                }
            });
        }

        private bool IsOfflineToken(string token) => token != null && token.StartsWith("offline_");

        public void LoginGuest(Action<bool, string> onComplete)
        {
            var clientId = GetOrCreateClientId();
            var req = new GuestLoginRequestJson { clientId = clientId, displayName = null };
            Debug.Log($"[AuthService] Attempting Guest Login with ClientId: {clientId}");
            Post("/api/auth/guest", JsonUtility.ToJson(req), text =>
            {
                var response = JsonUtility.FromJson<AuthResponseJson>(text);
                CompleteSession(true, "", "guest", response, onComplete);
            }, error => onComplete?.Invoke(false, error));
        }

        public void LoginGoogle(string idToken, string nonce, Action<bool, string> onComplete)
            => LoginGoogleInternal(idToken, nonce, IsGuest ? _refreshToken : null, onComplete);

        public void SwitchGoogle(string idToken, string nonce, Action<bool, string> onComplete)
            => LoginGoogleInternal(idToken, nonce, null, onComplete);

        private void LoginGoogleInternal(string idToken, string nonce, string guestRefreshToken, Action<bool, string> onComplete)
        {
            var clientId = GetOrCreateClientId();
            var req = new GoogleLoginRequestJson
            {
                clientId = clientId,
                idToken = idToken,
                nonce = string.IsNullOrEmpty(nonce) ? null : nonce,
                guestRefreshToken = string.IsNullOrEmpty(guestRefreshToken) ? null : guestRefreshToken
            };
            Post("/api/auth/google", JsonUtility.ToJson(req), text =>
            {
                var response = JsonUtility.FromJson<AuthResponseJson>(text);
                CompleteSession(true, "", "google", response, onComplete);
            }, error => onComplete?.Invoke(false, error));
        }

        public void Refresh(Action<bool, string> onComplete)
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                onComplete?.Invoke(false, "SESSION_EXPIRED");
                return;
            }

            if (_refreshInFlight)
            {
                if (_refreshWaiters == null) _refreshWaiters = new List<Action<bool, string>>();
                _refreshWaiters.Add(onComplete);
                return;
            }

            _refreshInFlight = true;
            var req = new RefreshRequestJson { refreshToken = _refreshToken };
            Post("/api/auth/refresh", JsonUtility.ToJson(req), text =>
            {
                var response = JsonUtility.FromJson<AuthResponseJson>(text);
                _refreshInFlight = false;
                var waiters = _refreshWaiters;
                _refreshWaiters = null;

                CompleteSession(true, "", _provider, response, (ok, err) =>
                {
                    onComplete?.Invoke(ok, err);
                    if (waiters != null)
                        foreach (var w in waiters) w?.Invoke(ok, err);
                });
            }, error =>
            {
                _refreshInFlight = false;
                var waiters = _refreshWaiters;
                _refreshWaiters = null;

                // Only wipe tokens when the server explicitly rejects them (parseable JSON error).
                // Network failures (server unreachable, timeout) return raw strings — preserve tokens
                // so the next launch can retry the refresh instead of creating a new guest account.
                if (IsServerAuthRejection(error)) ClearToken();

                onComplete?.Invoke(false, error);
                if (waiters != null)
                    foreach (var w in waiters) w?.Invoke(false, error);
            });
        }

        public void ContinueAsGuest(Action<AuthResult> onComplete)
        {
            _accountSwitched = false;
            LoginGuest((success, error) =>
            {
                if (success)
                    onComplete?.Invoke(_accountSwitched ? AuthResult.NewGuestCreated : AuthResult.Guest);
                else
                {
                    Debug.LogWarning($"[AuthService] Guest login failed: {error}");
                    onComplete?.Invoke(AuthResult.NetworkError);
                }
            });
        }

        public void Logout(Action<bool, string> onComplete = null)
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                ClearToken();
                ClearAllLocalProgress();
                onComplete?.Invoke(true, "");
                return;
            }

            var req = new LogoutRequestJson { refreshToken = _refreshToken, reason = "client_logout" };
            Post("/api/auth/logout", JsonUtility.ToJson(req), text =>
            {
                ClearToken();
                ClearAllLocalProgress();
                onComplete?.Invoke(true, "");
            }, error =>
            {
                ClearToken();
                ClearAllLocalProgress();
                onComplete?.Invoke(true, ""); // Force logout locally even if request fails
            });
        }

        private void ClearAllLocalProgress()
        {
            // Preserve auth/identity keys across the DeleteAll (ClientId = device identity, tokens = current session)
            var clientId     = PlayerPrefs.GetString(ClientIdKey, "");
            var accessToken  = _storage.Get(AccessTokenKey);
            var refreshToken = _storage.Get(RefreshTokenKey);
            var provider     = _storage.Get(ProviderKey);
            var expiresAt    = _storage.Get(AccessExpiresAtKey);
            var pid          = PlayerPrefs.GetString(PlayerPrefsPidKey, "");

            PlayerPrefs.DeleteAll();

            if (!string.IsNullOrEmpty(clientId))     PlayerPrefs.SetString(ClientIdKey, clientId);
            if (!string.IsNullOrEmpty(accessToken))  _storage.Set(AccessTokenKey, accessToken);
            if (!string.IsNullOrEmpty(refreshToken)) _storage.Set(RefreshTokenKey, refreshToken);
            if (!string.IsNullOrEmpty(provider))     _storage.Set(ProviderKey, provider);
            if (!string.IsNullOrEmpty(expiresAt))    _storage.Set(AccessExpiresAtKey, expiresAt);
            if (!string.IsNullOrEmpty(pid))          PlayerPrefs.SetString(PlayerPrefsPidKey, pid);

            PlayerPrefs.Save();

            if (PlayerProgressService.Instance != null)
                PlayerProgressService.Instance.ResetData();

            Debug.Log("[AuthService] All local progress cleared (Disk + Memory) for logout.");
        }

        private void CompleteSession(bool ok, string error, string provider, AuthResponseJson auth, Action<bool, string> onComplete)
        {
            if (!ok || auth == null || string.IsNullOrEmpty(auth.accessToken))
            {
                onComplete?.Invoke(false, string.IsNullOrEmpty(error) ? "AUTH_FAILED" : error);
                return;
            }

            // Detect account switch before any writes; clear stale progress if userId changed
            var oldPid = PlayerPrefs.GetString(PlayerPrefsPidKey, string.Empty);
            var newPid = auth.profile?.pid ?? string.Empty;
            if (!string.IsNullOrEmpty(oldPid) && !string.IsNullOrEmpty(newPid) && oldPid != newPid)
            {
                Debug.LogWarning($"[AuthService] Account switch detected ({oldPid} → {newPid}). Will clear on restart confirmation.");
                _accountSwitched = true;
            }

            _accessToken = auth.accessToken;
            _refreshToken = auth.refreshToken;
            _provider = provider;
            _displayName = auth.profile?.displayName ?? string.Empty;
            _avatarId = auth.profile?.avatarId ?? 1;

            if (DateTimeOffset.TryParse(auth.expiresAt, out var expires))
                _accessExpiresAt = expires;
            else
                _accessExpiresAt = DateTimeOffset.UtcNow.AddHours(1);

            _storage.Set(AccessTokenKey, _accessToken);
            _storage.Set(RefreshTokenKey, _refreshToken);
            _storage.Set(ProviderKey, _provider);
            _storage.Set(AccessExpiresAtKey, _accessExpiresAt.ToString("O"));

            // Store PID in plain PlayerPrefs (survives SecureTokenStorage decryption failures)
            if (!string.IsNullOrEmpty(newPid))
                PlayerPrefs.SetString(PlayerPrefsPidKey, newPid);

            _storage.Save(); // flushes all PlayerPrefs to disk including auth_userid

            SyncServiceTokens();
            OnAuthStateChanged?.Invoke(true, _provider);

            if (_accountSwitched)
            {
                Game.Core.UIManager.Instance?.HideLoading();
                Game.Core.UIManager.Instance?.CloseAllPopups();
                ShowAccountRestartPopup(() =>
                {
                    ClearAllLocalProgress();
                    Game.Core.SceneTransition.Instance?.FadeToScene("Boot");
                });
                return;
            }

            onComplete?.Invoke(true, "");
        }

        public void UpdateCachedProfile(string displayName, int avatarId)
        {
            _displayName = displayName;
            _avatarId = avatarId;
            OnProfileChanged?.Invoke();
        }

        private static bool IsServerAuthRejection(string error)
        {
            if (string.IsNullOrEmpty(error) || !error.TrimStart().StartsWith("{")) return false;
            try
            {
                var parsed = JsonUtility.FromJson<ErrorResponseJson>(error);
                return parsed != null && !string.IsNullOrEmpty(parsed.code);
            }
            catch { return false; }
        }

        private static bool IsRateLimitedError(string error)
        {
            if (string.IsNullOrEmpty(error) || !error.TrimStart().StartsWith("{")) return false;
            try
            {
                var parsed = JsonUtility.FromJson<PlatformErrorJson>(error);
                return parsed != null
                    && (parsed.error == "rate_limited" || parsed.error == "too_many_failures");
            }
            catch { return false; }
        }

        private void ClearToken()
        {
            _accessToken = string.Empty;
            _refreshToken = string.Empty;
            _provider = string.Empty;
            _accessExpiresAt = default;

            _storage.Delete(AccessTokenKey);
            _storage.Delete(RefreshTokenKey);
            _storage.Delete(ProviderKey);
            _storage.Delete(AccessExpiresAtKey);
            _storage.Save();

            SyncServiceTokens();
            OnAuthStateChanged?.Invoke(false, string.Empty);
        }

        private void SyncServiceTokens()
        {
            NetworkService.Instance.SetAuthToken(_accessToken);
            NetworkService.Instance.SetTokenRefresher(cb => Refresh((ok, _) => cb(ok)));
        }

        private void LoadSession()
        {
            _accessToken = _storage.Get(AccessTokenKey);
            _refreshToken = _storage.Get(RefreshTokenKey);
            _provider = _storage.Get(ProviderKey);
            
            var expiresStr = _storage.Get(AccessExpiresAtKey);
            if (!DateTimeOffset.TryParse(expiresStr, out _accessExpiresAt))
                _accessExpiresAt = default;
        }

        private bool HasUsableAccessToken()
        {
            if (string.IsNullOrEmpty(_accessToken) || IsOfflineToken(_accessToken)) return false;
            return _accessExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5);
        }

        private string GetOrCreateClientId()
        {
            // Plain PlayerPrefs — ClientId is a device identifier, not a secret
            var id = PlayerPrefs.GetString(ClientIdKey, "");
            if (!string.IsNullOrEmpty(id)) return id;

            // One-time migration: existing installs stored ClientId in SecureTokenStorage
            var legacy = _storage.Get(ClientIdKey);
            if (!string.IsNullOrEmpty(legacy))
            {
                PlayerPrefs.SetString(ClientIdKey, legacy);
                _storage.Delete(ClientIdKey);
                PlayerPrefs.Save();
                return legacy;
            }

            id = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(ClientIdKey, id);
            PlayerPrefs.Save();
            return id;
        }

        public void LinkGoogle(string idToken, string nonce, Action<bool, string, LinkAccountResponseJson> onComplete)
        {
            var guestRefreshToken = _refreshToken ?? string.Empty;
            var req = new LinkAccountRequestJson { provider = "google", idToken = idToken, guestRefreshToken = guestRefreshToken };
            Post("/api/auth/link-oauth", JsonUtility.ToJson(req), (ok, text) =>
            {
                Debug.Log($"[LinkGoogle] ok={ok} | body={text}");
                if (!ok) { onComplete?.Invoke(false, text, null); return; }
                var resp = JsonUtility.FromJson<LinkAccountResponseJson>(text);
                if (!resp.conflict && resp.auth != null && !string.IsNullOrEmpty(resp.auth.accessToken))
                {
                    CompleteSession(true, string.Empty, "google", resp.auth, (authOk, authErr) =>
                        onComplete?.Invoke(authOk, authErr, resp));
                    return;
                }
                onComplete?.Invoke(true, string.Empty, resp);
            });
        }

        public void ResolveConflict(string conflictToken, string selection, Action<bool, string> onComplete)
        {
            var req = new ResolveConflictRequestJson { conflictToken = conflictToken, selection = selection };
            Post("/api/auth/resolve-conflict", JsonUtility.ToJson(req), (ok, text) =>
            {
                if (!ok) { onComplete?.Invoke(false, text); return; }
                var resp = JsonUtility.FromJson<ResolveConflictResponseJson>(text);
                if (resp?.auth != null && !string.IsNullOrEmpty(resp.auth.accessToken))
                {
                    CompleteSession(true, string.Empty, "google", resp.auth, (authOk, authErr) =>
                        onComplete?.Invoke(authOk, authErr));
                }
                else
                {
                    onComplete?.Invoke(resp?.success == true, string.Empty);
                }
            });
        }

        private static void ShowAccountRestartPopup(System.Action onConfirm)
        {
            if (Game.Core.UIManager.Instance == null ||
                Resources.Load<Game.OutGame.Settings.AccountRestartPopupView>("Prefabs/UI/AccountRestartPopupView") == null)
            {
                Debug.LogWarning("[AuthService] AccountRestartPopupView unavailable — executing restart directly.");
                onConfirm?.Invoke();
                return;
            }
            Game.Core.UIManager.Instance.ShowPopup<Game.OutGame.Settings.AccountRestartPopupView>(
                v => v.Init(onConfirm));
        }

        private void Post(string path, string jsonPayload, Action<bool, string> onComplete)
        {
            NetworkService.Instance.Post(path, jsonPayload, onComplete);
        }

        private void Post(string path, string jsonPayload, Action<string> onSuccess, Action<string> onError)
        {
            NetworkService.Instance.Post(path, jsonPayload, (ok, result) =>
            {
                if (ok) onSuccess?.Invoke(result);
                else    onError?.Invoke(result);
            });
        }

        // --- JSON Helper Classes for Unity JsonUtility ---
        [Serializable]
        private class PlatformErrorJson
        {
            public string error;
        }

        [Serializable]
        private class GuestLoginRequestJson
        {
            public string clientId;
            public string displayName;
        }

        [Serializable]
        private class GoogleLoginRequestJson
        {
            public string clientId;
            public string idToken;
            public string nonce;
            public string guestRefreshToken;
        }

        [Serializable]
        private class RefreshRequestJson
        {
            public string refreshToken;
        }

        [Serializable]
        private class LogoutRequestJson
        {
            public string refreshToken;
            public string reason;
        }

        [Serializable]
        public class ProfileJson
        {
            public string userId;
            public string pid;
            public string displayName;
            public bool isGuest;
            public List<string> linkedProviders;
            public int avatarId;
            public string createdAt;
        }

        [Serializable]
        public class AuthResponseJson
        {
            public string accessToken;
            public string refreshToken;
            public string expiresAt;
            public ProfileJson profile;
        }

        [Serializable]
        public class SaveSnapshotJson
        {
            public int maxStageId;
            public long gold;
            public int totalClearedStages;
            public int totalItems;
        }

        [Serializable]
        public class LinkAccountResponseJson
        {
            public bool success;
            public bool conflict;
            public SaveSnapshotJson localSave;
            public SaveSnapshotJson cloudSave;
            public string conflictToken;
            // No-conflict path: server wraps auth tokens in this field
            public AuthResponseJson auth;
        }

        [Serializable]
        private class LinkAccountRequestJson
        {
            public string provider;
            public string idToken;
            public string guestRefreshToken;
        }

        [Serializable]
        private class ResolveConflictRequestJson
        {
            public string conflictToken;
            public string selection;
        }

        [Serializable]
        private class ResolveConflictResponseJson
        {
            public bool success;
            public AuthResponseJson auth;
        }
    }
}
