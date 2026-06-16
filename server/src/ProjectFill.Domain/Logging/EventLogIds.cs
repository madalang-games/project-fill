namespace ProjectFill.Domain.Logging;

public static class EventLogIds
{
    public const int StageAttemptStarted = 2101;
    public const int StageAttemptCleared = 2102;
    public const int StageAttemptFailed = 2103;
    public const int StageAttemptReplaced = 2104;
    public const int StageAttemptRevivedByAd = 2105;
    public const int StageClearRewardGranted = 2106;

    public const int InventoryChanged = 2201;

    public const int RewardClaimed = 6001;
    public const int AdRewardClaimed = 6101;
    public const int AdInterstitialShown = 6102;
    public const int AdDoubleRewardGranted = 6103;

    public const int IapPurchaseCompleted = 7001;

    public const int CosmeticUnlocked = 8001;
    public const int CosmeticEquipped = 8002;
    public const int AttendanceClaimed = 8101;
    public const int AchievementClaimed = 8201;
    public const int ChallengeCleared = 8301;
}
