# shared/datas/currency

## Files
| file | role |
|------|------|
| `currency.csv` | Currency type definitions — maps server reward_type_key to client display data |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `currency_id` | PK | int32 |
| `reward_type_key` | string(32) | Matches server GrantedReward.RewardType (e.g. "SOFT_CURRENCY") |
| `name_key` | string(64) | FK → client_string.csv |
| `icon_name` | string(64) | C-scope; FK → dynamic_resource.csv resource_key |
| `desc_key` | string(64) | C-scope; FK → client_string.csv; used in RewardItemCellView long-press tooltip |

## Rules
- `reward_type_key` values must match server-side RewardType enum strings exactly
- `icon_name` scope is C (client only) — maps to `dynamic_resource.csv`

## Cross-refs
- Consumed by: `Game.Services.CurrencyDataService`
- Depends on: `shared/datas/common/dynamic_resource.csv`, `shared/datas/string/client_string.csv`
