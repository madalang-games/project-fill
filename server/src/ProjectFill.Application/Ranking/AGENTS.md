# ProjectFill.Application/Ranking

## Files
| file | class | role |
|------|-------|------|
| `RankingService.cs` | `RankingService` | Global ranking Redis reads, global leaderboard pages, and Redis rebuild |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RankingService.GetGlobalPageAsync` | method | Paged global `stars` or `max-stage` ranking |
| `RankingService.GetMyGlobalRankAsync` | method | Current user's global ranking card |
| `RankingService.RebuildAllAsync` | method | Rebuilds Redis global ranking keys from `user_ranking_totals` |

## Rules
- DB is source of truth; Redis is rebuildable cache/index.
- Global rankings use deterministic tie-break by earlier achieved timestamp.
- Stage-specific ranking methods (RecordClearAsync, GetStageRankAsync) are removed; implement per new game mechanics.

## Cross-refs
- Depends on: `ProjectFill.Infrastructure.Generated.AppDbContext`
- Depends on: `StackExchange.Redis.IDatabase`
- Consumed by: `ProjectFill.API.Controllers.RankingController`
- Consumed by: `ProjectFill.API.RankingRebuildHostedService`
