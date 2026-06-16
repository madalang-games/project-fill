# ProjectFill.Application/Cosmetic

## Files
| file | class | role |
|------|-------|------|
| `CosmeticService.cs` | `CosmeticService` | Cosmetic catalog listing, gold unlock, equip, and condition-based unlock |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CosmeticService.GetListAsync` | method | Catalog + per-player unlock state + active equip |
| `CosmeticService.UnlockWithGoldAsync` | method | Spends gold for `unlock_type=Gold` cosmetic; transactional; returns `CurrencySnapshot` |
| `CosmeticService.SetActiveAsync` | method | Equips chip/lane/board skins; validates ownership + category; empty slot → default |
| `CosmeticService.UnlockByConditionAsync` | method | Pull-based unlock of all cosmetics matching `unlock_condition_id`; no SaveAsync (caller saves); returns newly unlocked ids |

## Rules
- Default cosmetics (`unlock_type=Default`) are always owned implicitly — no `user_cosmetics` row.
- Cosmetics are visual only; never gate gameplay.
- `UnlockByConditionAsync` is the seam for attendance / achievement / daily-challenge milestone grants; condition id namespace: achievement_id, `day_{n}`, `streak_{n}`.

## Cross-refs
- Depends on: `shared/datas/cosmetic/cosmetic_item.csv`, `ProjectFill.Application.Currency.CurrencyService`
- Depends on: `ProjectFill.Infrastructure.Generated.UserCosmeticsRow`, `UserActiveCosmeticsRow`
- Consumed by: `ProjectFill.API.Controllers.CosmeticController`
