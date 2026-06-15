# shared/datas/shop — Shop Catalog

## Files
| file | PK | scope | role |
|------|----|-------|------|
| `iap_category.csv` | `category_id` | C | Shop section groupings; `name_key` → `client_string`; `sort_order` defines display sequence |
| `iap_product.csv` | `info_id` | CS/C | IAP product definitions; `category_id` (C) → `iap_category`; `reward_group_id` → `reward/reward_group` |

## Column Notes — iap_product.csv
| column | scope | note |
|--------|-------|------|
| `info_id` | CS | integer PK; used server-side for purchase tracking |
| `store_product_id` | CS | platform store ID string (UQ); used for Unity IAP lookup |
| `category_id` | C | client-only FK to `iap_category`; drives ShopTabView section grouping |
| `sort_order` | CS | within-category display order; global order = `iap_category.sort_order` → `iap_product.sort_order` |
| `purchase_limit` | CS | 0 = unlimited; >0 = max purchases per `reset_period` |
| `reset_period` | CS | `PurchaseResetPeriod` enum: None/Daily/Weekly/Monthly; lazy-reset on server |
| `reward_group_id` | CS | FK to `reward/reward_group`; drives reward grant on purchase |

## Rules
- `iap_category.csv` is C-only; never add S or CS columns
- `sort_order` in `iap_product` is within-category local order, not global
- `NO_ADS` products: `product_type = NonConsumable`, `purchase_limit = 1`, `reset_period = None`
- reward must be defined in `reward/reward_group` + `reward/reward_item` before referencing here
- NEW_FILE: update this AGENTS.md Files table

## Cross-refs
- Consumed by: `Game.OutGame.Lobby.ShopTabView`
- Consumed by: `ProjectFill.Application.Iap.IapService`
- Depends on: `reward/reward_group.csv`, `reward/reward_item.csv`
