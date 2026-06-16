# Scripts/InGame/Controller

MonoBehaviour orchestrators for the Signal Sort loop and scene entry.

## Files
| file | class | role |
|------|-------|------|
| `InGameSceneEntry.cs` | `InGameSceneEntry` | Sets portrait/fps; starts controller at `_startStageIndex` |
| `InGameController.cs` | `InGameController` | Loop: select/move, boosters, Soft/Hard Stuck, clear; cycles per-chapter sample stages |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `InGameController.Begin(startIndex)` | method | Subscribes to BoardView events (once) + loads first stage |
| `InGameController._boardView` | SerializeField | Bound in InGame.unity |
| `InGameSceneEntry._controller` / `_sceneBg` | SerializeField | Bound in InGame.unity (`_sceneBg` kept for compat) |
| `InGameSceneEntry._startStageIndex` | SerializeField | Index into `StageLibrary.Samples` (0 = Ch1) |

## Rules
- Controller owns game logic; BoardView owns rendering/animation. Controller never touches GameObjects.
- BoardView events are subscribed once in `Begin` (BoardView persists across stage reloads).
- Move flow: capture top chip + slot → `Board.Move` → `BoardView.AnimateMove(…, onComplete: PostMoveCheck)`.
