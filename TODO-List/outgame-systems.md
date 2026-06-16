# OutGame Systems — Release Tracker

Status of the OutGame design systems (excludes InGame gameplay).

| System | Data | Contracts | DB | Server | Client API | Tests | UI View+builder | UI prefab build | Status |
|--------|------|-----------|----|--------|-----------|-------|-----------------|-----------------|--------|
| Cosmetic | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | FLAG(Unity menu) | full (board unified into Shop section) |
| Daily-login (attendance) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | FLAG(Unity menu) | full (auto-popup + badge) |
| Achievement | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | FLAG(Unity menu) | full (list+toast; AccountPopup entry) |
| Daily-challenge (OutGame) | n/a | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | FLAG(Unity menu) | full (popup + Ranking tab; Start=coming-soon) |

## UI implemented (this pass)
- **Views** (`OutGame/Lobby/`): RewardDisplay, CosmeticSectionView, CosmeticPreviewPopupView, AttendancePopupView, AchievementListPopupView, AchievementToastView, DailyChallengePopupView.
- **UIEditorSetup builders + menu items**: CosmeticPreviewPopup, CosmeticItemCell, AttendancePopup, AchievementListPopup, AchievementItemCell, AchievementToast, DailyChallengePopup; SetupLobby adds Shop cosmetic section + Ranking Challenge tab; CreateAccountPopup swaps board tab → [Achievements] button.
- **Integrations**: ShopTabView (`_cosmeticSection` last sibling), RankingTabView (Challenge tab), LobbyView (GoToRankingChallenge + attendance auto-open), LobbyBadgeContainer (attendance + challenge badges), AccountPopupView (board tab removed, achievement entry).
- **Board unification decision applied**: cosmetic Board category is the canonical board-customization surface; AccountPopup board-skin tab removed.

## Remaining (FLAG / out of scope)
- **Build UI prefabs in Unity** (agent cannot run GUI): run `Tools/UI Setup/1 - Create All Prefabs` (rebuilds Lobby cosmetic section + Ranking Challenge tab + AccountPopup) **and** the individual `Tools/UI Setup/Prefabs/{AttendancePopup,AchievementListPopup,AchievementToast,CosmeticPreviewPopup,DailyChallengePopup}` items. Re-run `LobbyCanvas` + `AccountPopup` menus to apply the cosmetic-section / board-tab-removal structure changes.
- **Cosmetic preview art**: `preview_res` keys (`ui_cosmetic_*`) and any new badge icons have no sprites yet → cells render placeholder color. Add PNGs + `dynamic_resource.csv` rows when art lands.
- **Board unification — InGame half (out of scope)**: cosmetic `active_board_skin` → InGame board render mapping not wired; legacy `EquippedBoardThemeId` render path remains. Wire when InGame board-skin rendering is implemented.
- **Achievement toast trigger**: `AchievementToastView` prefab/View ready; gameplay achievement-completion trigger is the InGame seam.
- **InGame coupling (excluded from this scope)**:
  - Achievement progress for gameplay condition types (StageClearCount, BoosterlessClearCount, BestMovesRenewCount, MoveTopPercentileCount, ShufflelessWeek, ChallengeBreakClearCount) — wire InGame/stage-clear flow to `AchievementService.ReportValueAsync`/`ReportCountAsync`.
  - Daily-challenge puzzle-play (`/attempt`→board→`/clear`) and procedural Reverse-Path generation + Solver (design §6). Server stores seed/params only and trusts submitted moves.
- **Design inconsistencies flagged** (kept faithful to cosmetic catalog as SoT):
  - `chip_ghost` + `lane_ghost` both unlock from achievement `skl_03`.
  - achievement `ded_04` "Daily Chip Skin" / `ded_06` "Veteran Board Skin" have no matching cosmetic catalog entry — granted gold only; `chip_daily`/board skins unlock via challenge streak conditions per catalog.

## Reward group ranges added
| Range | Domain |
|-------|--------|
| `4001–4017`, `4101–4103` | Daily-login day + milestone rewards |
| `6001–6023` | Achievement rewards |
| `7001`, `7003/7007/7030/7100` | Daily-challenge base + streak rewards |
