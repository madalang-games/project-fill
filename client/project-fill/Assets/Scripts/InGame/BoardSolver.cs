using System.Collections.Generic;

namespace Game.InGame
{
    // Depth-first solvability check used for Soft Stuck detection (and generation insurance).
    // Bounded by nodeCap; on cap exhaustion returns resultOnCapExceeded so we never falsely nag.
    public static class BoardSolver
    {
        public static bool IsSolvable(Board board, int nodeCap, bool resultOnCapExceeded)
        {
            var start = board.Clone();
            if (start.IsCleared) return true;

            var visited = new HashSet<string> { start.StateKey() };
            var stack   = new Stack<Board>();
            stack.Push(start);

            int nodes = 0;
            while (stack.Count > 0)
            {
                if (++nodes > nodeCap) return resultOnCapExceeded;

                var cur = stack.Pop();
                foreach (var (from, to) in cur.EnumerateMoves())
                {
                    var next = cur.Clone();
                    next.ApplyMoveRaw(from, to);
                    if (next.IsCleared) return true;
                    if (visited.Add(next.StateKey())) stack.Push(next);
                }
            }
            return false;
        }
    }
}
