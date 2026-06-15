# client - Unity 6 (URP 2D)

## Stack
Unity 6 | URP 2D | C# | New Input System

## Nav
| path | role |
|------|------|
| `project-fill/Assets/Scripts/Core/` | App lifecycle, singletons, UIManager, SceneTransition, GameConfig | → `Core/AGENTS.md` |
| `project-fill/Assets/Scripts/Core/UI/` | Animation components, common popup views | → `Core/UI/AGENTS.md` |
| `project-fill/Assets/Scripts/InGame/` | Signal Sort gameplay domain (Board, SlotLane, SignalChip, Booster) | → `InGame/AGENTS.md` |
| `project-fill/Assets/Scripts/OutGame/` | Non-gameplay scenes (Boot, Lobby, Settings) | → `OutGame/AGENTS.md` |
| `project-fill/Assets/Scripts/Services/` | DDOL services: StageData, Auth, PlayerProgress, Localization | → `Services/AGENTS.md` |
| `project-fill/Assets/Scripts/Localization/` | FontLocalizationConfig ScriptableObject | → `Localization/AGENTS.md` |
| `project-fill/Assets/Scripts/Data/Generated/` | Auto-generated C# models — DO NOT EDIT |
| `project-fill/Assets/Scripts/Generated/Contracts/` | Auto-synced from `shared/contracts/` via pkt_generator |
| `project-fill/Assets/Scripts/Utils/` | Stateless helpers |
| `project-fill/Assets/Scripts/Editor/` | Unity Editor-only automation tools |
| `project-fill/Assets/Prefabs/` | Runtime prefabs |
| `project-fill/Assets/Sprites/PlayerSettings/` | PlayerSettings splash/background/logo/icon source sprites |
| `project-fill/Assets/Resources/Data/` | Runtime CSVs — generated, DO NOT EDIT |
| `project-fill/Assets/Resources/Prefabs/UI/` | Runtime-loaded popup prefabs |
| `project-fill/Assets/Plugins/` | Platform native plugins |
| `project-fill/Assets/Scenes/` | Unity scenes |
| `project-fill/Assets/Audio/SFX/` | SFX WAV files (10 clips from Casual Game Sounds U6 + Free UI Click SFX Pack) |

## Rules
- NEVER edit `Assets/Resources/Data/` — source is `shared/datas/`, regenerate with `npm run gen:info`
- NEVER edit `Assets/Scripts/Data/Generated/` or `Assets/Scripts/Generated/` — regenerate with gen tools
- **UI Prefabs**: Create required prefabs in accordance with the setup in [UIEditorSetup.cs](project-fill/Assets/Scripts/Editor/UIEditorSetup.cs).

## Conventions
- Namespace mirrors folder path: `Game.InGame.Board`, `Game.OutGame.UI`, etc.
- MonoBehaviour suffix: `View` (e.g. `BoardView`, `NodeView`)
- Pure data/logic classes: no suffix (e.g. `Board`, `SignalChip`)
- Input: New Input System via `InputSystem_Actions.inputactions`

## Cross-refs
| type | refs |
|------|------|
| Depends on | `docs/refs/platform-auth.md` |
| Platform source | `platform:docs/refs/auth.md` |
