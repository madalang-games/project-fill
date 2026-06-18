namespace Game.Services.Tutorial
{
    /// <summary>
    /// Board gimmick kinds that can trigger a GimmickAppear tutorial.
    /// Names must match the tutorial_step.csv `trigger_value` values for GimmickAppear rows
    /// (parsed via Enum.Parse) — never branch on raw strings.
    /// </summary>
    public enum TutorialGimmick
    {
        Locked,
        Blind,
        Relay,
        Overload,
    }
}
