# shared/contracts/Achievement

## Files
| file | class | role |
|------|-------|------|
| `AchievementContracts.cs` | `AchievementDto`, `AchievementListResponse`, `ClaimAchievementResponse` | Achievement list + claim DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AchievementDto.Progress` | property | Effective progress (derived or stored) |
| `AchievementDto.IsCompleted` | property | progress ≥ condition_value |
| `AchievementDto.RewardClaimed` | property | Reward already collected |
| `ClaimAchievementResponse.UnlockedCosmetics` | property | Cosmetics unlocked via achievement condition |

## Rules
- Reuses `Rewards.GrantedRewardDto` and `Currency.CurrencySnapshot`.
- Enum fields serialized as int (`AchievementCategory`, `AchievementTier`, `AchievementConditionType` in `GameTypes`).

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.AchievementController`
- Consumed by: `Game.Services.AchievementApiService`
