using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// Signal Sort stage generator CLI.
// Mirrors the runtime board model (client/.../InGame/{Chip,SlotLane,Board,BoardFactory,BoardSolver}.cs)
// so a (StageDefinition, seed) pair reproduces the exact same board the Unity client builds.
// Adds an editor-only scoring layer: sample many seeds, batch-solve each, score difficulty, keep best.

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

try
{
    if (args.Length != 1)
    {
        Console.Error.WriteLine("Usage: StageGenerator.Cli <request-json-path>");
        return 2;
    }

    var request = JsonSerializer.Deserialize<GeneratorRequest>(File.ReadAllText(args[0]), jsonOptions);
    if (request is null)
    {
        Console.Error.WriteLine("Invalid request JSON.");
        return 2;
    }

    var result = StageGenerator.Generate(request);
    Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

// ── JSON contract ────────────────────────────────────────────────────────────

public sealed record GeneratorRequest(
    int Types,
    string LaneKinds,     // per-lane: 'N' Normal, 'L' Locked, 'B' Blind  (length = lane count)
    string LockUnlock,    // per-lane unlock glyph or '.'; "" = no locks
    int OverloadType,     // -1 = none, else SignalType index
    string RelayOrder,    // glyph sequence e.g. "RBGYP"; "" = no relay
    int ScrambleSteps,
    int Difficulty,       // 0 Easy / 1 Normal / 2 Hard — drives scoring only
    int Seed,             // reproduce mode: rebuild this exact board
    bool Reproduce,
    int MaxAttempts);

public sealed record ChipDto(int Type, bool Overload);
public sealed record LaneDto(int Kind, bool Locked, int UnlockType, ChipDto[] Chips);
public sealed record GeneratorResult(
    LaneDto[] Lanes,
    int Types,
    int Seed,
    int Attempts,
    int SolveLength,
    string VerifiedSolution,
    double Score);

// ── Runtime-mirrored model ─────────────────────────────────────────────────────

public enum SignalType
{
    Red = 0, Blue = 1, Green = 2, Yellow = 3, Purple = 4,
    Cyan = 5, Orange = 6, Magenta = 7, Lime = 8, Teal = 9
}

public enum LaneKind { Normal = 0, Locked = 1, Blind = 2 }

public readonly struct Chip
{
    public readonly SignalType Type;
    public readonly bool Overload;
    public Chip(SignalType type, bool overload = false) { Type = type; Overload = overload; }
}

public sealed class SlotLane
{
    public const int Capacity = 4;
    private readonly List<Chip> _chips = new();

    public LaneKind   Kind       { get; }
    public bool       Locked     { get; set; }
    public SignalType UnlockType { get; }
    public bool       Pending    { get; set; }

    public SlotLane(LaneKind kind = LaneKind.Normal, SignalType unlockType = SignalType.Red)
    {
        Kind = kind;
        UnlockType = unlockType;
        Locked = kind == LaneKind.Locked;
    }

    public IReadOnlyList<Chip> Chips => _chips;
    public int  Count   => _chips.Count;
    public bool IsFull  => _chips.Count >= Capacity;
    public bool IsEmpty => _chips.Count == 0;
    public Chip? TopChip => _chips.Count > 0 ? _chips[^1] : (Chip?)null;

    public bool CanAccept(Chip c)
    {
        if (Locked || Pending || IsFull) return false;
        if (c.Overload && IsEmpty)       return false;
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
    public Chip Pop() { var t = _chips[^1]; _chips.RemoveAt(_chips.Count - 1); return t; }
    public void Clear() => _chips.Clear();

    public SlotLane Clone()
    {
        var clone = new SlotLane(Kind, UnlockType) { Locked = Locked, Pending = Pending };
        clone._chips.AddRange(_chips);
        return clone;
    }
}

public sealed class Board
{
    private readonly List<SlotLane>   _lanes;
    private readonly List<SignalType> _relayOrder;
    private readonly List<SignalType> _registered = new();
    private int _relayProgress;

    public IReadOnlyList<SlotLane> Lanes => _lanes;
    public List<SlotLane> LanesMutable => _lanes;
    public bool HasRelay  => _relayOrder.Count > 0;
    public int  MoveCount    { get; private set; }
    public int  CompletedSets { get; private set; }
    public int  TotalSets     { get; }
    public bool IsCleared     => CompletedSets >= TotalSets;

    public Board(List<SlotLane> lanes, int totalSets, List<SignalType>? relayOrder = null)
    {
        _lanes = lanes;
        TotalSets = totalSets;
        _relayOrder = relayOrder ?? new List<SignalType>();
    }

    public bool CanMoveTo(int from, int to)
    {
        if (from == to || (uint)from >= _lanes.Count || (uint)to >= _lanes.Count) return false;
        var src = _lanes[from];
        if (src.IsEmpty || src.Pending) return false;
        return _lanes[to].CanAccept(src.TopChip!.Value);
    }

    // Batch move (player-facing semantics, matches runtime Board.Move minus undo/FX).
    public void ApplyMoveBatch(int from, int to)
    {
        var type = _lanes[from].TopChip!.Value.Type;
        while (!_lanes[from].IsEmpty
            && _lanes[from].TopChip!.Value.Type == type
            && _lanes[to].CanAccept(_lanes[from].TopChip!.Value))
        {
            _lanes[to].Push(_lanes[from].Pop());
        }
        MoveCount++;
        ResolveCompletions();
    }

    // Single-chip move (solver insurance path, matches runtime Board.ApplyMoveRaw).
    public void ApplyMoveRaw(int from, int to)
    {
        var chip = _lanes[from].Pop();
        _lanes[to].Push(chip);
        MoveCount++;
        ResolveCompletions();
    }

    private void ResolveCompletions()
    {
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
                    if (_relayProgress < _relayOrder.Count && _relayOrder[_relayProgress] == type)
                    {
                        lane.Pending = false;
                        Absorb(lane, type);
                        _relayProgress++;
                        changed = true;
                    }
                    else if (!lane.Pending)
                    {
                        lane.Pending = true;
                    }
                }
                else
                {
                    Absorb(lane, type);
                    changed = true;
                }
            }
        }
    }

    private void Absorb(SlotLane lane, SignalType type)
    {
        lane.Clear();
        CompletedSets++;
        _registered.Add(type);
        foreach (var l in _lanes)
            if (l.Locked && l.UnlockType == type) l.Locked = false;
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

    public Board Clone()
    {
        var lanes = new List<SlotLane>(_lanes.Count);
        foreach (var l in _lanes) lanes.Add(l.Clone());
        var b = new Board(lanes, TotalSets, new List<SignalType>(_relayOrder))
        {
            _relayProgress = _relayProgress,
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
}

// Verbatim runtime solvability check (single-chip moves) — used as generation insurance.
public static class BoardSolver
{
    public static bool IsSolvable(Board board, int nodeCap, bool resultOnCapExceeded)
    {
        var start = board.Clone();
        if (start.IsCleared) return true;

        var visited = new HashSet<string> { start.StateKey() };
        var stack = new Stack<Board>();
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

// Verbatim runtime reverse-path generation — deterministic for a given seed.
public sealed class StageDef
{
    public int          Types;
    public LaneKind[]   LaneKinds = Array.Empty<LaneKind>();
    public SignalType[] LockUnlock = Array.Empty<SignalType>();
    public SignalType?  OverloadType;
    public SignalType[] RelayOrder = Array.Empty<SignalType>();
    public int          ScrambleSteps;
    public int          Seed;
    public int LaneCount => LaneKinds.Length;
}

public static class BoardFactory
{
    private const int GenNodeCap = 80_000;
    private const int InternalAttempts = 60; // reshuffles per seed before giving up

    // Seeded random-fill + solvability verification. Robust for any reasonable lane/type config
    // (the runtime's reverse-scramble deadlocks into freebies for normal counts). `ScrambleSteps`
    // is kept on the def for forward compatibility but no longer drives the deal.
    public static Board? Generate(StageDef def)
    {
        var rng = new Random(def.Seed);
        for (int attempt = 0; attempt < InternalAttempts; attempt++)
        {
            var board = BuildRandom(def, rng);
            if (board is null) return null;                                  // config can never fit
            if (AnyCompleteLane(board)) continue;                            // A-R09 no-freebie
            if (!BoardSolver.IsSolvable(board.Clone(), GenNodeCap, false)) continue;
            return board;
        }
        return null;
    }

    private static Board? BuildRandom(StageDef def, Random rng)
    {
        var lanes = new List<SlotLane>(def.LaneCount);
        var fillIdx = new List<int>();
        for (int i = 0; i < def.LaneCount; i++)
        {
            var kind   = def.LaneKinds[i];
            var unlock = i < def.LockUnlock.Length ? def.LockUnlock[i] : SignalType.Red;
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

        return new Board(lanes, def.Types,
            def.RelayOrder.Length > 0 ? new List<SignalType>(def.RelayOrder) : null);
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static bool AnyCompleteLane(Board board)
    {
        foreach (var l in board.Lanes) if (!l.IsEmpty && l.IsComplete) return true;
        return false;
    }
}

// Editor-only batch solver — a player-facing move sequence that clears the board.
// Node-capped DFS returning the first clear found (a solution always exists by reverse-scramble
// construction). Length is near-minimal — precise enough for difficulty scoring, far cheaper than
// an optimal BFS whose frontier explodes on ball-sort state spaces.
public static class BatchSolver
{
    public static List<(int from, int to)>? Solve(Board board, int nodeCap)
    {
        var start = board.Clone();
        if (start.IsCleared) return new List<(int, int)>();

        var visited = new HashSet<string> { start.StateKey() };
        var path = new List<(int, int)>();
        int nodes = 0;
        return Dfs(start, visited, path, ref nodes, nodeCap) ? path : null;
    }

    private static bool Dfs(Board cur, HashSet<string> visited, List<(int, int)> path, ref int nodes, int nodeCap)
    {
        if (cur.IsCleared) return true;
        if (++nodes > nodeCap) return false;

        // Prefer moves that complete or grow same-type stacks first (faster descent to a clear).
        var moves = cur.EnumerateMoves().ToList();
        moves.Sort((a, b) => MovePriority(cur, b) - MovePriority(cur, a));

        foreach (var (from, to) in moves)
        {
            var next = cur.Clone();
            next.ApplyMoveBatch(from, to);
            var key = next.StateKey();
            if (!visited.Add(key)) continue;
            path.Add((from, to));
            if (Dfs(next, visited, path, ref nodes, nodeCap)) return true;
            path.RemoveAt(path.Count - 1);
            if (nodes > nodeCap) return false;
        }
        return false;
    }

    // Heuristic: stacking onto a matching non-empty lane, or emptying the source, is most productive.
    private static int MovePriority(Board b, (int from, int to) m)
    {
        var src = b.Lanes[m.from];
        var dst = b.Lanes[m.to];
        int p = dst.IsEmpty ? 0 : 2;              // stacking onto same type

        var type = src.TopChip!.Value.Type;       // length of the same-type run at the source top
        int run = 0;
        for (int i = src.Count - 1; i >= 0 && src.Chips[i].Type == type; i--) run++;
        if (run == src.Count) p += 1;             // whole lane is one run → move can empty it
        return p;
    }
}

public static class StageGenerator
{
    private const int SolveNodeCap = 120_000;

    public static GeneratorResult? Generate(GeneratorRequest request)
    {
        var def = BuildDef(request);
        if (def.LaneCount == 0 || def.Types <= 0) return null;

        if (request.Reproduce)
        {
            def.Seed = request.Seed;
            return Evaluate(def, request.Difficulty, request.Seed, attempts: 1);
        }

        if (request.MaxAttempts <= 0) return null;

        GeneratorResult? best = null;
        var sync = new object();
        var baseSeed = Environment.TickCount;

        Parallel.For(1, request.MaxAttempts + 1, attempt =>
        {
            var seed = unchecked(baseSeed * 31 + attempt * 7919);
            if (seed == 0) seed = attempt;
            var localDef = BuildDef(request);
            localDef.Seed = seed;
            var candidate = Evaluate(localDef, request.Difficulty, seed, attempt);
            if (candidate is null) return;
            lock (sync)
            {
                if (best is null || candidate.Score > best.Score) best = candidate;
            }
        });

        return best;
    }

    private static GeneratorResult? Evaluate(StageDef def, int difficulty, int seed, int attempts)
    {
        var board = BoardFactory.Generate(def);
        if (board is null) return null; // config can't produce a clean solvable board

        var solution = BatchSolver.Solve(board.Clone(), SolveNodeCap);
        if (solution is null || solution.Count == 0) return null;

        double score = ScoreCandidate(def, difficulty, solution.Count, board);
        var lanes = board.Lanes.Select(l => new LaneDto(
            (int)l.Kind, l.Locked, (int)l.UnlockType,
            l.Chips.Select(c => new ChipDto((int)c.Type, c.Overload)).ToArray())).ToArray();
        var solutionStr = string.Join(';', solution.Select(m => $"{m.from},{m.to}"));

        return new GeneratorResult(lanes, def.Types, seed, attempts, solution.Count, solutionStr, score);
    }

    // Difficulty-fit scoring (flood-style: reward target length + gimmick engagement, penalise drift).
    private static double ScoreCandidate(StageDef def, int difficulty, int solveLength, Board board)
    {
        double score = 1000.0;
        int idealMoves = difficulty == 0 ? def.Types + 2
                       : difficulty == 1 ? (int)Math.Round(def.Types * 2.0) + 2
                       :                   (int)Math.Round(def.Types * 3.0) + 2;
        double driftW  = difficulty == 0 ? 14 : difficulty == 1 ? 22 : 30;
        double lengthW = difficulty == 0 ? 4  : difficulty == 1 ? 8  : 12;

        score -= Math.Abs(solveLength - idealMoves) * driftW;
        score += Math.Min(solveLength, 40) * lengthW;

        // Trivial boards (solvable in fewer moves than there are sets) are bad at any difficulty.
        if (solveLength < def.Types) score -= (def.Types - solveLength) * 60;

        // Gimmick engagement bonuses scale with difficulty.
        int locked = board.Lanes.Count(l => l.Kind == LaneKind.Locked);
        int blind  = board.Lanes.Count(l => l.Kind == LaneKind.Blind);
        if (locked > 0)               score += (difficulty == 0 ? 20 : 60) * locked;
        if (blind > 0)                score += (difficulty == 0 ? 10 : 30) * blind;
        if (def.RelayOrder.Length > 0) score += difficulty == 0 ? 25 : 80;
        if (def.OverloadType.HasValue) score += difficulty == 0 ? 20 : 70;

        return score;
    }

    private static StageDef BuildDef(GeneratorRequest r)
    {
        var kinds = (r.LaneKinds ?? "").Select(ParseKind).ToArray();
        var unlock = new SignalType[kinds.Length];
        for (int i = 0; i < kinds.Length; i++)
        {
            SignalType u = SignalType.Red;
            if (!string.IsNullOrEmpty(r.LockUnlock) && i < r.LockUnlock.Length)
            {
                var g = r.LockUnlock[i];
                if (g != '.' && TryParseGlyph(g, out var st)) u = st;
            }
            unlock[i] = u;
        }
        SignalType? overload = r.OverloadType >= 0 ? (SignalType)r.OverloadType : null;
        var relay = (r.RelayOrder ?? "")
            .Where(g => TryParseGlyph(g, out _))
            .Select(g => { TryParseGlyph(g, out var st); return st; })
            .ToArray();

        return new StageDef
        {
            Types = r.Types,
            LaneKinds = kinds,
            LockUnlock = unlock,
            OverloadType = overload,
            RelayOrder = relay,
            ScrambleSteps = r.ScrambleSteps,
            Seed = r.Seed,
        };
    }

    private static LaneKind ParseKind(char c) => c switch
    {
        'L' or 'l' => LaneKind.Locked,
        'B' or 'b' => LaneKind.Blind,
        _          => LaneKind.Normal,
    };

    private static readonly string Glyphs = "RBGYPCOMLT"; // SignalType order

    private static bool TryParseGlyph(char g, out SignalType type)
    {
        var idx = Glyphs.IndexOf(char.ToUpperInvariant(g));
        if (idx < 0) { type = SignalType.Red; return false; }
        type = (SignalType)idx;
        return true;
    }
}
