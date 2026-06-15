# shared/contracts/Rewards

## Files
| file | class | role |
|------|-------|------|
| `RewardRequests.cs` | `RewardClaimRequest`, `AdRewardClaimRequest` | Reward claim request DTOs |
| `RewardResponses.cs` | `RewardSourceDto`, `GrantedRewardDto`, `RewardClaimResponse`, `AdRewardClaimResponse` | Reward source and claim response DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RewardClaimRequest.SourceId` | property | Generic claim source, e.g. `chapter1_chest` |
| `GrantedRewardDto.RewardType` | property | Generic reward type, e.g. `SOFT_CURRENCY`, `ITEM`, `NO_ADS` |

## Rules
- Keep claim source generic; do not add life-specific request DTOs.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.RewardsController`
- Consumed by: `ProjectFill.Application.Rewards.RewardService`
