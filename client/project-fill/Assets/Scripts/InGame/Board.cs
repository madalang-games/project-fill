using System.Collections.Generic;
using System.Text;

namespace Game.InGame
{
    public class Board
    {
        private readonly List<SlotLane>     _lanes;
        private readonly List<SignalType>   _relayOrder;       // empty = no Relay constraint
        private readonly List<SignalType>   _registered = new(); // types absorbed into the Signal Panel
        private readonly Stack<BoardSnapshot> _undoStack = new();
        private bool _addLaneUsed;
        private int  _relayProgress;

        public IReadOnlyList<SlotLane>   Lanes          => _lanes;
        public IReadOnlyList<SignalType> RelayOrder     => _relayOrder;
        public IReadOnlyList<SignalType> RegisteredTypes => _registered;
        public int  RelayProgress => _relayProgress;
        public bool HasRelay      => _relayOrder.Count > 0;
        public int  MoveCount     { get; private set; }
        public int  CompletedSets { get; private set; }
        public int  TotalSets     { get; }
        public bool IsCleared     => CompletedSets >= TotalSets;
        public bool AddLaneUsed   => _addLaneUsed;
        public bool CanUndo       => _undoStack.Count > 0;

        public Board(List<SlotLane> lanes, int totalSets, List<SignalType> relayOrder = null)
        {
            _lanes      = lanes;
            TotalSets   = totalSets;
            _relayOrder = relayOrder ?? new List<SignalType>();
        }

        // ── Move rules ───────────────────────────────────────────────────────

        public bool CanMoveTo(int from, int to)
        {
            if (from == to || (uint)from >= _lanes.Count || (uint)to >= _lanes.Count) return false;
            var src = _lanes[from];
            if (src.IsEmpty || src.Pending) return false; // A-R01 / pending lanes are sealed
            return _lanes[to].CanAccept(src.TopChip!.Value);
        }

        // Returns the lanes absorbed by this move (lane index + type) for completion FX.
        // Empty list = legal move without completion. Null = illegal move.
        public List<(int lane, SignalType type)> Move(int from, int to)
        {
            if (!CanMoveTo(from, to)) return null;
            SaveSnapshot();
            var chip = _lanes[from].Pop();
            _lanes[to].Push(chip);
            MoveCount++;
            return ResolveCompletions();
        }

        // Absorb any complete lanes, honoring Relay order + Locked-lane unlocks. Cascades.
        private List<(int lane, SignalType type)> ResolveCompletions()
        {
            var absorbed = new List<(int, SignalType)>();
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < _lanes.Count; i++)
                {
                    var lane = _lanes[i];
                    if (!lane.IsComplete) continue;

                    var type = lane.Chips[0].Type;

                    if (HasRelay)
                    {
                        // Only the next type in the relay order may register.
                        if (_relayProgress < _relayOrder.Count && _relayOrder[_relayProgress] == type)
                        {
                            lane.Pending = false;
                            Absorb(lane, type, absorbed, i);
                            _relayProgress++;
                            changed = true;
                        }
                        else if (!lane.Pending)
                        {
                            lane.Pending = true; // wrong order → wait
                        }
                    }
                    else
                    {
                        Absorb(lane, type, absorbed, i);
                        changed = true;
                    }
                }
            }
            return absorbed;
        }

        private void Absorb(SlotLane lane, SignalType type, List<(int, SignalType)> absorbed, int index)
        {
            lane.Clear();
            CompletedSets++;
            _registered.Add(type);
            absorbed.Add((index, type));
            // Ch2: registering this type unlocks any lane keyed to it.
            foreach (var l in _lanes)
                if (l.Locked && l.UnlockType == type) l.Locked = false;
        }

        // ── Stuck detection ──────────────────────────────────────────────────

        public bool IsHardStuck()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i].IsEmpty || _lanes[i].Pending) continue;
                var top = _lanes[i].TopChip!.Value;
                for (int j = 0; j < _lanes.Count; j++)
                {
                    if (i == j) continue;
                    if (_lanes[j].CanAccept(top)) return false;
                }
            }
            return true;
        }

        // ── Boosters ───────────────────────────────────────────────────────

        public bool Undo()
        {
            if (_undoStack.Count == 0) return false;
            _undoStack.Pop().Restore(this);
            return true;
        }

        public bool TryAddLane()
        {
            if (_addLaneUsed) return false;
            SaveSnapshot();
            _lanes.Add(new SlotLane());
            _addLaneUsed = true;
            return true;
        }

        // Reverse-path reshuffle: solvable-by-construction redeal of the current chips.
        public void Shuffle()
        {
            SaveSnapshot();
            BoardFactory.Reshuffle(this);
        }

        // ── Solver / generation support ──────────────────────────────────────

        internal List<SlotLane>   LanesMutable   => _lanes;
        internal List<SignalType> RelayOrderList => _relayOrder;
        internal void SetRelayProgress(int v)    => _relayProgress = v;
        internal void RestoreCounters(int completedSets, int moveCount)
        {
            CompletedSets = completedSets;
            MoveCount     = moveCount;
        }
        internal void RestoreRegistered(IReadOnlyList<SignalType> reg)
        {
            _registered.Clear();
            _registered.AddRange(reg);
        }

        public IEnumerable<(int from, int to)> EnumerateMoves()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i].IsEmpty || _lanes[i].Pending) continue;
                var top = _lanes[i].TopChip!.Value;
                for (int j = 0; j < _lanes.Count; j++)
                {
                    if (i == j) continue;
                    if (_lanes[j].CanAccept(top)) yield return (i, j);
                }
            }
        }

        // Apply a move without undo bookkeeping (solver use).
        public void ApplyMoveRaw(int from, int to)
        {
            var chip = _lanes[from].Pop();
            _lanes[to].Push(chip);
            MoveCount++;
            ResolveCompletions();
        }

        public Board Clone()
        {
            var lanes = new List<SlotLane>(_lanes.Count);
            foreach (var l in _lanes) lanes.Add(l.Clone());
            var b = new Board(lanes, TotalSets, new List<SignalType>(_relayOrder))
            {
                _relayProgress = _relayProgress,
                _addLaneUsed   = _addLaneUsed,
                CompletedSets  = CompletedSets,
                MoveCount      = MoveCount,
            };
            b._registered.AddRange(_registered);
            return b;
        }

        public string StateKey()
        {
            var sb = new StringBuilder(_lanes.Count * 12);
            foreach (var lane in _lanes)
            {
                sb.Append(lane.Locked ? 'L' : lane.Pending ? 'P' : '.');
                foreach (var c in lane.Chips)
                {
                    sb.Append((char)('0' + (int)c.Type));
                    if (c.Overload) sb.Append('!');
                }
                sb.Append('/');
            }
            sb.Append('#').Append(_relayProgress);
            return sb.ToString();
        }

        private void SaveSnapshot() => _undoStack.Push(new BoardSnapshot(this));
    }

    // Full-state snapshot for Undo (lanes + gimmick flags + counters).
    public class BoardSnapshot
    {
        private readonly SlotLane[]   _lanes;
        private readonly SignalType[] _registered;
        private readonly int  _completedSets;
        private readonly int  _moveCount;
        private readonly int  _relayProgress;

        public BoardSnapshot(Board board)
        {
            var src = board.Lanes;
            _lanes = new SlotLane[src.Count];
            for (int i = 0; i < src.Count; i++) _lanes[i] = src[i].Clone();
            var reg = board.RegisteredTypes;
            _registered = new SignalType[reg.Count];
            for (int i = 0; i < reg.Count; i++) _registered[i] = reg[i];
            _completedSets = board.CompletedSets;
            _moveCount     = board.MoveCount;
            _relayProgress = board.RelayProgress;
        }

        public void Restore(Board board)
        {
            var lanes = board.LanesMutable;
            lanes.Clear();
            lanes.AddRange(_lanes);
            board.SetRelayProgress(_relayProgress);
            // CompletedSets / MoveCount are private set on Board — reflect via internal setters.
            board.RestoreCounters(_completedSets, _moveCount);
            board.RestoreRegistered(_registered);
        }
    }
}
