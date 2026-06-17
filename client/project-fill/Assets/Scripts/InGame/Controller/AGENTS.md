# Scripts/InGame/Controller

MonoBehaviour orchestrators for the Signal Sort loop and scene entry.

## Files
| file | class | role |
|------|-------|------|
| `InGameSceneEntry.cs` | `InGameSceneEntry` | Sets portrait/fps; resolves the lobby-selected stage (`ScrollStateCache.LastPlayedStageId-1`, `_startStageIndex` fallback); gates board build behind `StageApiService.StartStage` (server unlock check) — STAGE_LOCKED → toast + slide back to Lobby. Skips the gate for daily-challenge (`ChallengeContext.Active`) and offline/dev (no `StageApiService`) |
| `InGameController.cs` | `InGameController` | Loop: select/move, boosters, Soft/Hard Stuck, clear; cycles per-chapter sample stages |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `InGameController.Begin(startIndex)` | method | Subscribes to BoardView events (once); consumes `ChallengeContext.Active` (campaign vs daily-challenge mode); loads first stage |
| `InGameController.SubmitClear(stageId)` | method | POSTs the clear to `StageApiService.ClearStage`; applies server best/unlock, then shows `ResultOverlayView` with a `ClearSummary` (moves/best/new-best from the response) + granted rewards + double-reward eligibility (`IsFirstClear` && rewards>0) + a per-clear attemptId; local fallback (no rewards, moves=best) when API absent |
| `InGameController.LoadChallenge()` | method | Daily-challenge board from `ChallengeContext` seed/params (deterministic); tracks clear time |
| `InGameController.SubmitChallengeClear()` | method | POSTs `DailyChallengeApiService.SubmitClear` (moves + clear seconds); clear popup → lobby (no next stage) |
| `InGameController.SpendThen(type,onPaid)` | method | Server-authoritative booster spend (`CurrencyApiService.SpendGold`, price from `item.csv`); local-balance pre-check + insufficient/error toast; applies `onPaid` only on confirm; local-deduct fallback when currency service absent. Undo is free (bypasses) |
| `InGameController.WatchAdForAddLane()` | method | Stuck Add Lane via rewarded ad (`STUCK_ADD_LANE`); grants free lane only on `Earned`, sets `_adRewardedThisStage`, re-runs `PostMoveCheck`; free fallback if no ad service |
| `InGameController.ShowInterstitialThen(proceed)` | method | Post-stage interstitial (`ShowInterstitialIfEligible`, suppressed by `_adRewardedThisStage`); on shown → `AdApiService.RecordInterstitialShown` + `AdEligibilityCache.OnInterstitialShown`; then `proceed` |
| `InGameController._boardView` | SerializeField | Bound in InGame.unity |
| `InGameSceneEntry._controller` / `_sceneBg` | SerializeField | Bound in InGame.unity (`_sceneBg` kept for compat) |
| `InGameSceneEntry.OnStartFailed(error)` | method | STAGE_LOCKED/server error → localized toast + `SceneTransition.SlideDownToScene("Lobby")` |
| `InGameSceneEntry._startStageIndex` | SerializeField | Fallback index into `StageLibrary.Samples` (0 = Ch1) when `ScrollStateCache.LastPlayedStageId` is unset (direct in-editor play) |

## Rules
- Controller owns game logic; BoardView owns rendering/animation. Controller never touches GameObjects.
- Booster economy (spec §4): Undo free/unlimited; Shuffle + AddLane charge gold via `SpendThen` (server-authoritative); prices read from `item.csv` (`BoosterType.ItemId()` → `Item.price`), never hardcoded. Stuck-popup Shuffle also charges; stuck AddLane is the free ad reward (§5.1/§5.4).
- Stage clear is server-authoritative: `SubmitClear` POSTs `StageApiService.ClearStage` (moves + completed signal types `{0..types-1}`); server owns best-moves, ranking, first-clear / chapter-chest rewards, and achievement progress. Response syncs gold (in API service) + caches best-moves and unlock (`ApplyMaxClearedStage`). Local fallback (RecordBestMoves + UnlockStage) when the API is absent. Star rating removed.
- Ads (spec §5.4): rewarded `STUCK_ADD_LANE` gates the stuck Add Lane; `_adRewardedThisStage` suppresses the post-stage interstitial; flag resets per stage in `LoadCurrent`. All ad calls null-guard the service (dev/no-Boot play still works).
- BoardView events are subscribed once in `Begin` (BoardView persists across stage reloads).
- Campaign entry is server-gated: `InGameSceneEntry` calls `StageApiService.StartStage` and only calls `_controller.Begin` on success; STAGE_LOCKED bounces back to Lobby. Daily-challenge (`ChallengeContext.Active`) and offline/dev (no `StageApiService`) bypass the gate and `Begin` directly.
- Move flow: capture top chip + `MovableCount` + destBase → `Board.Move` (A-R08 stack pour) → `BoardView.AnimateMove(…, onComplete: PostMoveCheck)`.
- `LoadCurrent` → `ResolveRow(index)` reads `StageDataService.GetStage(index+1)`; `BuildDefinition` maps gimmicks to `StageDefinition`; the board comes from `BoardCodec.Decode(row.board, def)`. Empty `board`/no row → `BoardFactory.Generate` fallback; null → `StageLibrary`. Glyph order `RBGYPCOMLT`=SignalType 0..9; lane codes N/L/B.
