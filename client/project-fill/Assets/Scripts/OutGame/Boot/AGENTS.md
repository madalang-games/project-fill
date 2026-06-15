# OutGame/Boot — Boot Scene

Namespace: `Game.OutGame.Boot`

## Files
| file | class | role |
|------|-------|------|
| `BootView.cs` | `BootView` | Logo image + spinner toggle |
| `BootSceneEntry.cs` | `BootSceneEntry` | MonoBehaviour; calls AuthService.Initialize → UIManager.ShowLoading → FadeToScene("Lobby") |
| `ReLoginView.cs` | `ReLoginView` | Canvas_Popup panel: Re-login + Continue as Guest buttons |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BootSceneEntry.Start()` | method | Entry point; shows loading, calls auth |
| `BootSceneEntry.OnContinueAsGuestConfirmed()` | method | Calls `AuthService.ContinueAsGuest` — NOT Initialize; only explicit user action creates guest session |
| `BootView.SetSpinnerActive(bool)` | method | Shows/hides spinner GameObject |
| `ReLoginView.Init(onReLogin,onContinueAsGuest)` | method | Wires button callbacks |

## Rules
- Boot scene hosts UIManager, SceneTransition, StageDataService, PlayerProgressService, AuthService, CurrencyApiService, TutorialManager, TutorialApiService GameObjects (DDOL)
- Auth failure (ReLoginRequired) shows ReLoginView — NEVER auto-fallback to guest

## Cross-refs
- Depends on: `Game.Core.UIManager`, `Game.Core.SceneTransition`, `Game.Services.AuthService`
