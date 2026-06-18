# Scripts/Services/Tutorial — Tutorial Manager and Sequencer

Namespace: `Game.Services.Tutorial`

## Files
| file | class | role |
|------|-------|------|
| `TutorialManager.cs` | `TutorialManager` | MonoBehaviour DDOL singleton; evaluates onboarding triggers, tracks group completion, and triggers UI overlay |
| `TutorialStepSequencer.cs` | `TutorialStepSequencer` | Pure C# step sequencer driving active step change events |
| `TutorialEnums.cs` | `TutorialGimmick` | Board gimmick kinds (Locked/Blind/Relay/Overload) for GimmickAppear matching; names must equal CSV `trigger_value` |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `TutorialManager.Instance` | prop | DDOL singleton instance (error-logs if missing) |
| `TutorialManager.ActiveBlocking` | static prop | Null-safe "a tutorial is active" — gates boosters/pause |
| `TutorialManager.BoardInputBlocked` | static prop | True only on `Tap` (info) steps; `Select`/`Move` steps let the real board tap through |
| `TutorialManager.NotifyAction(TutorialAdvanceMode)` | static method | Player did a real action (Select/Move); advances if the current step expects it |
| `TutorialManager.NotifyBoardReady(stageId, gimmicks)` | static method | Null-safe `OnBoardReady` wrapper for scene callers |
| `TutorialManager.NotifyFailRepeat(failCount)` | static method | Null-safe `CheckFailTriggers` wrapper |
| `TutorialManager.Recap()` / `ShowRecap()` | static/method | Manual "How to play" recap (Pause); shows group 8, never persists |
| `TutorialManager.LoadProgress` | method | Fetches server tutorial progress (guest → local PlayerPrefs); invoked by `BootSceneEntry` after auth completes — NOT on own Start(). **Merges (adds only)** |
| `TutorialManager.ReloadProgress` / `ReloadFromServer()` | method / static | Clear-then-fetch (replace, not merge) so a server-side removal reflects; null-safe static wrapper used by the dev `/tutorial` cheat |
| `TutorialManager.IsBlocking` | prop | `_sequencer.IsActive` — every active tutorial step blocks scene input (tap-only model) |
| `TutorialManager.OnBoardReady(stageId, IReadOnlyCollection<TutorialGimmick>)` | method | Evaluates FirstLaunch (by stage id) + GimmickAppear (by present gimmick); triggers lowest incomplete group |
| `TutorialManager.CheckFailTriggers(failCount)` | method | Evaluates FailRepeat (trigger_value == consecutive fail count) |
| `TutorialGimmick` | enum | Locked / Blind / Relay / Overload |

## Rules
- Save unit is `group_id` (from `tutorial_step.csv`), NOT `info_id`; one server save per fully-viewed group (cost: avoids per-step round-trips).
- `TutorialManager` handles guest fallback via local `PlayerPrefs` (`tut_done_{groupId}`).
- Syncs completed `group_id`s to the server database for authenticated players.
- Only one group runs at a time; other eligible groups fire on a later board-ready/fail evaluation.

## Cross-refs
- Consumed by: `Game.OutGame.Boot.BootSceneEntry` (calls `LoadProgress` post-auth)
- Consumed by: `Game.InGame.Controller.InGameController`
- Consumed by: `Game.InGame.Controller.InGameSceneEntry`
- Consumed by: `Game.OutGame.Lobby.LobbyView`
- Consumed by: `Game.Core.UI.TutorialOverlay`
- Depends on: `ProjectFill.Data.Generated.TutorialStep`
- Depends on: `Game.Services.TutorialApiService`
