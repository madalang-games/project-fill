# OutGame Systems — Release Tracker

Status of the OutGame design systems (excludes InGame gameplay).

| System | Data | Contracts | DB | Server | Client API | Tests | UI View+builder | UI prefab build | Status |
|--------|------|-----------|----|--------|-----------|-------|-----------------|-----------------|--------|
| Cosmetic | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | FLAG(Unity menu) | full (board unified into Shop section) |
| Daily-login (attendance) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | FLAG(Unity menu) | full (auto-popup + badge) |
| Achievement | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | FLAG(Unity menu) | full (list+toast; AccountPopup entry) |
| Weekly-mission event | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | FLAG(Unity menu) | full (replaced daily-challenge; popup + lobby badge; progress aggregated in stage-clear flow) |

## UI implemented (this pass)
- **Views** (`OutGame/Lobby/`): RewardDisplay, CosmeticSectionView, CosmeticPreviewPopupView, AttendancePopupView, AchievementListPopupView, AchievementToastView, WeeklyMissionPopupView.
- **UIEditorSetup builders + menu items**: CosmeticPreviewPopup, CosmeticItemCell, AttendancePopup, AchievementListPopup, AchievementItemCell, AchievementToast, WeeklyMissionPopup; SetupLobby adds Shop cosmetic section; CreateAccountPopup swaps board tab → [Achievements] button.
- **Integrations**: ShopTabView (`_cosmeticSection` last sibling), RankingTabView (3 tabs stage/perfect/weekly), LobbyView (attendance auto-open), LobbyBadgeContainer (attendance + weekly-mission badges), AccountPopupView (board tab removed, achievement entry).
- **Board unification decision applied**: cosmetic Board category is the canonical board-customization surface; AccountPopup board-skin tab removed.

## Remaining (FLAG / out of scope)
- **Build UI prefabs in Unity** (agent cannot run GUI): run `Tools/UI Setup/1 - Create All Prefabs` (rebuilds Lobby cosmetic section + badge/ranking structure + AccountPopup) **and** the individual `Tools/UI Setup/Prefabs/{AttendancePopup,AchievementListPopup,AchievementToast,CosmeticPreviewPopup,WeeklyMissionPopup}` items. Re-run `LobbyCanvas` + `AccountPopup` menus to apply the cosmetic-section / board-tab-removal structure changes.
- **Cosmetic preview art**: `preview_res` keys (`ui_cosmetic_*`) and any new badge icons have no sprites yet → cells render placeholder color. Add PNGs + `dynamic_resource.csv` rows when art lands.
- **Board unification — InGame half (out of scope)**: cosmetic `active_board_skin` → InGame board render mapping not wired; legacy `EquippedBoardThemeId` render path remains. Wire when InGame board-skin rendering is implemented.
- **Achievement toast trigger**: `AchievementToastView` prefab/View ready; gameplay achievement-completion trigger is the InGame seam.
- **Stage-clear seam wired**: `StageService.ClearStageAsync` now reports `StageClearCount`, `BestMovesRenewCount`, `ChapterComplete`, and `BoosterlessClearCount` (via `StageClearRequest.BoostersUsed`) to achievements, plus the Weekly Mission Event progress (`StageClearCount`/`PerfectClearCount`/`BoosterlessClear`/`ChapterProgress`/`BestMovesRenew`). Client `InGameController` tracks booster use (`_boostersUsed`).
- **Still-unwired achievement seams**: `MoveTopPercentileCount`, `ShufflelessWeek`, `ChallengeBreakClearCount` (campaign endless Lv.101+), `WeeklyRankFirst` (needs a weekly-ranking settlement job).
- **Design inconsistencies flagged** (kept faithful to cosmetic catalog as SoT):
  - `chip_ghost` + `lane_ghost` both unlock from achievement `skl_03`.
  - `chip_daily` now unlocks from **attendance day_30**, `board_challenge` from **achievement `skl_04`** (daily-challenge streak unlocks removed).

## Reward group ranges added
| Range | Domain |
|-------|--------|
| `4001–4017`, `4101–4103` | Daily-login day + milestone rewards |
| `6001–6023` | Achievement rewards |
| `7001–7004` | Weekly-mission EP track milestone rewards (200/500/900/1200) |
