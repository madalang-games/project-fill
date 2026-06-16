namespace Game.InGame
{
    // A single signal chip. Overload chips (Ch5) cannot be placed onto an empty lane.
    public readonly struct Chip
    {
        public readonly SignalType Type;
        public readonly bool Overload;

        public Chip(SignalType type, bool overload = false)
        {
            Type     = type;
            Overload = overload;
        }
    }
}
