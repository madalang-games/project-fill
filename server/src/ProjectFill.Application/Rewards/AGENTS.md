# ProjectFill.Application/Rewards

## Files
| file | class | role |
|------|-------|------|
| `RewardService.cs` | `RewardService` | Generic reward source listing, claim handling, and reward group dispatch |
| `AdRewardService.cs` | `AdRewardService` | Generic ad reward facade for supported placements |
| `IAdRewardVerifier.cs` | `IAdRewardVerifier`, `AdVerifyResult` | Provider reward verification boundary |
| `DevelopmentAdRewardVerifier.cs` | `DevelopmentAdRewardVerifier` | Legacy/test verifier; not registered by default |
| `AdMobSsvKeyCache.cs` | `AdMobSsvKeyCache` | IHostedService; fetches Google ECDSA public keys hourly |
| `AdMobSsvCallbackService.cs` | `AdMobSsvCallbackService` | Processes AdMob SSV callback: verifies ECDSA, stores Redis ssv:{nonce}=txid |
| `AdMobSsvVerifier.cs` | `AdMobSsvVerifier` | IAdRewardVerifier impl; `mock` accepts nonblank token, `ssv` consumes Redis nonce |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AdVerifyResult` | record | `(bool Verified, string ProviderTxId)` |
| `IAdRewardVerifier.VerifyAsync` | method | `(provider, adToken, ct)` -> `AdVerifyResult`; adToken = SSV nonce or temporary mock token |
| `RewardService.ClaimAsync` | method | Claims `reward_source` and dispatches reward items |
| `RewardService.GrantRewardGroupAsync` | method | Dispatches reward group items (SOFT_CURRENCY, ITEM) |
| `AdRewardService.ClaimAsync` | method | Routes ad reward through service |
| `AdMobSsvKeyCache.GetKeyBytes` | method | Returns ECDSA public key bytes by keyId |
| `AdMobSsvCallbackService.ProcessAsync` | method | rawQuery -> verify ECDSA -> Redis ssv:{nonce}=txid TTL 5min |

## Rules
- Daily claim periods use KST.
- Ad reward verification is controlled by `AD_REWARD_VERIFY_MODE`, not ASP.NET environment.
- `AD_REWARD_VERIFY_MODE=mock`: `AdMobSsvVerifier` accepts nonblank provider/adToken and creates bounded mock provider tx id.
- `AD_REWARD_VERIFY_MODE=ssv`: `AdMobSsvVerifier` requires a nonce stored by AdMob SSV callback in Redis.
- `VerifyAsync` consumes the nonce (GETDEL); non-200 responses from SSV callback trigger Google retry.

## Cross-refs
- Depends on: `shared/datas/reward/reward_source.csv`
- Depends on: `shared/datas/reward/reward_item.csv`
- Consumed by: `ProjectFill.API.Controllers.RewardsController`
- Consumed by: `ProjectFill.Application.Stage.AdDoubleRewardService`
