# OutGame/Boot — Boot Scene

Namespace: `Game.OutGame.Boot`

## Files
| file | class | role |
|------|-------|------|
| `BootView.cs` | `BootView` | Logo image + spinner toggle |
| `BootSceneEntry.cs` | `BootSceneEntry` | MonoBehaviour; BootstrapService gate → AuthService.Initialize → UIManager.ShowLoading → FadeToScene("Lobby"); falls back to direct auth if BootstrapService absent |
| `ReLoginView.cs` | `ReLoginView` | Canvas_Popup panel: Re-login + Continue as Guest buttons |
| `ForceUpdateView.cs` | `ForceUpdateView` | Forced-action popup; Init() wires Update button → opens platform store (market://, AppStoreUrl, GooglePlayStoreUrl) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BootSceneEntry.Start()` | method | Entry point; shows loading, runs BootstrapService gate (or auth if absent) |
| `BootSceneEntry.OnBootstrapResult(BootstrapResult)` | method | ForceUpdate → ForceUpdateView popup; PatchFailed → network error retry; OK → AuthService.Initialize |
| `ForceUpdateView.Init()` | method | Wires Update button to open store; logs error if button unassigned |
| `BootSceneEntry.OnContinueAsGuestConfirmed()` | method | Calls `AuthService.ContinueAsGuest` — NOT Initialize; only explicit user action creates guest session |
| `BootView.SetSpinnerActive(bool)` | method | Shows/hides spinner GameObject |
| `ReLoginView.Init(onReLogin,onContinueAsGuest)` | method | Wires button callbacks |

## Rules
- Boot scene hosts UIManager, SceneTransition, StageDataService, PlayerProgressService, AuthService, CurrencyApiService, TutorialManager, TutorialApiService, BootstrapService GameObjects (DDOL)
- BootstrapService runs before auth; if its GameObject is absent the boot falls back to direct AuthService.Initialize
- `ForceUpdateView` prefab built via `UIEditorSetup` (`Tools/UI Setup/Prefabs/ForceUpdateView`); lives in `Resources/Prefabs/UI/`
- Auth failure (ReLoginRequired) shows ReLoginView — NEVER auto-fallback to guest

## Cross-refs
- Depends on: `Game.Core.UIManager`, `Game.Core.SceneTransition`, `Game.Services.AuthService`
