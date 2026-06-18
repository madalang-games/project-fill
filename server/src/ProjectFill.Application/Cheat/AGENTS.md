# ProjectFill.Application/Cheat

Dev-only state forcing. Reuses existing domain services/repositories so cheats share gameplay's
audit trail (`currency_logs` / `InventoryChanged`) plus a `Cheat*` `event_logs` row per command.

## Files
| file | class | role |
|------|-------|------|
| `CheatService.cs` | `CheatService` | Gold/item/stage/tutorial/ad/cosmetic/achievement/attendance state mutation for the caller's own account |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CheatService.GoldAsync` | method | Add/Reduce/Set soft gold via `CurrencyService.GrantSoftAsync`; clamps `0..999,999,999`; returns balanceAfter |
| `CheatService.ItemAsync` | method | Per-item or `all` (null id) Add/Reduce/Set via `InventoryService`; reduce clamps at 0; returns `{id:qty}` |
| `CheatService.StageAsync` | method | Marks static stages `1..stageId` cleared, deletes progress beyond, sets `max_cleared_stage_id`; returns highestStageAfter |
| `CheatService.TutorialAsync` | method | Single id set/clear; `all` supports only clear (no server-side tutorial id list) → `all true` throws `InvalidCommand` |
| `CheatService.AdAsync` | method | Toggles Redis `cheat:ad:{uid}` flag; returns nothing |
| `CheatService.AdBypassKey` | method | `cheat:ad:{userId}` Redis key |
| `CheatService.CosmeticAsync` | method | Per-id or `all` (null id) unlock/lock of `user_cosmetics` rows (free, no gold/condition gate); returns owned ids |
| `CheatService.AchievementAsync` | method | Per-id or `all` complete (progress→threshold + `IsCompleted`) / reset (delete row); reward claim stays separate; returns `{id:completed}` |
| `CheatService.AttendanceAsync` | method | Force `current_day` (clamp 1..7, clears claim flags → re-claimable) or null=reset (delete row); returns dayAfter (0 on reset) |

## Rules
- Caller's own `userId` only; never accepts a target uid (server auth convention).
- Every command writes one `EventLogFactory.Cheat*` row (audit) in addition to domain-level logs.

## Cross-refs
- Depends on: `CurrencyService`, `InventoryService`, `IStaticDataService`, `IConnectionMultiplexer`
- Depends on: `ProjectFill.Domain.Enums.CheatAction`
- Consumed by: `ProjectFill.API.Dev.CheatDispatcher`
