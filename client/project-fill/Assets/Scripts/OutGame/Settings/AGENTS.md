# OutGame/Settings — Settings Panel & Account Popup

Namespace: `Game.OutGame.Settings`

## Files
| file | class | role |
|------|-------|------|
| `SettingsPanelView.cs` | `SettingsPanelView` | Bottom-sheet popup: BGM/SFX/ScreenShake/Haptic toggles, language dropdown, version text |
| `AccountPopupView.cs` | `AccountPopupView` | Title ribbon (`popup.account.title`); `NicknameArea` (label+input+save); `PidArea` (PID label `popup.account.label_pid` + `_pidText` value + Copy→clipboard+toast); Privacy/Terms web links; guest → Link Account; OAuth → Switch Account. Avatar selection moved to Shop avatar section; achievements moved to lobby Achievement tab. Retains `_avatarSprites` + `GetAvatarSprite` (HeaderView/TutorialOverlay read equipped avatar sprite from this prefab). **`_pidText` value has NO LocalizedText stringId** — runtime `Awake` sets PID; a stringId would clobber it via `LocalizedText.OnEnable` |
| `AccountRestartPopupView.cs` | `AccountRestartPopupView` | Inform popup: game restart required; single confirm → FadeToScene("Boot") |
| `AccountConflictPopupView.cs` | `AccountConflictPopupView` | Shows local vs cloud SaveSnapshot; user picks keep-local or use-cloud |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `SettingsPanelView._bgmToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_bgm` |
| `SettingsPanelView._sfxToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_sfx` |
| `SettingsPanelView._screenShakeToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_screen_shake` |
| `SettingsPanelView._hapticToggle` | SerializeField | Toggle; persisted to PlayerPrefs `setting_haptic_enabled` |
| `SettingsPanelView._langDropdown` | SerializeField | TMP_Dropdown; options built from `_supportedLangs`; saves via `LocalizationService.SetLanguage` |
| `SettingsPanelView._supportedLangs` | static field | `Language[]` — currently `{KO, EN}`; expand to add more languages |
| `SettingsPanelView._langStringIds`   | static field | CSV keys aligned with `_supportedLangs`; `LocalizationService.Get()` resolves to current-language name (e.g. EN→"Korean"/"English", KO→"한국어"/"영어") |
| `AccountPopupView.OnCopyPid()` | method | Copies `AuthService.Pid` to `GUIUtility.systemCopyBuffer`; success toast `toast.pid_copied`; no-op if PID empty |
| `AccountPopupView.OpenWeb(path)` | method | `Application.OpenURL` to `AppConfig.{Dev,Prod}WebUrl + path`; env from `NetworkService.Environment`; called by Privacy/Terms buttons (`AppConfig.WebPrivacyPath`/`WebTermsPath`) |
| `AccountPopupView.OnLinkAccount()` | method | Guest only; Google Sign-In → `AuthService.LinkGoogle` → conflict popup or close |
| `AccountPopupView.OnSwitchAccount()` | method | OAuth only; confirm dialog → Google Sign-In → `AuthService.LoginGoogle`; PID mismatch triggers restart via `CompleteSession` |
| `AccountPopupView.ResolveConflict(token,selection)` | method | Calls `AuthService.ResolveConflict`; restart handled by `CompleteSession` |
| `AccountPopupView.GetAvatarSprite(int)` | method | Resolves equipped avatar sprite from `_avatarSprites`; consumed by `Lobby.HeaderView` + `Core.UI.TutorialOverlay` |
| `AccountRestartPopupView.Init(onConfirm)` | method | Sets localized strings; confirm button fires `onConfirm` then closes popup |
| `AccountConflictPopupView._backdropButton` | SerializeField | Interactive backdrop Btn; calls Close() — wired in UIEditorSetup |
| `AccountConflictPopupView._closeButton` | SerializeField | Square top-right ✕ button; calls Close() |
| `AccountConflictPopupView.Init(...)` | method | save-snapshot (maxStage/gold/**cleared stages**/items, local+cloud) + 2 action callbacks; cancel/backdrop/close all call Close(). Star rating removed → shows `Cleared: {n}` from server `totalClearedStages` |

## Rules
- SettingsPanelView entry points: Lobby Header ⚙ button AND InGame Pause popup [Settings]
- AccountPopupView is NOT opened from SettingsPanelView; it is a standalone popup
- Both panels live on Canvas_Popup (via UIManager.ShowPopup)
- Language dropdown options use `_langStringIds` CSV keys → always renders in the active font atlas (no hardcoded strings); add new entry to both `_supportedLangs` and `_langStringIds` in tandem, and add the key to `client_string.csv`
- Google Sign-In: `OnLinkAccount` / `DoSwitchAccount` use `GoogleSignInBridge.SignIn`; Android-only; guarded by `#if UNITY_ANDROID`
- `AppConfig.GoogleWebClientId` must be set before Google Sign-In works; shows error toast if empty
- **Logout removed**: no logout button or flow; guest cannot recover account after new guest creation
- Link flow: `AuthService.LinkGoogle` → `POST /api/auth/link-oauth`; server ALWAYS returns `Conflict: true` for guest→Google link; `AccountConflictPopupView` always shown
- Resolve flow: `AuthService.ResolveConflict` → `POST /api/auth/resolve-conflict`; selection = "local" | "cloud"; triggers `CompleteSession` → `AccountRestartPopupView` → Boot

## Cross-refs
- Depends on: `Game.Core.UIManager`, `Game.Services.AuthService`, `Game.Services.LocalizationService`
