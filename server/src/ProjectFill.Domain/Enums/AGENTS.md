# ProjectFill.Domain/Enums

## Files
| file | class | role |
|------|-------|------|
| `RankingEnums.cs` | `GlobalRankingType` | Internal ranking type discriminator |
| `AdEnums.cs` | `AdPlacementKeys` | Named ad placement key constants |
| `PlayerEnums.cs` | `AvatarUnlockTypes`, `AvatarClaimKeys` | Avatar unlock type constants and claim key prefix |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `GlobalRankingType.ClearedStages` | enum value | Total cleared-stage count ranking; Redis key `ranking:global:stages` |
| `GlobalRankingType.MaxStage` | enum value | Highest cleared stage ranking; Redis key `ranking:global:max-stage` |
| `AdPlacementKeys.AddLane` | const string | `"STUCK_ADD_LANE"` — rewarded ad for Add Lane item grant; matches `ad_placement.csv` placement_key |
| `AdPlacementKeys.InterstitialPostStage` | const string | `"INTERSTITIAL_POST_STAGE"` — cooldown-gated interstitial |
| `AvatarUnlockTypes.Achievement` | const string | `"achievement"` — avatar unlock gated by reward claim |
| `AvatarClaimKeys.UnlockPrefix` | const string | `"avatar_unlock:"` — source_id prefix in `user_reward_claim_state` |

## Rules
- Server-internal enums and string-constant discriminators only.
- If a value must be agreed upon by both server AND client, put it in `shared/contracts/GameTypes/GameEnums.cs` instead.
- String-constant classes (not `enum`) are used when the discriminating value is a CSV-originated string key.
