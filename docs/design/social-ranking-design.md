# 소셜 및 랭킹 시스템 기획서 (Social & Ranking System Design)

## 1. 권한 (Authority)
- DB는 플레이어 진행도 및 랭킹 합계에 대한 원천 데이터(Source of Truth)입니다.
- Redis는 재구축 가능한 랭킹 캐시/인덱스로 사용됩니다. Redis 쓰기는 DB 커밋 후에 발생하며, 클리어 응답 시 더티 리드(Dirty-read)가 발생할 수 있습니다.
- Redis 데이터 손실 시 서버 초기화 중에 DB에서 복구하거나, 캐시 미스 시 지연 재구축(Lazy rebuild), 또는 관리자 트리거 재구축을 통해 복구합니다.

## 2. 스테이지 클리어 검증
클라이언트는 `StageAttemptClearRequest`를 통해 요약된 입력을 보냅니다:
- `ruleset_version`
- `moves_used` (사용된 이동 횟수)
- `completed_signal_types` (완료된 신호 타입 리스트)

서버 검증 항목:
- 활성화된 Redis 어테스트(attempt)가 존재하고 유저/스테이지/시도 ID와 일치하는지 확인
- 시도가 만료되지 않았는지 확인
- 요청된 규칙 버전이 정적 스테이지 규칙 버전과 일치하는지 확인
- `moves_used`가 1 이상의 정수인지 확인
- `completed_signal_types`가 정적 레벨 파일의 모든 등장 Signal Type 목록과 완벽히 일치하는지 확인 (치트 방지)

서버는 정적 스테이지 데이터(목표 별점 이동 횟수 임계값)를 사용하여 별(Star) 개수를 계산합니다.

## 3. DB 모델
`players`
- 플레이어 계정을 저장합니다. `display_name` 및 `avatar_id`를 포함합니다.

`user_stage_progress`
- 유저/스테이지별 한 행씩 존재합니다.
- `best_star`, `best_moves_used`, 최초 클리어 시간 등을 저장합니다.

`user_ranking_totals`
- 유저별 한 행씩 존재합니다.
- `total_earned_stars`(총 획득 별점), `max_cleared_stage_id`(최대 클리어 스테이지), `win_streak`(연승 기록) 등을 저장합니다.

## 4. 스테이지 랭킹
- 랭킹 단위: 스테이지별 최고 기록인 `best_moves_used`(사용 이동 횟수가 적을수록 상위).
- 각 플레이어의 최고 기록만 인덱싱됩니다.
- 내 스테이지 순위 = 나보다 적은 이동 횟수로 클리어한 플레이어 수 + 1.

Redis 키: `ranking:stage:{stageId}:moves`

## 5. 글로벌 랭킹
랭킹 탭은 두 가지 페이지를 노출합니다.

별점(Stars) 탭:
- 점수: `total_earned_stars`
- 높을수록 상위, 동점 시 `total_stars_achieved_at`이 빠를수록 상위.

최대 스테이지(Max stage) 탭:
- 점수: `max_cleared_stage_id`
- 높을수록 상위, 동점 시 `max_stage_achieved_at`이 빠를수록 상위.

## 6. API 규약
스테이지 클리어 응답에는 다음이 추가됩니다:
- `stars`
- `moves_used`
- `stage_rank`
- `is_new_best` (최고 기록 갱신 여부)

랭킹 API:
- `GET /api/rankings/global/stars?offset=&limit=`
- `GET /api/rankings/global/max-stage?offset=&limit=`
- `GET /api/rankings/stages/{stageId}/me`

프로필 API:
- `POST /api/player/profile` (이름 및 아바타 설정)

## 7. 아바타 메타데이터 (`avatar.csv`)
아바타는 정적 메타데이터에 정의됩니다. 플레이어는 커스텀 이미지를 업로드할 수 없으며 미리 정의된 옵션 중에서 선택해야 합니다.
- `unlock_cost`: 잠금 해제에 필요한 골드 비용 (0이면 무료).
- `unlock_type`: 조건 카테고리 (무료, 골드, 업적).
