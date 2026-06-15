# Scripts/Services/Tutorial — Tutorial Manager and Sequencer

Namespace: `Game.Services.Tutorial`

## Files
| file | class | role |
|------|-------|------|
| `TutorialManager.cs` | `TutorialManager` | MonoBehaviour DDOL singleton; evaluates onboarding triggers, tracks progress, and triggers UI overlay |
| `TutorialStepSequencer.cs` | `TutorialStepSequencer` | Pure C# step sequencer driving active step change events |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `TutorialManager.Instance` | prop | DDOL singleton instance |
| `TutorialManager.IsBlocking` | prop | Returns true if active tutorial step blocks board click interactions |
| `TutorialManager.OnBoardReady` | method | Evaluates board triggers for Phase A and B tutorials |
| `TutorialManager.CheckFailTriggers` | method | Evaluates consecutive failure counts for Phase C item hints |

## Rules
- `TutorialManager` handles guest fallback via local `PlayerPrefs` (`tut_done_{groupId}`).
- Syncs completed group IDs to the server database for authenticated players.

## Cross-refs
- Consumed by: `Game.InGame.Controller.InGameController`
- Consumed by: `Game.InGame.Controller.InGameSceneEntry`
- Consumed by: `Game.OutGame.Lobby.LobbyView`
- Consumed by: `Game.Core.UI.TutorialOverlay`
- Depends on: `ProjectFill.Data.Generated.TutorialStep`
- Depends on: `Game.Services.TutorialApiService`
