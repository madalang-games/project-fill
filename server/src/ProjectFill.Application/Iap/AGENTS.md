# ProjectFill.Application/Iap

## Files
| file | class | role |
|------|-------|------|
| `IapService.cs` | `IapService` | IAP purchase verification, product status listing, purchase limit enforcement |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `IapService.VerifyIapAsync` | method | Validates purchase, records transaction, grants reward group |
| `IapService.GetProductStatusesAsync` | method | Returns enabled products with remaining purchase counts |

## Rules
- Duplicate order check (`order_id` + `platform`) is the idempotency guard.
- Purchase limit enforcement respects `PurchaseResetPeriod` from `IapProductData`.
- Reward grant is wrapped in the same DB transaction as the purchase record.

## Cross-refs
- Depends on: `shared/datas/shop/iap_product.csv`
- Depends on: `ProjectFill.Application.Rewards.RewardService`
- Depends on: `ProjectFill.Application.Currency.CurrencyService`
- Consumed by: `ProjectFill.API.Controllers.IapController`
