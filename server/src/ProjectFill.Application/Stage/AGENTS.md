# ProjectFill.Application/Stage

## Files
| file | class | role |
|------|-------|------|
| `StageService.cs` | `StageService` | Signal Sort campaign stage-clear: validate, progress upsert, first-clear reward, chapter milestone, ranking totals/weekly, achievement reports |
| `AdInterstitialService.cs` | `AdInterstitialService` | Interstitial eligibility (cooldown) and shown recording |
| `AdDoubleRewardService.cs` | `AdDoubleRewardService` | Result-screen 2x reward: re-grants `stage.reward_group_id` once after a verified rewarded ad |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `StageService.StartStageAsync` | method | Stage-entry gate: validates stage exists + reachable (`ValidateUnlockedAsync`); issues a single-use attempt token to Redis (`SessionKey`, `StageSessionTtl`); returns `StageStartResponse` (server-authoritative `max_cleared_stage_id` + ruleset + `SessionId`) |
| `StageService.SessionKey` | method | `stage_session:{userId}:{stageId}` Redis key for the per-start attempt token (single-use, `StageSessionTtl`=1h) |
| `StageService.ValidateUnlockedAsync` | method | Campaign unlock check: stage 1 always open, else requires `max_cleared_stage_id >= stageId-1`, else `StageLocked`. Shared by Start + Clear. Returns the player's `max_cleared_stage_id` |
| `StageService.ClearStageAsync` | method | Records a stage clear; rejects locked stages via `ValidateUnlockedAsync`; validates+consumes the start-issued `SessionId` (missing/mismatch/expired → `InvalidStageAttempt`); grants first-clear reward + chapter chest milestone; updates `user_ranking_totals`/`user_weekly_ranking`; syncs Redis via `RankingService.RecordClearAsync`; returns `StageClearResponse` |
| `AdInterstitialService.GetEligibilityAsync` | method | Returns cooldown state for INTERSTITIAL_POST_STAGE |
| `AdInterstitialService.RecordShownAsync` | method | Upserts `user_interstitial_state.last_shown_at` |
| `AdDoubleRewardService.ClaimAsync` | method | Verifies rewarded ad (`IAdRewardVerifier`) → idempotent per stage (`double_reward:{stageId}` once) → re-grants `stage.RewardGroupId` via `RewardService.GrantRewardGroupAsync`. Gated on stage first-clear (`DoubleRewardNotEligible`); unverified → stashes pending + throws `AdSsvPending`; provider-tx replay → `Duplicate` |

## Rules
- Stage entry is server-gated: `StartStageAsync` validates the stage is reachable before the client builds the board and issues a single-use attempt token (Redis `stage_session:{userId}:{stageId}`, 1h TTL) returned as `SessionId`. `ClearStageAsync` re-runs the same `ValidateUnlockedAsync` AND validates+consumes the token (missing/mismatch/expired → `InvalidStageAttempt`), so a clear cannot be posted without a fresh matching start (closes the result-screen "Next" start-skip bypass). Unlock rule: stage 1 open; stage N requires `max_cleared_stage_id >= N-1` (`StageLocked` otherwise).
- Server trusts submitted moves (MVP); validates only `ruleset_version` (== `CurrentRulesetVersion`) and that `completed_signal_types` equals the stage's `{0..types-1}` set.
- First clear (was not previously cleared) grants `stage.reward_group_id` once and increments `total_cleared_stages`; re-clears grant nothing (per content design §4).
- `best_moves_used` is the minimum across clears; `is_new_best` true when beaten or on first clear.
- Chapter milestone: when all stages of a chapter are first-cleared, grants `chapter.chest_reward_group_id` once (idempotent via `chapter_chest:{id}` claim-state row).
- `weekly_cleared_count` counts every clear submission in the current week (Monday 00:00 UTC); rolls over on a new week.
- Perfect clear: when `best_moves_used <= stage.par_moves` (par_moves>0) for the first time, sets `is_perfect` and increments `user_ranking_totals.perfect_clears` (once per stage). Feeds the `perfect` global ranking.
- Achievement seams: `StageClearCount` (+1 first clear), `BestMovesRenewCount` (+1 on beaten prior best), `ChapterComplete` (+1 on milestone), `BoosterlessClearCount` (+1 when `!BoostersUsed`).
- Weekly Mission Event seam (`WeeklyMissionService.ReportProgressAsync`, same point as the achievement reports): `StageClearCount` (every clear), `ChapterProgress` (first clear), `BestMovesRenew` (beat prior best), `PerfectClearCount` (newly perfect), `BoosterlessClear` (`!BoostersUsed`).
- `StageClearRequest.BoostersUsed` (client-reported, cheat-trust like moves) gates the boosterless seams.
- Redis ranking writes happen post-commit (`RankingService.RecordClearAsync`), per social-ranking design authority rule.
- Interstitial cooldown is controlled by `ad_placement.csv` `cooldown_seconds`.
- `DOUBLE_REWARD_STAGE_CLEAR` (REWARDED) doubles the stage-clear reward by re-granting `stage.reward_group_id`. First-clear only, once per stage (idempotent via `double_reward:{stageId}` claim-state), server-verified via the shared SSV pipeline. Endpoint: `POST /api/ad/double-reward`; SSV-pending claims re-drive through `AdRewardsStatusController`.

## Cross-refs
- Depends on: `RewardService`, `AchievementService`, `WeeklyMissionService`, `RankingService`, `CurrencyService`, `IStaticDataService`, `StackExchange.Redis.IConnectionMultiplexer` (attempt-token store)
- Depends on: generated rows `UserStageProgressRow`, `UserRankingTotalsRow`, `UserWeeklyRankingRow`
- Depends on: `shared/datas/stage/{stage,chapter}.csv`, `shared/datas/reward/reward_group.csv`
- Depends on: `shared/datas/ad/ad_placement.csv` (cooldown_seconds)
- Consumed by: `ProjectFill.API.Controllers.StageController`, `ProjectFill.API.Controllers.AdController`
