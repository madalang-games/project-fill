using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.UI;

namespace Game.OutGame.Settings
{
    public class AccountPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text       _userIdText;
        [SerializeField] private Button         _linkAccountButton;
        [SerializeField] private Button         _switchAccountButton;
        [SerializeField] private Button         _closeButton;

        [Header("Profile Modifications")]
        [SerializeField] private TMP_InputField _displayNameInput;
        [SerializeField] private Button         _saveNicknameButton;
        [SerializeField] private GameObject     _nicknameArea;

        // Avatar selection moved to the Shop avatar section. This sprite mapping is retained because
        // HeaderView and TutorialOverlay read the equipped avatar sprite via GetAvatarSprite on this prefab.
        [System.Serializable]
        public struct AvatarSpriteMapping
        {
            public int avatarId;
            public string resourceName;
            public Sprite sprite;
        }

        [SerializeField] private List<AvatarSpriteMapping> _avatarSprites = new List<AvatarSpriteMapping>();

        public Sprite GetAvatarSprite(int avatarId)
        {
            if (_avatarSprites != null)
            {
                foreach (var mapping in _avatarSprites)
                {
                    if (mapping.avatarId == avatarId)
                        return mapping.sprite;
                }
            }
            return null;
        }

        private void Awake()
        {
            var auth = AuthService.Instance;
            bool isGuest = auth == null || auth.IsGuest;

            if (_userIdText != null)
                _userIdText.text = isGuest ? (LocalizationService.Instance?.Get("common.guest") ?? "Guest") : auth.UserId;

            if (_linkAccountButton   != null) _linkAccountButton.gameObject.SetActive(isGuest);
            if (_switchAccountButton != null) _switchAccountButton.gameObject.SetActive(!isGuest);

            _linkAccountButton?.onClick.AddListener(OnLinkAccount);
            _switchAccountButton?.onClick.AddListener(OnSwitchAccount);
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);

            // Bind nickname
            if (_displayNameInput != null && auth != null)
            {
                _displayNameInput.text = auth.DisplayName;
            }
            _saveNicknameButton?.onClick.AddListener(OnSaveNickname);

            if (_nicknameArea != null) _nicknameArea.SetActive(true);
        }

        private void OnSaveNickname()
        {
            if (_displayNameInput == null || PlayerApiService.Instance == null) return;
            string nickname = _displayNameInput.text.Trim();

            if (nickname.Length < 2 || nickname.Length > 24)
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.nickname_length_error"), Core.UI.ToastType.Error);
                return;
            }

            // ASCII validation: alphanumeric, space, underscore, hyphen
            foreach (char c in nickname)
            {
                if (!((c >= 'a' && c <= 'z') ||
                      (c >= 'A' && c <= 'Z') ||
                      (c >= '0' && c <= '9') ||
                      c == ' ' || c == '_' || c == '-'))
                {
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.nickname_char_error"), Core.UI.ToastType.Error);
                    return;
                }
            }

            UIManager.Instance?.ShowLoading();
            PlayerApiService.Instance.UpdateProfile(nickname, null, null, (ok, res, err) =>
            {
                UIManager.Instance?.HideLoading();
                if (ok && res != null)
                {
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.nickname_updated"), Core.UI.ToastType.Success);
                }
                else
                {
                    string errorMsg = LocalizationService.Instance.Get("toast.nickname_update_failed");
                    if (!string.IsNullOrEmpty(err))
                    {
                        errorMsg = LocalizationService.Instance != null 
                            ? LocalizationService.Instance.GetError(err) 
                            : err;
                    }
                    Game.Core.UIManager.Instance?.ShowToast(errorMsg, Core.UI.ToastType.Error);
                }
            });
        }

        private void OnLinkAccount()
        {
            var webClientId = Game.Core.AppConfig.GoogleWebClientId;
            if (string.IsNullOrEmpty(webClientId))
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_not_configured"), Core.UI.ToastType.Error);
                return;
            }

#if UNITY_ANDROID
            var bridge = Game.Core.GoogleSignInBridge.Instance;
            if (bridge == null)
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_unavailable"), Core.UI.ToastType.Error);
                return;
            }

            Game.Core.UIManager.Instance?.ShowLoading();
            bridge.SignIn(webClientId, (idToken, error) =>
            {
                if (string.IsNullOrEmpty(idToken))
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    if (error != "GOOGLE_SIGN_IN_CANCELLED")
                    {
                        var msg = LocalizationService.Instance?.GetError(error) ?? error;
                        Game.Core.UIManager.Instance?.ShowToast(msg, Core.UI.ToastType.Error);
                    }
                    return;
                }

                AuthService.Instance.LinkGoogle(idToken, null, (ok, err, linkResp) =>
                {
                    Debug.Log($"[LinkGoogle CB] ok={ok} err={err} | conflict={linkResp?.conflict} success={linkResp?.success} conflictToken={linkResp?.conflictToken}");
                    Game.Core.UIManager.Instance?.HideLoading();
                    if (!ok)
                    {
                        var msg = LocalizationService.Instance?.GetError(err) ?? err;
                        Game.Core.UIManager.Instance?.ShowToast(msg, Core.UI.ToastType.Error);
                        return;
                    }

                    if (linkResp != null && linkResp.conflict)
                    {
                        var local = linkResp.localSave;
                        var cloud = linkResp.cloudSave;
                        var token = linkResp.conflictToken;
                        void ShowConflict()
                        {
                            Game.Core.UIManager.Instance?.CloseTopPopup();
                            Game.Core.UIManager.Instance?.ShowPopup<AccountConflictPopupView>(v => v.Init(
                                localMaxStage: local?.maxStageId ?? 0,
                                localGold:     local?.gold ?? 0,
                                localStars:    local?.totalStars ?? 0,
                                localItems:    local?.totalItems ?? 0,
                                cloudMaxStage: cloud?.maxStageId ?? 0,
                                cloudGold:     cloud?.gold ?? 0,
                                cloudStars:    cloud?.totalStars ?? 0,
                                cloudItems:    cloud?.totalItems ?? 0,
                                onKeepLocal: () => ResolveConflict(token, "local"),
                                onKeepCloud: () => ResolveConflict(token, "cloud")
                            ));
                        }
                        var appear = GetComponent<Core.UI.UIPanelAppear>();
                        if (appear != null)
                            appear.Disappear(ShowConflict);
                        else
                            ShowConflict();
                    }
                    else
                    {
                        // No conflict — new Google account, link completed server-side
                        // CompleteSession will show restart popup if PID changed
                        // If no popup was shown, auth succeeded without switch (shouldn't happen for link flow)
                        Close();
                    }
                });
            });
#else
            Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_android_only"), Core.UI.ToastType.Error);
#endif
        }

        private void ResolveConflict(string conflictToken, string selection)
        {
            Game.Core.UIManager.Instance?.ShowLoading();
            AuthService.Instance.ResolveConflict(conflictToken, selection, (ok, err) =>
            {
                Game.Core.UIManager.Instance?.HideLoading();
                if (!ok)
                {
                    var msg = LocalizationService.Instance?.GetError(err) ?? err;
                    Game.Core.UIManager.Instance?.ShowToast(msg, Core.UI.ToastType.Error);
                }
                // On success: CompleteSession shows AccountRestartPopupView -> Boot redirect
            });
        }

        private void OnSwitchAccount()
        {
            Game.Core.UIManager.Instance?.ShowPopup<Core.UI.ConfirmDialogView>(v => v.Init(
                title:        LocalizationService.Instance.Get("popup.account.confirm_switch_title"),
                body:         LocalizationService.Instance.Get("popup.account.confirm_switch_body"),
                confirmLabel: LocalizationService.Instance.Get("common.btn_switch"),
                onConfirm:    DoSwitchAccount,
                danger:       false));
        }

        private void DoSwitchAccount()
        {
            var webClientId = Game.Core.AppConfig.GoogleWebClientId;
            if (string.IsNullOrEmpty(webClientId))
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_not_configured"), Core.UI.ToastType.Error);
                return;
            }

#if UNITY_ANDROID
            var bridge = Game.Core.GoogleSignInBridge.Instance;
            if (bridge == null)
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_unavailable"), Core.UI.ToastType.Error);
                return;
            }

            Game.Core.UIManager.Instance?.ShowLoading();
            bridge.SignOut(webClientId);
            bridge.SignIn(webClientId, (idToken, error) =>
            {
                if (string.IsNullOrEmpty(idToken))
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    if (error != "GOOGLE_SIGN_IN_CANCELLED")
                    {
                        var msg = LocalizationService.Instance?.GetError(error) ?? error;
                        Game.Core.UIManager.Instance?.ShowToast(msg, Core.UI.ToastType.Error);
                    }
                    return;
                }

                AuthService.Instance.SwitchGoogle(idToken, null, (ok, err) =>
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    if (ok)
                    {
                        // Same account selected — no PID mismatch occurred
                        Game.Core.UIManager.Instance?.ShowToast(
                            LocalizationService.Instance.Get("toast.account_already_active"), Core.UI.ToastType.Warning);
                        Close();
                    }
                    else
                    {
                        var msg = LocalizationService.Instance?.GetError(err) ?? err;
                        Game.Core.UIManager.Instance?.ShowToast(msg, Core.UI.ToastType.Error);
                    }
                    // If PID mismatch detected: CompleteSession shows AccountRestartPopupView -> never reaches here
                });
            });
#else
            Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_android_only"), Core.UI.ToastType.Error);
#endif
        }

        private void Close()
        {
            var appear = GetComponent<Core.UI.UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Game.Core.UIManager.Instance?.CloseTopPopup());
            else
                Game.Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
