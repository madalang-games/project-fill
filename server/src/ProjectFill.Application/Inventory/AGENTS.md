# ProjectFill.Application/Inventory

## Files
| file | class | role |
|------|-------|------|
| `InventoryService.cs` | `InventoryService` | Booster item inventory: sync, spend, grant, and buy-with-Gold |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `InventoryService.GetInventoryAsync` | method | Returns all `user_inventory` rows for the player |
| `InventoryService.SpendItemAsync` | method | Deducts item count; throws if insufficient |
| `InventoryService.GrantItemAsync` | method | Upserts item count; inserts row if missing |
| `InventoryService.BuyItemAsync` | method | Spends soft currency then grants 1 item; transactional |

## Cross-refs
- Depends on: `shared/datas/item/item.csv`
- Depends on: `ProjectFill.Application.Currency.CurrencyService`
- Consumed by: `ProjectFill.API.Controllers.InventoryController`
- Consumed by: `ProjectFill.Application.Rewards.AdRewardService`
