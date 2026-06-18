# shared/datas/common

## Files
| file | role |
|------|------|
| `color_palette.csv` | Master color palette — 16 predefined colors (color_id 0–15) |
| `dynamic_resource.csv` | Dynamic sprite resources (Signal Sort boosters, chests, toasts, avatars, UI assets, IAP product icons, event badges, decoration) |

## Rules
- `color_palette.csv` has exactly 16 Signal Type colors (color_id 0–15); do not reorder or delete rows.
- RGB values are placeholder until finalized by art; do not change without art sign-off

## Cross-refs
- Consumed by: stage editor (Signal Type color picker), `client/Assets/Scripts/` (Signal Chip renderer)
- Gen output: `client/Assets/Resources/Data/common/`
