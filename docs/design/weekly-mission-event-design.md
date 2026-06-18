# 주간 미션 이벤트 기획서 (Weekly Mission Event Design)

> **네이밍**: 노출 명칭은 "주간 미션 이벤트". "패스"(배틀패스=유료 BM 연상) 워딩 미사용 — 본 이벤트는 무과금 리텐션 콘텐츠다.

`Status: implemented`
NOTE: synced to impl 2026-06-18 — built across data/contracts/DB/server/client. Progress aggregated in the stage-clear flow (no submit endpoint). Reward track uses `reward_group_id` FK (range 7001–7004) per the data normalization mandate instead of the §8.1 inline reward columns. Cosmetic ties moved to attendance (`chip_daily`→day_30) / achievement (`board_challenge`→`skl_04`).

## 0. 배경 (대체 사유)

데일리 챌린지는 "전 세계 동일 퍼즐"을 위해 **절차적 퍼즐 생성(Reverse-Path) + Solver 검증**에 의존했으나 해당 파이프라인이 미구현 상태로 `Start=coming-soon` 껍데기로 남았다. 챌린지 랭킹도 이미 랭킹 탭에서 제거되어(팝업 내부 모드로만 잔존) 콘텐츠 의미를 상실했다.

주간 미션 이벤트는 데일리 챌린지를 **완전 대체**한다. 핵심 차이:
- **새 퍼즐 생성 불필요** — 기존 캠페인 스테이지 플레이 행동을 집계.
- 경쟁축은 기존 **Weekly 랭킹 탭**이 담당(중복 랭킹 신설 안 함).
- 코스메틱 한정 해금은 출석·업적으로 이관(§7), 본 이벤트는 **골드/소비재 보상만**.

> 제거된 챌린지 스펙은 `daily-challenge-design.md`(superseded) 참조.

---

## 1. 개요

주간 미션 이벤트는 매주 회전하는 미션 세트로 기존 캠페인 플레이를 반복·일일 접속으로 유도하는 리텐션 이벤트다. 미션 달성 → 이벤트 포인트(EP) 누적 → 리워드 트랙 마일스톤 해제. 릴랙싱 정체성에 맞춰 **미완료 페널티 없음**(매주 새 미션으로 자연 초기화).

---

## 2. 핵심 규칙

| 항목 | 내용 |
| :--- | :--- |
| **주기** | 매주 월요일 00:00 UTC 초기화 (Weekly 랭킹과 동일 경계 → `week_start_date` 재사용) |
| **만료** | 일요일 24:00 UTC. 미수령 마일스톤 보상은 만료 시 소멸 (만료 전 배지 알림) |
| **미션 수** | 주당 5개 고정 (회전 풀에서 시드 선택, 전 세계 동일 세트) |
| **진행 갱신** | 별도 제출 없음 — 스테이지 클리어 플로우에서 서버가 자동 집계 (치트 신뢰 모델, `campaign-progression.md`와 동일) |
| **페널티** | 없음. 미완료는 단순히 다음 주 새 미션으로 대체 (비징벌) |
| **부스터/골드 비용** | 일반 플레이와 동일 (이벤트 전용 할인/비용 없음) |

---

## 3. 미션 풀 (회전)

미션은 기존 플레이 행동을 집계한다. `condition_type`은 업적 시스템과 **동일 트리거 시드를 재사용**(StageClearCount / PerfectClearCount / BoosterlessClearCount / BestMovesRenewCount / ChapterProgress) — 서버에 신규 집계 경로를 추가하지 않는다.

| 미션 타입 (`condition_type`) | 예시 목표 | EP |
| :--- | :--- | :---: |
| `stage_clear_count` | 스테이지 10회 클리어 | 200 |
| `perfect_clear_count` | 퍼펙트 클리어 3개 | 300 |
| `boosterless_clear` | 부스터 없이 5회 클리어 | 300 |
| `chapter_progress` | 신규 스테이지 5개 클리어(진도) | 200 |
| `best_moves_renew` | 최고 기록(`best_moves`) 갱신 5회 | 200 |

- 매주 풀에서 5개를 서버 시드로 선택 → 전 세계 동일 세트(공유 경험 유지).
- 풀은 회전 가능하도록 CSV 기반(`weekly_mission_pool.csv`). 신규 미션 추가 = 데이터 변경만.

---

## 4. 리워드 트랙

누적 EP가 마일스톤을 넘으면 해당 보상 수령 가능. **코스메틱 없음 — 골드/소비재 전용**(코스메틱 한정 해금은 출석·업적으로 이관, §7).

| 누적 EP | 보상 |
| :---: | :--- |
| 200 | 100 골드 |
| 500 | Shuffle ×3 |
| 900 | 300 골드 + Add Lane ×2 |
| **1,200 (완주)** | 200 골드 |

- 주간 총 보상 가치: **600 골드 + Shuffle ×3 + Add Lane ×2** (출석 주간 ~830–970 골드와 보완 관계, 과지급 회피).
- 5개 미션 EP 합계 = 1,200 → 전 미션 완수 시 트랙 완주.
- 마일스톤 수령은 개별(수령형). 미수령분은 주 만료 시 소멸.

---

## 5. 랭킹

**신설하지 않음.** 주간 경쟁은 기존 Weekly 랭킹 탭(주간 클리어 수, `social-ranking-design.md §5.2`)이 이미 담당. 주간 미션 이벤트는 개인 진행/보상 트랙으로 한정한다 — 랭킹 탭 비대화(챌린지 제거 사유) 방지.

---

## 6. UI 흐름

### 6.1. 로비 배지
- 기존 챌린지 배지 슬롯을 **주간 미션 배지**로 대체 (`LobbyBadgeContainer`의 `EventLayoutGroup`).
- 미완료 미션 또는 미수령 마일스톤 존재 시 레드 도트 알림.

### 6.2. 주간 미션 팝업 (`WeeklyMissionPopup`, `DailyChallengePopup` 대체)

```
┌────────────────────────────────────────┐
│  📋  이번 주 미션          D-3 남음     │
│  ─────────────────────────────────────│
│  EP ▓▓▓▓▓▓▓░░░░  700 / 1,200          │
│  트랙: ●200 ●500 ○900 ○1200           │
│  ─────────────────────────────────────│
│  ✓ 스테이지 10클리어        +200 EP    │
│  ✓ 퍼펙트 3개               +300 EP    │
│  ▓ 부스터 없이 5클리어  3/5 +300 EP    │
│  ○ 신규 5스테이지       0/5 +200 EP    │
│  ○ 최고기록 5갱신       2/5 +200 EP    │
│  ─────────────────────────────────────│
│        [200 보상 받기]                  │
└────────────────────────────────────────┘
```

- 미션 항목: 완료(✓) / 진행 중(진행 바) / 미시작(○).
- 마일스톤: 달성+미수령 → 수령 CTA, 수령 완료 → 점등, 미달성 → 잠금.
- 보상 수령 → RewardPopup 연동(`RewardDisplay` 재사용).

---

## 7. 코스메틱 해금 경로 이관

데일리 챌린지가 부여하던 한정 코스메틱은 다음으로 이관한다(`cosmetic-system-design.md` / `achievement-system-design.md` / `daily-login-design.md` 동기화 완료).

| 코스메틱 | 기존(제거) | 신규 해금 |
| :--- | :--- | :--- |
| `chip_daily` (Daily Mark) | 챌린지 30일 스트릭 (`ded_04`) | **출석 30일 마일스톤** |
| `board_challenge` (Signal Champion) | 챌린지 100일 스트릭 | **업적 `skl_04` (Platinum)** — 조건 재정의 |

본 이벤트 자체는 코스메틱을 부여하지 않는다(골드/소비재 전용).

---

## 8. 데이터 및 DB

### 8.1. 정적 데이터 (`shared/datas/event/`)

**`weekly_mission_pool.csv`** — 회전 미션 풀
```csv
mission_id,condition_type,condition_value,ep_reward,name_key,desc_key
wm_stage10,stage_clear_count,10,200,event.wm.stage10.name,event.wm.stage10.desc
wm_perfect3,perfect_clear_count,3,300,event.wm.perfect3.name,event.wm.perfect3.desc
wm_noboost5,boosterless_clear,5,300,event.wm.noboost5.name,event.wm.noboost5.desc
wm_progress5,chapter_progress,5,200,event.wm.progress5.name,event.wm.progress5.desc
wm_best5,best_moves_renew,5,200,event.wm.best5.name,event.wm.best5.desc
```

**`weekly_mission_track.csv`** — 리워드 트랙 마일스톤
```csv
ep_threshold,reward_type,reward_value,reward_item_id
200,gold,100,
500,item,3,booster_shuffle
900,gold+item,300,booster_add_lane
1200,gold,200,
```

### 8.2. DB 스키마 (`server/db/schema.json`)

**`weekly_mission_sets`** — 주차별 배정된 미션 세트(전역 1행/주)
| 컬럼 | 타입 | 설명 |
| :--- | :--- | :--- |
| `week_start_date` | date (PK) | 주 시작(월요일 UTC) |
| `mission_ids` | string | 배정된 5개 `mission_id` (CSV/JSON) |

**`user_weekly_missions`** — 유저별 미션 진행
| 컬럼 | 타입 | 설명 |
| :--- | :--- | :--- |
| `user_id` | FK | 플레이어 |
| `week_start_date` | date | 주차 |
| `mission_id` | string | 미션 ID |
| `progress` | int | 현재 진행 카운트 |
| `is_completed` | bool | 완료 여부 |

**`user_weekly_mission_state`** — 유저별 이벤트 진행 상태
| 컬럼 | 타입 | 설명 |
| :--- | :--- | :--- |
| `user_id` | FK | 플레이어 |
| `week_start_date` | date | 주차 |
| `total_ep` | int | 누적 EP |
| `claimed_thresholds` | string | 수령 완료한 마일스톤 EP 목록 |

> `week_start_date` 경계는 `user_weekly_ranking`과 동일 — 주간 롤오버 로직 재사용.

---

## 9. API 규약

| 메서드 | 경로 | 설명 |
| :--- | :--- | :--- |
| `GET` | `/api/events/weekly-mission` | 이번 주 미션 5개 + 내 진행 + 누적 EP + 트랙 수령 상태 |
| `POST` | `/api/events/weekly-mission/claim/{threshold}` | 마일스톤 보상 수령 |

- 미션 진행 갱신 전용 엔드포인트는 **없음** — 스테이지 클리어 응답 처리 seam에서 서버가 `user_weekly_missions.progress`를 자동 갱신(`AchievementService` 리포트 경로와 동일 트리거 재사용).

---

## 10. 구현 영향 (follow-up)

본 문서는 설계(planned). 구현 시 다음이 필요하며 각각 별도 작업으로 진행:
- `shared/datas/event/*.csv` 신규 → `info_generator`
- `shared/datas/string/client_string.csv` 신규 문자열 → `info_generator` + `subset_fonts`
- `shared/contracts/` 신규 DTO(WeeklyMission 조회/수령) → `pkt_generator`
- `server/db/schema.json` 3개 테이블 → `db_generator`
- 서버: 주차 배정 잡 + 진행 집계 seam + 수령 API
- 클라이언트: `WeeklyMissionPopup` (구 `DailyChallengePopup` 대체), 배지 교체
- **챌린지 코드 제거**: `DailyChallengePopupView`, `DailyChallengeApiService`, `DailyChallengeContracts`, `ChallengeContext`, `RankingTabView.SelectChallenge`/`RefreshChallenge`, 서버 daily-challenge 레이어/테이블
