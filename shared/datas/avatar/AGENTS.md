# shared/datas/avatar

## Files
| file | class | role |
|------|-------|------|
| `avatar.csv` | `Avatar` | Avatar resource and unlock metadata |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `Avatar.id` | column | Stable avatar id |
| `Avatar.resource_name` | column | Resource key mapped by UI avatar assets |
| `Avatar.unlock_cost` | column | Gold cost; `0` means initially available |
| `Avatar.unlock_type` | column | Unlock tier label such as `common`, `rare`, or `legendary` |

## Rules
- Avatar data is cosmetic only; do not add gameplay stats.
- New avatar resources must be added to `common/dynamic_resource.csv` when dynamically loaded.

## Cross-refs
- Depends on: `shared/datas/common/dynamic_resource.csv`
- Consumed by: `Game.OutGame.Account`
