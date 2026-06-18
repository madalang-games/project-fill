namespace ProjectFill.Application.Common;

public static class ErrorCodes
{
    // --- Infrastructure ---
    public const string Unauthorized           = "UNAUTHORIZED";
    public const string ConcurrentModification = "CONCURRENT_MODIFICATION";
    public const string InternalError          = "INTERNAL_ERROR";
    public const string RateLimited            = "RATE_LIMITED";
    public const string VersionMismatch        = "VERSION_MISMATCH";
    public const string ProtocolMismatch       = "PROTOCOL_MISMATCH";
    public const string UserLockTimeout        = "USER_LOCK_TIMEOUT";
    public const string PlayerUnresolved       = "PLAYER_UNRESOLVED";

    // --- Auth Conflict ---
    public const string ConflictExpired  = "CONFLICT_EXPIRED";
    public const string InvalidConflict  = "INVALID_CONFLICT";
    public const string InvalidSelection = "INVALID_SELECTION";

    // --- Auth / Player ---
    public const string PlayerNotFound  = "PLAYER_NOT_FOUND";
    public const string AccountLocked   = "ACCOUNT_LOCKED";
    public const string AccountBanned   = "ACCOUNT_BANNED";
    public const string SessionExpired  = "SESSION_EXPIRED";
    public const string TokenExpired    = "TOKEN_EXPIRED";
    public const string TokenRevoked    = "TOKEN_REVOKED";

    // --- IAP ---
    public const string AlreadyOwned          = "ALREADY_OWNED";
    public const string DuplicateOrder        = "DUPLICATE_ORDER";
    public const string InvalidProduct        = "INVALID_PRODUCT";
    public const string PurchaseLimitReached  = "PURCHASE_LIMIT_REACHED";

    // --- Stage ---
    public const string StageNotFound        = "STAGE_NOT_FOUND";
    public const string StageLocked          = "STAGE_LOCKED";
    public const string StageAlreadyActive   = "STAGE_ALREADY_ACTIVE";
    public const string InvalidStageAttempt  = "INVALID_STAGE_ATTEMPT";
    public const string StageAttemptExpired  = "STAGE_ATTEMPT_EXPIRED";
    public const string StageRulesetMismatch = "STAGE_RULESET_MISMATCH";
    public const string InvalidStageClear    = "INVALID_STAGE_CLEAR";
    public const string ReviveLimitExceeded  = "REVIVE_LIMIT_EXCEEDED";

    // --- Inventory ---
    public const string InvalidAmount     = "INVALID_AMOUNT";
    public const string InsufficientItems = "INSUFFICIENT_ITEMS";
    public const string ItemNotFound      = "ITEM_NOT_FOUND";

    // --- Player ---
    public const string InvalidDisplayNameLength = "INVALID_DISPLAY_NAME_LENGTH";
    public const string InvalidDisplayNameChar   = "INVALID_DISPLAY_NAME_CHAR";
    public const string AvatarNotFound           = "AVATAR_NOT_FOUND";
    public const string AvatarLocked             = "AVATAR_LOCKED";

    // --- Rewards ---
    public const string RewardSourceNotFound = "REWARD_SOURCE_NOT_FOUND";

    // --- Currency ---
    public const string InsufficientCurrency = "INSUFFICIENT_CURRENCY";
    public const string InsufficientFunds    = "INSUFFICIENT_FUNDS";

    // --- Rewards ---
    public const string RewardAlreadyClaimed = "REWARD_ALREADY_CLAIMED";

    // --- Ads ---
    public const string AdRewardDuplicate       = "AD_REWARD_DUPLICATE";
    public const string AdRewardVerifyFailed    = "AD_REWARD_VERIFY_FAILED";
    public const string AdSsvPending            = "AD_SSV_PENDING";
    public const string DoubleRewardNotEligible = "DOUBLE_REWARD_NOT_ELIGIBLE";
    public const string AdTimeout               = "AD_TIMEOUT";
    public const string AdPlacementNotSupported = "AD_PLACEMENT_NOT_SUPPORTED";

    // --- Ranking ---
    public const string InvalidRankingType = "INVALID_RANKING_TYPE";

    // --- Cosmetic ---
    public const string CosmeticNotFound       = "COSMETIC_NOT_FOUND";
    public const string CosmeticAlreadyOwned   = "COSMETIC_ALREADY_OWNED";
    public const string CosmeticNotOwned       = "COSMETIC_NOT_OWNED";
    public const string CosmeticNotPurchasable = "COSMETIC_NOT_PURCHASABLE";
    public const string CosmeticCategoryMismatch = "COSMETIC_CATEGORY_MISMATCH";

    // --- Attendance ---
    public const string AttendanceAlreadyClaimed = "ATTENDANCE_ALREADY_CLAIMED";

    // --- Achievement ---
    public const string AchievementNotFound      = "ACHIEVEMENT_NOT_FOUND";
    public const string AchievementNotCompleted  = "ACHIEVEMENT_NOT_COMPLETED";
    public const string AchievementAlreadyClaimed = "ACHIEVEMENT_ALREADY_CLAIMED";

    // --- Weekly Mission Event ---
    public const string WeeklyMissionInvalidThreshold     = "WEEKLY_MISSION_INVALID_THRESHOLD";
    public const string WeeklyMissionThresholdNotReached  = "WEEKLY_MISSION_THRESHOLD_NOT_REACHED";
    public const string WeeklyMissionAlreadyClaimed       = "WEEKLY_MISSION_ALREADY_CLAIMED";

    // --- Dev Cheat (dev-only; never surfaced to prod clients — endpoint is 404 in prod) ---
    public const string InvalidCommand = "INVALID_COMMAND";
    public const string Forbidden      = "FORBIDDEN";
}
