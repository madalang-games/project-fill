# shared/contracts/Inventory

## Files
| file | class | role |
|------|-------|------|
| `InventoryContracts.cs` | `InventoryItemDto`, `InventorySnapshot`, `SpendItemRequest`, `SpendItemResponse`, `BuyItemRequest`, `BuyItemResponse` | Signal Sort booster inventory DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `InventoryItemDto.ItemId` | property | Static booster item id |
| `InventoryItemDto.Count` | property | Owned booster count |
| `SpendItemRequest.Reason` | property | Audit reason such as `stage_booster` |
| `BuyItemResponse.Currency` | property | Updated gold balance |

## Rules
- Current item ids are Signal Sort boosters: Add Lane, Shuffle, Hint, Undo.
- Do not add turn, bomb, rocket, or cell-clear inventory contracts.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.InventoryController`
- Consumed by: `Game.Services.PlayerApiService`
