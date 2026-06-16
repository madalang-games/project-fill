using System.Collections.Generic;

namespace Game.InGame
{
    // Builds solvable boards via reverse-path deal: start from the solved arrangement and apply
    // only reversible scatter moves, so the exact inverse sequence always solves the result.
    public static class BoardFactory
    {
        private const int GenNodeCap = 80_000;

        public static Board Generate(StageDefinition def)
        {
            var rng = new System.Random(def.Seed);
            Board last = null;

            for (int attempt = 0; attempt < 8; attempt++)
            {
                last = BuildSolved(def);
                Scramble(last, def.ScrambleSteps, rng);
                // Construction guarantees solvability; verification is cheap insurance.
                if (BoardSolver.IsSolvable(last, GenNodeCap, resultOnCapExceeded: true))
                    return last;
            }
            return last;
        }

        private static Board BuildSolved(StageDefinition def)
        {
            int laneCount = def.LaneCount;
            var lanes = new List<SlotLane>(laneCount);
            for (int i = 0; i < laneCount; i++)
            {
                var kind   = def.LaneKinds[i];
                var unlock = def.LockUnlock != null && i < def.LockUnlock.Length ? def.LockUnlock[i] : SignalType.Red;
                lanes.Add(new SlotLane(kind, unlock));
            }

            // Place each type's complete set into the next available non-locked lane.
            int typeIdx = 0;
            foreach (var lane in lanes)
            {
                if (lane.Kind == LaneKind.Locked) continue;
                if (typeIdx >= def.Types) break;
                var type        = (SignalType)typeIdx;
                bool overloadType = def.OverloadType.HasValue && def.OverloadType.Value == type;
                for (int c = 0; c < SlotLane.Capacity; c++)
                    // Bottom two chips are Overload so the top two normal chips can still scatter.
                    lane.Push(new Chip(type, overloadType && c < 2));
                typeIdx++;
            }

            var relay = def.RelayOrder != null ? new List<SignalType>(def.RelayOrder) : null;
            return new Board(lanes, def.Types, relay);
        }

        // Reshuffle in place: redeal the remaining (unabsorbed) chips into a fresh solvable layout,
        // preserving completed progress, relay progress and lane kinds/locks.
        internal static void Reshuffle(Board board)
        {
            var lanes = board.LanesMutable;

            // Collect remaining chips grouped by type (an unabsorbed type always has exactly 4).
            var byType = new Dictionary<SignalType, List<Chip>>();
            foreach (var lane in lanes)
            {
                foreach (var c in lane.Chips)
                {
                    if (!byType.TryGetValue(c.Type, out var list)) byType[c.Type] = list = new List<Chip>();
                    list.Add(c);
                }
                lane.Clear();
                lane.Pending = false;
            }

            // Refill non-locked lanes with one set per remaining type.
            var fillLanes = new List<SlotLane>();
            foreach (var lane in lanes) if (!lane.Locked) fillLanes.Add(lane);

            int idx = 0;
            foreach (var kv in byType)
            {
                if (idx >= fillLanes.Count) break;
                foreach (var c in kv.Value) fillLanes[idx].Push(c);
                idx++;
            }

            Scramble(board, board.Lanes.Count * 8, new System.Random());
        }

        // ── Reversible scatter scramble ──────────────────────────────────────

        private static void Scramble(Board board, int steps, System.Random rng)
        {
            var lanes = board.LanesMutable;
            (int from, int to) last = (-1, -1);
            var candidates = new List<(int from, int to)>();

            // Extra trailing passes break any lane that ended as a complete set (avoids freebies).
            int total = steps + lanes.Count * 2;
            for (int s = 0; s < total; s++)
            {
                bool needBreak = s >= steps && AnyCompleteLane(lanes);
                if (s >= steps && !needBreak) break;

                candidates.Clear();
                bool hasCompleteSource = false;

                for (int from = 0; from < lanes.Count; from++)
                {
                    var src = lanes[from];
                    if (src.IsEmpty || src.Locked || src.Pending) continue;
                    var top = src.TopChip!.Value;
                    if (!IsReversible(src, top)) continue;

                    for (int to = 0; to < lanes.Count; to++)
                    {
                        if (to == from) continue;
                        if (from == last.to && to == last.from) continue; // don't immediately undo
                        if (!lanes[to].CanAccept(top)) continue;
                        candidates.Add((from, to));
                        if (src.IsComplete) hasCompleteSource = true;
                    }
                }

                if (candidates.Count == 0) break;

                // While breaking freebies, only consider moves out of complete lanes.
                if (needBreak && hasCompleteSource)
                    candidates.RemoveAll(m => !lanes[m.from].IsComplete);

                var pick = candidates[rng.Next(candidates.Count)];
                var chip = lanes[pick.from].Pop();
                lanes[pick.to].Push(chip);          // raw move — never absorb during scramble
                last = pick;
            }
        }

        // A move is reversible iff its source can take the chip back afterwards.
        private static bool IsReversible(SlotLane src, Chip top)
        {
            if (src.Count >= 2) return src.Chips[src.Count - 2].Type == top.Type;
            return !top.Overload; // single chip → src empties → overload can't return
        }

        private static bool AnyCompleteLane(List<SlotLane> lanes)
        {
            foreach (var l in lanes) if (!l.IsEmpty && l.IsComplete) return true;
            return false;
        }
    }
}
