# docs/design

UI/UX specs, wireframes, game design documents.

## Files
| file | role |
|------|------|
| `economy-system-design.md` | Game economy spec: currency types, reward formulas, sink/source balance |
| `signal_sort_system_design_kr.md` | Signal Sort core gameplay rules, boosters, stuck mitigation flow, and system flow |
| `signal_sort_content_design_kr.md` | Signal Sort level design parameters, progression milestones, and gimmicks |
| `social-ranking-design.md` | Social and ranking spec: Redis-based global ranking |
| `progression-system-design.md` | Progression system spec: milestone rewards |
| `iap-badge-system-design.md` | IAP badge and purchase system spec |
| `ui-ux-config.md` | Global UI/UX conventions: palette, typography, touch targets, animation, safe area, z-order, pixel art scaling |
| `ui-ux-common-components.md` | Shared UI: ConfirmDialog, Toast, LoadingOverlay, RewardPopup, NetworkError, animation components, Settings panel |
| `ui-ux-canvas-architecture.md` | Canvas hierarchy, Sort Order, Canvas Scaler settings, SafeAreaHandler, UIManager API, responsive policy |
| `ui-ux-scene-structure.md` | Scene graph, transitions, overlay taxonomy, Lobby tab structure |
| `ui-ux-lobby.md` | Boot screen, Lobby layout, Home tab, Shop tab |
| `ui-ux-auth.md` | Boot auth sequence, Guest mode, OAuth link flow, account switching, clientLogin |
| `client-settings-system-design.md` | Client settings design spec: localized texts, sound levels, haptics, and local cache policies |
| `GEMINI.md` | SoT pointer to this file |

## Rules
- One file per major design area.
- Wireframe images: store in `design/assets/`.
- Do not duplicate content from ADRs; link to relevant `decisions/` entries instead.
- Signal Sort is the current game baseline. Do not reintroduce turn-limit failure, stamina, tube/bottle sort visuals, or generic non-circuit bundle art unless the `*_kr.md` Signal Sort specs change first.
