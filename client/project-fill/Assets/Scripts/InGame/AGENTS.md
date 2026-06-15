# Scripts/InGame — Signal Sort Gameplay Domain

Core game loop domain: Board (SlotLanes + SignalChips), Complete Set detection, Stuck detection, Booster system (Add Lane / Shuffle / Hint / Undo).
Controller and View stubs in place; core gameplay implementation pending.

## Nav
| path | role |
|------|------|
| `Controller/` | MonoBehaviour orchestrator stubs to preserve Unity serialization | → `Controller/AGENTS.md` |
| `View/` | MonoBehaviour view component stubs for UI editor mapping | → `View/AGENTS.md` |

## Rules
- Stub views and controllers must preserve public serialized fields to avoid breaking prefab links or editor mapping.
- Namespace: `Game.InGame.[SubDir]`
