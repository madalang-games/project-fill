# docs/design

UI/UX specs, wireframes, game design documents.

## Files
| file | role |
|------|------|
| `economy-system-design.md` | Game economy spec: currency types, reward formulas, sink/source balance (부스터 + 코스메틱 싱크, 데일리 소득원 포함) |
| `signal_sort_system_design_kr.md` | Signal Sort core gameplay rules, boosters (Add Lane/Shuffle/Undo), stuck mitigation flow, Soft Stuck detection, and system flow |
| `signal_sort_content_design_kr.md` | Signal Sort level design parameters, chapter progression (5×20 stages), milestone rewards, economy table, gimmick intro schedule |
| `signal_sort_gimmick_design_kr.md` | Signal Sort gimmick specs: Locked Lane, Blind Lane, Relay Node, Overload Chip, booster interaction matrix |
| `social-ranking-design.md` | Social and ranking spec: Daily Challenge ranking, Weekly ranking, All-Time global ranking, Redis-based cache |
| `progression-system-design.md` | Progression system spec: milestone rewards |
| `iap-badge-system-design.md` | IAP badge and purchase system spec; lobby badge layout (출석·챌린지·이벤트 배지 포함) |
| `daily-challenge-design.md` | Daily Challenge system: 전 세계 동일 퍼즐, 24시간 윈도우, 이동 횟수 글로벌 랭킹, 스트릭 보상, API |
| `achievement-system-design.md` | Achievement system: 4-tier (Bronze/Silver/Gold/Platinum), 4 categories, rewards (gold/avatar/cosmetic), DB schema |
| `daily-login-design.md` | Daily login attendance: 7-day cycle, non-punitive streak policy, reward table, long-term milestone bonuses |
| `cosmetic-system-design.md` | Cosmetic system: Chip Skin / Lane Skin / Board Skin, gold 구매 + 업적/챌린지 무료 해제, 24,200골드 장기 싱크 |
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
- Cosmetics are purely visual — never introduce pay-to-win or gameplay-affecting purchases.
- Daily systems (login, challenge) must preserve the relaxing identity: no punitive streak loss that blocks progress.
