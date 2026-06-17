using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// Signal Sort stage generator CLI.
// Mirrors the runtime board model (client/.../InGame/{Chip,SlotLane,Board,BoardFactory}.cs).
// Adds an editor-only layer: sample many seeds, exact-shortest-solve each (BFS over canonical states),
// score difficulty, keep the best. The board is persisted explicitly, so byte-parity with the runtime
// generator is not required.

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
    string LaneKinds,     // explicit mode: per-lane 'N'/'L'/'B' (length = lane count). Randomize mode: only length is read (= total lanes)
    string LockUnlock,    // per-lane unlock glyph or '.'; "" = no locks
    int OverloadType,     // -1 = none, else SignalType index
    string RelayOrder,    // glyph sequence e.g. "RBGYP"; "" = no relay
    int ScrambleSteps,
    int Difficulty,       // 0 Easy / 1 Normal / 2 Hard — drives scoring only
    int Seed,             // reproduce mode: rebuild this exact board
    bool Reproduce,
    int MaxAttempts,
    // Randomize mode: place gimmicks by count with random colors instead of explicit per-lane painting.
    int LockCount = 0,         // # Locked lanes (random positions, random unlock color)
    int BlindCount = 0,        // # Blind lanes (random positions)
    bool RandomizeGimmicks = false,  // true → ignore explicit LaneKinds/LockUnlock placement, use counts below
    bool RandomOverload = false,     // true → include overload with a random color (else OverloadType)
    bool RandomRelay = false,        // true → include relay with a random full-Types permutation (else RelayOrder)
    // Solve-only mode: decode an explicit `Board` (+ gimmick columns) and return its optimal solveLength.
    // Used by the par_moves migration; no generation/scoring.
    bool SolveOnly = false,
    string Board = "");

public sealed record ChipDto(int Type, bool Overload);
public sealed record LaneDto(int Kind, bool Locked, int UnlockType, ChipDto[] Chips);
public sealed record GeneratorResult(
    LaneDto[] Lanes,
    int Types,
    int Seed,
    int Attempts,
    int SolveLength,
    string VerifiedSolution,
    double Score,
    // Resolved gimmick layout (echoed so the editor can persist what randomize actually produced).
    string LaneKinds,
    string LockUnlock,
    int OverloadType,
    string RelayOrder);

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

    // Lane-order-invariant state key. Lanes are interchangeable in this game (moves reference lanes by
    // content, not index), so two boards differing only by a lane permutation are the same position —
    // sorting the per-lane signatures collapses them, shrinking the search space by orders of magnitude.
    // Mechanically-relevant identity only: Locked + unlock color (when locked), Pending (relay), chips,
    // and board-level relay progress. Kind (Blind vs Normal) and a former-locked lane's unlock color do
    // not affect any rule (CanAccept/IsComplete/Move ignore them), so they are omitted for tighter dedup.
    public string CanonicalKey()
    {
        var sigs = new List<string>(_lanes.Count);
        foreach (var lane in _lanes)
        {
            var sb = new StringBuilder(8);
            if (lane.Locked) { sb.Append('L'); sb.Append((char)('0' + (int)lane.UnlockType)); }
            else if (lane.Pending) sb.Append('P');
            else sb.Append('.');
            foreach (var c in lane.Chips)
            {
                sb.Append((char)('0' + (int)c.Type));
                if (c.Overload) sb.Append('!');
            }
            sigs.Add(sb.ToString());
        }
        sigs.Sort(StringComparer.Ordinal);
        var outSb = new StringBuilder(_lanes.Count * 8);
        foreach (var s in sigs) { outSb.Append(s); outSb.Append('/'); }
        outSb.Append('#').Append(_relayProgress);
        return outSb.ToString();
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
    private const int InternalAttempts = 60; // re-rolls per seed to dodge freebies before giving up

    // Seeded random-fill. Re-rolls only to avoid pre-completed (freebie) lanes — solvability is no
    // longer checked here (the exact BatchSolver proves it once while scoring; a board that can't be
    // cleared simply yields no candidate). `ScrambleSteps` is kept on the def for forward compatibility
    // but no longer drives the deal.
    public static Board? Generate(StageDef def, CancellationToken ct = default)
    {
        var rng = new Random(def.Seed);
        for (int attempt = 0; attempt < InternalAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            var board = BuildRandom(def, rng);
            if (board is null) return null;                                  // config can never fit
            if (AnyCompleteLane(board)) continue;                            // A-R09 no-freebie
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

        // Blind on an empty lane is a no-op gimmick (the player only ever stacks chips they placed
        // themselves, so nothing is truly hidden). Bias the fill so Blind lanes land in the initially
        // filled set — stable order keeps the shuffle random within each group.
        fillIdx = fillIdx.OrderByDescending(i => lanes[i].Kind == LaneKind.Blind).ToList();

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

// Editor-only batch solver — the SHORTEST player-facing move sequence that clears the board.
// BFS over canonical (lane-order-invariant) states: canonical dedup collapses lane permutations so the
// frontier stays tractable even at higher type counts, and BFS guarantees the returned length is the
// true minimum number of player moves — a valid, heuristic-free difficulty metric. Returns null if no
// clear is reachable within `nodeCap` (treated as "this seed produced no usable board").
public static class BatchSolver
{
    public static List<(int from, int to)>? Solve(Board board, int nodeCap, CancellationToken ct = default)
    {
        var start = board.Clone();
        if (start.IsCleared) return new List<(int, int)>();

        var startKey = start.CanonicalKey();
        // canonicalKey → (predecessor canonicalKey, move that produced this state)
        var parent = new Dictionary<string, (string? prev, int from, int to)> { [startKey] = (null, 0, 0) };
        var queue = new Queue<(Board board, string key)>();
        queue.Enqueue((start, startKey));

        int nodes = 0;
        while (queue.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            if (++nodes > nodeCap) return null;
            var (cur, curKey) = queue.Dequeue();
            foreach (var (from, to) in cur.EnumerateMoves())
            {
                var next = cur.Clone();
                next.ApplyMoveBatch(from, to);
                var key = next.CanonicalKey();
                if (parent.ContainsKey(key)) continue;
                parent[key] = (curKey, from, to);
                if (next.IsCleared) return Reconstruct(parent, key);
                queue.Enqueue((next, key));
            }
        }
        return null;
    }

    private static List<(int from, int to)> Reconstruct(
        Dictionary<string, (string? prev, int from, int to)> parent, string endKey)
    {
        var moves = new List<(int, int)>();
        var key = endKey;
        while (true)
        {
            var (prev, from, to) = parent[key];
            if (prev is null) break;
            moves.Add((from, to));
            key = prev;
        }
        moves.Reverse();
        return moves;
    }
}

public static class StageGenerator
{
    private const int SolveNodeCap = 120_000;

    // Safety budget: cap total generation wall-clock and parallel fan-out so a large
    // lane/type config can never spin the CLI (and its memory) without bound. On expiry
    // the loop is cancelled cooperatively and the best candidate found so far is returned.
    private static readonly TimeSpan TimeBudget = TimeSpan.FromSeconds(20);
    private const int MaxAttemptsCap = 500;

    public static GeneratorResult? Generate(GeneratorRequest request)
    {
        if (request.Types <= 0 || (request.LaneKinds ?? "").Length == 0) return null;

        if (request.SolveOnly)
            return SolveExplicit(request);

        if (request.Reproduce)
        {
            var def = BuildDef(request, request.Seed);
            if (def.LaneCount == 0) return null;
            def.Seed = request.Seed;
            return Evaluate(def, request.Difficulty, request.Seed, attempts: 1, CancellationToken.None);
        }

        if (request.MaxAttempts <= 0) return null;
        int maxAttempts = Math.Min(request.MaxAttempts, MaxAttemptsCap);

        GeneratorResult? best = null;
        var sync = new object();
        var baseSeed = Environment.TickCount;

        using var cts = new CancellationTokenSource(TimeBudget);
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1),
            CancellationToken = cts.Token,
        };

        try
        {
            Parallel.For(1, maxAttempts + 1, options, attempt =>
            {
                var seed = unchecked(baseSeed * 31 + attempt * 7919);
                if (seed == 0) seed = attempt;
                var localDef = BuildDef(request, seed);
                localDef.Seed = seed;
                var candidate = Evaluate(localDef, request.Difficulty, seed, attempt, cts.Token);
                if (candidate is null) return;
                lock (sync)
                {
                    if (best is null || candidate.Score > best.Score) best = candidate;
                }
            });
        }
        catch (OperationCanceledException) { /* time budget hit — return best-so-far */ }

        return best;
    }

    private static GeneratorResult? Evaluate(StageDef def, int difficulty, int seed, int attempts, CancellationToken ct)
    {
        var board = BoardFactory.Generate(def, ct);
        if (board is null) return null; // config can't produce a clean solvable board

        var solution = BatchSolver.Solve(board.Clone(), SolveNodeCap, ct);
        if (solution is null || solution.Count == 0) return null;

        double score = ScoreCandidate(def, difficulty, solution.Count, board);
        var lanes = board.Lanes.Select(l => new LaneDto(
            (int)l.Kind, l.Locked, (int)l.UnlockType,
            l.Chips.Select(c => new ChipDto((int)c.Type, c.Overload)).ToArray())).ToArray();
        var solutionStr = string.Join(';', solution.Select(m => $"{m.from},{m.to}"));

        // Echo the resolved gimmick layout so the editor persists exactly what was produced
        // (essential in randomize mode, where the editor never specified these).
        var laneKinds = new string(def.LaneKinds.Select(KindToChar).ToArray());
        var lockUnlock = new string(Enumerable.Range(0, def.LaneCount)
            .Select(i => def.LaneKinds[i] == LaneKind.Locked ? Glyphs[(int)def.LockUnlock[i]] : '.').ToArray());
        var overloadType = def.OverloadType.HasValue ? (int)def.OverloadType.Value : -1;
        var relayOrder = new string(def.RelayOrder.Select(t => Glyphs[(int)t]).ToArray());

        return new GeneratorResult(lanes, def.Types, seed, attempts, solution.Count, solutionStr, score,
            laneKinds, lockUnlock, overloadType, relayOrder);
    }

    // Decode an explicit board layout (stage.csv `board` column) and return its optimal solveLength.
    private static GeneratorResult? SolveExplicit(GeneratorRequest request)
    {
        var def = BuildDef(request, request.Seed);
        if (def.LaneCount == 0) return null;

        var board = BuildExplicitBoard(def, request.Board ?? "");
        var solution = BatchSolver.Solve(board.Clone(), SolveNodeCap);
        if (solution is null) return null; // not solvable within cap

        var lanes = board.Lanes.Select(l => new LaneDto(
            (int)l.Kind, l.Locked, (int)l.UnlockType,
            l.Chips.Select(c => new ChipDto((int)c.Type, c.Overload)).ToArray())).ToArray();
        var solutionStr = string.Join(';', solution.Select(m => $"{m.from},{m.to}"));
        var laneKinds = new string(def.LaneKinds.Select(KindToChar).ToArray());
        var lockUnlock = new string(Enumerable.Range(0, def.LaneCount)
            .Select(i => def.LaneKinds[i] == LaneKind.Locked ? Glyphs[(int)def.LockUnlock[i]] : '.').ToArray());
        var overloadType = def.OverloadType.HasValue ? (int)def.OverloadType.Value : -1;
        var relayOrder = new string(def.RelayOrder.Select(t => Glyphs[(int)t]).ToArray());

        return new GeneratorResult(lanes, def.Types, request.Seed, 0, solution.Count, solutionStr, 0,
            laneKinds, lockUnlock, overloadType, relayOrder);
    }

    // Mirrors tools/stage_editor lib/board-codec: 4 chars/lane, csv order, bottom→top,
    // UPPER=normal lower=overload '-'=empty. Lane kind/lock come from the def, not the board string.
    private static Board BuildExplicitBoard(StageDef def, string boardStr)
    {
        var lanes = new List<SlotLane>(def.LaneCount);
        for (int i = 0; i < def.LaneCount; i++)
        {
            var lane = new SlotLane(def.LaneKinds[i],
                i < def.LockUnlock.Length ? def.LockUnlock[i] : SignalType.Red);
            int start = i * SlotLane.Capacity;
            for (int c = 0; c < SlotLane.Capacity && start + c < boardStr.Length; c++)
            {
                char ch = boardStr[start + c];
                if (ch == '-') continue;
                int idx = Glyphs.IndexOf(char.ToUpperInvariant(ch));
                if (idx < 0) continue;
                lane.Push(new Chip((SignalType)idx, char.IsLower(ch)));
            }
            lanes.Add(lane);
        }
        return new Board(lanes, def.Types,
            def.RelayOrder.Length > 0 ? new List<SignalType>(def.RelayOrder) : null);
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

    // Builds a concrete StageDef for one attempt. In randomize mode the gimmick layout is derived
    // from the per-seed RNG (different attempts explore different layouts; scoring keeps the best).
    private static StageDef BuildDef(GeneratorRequest r, int seed)
    {
        int laneCount = (r.LaneKinds ?? "").Length;
        if (r.RandomizeGimmicks && !r.Reproduce)
            return BuildRandomizedDef(r, seed, laneCount);

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

        return new StageDef
        {
            Types = r.Types,
            LaneKinds = kinds,
            LockUnlock = unlock,
            OverloadType = overload,
            RelayOrder = ParseRelay(r.RelayOrder),
            ScrambleSteps = r.ScrambleSteps,
            Seed = r.Seed,
        };
    }

    // Randomize mode: scatter LockCount/BlindCount lanes at random positions with random colors,
    // honour the "always include, value random" overload/relay flags. Per-seed deterministic.
    private static StageDef BuildRandomizedDef(GeneratorRequest r, int seed, int laneCount)
    {
        var rng = new Random(seed);
        var kinds = new LaneKind[laneCount];   // all Normal by default
        var unlock = new SignalType[laneCount];

        var idx = Enumerable.Range(0, laneCount).ToList();
        Shuffle(idx, rng);
        int p = 0;
        for (int n = 0; n < Math.Max(0, r.LockCount) && p < laneCount; n++, p++)
        {
            kinds[idx[p]] = LaneKind.Locked;
            unlock[idx[p]] = (SignalType)rng.Next(r.Types);
        }
        // Blind is only meaningful on a filled lane, and exactly `Types` lanes are filled, so cap it there.
        int blindN = Math.Min(Math.Max(0, r.BlindCount), r.Types);
        for (int n = 0; n < blindN && p < laneCount; n++, p++)
            kinds[idx[p]] = LaneKind.Blind;

        SignalType? overload =
            r.RandomOverload ? (SignalType)rng.Next(r.Types)
            : r.OverloadType >= 0 ? (SignalType)r.OverloadType
            : null;

        SignalType[] relay;
        if (r.RandomRelay)
        {
            var order = Enumerable.Range(0, r.Types).Select(t => (SignalType)t).ToList();
            Shuffle(order, rng);
            relay = order.ToArray();
        }
        else relay = ParseRelay(r.RelayOrder);

        return new StageDef
        {
            Types = r.Types,
            LaneKinds = kinds,
            LockUnlock = unlock,
            OverloadType = overload,
            RelayOrder = relay,
            ScrambleSteps = r.ScrambleSteps,
            Seed = seed,
        };
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static SignalType[] ParseRelay(string? s) => (s ?? "")
        .Where(g => TryParseGlyph(g, out _))
        .Select(g => { TryParseGlyph(g, out var st); return st; })
        .ToArray();

    private static LaneKind ParseKind(char c) => c switch
    {
        'L' or 'l' => LaneKind.Locked,
        'B' or 'b' => LaneKind.Blind,
        _          => LaneKind.Normal,
    };

    private static char KindToChar(LaneKind k) => k switch
    {
        LaneKind.Locked => 'L',
        LaneKind.Blind  => 'B',
        _               => 'N',
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
