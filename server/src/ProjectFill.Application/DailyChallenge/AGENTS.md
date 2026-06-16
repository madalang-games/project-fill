# ProjectFill.Application/DailyChallenge

## Files
| file | class | role |
|------|-------|------|
| `DailyChallengeService.cs` | `DailyChallengeService` | Daily challenge today/clear/ranking/streak; deterministic per-date seed |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `DailyChallengeService.GetTodayAsync` | method | Ensures today's challenge row; returns seed/params + my participation + rank |
| `DailyChallengeService.SubmitClearAsync` | method | Records clear, advances streak, grants base + streak rewards + cosmetic, reports to achievements |
| `DailyChallengeService.GetRankingAsync` | method | Paged global ranking by moves asc, clear_time asc |
| `DailyChallengeService.GetStreakAsync` | method | Current/best challenge streak |

## Rules
- One shared puzzle per UTC date; `EnsureChallengeAsync` creates it deterministically (`StableHash(date)` → signal/lane counts). Board generation + play is **client/InGame** (out of scope here); only seed/params are stored.
- Streak resets when the previous clear was not yesterday; bonus at 3/7/30/100; cosmetic via `streak_30`/`streak_100` conditions.
- Cross-system seam: calls `AchievementService.ReportValueAsync` for `ChallengeClearStreak` and `ChallengeRankFirst`.
- Procedural Reverse-Path generation + Solver (design §6) and the `/clear` move-validation are deferred (InGame); server trusts submitted moves for MVP.

## Cross-refs
- Depends on: `RewardService`, `CosmeticService`, `AchievementService`
- Depends on: generated rows `DailyChallengesRow`, `UserDailyChallengeRecordsRow`, `UserChallengeStreaksRow`
- Consumed by: `ProjectFill.API.Controllers.DailyChallengeController`
