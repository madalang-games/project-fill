# shared/datas/ad

## Files
| file | class | role |
|------|-------|------|
| `ad_placement.csv` | `AdPlacement` | Rewarded-ad placement definitions |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AdPlacement.placement_key` | column | Stable ad placement key (UQ); e.g. `STUCK_ADD_LANE` |
| `AdPlacement.reward_group_id` | column | FK to `reward/reward_group`; reward granted on rewarded-ad completion |
| `AdPlacement.is_enabled` | column | Placement on/off toggle |
| `AdPlacement.ad_type` | column | `REWARDED` or `INTERSTITIAL` — CS scope |
| `AdPlacement.context_type` | column | Expected reward context |
| `AdPlacement.cooldown_seconds` | column | Server cooldown for INTERSTITIAL placements — S scope |
| `AdPlacement.min_stage` | column | Min stage to show INTERSTITIAL — S scope |

## Placements
| placement_key | ad_type | notes |
|---|---|---|
| `STUCK_ADD_LANE` | REWARDED | Stuck context, grants one Add Lane rescue |
| `DOUBLE_REWARD_STAGE_CLEAR` | REWARDED | Result context; doubles the stage-clear reward (re-grants `stage.reward_group_id`). reward_group_id=0 (per-stage dynamic, not placement-fixed). Server-verified + idempotent (`double_reward:{stage_id}` once) |
| `INTERSTITIAL_POST_STAGE` | INTERSTITIAL | 180s cooldown, stage 20+ |

## Rules
- Ad reward transaction storage is DB-owned; this table only describes placement policy.
- `cooldown_seconds` and `min_stage` are sparse (0 for REWARDED placements).

## Cross-refs
- Consumed by: `ProjectFill.Application.Stage.AdInterstitialService`
- Consumed by: `Game.Services.AdEligibilityCache`
