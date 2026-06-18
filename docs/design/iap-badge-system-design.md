# 인앱 결제(IAP) 및 로비 배지 시스템 기획서 (IAP & Lobby Badge System Design)

## 1. 개요
본 기획서는 모바일 퍼즐 게임인 "project-fill"의 지속 가능한 비즈니스 모델(BM)과 사용자 리텐션 강화를 위해 인앱 결제(IAP) 상품 및 로비 배지 시스템을 구축하는 것을 목적으로 합니다. 
핵심 결제 상품인 **광고 제거(Remove Ads)**를 필두로, 현대 F2P 캐주얼 게임에서 널리 차용되는 UI 배치 패러다임을 반영하여 직관적이고 완성도 높은 상점과 로비 화면을 제시합니다.

---

## 2. 로비 배지 배치 설계 (HomeTab Badges)
홈 탭의 HUD 하단에 배치되는 배지는 성격에 따라 좌측과 우측의 두 개 레이아웃으로 분리 배치하여 직관성을 높입니다.

```
┌──────────────────────────────────────────┐
│  [아바타]                      [🪙 1,240]  │  ← HUD Header
├──────────────────────────────────────────┤
│                                          │
│  [🎪 이벤트 배지]        [🚫 광고 제거 배지] │  ← HUD 아래 좌/우 배치 영역
│  [📅 출석부 배지]                            │
│  [📋 주간 미션 배지]                         │
│                                          │
│                  (맵 스크롤 영역)           │
│                         ●                │
│                        /                 │
│                       ●                  │
│                                          │
```

### 2.1. 좌측 영역: 이벤트 배지 컨테이너 (`EventLayoutGroup`)
- **역할**: 게임 내 플레이어의 참여를 유도하는 순수 이벤트 및 콘텐츠 바로가기 배지.
- **적용 콘텐츠**:
  - **출석부(Attendance/Daily Login)**: 매일 로그인 보상 팝업 링크. 미수령 시 레드 도트 표시. `daily-login-design.md` 참조.
  - **주간 미션 이벤트(Weekly Mission Event)**: 미완료 미션 또는 미수령 마일스톤 존재 시 레드 도트 배지. 탭 시 주간 미션 팝업 진입. `weekly-mission-event-design.md` 참조.
  - **인게임 이벤트(Event Milestone)**: 기간 한정 챕터 클리어 이벤트 등.
- **레이아웃**: 좌측 정렬(UpperLeft), 아래로 쌓이는 `VerticalLayoutGroup`.

### 2.2. 우측 영역: 구매/BM 배지 컨테이너 (`BuyLayoutGroup`)
- **역할**: 유료 IAP 유도 및 스페셜 세일 패키지 전용 바로가기 배지.
- **적용 콘텐츠 (예시)**:
  - **광고 제거(No Ads Badge)**: 탭하면 광고 제거의 메리트를 안내하고 즉시 상점 또는 구매 팝업을 연동합니다.
  - **스타터 팩(Starter Pack Badge)**: 한정 특가 패키지 바로가기.
- **레이아웃**: 우측 정렬(UpperRight), 아래로 쌓이는 `VerticalLayoutGroup`.
- **예외 처리**: 해당 IAP 상품 구매가 완료되면, 해당 배지는 화면에서 즉시 제거(숨김)됩니다.

---

## 3. 상점 설계 및 IAP 상품 정의 (ShopTab Packages)
소프트 커런시(골드) 전용 상품 영역 상단에 실제 화폐로 결제하는 IAP 상품군(Special Packages) 영역을 구성합니다.

### 3.1. IAP 상품 정보 (IAP Products)
현대 캐주얼/퍼즐 게임에 적합한 대표 상품 3종을 설계합니다.

| Product ID | 상품명 (Localization Key) | 구성 요소 및 혜택 | 가격 (USD) | 유형 |
| :--- | :--- | :--- | :--- | :--- |
| **no_ads** | 광고 제거 (`shop.iap.no_ads.title`) | 전면 광고 영구 제거 (보상형 광고 제외) | $1.99 | 비소모성 (Non-Consumable) |
| **starter_pack** | 스타터 팩 (`shop.iap.starter.title`) | 6,000 골드 (1회 한정 로스리더, 최고 골드/$ 가치) | $4.99 | 소모성 (Consumable) |
| **master_bundle** | 마스터 번들 (`shop.iap.master.title`) | 골드만 지급 (번들 규모별) | $9.99 | 소모성 (Consumable) |

<!-- NOTE: synced to impl 2026-06-18 — IAP 보상은 **골드만** 정책으로 전환(no_ads는 전용 상품 1001로만 판매; starter에서 제거). IAP가 부스터를 직접 지급하면 골드→아이템 싱크 루프를 우회하므로 골드만 주입. 골드 라더(골드/$): small $2.99→3,000 · normal $5.99→6,500 · large $11.99→14,000 · starter $4.99→6,000(1회 한정, 최고 가치). 실 카탈로그 SoT = `shared/datas/shop/iap_product.csv` + `shared/datas/reward/reward_item.csv` (현재 구현: bundle_small/normal/large; 위 master_bundle 명칭은 미반영 drift). -->

### 3.2. 영수증 검증 및 결제 로그 구조화
다른 기기 로그인 시의 데이터 동기화와 환불/CS 문의 대응을 위해 서버 측에 구조화된 이력 테이블(`iap_purchases`)을 추가합니다.
- **주문 상태 추적**: `status` 컬럼에 `COMPLETED`, `REFUNDED` 등의 결제 진행 상태를 저장하여 CS 대처 가능하도록 처리합니다.
- **CS 필수 항목**: 스토어에서 고유하게 발급하는 주문 ID(`order_id`), 실제 결제 금액(`price`), 결제 통화(`currency`), 구매 시점(`created_at`)을 기록합니다.

---

## 4. 데이터 및 데이터베이스 테이블 명세

### 4.1. `iap_product` 테이블 (`shared/datas/shop/iap_product.csv`)
```csv
product_id,product_type,name_key,desc_key,price_usd,icon_res,bonus_gold,bonus_item_id,bonus_item_count
no_ads,NonConsumable,shop.iap.no_ads.title,shop.iap.no_ads.desc,1.99,ui_iap_no_ads,0,,0
starter_pack,Consumable,shop.iap.starter.title,shop.iap.starter.desc,4.99,ui_iap_starter,1000,1001,1
master_bundle,Consumable,shop.iap.master.title,shop.iap.master.desc,9.99,ui_iap_master,5000,1001,5
```

### 4.2. DB 스키마 (`server/db/schema.json`)
#### `players` 테이블 (기존 테이블 수정)
- 컬럼 추가: `is_no_ads` (bool, default: false) - 플레이어의 광고 제거 상태를 전역 캐싱하여 동기화.

#### `iap_purchases` 테이블 (신규 테이블 추가)
- 결제 검증 및 CS 로그 저장을 위한 핵심 이력 테이블.
- PK: `purchase_id` (string(36))
- FK: `user_id` -> `players.user_id`
- Unique Index: `(platform, order_id)` - 중복 거래 방지.

---

## 5. 이미지 생성용 프롬프트 가이드 (GPT 2.0 / Midjourney 용)
Signal Sort의 반도체 칩, 카드 슬롯, 회로 기판 톤에 맞춘 리소스를 생성하기 위한 영어 프롬프트 모음입니다. 튜브/물병/상자 같은 기존 정렬 퍼즐 또는 범용 번들 오브젝트는 사용하지 않습니다.

> **💡 배경 투명화 관련 주의사항 (Important Transparency Tip):**
> 일부 AI 엔진에서 `transparent background` 요청 시, 실제 투명이 아닌 **가짜 체크무늬(checkerboard) 배경**이 이미지에 포함되어 생성되는 경우가 있습니다. 이를 방지하기 위해 다음 방법을 권장합니다:
> 1. **Alpha 지원 엔진 사용**: DALL-E 3나 Midjourney v6의 특정 모드 등 실제 투명도를 지원하는 경우 `transparent background`를 유지합니다.
> 2. **크로마키 방식 활용 (권장)**: 배경을 `solid bright green (#00FF00) background`로 설정하여 생성한 뒤, 포토샵이나 온라인 툴로 해당 색상만 제거하는 것이 가장 확실합니다. (아래 프롬프트는 크로마키 방식을 기본으로 작성되었습니다.)

### 5.1. 광고 제거 배지 및 아이템 (No Ads Icon)
> **Prompt:**  
> Clean minimalist pixel art icon for 'Remove Ads', 32x32 pixel resolution. A small circuit-board ad panel with a flat red disable slash, semiconductor chip frame, limited cool neon palette, sharp pixel edges, no gradients, no glows. Solid bright green (#00FF00) background for easy transparency removal --v 6.0

### 5.2. 스타터 팩 상점 카드 아트 (Starter Pack Card Art)
> **Prompt:**  
> Simple pixel art bundle icon for 'Starter Pack', 32x32 pixel resolution. A compact signal-chip tray containing a few gold tokens and one small undo-chip symbol, circuit-board styling, flat shading, clean lines, minimalist design. Solid bright green (#00FF00) background for easy transparency removal --v 6.0

### 5.3. 마스터 번들 상점 카드 아트 (Master Bundle Card Art)
> **Prompt:**  
> Clean pixel art bundle icon for 'Master Bundle', 64x64 pixel resolution for clarity. A neat array of Signal Sort booster chips: shuffle, undo, and add-lane symbols beside a small stack of gold tokens, semiconductor and circuit-board styling, minimalist flat pixel art, vibrant but limited colors, sharp edges. Solid bright green (#00FF00) background for easy transparency removal --v 6.0
