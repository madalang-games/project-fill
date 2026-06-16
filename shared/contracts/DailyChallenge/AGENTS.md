# shared/contracts/DailyChallenge

## Files
| file | class | role |
|------|-------|------|
| `DailyChallengeContracts.cs` | `DailyChallengeTodayResponse`, `SubmitChallengeClearRequest`, `SubmitChallengeClearResponse`, `ChallengeRankingEntryDto`, `ChallengeRankingResponse`, `ChallengeStreakResponse` | Daily challenge today/clear/ranking/streak DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `DailyChallengeTodayResponse.StageSeed` | property | Deterministic seed; client/InGame builds the board from it |
| `DailyChallengeTodayResponse.GimmickId` | property | -1 = no gimmick |
| `SubmitChallengeClearResponse.Rank` | property | Global rank after submission (moves asc, time asc) |
| `ChallengeRankingEntryDto.IsMe` | property | Marks the requesting player's row |

## Rules
- Challenge date is a `yyyy-MM-dd` string (UTC), not a datetime.
- Reuses `Rewards.GrantedRewardDto` and `Currency.CurrencySnapshot`.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.DailyChallengeController`
- Consumed by: `Game.Services.DailyChallengeApiService`
