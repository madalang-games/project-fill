# shared/contracts/Ad

## Files
| file | class | role |
|------|-------|------|
| `AdRequests.cs` | `AdDoubleRewardRequest`(stub), `AdInterstitialShownRequest` | Ad request DTOs; `AdDoubleRewardRequest` retained for compilation only |
| `AdResponses.cs` | `AdPlacementStatus`, `AdEligibilityResponse`, `AdDoubleRewardGrantResponse`(stub), `AdInterstitialShownResponse` | Ad response DTOs; `AdDoubleRewardGrantResponse` retained for compilation only |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AdDoubleRewardRequest` | class | **STUB** — double reward removed; retained to avoid breaking generated contract sync |
| `AdDoubleRewardGrantResponse` | class | **STUB** — double reward removed; retained to avoid breaking generated contract sync |
| `AdPlacementStatus.CooldownRemainingSeconds` | property | 0 if eligible |

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.AdController`
- Consumed by: `ProjectFill.Application.Stage.AdInterstitialService`
