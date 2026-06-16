using System.Collections.Generic;
using System.Text;

namespace Game.InGame
{
    // Encodes/decodes the compact board layout stored in stage.csv `board` column.
    // Fixed Capacity chars per lane, no delimiters (read in groups of Capacity, lane order = csv order):
    //   uppercase glyph = normal chip   (R B G Y P C O M L T = SignalType 0..9)
    //   lowercase glyph = overload chip
    //   '-'             = empty slot     (empty lane = "----")
    // Lane kind / locked / unlock come from the StageDefinition (lane_kinds + lock_unlock), not from here.
    // Mirror of tools/stage_editor/src/lib/board-codec.ts — keep both in sync.
    public static class BoardCodec
    {
        private const string Glyphs = "RBGYPCOMLT";

        public static Board Decode(string board, StageDefinition def)
        {
            int laneCount = def.LaneCount;
            var lanes = new List<SlotLane>(laneCount);
            for (int i = 0; i < laneCount; i++)
            {
                var kind   = def.LaneKinds[i];
                var unlock = def.LockUnlock != null && i < def.LockUnlock.Length ? def.LockUnlock[i] : SignalType.Red;
                var lane   = new SlotLane(kind, unlock);

                int baseIdx = i * SlotLane.Capacity;
                for (int s = 0; s < SlotLane.Capacity; s++)
                {
                    int p = baseIdx + s;
                    if (board == null || p >= board.Length) break;
                    char ch = board[p];
                    if (ch == '-') continue;
                    int t = Glyphs.IndexOf(char.ToUpperInvariant(ch));
                    if (t < 0) continue;
                    lane.Push(new Chip((SignalType)t, char.IsLower(ch)));
                }
                lanes.Add(lane);
            }

            var relay = def.RelayOrder != null ? new List<SignalType>(def.RelayOrder) : null;
            return new Board(lanes, def.Types, relay);
        }

        public static string Encode(Board board)
        {
            var sb = new StringBuilder(board.Lanes.Count * SlotLane.Capacity);
            foreach (var lane in board.Lanes)
            {
                for (int s = 0; s < SlotLane.Capacity; s++)
                {
                    if (s < lane.Chips.Count)
                    {
                        var c = lane.Chips[s];
                        char g = Glyphs[(int)c.Type];
                        sb.Append(c.Overload ? char.ToLowerInvariant(g) : g);
                    }
                    else sb.Append('-');
                }
            }
            return sb.ToString();
        }
    }
}
