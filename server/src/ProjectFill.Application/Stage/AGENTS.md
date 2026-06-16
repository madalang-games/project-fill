# ProjectFill.Application/Stage

## Files
| file | class | role |
|------|-------|------|
| `StageService.cs` | `StageService` | Signal Sort campaign stage-clear: validate, progress upsert, first-clear reward, chapter milestone, ranking totals/weekly, achievement reports |
| `AdInterstitialService.cs` | `AdInterstitialService` | Interstitial eligibility (cooldown) and shown recording |
| `AdDoubleRewardService.cs` | `AdDoubleRewardService` | **STUB** — double reward removed (not in Signal Sort design) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageService.ClearStageAsync` | method | Records a stage clear; grants first-clear reward + chapter chest milestone; updates `user_ranking_totals`/`user_weekly_ranking`; syncs Redis via `RankingService.RecordClearAsync`; returns `StageClearResponse` |
| `AdInterstitialService.GetEligibilityAsync` | method | Returns cooldown state for INTERSTITIAL_POST_STAGE |
| `AdInterstitialService.RecordShownAsync` | method | Upserts `user_interstitial_state.last_shown_at` |
| `AdDoubleRewardService.ClaimAsync` | method | STUB — throws NotImplementedException |

## Rules
- Server trusts submitted moves (MVP); validates only `ruleset_version` (== `CurrentRulesetVersion`) and that `completed_signal_types` equals the stage's `{0..types-1}` set.
- First clear (was not previously cleared) grants `stage.reward_group_id` once and increments `total_cleared_stages`; re-clears grant nothing (per content design §4).
- `best_moves_used` is the minimum across clears; `is_new_best` true when beaten or on first clear.
- Chapter milestone: when all stages of a chapter are first-cleared, grants `chapter.chest_reward_group_id` once (idempotent via `chapter_chest:{id}` claim-state row).
- `weekly_cleared_count` counts every clear submission in the current week (Monday 00:00 UTC); rolls over on a new week.
- Achievement seams: `StageClearCount` (+1 first clear), `BestMovesRenewCount` (+1 on beaten prior best), `ChapterComplete` (+1 on milestone).
- Redis ranking writes happen post-commit (`RankingService.RecordClearAsync`), per social-ranking design authority rule.
- Interstitial cooldown is controlled by `ad_placement.csv` `cooldown_seconds`.
- `DOUBLE_REWARD_STAGE_CLEAR` placement removed from `ad_placement.csv` — Signal Sort has no 2x stage-clear reward.

## Cross-refs
- Depends on: `RewardService`, `AchievementService`, `RankingService`, `CurrencyService`, `IStaticDataService`
- Depends on: generated rows `UserStageProgressRow`, `UserRankingTotalsRow`, `UserWeeklyRankingRow`
- Depends on: `shared/datas/stage/{stage,chapter}.csv`, `shared/datas/reward/reward_group.csv`
- Depends on: `shared/datas/ad/ad_placement.csv` (cooldown_seconds)
- Consumed by: `ProjectFill.API.Controllers.StageController`, `ProjectFill.API.Controllers.AdController`
