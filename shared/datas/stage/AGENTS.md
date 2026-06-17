# shared/datas/stage

## Files
| file | class | role |
|------|-------|------|
| `chapter.csv` | `Chapter` | Chapter order, unlock chain, and background theme id |
| `stage.csv` | `Stage` | Stage progression metadata + Signal Sort definition (gimmicks) + explicit `board` layout |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `Chapter.chapter_id` | column | Stable chapter id |
| `Chapter.unlock_chapter_id` | column | Previous chapter required for unlock; blank for first chapter |
| `Chapter.bg_theme_id` | column | Visual theme id for Signal Sort stage map |
| `Chapter.chest_reward_group_id` | column | FK to `reward/reward_group.csv`; milestone chest granted when all stages in chapter first-cleared |
| `Stage.stage_id` | column | Stable stage id |
| `Stage.reward_group_id` | column | FK to `reward/reward_group.csv` |
| `Stage.par_moves` | column | Server-only (scope `S`) target move count; a clear with `best_moves_used <= par_moves` counts as a perfect clear (feeds `perfect` ranking). Designer-authored per stage. |
| `Stage.types` | column | Signal Type count = number of sets |
| `Stage.lane_kinds` | column | Per-lane kind codes `N`/`L`/`B` (Normal/Locked/Blind); length = lane count |
| `Stage.lock_unlock` | column | Per-lane unlock glyph for Locked lanes, `.` otherwise (e.g. `....R`) |
| `Stage.overload_type` | column | Overload Signal Type index, `-1` = none |
| `Stage.relay_order` | column | Relay absorb order as glyph sequence (e.g. `RBGYP`); blank = none |
| `Stage.board` | column | Compact chip layout: `Capacity(4)` chars/lane, no delimiters; UPPER=normal, lower=overload, `-`=empty slot. `""` = not yet generated |

## Rules
- `stage.csv` is authored by `tools/stage_editor` (Signal Sort). `board` stores the **explicit** initial
  layout (no seed/regeneration) — unconditional render, decoupled from any generator algorithm.
- Glyph order = SignalType order: `R B G Y P C O M L T` (0–9). `lane_kinds`=`N`/`L`/`B`.
- Do not add turn-limit, stamina, cell-clear ratio, or other non-circuit fields.
- The Unity runtime builds boards from these columns: `InGameController.LoadCurrent` reads
  `StageDataService.GetStage(stage_id)`, maps gimmicks via `BuildDefinition`, and decodes `board` via
  `BoardCodec.Decode`. Empty `board` (or no row) → `BoardFactory.Generate` fallback; null → `StageLibrary`.

## Cross-refs
- Depends on: `shared/datas/reward/reward_group.csv`
- Consumed by: `Game.OutGame.Lobby`
- Authored by: `tools/stage_editor` → `tools/stage_generator` (C# CLI)
