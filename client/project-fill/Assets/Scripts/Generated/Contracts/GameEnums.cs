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
}
