# 업적 시스템 기획서 (Achievement System Design)

## 1. 개요

업적 시스템은 플레이어에게 스테이지 클리어 외의 보조 목표를 제공하고, 숙련도·참여도·수집 행동을 보상합니다. 아바타 잠금 해제, 코스메틱 획득, 골드 보상의 주요 공급원이며, 달성 UI를 통해 플레이어의 진행 성취감을 가시화합니다.

---

## 2. 티어 구조

| 티어 | 색상 | 기준 |
| :---: | :---: | :--- |
| **Bronze** | 주황빈티지 | 입문자가 자연스럽게 달성 |
| **Silver** | 은회색 | 중급 플레이어 (~20스테이지 이후) |
| **Gold** | 골드 | 컨텐츠 전체 플레이어 (~100스테이지) |
| **Platinum** | 청록 | 데일리·코스메틱 등 장기 참여 플레이어 전용 |

---

## 3. 카테고리 및 업적 목록

### 3.1. Progression (진행)

| ID | 업적명 | 조건 | 티어 | 보상 |
| :--- | :--- | :--- | :---: | :--- |
| `prg_01` | 첫 신호 | 스테이지 1 클리어 | Bronze | 50 골드 |
| `prg_02` | 챕터 1 완주 | Ch.1 모든 스테이지 클리어 | Bronze | 100 골드 |
| `prg_03` | 챕터 3 완주 | Ch.3 모든 스테이지 클리어 | Silver | 200 골드 + Avatar #05 |
| `prg_04` | 챕터 5 완주 | Ch.5 모든 스테이지 클리어 | Gold | 500 골드 + **Platinum Chip Skin** |
| `prg_05` | 챌린지 돌파구 | 챌린지 모드(Lv.101+) 5스테이지 클리어 | Gold | 300 골드 |
| `prg_06` | 1백 클리어 | 누적 스테이지 클리어 100회 | Gold | 800 골드 |

### 3.2. Skill (숙련)

| ID | 업적명 | 조건 | 티어 | 보상 |
| :--- | :--- | :--- | :---: | :--- |
| `skl_01` | 무보조 클리어 | 부스터 미사용으로 스테이지 클리어 (1회) | Bronze | 80 골드 |
| `skl_02` | 퓨어 런 | 부스터 미사용으로 스테이지 5회 클리어 | Silver | 200 골드 |
| `skl_03` | 퍼펙트 러너 | 부스터 미사용으로 스테이지 20회 클리어 | Gold | 500 골드 + **Ghost Lane Skin** |
| `skl_04` | 데일리 퍼스트 | 데일리 챌린지 당일 전체 1위 달성 | Platinum | 1,000 골드 + 전용 뱃지 |
| `skl_05` | 최고 기록 갱신 | `best_moves` 갱신 10회 | Silver | 150 골드 |
| `skl_06` | 정밀 분석가 | 10개 스테이지에서 이동 횟수 상위 10% 달성 | Gold | 400 골드 |

### 3.3. Dedication (참여)

| ID | 업적명 | 조건 | 티어 | 보상 |
| :--- | :--- | :--- | :---: | :--- |
| `ded_01` | 7일 로그인 | 7일 연속 접속 (일일 출석 스트릭 7) | Bronze | 150 골드 |
| `ded_02` | 30일 로그인 | 30일 연속 접속 | Silver | 400 골드 + Avatar #08 |
| `ded_03` | 챌린지 7연속 | 데일리 챌린지 7일 연속 클리어 | Silver | 300 골드 + Shuffle ×5 |
| `ded_04` | 챌린지 30연속 | 데일리 챌린지 30일 연속 클리어 | Gold | 800 골드 + **Daily Chip Skin** |
| `ded_05` | 셔플 없는 일주일 | 7일간 Shuffle 부스터 미사용 | Silver | 200 골드 |
| `ded_06` | 100일 플레이어 | 앱 총 접속 100일 (연속 불필요) | Platinum | 2,000 골드 + **Veteran Board Skin** |

### 3.4. Collection (수집)

| ID | 업적명 | 조건 | 티어 | 보상 |
| :--- | :--- | :--- | :---: | :--- |
| `col_01` | 첫 수집 | 아바타 1종 잠금 해제 | Bronze | 50 골드 |
| `col_02` | 아바타 수집가 | 아바타 5종 잠금 해제 | Silver | 200 골드 |
| `col_03` | 스킨 입문 | 코스메틱 1종 잠금 해제 | Bronze | 80 골드 |
| `col_04` | 코스메틱 팬 | 코스메틱 5종 잠금 해제 | Silver | 300 골드 |
| `col_05` | 풀 컬렉터 | 모든 코스메틱 잠금 해제 | Platinum | 3,000 골드 + **Collector Board Skin** |

---

## 4. UI 흐름

### 4.1. 업적 달성 알림 (토스트 기반)
- 업적 달성 시 화면 상단에 슬라이드 다운 알림 (3초 자동 종료).
- 아이콘: 티어별 색상 배지 + 업적명 + 보상 요약.

### 4.2. 업적 목록 화면
- **진입**: 로비 홈 탭 아바타 탭 → 계정 팝업 → [업적] 버튼.
- **레이아웃**: 카테고리 탭 (Progression / Skill / Dedication / Collection) + 스크롤 리스트.
- **항목 상태**: 달성(점등 배지 + 보상 수령 버튼) / 진행 중(진행 바 표시) / 미시작(어두운 오버레이).
- **보상 수령**: 항목 탭 → 보상 수령 확인 → RewardPopup 연동.

---

## 5. 데이터 및 DB

### 5.1. 정적 데이터 (`shared/datas/achievement/achievement.csv`)

```csv
achievement_id,category,name_key,desc_key,tier,reward_type,reward_value,reward_item_id,condition_type,condition_value
prg_01,progression,ach.prg_01.name,ach.prg_01.desc,bronze,gold,50,,stage_clear,1
prg_04,progression,ach.prg_04.name,ach.prg_04.desc,gold,gold+cosmetic,500,chip_skin_platinum,chapter_complete,5
skl_03,skill,ach.skl_03.name,ach.skl_03.desc,gold,gold+cosmetic,500,lane_skin_ghost,boosterless_clear,20
...
```

### 5.2. DB 스키마

**`user_achievements` 테이블**

| 컬럼 | 타입 | 설명 |
| :--- | :--- | :--- |
| `user_id` | FK | 플레이어 |
| `achievement_id` | string | 업적 ID |
| `progress` | int | 현재 진행 카운트 |
| `is_completed` | bool | 달성 여부 |
| `completed_at` | datetime? | 달성 시각 |
| `reward_claimed` | bool | 보상 수령 여부 |

### 5.3. 업적 진행도 갱신 트리거

| 업적 트리거 이벤트 | 갱신되는 업적 ID 예시 |
| :--- | :--- |
| 스테이지 클리어 | `prg_01`, `prg_06`, `skl_01~03`, `skl_05`, `skl_06` |
| 챕터 완료 | `prg_02`, `prg_03`, `prg_04` |
| 데일리 챌린지 클리어 | `ded_03`, `ded_04`, `skl_04` |
| 일일 로그인 | `ded_01`, `ded_02`, `ded_06` |
| 코스메틱 잠금 해제 | `col_03`, `col_04`, `col_05` |
| 아바타 잠금 해제 | `col_01`, `col_02` |

---

## 6. API 규약

| 메서드 | 경로 | 설명 |
| :--- | :--- | :--- |
| `GET` | `/api/achievements` | 내 전체 업적 목록 + 진행도 |
| `POST` | `/api/achievements/{id}/claim` | 완료된 업적 보상 수령 |
