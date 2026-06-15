using System;
using System.Collections;
using System.Text;
using Game.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Services
{
    /// <summary>
    /// 로그 노출 범위를 제어하는 레벨입니다.
    /// None       — 모든 HTTP 로그 비활성화
    /// ErrorOnly  — 전송 오류 및 4xx/5xx 응답만 출력
    /// Normal     — 요청 메소드·URL·응답 코드 출력 (바디 제외)
    /// Verbose    — 요청/응답 바디까지 모두 출력
    /// </summary>
    public enum NetworkLogLevel { None, ErrorOnly, Normal, Verbose }

    /// <summary>
    /// 중앙 집중식 HTTP 통신 서비스. DDOL 싱글톤.
    /// - AppConfig 에서 BaseUrl 을 가져오고, Application.version 을 X-Client-Version 헤더에 주입합니다.
    /// - AuthService 가 SetAuthToken 을 통해 토큰을 주입합니다.
    /// - 모든 요청/응답은 LogLevel 에 따라 로그를 남깁니다.
    /// </summary>
    public class NetworkService : MonoBehaviour
    {
        // ─── 싱글톤 ───────────────────────────────────────────────────────
        static NetworkService _instance;

        public static NetworkService Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[NetworkService] Instance is missing! Ensure it is placed in the Boot scene as a GameObject.");
                }
                return _instance;
            }
        }

        // ─── 인스펙터 필드 ────────────────────────────────────────────────
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] AppEnvironment _environment = AppEnvironment.Dev;
#else
        [SerializeField] AppEnvironment _environment = AppEnvironment.Prod;
#endif
        [SerializeField] string _protocolVersion = "1";
        [SerializeField] bool   _enableLogging   = true;
        [SerializeField] NetworkLogLevel _logLevel = NetworkLogLevel.Normal;

        // ─── 런타임 상태 ─────────────────────────────────────────────────
        string _authToken = string.Empty;
        Action<Action<bool>> _tokenRefresher;

        // ─── 프로퍼티 ────────────────────────────────────────────────────
        public AppEnvironment Environment => _environment;
        string BaseUrl => _environment == AppEnvironment.Prod
            ? AppConfig.ProdGameServerUrl
            : AppConfig.DevGameServerUrl;

        // ─── 생명주기 ────────────────────────────────────────────────────
        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Silence console stack trace for cleaner network logs
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ─── 공개 API ────────────────────────────────────────────────────

        /// <summary>인증 JWT 토큰을 설정합니다. AuthService 가 로그인 완료 시 호출합니다.</summary>
        public void SetAuthToken(string token) => _authToken = token ?? string.Empty;

        /// <summary>401 수신 시 호출할 토큰 갱신 델리게이트. AuthService 가 설정합니다.</summary>
        public void SetTokenRefresher(Action<Action<bool>> refresher) => _tokenRefresher = refresher;

        /// <summary>HTTP GET 요청. onComplete(success, responseText|errorText)</summary>
        public void Get(string path, Action<bool, string> onComplete)
            => StartCoroutine(SendGet(path, NetworkRetryOptions.None, onComplete));

        /// <summary>HTTP GET 요청 (재시도 옵션 지정). onComplete(success, responseText|errorText)</summary>
        public void Get(string path, NetworkRetryOptions retryOptions, Action<bool, string> onComplete)
            => StartCoroutine(SendGet(path, retryOptions ?? NetworkRetryOptions.None, onComplete));

        /// <summary>HTTP POST 요청. onComplete(success, responseText|errorText)</summary>
        public void Post(string path, string jsonPayload, Action<bool, string> onComplete)
            => StartCoroutine(SendWithBody(path, "POST", jsonPayload, NetworkRetryOptions.None, onComplete));

        /// <summary>HTTP POST 요청 (재시도 옵션 지정). onComplete(success, responseText|errorText)</summary>
        public void Post(string path, string jsonPayload, NetworkRetryOptions retryOptions, Action<bool, string> onComplete)
            => StartCoroutine(SendWithBody(path, "POST", jsonPayload, retryOptions ?? NetworkRetryOptions.None, onComplete));

        // ─── 내부 구현 ────────────────────────────────────────────────────

        IEnumerator SendGet(string path, NetworkRetryOptions retryOptions, Action<bool, string> onComplete)
        {
            var url = BuildUrl(path);
            int attempt = 0;
            bool didShowOverlay = false;
            bool tokenRefreshed = false;

            while (true)
            {
                using var req = UnityWebRequest.Get(url);
                ApplyHeaders(req);
                LogRequest("GET", path, null);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    if (didShowOverlay && retryOptions.ShowLoadingOverlay)
                    {
                        Core.UIManager.Instance?.HideLoading();
                    }
                    Complete(req, "GET", null, onComplete);
                    yield break;
                }

                if (req.responseCode == 401 && !tokenRefreshed && _tokenRefresher != null)
                {
                    tokenRefreshed = true;
                    bool refreshDone = false, refreshOk = false;
                    _tokenRefresher(ok => { refreshOk = ok; refreshDone = true; });
                    yield return new WaitUntil(() => refreshDone);
                    if (refreshOk) continue;
                }

                attempt++;
                if (attempt <= retryOptions.MaxRetries && retryOptions.ShouldRetry(req.responseCode, req.result))
                {
                    if (retryOptions.ShowLoadingOverlay && !didShowOverlay)
                    {
                        Core.UIManager.Instance?.ShowLoading();
                        didShowOverlay = true;
                    }

                    float delay = retryOptions.BaseDelaySeconds;
                    if (retryOptions.UseExponentialBackoff)
                    {
                        delay *= Mathf.Pow(2, attempt - 1);
                    }
                    float jitter = delay * UnityEngine.Random.Range(-retryOptions.JitterRatio, retryOptions.JitterRatio);
                    float finalDelay = Mathf.Max(0.1f, delay + jitter);

                    Debug.LogWarning($"[NetworkService] GET Failed ({req.result}, Code: {req.responseCode}). Retrying {attempt}/{retryOptions.MaxRetries} in {finalDelay:F2}s... URL: {url}");
                    yield return new WaitForSeconds(finalDelay);
                    continue;
                }

                if (didShowOverlay && retryOptions.ShowLoadingOverlay)
                {
                    Core.UIManager.Instance?.HideLoading();
                }

                if (!string.IsNullOrEmpty(retryOptions.FailureToastKey))
                {
                    Core.UIManager.Instance?.ShowToast(LocalizationService.Instance?.Get(retryOptions.FailureToastKey) ?? retryOptions.FailureToastKey, Core.UI.ToastType.Warning);
                }

                Complete(req, "GET", null, onComplete);
                yield break;
            }
        }

        IEnumerator SendWithBody(string path, string method, string jsonPayload, NetworkRetryOptions retryOptions, Action<bool, string> onComplete)
        {
            var url = BuildUrl(path);
            var body = string.IsNullOrEmpty(jsonPayload) ? "{}" : jsonPayload;
            int attempt = 0;
            bool didShowOverlay = false;
            bool tokenRefreshed = false;

            while (true)
            {
                using var req = new UnityWebRequest(url, method);
                var bytes = Encoding.UTF8.GetBytes(body);
                req.uploadHandler   = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                ApplyHeaders(req);
                LogRequest(method, path, body);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    if (didShowOverlay && retryOptions.ShowLoadingOverlay)
                    {
                        Core.UIManager.Instance?.HideLoading();
                    }
                    Complete(req, method, body, onComplete);
                    yield break;
                }

                if (req.responseCode == 401 && !tokenRefreshed && _tokenRefresher != null)
                {
                    tokenRefreshed = true;
                    bool refreshDone = false, refreshOk = false;
                    _tokenRefresher(ok => { refreshOk = ok; refreshDone = true; });
                    yield return new WaitUntil(() => refreshDone);
                    if (refreshOk) continue;
                }

                attempt++;
                if (attempt <= retryOptions.MaxRetries && retryOptions.ShouldRetry(req.responseCode, req.result))
                {
                    if (retryOptions.ShowLoadingOverlay && !didShowOverlay)
                    {
                        Core.UIManager.Instance?.ShowLoading();
                        didShowOverlay = true;
                    }

                    float delay = retryOptions.BaseDelaySeconds;
                    if (retryOptions.UseExponentialBackoff)
                    {
                        delay *= Mathf.Pow(2, attempt - 1);
                    }
                    float jitter = delay * UnityEngine.Random.Range(-retryOptions.JitterRatio, retryOptions.JitterRatio);
                    float finalDelay = Mathf.Max(0.1f, delay + jitter);

                    Debug.LogWarning($"[NetworkService] {method} Failed ({req.result}, Code: {req.responseCode}). Retrying {attempt}/{retryOptions.MaxRetries} in {finalDelay:F2}s... URL: {url}");
                    yield return new WaitForSeconds(finalDelay);
                    continue;
                }

                if (didShowOverlay && retryOptions.ShowLoadingOverlay)
                {
                    Core.UIManager.Instance?.HideLoading();
                }

                if (!string.IsNullOrEmpty(retryOptions.FailureToastKey))
                {
                    Core.UIManager.Instance?.ShowToast(LocalizationService.Instance?.Get(retryOptions.FailureToastKey) ?? retryOptions.FailureToastKey, Core.UI.ToastType.Warning);
                }

                Complete(req, method, body, onComplete);
                yield break;
            }
        }

        string BuildUrl(string path)
        {
            if (string.IsNullOrEmpty(path)) return BaseUrl;
            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
            return $"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        void ApplyHeaders(UnityWebRequest req)
        {
            req.SetRequestHeader("X-Client-Version",   Application.version);
            req.SetRequestHeader("X-Protocol-Version", _protocolVersion);
            if (!string.IsNullOrEmpty(_authToken))
                req.SetRequestHeader("Authorization", $"Bearer {_authToken}");
        }

        void Complete(UnityWebRequest req, string method, string requestBody, Action<bool, string> onComplete)
        {
            var responseBody = req.downloadHandler?.text ?? string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                LogResponse(method, req.url, req.responseCode, requestBody, responseBody, isError: false);
                onComplete?.Invoke(true, responseBody);
                return;
            }

            var status = req.responseCode > 0 ? req.responseCode.ToString() : req.result.ToString();
            var error  = string.IsNullOrEmpty(responseBody) ? req.error : responseBody;
            LogResponse(method, req.url, req.responseCode, requestBody, error, isError: true);
            onComplete?.Invoke(false, error);
        }

        // ─── 로깅 헬퍼 ────────────────────────────────────────────────────

        void LogRequest(string method, string path, string body)
        {
            // Removed [HTTP ▶] logs per user request.
        }

        void LogResponse(string method, string url, long code, string requestBody, string responseBody, bool isError)
        {
            if (!_enableLogging || _logLevel == NetworkLogLevel.None) return;

            // ErrorOnly: 에러일 때만 출력
            if (_logLevel == NetworkLogLevel.ErrorOnly && !isError) return;

            var sb = new StringBuilder($"[HTTP {(isError ? "✗" : "✓")}] {method} {url} → {code}");

            if (_logLevel == NetworkLogLevel.Verbose)
            {
                sb.Append($"\n  request : {requestBody ?? ""}");
                sb.Append($"\n  response: {responseBody}");
            }
            else if (_logLevel == NetworkLogLevel.Normal && isError)
            {
                sb.Append($"\n  error: {responseBody}");
            }

            if (isError)
            {
                if (code >= 500 || code == 0)
                    Debug.LogError(sb.ToString());
                else
                    Debug.LogWarning(sb.ToString());
            }
            else
            {
                Debug.Log(sb.ToString());
            }
        }
    }
}
