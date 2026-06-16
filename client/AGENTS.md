# client - Unity 6 (URP 2D)

## Stack
Unity 6 | URP 2D | C# | New Input System

## Nav
| path | role |
|------|------|
| `project-fill/Assets/Scripts/` | C# code root: Core, InGame, OutGame, Services, Editor | → `Scripts/AGENTS.md` |
| `project-fill/Assets/Resources/Prefabs/Game/` | Runtime-loaded gameplay prefabs |
| `project-fill/Assets/Sprites/PlayerSettings/` | PlayerSettings splash/background/logo/icon source sprites |
| `project-fill/Assets/Resources/Data/` | Runtime CSVs — generated, DO NOT EDIT |
| `project-fill/Assets/Resources/Prefabs/UI/` | Runtime-loaded popup prefabs |
| `project-fill/Assets/Plugins/` | Platform native plugins |
| `project-fill/Assets/Scenes/` | Unity scenes |
| `project-fill/Assets/Audio/SFX/` | SFX WAV files (10 clips from Casual Game Sounds U6 + Free UI Click SFX Pack) |

## Rules
- NEVER edit `Assets/Resources/Data/` — source is `shared/datas/`, regenerate with `npm run gen:info`
- NEVER edit `Assets/Scripts/Data/Generated/` or `Assets/Scripts/Generated/` — regenerate with gen tools
- **UI Prefabs — NO DIRECT EDIT**: NEVER create or modify `.prefab` files manually. NEVER add UI objects directly in Scene files. Only define structure in `UIEditorSetup.cs`; generate from Unity Editor menu.

## Completion Gate (asset / data)
Evaluate before declaring client work complete. Output each result (`✓ applied` / `N/A`):

| # | Check | Trigger | Required Action |
|---|-------|---------|-----------------|
| 1 | new UI string | Any new string displayed in UI | Add to `shared/datas/string/client_string.csv` → FLAG `tools/info_generator.bat` (regenerates `StringIds.cs` that `UIEditorSetup.cs` references) **then** `tools/subset_fonts.bat` — in that order |
| 2 | dynamic image ref | New runtime-loaded sprite or atlas | Add entry to `shared/datas/common/dynamic_resource.csv` |
| 3 | new UI prefab | New popup, panel, or HUD element | Implement in `UIEditorSetup.cs` ONLY — no direct `.prefab` edit → FLAG(Unity) user runs the `Tools/UI Setup/Prefabs/X` menu item (agent cannot run Unity GUI) |

Code triggers (new contract, leaf AGENTS.md update) → see `Assets/Scripts/AGENTS.md` Completion Gate.

## Cross-refs
| type | refs |
|------|------|
| Depends on | `docs/refs/platform-auth.md` |
| Platform source | `platform:docs/refs/auth.md` |
