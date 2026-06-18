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
    public const int WeeklyMissionClaimed = 8301;

    // Dev cheat audit (dev-only).
    public const int CheatGold = 9001;
    public const int CheatItem = 9002;
    public const int CheatStage = 9003;
    public const int CheatTutorial = 9004;
    public const int CheatAd = 9005;
    public const int CheatCosmetic = 9006;
    public const int CheatAchievement = 9007;
    public const int CheatAttendance = 9008;
}
