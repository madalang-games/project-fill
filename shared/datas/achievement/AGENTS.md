# shared/datas/achievement

## Files
| file | class | role |
|------|-------|------|
| `achievement.csv` | `Achievement` | Achievement catalog: tier, category, condition, reward group |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `Achievement.achievement_id` | column | PK; string id referenced by cosmetic `unlock_condition_id` |
| `Achievement.category` | column | `AchievementCategory`: Progression / Skill / Dedication / Collection |
| `Achievement.tier` | column | `AchievementTier`: Bronze / Silver / Gold / Platinum |
| `Achievement.reward_group_id` | column | FK reward group granted on claim (gold + avatar/booster) |
| `Achievement.condition_type` | column | `AchievementConditionType`; drives progress evaluation |
| `Achievement.condition_value` | column | Target count/threshold for completion |

## Rules
- Rewards by reference (`reward_group_id`); cosmetic rewards are pull-based via cosmetic catalog `unlock_condition_id == achievement_id` (e.g. `prg_04`→chip_platinum).
- `condition_type` derivable from existing server state (TotalLoginDays, LoginStreak, ChallengeClearStreak, AvatarUnlockCount, CosmeticUnlockCount) is auto-evaluated on read; gameplay-sourced types (StageClearCount, BoosterlessClearCount, BestMovesRenewCount, MoveTopPercentileCount, ChallengeRankFirst, ChallengeBreakClearCount, ShufflelessWeek) are updated via `AchievementService.ReportProgressAsync` from gameplay events (InGame — not wired here).
- `col_05` condition_value (19) = count of non-default cosmetics; keep in sync if the catalog grows.

## Cross-refs
- Depends on: `shared/datas/reward/reward_group.csv`
- Consumed by: `ProjectFill.Application.Achievement.AchievementService`
- Consumed by: `Game.Services.AchievementApiService`
