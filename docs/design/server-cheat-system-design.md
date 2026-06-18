# Server Cheat System Design (서버 치트 시스템 기획서)

Status: partial

> NOTE: synced to impl 2026-06-18 — Phase 0/1/1.5 (server) + Phase 2 (client overlay) + Phase 3
> (cosmetic/achievement/attendance) all implemented. §7 UI built per spec as a **`UIEditorSetup` prefab**
> (`UIEditorSetup.Cheat.cs::CreateCheatOverlay` → `Resources/Prefabs/UI/CheatOverlayView.prefab`,
> palette/Panel-3-layer/Btn-96px/TMP conventions) **dynamic-loaded** at runtime by
> `CheatOverlayView.Bootstrap` (`RuntimeInitializeOnLoadMethod`, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`).
> Overlay labels are raw dev English (TMP `stringId=null` → LocalizedText font-only), so §9's
> `client_string.csv` + `subset_fonts` step is NOT needed. Remaining open: ad-bypass client enforcement
> (§11).

개발/QA 편의를 위한 **서버 권위(server-authoritative) 치트 시스템**. 클라이언트는 입력 UI일 뿐이며, 모든 상태 변경은 서버가 검증·실행·기록한다. project-link의 치트 구조를 참고하되, project-fill의 아키텍처(stateless JWT, UIEditorSetup 컨벤션, NetworkService)에 맞게 보완한다.

---

## 1. 목표 / 비목표

### 목표
- dev 환경에서만 동작하는 서버 권위 치트 (골드/아이템/스테이지/튜토리얼/광고/코스메틱 등 상태 강제 변경).
- 백틱(`` ` ``) 키로 토글되는 인게임/아웃게임 공용 오버레이.
- 두 가지 입력 경로 제공:
  1. **커맨드 모드** — `InputField`에 `/gold set 99999` 식 명령어 입력.
  2. **버튼 모드(폴백)** — 명령어 타이핑이 번거로울 때 도메인별 프리셋 버튼 + 숫자 `InputField`로 즉시 실행.
- 모든 치트 실행을 `event_logs`에 감사(audit) 기록.

### 비목표
- 운영(prod) 환경 노출 금지. 운영용 어드민 콘솔은 별도 과제.
- 다른 유저(`TargetUserId`) 조작 — 본인 계정만 대상. (project-link의 레거시 ops 엔드포인트는 이식하지 않음.)
- 치트 전용 신규 게임플레이 로직 추가 — 기존 도메인 서비스 재사용이 원칙.

---

## 2. project-link 구조 분석 (참고)

| 레이어 | 파일 | 역할 |
|--------|------|------|
| Client | `Core/CheatCliOverlay.cs` | `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` 가드, `RuntimeInitializeOnLoadMethod`로 자동 생성, **런타임 코드로 UI 빌드**, Shift+F1 토글, `POST /api/admin/cheat/command` 호출 후 응답으로 로컬 캐시(`UserDataCache`/Lobby) 갱신 |
| API | `AdminCheatController.cs` | `[Authorize]` + `AdminWhitelistFilter`, `command` 엔드포인트가 파싱→`DispatchAsync`로 도메인 분기. 레거시 도메인별 ops 엔드포인트(TargetUserId) 병존 |
| API | `CheatCommandParser.cs` | `/domain action target value` 문자열 → `ParsedCheatCommand` 레코드. 도메인별 arity 검증 |
| API | `Filters/AdminWhitelistFilter.cs` | `AdminWhitelistCache`(DB 백업) 조회, 미등록 시 403 `FORBIDDEN` |
| App | `Cheat/CheatService.cs` | 실제 상태 변경. 기존 테이블 직접 조작 + `CurrencyLogs`/`InventoryLogs`/`EventLogs` 기록 |

**핵심 패턴(계승할 것):**
- 명령어 문법 통일 (`/domain action target value`).
- 응답 `data`에 변경 후 값(`balanceAfter`, `staminaAfter` …) 포함 → 클라가 재조회 없이 캐시 갱신.
- 모든 변경 `EventLogFactory.Cheat*`로 감사 기록.

**약점(보완 대상):**
- 게이팅이 **화이트리스트(DB)** 단일 의존 — 화이트리스트만 통과하면 prod에서도 실행 가능. 환경 게이트 없음.
- 토글이 Shift+F1 (조합키, 모바일/에디터에서 직관성 낮음).
- UI를 **런타임 코드로 빌드** → project-fill의 "UI는 UIEditorSetup에서만" 컨벤션과 충돌.
- 입력이 **커맨드 단일 경로** — 오타 시 `INVALID_COMMAND`, 도메인/문법 암기 필요.

---

## 3. 보완점 (project-fill 적용)

| # | project-link | project-fill 보완 |
|---|--------------|-------------------|
| 1 | 화이트리스트 단독 게이트 | **환경 게이트 1차(`GAME_ENV == dev`) + 설정 화이트리스트 2차**. prod에서는 컨트롤러가 아예 404. DB 테이블 불필요(config PID 목록) |
| 2 | Shift+F1 토글 | **백틱(`` ` ``) 단일키 토글** (New Input System `Keyboard.current.backquoteKey`) |
| 3 | 런타임 코드 UI | **`UIEditorSetup.CreateCheatOverlay()` 프리팹** 으로 빌드 (팔레트/폰트/Panel 3-layer/버튼 96px 컨벤션 준수). 런타임 스크립트는 `CheatOverlayView`만 |
| 4 | 커맨드 단일 입력 | **커맨드 모드 + 도메인 탭 버튼 모드** 병행. 버튼 모드는 숫자 `InputField` + 프리셋 액션 버튼으로 무타이핑 실행 |
| 5 | 레거시 TargetUserId 엔드포인트 | 제거. `command` 단일 엔드포인트만 (본인 `PlayerId` 고정) |
| 6 | `RuntimeInitializeOnLoad` 자동 생성 | Boot 시 `Debug.isDebugBuild \|\| Application.isEditor` 조건부 `UIManager` 등록(아래 8.3) |

---

## 4. 아키텍처

```
[Client]                                  [Server]
CheatOverlayView (`` ` `` toggle)
   │  커맨드 or 버튼 → command 문자열 조립
   ▼
NetworkService.Post("/api/dev/cheat/command", {command})
   │                                         ▼
   │                              DevCheatController  [Authorize]
   │                                ├─ DevOnlyGate (env != dev → 404)
   │                                ├─ CheatWhitelistFilter (PID 미등록 → 403)
   │                                ├─ CheatCommandParser.TryParse
   │                                └─ CheatDispatcher → CheatService.*
   │                                         │  (기존 도메인 서비스/리포지토리 재사용)
   │                                         ▼
   │                              CheatCommandResponse{success,message,data}
   ◄─────────────────────────────────────────┘
RefreshLocalState(prefix, data)  // 캐시/HUD 갱신
```

### 4.1 신규/변경 파일 매니페스트

**shared/contracts/Cheat/** (신규 도메인)
- `CheatCommandRequest.cs` — `{ string Command }`
- `CheatCommandResponse.cs` — `{ bool Success, string Command, string Message, object? Data }`

> contracts 변경 → `pkt_generator` 대상. 클라 `Generated/Contracts/`에 동기화.

**server/src/ProjectFill.API/**
- `Controllers/DevCheatController.cs` — `[Route("api/dev/cheat")]`, `command`(POST) + `docs`(GET HTML) 액션
- `Filters/CheatWhitelistFilter.cs` — 설정 기반 PID 화이트리스트 검사
- `Dev/CheatCommandParser.cs` — 문자열 → `ParsedCheatCommand`
- `Dev/CheatDispatcher.cs` — 도메인 분기 (project-link `DispatchAsync` 이식)
- `Dev/CheatCommandCatalog.cs` — **치트 명령어 단일 진실 공급원**(도메인/문법/설명/예시 메타). 파서·문서 페이지·클라 버튼 모드가 공유
- `Dev/CheatDocsPage.cs` — 카탈로그 → 정적 HTML 렌더(문자열 빌드, 외부 의존 없음)

**server/src/ProjectFill.Application/Cheat/**
- `CheatService.cs` — 도메인별 상태 변경 메서드 (기존 Currency/Inventory/Stage/Tutorial/Cosmetic 서비스·리포지토리 재사용)

**server/src/ProjectFill.API/ProjectFillConfiguration.cs** (변경)
- `Dev.Enabled` (`GAME_ENV == dev` 파생) / `Dev.CheatWhitelist`(PID 목록, 비면 dev에서 전체 허용)

**client/.../Scripts/OutGame/Dev/** (신규, `#if UNITY_EDITOR || DEVELOPMENT_BUILD`)
- `CheatOverlayView.cs` — 토글/입력/네트워크/로그/캐시 갱신
- `OutGame/Dev/DevEnums.cs` — `CheatDomain` enum (하드코딩 금지 컨벤션)

**client/.../Scripts/Editor/UIEditorSetup.Cheat.cs** (partial 추가)
- `CreateCheatOverlay()` — 프리팹 빌더 (`Tools/UI Setup/Prefabs/CheatOverlay`)

---

## 5. 명령어 스펙

문법: `/{domain} [target] [action] [value]` (project-link 문법 계승, 도메인은 project-fill에 맞게 재정의).

| 명령어 | 도메인 | 서버 동작 | 응답 data |
|--------|--------|-----------|-----------|
| `/gold add\|red\|set {amount}` | Currency | 소프트 골드 가감/설정 (Max 클램프, `CurrencyLogs`) | `{ balanceAfter }` |
| `/item {id\|all} add\|red\|set {amount}` | Inventory | 부스터 아이템 수량 조작 (`InventoryLogs`) | `{ inventoryAfter: {id:qty} }` |
| `/stage set {stageId}` | Stage | `stageId`까지 클리어 처리, 초과분 삭제, `max_cleared_stage_id` 갱신 | `{ highestStageAfter }` |
| `/tutorial {id\|all} true\|false` | Tutorial | 튜토리얼 완료 상태 set/clear | `{ seenTutorialIds }` |
| `/ad true\|false` | Ad | 광고 바이패스 토글 (Redis `cheat:ad:{uid}`) | `null` |
| `/cosmetic {id\|all} unlock\|lock` | Cosmetic | 스킨 해제/회수 | `{ unlockedCosmeticIds }` |
| `/achievement {id\|all} complete\|reset` | Achievement | 업적 진행 완료/초기화 | `{ achievementStateAfter }` |
| `/attendance setday {n}\|reset` | Attendance | 출석 사이클 일자 강제/초기화 | `{ attendanceDay }` |

> **단계적 도입**: Phase 1 = `gold/item/stage/tutorial/ad`(project-link 직접 대응). Phase 2 = `cosmetic/achievement/attendance`(project-fill 고유). 버튼 모드 탭도 동일 순서로 노출.

파싱 실패 → `400 INVALID_COMMAND`. 도메인 분기 실패 → `ArgumentException` → `400`.

---

## 6. 보안 / 게이팅 (다층 방어)

운영 노출은 치명적이므로 **3중 방어**. 하나라도 막히면 실행 불가.

1. **컴파일 가드 (클라)** — `CheatOverlayView`, `DevEnums`, `UIEditorSetup.Cheat`의 빌더 호출부를 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 감싼다. 릴리스 빌드 바이너리에서 코드 제거.
2. **환경 게이트 (서버 1차)** — `DevCheatController`는 `GAME_ENV != dev`이면 라우트 자체를 404 처리(`DevOnlyGate` 액션필터 또는 컨트롤러 진입 가드). prod 배포 시 엔드포인트 부재와 동일.
3. **화이트리스트 (서버 2차)** — `CheatWhitelistFilter`가 `Dev:CheatWhitelist`(플랫폼 PID 목록) 검사. 목록이 비어 있으면 dev 한정 전체 허용(로컬 편의), 값이 있으면 등록 PID만. 미등록 → 403 `FORBIDDEN`.

추가:
- 본인 계정만 대상 (`ControllerBaseEx.PlayerId` 고정, 요청 바디로 uid 수신 금지 — 서버 auth 컨벤션 준수).
- 모든 치트 → `event_logs` 기록 (correlationId 포함). 누가/언제/무엇을 변경했는지 추적.
- 화이트리스트는 **DB 테이블 대신 설정(env)** 으로 — `schema.json` 변경/마이그레이션 불필요(project-link 대비 단순화). dev 전용이므로 config로 충분.

---

## 7. UI 설계 (UIEditorSetup 컨벤션)

### 7.1 토글
- New Input System: `Keyboard.current.backquoteKey.wasPressedThisFrame` → 패널 `SetActive` 토글.
- 토글 시 인증 토큰(`NetworkService` 토큰) 없으면 무시(부트 인증 전 차단).
- 열릴 때 커맨드 `InputField.ActivateInputField()`.

### 7.2 프리팹 구조 (`CreateCheatOverlay()`)
모두 `UIEditorSetup` 헬퍼(`Panel`/`Btn`/`TMP`/`CloseBtnAt`)로 빌드 — 팔레트·폰트·3-layer·96px·AutoFontSize 컨벤션 자동 준수.

```
CheatOverlayCanvas (Sort Order 100, ApplyCanvasScaler)
└─ Panel (UI_BG_DEEP, 3-layer)               ← UIPanelAppear + CanvasGroup (필수)
   ├─ Header: "CHEAT [`]" (TMP Header) + CloseBtn(96)
   ├─ ModeTabs (HLG): [Command] [Buttons]    ← UI_PRIMARY 탭
   ├─ LogArea (RectMask2D, BottomLeft TMP)    ← 최근 N줄, richText
   ├─ CommandRow (HLG)                        ← Command 모드
   │   ├─ InputField (flexibleWidth)          ← "/gold set 99999"
   │   └─ SendBtn (UI_SUCCESS, "Send")
   └─ ButtonPanel (VLG)                       ← Buttons 모드(폴백)
       ├─ DomainTabs (HLG): Gold/Item/Stage/Tutorial/Ad…
       └─ ActionArea (도메인별 전환)
           예) Gold 탭:
             AmountInput (InputField, 숫자)
             [Add] [Reduce] [Set]  ← 누르면 `/gold {action} {amount}` 조립·전송
```

- **버튼 모드 = 폴백 핵심**: 도메인 탭 선택 → 숫자 `InputField` 1개 + 프리셋 액션 버튼. 버튼 클릭이 명령어 문자열을 조립해 커맨드 모드와 동일 경로로 전송 → 서버/파서는 입력 경로를 구분하지 않음(단일 진실 공급원).
- 자주 쓰는 값은 퀵버튼 제공(예: Gold `[+1만] [+10만] [Max]`, Stage `[+1] [최종]`).
- 모든 버튼 `Btn()` 사용 → 96px 최소·CTA 애니메이션·팔레트 자동.

### 7.3 생성/등록
- `Tools/UI Setup/Prefabs/CheatOverlay` 메뉴(에디터)에서 프리팹 생성 → `Resources/Prefabs/UI/CheatOverlayView.prefab`.
- Boot에서 `if (Debug.isDebugBuild || Application.isEditor)` 일 때만 `UIManager`에 오버레이 인스턴스 등록. 릴리스에서는 미등록 → 백틱 무반응.

### 7.4 응답 후 로컬 갱신
project-link `RefreshCaches` 패턴 이식. 응답 `data`의 변경 후 값으로 `UserDataCache`/Lobby HUD 직접 갱신, 필요 시 도메인 GET 재조회. prefix별 분기(`/gold`→잔액, `/stage`→진행도+로비 새로고침 등).

---

## 8. 치트 문서 정적 페이지

치트 명령어를 브라우저에서 확인하는 **서버 렌더 정적 페이지**. 단, 정적파일 미들웨어로 그냥 노출하지 않고 **반드시 미들웨어 검증 파이프라인을 통과**해야 한다(요구사항).

### 8.1 미들웨어 검증 (핵심 제약)
project-fill 미들웨어 순서: `CorrelationId → ApiException → Auth → UserIdResolution → Authorization → RateLimit → VersionCheck → Controllers`.

- 정적 페이지를 `UseStaticFiles`/`wwwroot`로 서빙하면 **인증 미들웨어를 우회**한다 → **금지**.
- 대신 **MVC 컨트롤러 액션**(`GET /api/dev/cheat/docs`)이 `text/html`을 반환 → 위 파이프라인을 그대로 통과.
- 적용 게이트(치트 실행 엔드포인트와 동일 다층 방어):
  1. `DevOnlyGate` — `GAME_ENV != dev` → 404 (prod 노출 차단)
  2. `[Authorize]` (Auth 미들웨어) — 유효 JWT 필수
  3. `CheatWhitelistFilter` — 미등록 PID → 403
- 즉 문서 페이지도 치트 실행과 **동일한 권한**이 있어야만 열람 가능. 권한 없는 외부에 명령어 카탈로그가 새지 않는다.

### 8.2 단일 진실 공급원 (drift 방지)
project-link는 명령어 문법이 **파서 주석**에만 존재 → 코드와 문서가 어긋날 위험. 보완:

- `CheatCommandCatalog` 가 도메인·문법·설명·예시를 **메타 데이터로 보유**.
- 소비처 3곳이 같은 카탈로그 참조:
  1. `CheatCommandParser` — arity/도메인 검증
  2. `CheatDocsPage` — HTML 렌더 (표: 명령어 / 설명 / 예시 / 응답)
  3. 클라 버튼 모드 — 도메인 탭/프리셋 구성 근거(클라에는 동일 메타를 contracts 또는 `docs` 응답으로 전달, 또는 클라 상수와 동기 — 구현 시 택일)
- 명령어 추가 = 카탈로그 1곳 수정 → 파서·문서·버튼이 함께 갱신. 문서가 구현과 절대 어긋나지 않음.

### 8.3 페이지 내용 / 형태
- 외부 의존 없는 **인라인 HTML/CSS 문자열**(서버가 카탈로그 순회해 빌드). 빌드 산출물·노드 패키지 불필요.
- 구성: 헤더(현재 `GAME_ENV`/뷰어 PID) → 게이팅 설명 → 도메인별 명령어 표(문법·설명·값 범위·예시·응답 `data`) → 버튼 모드 사용법 → 백틱 토글 안내.
- 클라 오버레이 헤더의 `?`(Docs) 버튼 → 인증된 `NetworkService`로 `GET /api/dev/cheat/docs` 호출, 받은 HTML을 디바이스 기본 브라우저/웹뷰로 표시(또는 dev PC에서 직접 URL 열람).

### 8.4 브라우저 직접 열람 시 인증 전달
미들웨어 `[Authorize]`는 `Authorization: Bearer` 헤더를 요구 → 브라우저 주소창 직접 접근은 토큰 없음.
- **QUESTION: 문서 페이지 토큰 전달** | OPTIONS: A) 클라 오버레이가 인증 호출로 HTML 받아 웹뷰 표시(헤더 토큰 자동) B) dev 한정 `?token=` 쿼리 허용(GET 문서에 한해) | RECOMMEND: **A** — 미들웨어 우회/토큰 노출 없이 기존 인증 경로 재사용. B는 dev에서 URL 공유 편의가 필요할 때만 추가.

---

## 9. 데이터 / 스키마 영향

| 항목 | 영향 |
|------|------|
| `schema.json` (DB) | **변경 없음** — 화이트리스트는 config, 치트는 기존 테이블 재사용 |
| `shared/contracts/Cheat/` | 신규 DTO 2종 → `pkt_generator` |
| `shared/datas/string/client_string.csv` | 오버레이 라벨 문자열 추가 → `info_generator` + `subset_fonts` |
| `dynamic_resource.csv` | 신규 스프라이트 불필요(텍스트/팔레트 색만 사용) |
| `event_logs` | `CheatGold/CheatItem/CheatStage/…` 로그 타입 추가(`EventLogIds`/`EventLogFactory`) |

---

## 10. 단계별 구현 계획

**Phase 0 — 기반**
1. `shared/contracts/Cheat/` DTO → `pkt_generator`
2. `ProjectFillConfiguration`에 `Dev.Enabled`/`CheatWhitelist` 추가
3. `CheatCommandCatalog`(단일 진실 공급원) 정의

**Phase 1 — 코어 치트 (project-link 대응)**
4. `CheatService`(gold/item/stage/tutorial/ad) + `CheatCommandParser`(카탈로그 참조) + `CheatDispatcher`
5. `DevCheatController.command` + `DevOnlyGate` + `CheatWhitelistFilter`
6. `event_logs` 치트 로그
7. 서버 테스트: 파서 단위 / 환경게이트 404 / 비화이트리스트 403 / 각 도메인 동작

**Phase 1.5 — 문서 정적 페이지**
8. `CheatDocsPage`(카탈로그 → HTML) + `DevCheatController.docs`(GET, 동일 게이트 통과)
9. 테스트: prod 404 / 비인증 401 / 비화이트리스트 403 / dev+화이트리스트 200 `text/html`

**Phase 2 — UI** ✅ (UIEditorSetup prefab + dynamic load — see top NOTE)
10. `UIEditorSetup.Cheat.cs`의 `CreateCheatOverlay()`(헤더 Docs 버튼 포함) → `Resources/Prefabs/UI/CheatOverlayView.prefab`. FLAG(Unity) `Tools/UI Setup/Prefabs/CheatOverlay`.
11. `OutGame/Dev/CheatOverlayView.cs`(프리팹 바인드: 백틱 토글/커맨드+버튼 모드/로그/프리픽스 캐시갱신/Docs 호출; `RuntimeInitializeOnLoadMethod`로 프리팹 동적 로드) + `OutGame/Dev/DevEnums.cs`
12. ~~문자열 → info_generator + subset_fonts~~ → 불필요(라벨 인라인 영문, TMP stringId=null → LocalizedText font-only)

**Phase 3 — project-fill 고유 도메인** ✅
13. `cosmetic/achievement/attendance` 치트 + 카탈로그/버튼 탭 확장 (`CheatService.{Cosmetic,Achievement,Attendance}Async`, 카탈로그 3행, 디스패처 3분기, `event_logs` 9006–9008)

각 단계 완료 시 해당 레이어 `AGENTS.md`(Files/Symbols) 갱신.

---

## 11. 미해결 / 확인 필요

- **QUESTION: 화이트리스트 게이트 강도** | OPTIONS: A) env 게이트만(dev면 누구나) B) env + PID 화이트리스트(비면 전체 허용) | RECOMMEND: **B** — 기본은 로컬 편의(전체 허용), 공유 dev 서버에선 PID 등록으로 잠금. DB 없이 config로 토글.
- 버튼 모드 도메인 탭의 1차 노출 범위: Phase 1 도메인(gold/item/stage/tutorial/ad)부터, Phase 3에서 확장.
- 광고 바이패스의 클라 측 처리(`AdBypassEnabled` 플래그)는 project-fill 광고 서비스 구조 확인 후 연동(AdController/AdService 매핑).
- 문서 페이지 브라우저 토큰 전달 방식(§8.4): 기본 A(오버레이 인증 호출), URL 직접 공유 필요 시 B 추가.
