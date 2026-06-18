# shared/datas/item

## Files
| file | class | role |
|------|-------|------|
| `item.csv` | `Item` | Signal Sort booster metadata and prices |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `Item.id` | column | Stable booster id |
| `Item.name_key` | column | FK to `string/client_string.csv` |
| `Item.desc_key` | column | FK to `string/client_string.csv` |
| `Item.icon_name` | column | Dynamic resource key |
| `Item.price` | column | Gold price for instant purchase |

## Rules
- Current boosters are `Add Lane`, `Shuffle`, and `Undo` (item ids 1/2/4; id 3 `Hint` removed from spec).
- Do not add turn, bomb, rocket, cell-clear items for Signal Sort.

## Cross-refs
- Depends on: `shared/datas/string/client_string.csv`
- Depends on: `shared/datas/common/dynamic_resource.csv`
- Consumed by: `Game.Services.ItemDataService`
- Consumed by: `Game.InGame.Controller.InGameController` (BoosterType handling)
