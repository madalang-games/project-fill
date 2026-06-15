using System;
using System.Collections.Generic;

namespace Game.InGame
{
    public class Board
    {
        private readonly List<SlotLane> _lanes;
        private readonly Stack<BoardSnapshot> _undoStack = new();
        private bool _addLaneUsed;

        public IReadOnlyList<SlotLane> Lanes => _lanes;
        public int MoveCount      { get; private set; }
        public int CompletedSets  { get; private set; }
        public int TotalSets      { get; }
        public bool IsCleared     => CompletedSets >= TotalSets;
        public bool AddLaneUsed   => _addLaneUsed;

        public Board(List<SlotLane> lanes, int totalSets)
        {
            _lanes    = lanes;
            TotalSets = totalSets;
        }

        public bool CanMoveTo(int from, int to)
        {
            if (from == to || from < 0 || to < 0 || from >= _lanes.Count || to >= _lanes.Count) return false;
            if (_lanes[from].IsEmpty) return false;
            return _lanes[to].CanAccept(_lanes[from].TopChip!.Value);
        }

        public bool Move(int from, int to)
        {
            if (!CanMoveTo(from, to)) return false;
            SaveSnapshot();
            var chip = _lanes[from].Pop();
            _lanes[to].Push(chip);
            MoveCount++;
            if (_lanes[to].IsComplete)
            {
                _lanes[to].Clear();
                CompletedSets++;
            }
            return true;
        }

        public bool Undo()
        {
            if (_undoStack.Count == 0) return false;
            var snap = _undoStack.Pop();
            snap.Restore(_lanes);
            CompletedSets = snap.CompletedSets;
            MoveCount     = Math.Max(0, MoveCount - 1);
            return true;
        }

        public void Shuffle()
        {
            var chips = new List<SignalType>();
            foreach (var lane in _lanes) chips.AddRange(lane.Chips);

            var rng = new Random();
            for (int i = chips.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (chips[i], chips[j]) = (chips[j], chips[i]);
            }

            foreach (var lane in _lanes) lane.Clear();
            int idx = 0;
            foreach (var lane in _lanes)
                while (idx < chips.Count && !lane.IsFull)
                    lane.Push(chips[idx++]);
        }

        public (int from, int to)? GetHint()
        {
            // Prefer: move to a lane that already has same type chips (progress toward complete)
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i].IsEmpty) continue;
                var top = _lanes[i].TopChip!.Value;
                for (int j = 0; j < _lanes.Count; j++)
                {
                    if (i == j || _lanes[j].IsEmpty) continue;
                    if (_lanes[j].CanAccept(top)) return (i, j);
                }
            }
            // Fallback: any valid move
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i].IsEmpty) continue;
                var top = _lanes[i].TopChip!.Value;
                for (int j = 0; j < _lanes.Count; j++)
                {
                    if (i == j) continue;
                    if (_lanes[j].CanAccept(top)) return (i, j);
                }
            }
            return null;
        }

        public bool IsStuck()
        {
            for (int i = 0; i < _lanes.Count; i++)
            {
                if (_lanes[i].IsEmpty) continue;
                var top = _lanes[i].TopChip!.Value;
                for (int j = 0; j < _lanes.Count; j++)
                {
                    if (i == j) continue;
                    if (_lanes[j].CanAccept(top)) return false;
                }
            }
            return true;
        }

        public bool TryAddLane()
        {
            if (_addLaneUsed) return false;
            _lanes.Add(new SlotLane());
            _addLaneUsed = true;
            return true;
        }

        private void SaveSnapshot() => _undoStack.Push(new BoardSnapshot(_lanes, CompletedSets));
    }

    public class BoardSnapshot
    {
        private readonly SignalType[][] _data;
        public int CompletedSets { get; }

        public BoardSnapshot(IReadOnlyList<SlotLane> lanes, int completedSets)
        {
            CompletedSets = completedSets;
            _data = new SignalType[lanes.Count][];
            for (int i = 0; i < lanes.Count; i++)
                _data[i] = lanes[i].ToArray();
        }

        public void Restore(List<SlotLane> lanes)
        {
            while (lanes.Count > _data.Length) lanes.RemoveAt(lanes.Count - 1);
            for (int i = 0; i < _data.Length; i++)
                lanes[i].LoadFrom(_data[i]);
        }
    }
}
