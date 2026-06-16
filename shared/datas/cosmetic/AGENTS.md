# shared/datas/cosmetic

## Files
| file | class | role |
|------|-------|------|
| `cosmetic_item.csv` | `CosmeticItem` | Cosmetic catalog: chip/lane/board skins, unlock method, gold cost |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CosmeticItem.cosmetic_id` | column | PK; string id referenced by reward/unlock systems |
| `CosmeticItem.category` | column | `CosmeticCategory` enum: Chip / Lane / Board |
| `CosmeticItem.unlock_type` | column | `CosmeticUnlockType` enum: Default / Gold / Achievement / Attendance / Challenge |
| `CosmeticItem.unlock_cost` | column | Gold cost when `unlock_type=Gold`; 0 otherwise |
| `CosmeticItem.unlock_condition_id` | column | Achievement id / attendance milestone (`day_30`,`day_100`) / challenge streak (`streak_30`,`streak_100`) that grants this cosmetic; blank for Default/Gold |
| `CosmeticItem.preview_res` | column | Dynamic resource key for preview sprite |

## Rules
- Cosmetics are purely visual — never add gameplay-affecting stats.
- Unlock is pull-based: when a system reaches a milestone it calls `CosmeticService.UnlockByConditionAsync(condition_id)`, which unlocks every cosmetic whose `unlock_condition_id` matches.
- `unlock_condition_id` values MUST match the emitting system's milestone ids (achievement_id, `day_{n}`, `streak_{n}`).

## Cross-refs
- Consumed by: `ProjectFill.Application.Cosmetic.CosmeticService`
- Consumed by: `Game.Services.CosmeticApiService`
- Depends on: `shared/datas/common/dynamic_resource.csv` (preview_res)
