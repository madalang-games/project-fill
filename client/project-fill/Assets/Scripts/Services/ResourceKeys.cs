namespace Game.Services
{
    /// <summary>
    /// Centralized <c>dynamic_resource.csv</c> resource_key constants for sprites that are
    /// referenced by a fixed key in code (not resolved from a data column). Use these instead
    /// of raw string literals so a key typo fails at compile time, not silently as a null sprite.
    /// Data-driven keys (item.icon_name, currency.icon_name, iap_product.icon_res) stay in CSV.
    /// </summary>
    public static class ResourceKeys
    {
        // IAP product icons
        public const string IapNoAds = "ui_iap_no_ads";
        public const string IapStarter = "ui_iap_starter";
        public const string IapBundleSmall = "ui_iap_bundle_small";
        public const string IapBundleNormal = "ui_iap_bundle_normal";
        public const string IapBundleLarge = "ui_iap_bundle_large";

        // Lobby event badges
        public const string BadgeDailyLogin = "ui_badge_daily_login";
        public const string BadgeWeeklyMission = "ui_badge_weekly_mission";
    }
}
