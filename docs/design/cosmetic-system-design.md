# 코스메틱 시스템 기획서 (Cosmetic System Design)

## 1. 개요

코스메틱 시스템은 게임 플레이에 영향 없는 순수 외관 커스터마이징을 제공합니다. 스테이지 100개 완주 이후에도 플레이어가 목표를 가질 수 있는 **장기 골드 소비처**이자, 업적·출석 시스템의 희귀 보상 채널입니다.

> **설계 원칙**: 코스메틱은 Pay-to-Win 요소 없음. 골드(소프트 커런시)로만 구매 가능하거나 업적/출석으로 무료 획득. IAP로 코스메틱을 직접 판매하지 않음.

---

## 2. 카테고리

### 2.1. Chip Skin (칩 스킨)
Signal Chip의 테두리 형태·텍스처를 변경합니다. 색상(Signal Type 식별용)은 항상 유지됩니다.

| ID | 이름 | 설명 | 잠금 해제 방법 | 골드 가격 |
| :--- | :--- | :--- | :--- | :---: |
| `chip_default` | Circuit Board | 기본 라운드 사각형 회로 칩 | 기본 제공 | — |
| `chip_hex` | Hex Grid | 육각형 격자 패턴 칩 | 골드 구매 | 800 |
| `chip_crystal` | Crystal Shard | 다이아몬드형 결정 패턴 | 골드 구매 | 1,200 |
| `chip_retro` | Classic Dot | 레트로 도트 매트릭스 | 골드 구매 | 600 |
| `chip_neon` | Neon Pulse | 네온 펄스 외곽선 애니메이션 | 골드 구매 | 1,500 |
| `chip_platinum` | Platinum Core | 백금 광택 내부 패턴 | **업적 `prg_04` 달성** | — |
| `chip_ghost` | Ghost Signal | 반투명 홀로그램 | 업적 `skl_03` 달성 | — |
| `chip_daily` | Daily Mark | 한정 마크 | **출석 30일 달성** | — |
| `chip_prism` | Prism | 색을 순환하는 외곽선(스펙트럼) | 골드 구매 | 2,500 |

### 2.2. Lane Skin (레인 스킨)
Slot Lane의 컨테이너 외관을 변경합니다. 레인 용량·규칙 영향 없음.

| ID | 이름 | 설명 | 잠금 해제 방법 | 골드 가격 |
| :--- | :--- | :--- | :--- | :---: |
| `lane_default` | Circuit Rack | 기본 어두운 카드 슬롯 | 기본 제공 | — |
| `lane_holo` | Holographic | 홀로그래픽 테두리 + 내부 빛 산란 | 골드 구매 | 1,200 |
| `lane_bronze` | Bronze Circuit | 청동 산화된 회로 레일 | 골드 구매 | 900 |
| `lane_crystal` | Crystal Tower | 투명 수정 기둥 스타일 | 골드 구매 | 2,000 |
| `lane_terminal` | Green Terminal | 레트로 터미널 녹색 파이프 | 골드 구매 | 1,500 |
| `lane_ghost` | Ghost Lane | 반투명 음영 레인 | **업적 `skl_03` 달성** | — |

### 2.3. Board Skin (보드 스킨)
인게임 퍼즐 보드 전체의 배경 테마를 변경합니다. 챕터 테마(자동 적용)와 별개로 오버라이드됩니다.

| ID | 이름 | 설명 | 잠금 해제 방법 | 골드 가격 |
| :--- | :--- | :--- | :--- | :---: |
| `board_default` | Neo-Semi | 기본 반도체 네온 보드 | 기본 제공 | — |
| `board_void` | Void | 극도로 미니멀한 검정 그리드 | 골드 구매 | 2,000 |
| `board_quantum` | Quantum Field | 양자 파동 배경 + 파티클 | 골드 구매 | 3,500 |
| `board_retro_dos` | Retro DOS | 80년대 DOS 터미널 스타일 | 골드 구매 | 2,500 |
| `board_circuit` | Circuit Board | 출석 30일 달성 코스메틱 | **출석 30일 달성** | — |
| `board_vintage` | Vintage Terminal | 레트로 호박색 터미널 | **출석 100일 달성** | — |
| `board_collector` | Collector's Edition | 모든 코스메틱 수집 달성 | **업적 `col_05` 달성** | — |
| `board_challenge` | Signal Champion | 장기 숙련 보상 | **업적 `skl_04` 달성 (Platinum)** | — |
| `board_spectrum` | Spectrum Flux | 외곽선이 색을 순환하며 궤도 스파크가 도는 보드 | 골드 구매 | 6,000 |

### 2.4. 스킨 렌더링 모델 (BoardTheme / ChipFinish / Fx Tier)

스킨은 별도 텍스처 에셋이 아니라 **렌더 토큰 묶음**으로 프로시저럴하게 합성됩니다.

- **Board = 마스터 컨테이너**: 보드 코스메틱은 `BoardTheme`로 칩·레인·노드 3종 조각의 색/마감 토큰과 Fx 등급을 한 번에 테마링합니다. Chip/Lane 코스메틱은 그 위에 자기 오버라이드를 레이어링 → 플레이어가 칩 스킨을 임의 보드 위에 혼합 가능.
- **Fx 등급(`BoardFxTier`)**: `Static`(색만 바꾸는 저가 스킨) / `Dynamic`(네온 엣지·호흡 테두리·스펙트럼 순환 등 애니메이션 프리미엄 스킨).
- **칩 마감 축(`ChipFinish`)**: 신호 색 위에 덧입히는 **표면 재질** — `Flat`(기본) / `Dither`(도트 스티플) / `Scanline`(CRT 주사선) / `Bevel`(엠보스) / `Gloss`(광택). **칩 몸체 색은 항상 신호 색으로 유지**(게임플레이 식별성 불변), 마감은 표면만 변조.
- **기본값 = 무변경 보장**: 기본 보드/칩/레인 플레이어는 기존 프로시저럴 외형과 동일하게 렌더(회귀 없음). `chip_prism` / `board_spectrum`은 외곽선이 색 스펙트럼을 순환하는 최상위 Dynamic 스킨.

---

## 3. 경제 영향

| 항목 | 총 골드 가치 |
| :--- | :---: |
| Chip Skin 구매 가능 총합 | 8,600 골드 |
| Lane Skin 구매 가능 총합 | 7,600 골드 |
| Board Skin 구매 가능 총합 | 16,500 골드 |
| **총 구매 가능 골드 소비** | **32,700 골드** |

100스테이지 완주 플레이어 순 잔액(~3,100 골드 추산, `economy-system-design.md` §4 기준)으로 Chip Skin 일부 구매 가능. 데일리 시스템 참여 시 수개월 안에 풀 컬렉션 달성 가능 → 장기 목표 제공.

---

## 4. UI 흐름

### 4.1. 상점 탭 내 코스메틱 섹션

- 상점(Shop) 탭은 최상위 서브탭(`ShopSubTab`)으로 분기: **Product(상품/IAP) / Skin(스킨) / Avatar(아바타)** (`shop.tab.*`).
- Skin 서브탭 내부: 카테고리 탭 (Chip / Lane / Board) + 그리드 레이아웃.

```
┌──────────────────────────────────────────┐
│  외관 커스터마이징 (Cosmetics)            │
│  ┌─────────┬─────────┬─────────┐        │
│  │  Chip   │  Lane   │  Board  │  ← 탭  │
│  └─────────┴─────────┴─────────┘        │
│                                          │
│  [회로 칩]  [육각형]  [결정]  [레트로]   │
│  (적용 중) (800🪙)  (1200🪙) (600🪙)    │
│                                          │
│  [네온]    [플래티넘] [고스트]  [데일리]  │
│  (1500🪙)  (업적)   (업적)   (출석)     │
└──────────────────────────────────────────┘
```

### 4.2. 코스메틱 아이템 탭 시 미리보기 팝업

```
┌──────────────────────────────────┐
│  Hex Grid Chip Skin              │
│  ─────────────────────────────  │
│  [미리보기: 인게임 칩 렌더링]      │
│                                  │
│  육각형 격자 패턴 신호 칩          │
│                                  │
│  🪙 800 골드                     │
│  ─────────────────────────────  │
│  [구매 및 적용]    [닫기]          │
└──────────────────────────────────┘
```

### 4.3. 인게임 내 코스메틱 적용
- 상점에서 코스메틱 적용 즉시 서버 저장.
- 다음 인게임 진입 시부터 적용. 현재 진행 중인 스테이지에는 미적용.
- 챕터 테마가 Board Skin을 자동 덮어쓰지 않도록 `use_custom_board_skin` 플래그로 제어.

---

## 5. 데이터 및 DB

### 5.1. 정적 데이터 (`shared/datas/cosmetic/cosmetic_item.csv`)

```csv
cosmetic_id,category,name_key,desc_key,unlock_type,unlock_cost,unlock_condition_id,preview_res
chip_hex,chip,cosmetic.chip_hex.name,cosmetic.chip_hex.desc,gold,800,,ui_cosmetic_chip_hex
chip_platinum,chip,cosmetic.chip_plat.name,cosmetic.chip_plat.desc,achievement,0,prg_04,ui_cosmetic_chip_platinum
board_vintage,board,cosmetic.board_vintage.name,cosmetic.board_vintage.desc,attendance,0,day_100,ui_cosmetic_board_vintage
...
```

### 5.2. DB 스키마

**`user_cosmetics` 테이블**

| 컬럼 | 타입 | 설명 |
| :--- | :--- | :--- |
| `user_id` | FK | 플레이어 |
| `cosmetic_id` | string | 코스메틱 ID |
| `unlocked_at` | datetime | 잠금 해제 시각 |

**`user_active_cosmetics` 테이블** (현재 적용 중인 코스메틱)

| 컬럼 | 타입 | 설명 |
| :--- | :--- | :--- |
| `user_id` | FK (PK) | 플레이어 |
| `active_chip_skin` | string | 현재 적용 Chip Skin ID |
| `active_lane_skin` | string | 현재 적용 Lane Skin ID |
| `active_board_skin` | string | 현재 적용 Board Skin ID |
| `use_custom_board_skin` | bool | 챕터 테마 오버라이드 여부 |

---

## 6. API 규약

| 메서드 | 경로 | 설명 |
| :--- | :--- | :--- |
| `GET` | `/api/cosmetics` | 전체 코스메틱 목록 + 내 잠금 해제 현황 |
| `POST` | `/api/cosmetics/{id}/unlock` | 골드로 코스메틱 잠금 해제 |
| `PUT` | `/api/cosmetics/active` | 활성 코스메틱 변경 |
