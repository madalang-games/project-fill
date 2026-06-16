# shared/datas/daily_login

## Files
| file | class | role |
|------|-------|------|
| `daily_login_reward.csv` | `DailyLoginReward` | Per-(cycle_type, day) attendance reward group mapping |
| `daily_login_milestone.csv` | `DailyLoginMilestone` | Cumulative-attendance milestone rewards (30/60/100 days) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `DailyLoginReward.cycle_type` | column | `AttendanceCycleType`: First (cycle 1) / Repeat (cycle 2+) |
| `DailyLoginReward.day` | column | Day within 7-day cycle (1–7) |
| `DailyLoginReward.reward_group_id` | column | FK reward group granted on claim |
| `DailyLoginMilestone.threshold_days` | column | Cumulative attended days needed (non-consecutive) |
| `DailyLoginMilestone.cosmetic_condition_id` | column | `day_{n}` condition passed to `CosmeticService.UnlockByConditionAsync`; blank if none |

## Rules
- Non-punitive: cycle never resets; only the consecutive streak resets when a day is missed.
- Cosmetic milestone rewards are pull-based via `cosmetic_condition_id` (`day_30`,`day_100`); never inline cosmetic into reward_item.
- Avatar rewards travel as `AVATAR` reward_item rows (e.g. Day7 first cycle → avatar 3).

## Cross-refs
- Depends on: `shared/datas/reward/reward_group.csv`
- Consumed by: `ProjectFill.Application.Attendance.AttendanceService`
- Consumed by: `Game.Services.AttendanceApiService`
