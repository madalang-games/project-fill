using System.Collections.Generic;

namespace Game.InGame
{
    public class SlotLane
    {
        public const int Capacity = 4;

        private readonly List<Chip> _chips = new();

        // Gimmick state
        public LaneKind   Kind       { get; }
        public bool       Locked     { get; set; } // Ch2: true while still locked
        public SignalType UnlockType { get; }      // type whose Complete Set unlocks this lane
        public bool       Pending    { get; set; } // Ch4: complete but awaiting relay order

        public SlotLane(LaneKind kind = LaneKind.Normal, SignalType unlockType = SignalType.Red)
        {
            Kind       = kind;
            UnlockType = unlockType;
            Locked     = kind == LaneKind.Locked;
        }

        public IReadOnlyList<Chip> Chips => _chips;
        public int  Count   => _chips.Count;
        public bool IsFull  => _chips.Count >= Capacity;
        public bool IsEmpty => _chips.Count == 0;
        public Chip? TopChip => _chips.Count > 0 ? _chips[^1] : (Chip?)null;

        // A-R02 + gimmick placement rules.
        public bool CanAccept(Chip c)
        {
            if (Locked || Pending || IsFull) return false;
            if (c.Overload && IsEmpty)       return false; // Ch5: overload can't sit alone
            if (IsEmpty)                      return true;
            return _chips[^1].Type == c.Type;
        }

        public bool IsComplete
        {
            get
            {
                if (_chips.Count != Capacity) return false;
                var first = _chips[0].Type;
                foreach (var c in _chips) if (c.Type != first) return false;
                return true;
            }
        }

        public void Push(Chip c) => _chips.Add(c);

        public Chip Pop()
        {
            var top = _chips[^1];
            _chips.RemoveAt(_chips.Count - 1);
            return top;
        }

        public void Clear() => _chips.Clear();

        public SlotLane Clone()
        {
            var clone = new SlotLane(Kind, UnlockType) { Locked = Locked, Pending = Pending };
            clone._chips.AddRange(_chips);
            return clone;
        }
    }
}
