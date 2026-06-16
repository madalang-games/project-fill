# datas/tutorial — Tutorial CSV Data

## Files
| file | role |
|------|------|
| `tutorial_step.csv` | Static data defining tutorial step parameters, triggers, target UI identifiers, and text localizations |

## Columns of `tutorial_step.csv`
- `info_id` (int32): Range-based info_id for the step (e.g. 101, 102). PK. Range encodes tutorial group: 1xx = group 1, 2xx = group 2.
- `trigger_type` (TutorialTriggerType): Condition that starts the tutorial (`FirstLaunch`, `GimmickAppear`, `FailRepeat`).
- `trigger_value` (string): Context/value for the trigger condition (e.g., stage ID, gimmick name, fail count).
- `step_index` (int32): 0-indexed step sequence inside the group.
- `content_type` (TutorialContentType): Visual representation (`FingerOverlay`, `Tooltip`, `HighlightOnly`).
- `target_ui_id` (string): ID of the UI element or board target (e.g. `slot_lane_1`, `hud_moves_count`, `signal_panel`, `booster_bar`).
- `target_space` (TargetSpaceType): Space coordinate type (`World`, `UI`).
- `text_key` (string): Localization key mapping to `client_string.csv`.
- `auto_advance_sec` (float): Auto-advance duration (0.0 = wait for user action).
- `is_blocking` (bool): If true, blocks general interactions except the targeted action.

## Rules
- When changing this CSV, re-run `tools/info_generator.bat` or `tools/all_generator.bat` to rebuild the C# static tables and JSON bundles.
- Keep `info_id` values unique within each range group and logically grouped.

## Cross-refs
- Gen output: `client/project-fill/Assets/Resources/Data/tutorial/tutorial_step.csv` (via `info_generator`)
- Consumed by: `ProjectFill.Data.Generated.TutorialStep`
