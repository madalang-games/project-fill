#if UNITY_EDITOR
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// project-fill UI color palette — Dark Neon Puzzle style.
    /// Used exclusively by UIEditorSetup when generating prefabs.
    /// InGame colors (chips, board) are managed via shared/datas/common/color_palette.csv.
    /// </summary>
    internal static class UIColorPalette
    {
        public static Color UI_BG_DEEP  => Hex("0B0B1E"); // Deep midnight navy (base background)
        public static Color UI_BG_MID   => Hex("181836"); // Dark indigo (panel fills)
        public static Color UI_PRIMARY  => Hex("4CC9F0"); // Electric cyan (tabs, secondary buttons)
        public static Color UI_CTA      => Hex("F72585"); // Hot neon magenta (primary CTA)
        public static Color UI_SUCCESS  => Hex("06D6A0"); // Neon teal (success, play)
        public static Color UI_DANGER   => Hex("EF233C"); // Neon red (danger, close)
        public static Color UI_TEXT     => Hex("E8E8FF"); // Ice white (all text)
        public static Color UI_BORDER   => Hex("7B2FBE"); // Electric violet (borders, accents)
        public static Color DIM         => new Color(0.02f, 0.02f, 0.08f, 0.80f); // Near-black overlay
        public static Color UI_SHADOW   => new Color(0.01f, 0.01f, 0.04f, 0.90f); // Pixel art drop shadow (near-black)

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out Color c); return c; }
    }
}
#endif
