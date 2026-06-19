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
| `Avatar.unlock_cost` | column | Gold cost; `0` = NOT gold-purchasable (auto-unlocked common, or reward-only) |
| `Avatar.unlock_type` | column | Acquisition marker: `common`/`free` (auto-unlocked), `uncommon`/`rare`/`epic` (gold), `achievement` (reward-only — blocked from gold purchase, granted only via `AVATAR` reward_item from achievement/daily/event groups) |

## Rules
- Avatar data is cosmetic only; do not add gameplay stats.
- New avatar resources must be added to `common/dynamic_resource.csv` when dynamically loaded.
- Reward-only avatars: `unlock_cost=0` + `unlock_type=achievement` (matches server `AvatarUnlockTypes.Achievement`); must be granted by an `AVATAR` row in `reward/reward_item.csv` or they are unobtainable. Gold purchase is rejected server-side (`PlayerService.UpdateProfileAsync`).

## Cross-refs
- Depends on: `shared/datas/common/dynamic_resource.csv`
- Consumed by: `Game.OutGame.Settings.AccountPopupView`
