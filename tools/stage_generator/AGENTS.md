# stage_generator — Signal Sort Board Generator CLI

Standalone .NET 8 console app invoked by `tools/stage_editor` (`/api/generate-board`) via `dotnet exec`.
Reads a generator request (JSON file path arg), writes the result as JSON to stdout. No project deps.

## Files
| file | role |
|------|------|
| `Program.cs` | Entire engine + scoring + JSON contract (single file) |
| `StageGenerator.Cli.csproj` | net8.0 Exe; AssemblyName `StageGenerator.Cli` |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `GeneratorRequest` | record | Input: types, laneKinds, lockUnlock, overloadType, relayOrder, scrambleSteps, difficulty, seed, reproduce, maxAttempts; **randomize mode**: lockCount, blindCount, randomizeGimmicks, randomOverload, randomRelay; **solve-only**: solveOnly, board |
| `StageGenerator.SolveExplicit` / `BuildExplicitBoard` | method | `solveOnly` mode: decode an explicit `board` string (+ gimmick cols) → optimal solveLength (no generation). Used by the par_moves migration |
| `GeneratorResult` | record | Output: lanes[], types, seed, attempts, solveLength, verifiedSolution, score; **resolved gimmick echo**: laneKinds, lockUnlock, overloadType, relayOrder |
| `Chip` / `SlotLane` / `Board` | class | Runtime-mirrored board model (Capacity 4, CanAccept, batch Move, relay/locked absorb) |
| `Board.CanonicalKey` | method | Lane-order-invariant state key (sorted per-lane signatures + relay progress) → collapses lane permutations for the solver's dedup |
| `BoardFactory.Generate` | method | Seeded random-fill, re-rolls only to dodge freebie lanes → `Board?` (null if config can't fit). No solvability check (the solver proves it) |
| `BatchSolver.Solve` | method | **Exact-shortest BFS** over canonical states → minimal clearing move sequence (true difficulty metric); null if no clear within node cap |
| `StageGenerator.Generate` | method | Scoring layer: sample `maxAttempts` seeds in parallel (capped `ProcessorCount-1`, 20s budget), score, return best; `reproduce` rebuilds one |
| `StageGenerator.BuildDef` / `BuildRandomizedDef` | method | Per-seed StageDef; randomize mode scatters lockCount/blindCount lanes with random colors + random overload/relay |
| `StageGenerator.ScoreCandidate` | method | Difficulty-fit score: target solve length + gimmick engagement bonuses (now fed the **optimal** solveLength) |

## Rules
- **Glyph order = SignalType**: `R B G Y P C O M L T` (0–9). `lockUnlock` uses `.` for non-locked lanes.
- The CLI returns the scored candidate as `lanes[]`. The editor **encodes those lanes into the
  `stage.csv` board column** (explicit layout) — persistence is the board, NOT the seed. `seed` in the
  response is provenance only; reproduce mode is no longer on the persistence path.
- Generation is **random-fill + exact-solve**, not a reverse-scramble (which deadlocks into freebies for normal
  lane counts). Because the board is stored explicitly, this algorithm does **not** need to stay byte-identical
  with the runtime `BoardFactory` — the runtime only generates as a fallback when `board` is empty.
- **Solver = exact-shortest BFS over `CanonicalKey`** (lane permutations collapsed) → `solveLength` is the true
  minimum player-move count, so difficulty scoring is valid. No heuristic move ordering; no false negatives.
- **Randomize mode** (`randomizeGimmicks`): only `laneKinds.Length` (total lane count) is read from the request;
  `lockCount`/`blindCount` lanes are scattered at random positions, lock unlock colors random, overload
  (`randomOverload`) and relay (`randomRelay`) are **always included with random color/order** when set. The
  result echoes the resolved `laneKinds`/`lockUnlock`/`overloadType`/`relayOrder` so the editor persists them.
- **Blind must land on a filled lane** — blind on an empty lane hides nothing (the player only stacks chips
  they placed), so it adds no difficulty yet still scores a blind bonus. `BuildRandom` biases the fill to
  place Blind lanes in the initially-filled set, and `blindCount` is capped at `Types` (= # filled lanes).
- **Safety budget**: `StageGenerator.TimeBudget` (20s wall-clock, `CancellationTokenSource`) + `MaxAttemptsCap`
  (500) bound the parallel search; on expiry the loop is cancelled cooperatively (token threaded through
  `BoardFactory.Generate`/`BatchSolver.Solve`) and **best-so-far is returned**. `Parallel.For` is capped at
  `ProcessorCount-1`. `route.ts` `execFile` adds a 30s hard `timeout`/`SIGKILL` (> CLI budget, so the CLI exits
  gracefully first).
- Needs ≥ `types + 1` non-locked lanes (one spare) or generation returns null.
- Build/publish handled by `tools/stage_editor.bat` (publishes to `bin/publish/` when source is newer).

## Cross-refs
- Consumed by: `tools/stage_editor` `src/app/api/generate-board/route.ts`
- Mirrors: `client/.../Scripts/InGame/{Chip,SlotLane,Board}.cs` (board model only; the generator's solver
  is an editor-only exact BFS, not the runtime solvability check)
