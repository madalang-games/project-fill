# shared/datas/ad

## Files
| file | class | role |
|------|-------|------|
| `ad_placement.csv` | `AdPlacement` | Rewarded-ad placement definitions |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AdPlacement.placement_id` | column | Stable ad placement id |
| `AdPlacement.ad_type` | column | `REWARDED` or `INTERSTITIAL` — CS scope |
| `AdPlacement.context_type` | column | Expected reward context |
| `AdPlacement.cooldown_seconds` | column | Server cooldown for INTERSTITIAL placements — S scope |
| `AdPlacement.min_stage` | column | Min stage to show INTERSTITIAL — S scope |

## Placements
| placement_id | ad_type | notes |
|---|---|---|
| `STUCK_ADD_LANE` | REWARDED | Stuck context, grants one Add Lane rescue |
| `INTERSTITIAL_POST_STAGE` | INTERSTITIAL | 180s cooldown, stage 20+ |

## Rules
- Ad reward transaction storage is DB-owned; this table only describes placement policy.
- `cooldown_seconds` and `min_stage` are sparse (0 for REWARDED placements).

## Cross-refs
- Consumed by: `Server.AdInterstitialService`
- Consumed by: `Client.AdService`
