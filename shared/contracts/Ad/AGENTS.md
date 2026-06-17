# shared/contracts/Ad

## Files
| file | class | role |
|------|-------|------|
| `AdRequests.cs` | `AdDoubleRewardRequest`, `AdInterstitialShownRequest` | Ad request DTOs; `AdDoubleRewardRequest` = result-screen 2x reward claim (provider/adToken/stageId/attemptId) |
| `AdResponses.cs` | `AdPlacementStatus`, `AdEligibilityResponse`, `AdDoubleRewardGrantResponse`, `AdInterstitialShownResponse` | Ad response DTOs; `AdDoubleRewardGrantResponse` = 2x grant result (granted/duplicate/rewards/currency) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AdDoubleRewardRequest` | class | Result-screen 2x reward claim: `Provider`, `AdToken` (SSV nonce), `StageId`, `AttemptId` |
| `AdDoubleRewardGrantResponse` | class | 2x grant result: `Granted`, `Duplicate`, `Rewards` (`GrantedRewardDto[]`), `Currency` |
| `AdPlacementStatus.CooldownRemainingSeconds` | property | 0 if eligible |

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.AdController`
- Consumed by: `ProjectFill.Application.Stage.AdInterstitialService`
- Consumed by: `ProjectFill.Application.Stage.AdDoubleRewardService`
