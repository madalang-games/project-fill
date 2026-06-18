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
        Manual        = 3, // never auto-fires; triggered on demand (e.g. Pause "How to play" recap)
    }

    public enum TutorialContentType
    {
        FingerOverlay  = 0,
        Tooltip        = 1,
        HighlightOnly  = 2,
        DragPointer    = 3, // ui_drag_pointer line animated from target_ui_id → target_ui_id_to
    }

    // How a step advances. Tap = overlay tap (informational). Select/Move = real board action
    // (interactive: overlay lets the tap through to the board, advances when the action happens).
    public enum TutorialAdvanceMode
    {
        Tap    = 0,
        Select = 1,
        Move   = 2,
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
        WeeklyRankFirst          = 5,
        WeeklyMissionComplete    = 6,
        ChallengeBreakClearCount = 7,
        LoginStreak              = 8,
        ShufflelessWeek          = 9,
        TotalLoginDays           = 10,
        AvatarUnlockCount        = 11,
        CosmeticUnlockCount      = 12,
    }

    public enum WeeklyMissionConditionType
    {
        StageClearCount   = 0,
        PerfectClearCount = 1,
        BoosterlessClear  = 2,
        ChapterProgress   = 3,
        BestMovesRenew    = 4,
    }
}
