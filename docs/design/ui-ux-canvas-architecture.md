# UI/UX — 캔버스 아키텍처 (Canvas Architecture)

## 개요

하이브리드 모델: persistent 상태인 DDOL UIManager + 씬별 전용 Canvas (Scene-specific UI).

```
[UIManager — DontDestroyOnLoad]
  ├── Canvas_Popup    (Sort: 10)
  ├── Canvas_Overlay  (Sort: 20)
  ├── Canvas_Toast    (Sort: 30)
  └── Canvas_Loading  (Sort: 100)

[Boot Scene]
  └── Canvas_Scene    (Sort: 0)

[Lobby Scene]
  └── Canvas_Scene    (Sort: 0)

[InGame Scene]
  └── Canvas_Scene    (Sort: 0)
```

---

## DDOL UIManager 캔버스 레이어

| 캔버스 | 정렬 순서(Sort) | 주요 내용 | 인스턴스화 방식 |
|--------|-----------|----------|----------------|
| Canvas_Popup | 10 | 확인 다이얼로그, 스테이지 정보, 계정 팝업, 설정 패널, 보상 팝업 | 동적 생성/파괴 |
| Canvas_Overlay | 20 | 결과 오버레이, 실패 오버레이, 일시정지 팝업, 챕터 해금 오버레이, 튜토리얼 오버레이 | 동적 생성/파괴 |
| Canvas_Toast | 30 | 토스트(Toast) 알림 | 정적 인스턴스, 표시/숨기기 |
| Canvas_Loading | 100 | 로딩 오버레이, 네트워크 에러 | 정적 인스턴스, 표시/숨기기 |

**중첩 규칙:**
- 오버레이(Overlay): 한 번에 하나만 표시. 새 오버레이 요청 시 기존 오버레이를 먼저 닫음.
- 팝업(Popup): 중첩 가능. 확인 다이얼로그는 설정 패널 위에 뜰 수 있음.
- 백(Back) 제스처: 가장 위에 있는 팝업을 닫음. 오버레이는 버튼을 통해서만 닫힘.

---

## 씬별 캔버스 내용

| 씬(Scene) | Canvas_Scene 주요 내용 |
|-------|----------------------|
| Boot | 로고 이미지, 로딩 스피너 |
| Lobby | 헤더 (아바타, 골드), 하단 네비게이션 바, 탭 콘텐츠 (홈/상점/랭킹) |
| InGame | HUD (일시정지 버튼, Moves/Best Moves), Signal Panel, Slot Lane 보드 컨테이너 앵커, 부스터 바 |

씬 UI는 항상 정렬 순서 0으로 렌더링되어 UIManager 캔버스(10~100)보다 항상 뒤에 위치합니다.

---

## Canvas Scaler — 모든 캔버스에 동일 설정 적용 (필수)

| 속성 | 값 |
|----------|-------|
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | 1080 × 1920 |
| Screen Match Mode | Match Width Or Height |
| Match | 0.5 |
| Reference Pixels Per Unit | 100 |

캔버스 간 설정이 다를 경우 레이어 간 크기 불일치가 발생합니다.

---

## SafeAreaHandler 컴포넌트

노치 / 다이내믹 아일랜드 / 홈 바(iPhone X 이상)에 대응합니다. 화면 가장자리에 인접한 UI 컨테이너에 부착합니다.

**부착 대상:**
- 로비 헤더 컨테이너
- 하단 네비게이션 바 컨테이너
- 인게임 HUD 컨테이너
- DDOL Canvas_Loading 패널

배경 이미지: 화면 끝까지 채워지는 것(bleed)을 허용하며 SafeAreaHandler가 필요하지 않습니다.

---

## UIManager API (동작 명세)

```
UIManager.ShowPopup<T>(params)      → Canvas_Popup에 T 생성, 인스턴스 반환
UIManager.ShowOverlay<T>(params)    → Canvas_Overlay에 T 생성 (기존 오버레이 먼저 닫음)
UIManager.ShowToast(msg, type)      → Canvas_Toast의 토스트 활성화
UIManager.ShowLoading()             → Canvas_Loading의 로딩 오버레이 활성화
UIManager.HideLoading()             → 로딩 오버레이 비활성화
UIManager.ShowNetworkError(onRetry) → 네트워크 에러 활성화, 재시도 콜백 바인딩
UIManager.CloseTopPopup()           → Canvas_Popup의 최상단 아이템 제거
```

---

## 반응형 내부 요소

### TMP 폰트 크기 정책
- 고정된 헤더, 버튼 라벨, 숫자는 **고정 크기**를 사용하며 Canvas Scaler가 기기별 스케일링을 담당합니다.
- 플레이어 이름, 아이템 설명 등 동적 콘텐츠는 **TMP Auto Sizing**을 사용합니다 (최소 12dp).

### 지원 종횡비
- 16:9 (구형 안드로이드), 18:9~20:9 (최신 폰), 19.5:9 (다이내믹 아일랜드)를 기본적으로 지원합니다.
- 태블릿(4:3)은 레이아웃 틀어짐이 발생할 수 있어 MVP에서는 지원하지 않으며, 2단계에서 별도 레이아웃 프로필로 지원할 예정입니다.
