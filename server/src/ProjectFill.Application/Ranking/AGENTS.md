# ProjectFill.Application/Ranking

## Files
| file | class | role |
|------|-------|------|
| `RankingService.cs` | `RankingService` | Global/weekly/stage ranking Redis reads + writes, leaderboard pages, and Redis rebuild |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RankingService.GetGlobalPageAsync` | method | Paged global `stages` (cleared count) or `max-stage` ranking |
| `RankingService.GetMyGlobalRankAsync` | method | Current user's global ranking card |
| `RankingService.GetWeeklyPageAsync` | method | Paged current-week cleared-stage ranking (`ranking:weekly:{weekStart}:stages`) |
| `RankingService.GetMyWeeklyRankAsync` | method | Current user's weekly ranking card |
| `RankingService.GetStageRankAsync` | method | My rank for a stage by best moves used (`ranking:stage:{id}:moves`) |
| `RankingService.RecordClearAsync` | method | Post-commit Redis sync of global/weekly/stage indexes from committed DB rows |
| `RankingService.RebuildAllAsync` | method | Rebuilds Redis global + current-week ranking keys from DB |

## Rules
- DB is source of truth; Redis is rebuildable cache/index. Ranking Redis writes happen post-commit.
- Global `stages`/`max-stage` and `weekly` rankings: higher value better, deterministic tie-break by earlier achieved timestamp.
- Stage ranking: fewer best moves better (ascending); missing key triggers lazy rebuild from `user_stage_progress`.
- Global type strings: `stages` (cleared count), `max-stage`. Weekly key is the current Monday `yyyy-MM-dd`.

## Cross-refs
- Depends on: `ProjectFill.Infrastructure.Generated.AppDbContext`
- Depends on: `StackExchange.Redis.IDatabase`
- Consumed by: `ProjectFill.API.Controllers.RankingController`
- Consumed by: `ProjectFill.API.RankingRebuildHostedService`
- Consumed by: `ProjectFill.Application.Stage.StageService` (RecordClearAsync, GetStageRankAsync)
