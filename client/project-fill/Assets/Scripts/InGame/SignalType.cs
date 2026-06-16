using UnityEngine;

namespace Game.InGame
{
    // 10 signal colors (Chapter 5 uses up to 10 types). Index = enum value.
    public enum SignalType
    {
        Red = 0, Blue = 1, Green = 2, Yellow = 3, Purple = 4,
        Cyan = 5, Orange = 6, Magenta = 7, Lime = 8, Teal = 9
    }

    // Spec defines exactly 3 boosters (Undo / Shuffle / Add Lane). No Hint.
    public enum BoosterType { Undo, Shuffle, AddLane }

    public static class SignalTypeExtensions
    {
        public const int Count = 10;

        private static readonly Color[] Colors =
        {
            new(1.00f, 0.27f, 0.34f), // Red
            new(0.27f, 0.53f, 1.00f), // Blue
            new(0.27f, 1.00f, 0.53f), // Green
            new(1.00f, 0.84f, 0.27f), // Yellow
            new(0.71f, 0.27f, 1.00f), // Purple
            new(0.27f, 1.00f, 1.00f), // Cyan
            new(1.00f, 0.53f, 0.27f), // Orange
            new(1.00f, 0.30f, 0.72f), // Magenta
            new(0.66f, 1.00f, 0.30f), // Lime
            new(0.20f, 0.80f, 0.74f), // Teal
        };

        // Distinct glyph per type so colorblind players can still read the board.
        private static readonly string[] Glyphs =
        {
            "R", "B", "G", "Y", "P", "C", "O", "M", "L", "T"
        };

        public static Color ToColor(this SignalType t) => Colors[(int)t];

        public static string ToLabel(this SignalType t) => Glyphs[(int)t];
    }
}
