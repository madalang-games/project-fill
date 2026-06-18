# Scripts/OutGame — Non-Gameplay Scenes

## Nav
| path | role |
|------|------|
| `Boot/` | Boot scene: auth sequence, splash screen | → `Boot/AGENTS.md` |
| `Lobby/` | Lobby scene: chapter/stage scroll, tabs, header | → `Lobby/AGENTS.md` |
| `Settings/` | Settings panel, account popup | → `Settings/AGENTS.md` |
| `Dev/` | DEV-ONLY backtick cheat overlay (UIEditorSetup prefab, dynamic-loaded; `#if` compiled out of release) | → `Dev/AGENTS.md` |

## Rules
- Namespace mirrors path: `Game.OutGame.Boot`, `Game.OutGame.Lobby`, `Game.OutGame.Settings`
- MonoBehaviour suffix: `View` (e.g. `LobbyView`, `HeaderView`)
