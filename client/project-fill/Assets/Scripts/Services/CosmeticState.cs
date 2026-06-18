using ProjectFill.Contracts.Cosmetic;

namespace Game.Services
{
    // Session cache of the player's active cosmetics, populated by CosmeticApiService
    // (Fetch/SetActive). Lets other scenes — e.g. the InGame board background — read the
    // active board skin without issuing another network request.
    public static class CosmeticState
    {
        public static string ActiveChipSkin     { get; private set; } = string.Empty;
        public static string ActiveLaneSkin     { get; private set; } = string.Empty;
        public static string ActiveBoardSkin    { get; private set; } = string.Empty;
        public static bool   UseCustomBoardSkin { get; private set; }
        public static bool   HasData            { get; private set; }

        public static void Set(ActiveCosmeticsDto a)
        {
            if (a == null) return;
            ActiveChipSkin     = a.ChipSkin  ?? string.Empty;
            ActiveLaneSkin     = a.LaneSkin  ?? string.Empty;
            ActiveBoardSkin    = a.BoardSkin ?? string.Empty;
            UseCustomBoardSkin = a.UseCustomBoardSkin;
            HasData            = true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // DEV-ONLY: force the active cosmetics for the in-game skin switcher (no server round-trip).
        // Compiled out of release builds. See InGame.Controller.DevSkinSwitcher.
        public static void DevOverride(string board, string chip, string lane)
        {
            ActiveBoardSkin    = board ?? string.Empty;
            ActiveChipSkin     = chip  ?? string.Empty;
            ActiveLaneSkin     = lane  ?? string.Empty;
            UseCustomBoardSkin = true;
            HasData            = true;
        }
#endif

        // Board skin to render in-game: the custom override if enabled, else the default.
        public static string ResolveBoardSkin()
            => UseCustomBoardSkin && !string.IsNullOrEmpty(ActiveBoardSkin)
                ? ActiveBoardSkin
                : "board_default";
    }
}
