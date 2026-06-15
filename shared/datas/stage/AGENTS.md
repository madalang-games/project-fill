# shared/datas/stage

## Files
| file | class | role |
|------|-------|------|
| `chapter.csv` | `Chapter` | Chapter order, unlock chain, and background theme id |
| `stage.csv` | `Stage` | Stage-to-chapter metadata and reward group references |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `Chapter.chapter_id` | column | Stable chapter id |
| `Chapter.unlock_chapter_id` | column | Previous chapter required for unlock; blank for first chapter |
| `Chapter.bg_theme_id` | column | Visual theme id for Signal Sort stage map |
| `Stage.stage_id` | column | Stable stage id |
| `Stage.reward_group_id` | column | FK to `reward/reward_group.csv` |

## Rules
- Stage rows describe progression metadata only; board layouts and Signal Type arrangements belong in dedicated level data when added.
- Do not add turn-limit, stamina, cell-clear ratio, or non-circuit fields.

## Cross-refs
- Depends on: `shared/datas/reward/reward_group.csv`
- Consumed by: `Game.OutGame.Lobby`
