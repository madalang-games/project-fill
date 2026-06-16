# Scripts/InGame — Signal Sort Gameplay Domain

Playable prototype of the core loop: chip selection/move, Complete Set absorption into the Signal
Panel, 3 boosters (Undo/Shuffle/AddLane), Soft/Hard Stuck handling, and all four chapter gimmicks
(Locked / Blind / Relay / Overload). Chrome is authored on the InGame scene canvas via
`UIEditorSetup.SetupInGame`; lanes/chips/panel nodes spawn at runtime from `Resources/Prefabs/Game/`
prefabs; stuck/clear popups go through `UIManager`. Procedural visuals (`TextureFactory`) with art
slot-in via `BoardSkin`.

## Nav
| path | role |
|------|------|
| `Controller/` | Scene entry + loop orchestrator | → `Controller/AGENTS.md` |
| `View/` | Runtime-built board UI, procedural sprites, animations | → `View/AGENTS.md` |

## Files
| file | class | role |
|------|-------|------|
| `SignalType.cs` | `SignalType`, `BoosterType`, `SignalTypeExtensions` | 10 signal colors + glyphs; 3 boosters (Undo/Shuffle/AddLane) |
| `InGameEnums.cs` | `LaneKind` | Normal / Locked / Blind lane kind |
| `Chip.cs` | `Chip` | Readonly struct: SignalType + Overload flag |
| `SlotLane.cs` | `SlotLane` | Lane model: chip stack, capacity, Locked/Pending/Blind state, CanAccept rules |
| `Board.cs` | `Board`, `BoardSnapshot` | Board model: move rules, relay-order cascade absorb, locked unlock, undo, solver hooks (Clone/EnumerateMoves/ApplyMoveRaw/StateKey) |
| `BoardSolver.cs` | `BoardSolver` | Capped DFS solvability check for Soft Stuck + generation insurance |
| `BoardFactory.cs` | `BoardFactory` | Seeded random-fill + solvability verify; **fallback** generator when a stage row has no `board`; in-place Reshuffle for Shuffle booster |
| `BoardCodec.cs` | `BoardCodec` | Encode/decode the `board` column layout (4 chars/lane; lower=overload; `-`=empty); mirror of editor `board-codec.ts` |
| `StageDefinition.cs` | `StageDefinition`, `StageLibrary` | Declarative stage layout; one sample stage per chapter (gimmick verification) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `SignalType` | enum | 0=Red…9=Teal (10 colors) |
| `BoosterType` | enum | Undo / Shuffle / AddLane (no Hint — spec defines 3) |
| `LaneKind` | enum | Normal / Locked / Blind |
| `Board.Move(from,to)` | method | Pours the contiguous same-type top run onto dest (A-R08 stack pour, top peels first, counts as 1 move); returns absorbed `(lane,type)` list; null if illegal |
| `Board.MovableCount(from,to)` | method | Chips a `Move` would relocate (same-type run capped by dest capacity); 0 if illegal — drives batch flight FX |
| `Board.IsHardStuck()` | method | No legal move from any lane |
| `Board.Clone()` / `StateKey()` | method | Solver state copy / dedup hash |
| `BoardFactory.Generate(def)` | method | Seeded random-fill board; null if config can't fit / unsolvable in 60 tries; fallback only (empty `board`) |
| `BoardCodec.Decode(board,def)` | method | `board` string → `Board` (authoritative render path for saved stages) |
| `StageLibrary.Get(index)` | method | Wraps index into per-chapter Samples |

## Rules
- Model classes (`Board`/`SlotLane`/`Chip`/solver/factory) are pure C# — NO UnityEngine dependency.
- Namespace: `Game.InGame` (model), `Game.InGame.[SubDir]` (controller/view).
- Saved stages render from the explicit `board` column via `BoardCodec.Decode` (no regeneration). `BoardFactory` (seeded random-fill + DFS verify) is the **fallback** when `board` is empty, and backs the Shuffle booster + Soft Stuck solver.
- Sample stages deviate slightly from the content-design difficulty table (fewer types / extra empties)
  to keep the runtime solver light and relay/overload demos reliable — they are a dev verification harness.
- `BoardFactory.Generate` rejects boards that start with any Complete Set (A-R09 no-freebie); solver path stays single-chip (batch is a player convenience, not a new reachable state).
