using System;

namespace Game.Services
{
    /// <summary>
    /// HTTP 요청 재시도 설정을 지정하는 옵션 클래스입니다.
    /// </summary>
    public class NetworkRetryOptions
    {
        /// <summary>최대 재시도 횟수 (0이면 재시도하지 않음)</summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>첫 번째 재시도 대기 시간 (초)</summary>
        public float BaseDelaySeconds { get; set; } = 1.0f;

        /// <summary>지수 백오프 적용 여부 (대기 시간이 1s -> 2s -> 4s 형태로 증가)</summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>Thundering Herd 방지를 위한 랜덤 Jitter 적용 비율 (0 ~ 0.5f)</summary>
        public float JitterRatio { get; set; } = 0.1f;

        /// <summary>재시도 동작 중 UIManager.ShowLoading/HideLoading 을 호출할지 여부</summary>
        public bool ShowLoadingOverlay { get; set; } = false;

        /// <summary>최대 재시도 초과 실패 시 노출할 Toast의 client_string.csv 키 (null 또는 비어있으면 노출 안 함)</summary>
        public string FailureToastKey { get; set; } = null;

        /// <summary>
        /// 특정 HTTP 상태 코드 및 웹 요청 결과를 기반으로 재시도 여부를 판단하는 델리게이트입니다.
        /// </summary>
        public Func<long, UnityEngine.Networking.UnityWebRequest.Result, bool> ShouldRetry { get; set; } 
            = (responseCode, result) =>
            {
                // Connection Error (인터넷 차단, 도메인 연결 실패, 타임아웃 등)
                if (result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError)
                    return true;

                // HTTP status code가 0이거나 5xx(서버 내부 오류)인 경우
                if (responseCode == 0 || (responseCode >= 500 && responseCode <= 599))
                    return true;

                return false;
            };

        /// <summary>재시도하지 않는 기본 설정 인스턴스 (일반 API용)</summary>
        public static NetworkRetryOptions None => new NetworkRetryOptions { MaxRetries = 0 };

        /// <summary>중요 로비 진입 및 데이터 저장용 재시도 설정 프리셋</summary>
        public static NetworkRetryOptions LobbyAndSave => new NetworkRetryOptions
        {
            MaxRetries = 3,
            BaseDelaySeconds = 1.0f,
            UseExponentialBackoff = true,
            ShowLoadingOverlay = true,
            FailureToastKey = "error.server_unstable"
        };
    }
}
