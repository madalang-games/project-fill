namespace Game.Core
{
    public enum AppEnvironment { Dev, Prod }

    /// <summary>
    /// 환경별 서버 주소 및 외부 서비스 클라이언트 ID를 보관합니다.
    /// 향후 추가될 환경 변수(예: WebClientId)도 여기에 매핑합니다.
    /// </summary>
    public static class AppConfig
    {
        public const string DevGameServerUrl            = "http://localhost:20201"; // 개발 서버 URL
        public const string ProdGameServerUrl           = "https://pixelpop.madalang.com"; // TODO: 실제 프로덕션 서버 URL로 교체

        // Google OAuth 2.0 web client ID — Google Cloud Console > APIs & Services > Credentials
        public const string GoogleWebClientId           = "598353589064-33klnpsljo3sia08kaineica4dfpknsg.apps.googleusercontent.com";

        // Google Mobile Ads App IDs
        public const string AdMobAndroidAppIdFree       = "ca-app-pub-5332715773102134/4066441936";
        public const string AdMobAndroidAppIdReward     = "ca-app-pub-5332715773102134/4170678860";

        // Google Mobile Ads App IDs (SDK Settings)
        public const string AdMobAndroidAppId           = "ca-app-pub-3940256099942544~3347511713"; // Test ID (Replace with production App ID before release)
        public const string AdMobIOSAppId               = "ca-app-pub-3940256099942544~1458002511"; // Test ID (Replace with production App ID before release)
    }
}
