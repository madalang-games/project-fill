# shared/contracts/Stage

## Files
| file | class | role |
|------|-------|------|
| `StageRequests.cs` | `StageClearRequest` | Signal Sort campaign stage-clear submission DTO (carries the start-issued `SessionId`) |
| `StageResponses.cs` | `StageStartResponse`, `StageClearResponse` | Stage-start gate result (max-cleared reach + ruleset + `SessionId`); stage-clear result: best moves, rank, first-clear/milestone, granted rewards |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageStartResponse.MaxClearedStageId` | property | Server-authoritative campaign reach; client calls `ApplyMaxClearedStage` to correct stale local unlock state |
| `StageStartResponse.RulesetVersion` | property | Current server ruleset version returned at stage entry |
| `StageStartResponse.SessionId` | property | Per-start attempt token (Redis, single-use). Client echoes it in the matching `StageClearRequest`; clear without a valid token → `INVALID_STAGE_ATTEMPT` |
| `StageClearRequest.RulesetVersion` | property | Static stage ruleset version; server rejects on mismatch |
| `StageClearRequest.SessionId` | property | Token issued by the matching `start`; server validates+consumes it; missing/mismatch/expired → `INVALID_STAGE_ATTEMPT` |
| `StageClearRequest.MovesUsed` | property | Moves used this clear; >=1; ranking-only, no reward effect |
| `StageClearRequest.CompletedSignalTypes` | property | Distinct completed signal-type indices; must equal stage's `types` set (cheat shape check) |
| `StageClearRequest.BoostersUsed` | property | True if any booster (Undo/Shuffle/AddLane) was used this clear; drives boosterless achievement + weekly-mission seams (cheat-trust) |
| `StageClearResponse.IsNewBest` | property | True when `MovesUsed` beat prior `best_moves_used` |
| `StageClearResponse.IsFirstClear` | property | True only on the first ever clear of this stage (grants stage reward group) |
| `StageClearResponse.ChapterCompleted` | property | True when this clear completed the chapter milestone (grants chest) |
| `StageClearResponse.StageRank` | property | Competition rank by best moves used (ascending) |
| `StageClearResponse.WeeklyClearedCount` | property | This week's cleared-stage count after this clear |

## Rules
- `netstandard2.1` only; DTOs only, no logic.
- Reuses `Rewards.GrantedRewardDto` and `Currency.CurrencySnapshot`.
- Stage entry (`StageStartResponse`) issues a single-use `SessionId` (Redis, TTL) per start and confirms the stage is unlocked (echoes `max_cleared_stage_id` + ruleset). `StageClearRequest.SessionId` must match the start-issued token; the server validates+consumes it, so a clear cannot be posted without a fresh start. Unlock is re-checked on clear too.
- Server trusts submitted moves (MVP, per social-ranking design); validates ruleset version + completed-type set shape only.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.StageController`
- Gen output: `client/project-fill/Assets/Scripts/Generated/Contracts/` (via `pkt_generator`)
