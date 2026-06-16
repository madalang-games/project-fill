namespace Game.InGame
{
    // Per-lane gimmick kind. Pending (Relay) and Overload (chip) are not lane kinds:
    //  - Pending is a transient runtime flag on a lane (see SlotLane.Pending).
    //  - Overload is a per-chip property (see Chip.Overload).
    public enum LaneKind
    {
        Normal = 0, // standard slot lane
        Locked = 1, // Ch2: cannot accept chips until its UnlockType set is registered
        Blind  = 2, // Ch3: only the Top Chip's type is revealed
    }
}
