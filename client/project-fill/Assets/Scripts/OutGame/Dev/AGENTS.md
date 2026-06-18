# Scripts/OutGame/Dev — DEV-ONLY cheat overlay

Entire folder is compiled out of release builds (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`). Client face
of the server cheat system (`docs/design/server-cheat-system-design.md`). The UI is a **prefab built by
`UIEditorSetup.CreateCheatOverlay()`** (palette / Panel 3-layer / Btn 96px / TMP AutoFontSize convention)
at `Resources/Prefabs/UI/CheatOverlayView.prefab`, **dynamic-loaded** once at startup.

## Files
| file | class | role |
|------|-------|------|
| `DevEnums.cs` | `CheatDomain` | Client mirror of server `CheatDomain`; drives button-mode tabs + command tokens (token = enum name lowercased); array index order must match the prefab's `_domainTabButtons` |
| `CheatOverlayView.cs` | `CheatOverlayView` | Prefab-bound view: backtick toggle, command + button input → `POST /api/dev/cheat/command`, log + prefix-based local refresh, Docs button → `GET /api/dev/cheat/docs` |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CheatOverlayView.Bootstrap` | static method | `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`; `Resources.Load("Prefabs/UI/CheatOverlayView")` + Instantiate (DDOL) once in editor/debug builds |
| `CheatOverlayView.SendCommand` | method | Assembles `{command}` JSON, posts, logs message, calls `RefreshLocalState` |
| `CheatOverlayView.RebuildActions` | method | Relabels/rewires the 4 pooled `_actionButtons` for the selected `CheatDomain` (presets in `PresetsFor`) |
| `CheatOverlayView.RefreshLocalState` | static method | Prefix branch: `/gold`→`CurrencyApiService.FetchGold`, `/item`→`InventoryApiService.FetchInventory`, `/cosmetic`→`CosmeticApiService.FetchCosmetics`, `/stage`→`PlayerProgressService.ApplyMaxClearedStage` + `HomeTabView.Refresh` (stage map is local-cache driven; no lobby refetch endpoint), `/tutorial`→`TutorialManager.ReloadFromServer` (clear-then-fetch; evaluated on board entry so no re-render). achievement/attendance self-fetch on tab/popup open; ad has no client state |
| `CheatOverlayView.OpenDocs` | method | GETs docs HTML, writes `persistentDataPath/cheat_docs.html`, `Application.OpenURL` |

## Rules
- DEV-ONLY: whole file `#if`-guarded; ships nothing in release. The prefab asset may ship but is never loaded (Bootstrap is compiled out; the MonoBehaviour script is absent → missing-script, harmless).
- UI structure lives in `UIEditorSetup.Cheat.cs` (no manual `.prefab` edit). After changing the prefab structure → FLAG(Unity) `Tools/UI Setup/Prefabs/CheatOverlay`.
- Backquote (`` ` ``) toggles the `Panel`; the root stays active (so `Update` polls the key); starts hidden.
- Button mode branches on `CheatDomain` enum; never on raw command strings. Both modes assemble the same
  `/domain …` string and post to the same endpoint (server/parser do not distinguish input path).
- Labels are inline English literals (TMP stringId = null → LocalizedText font-only) — NOT localized, NO
  `client_string.csv`/font-subset churn.

## Cross-refs
- Built by: `Game.Editor.UIEditorSetup.CreateCheatOverlay` (`UIEditorSetup.Cheat.cs`)
- Depends on: `Game.Services.NetworkService`, `CurrencyApiService`/`InventoryApiService`/`CosmeticApiService`
- Server endpoint: `ProjectFill.API.Controllers.DevCheatController` (`/api/dev/cheat/command` + `/docs`)
