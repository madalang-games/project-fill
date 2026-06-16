#nullable enable
namespace ProjectFill.Contracts.GameTypes
{
    public enum IapProductType
    {
        NonConsumable = 0,
        Consumable    = 1,
    }

    public enum PurchaseResetPeriod
    {
        None    = 0,
        Daily   = 1,
        Weekly  = 2,
        Monthly = 3,
    }

    public enum TutorialTriggerType
    {
        FirstLaunch   = 0,
        GimmickAppear = 1,
        FailRepeat    = 2,
    }

    public enum TutorialContentType
    {
        FingerOverlay  = 0,
        Tooltip        = 1,
        HighlightOnly  = 2,
    }

    public enum TargetSpaceType
    {
        UI    = 0,
        World = 1,
    }

    public enum CosmeticCategory
    {
        Chip  = 0,
        Lane  = 1,
        Board = 2,
    }

    public enum CosmeticUnlockType
    {
        Default     = 0,
        Gold        = 1,
        Achievement = 2,
        Attendance  = 3,
        Challenge   = 4,
    }

    public enum AttendanceCycleType
    {
        First  = 0,
        Repeat = 1,
    }

    public enum AchievementTier
    {
        Bronze   = 0,
        Silver   = 1,
        Gold     = 2,
        Platinum = 3,
    }

    public enum AchievementCategory
    {
        Progression = 0,
        Skill       = 1,
        Dedication  = 2,
        Collection  = 3,
    }

    public enum AchievementConditionType
    {
        StageClearCount          = 0,
        ChapterComplete          = 1,
        BoosterlessClearCount    = 2,
        BestMovesRenewCount      = 3,
        MoveTopPercentileCount   = 4,
        ChallengeRankFirst       = 5,
        ChallengeClearStreak     = 6,
        ChallengeBreakClearCount = 7,
        LoginStreak              = 8,
        ShufflelessWeek          = 9,
        TotalLoginDays           = 10,
        AvatarUnlockCount        = 11,
        CosmeticUnlockCount      = 12,
    }
}
