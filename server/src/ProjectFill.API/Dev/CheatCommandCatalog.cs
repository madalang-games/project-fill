using ProjectFill.Domain.Enums;

namespace ProjectFill.API.Dev;

// Single source of truth for cheat command metadata. Shared by the parser (domain validation),
// the docs page (HTML rendering), and the client button mode. Add a command = edit one row here.
public sealed record CheatCommandSpec(
    CheatDomain Domain,
    string Token,
    string Syntax,
    string Description,
    string Example,
    string ResponseShape);

public static class CheatCommandCatalog
{
    public static readonly IReadOnlyList<CheatCommandSpec> All = new[]
    {
        new CheatCommandSpec(CheatDomain.Gold, "gold",
            "/gold add|red|set {amount}",
            "Add / subtract / set soft gold (clamped 0..999,999,999).",
            "/gold set 99999", "{ balanceAfter }"),
        new CheatCommandSpec(CheatDomain.Item, "item",
            "/item {id|all} add|red|set {amount}",
            "Adjust booster item counts; 'all' targets every item.",
            "/item all set 99", "{ inventoryAfter: {id:qty} }"),
        new CheatCommandSpec(CheatDomain.Stage, "stage",
            "/stage set {stageId}",
            "Clear up to stageId; drops progress beyond it; updates max_cleared_stage_id.",
            "/stage set 50", "{ highestStageAfter }"),
        new CheatCommandSpec(CheatDomain.Tutorial, "tutorial",
            "/tutorial {id|all} true|false",
            "Mark a tutorial seen/unseen. 'all' supports only false (clear all).",
            "/tutorial all false", "{ seenTutorialIds }"),
        new CheatCommandSpec(CheatDomain.Ad, "ad",
            "/ad true|false",
            "Toggle the ad-bypass flag (Redis cheat:ad:{uid}).",
            "/ad true", "null"),
        new CheatCommandSpec(CheatDomain.Cosmetic, "cosmetic",
            "/cosmetic {id|all} unlock|lock",
            "Unlock / lock cosmetic skins; 'all' targets every cosmetic.",
            "/cosmetic all unlock", "{ unlockedCosmeticIds }"),
        new CheatCommandSpec(CheatDomain.Achievement, "achievement",
            "/achievement {id|all} complete|reset",
            "Force achievement progress complete / reset; 'all' targets every achievement.",
            "/achievement all complete", "{ achievementStateAfter: {id:completed} }"),
        new CheatCommandSpec(CheatDomain.Attendance, "attendance",
            "/attendance setday {n}|reset",
            "Force attendance cycle day (1..7, re-claimable) or reset attendance.",
            "/attendance setday 5", "{ attendanceDay }"),
    };

    public static bool TryGet(string token, out CheatCommandSpec? spec)
    {
        spec = All.FirstOrDefault(s => s.Token.Equals(token, StringComparison.OrdinalIgnoreCase));
        return spec is not null;
    }
}
