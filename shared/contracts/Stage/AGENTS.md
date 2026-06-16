# shared/contracts/Stage

## Files
| file | class | role |
|------|-------|------|
| `StageRequests.cs` | `StageClearRequest` | Signal Sort campaign stage-clear submission DTO |
| `StageResponses.cs` | `StageClearResponse` | Stage-clear result: best moves, rank, first-clear/milestone, granted rewards |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageClearRequest.RulesetVersion` | property | Static stage ruleset version; server rejects on mismatch |
| `StageClearRequest.MovesUsed` | property | Moves used this clear; >=1; ranking-only, no reward effect |
| `StageClearRequest.CompletedSignalTypes` | property | Distinct completed signal-type indices; must equal stage's `types` set (cheat shape check) |
| `StageClearResponse.IsNewBest` | property | True when `MovesUsed` beat prior `best_moves_used` |
| `StageClearResponse.IsFirstClear` | property | True only on the first ever clear of this stage (grants stage reward group) |
| `StageClearResponse.ChapterCompleted` | property | True when this clear completed the chapter milestone (grants chest) |
| `StageClearResponse.StageRank` | property | Competition rank by best moves used (ascending) |
| `StageClearResponse.WeeklyClearedCount` | property | This week's cleared-stage count after this clear |

## Rules
- `netstandard2.1` only; DTOs only, no logic.
- Reuses `Rewards.GrantedRewardDto` and `Currency.CurrencySnapshot`.
- Server trusts submitted moves (MVP, per social-ranking design); validates ruleset version + completed-type set shape only.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.StageController`
- Gen output: `client/project-fill/Assets/Scripts/Generated/Contracts/` (via `pkt_generator`)
