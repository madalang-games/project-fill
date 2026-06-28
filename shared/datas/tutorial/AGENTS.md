# datas/tutorial — Tutorial CSV Data

## Files
| file | role |
|------|------|
| `tutorial_step.csv` | Static data defining tutorial step parameters, triggers, target UI identifiers, and text localizations |

## Columns of `tutorial_step.csv`
- `info_id` (int32): Range-based info_id for the step (e.g. 101, 102). PK.
- `group_id` (int32): Explicit tutorial group = **server save unit**. All steps in a group share one trigger; the client saves/queries completion per `group_id` (not per `info_id`). Replaces the old `(info_id/100)*100` derivation.
- `trigger_type` (TutorialTriggerType): Condition that starts the group (`FirstLaunch`, `GimmickAppear`, `FailRepeat`).
- `trigger_value` (string): Context/value for the trigger, parsed by `trigger_type` — FirstLaunch=stage id (int), GimmickAppear=gimmick enum name (`Locked`/`Blind`/`Relay`/`Overload`, parsed to `TutorialGimmick`), FailRepeat=consecutive-fail threshold (int).
- `step_index` (int32): 0-indexed step sequence inside the group.
- `content_type` (TutorialContentType): Visual representation (`FingerOverlay`, `Tooltip`, `HighlightOnly`, `DragPointer`). `DragPointer` animates `ui_drag_pointer` from `target_ui_id`→`target_ui_id_to`.
- `advance_mode` (TutorialAdvanceMode): How the step advances. `Tap` = overlay tap (informational, blocks scene). `Select`/`Move` = real board action (interactive; overlay lets the tap through, advances when the player selects a lane / completes a move — generic, any valid action).
- `target_ui_id` (string): Spotlight/source target id (matches a `TutorialTarget._targetIds`). Empty = no spotlight, centered tooltip. Registered ids: `hud_moves_count`+`booster_bar` (UIEditorSetup, UI); `slot_lane_{n}`+`slot_lane_area`+`signal_panel`+gimmick ids `gimmick_locked`/`gimmick_blind`/`gimmick_overload` (BoardView, runtime, World) + `signal_node_{n}` per panel node (SignalPanelView, runtime, World; n=1 is first in relay order). GimmickAppear rows target the gimmick id so the real gimmick lane is highlighted.
- `target_ui_id_to` (string): Drag destination id for `DragPointer`/`Move` steps (e.g. `slot_lane_2`). Empty otherwise.
- `target_space` (TargetSpaceType): Space coordinate type (`World`, `UI`).
- `text_key` (string): Localization key mapping to `client_string.csv`.

## Groups (server save unit = `group_id`)
| group_id | trigger | trigger_value | info_id range |
|----------|---------|---------------|---------------|
| 1 | FirstLaunch | 1 (stage) | 101–104 |
| 2 | FirstLaunch | 2 (stage) | 201 |
| 3 | GimmickAppear | Locked | 301–302 (intro + how) |
| 4 | GimmickAppear | Blind | 401–402 (what + reveal) |
| 5 | GimmickAppear | Relay | 501–503 (panel overview → node1+drag → node2+drag) |
| 6 | GimmickAppear | Overload | 601–602 (intro + rule) |
| 7 | FailRepeat | 3 (fails) | 701 |
| 8 | Manual (Pause "How to play" recap) | howtoplay | 801–804 |

Group 1 is interactive: 101 `Select` (tap a lane) + 102 `Move`/`DragPointer` (drag to slot_lane_2). GimmickAppear groups are multi-step for detail; Relay walks the Signal Panel: whole-panel highlight → `signal_node_1` + drag pointer → `signal_node_2` + drag pointer (drag `signal_node_k`→`signal_node_{k+1}`; parks on source when the next node is absent). The rest are `Tap`. Group 8 never auto-fires (Manual) and never persists completion.

## Rules
- When changing this CSV, re-run `tools/info_generator.bat` or `tools/all_generator.bat` to rebuild the C# static tables and JSON bundles.
- Keep `info_id` unique; all steps of one tutorial sequence share one `group_id` and one trigger.
- `Tap` steps advance on overlay tap (scene blocked); `Select`/`Move` steps advance on the real board action (scene input passes through).

## Cross-refs
- Gen output: `client/project-fill/Assets/Resources/data/tutorial/tutorial_step.csv` (via `info_generator`)
- Consumed by: `ProjectFill.Data.Generated.TutorialStep`
