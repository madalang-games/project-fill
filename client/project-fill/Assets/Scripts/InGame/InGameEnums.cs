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

    // Maps each booster to its item.csv id — the single source of truth for price (item.price)
    // and inventory counts (PlayerProgressService.GetItemCount). Keeps booster spending off magic numbers.
    public static class BoosterItem
    {
        public const int AddLaneId = 1;
        public const int ShuffleId = 2;
        public const int UndoId    = 4;

        public static int ItemId(this BoosterType type) => type switch
        {
            BoosterType.AddLane => AddLaneId,
            BoosterType.Shuffle => ShuffleId,
            BoosterType.Undo    => UndoId,
            _                   => 0,
        };
    }
}
