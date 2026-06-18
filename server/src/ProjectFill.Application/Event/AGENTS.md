# ProjectFill.Application/Event

## Files
| file | class | role |
|------|-------|------|
| `WeeklyMissionService.cs` | `WeeklyMissionService` | Weekly Mission Event: per-week mission set assignment, status, progress aggregation seam, milestone claim |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `WeeklyMissionService.CurrentWeekStart` | static method | Monday 00:00 UTC `yyyy-MM-dd`; same boundary as `StageService.CurrentWeekStart` / `user_weekly_ranking` |
| `WeeklyMissionService.GetStatusAsync` | method | Ensures week's set; returns 5 missions + my progress + cumulative EP + track claim state + days remaining |
| `WeeklyMissionService.ClaimAsync` | method | Claims a milestone reward group; throws `WeeklyMissionInvalidThreshold`/`WeeklyMissionThresholdNotReached`/`WeeklyMissionAlreadyClaimed` |
| `WeeklyMissionService.ReportProgressAsync` | method | Stage-clear-flow seam: increments matching mission progress + accrues EP on completion; reports `WeeklyMissionComplete` to achievements on full track |
| `WeeklyMissionService.EnsureSetAsync` | method | Deterministic global per-week selection of 5 mission ids from the pool (`StableHash(week:id)`) |

## Rules
- One global mission set per week (`weekly_mission_sets`, 1 row/week), assigned lazily on first access (mirrors the old `EnsureChallengeAsync` pattern); 5 missions seeded deterministically from `week_start_date` so the set is identical worldwide.
- Progress has **no submit endpoint** — `ReportProgressAsync` is called from `StageService.ClearStageAsync` (same seam as the achievement reports) for condition types `StageClearCount` (every clear), `ChapterProgress` (first clear), `BestMovesRenew` (beat prior best), `PerfectClearCount` (newly perfect), `BoosterlessClear` (no boosters used).
- EP accrues only on mission completion (sum of `ep_reward`); track full completion (max threshold) reports `AchievementConditionType.WeeklyMissionComplete` once per week.
- Milestone claim is pull-based and idempotent via `claimed_thresholds` CSV; non-punitive — unclaimed milestones simply expire at week rollover.
- Rewards are gold/consumable only (`reward_group` 7001–7099); cosmetics are owned by attendance/achievement systems.

## Cross-refs
- Depends on: `RewardService`, `AchievementService`, `IStaticDataService` (`GetAllWeeklyMissionPools`/`GetWeeklyMissionPool`/`GetAllWeeklyMissionTracks`/`GetWeeklyMissionTrack`)
- Depends on: generated rows `WeeklyMissionSetsRow`, `UserWeeklyMissionsRow`, `UserWeeklyMissionStateRow`
- Depends on: `shared/datas/event/{weekly_mission_pool,weekly_mission_track}.csv`
- Consumed by: `ProjectFill.API.Controllers.WeeklyMissionController`, `ProjectFill.Application.Stage.StageService`
