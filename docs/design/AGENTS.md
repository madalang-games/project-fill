# docs/design

UI/UX specs, wireframes, game design documents.

## Files
| file | role |
|------|------|
| `economy-system-design.md` | Game economy spec: currency types, reward formulas, sink/source balance (부스터 + 코스메틱 싱크, 데일리 소득원 포함) |
| `signal_sort_system_design_kr.md` | Signal Sort core gameplay rules, boosters (Add Lane/Shuffle/Undo), stuck mitigation flow, Soft Stuck detection, and system flow |
| `signal_sort_content_design_kr.md` | Signal Sort level design parameters, chapter progression (5×20 stages), milestone rewards, economy table, gimmick intro schedule |
| `signal_sort_gimmick_design_kr.md` | Signal Sort gimmick specs: Locked Lane, Blind Lane, Relay Node, Overload Chip, booster interaction matrix |
| `social-ranking-design.md` | Social and ranking spec: Weekly ranking, All-Time global ranking, Redis-based cache (Daily Challenge tab removed) |
| `progression-system-design.md` | Progression system spec: milestone rewards |
| `iap-badge-system-design.md` | IAP badge and purchase system spec; lobby badge layout (출석·주간미션·이벤트 배지 포함) |
| `weekly-mission-event-design.md` | Weekly Mission Event (데일리 챌린지 대체, "패스" 워딩 미사용): 주간 회전 미션 5종(캠페인 행동 집계, 신규 퍼즐 생성 불필요) → EP 리워드 트랙(골드/소비재), 코스메틱 이관(출석/업적), DB/API. `Status: implemented` |
| `achievement-system-design.md` | Achievement system: 4-tier (Bronze/Silver/Gold/Platinum), 4 categories, rewards (gold/avatar/cosmetic), DB schema |
| `daily-login-design.md` | Daily login attendance: 7-day cycle, non-punitive streak policy, reward table, long-term milestone bonuses |
| `cosmetic-system-design.md` | Cosmetic system: Chip Skin / Lane Skin / Board Skin, gold 구매 + 업적/출석 무료 해제, 32,700골드 장기 싱크, BoardTheme/ChipFinish 렌더 모델 + Shop 서브탭 |
| `art-style-guide.md` | Art style 가이드. **SoT = `UIEditorSetup.cs` UI 스타일 + `UIColorPalette.cs` 팔레트("Dark Neon Puzzle")**. 팔레트 토큰 hex, 픽셀 패널/드롭섀도 형태 언어, 네온 Signal 액센트, 카테고리별 규칙, Unity 임포트(Bilinear/PPU100), 네이밍 prefix, 신규 아이콘/이미지 생성 체크리스트. 색 추가·변경은 UIColorPalette.cs에서 |
| `ui-ux-config.md` | Global UI/UX conventions: palette, typography, touch targets, animation, safe area, z-order, pixel art scaling |
| `ui-ux-common-components.md` | Shared UI: ConfirmDialog, Toast, LoadingOverlay, RewardPopup, NetworkError, animation components, Settings panel |
| `ui-ux-canvas-architecture.md` | Canvas hierarchy, Sort Order, Canvas Scaler settings, SafeAreaHandler, UIManager API, responsive policy |
| `ui-ux-scene-structure.md` | Scene graph, transitions, overlay taxonomy, Lobby tab structure |
| `ui-ux-lobby.md` | Boot screen, Lobby layout, Home tab, Shop tab |
| `ui-ux-auth.md` | Boot auth sequence, Guest mode, OAuth link flow, account switching, clientLogin |
| `client-settings-system-design.md` | Client settings design spec: localized texts, sound levels, haptics, and local cache policies |
| `server-cheat-system-design.md` | Server-authoritative dev cheat system: backtick-toggle overlay (UIEditorSetup), command + button-fallback input, `/api/dev/cheat/command`, middleware-gated docs static page `GET /api/dev/cheat/docs` (CheatCommandCatalog single-source-of-truth), 3-layer gating (compile guard + env + PID whitelist). `Status: planned` |
| `GEMINI.md` | SoT pointer to this file |

## Rules
- One file per major design area.
- Wireframe images: store in `design/assets/`.
- Do not duplicate content from ADRs; link to relevant `decisions/` entries instead.
- Signal Sort is the current game baseline. Do not reintroduce turn-limit failure, stamina, or tube/bottle sort visuals unless the `*_kr.md` Signal Sort specs change first. Art direction is **color-token + pixel/casual** (circuit motif is a Blind-back accent only; Signal Panel lights as bulb/star nodes) per `signal_sort_system_design_kr.md` §1 and §9.
- Cosmetics are purely visual — never introduce pay-to-win or gameplay-affecting purchases.
- Recurring engagement systems (login, weekly mission pass) must preserve the relaxing identity: no punitive streak/expiry loss that blocks progress.

## Design-Doc Sync Policy (lazy, reconcile-on-read)
Design docs (기획서) are **intent/spec**, NOT behavior authority. Authority by feature state:
- **Implemented / partial** feature: code + its layer `AGENTS.md` symbol map win. If a doc section conflicts → update the doc to match impl, append `NOTE: synced to impl <date>`.
- **Planned** (not-yet-built) feature: the design doc wins — it is the build target. NEVER overwrite a planned section with current code.
Direction marker — tag major sections with `Status: implemented | partial | planned`. Absent marker ⇒ treat as `implemented` (reconcile against code). Status source of record = `TODO-List/`.
Lazy: reconcile ONLY when a doc is read as input for a new task — do not sweep-sync docs proactively. Markers are added going forward (new/edited sections); no retroactive backfill.
