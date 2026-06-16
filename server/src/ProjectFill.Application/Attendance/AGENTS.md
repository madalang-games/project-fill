# ProjectFill.Application/Attendance

## Files
| file | class | role |
|------|-------|------|
| `AttendanceService.cs` | `AttendanceService` | Daily attendance status + claim, 7-day non-punitive cycle, milestone rewards |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AttendanceService.GetStatusAsync` | method | Pending day/cycle, streak, 7 day cards with claimed/today flags |
| `AttendanceService.ClaimAsync` | method | Advances cycle, grants day reward group + cumulative milestone rewards + cosmetic unlocks; throws `AttendanceAlreadyClaimed` |

## Rules
- UTC calendar day boundary; duplicate claim guarded by `last_attended_date == today`.
- Non-punitive: cycle/day always advance; only `current_streak` resets when yesterday was missed.
- Day reward group resolved from `daily_login_reward.csv` by (cycle_type, day); cycle 1 = First, cycle 2+ = Repeat.
- Milestone (30/60/100 cumulative days) grants `daily_login_milestone.csv` reward group; cosmetic via `CosmeticService.UnlockByConditionAsync`.

## Cross-refs
- Depends on: `shared/datas/daily_login/*.csv`, `ProjectFill.Application.Rewards.RewardService`, `ProjectFill.Application.Cosmetic.CosmeticService`
- Depends on: `ProjectFill.Infrastructure.Generated.UserLoginAttendanceRow`
- Consumed by: `ProjectFill.API.Controllers.AttendanceController`
