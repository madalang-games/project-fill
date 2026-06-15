# ProjectFill.Application/Player

## Files
| file | class | role |
|------|-------|------|
| `PlayerService.cs` | `PlayerService` | Player progress summary: max cleared stage and per-stage best stars |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `PlayerService.GetProgressAsync` | method | Queries `user_ranking_totals` (max cleared) + `user_stage_progress` (best stars); returns `PlayerProgressResponse` |

## Rules
- Returns only stages with `BestStar > 0`; client derives unlock from `MaxClearedStageId` using rule: stage N unlocked when `MaxClearedStageId >= N-1`.
- No writes; read-only query.

## Cross-refs
- Depends on: `ProjectFill.Infrastructure.Generated.AppDbContext` (`UserRankingTotals`, `UserStageProgress`)
- Consumed by: `ProjectFill.API.Controllers.PlayerController`
