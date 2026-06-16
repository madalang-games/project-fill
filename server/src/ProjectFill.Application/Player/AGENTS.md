# ProjectFill.Application/Player

## Files
| file | class | role |
|------|-------|------|
| `PlayerService.cs` | `PlayerService` | Player profile + progress: unlocked avatars, no-ads state; profile (display name / avatar) update |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `PlayerService.GetProgressAsync` | method | Returns `PlayerProgressResponse` (unlocked avatar ids from `user_reward_claim_state` + `IsNoAds`); no star/stage data |
| `PlayerService.UpdateProfileAsync` | method | Validates + updates display name / avatar; spends gold on avatar unlock when required |

## Rules
- No star system: Signal Sort has no per-stage stars (removed per spec). Stage clear/best-moves progress lives in `user_stage_progress` and is served by `StageService`/`RankingService`, not here.
- Profile update writes; progress read is read-only.

## Cross-refs
- Depends on: `ProjectFill.Infrastructure.Generated.AppDbContext` (`Players`, `UserRewardClaimState`), `CurrencyService`, `IStaticDataService`
- Consumed by: `ProjectFill.API.Controllers.PlayerController`
