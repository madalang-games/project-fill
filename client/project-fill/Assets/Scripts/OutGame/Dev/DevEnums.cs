#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Game.OutGame.Dev
{
    // Client mirror of the server CheatDomain (ProjectFill.Domain.Enums.CheatDomain). DEV-ONLY:
    // drives the cheat overlay button-mode domain tabs + command assembly. Token strings are derived
    // from the enum name (lowercase); never branch on a raw string. Keep ordering in sync with the
    // server catalog so the tabs read in the same Phase 1 → Phase 3 order.
    public enum CheatDomain
    {
        Gold,
        Item,
        Stage,
        Tutorial,
        Ad,
        Cosmetic,
        Achievement,
        Attendance,
    }
}
#endif
