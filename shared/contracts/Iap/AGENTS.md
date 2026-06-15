# shared/contracts/Iap

## Files
| file | class | role |
|------|-------|------|
| `IapContracts.cs` | `VerifyIapRequest`, `VerifyIapResponse`, `IapProductStatusDto`, `GetIapProductsResponse` | IAP verification and product status DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `VerifyIapRequest.InfoId` | property | Static `iap_product.csv` row id |
| `VerifyIapRequest.StoreProductId` | property | Platform store product id |
| `VerifyIapResponse.GrantedRewards` | property | Rewards granted by purchase verification |
| `IapProductStatusDto.RemainingPurchases` | property | `-1` = unlimited; `0+` = remaining purchase count |

## Rules
- DTOs are platform-neutral; store-specific receipt validation stays server-side.
- IAP products grant Signal Sort rewards only: gold, boosters, and no-ads.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.IapController`
- Consumed by: `Game.Services.IAPService`
