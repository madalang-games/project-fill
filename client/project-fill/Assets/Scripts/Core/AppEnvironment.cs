namespace Game.Core
{
    public enum AppEnvironment { Dev, Prod }

    /// <summary>
    /// 환경별 서버 주소 및 외부 서비스 클라이언트 ID를 보관합니다.
    /// 향후 추가될 환경 변수(예: WebClientId)도 여기에 매핑합니다.
    /// </summary>
    public static class AppConfig
    {
        public const string DevGameServerUrl            = "http://localhost:20301"; // 개발 서버 URL
        public const string ProdGameServerUrl           = "https://popback.madalang.com"; // TODO: 실제 프로덕션 서버 URL로 교체

        // 약관/정책 웹 페이지 베이스 URL (게임 API 서버와 별개)
        public const string DevWebUrl                   = "http://localhost:20002"; // 개발 웹 URL
        public const string ProdWebUrl                  = "https://www.madalang.com"; // 프로덕션 웹 URL
        public const string WebPrivacyPath              = "/privacy"; // 개인정보처리방침 경로
        public const string WebTermsPath                = "/terms"; // 이용약관 경로

        // Google OAuth 2.0 web client ID — Google Cloud Console > APIs & Services > Credentials
        public const string GoogleWebClientId           = "598353589064-au43ludi2ej1kqr9umvg9nab5ugltoqs.apps.googleusercontent.com";

        // Google Mobile Ads App IDs
        public const string AdMobAndroidAppIdFree       = "ca-app-pub-5332715773102134/8364681531";
        public const string AdMobAndroidAppIdReward     = "ca-app-pub-5332715773102134/9691985318";

        // Google Mobile Ads App IDs (SDK Settings)
        public const string AdMobAndroidAppId           = "ca-app-pub-5332715773102134~4555208704"; // Production Android App ID
        public const string AdMobIOSAppId               = "ca-app-pub-3940256099942544~1458002511"; // Test ID (Replace with production App ID before release)

        // Store URLs for force update flow — TODO: update before release
        public const string GooglePlayStoreUrl          = "https://play.google.com/store/apps/details?id=com.madalang.popback";
        public const string AppStoreUrl                 = ""; // TODO: fill in App Store ID before iOS release
    }
}
