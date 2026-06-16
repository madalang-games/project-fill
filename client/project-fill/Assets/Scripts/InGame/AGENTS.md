# Scripts/InGame — Signal Sort Gameplay Domain

Playable prototype of the core loop: chip selection/move, Complete Set absorption into the Signal
Panel, 3 boosters (Undo/Shuffle/AddLane), Soft/Hard Stuck handling, and all four chapter gimmicks
(Locked / Blind / Relay / Overload). UI is built procedurally at runtime (no prefab dependency).

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
| `BoardFactory.cs` | `BoardFactory` | Reverse-path (reversible-scramble) solvable generation; in-place Reshuffle for Shuffle booster |
| `StageDefinition.cs` | `StageDefinition`, `StageLibrary` | Declarative stage layout; one sample stage per chapter (gimmick verification) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `SignalType` | enum | 0=Red…9=Teal (10 colors) |
| `BoosterType` | enum | Undo / Shuffle / AddLane (no Hint — spec defines 3) |
| `LaneKind` | enum | Normal / Locked / Blind |
| `Board.Move(from,to)` | method | Returns absorbed `(lane,type)` list; null if illegal |
| `Board.IsHardStuck()` | method | No legal move from any lane |
| `Board.Clone()` / `StateKey()` | method | Solver state copy / dedup hash |
| `BoardFactory.Generate(def)` | method | Solvable board from StageDefinition |
| `StageLibrary.Get(index)` | method | Wraps index into per-chapter Samples |

## Rules
- Model classes (`Board`/`SlotLane`/`Chip`/solver/factory) are pure C# — NO UnityEngine dependency.
- Namespace: `Game.InGame` (model), `Game.InGame.[SubDir]` (controller/view).
- Generation is solvable-by-construction (reversible scramble); solver is verification + Soft Stuck only.
- Sample stages deviate slightly from the content-design difficulty table (fewer types / extra empties)
  to keep the runtime solver light and relay/overload demos reliable — they are a dev verification harness.
