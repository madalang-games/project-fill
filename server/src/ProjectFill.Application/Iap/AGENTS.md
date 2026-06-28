# ProjectFill.Application/Iap

## Files
| file | class | role |
|------|-------|------|
| `IapService.cs` | `IapService` | IAP purchase verification, product status listing, purchase limit enforcement |
| `GooglePlayVerifier.cs` | `GooglePlayVerifier` | Server-side Google Play receipt verification + acknowledge; returns store-authoritative order id/token |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `IapService.VerifyIapAsync` | method | Validates purchase (real store verify for non-mock platforms), records transaction, grants reward group, flips no-ads for NonConsumable |
| `IapService.GetProductStatusesAsync` | method | Returns enabled products with remaining purchase counts |
| `GooglePlayVerifier.VerifyAndAcknowledgeAsync` | method | `(productSku, receiptData, ct)` → `(orderId, purchaseToken)`; parses Unity receipt, verifies + acknowledges via AndroidPublisher |

## Rules
- Mock platform (`platform == "mock"`, client-trusted id, no store check) is allowed only outside prod (`Game:Environment` not `prod*`); rejected with `IAP_VERIFICATION_FAILED` in prod.
- Real platforms: `order_id`/`purchase_token` come from `GooglePlayVerifier` (store-authoritative), never from the request body.
- Transient store/API outages → `IAP_VERIFY_PENDING` (client retries); permanent failures → `IAP_VERIFICATION_FAILED`.
- Duplicate order check (resolved `order_id` + `platform`) is the idempotency guard.
- Purchase limit enforcement respects `PurchaseResetPeriod` from `IapProductData`.
- NonConsumable products flip the persistent `players.is_no_ads` flag inside the purchase transaction.
- Reward grant is wrapped in the same DB transaction as the purchase record; store verification runs BEFORE the transaction opens.

## Cross-refs
- Depends on: `shared/datas/shop/iap_product.csv`
- Depends on: `ProjectFill.Application.Rewards.RewardService`
- Depends on: `ProjectFill.Application.Currency.CurrencyService`
- Depends on: `Google.Apis.AndroidPublisher.v3` (NuGet); env `GOOGLE_PLAY_PACKAGE_NAME`, `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` → config `GooglePlay:*`
- Consumed by: `ProjectFill.API.Controllers.IapController`
