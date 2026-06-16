using System;

namespace Game.InGame
{
    // Hand-off from the daily-challenge popup to the InGame scene. The board is generated from the
    // server's stageSeed so every player worldwide solves the identical puzzle (design: 전 세계 동일 퍼즐).
    // Pure C# (no UnityEngine) so it stays testable and platform-stable.
    public static class ChallengeContext
    {
        public static bool Active { get; private set; }
        public static int  Seed;
        public static int  Types;
        public static int  LaneCount;
        public static int  GimmickId;

        public static void Set(string stageSeed, int types, int laneCount, int gimmickId)
        {
            Active    = true;
            Seed      = StableHash(stageSeed);
            Types     = types;
            LaneCount = laneCount;
            GimmickId = gimmickId;
        }

        public static void Clear() => Active = false;

        // GimmickId → gimmick (mirrors the chapter intro order: 1=Locked 2=Blind 3=Relay 4=Overload; 0=none).
        public static StageDefinition BuildDefinition()
        {
            int lanes  = Math.Max(LaneCount, Types + 1);
            var kinds  = new LaneKind[lanes];
            var unlock = new SignalType[lanes];
            for (int i = 0; i < lanes; i++) kinds[i] = LaneKind.Normal;

            SignalType[] relay = null;
            SignalType?  overload = null;
            switch (GimmickId)
            {
                case 1: kinds[lanes - 1] = LaneKind.Locked; unlock[lanes - 1] = SignalType.Red; break;
                case 2: kinds[0] = LaneKind.Blind; if (lanes > 1) kinds[1] = LaneKind.Blind; break;
                case 3: relay = new SignalType[Types]; for (int i = 0; i < Types; i++) relay[i] = (SignalType)i; break;
                case 4: overload = SignalType.Green; break;
            }

            return new StageDefinition
            {
                Name          = "DAILY CHALLENGE",
                Chapter       = 0,
                Types         = Types,
                Seed          = Seed,
                ScrambleSteps = Types * 10,
                LaneKinds     = kinds,
                LockUnlock    = unlock,
                RelayOrder    = relay,
                OverloadType  = overload,
            };
        }

        // Stable across runs/platforms (string.GetHashCode is randomized per-process) → identical board everywhere.
        private static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                if (s != null) foreach (char c in s) h = h * 31 + c;
                return h;
            }
        }
    }
}
