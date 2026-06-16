# Campaign Progression + Ranking — Release Tracker

Signal Sort core campaign stage-clear flow + global/weekly/stage ranking. Previously designed (`progression-system-design.md`, `social-ranking-design.md`, `signal_sort_content_design_kr.md` §3·4) but unimplemented on the server.

| System | Data | Contracts | DB | Server | Client API | Tests | Status |
|--------|------|-----------|----|--------|-----------|-------|--------|
| Stage clear (first-clear reward, best/latest moves) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | full |
| Chapter milestone chest | ✓ (chapter.csv `chest_reward_group_id`) | n/a | ✓ | ✓ | n/a | ✓ | full |
| Global ranking (cleared stages / max stage) | n/a | ✓ | ✓ | ✓ | ✓ | ✓ | full (renamed stars→cleared_stages) |
| Weekly ranking | n/a | ✓ (reuses RankingPage) | ✓ | ✓ | ✓ | ✓ | full |
| Stage-moves ranking | n/a | ✓ | ✓ | ✓ | ✓ | ✓ | full (was stub) |

## Implemented (this pass)
- **Data**: `chapter.csv` + `chest_reward_group_id` (ch1–4 → 3001–3004).
- **Contracts**: new `Stage/` domain — `StageClearRequest`, `StageClearResponse`.
- **DB**: `user_stage_progress`, `user_weekly_ranking` tables; `user_ranking_totals` `total_earned_stars`→`total_cleared_stages`, `total_stars_achieved_at`→`total_cleared_at`.
- **Server**: `StageService.ClearStageAsync` (validate → progress upsert → first-clear reward → chapter milestone → totals/weekly → achievement reports → post-commit Redis sync). `RankingService` + `RecordClearAsync`, `GetWeeklyPageAsync`/`GetMyWeeklyRankAsync`, `GetStageRankAsync`, weekly/stage rebuild.
- **API**: `POST /api/stages/{id}/clear`; `GET /api/rankings/weekly`, `weekly/me`; `stages/{id}/me` wired (was stub); global types `stages`/`max-stage`. `RateLimitingMiddleware` clear regex → `/api/stages/{id}/clear`.
- **Tests**: `StageServiceTests` (first/re-clear, best-moves, milestone, validation, unknown stage); `RankingServiceTests` updated to `stages`.

## Design decisions applied
- **Anti-cheat = trust submitted moves** (MVP, matches daily-challenge). Server validates only `ruleset_version` + `completed_signal_types` == stage `{0..types-1}` set. No Redis attempt-attestation (design §2 attempt-token flow deferred).
- **`stars` → `cleared_stages`** rename to match no-star design (no per-stage stars in Signal Sort): DB `total_cleared_stages`, enum `GlobalRankingType.ClearedStages`, type string `stages`, Redis `ranking:global:stages`, and `Account.SaveSnapshotDto.TotalStars` → `TotalClearedStages`. Stale "best stars" AGENTS docs (Player, Controllers, Domain/Enums) corrected.
- **Weekly count** = every clear submission in current week (activity metric), not distinct-stage.

## Remaining (FLAG / out of scope)
- **Local DB already migrated** by `gen:db` (dry_run was off): `user_stage_progress`/`user_weekly_ranking` created, `total_cleared_stages` ADDED. NOTE: gen:orm never drops — legacy `total_earned_stars`/`total_stars_achieved_at` columns remain orphan in any existing DB; drop manually if desired. Prod/other envs: run `tools/db_generator.bat`.
- **Ch.5 content** (`chapter.csv` ch5 + `3005` chest reward_group + stages 81–100 boards) — content authoring via `tools/stage_editor`; not part of this server pass.
- **Client gameplay → `/clear` call wiring** is the InGame seam (InGameController stage-complete → `StageApiService`).
- **Pre-existing unrelated test failure**: `PlayerServiceTests.UpdateProfile_InvalidNicknameChars` expects `INVALID_DISPLAY_NAME` but code returns `INVALID_DISPLAY_NAME_CHAR` (predates this work).
