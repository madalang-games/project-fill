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

    // Chip cosmetic "material" axis — a surface finish composited OVER the signal-colour body.
    // The body hue ALWAYS stays the signal colour (gameplay identity); the finish only modulates
    // the surface (stipple / scanline / emboss / gloss), never the colour. Flat = original look.
    // Carried by a chip skin (see BoardTheme.ApplyChip); rendered by ChipView via a procedural
    // overlay (TextureFactory.ChipFinishOverlay).
    public enum ChipFinish
    {
        Flat     = 0, // no surface pattern (default — no regression)
        Dither   = 1, // ordered-dither stipple (retro pixel shading)
        Scanline = 2, // horizontal CRT scanlines
        Bevel    = 3, // top highlight + bottom shadow (embossed cell)
        Gloss    = 4, // glossy sheen fading down from the top (glassy)
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
