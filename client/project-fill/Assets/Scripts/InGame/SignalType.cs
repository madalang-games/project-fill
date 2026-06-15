using UnityEngine;

namespace Game.InGame
{
    public enum SignalType { Red = 0, Blue = 1, Green = 2, Yellow = 3, Purple = 4, Cyan = 5, Orange = 6 }

    public enum BoosterType { Undo, Hint, Shuffle, AddLane }

    public static class SignalTypeExtensions
    {
        private static readonly Color[] Colors =
        {
            new(1.00f, 0.27f, 0.34f), // Red
            new(0.27f, 0.53f, 1.00f), // Blue
            new(0.27f, 1.00f, 0.53f), // Green
            new(1.00f, 0.84f, 0.27f), // Yellow
            new(0.71f, 0.27f, 1.00f), // Purple
            new(0.27f, 1.00f, 1.00f), // Cyan
            new(1.00f, 0.53f, 0.27f), // Orange
        };

        public static Color ToColor(this SignalType t) => Colors[(int)t];

        public static string ToLabel(this SignalType t) => t switch
        {
            SignalType.Red    => "R",
            SignalType.Blue   => "B",
            SignalType.Green  => "G",
            SignalType.Yellow => "Y",
            SignalType.Purple => "P",
            SignalType.Cyan   => "C",
            SignalType.Orange => "O",
            _                 => "?"
        };
    }
}
