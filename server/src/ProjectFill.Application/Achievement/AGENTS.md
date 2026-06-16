# ProjectFill.Application/Achievement

## Files
| file | class | role |
|------|-------|------|
| `AchievementService.cs` | `AchievementService` | Achievement listing, claim, derived-progress evaluation, progress-report seams |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AchievementService.GetListAsync` | method | All achievements with effective progress + completion + claim state |
| `AchievementService.ClaimAsync` | method | Grants reward group + cosmetic-by-condition for a completed achievement; throws `AchievementNotCompleted`/`AchievementAlreadyClaimed` |
| `AchievementService.ReportValueAsync` | method | Seam: set progress to max(value) for streak/threshold condition types |
| `AchievementService.ReportCountAsync` | method | Seam: increment progress for count condition types |

## Rules
- Derived (auto-evaluated on read) condition types: `TotalLoginDays`, `LoginStreak`, `AvatarUnlockCount`, `CosmeticUnlockCount` — computed from existing DB state.
- All other condition types are gameplay/challenge-sourced — updated only via `ReportValueAsync`/`ReportCountAsync` (called from InGame / DailyChallengeService; InGame wiring not included here).
- Cosmetic rewards are pull-based: `ClaimAsync` calls `CosmeticService.UnlockByConditionAsync(achievement_id)`.

## Cross-refs
- Depends on: `shared/datas/achievement/achievement.csv`, `RewardService`, `CosmeticService`
- Depends on: `ProjectFill.Infrastructure.Generated.UserAchievementsRow`
- Consumed by: `ProjectFill.API.Controllers.AchievementController`, `ProjectFill.Application.DailyChallenge.DailyChallengeService`
