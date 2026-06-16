# Scripts — Client Code Root

C# code root. Code-level Nav + conventions. Asset/data rules live in `client/AGENTS.md`.

## Nav
| path | role |
|------|------|
| `Core/` | App lifecycle, singletons, UIManager, SceneTransition, GameConfig | → `Core/AGENTS.md` |
| `Core/UI/` | Animation components, common popup views | → `Core/UI/AGENTS.md` |
| `InGame/` | Signal Sort gameplay domain (Board, SlotLane, SignalChip, Booster) | → `InGame/AGENTS.md` |
| `OutGame/` | Non-gameplay scenes (Boot, Lobby, Settings) | → `OutGame/AGENTS.md` |
| `Services/` | DDOL services: StageData, Auth, PlayerProgress, Localization | → `Services/AGENTS.md` |
| `Localization/` | FontLocalizationConfig ScriptableObject | → `Localization/AGENTS.md` |
| `Utils/` | Stateless helpers | → `Utils/AGENTS.md` |
| `Editor/` | Unity Editor-only automation tools | → `Editor/AGENTS.md` |
| `Data/Generated/` | Auto-generated C# models — DO NOT EDIT |
| `Generated/Contracts/` | Auto-synced from `shared/contracts/` via pkt_generator — DO NOT EDIT |

## Conventions
- Namespace mirrors folder path: `Game.InGame.Board`, `Game.OutGame.UI`, etc.
- MonoBehaviour suffix: `View` (e.g. `BoardView`, `NodeView`)
- Pure data/logic classes: no suffix (e.g. `Board`, `SignalChip`)
- Input: New Input System via `InputSystem_Actions.inputactions`

## Enum Policy
NEVER define enum inside a class body or use magic number/string for branching.

| Scope | File location |
|-------|--------------|
| Server + Client shared | `shared/contracts/GameTypes/GameEnums.cs` (synced via pkt_generator) |
| Client-internal (domain-specific) | `[DomainFolder]/[Domain]Enums.cs` (e.g. `InGame/InGameEnums.cs`) |
| Client-internal (cross-domain) | `Core/CoreEnums.cs` |

Decision: if value appears in a contract DTO or must match server → shared. If only client rendering/logic uses it → client-internal.

## Completion Gate (code)
Evaluate before declaring code work complete. Output each result (`✓ applied` / `N/A`):

| # | Check | Trigger | Required Action |
|---|-------|---------|-----------------|
| 1 | new contract used | New contract type referenced in client | Verify exists in `shared/contracts/`; if missing → expand task scope |
| 2 | AGENTS.md | New file, class, or symbol added | Update `## Files` and `## Symbols` in affected leaf `AGENTS.md` |

Asset/data triggers (UI string, dynamic image, UI prefab) → see `client/AGENTS.md` Completion Gate.
