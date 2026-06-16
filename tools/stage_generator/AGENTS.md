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
| `GeneratorRequest` | record | Input: types, laneKinds, lockUnlock, overloadType, relayOrder, scrambleSteps, difficulty, seed, reproduce, maxAttempts |
| `GeneratorResult` | record | Output: lanes[], types, seed, attempts, solveLength, verifiedSolution, score |
| `Chip` / `SlotLane` / `Board` | class | Runtime-mirrored board model (Capacity 4, CanAccept, batch Move, relay/locked absorb) |
| `BoardFactory.Generate` | method | Seeded random-fill + solvability verification → `Board?` (null if config can't fit) |
| `BoardSolver.IsSolvable` | method | Single-chip DFS (runtime-mirrored) solvability insurance |
| `BatchSolver.Solve` | method | Node-capped DFS → first clearing move sequence (player-facing batch moves) |
| `StageGenerator.Generate` | method | Scoring layer: sample `maxAttempts` seeds in parallel, score, return best; `reproduce` rebuilds one |
| `StageGenerator.ScoreCandidate` | method | Difficulty-fit score: target solve length + gimmick engagement bonuses |

## Rules
- **Glyph order = SignalType**: `R B G Y P C O M L T` (0–9). `lockUnlock` uses `.` for non-locked lanes.
- The CLI returns the scored candidate as `lanes[]`. The editor **encodes those lanes into the
  `stage.csv` board column** (explicit layout) — persistence is the board, NOT the seed. `seed` in the
  response is provenance only; reproduce mode is no longer on the persistence path.
- Generation is **random-fill + verify**, not a reverse-scramble (which deadlocks into freebies for normal
  lane counts). Because the board is stored explicitly, this algorithm does **not** need to stay byte-identical
  with the runtime `BoardFactory` — the runtime only generates as a fallback when `board` is empty.
- Needs ≥ `types + 1` non-locked lanes (one spare) or generation returns null.
- Build/publish handled by `tools/stage_editor.bat` (publishes to `bin/publish/` when source is newer).

## Cross-refs
- Consumed by: `tools/stage_editor` `src/app/api/generate-board/route.ts`
- Mirrors: `client/.../Scripts/InGame/{Chip,SlotLane,Board,BoardSolver}.cs`
