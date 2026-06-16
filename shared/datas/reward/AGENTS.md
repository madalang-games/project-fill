# shared/datas/reward

## Files
| file | class | role |
|------|-------|------|
| `reward_group.csv` | `RewardGroup` | Reward bundle definitions |
| `reward_item.csv` | `RewardItem` | Items granted by a reward group |
| `reward_source.csv` | `RewardSource` | Claimable reward sources and claim policies |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RewardGroup.reward_group_id` | column | PK; range-based info_id (2xxx stage-clear, 3xxx chapter-chest, 5xxx IAP) |
| `RewardItem.reward_type` | column | Signal Sort reward type such as `SOFT_CURRENCY`, `ITEM`, or `NO_ADS` |
| `RewardItem.stack_policy` | column | Reward stack policy, usually `NONE` for currency/items |
| `RewardSource.source_id` | column | Client/server stable claim identifier |
| `RewardSource.claim_policy` | column | Source claim limit policy |

## Rules
- Rewards by reference only: features point to `reward_group_id`; do not inline reward columns into feature tables.

## Cross-refs
- Consumed by: `ProjectFill.Application.Rewards.RewardService`
- Consumed by: `Game.OutGame.Lobby.HomeTabView`
