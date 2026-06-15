# ProjectFill.Application/Stage

## Files
| file | class | role |
|------|-------|------|
| `AdInterstitialService.cs` | `AdInterstitialService` | Interstitial eligibility (cooldown) and shown recording |
| `AdDoubleRewardService.cs` | `AdDoubleRewardService` | **STUB** — double reward removed (not in Signal Sort design) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AdInterstitialService.GetEligibilityAsync` | method | Returns cooldown state for INTERSTITIAL_POST_STAGE |
| `AdInterstitialService.RecordShownAsync` | method | Upserts `user_interstitial_state.last_shown_at` |
| `AdDoubleRewardService.ClaimAsync` | method | STUB — throws NotImplementedException |

## Rules
- Interstitial cooldown is controlled by `ad_placement.csv` `cooldown_seconds`.
- `DOUBLE_REWARD_STAGE_CLEAR` placement removed from `ad_placement.csv` — Signal Sort has no 2x stage-clear reward.

## Cross-refs
- Depends on: `shared/datas/ad/ad_placement.csv` (cooldown_seconds)
- Consumed by: `ProjectFill.API.Controllers.AdController`
