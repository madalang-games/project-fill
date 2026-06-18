# Signal Sort 시스템 기획서 (Signal Sort System Design)

본 문서는 `project-fill`의 핵심 게임 플레이인 **Signal Sort (시그널 소트)**에 대한 시스템 기획서입니다. 플레이어가 흩어진 컬러 신호 칩을 슬롯 레인에 정렬하여 상단의 시그널 패널을 복구하는 핵심 규칙, 부스터 사양, 교착 상태 구제 로직을 다룹니다.

---

## 1. 개요 및 설계 원칙

- **한 줄 콘셉트**: 흩어진 컬러 신호 칩을 슬롯 레인에 정렬하여 상단의 Signal Panel을 복구하는 미니멀 코드 퍼즐.
- **비주얼 톤앤매너**: 튜브나 물병 같은 전통적인 오브젝트를 배제하고, **둥근 컬러 토큰 칩**(굵은 외곽선·파스텔 팔레트)을 중심으로 한 **픽셀 + 캐주얼** 아트 스타일을 따릅니다. 회로/기판 모티프는 전면 테마가 아니라 **Blind 레인 뒷면 등 액센트**로만 사용하고, 상단 Signal Panel은 노드가 전구/별처럼 점등되는 따뜻한 게이지로 표현합니다.
- **핵심 판타지**: 고장 나거나 꼬인 시스템의 컬러 신호를 정돈해 나가며 상단 패널의 노드와 라인이 하나씩 점등되는 릴랙싱한 기계 복구 감각.

---

## 2. 용어 정의

| 용어 | 정의 |
| :--- | :--- |
| **Signal Chip (칩)** | 유저가 이동시키는 기본 퍼즐 피스. 라운드 사각형 모양이며, 색상과 고유 신호 타입 심볼/패턴이 표기됩니다. |
| **Slot Lane (레인)** | 칩이 수직으로 쌓이는 컨테이너. 물리적인 튜브가 아니라 카드 랙이나 회로 슬롯처럼 표현됩니다. |
| **Capacity (수용량)** | 한 레인이 가질 수 있는 최대 칩 개수. 기본값은 4개입니다. |
| **Top Chip (맨 위 칩)** | 레인의 가장 위에 위치한 칩으로, 현재 조작을 통해 이동할 수 있는 유일한 대상입니다. |
| **Complete Set (세트 완성)** | 한 레인이 동일한 Signal Type의 칩 4개(Capacity)로 완전히 채워진 상태입니다. |
| **Assist (부스터)** | 플레이 중 위기 탈출이나 편의를 돕는 아이템 및 기능. `Add Lane`, `Shuffle`, `Undo` 3종이 존재합니다. |

---

## 3. 기본 규칙 (Gameplay Rules)

- **A-R01 (선택 규칙)**: 유저는 레인의 가장 위에 있는 `Top Chip`만 선택하여 집어 올릴 수 있습니다.
- **A-R02 (이동 규칙)**: 선택한 칩은 **비어 있는 레인** 또는 **맨 위의 칩이 동일한 Signal Type인 레인**으로만 이동할 수 있습니다.
- **A-R03 (수용량 규칙)**: 각 레인의 `Capacity`는 기본 4이며, 이를 초과하여 칩을 배치할 수 없습니다.
- **A-R04 (완성 규칙)**: 한 레인이 동일한 Signal Type 칩 4개로 채워지면 `Complete Set`이 됩니다.
- **A-R05 (완성 연출)**: Complete Set이 된 레인의 칩들은 상단 Signal Panel로 흡수되는 연출이 재생되며, **완성된 레인은 빈 상태로 전환되어 다시 칩을 수용할 수 있게 됩니다.** 레인 자체는 제거되지 않습니다.
- **A-R06 (클리어 조건)**: 스테이지에 존재하는 모든 Signal Type 칩을 Complete Set으로 정렬하여 상단 Panel에 모두 등록하면 스테이지를 클리어합니다.
- **A-R07 (예외 처리)**: 룰에 어긋나는 이동을 시도할 시 이동이 수행되지 않고, 칩의 미세한 흔들림(Shake) 및 경고색 테두리 연출을 통해 실패 피드백을 즉각 제공합니다.
- **A-R08 (동일 색상 일괄 이동)**: `Top Chip`을 기준으로 **위에서부터 연속으로 쌓인 동일 Signal Type 칩들은 한 번의 조작으로 함께 이동**합니다. 이동량은 목적지 레인의 잔여 `Capacity`만큼으로 제한되며(부족하면 들어갈 수 있는 만큼만 이동), 이동 가능 여부는 `Top Chip` 기준 A-R02를 따릅니다. 배치 순서는 **스택 pour**를 따릅니다 — 맨 위 칩이 먼저 떨어져 목적지의 가장 아래에 깔리고 그 위로 차례로 쌓입니다. 연출은 각 칩이 **목적지 슬롯 바로 위에서 생성(materialize)되어 중력 가속으로 낙하**하며(소스→목적지 횡단 없이 "워프 아웃" 후 낙하), 여러 칩은 아래부터 살짝 시차를 두고 캐스케이드합니다. **일괄 이동은 이동 횟수(Moves) 1회로 집계**되고, `Undo` 1회로 일괄 이동 전체를 되돌립니다.
- **A-R09 (초기 배치 규칙)**: 스테이지 생성 시 **어떤 레인도 이미 `Complete Set`(동일 타입 4개) 상태로 시작하지 않습니다.** 무료 완성(freebie)을 방지하기 위해 생성기는 완성 세트가 없는 배치만 채택합니다.

---

## 4. 부스터 시스템 (3 Booster Types)

플레이어는 아래 3종류의 부스터를 사용할 수 있습니다. 인게임 하단 UI 또는 스테이지 진입 전에 사용할 수 있습니다.

### 1) Add Lane (레인 추가)
- **효과**: 현재 스테이지에 빈 `Slot Lane`을 1개 추가로 생성합니다.
- **제한**: 스테이지당 **최대 1회**만 사용 가능합니다.
- **비용**:
  - 일반 구매: **500 골드**
  - Stuck 팝업 광고 보상: 무료 (1회/스테이지 한정, Add Lane 미사용 시에만 노출)

### 2) Shuffle (셔플)
- **효과**: 현재 보드 상태를 **풀이 가능이 보장된 새 초기 배치로 재생성**합니다. 역방향 경로 재생성(Reverse-Path Re-deal) 방식으로 동작하여 새 배치의 풀이 가능성이 항상 보장됩니다.
- **제한**: 사용 횟수 제한 없음.
- **비용**: **300 골드**

### 3) Undo (되돌리기)
- **효과**: **직전 1수**만 취소하고 이전 상태로 되돌립니다. 한 번 되돌린 뒤에는 새 이동을 하기 전까지 추가 Undo가 불가합니다(연속 되돌리기 불가).
- **제한**: 가장 최근 액션 1개의 스냅샷만 보관합니다. Undo 사용 또는 직전 이동이 없으면 버튼이 비활성화됩니다(`Board.CanUndo`).
- **비용**: **무료** (1수 한정, 인벤토리·골드 비용 없음)
<!-- NOTE: synced to impl 2026-06-18 — 무제한 연속 Undo는 Stuck/광고 funnel과 부스터 골드 싱크를 무력화하므로 1수(단일 스냅샷)로 변경. Board.cs `_lastSnapshot`. -->

---

## 5. 교착 상태 (Stuck State) 및 구제 흐름

스태미나 및 이동 횟수 제한이 없으므로, 스테이지 실패는 주로 **더 이상 어떤 칩도 이동할 수 없는 교착 상태(Stuck)**에 봉착했을 때 발생합니다.

### 5.1. Hard Stuck 자동 감지
- 모든 레인의 `Top Chip`들이 어떠한 유효한 이동(빈 레인 이동 포함)도 불가능한 상태를 **Hard Stuck**으로 정의합니다.
- Hard Stuck 감지 시 즉시 조작을 중단시키고 Stuck 팝업을 표시합니다.

### 5.2. Soft Stuck 감지 (Pre-Stuck)
- 이동은 가능하지만 모든 경로가 Hard Stuck으로 수렴하는 상태를 **Soft Stuck**으로 정의합니다.
- 솔버(Solver)가 현재 보드 상태에서 풀이 경로를 찾지 못할 경우 Soft Stuck으로 판정합니다.
- **UX 표현**: Hard Stuck 팝업 없이, `Shuffle` 버튼 Pulse 애니메이션 + 보드 전체 알파 0.85 Dim 효과로 비침습적 유도합니다. 플레이어는 무시하고 계속 진행할 수 있습니다.

### 5.3. Stuck 팝업 UI

**Add Lane 미사용 상태 (기본):**
```
┌──────────────────────────────────────────┐
│    [글리치 신호 아이콘 — 회로 단선 모티프]  │
│    SIGNAL BLOCKED                         │  ← 픽셀 폰트, 앰버색
│    더 이상 이동 가능한 신호 칩이 없습니다   │  ← 소형, 회색
│  ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─   │  ← 회로선 구분자
│                                            │
│  ┌────────────────────────────────────┐   │
│  │  📺  ADD LANE 획득                 │   │  ← 1순위: 강조 버튼 (민트/청록)
│  │       광고 시청 (무료)              │   │
│  └────────────────────────────────────┘   │
│                                            │
│  ┌────────────────────────────────────┐   │
│  │  🔀  SHUFFLE 실행      🪙 300      │   │  ← 2순위: 보조 버튼 (다크 테두리)
│  └────────────────────────────────────┘   │
│                                            │
│     [ 스테이지 포기하고 나가기 ]             │  ← 3순위: 텍스트 버튼 (소형, 회색)
└──────────────────────────────────────────┘
```

**Add Lane 이미 사용한 상태:** 광고 버튼 비노출, Shuffle 버튼이 1순위로 이동.

### 5.4. 광고 정책
- 보상형 광고(Add Lane) 시청 완료 시 `ad_rewarded_this_stage = true` 플래그 설정.
- 해당 스테이지 내 인터스티셜(전면) 광고 노출 차단.
- 스테이지 이탈 또는 다음 스테이지 진입 시 플래그 해제.

### 5.6. 결과 화면 보상 2배 (DoubleReward)
- 스테이지 클리어 결과 화면에서 보상형 광고(`DOUBLE_REWARD_STAGE_CLEAR`) 시청 시 획득 보상을 2배로 지급.
- **첫 클리어 한정**: 재클리어는 보상을 지급하지 않으므로 2배 버튼을 노출하지 않음.
- **스테이지당 1회**: 서버가 `double_reward:{stage_id}` 클레임 상태로 멱등 보장(중복 지급 차단).
- **서버 권위 + 광고 검증**: 클라가 광고 토큰을 서버에 제출, SSV(`AD_REWARD_VERIFY_MODE`) 검증 통과 시에만 `stage.reward_group_id`를 한 번 더 지급(순효과 2배).
- 결과 화면은 획득 보상 목록을 명시 표시하고, 2배 적용 후 수량을 갱신하며 버튼을 비활성화.

### 5.5. 포기하기
- 구제를 거부하고 로비로 돌아가거나 스테이지를 처음부터 재시작할 수 있습니다.

---

## 6. 이동 횟수 (Moves) 기록

- 스테이지 내 총 이동 횟수는 실시간으로 HUD에 표시됩니다.
- 동일 색상 일괄 이동(A-R08)은 칩 개수와 무관하게 **1회**로 집계됩니다.
- 클리어 시 `latest_moves` 및 `best_moves`가 서버에 기록됩니다.
- 이동 횟수는 **스테이지별 랭킹 산정(오름차순)**에 활용됩니다.
- 이동 횟수는 클리어 보상에 영향을 주지 않습니다.

---

## 6.5. 서버 권위 — 스테이지 시도 토큰 (Anti-Cheat)

클리어 요청을 정상 시작(Start) 없이 위조해 보상을 받는 우회를 차단하는 일회용 토큰 검증입니다.

- **발급(Start)**: `StartStage` 시 서버가 `SessionId`(GUID)를 새로 발급해 응답에 포함하고, Redis 키 `stage_session:{userId}:{stageId}`에 **TTL 1시간**으로 저장합니다.
- **검증·소모(Clear)**: `SubmitStageResult`는 요청의 `SessionId`를 함께 제출해야 하며, 서버가 저장된 토큰과 일치하는지 확인한 뒤 **즉시 삭제**(단일 사용)합니다.
- **거부 조건**: 키가 없음(미발급/TTL 만료) 또는 토큰 불일치 → `INVALID_STAGE_ATTEMPT` 에러 반환. 이는 결과 화면의 "다음" 등으로 Start를 건너뛰고 Clear만 반복 전송하는 우회를 막습니다.
- **클라이언트 처리**: 클라는 `ServerErrorCodes.IsStageSessionError`로 세션 오류를 식별하고, `popup.session_invalid`("세션 종료") 팝업 표시 후 로비로 복귀시킵니다.

---

## 7. 핵심 게임 플레이 루프

```mermaid
graph TD
    A[스테이지 진입] --> B[Slot Lane에 칩 초기 배치]
    B --> C[플레이어 칩 드래그 및 이동]
    C --> D{이동 후 보드 검사}

    D -->|Complete Set 완성| E[Signal Panel 흡수 연출 + 레인 빈 상태 전환]
    E --> F{모든 Signal Complete?}
    F -->|Yes| G[스테이지 클리어 + 골드 보상 + Moves 기록]
    F -->|No| C

    D -->|Soft Stuck 감지| H[Shuffle 버튼 Pulse + 보드 Dim]
    H --> C

    D -->|Hard Stuck 감지| I[Stuck 팝업 노출]
    I -->|광고 시청| J[Add Lane 적용 + 팝업 해제]
    I -->|300골드 → Shuffle| K[Solver-verified 재배치 + 팝업 해제]
    J --> C
    K --> C
    I -->|포기 / 로비| L[클리어 실패 + 메인 로비 복귀]
```

---

## 8. 인게임 UI 및 연출 가이드

- **인게임 배치**:
  - **상단 (HUD)**: 현재 스테이지 번호, 현재 누적 이동 횟수(Moves), 개인 최고 이동 횟수(Best Moves).
  - **중앙 상단**: `Signal Panel` — 각 Signal Type 세트 완성 시 해당 노드가 전구/별처럼 하나씩 점등되는 게이지 연출(회로 배선 대신 토큰 점등).
  - **중앙**: 퍼즐 조작부인 `Slot Lanes`.
  - **하단 (부스터 바)**: `Undo`, `Shuffle`, `Add Lane` 아이콘 버튼 및 보유 수량(미보유 시 골드 가격 표시).
- **상태별 피드백**:
  - **칩 선택 시**: 칩 스케일이 약간 커지며 테두리에 얇은 펄스 광 효과가 생깁니다.
  - **이동 가능 레인**: Top Chip을 든 상태에서 배치 가능한 레인들이 은은하게 깜빡(Pulse)입니다.
  - **이동 불가 레인 진입**: 칩이 원래 위치로 돌아가며, 레인 테두리가 빨간색으로 0.2초간 깜빡이고 짧은 좌우 흔들림(Shake)을 줍니다.
  - **Complete Set 완성**: 칩들이 부드럽게 트윈되어 상단 패널 해당 노드로 모인 뒤 픽셀 파티클 연출과 함께 밝게 점등됩니다.
  - **Soft Stuck 감지**: Shuffle 버튼 Pulse 애니메이션, 보드 패널 알파 0.85 Dim.

---

## 9. 아트 에셋 — 스프라이트 시트 생성 가이드

아트 방향: **컬러 토큰 중심 (픽셀 + 캐주얼)**. 칩은 둥근 파스텔 컬러 토큰, 상단 패널은 전구/별 점등 게이지, 회로 패턴은 Blind 레인 뒷면 한정 액센트.

아래 **단일 프롬프트로 하나의 스프라이트 시트**(투명 배경, 셀 사이 선 없음, 모든 셀 동일 크기)를 생성한 뒤, 각 셀을 잘라 `BoardSkin`(SpriteSet) 슬롯에 매핑한다.

### 단일 시트 프롬프트 (이미지 생성기용)

```
True 2D PIXEL ART sprite sheet, 3x3 grid of equally sized cells, transparent background.
NO grid lines / NO separators / NO borders between cells; each icon centered with uniform padding.
Style: retro pixel art, low-resolution chunky pixels, hard pixel edges, visible square pixels,
limited flat palette (max ~3 shades per color), simple 2-tone shading (one base + one shadow),
bold 1px dark outline, cute casual mobile-puzzle look. Front-facing flat 2D icons.
HARD CONSTRAINTS: NO 3D render, NO photorealism, NO smooth gradients, NO soft blur, NO drop shadows,
NO bevel/glossy plastic, NO anti-aliased smooth curves — keep it crisp pixel art.

Cells (left to right, top to bottom):
1. a rounded-square color token, flat fill + one darker pixel shade, bold dark pixel outline,
   one small pixel highlight — neutral/white so it can be color-tinted at runtime
2. the same rounded-square token as an OUTLINE ONLY, hollow transparent center, thick pixel stroke
   — neutral/white for tinting
3. a tall vertical slot holder that stacks 4 tokens, simple pixel frame with inner rail dots,
   semi-transparent fill
4. a soft round glow blob made of concentric pixel rings fading out (still pixelated, not blurry)
5. a small solid pixel dot (burst particle)
6. a thin hollow pixel ring / halo, transparent center
7. a "blind back" card: rounded pixel card with a tiny circuit-dot pattern, muted teal on dark
8. a lit panel node: a glowing pixel light-bulb / star, warm, with a dark socket base
9. a small pixel padlock icon, chunky, amber tint

Output: 512x512, transparent PNG, no text, no labels. Pixel-art only.
```

### 셀 → SpriteSet 슬롯 매핑

| 셀 | 슬롯 / 용도 |
|----|------------|
| 1 | `Chip` (토큰 본체, 런타임 컬러 틴트) |
| 2 | `ChipOutline` (틴트용 외곽선) |
| 3 | `LaneSlot` (레인 랙 프레임) |
| 4 | `Glow` (선택/광 효과) |
| 5 | `Disc` (파티클 도트) |
| 6 | `Ring` (노드 헤일로) |
| 7 | `Circuit` (Blind 뒷면) |
| 8 | Panel node — `SignalNodeView` 스킨(전구/별) |
| 9 | Lock 아이콘 — `LaneView` 잠금 |

생성 후 Unity Sprite Editor에서 9-slice 대상 셀(1·2·3)은 모서리 Border 지정, Chip/Outline/Node는 흰색 톤으로 받아 런타임 틴트. 슬롯 적용은 `BoardSkin`(SpriteSet) 인스펙터 드래그.
