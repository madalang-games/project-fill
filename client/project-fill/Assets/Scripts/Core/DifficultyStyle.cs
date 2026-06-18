using UnityEngine;

namespace Game.Core
{
    public static class DifficultyStyle
    {
        public static readonly Color Normal = new Color(0.118f, 0.855f, 0.769f, 1f); // #1EDAC4 neon teal
        public static readonly Color Hard   = new Color(0.863f, 0.078f, 0.235f, 1f); // #DC143C crimson

        public static Color Get(int difficulty, Color easyFallback = default) => difficulty switch
        {
            1 => Normal,
            2 => Hard,
            _ => easyFallback
        };
    }
}
