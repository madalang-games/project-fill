using System.Collections.Generic;

namespace Game.InGame
{
    // Declarative description of one stage layout. Drives BoardFactory generation.
    public class StageDefinition
    {
        public string       Name;
        public int          Chapter;
        public int          Types;            // number of Signal Types (= number of sets)
        public LaneKind[]   LaneKinds;        // length = lane count; order = left→right
        public SignalType[] LockUnlock;       // parallel to LaneKinds; unlock type for Locked lanes
        public SignalType?  OverloadType;     // Ch5: all chips of this type are Overload chips
        public SignalType[] RelayOrder;       // Ch4: required completion order (null/empty = none)
        public int          ScrambleSteps;
        public int          Seed;

        public int LaneCount => LaneKinds.Length;
        public string GimmickLabel => Chapter switch
        {
            2 => "LOCKED LANE",
            3 => "BLIND LANE",
            4 => "RELAY NODE",
            5 => "OVERLOAD CHIP",
            _ => "BASIC"
        };
    }

    // One representative sample stage per chapter so every gimmick can be verified at runtime.
    public static class StageLibrary
    {
        public static readonly StageDefinition[] Samples =
        {
            // ── Ch1: basic rules ───────────────────────────────────────────
            new()
            {
                Name = "Ch1 · Neo-Semi", Chapter = 1, Types = 3, Seed = 101, ScrambleSteps = 34,
                LaneKinds  = new[] { N, N, N, N, N },
                LockUnlock = new SignalType[5],
            },

            // ── Ch2: Locked Lane (last lane unlocks when Red set registers) ─
            new()
            {
                Name = "Ch2 · Glyph-Node", Chapter = 2, Types = 4, Seed = 221, ScrambleSteps = 44,
                LaneKinds  = new[] { N, N, N, N, N, L },
                LockUnlock = new[] { R, R, R, R, R, SignalType.Red },
            },

            // ── Ch3: Blind Lane (first two filled lanes hide non-top chips) ─
            new()
            {
                Name = "Ch3 · Diode-Panel", Chapter = 3, Types = 5, Seed = 341, ScrambleSteps = 54,
                LaneKinds  = new[] { B, B, N, N, N, N, N },
                LockUnlock = new SignalType[7],
            },

            // ── Ch4: Relay Node (must register types in ascending order) ────
            new()
            {
                Name = "Ch4 · Quantum-Core", Chapter = 4, Types = 5, Seed = 461, ScrambleSteps = 52,
                LaneKinds  = new[] { N, N, N, N, N, N, N, N },
                LockUnlock = new SignalType[8],
                RelayOrder = new[] { SignalType.Red, SignalType.Blue, SignalType.Green, SignalType.Yellow, SignalType.Purple },
            },

            // ── Ch5: Overload Chip (all Green chips are overload) ───────────
            new()
            {
                Name = "Ch5 · Overload-Nexus", Chapter = 5, Types = 6, Seed = 581, ScrambleSteps = 56,
                LaneKinds    = new[] { N, N, N, N, N, N, N, N, N },
                LockUnlock   = new SignalType[9],
                OverloadType = SignalType.Green,
            },
        };

        // Shorthand for the table above.
        private const LaneKind N = LaneKind.Normal;
        private const LaneKind L = LaneKind.Locked;
        private const LaneKind B = LaneKind.Blind;
        private const SignalType R = SignalType.Red;

        public static StageDefinition Get(int index) => Samples[((index % Samples.Length) + Samples.Length) % Samples.Length];
    }
}
