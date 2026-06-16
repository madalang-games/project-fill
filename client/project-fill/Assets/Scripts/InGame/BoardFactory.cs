using System.Collections.Generic;

namespace Game.InGame
{
    // Mirror of tools/stage_generator (Program.cs) BoardFactory: seeded random-fill + solvability
    // verification. A (StageDefinition, seed) pair reproduces the EXACT board the stage_editor CLI
    // scored and persisted to stage.csv. The legacy reverse-scramble deal is gone — it deadlocked
    // into freebie Complete Sets for normal lane counts and did not match the CLI.
    //
    // Determinism contract (must stay byte-for-byte aligned with the CLI):
    //   • new System.Random(seed) created once, before the attempt loop (seeded Random is identical
    //     across .NET 8 and Unity Mono/IL2CPP — both use the legacy subtractive generator)
    //   • per attempt: BuildRandom does Shuffle(pool) then Shuffle(fillIdx); accept iff no complete
    //     lane AND solvable (single-chip DFS, GenNodeCap, resultOnCapExceeded=false)
    //   • EnumerateMoves / ApplyMoveRaw / StateKey are shared with BoardSolver and the CLI verbatim
    public static class BoardFactory
    {
        private const int GenNodeCap       = 80_000;
        private const int InternalAttempts = 60;

        // Returns null only if the config can never fit (≤ types non-locked lanes) or no clean
        // solvable board appears within InternalAttempts. For a CLI-authored seed this always yields
        // the same board the CLI produced.
        public static Board Generate(StageDefinition def)
        {
            var rng = new System.Random(def.Seed);
            for (int attempt = 0; attempt < InternalAttempts; attempt++)
            {
                var board = BuildRandom(def, rng);
                if (board == null) return null;                              // config can never fit
                if (AnyCompleteLane(board.LanesMutable)) continue;           // A-R09 no-freebie
                if (!BoardSolver.IsSolvable(board.Clone(), GenNodeCap, false)) continue;
                return board;
            }
            return null;
        }

        private static Board BuildRandom(StageDefinition def, System.Random rng)
        {
            int laneCount = def.LaneCount;
            var lanes   = new List<SlotLane>(laneCount);
            var fillIdx = new List<int>();
            for (int i = 0; i < laneCount; i++)
            {
                var kind   = def.LaneKinds[i];
                var unlock = def.LockUnlock != null && i < def.LockUnlock.Length ? def.LockUnlock[i] : SignalType.Red;
                lanes.Add(new SlotLane(kind, unlock));
                if (kind != LaneKind.Locked) fillIdx.Add(i);
            }
            // Ball-sort solvability needs at least one spare empty lane beyond the filled sets.
            if (fillIdx.Count <= def.Types) return null;

            var pool = new List<Chip>(def.Types * SlotLane.Capacity);
            for (int t = 0; t < def.Types; t++)
            {
                bool ov = def.OverloadType.HasValue && (int)def.OverloadType.Value == t;
                for (int c = 0; c < SlotLane.Capacity; c++)
                    pool.Add(new Chip((SignalType)t, ov && c < 2)); // 2 overload chips per overload set
            }
            Shuffle(pool, rng);
            Shuffle(fillIdx, rng);

            // Fill `Types` lanes to capacity from the shuffled pool; remaining fill lanes stay empty.
            for (int k = 0; k < def.Types; k++)
            {
                var lane = lanes[fillIdx[k]];
                for (int c = 0; c < SlotLane.Capacity; c++)
                    lane.Push(pool[k * SlotLane.Capacity + c]);
            }

            var relay = def.RelayOrder != null && def.RelayOrder.Length > 0
                ? new List<SignalType>(def.RelayOrder) : null;
            return new Board(lanes, def.Types, relay);
        }

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static bool AnyCompleteLane(List<SlotLane> lanes)
        {
            foreach (var l in lanes) if (!l.IsEmpty && l.IsComplete) return true;
            return false;
        }

        // Reshuffle in place (Shuffle booster): redeal the remaining (unabsorbed) chips into a fresh
        // solvable layout, preserving completed progress and lane kinds/locks. Not seed-reproduced —
        // uses the same random-fill + verify strategy as Generate with a fresh Random.
        internal static void Reshuffle(Board board)
        {
            var lanes = board.LanesMutable;

            // Collect remaining chips grouped by type (an unabsorbed type always has exactly 4).
            var byType    = new Dictionary<SignalType, List<Chip>>();
            var typeOrder = new List<SignalType>();
            foreach (var lane in lanes)
            {
                foreach (var c in lane.Chips)
                {
                    if (!byType.TryGetValue(c.Type, out var list))
                    {
                        byType[c.Type] = list = new List<Chip>();
                        typeOrder.Add(c.Type);
                    }
                    list.Add(c);
                }
                lane.Clear();
                lane.Pending = false;
            }

            var fillIdx = new List<int>();
            for (int i = 0; i < lanes.Count; i++) if (!lanes[i].Locked) fillIdx.Add(i);

            int typeCount = typeOrder.Count;
            var rng = new System.Random();
            for (int attempt = 0; attempt < InternalAttempts; attempt++)
            {
                foreach (var i in fillIdx) lanes[i].Clear();

                var pool = new List<Chip>(typeCount * SlotLane.Capacity);
                foreach (var t in typeOrder) pool.AddRange(byType[t]);
                Shuffle(pool, rng);
                Shuffle(fillIdx, rng);

                int place = typeCount < fillIdx.Count ? typeCount : fillIdx.Count;
                for (int k = 0; k < place; k++)
                {
                    var lane = lanes[fillIdx[k]];
                    for (int c = 0; c < SlotLane.Capacity; c++)
                        lane.Push(pool[k * SlotLane.Capacity + c]);
                }

                if (AnyCompleteLane(lanes)) continue;
                if (BoardSolver.IsSolvable(board.Clone(), GenNodeCap, false)) return;
            }
            // Best effort: leave the last deal as-is if no verified-solvable layout was found.
        }
    }
}
