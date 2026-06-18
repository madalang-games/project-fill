# Art Style Guide

`Status: implemented` — 아트 SoT는 **`UIEditorSetup.cs`가 생성하는 UI 스타일**(= `UIColorPalette.cs` 팔레트 + Editor 빌더 규칙). 신규 아이콘·이미지 리소스는 이 스타일에 맞춰 출력한다. 본 문서는 그 스타일을 리소스 제작 가이드로 옮긴 것.

- 팔레트 SoT: `client/.../Assets/Scripts/Editor/UIColorPalette.cs`
- 적용 규칙 SoT: `client/.../Assets/Scripts/Editor/UIEditorSetup.cs` (+ `Editor/AGENTS.md` Rules)
- InGame 보드/칩 색은 별도: `shared/datas/common/color_palette.csv` (생성 데이터)
- 게임 모티프(Signal/회로/전구·별 노드): `signal_sort_system_design_kr.md` §1·§9

> ⚠️ 이전 "PixelPop / 캔디 파스텔" 방향은 폐기. PixelPop은 구 프로젝트명, 파스텔 그라데이션은 PlayerSettings 스플래시 한정 레거시.

---

## 1. 핵심 정체성 — Dark Neon Puzzle

**어두운 미드나잇 네이비 배경 + 네온 액센트 + 픽셀아트 패널.** 차분한 다크 베이스 위에 시안/마젠타/틸/바이올렛 네온이 빛나는 톤. 글로시 캔디 아님.

- 베이스: 딥 네이비 (`#0B0B1E`) — 거의 검정에 가까운 남보라
- 액센트: 고채도 네온 (전기 시안·핫 마젠타·네온 틸·일렉트릭 바이올렛)
- 형태: 픽셀아트 — 굵은 외곽선 + **near-black 픽셀 드롭섀도** + 계단형 코너
- 텍스트: 아이스 화이트 (`#E8E8FF`)
- InGame 카메라 배경도 다크 네온: `rgb(0.06, 0.07, 0.11)`

---

## 2. 팔레트 (SoT = UIColorPalette.cs)

토큰 그대로 사용. 인라인 hex 금지(일회성 컨텍스트 색만 예외).

| 토큰 | Hex | 용도 |
|------|-----|------|
| `UI_BG_DEEP` | `#0B0B1E` | 딥 미드나잇 네이비 — 최하단 배경, dismiss/backdrop |
| `UI_BG_MID` | `#181836` | 다크 인디고 — 패널 채움(fill) |
| `UI_PRIMARY` | `#4CC9F0` | 일렉트릭 시안 — 탭, 보조 긍정 버튼 |
| `UI_CTA` | `#F72585` | 핫 네온 마젠타 — 화면당 단일 주요 액션 |
| `UI_SUCCESS` | `#06D6A0` | 네온 틸 — 성공, Play |
| `UI_DANGER` | `#EF233C` | 네온 레드 — 파괴적/되돌릴 수 없는 액션 |
| `UI_TEXT` | `#E8E8FF` | 아이스 화이트 — 모든 텍스트 |
| `UI_BORDER` | `#7B2FBE` | 일렉트릭 바이올렛 — 기본 패널 외곽선/액센트 |
| `DIM` | `rgba(0.02,0.02,0.08,0.80)` | near-black 오버레이(비상호작용 backdrop) |
| `UI_SHADOW` | `rgba(0.01,0.01,0.04,0.90)` | 픽셀아트 드롭섀도(near-black) |

**버튼 색 시맨틱** (리소스 배경/틴트 정할 때 동일 적용):

| 색 | 의미 | 예 |
|----|------|-----|
| `UI_CTA` 마젠타 | 화면당 단일 주요 액션 | Confirm, Retry, Next, Link |
| `UI_PRIMARY` 시안 | 보조 긍정/대안 CTA | Double Reward, 보조 Retry |
| `UI_DANGER` 레드 | 파괴/되돌릴 수 없는 부정 | Forfeit, Restart |
| `UI_BG_DEEP` 네이비 | dismiss/취소/네비 | Map, back/close |

---

## 3. 형태 언어 (UIEditorSetup 빌더 = 리소스가 맞춰야 할 기준)

UI 패널/버튼은 코드가 합성하므로, **개별 이미지 리소스(아이콘 등)는 이 형태 언어와 일관되게** 제작.

- **픽셀 드롭섀도**: 모든 주요 요소(Panel/Button/Ribbon)는 오프셋 **우측+8 / 하단-8**, near-black(`UI_SHADOW`). 아이콘도 동일 방향 픽셀 섀도를 내장하면 합성 UI와 일치.
- **외곽선**: 패널 기본 외곽선 = 바이올렛(`UI_BORDER`), 8px 두께. 픽셀 오브젝트(셀/아바타)는 굵은 다크 외곽선.
- **3레이어 패널 구조**(참고): Shadow → Border(바이올렛) → Content(인디고 fill). 아이콘을 패널 위에 얹을 땐 인디고/네이비 배경 대비 고려.
- **코너**: 계단형(stair-step) 픽셀 코너. 안티앨리어싱된 매끄러운 곡선 금지.
- **최소 크기**: 버튼 ≥ 96×96px. 정사각 버튼은 `Visual/Icon`(preserveAspect) + GlowSweep 아이들 애니. → 아이콘 리소스는 **정사각·여백 포함·preserveAspect 안전**하게.
- **텍스트**: 아이스 화이트, AutoFontSize(min 32 / Header 72 · Button 56 · Normal 40). 이미지에 텍스트 굽지 말 것(로컬라이즈 깨짐).

---

## 4. Signal/Neon 액센트 (글로우 레이어)

게임 모티프 표현. 발광 처리 필수.

- 챕터 경로 `path_chapter`: 발광 **시안 네온 라인 + LED 전구 노드**.
- 데코: `led_star`(점멸 LED 별), `deco_pulse`(경로 진행 펄스), `deco_scanline`(스캔라인), `deco_mote`(글로우 mote).
- 회로(circuit) 모티프는 **Blind-back 액센트 + 경로 한정**. 전면 게임 보드를 회로기판화 금지. Signal Panel은 전구/별 노드로.
- 글로우 색은 팔레트 액센트(주로 시안)와 일치, 외곽 소프트 블룸 + 애디티브.

---

## 5. 카테고리별 규칙

| 카테고리 | 경로 | 규칙 |
|----------|------|------|
| **UI 아이콘** | `Sprites/UI/Icons`, `Nav` | 픽셀 심볼, 굵은 외곽선, 정사각, 투명 배경. 네온 액센트/아이스화이트 틸트. near-black 픽셀 섀도 내장 권장 |
| **Cell(게임말)** | `Sprites/Gameplay/Cells` | 라운드 픽셀 타일, 좌상단 흰 노치, 다크 비벨. core=골드 별, obstacle=다크 해칭 X, protector=라이트블루 아웃라인. native 118×118 |
| **BoardSkin** | `Sprites/Gameplay/BoardSkins` | 계단형 굵은 테두리 + 체커 필, 테마 컬러쌍 |
| **Avatar** | `Sprites/UI/Avatar` | 픽셀아트 큐트 동물 + 볼터치, 등급별 프레임 컬러(common~legendary 각 5종, 총 25) |
| **Chest** | `Sprites/UI/Chest` | 픽셀 골드 보물상자, inactive/active/claimed 3상태 |
| **IAP 상품** | `Resources/Sprites/UI` | 픽셀 우드 크레이트 + 내용물(코인·로켓 등) |
| **Item/Booster** | `Resources/Sprites/Items` | 글로시 심볼 + 스파클, 굵은 외곽선 |
| **Toast** | `Sprites/UI/Toast` | 원형 컬러 배지 + 흰 심볼. success=틸 / warning=옐로 / error=레드 |
| **Star** | `Sprites/UI/Stars` | 골드 비벨 별 / 빈 아웃라인 |
| **Path/Decoration** | `Resources/Sprites/Path`,`Decoration` | §4 네온 글로우 |

---

## 6. 기술 사양 (Unity 임포트)

| 항목 | 값 | 비고 |
|------|-----|------|
| filterMode | **Bilinear (1)** | ⚠️ Point 아님. 신규 리소스도 Bilinear 유지(샘플링 일치) |
| Pixels Per Unit | 100 | Canvas Scaler refPPU=100과 일치 |
| Mesh Type | Tight | |
| Pivot | Center (0.5,0.5) | |
| alphaIsTransparency | on | 투명 PNG, premultiply 금지 |
| Max Size / 압축 | 2048 / Android ASTC | |
| Canvas 기준 해상도 | 1080×1920, Match 0.5 | UI 리소스 스케일 산정 기준 |
| 셀 native | 118×118 | 카테고리별 상이, 정사각 권장 |

---

## 7. 네이밍 컨벤션

`snake_case` + 카테고리 prefix. 동적 로드 시 `shared/datas/common/dynamic_resource.csv`에 `resource_key`+`sprite_path` 등록 → FLAG `info_generator.bat`.

| prefix | 카테고리 |
|--------|----------|
| `cell_` | 게임 셀 |
| `avatar_` | 아바타 |
| `BoardSkins_` | 보드스킨 |
| `chest_` | 보물상자 |
| `toast_` | 토스트 |
| `item_` | 부스터 |
| `nav_` | 하단 탭 |
| `ui_` | 범용 UI 아이콘 |
| `ui_iap_` | IAP 상품 |
| `star_` | 클리어 별 |
| `path_` / `led_` / `deco_` | 경로·데코·파티클 |

---

## 8. 신규 아이콘/이미지 생성 체크리스트

1. **스타일**: Dark Neon Puzzle — 픽셀아트, 굵은 외곽선, near-black 드롭섀도(우+8/하-8), 계단 코너
2. **컬러**: §2 팔레트 토큰만 사용. 액센트=네온 시안/마젠타/틸/바이올렛, 텍스트/하이라이트=아이스화이트. 다크 네이비 위 대비 확보
3. **Signal 계열**: 시안 네온 글로우 + 전구/별 노드. 회로는 경로/Blind-back 한정
4. **형태**: 정사각·여백 포함·preserveAspect 안전, 버튼용은 96×96 기준
5. **임포트**: Bilinear / PPU 100 / Tight / alphaIsTransparency
6. **텍스트 금지**: 이미지에 문자 굽지 말 것(TMP+로컬라이즈가 담당)
7. **등록**: 동적 로드면 `dynamic_resource.csv` 등록
8. **팔레트 변경 시**: `UIColorPalette.cs`가 SoT — 색 추가/변경은 거기서, 본 문서·`Editor/AGENTS.md` 동기화

---

## 9. 하지 말 것 (Anti-patterns)

- 캔디/파스텔 글로시 톤 (Dark Neon Puzzle 위반)
- 외곽선 없는 플랫 벡터, 안티앨리어싱 곡선 코너
- 팔레트 외 임의 색 (인라인 hex는 일회성만)
- 이미지에 텍스트 베이크
- Point 필터로 변경 (기존 Bilinear 불일치)
- 전면 게임 보드 회로기판화 (회로=경로/Blind-back 액센트 한정)
- 색만으로 셀 구분 (색맹 대응 형상/심볼 병행)
- tube/bottle sort·스태미나·턴제한 실패 비주얼 재도입 (Signal Sort 정체성 위반)
- pay-to-win 암시 비주얼 (코스메틱은 순수 시각)
