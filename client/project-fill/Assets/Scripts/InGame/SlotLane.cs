using System.Collections.Generic;

namespace Game.InGame
{
    public class SlotLane
    {
        public const int Capacity = 4;

        private readonly List<SignalType> _chips = new();

        public IReadOnlyList<SignalType> Chips => _chips;
        public int Count => _chips.Count;
        public bool IsFull  => _chips.Count >= Capacity;
        public bool IsEmpty => _chips.Count == 0;
        public SignalType? TopChip => _chips.Count > 0 ? _chips[^1] : (SignalType?)null;

        public bool CanAccept(SignalType type)
        {
            if (IsFull) return false;
            if (IsEmpty) return true;
            return TopChip!.Value == type;
        }

        public bool IsComplete
        {
            get
            {
                if (_chips.Count != Capacity) return false;
                var first = _chips[0];
                foreach (var c in _chips) if (c != first) return false;
                return true;
            }
        }

        public void Push(SignalType type) => _chips.Add(type);

        public SignalType Pop()
        {
            var top = _chips[^1];
            _chips.RemoveAt(_chips.Count - 1);
            return top;
        }

        public void Clear() => _chips.Clear();

        public SignalType[] ToArray()
        {
            var arr = new SignalType[_chips.Count];
            _chips.CopyTo(arr);
            return arr;
        }

        public void LoadFrom(SignalType[] arr)
        {
            _chips.Clear();
            foreach (var t in arr) _chips.Add(t);
        }
    }
}
