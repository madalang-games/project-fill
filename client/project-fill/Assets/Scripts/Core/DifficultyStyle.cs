using UnityEngine;

namespace Game.Core
{
    public static class DifficultyStyle
    {
        public static readonly Color Normal = new Color(0.267f, 0.533f, 1f,    1f); // #4488FF neon blue
        public static readonly Color Hard   = new Color(1f,    0.278f, 0.341f, 1f); // #FF4757 coral red

        public static Color Get(int difficulty, Color easyFallback = default) => difficulty switch
        {
            1 => Normal,
            2 => Hard,
            _ => easyFallback
        };
    }
}
