namespace ProjectFill.Domain.Enums;

// Dev cheat command domains. The token strings live in CheatCommandCatalog (parse/doc metadata);
// branching is on this enum, never on raw strings.
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

// Verbs a cheat command can carry. Mapped from per-domain action tokens by CheatCommandParser.
public enum CheatAction
{
    Add,
    Reduce,
    Set,
    Enable,
    Disable,
}
