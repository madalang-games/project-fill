using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.UI;
using Game.InGame.View;
using Game.OutGame.Settings;
using Game.Services;
using Game.Services.Tutorial;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Contracts.Rewards;
using UnityEngine;

namespace Game.InGame.Controller
{
    // Orchestrates the Signal Sort loop: selection, moves, boosters, stuck handling,
    // and cycling through the per-chapter sample stages so every gimmick can be verified.
    public class InGameController : MonoBehaviour
    {
        private const int SoftStuckNodeCap = 8000;

        // Rewarded-ad placement key for the Stuck Add Lane (mirror shared/datas/ad/ad_placement.csv).
        private const string PlacementAddLane = "STUCK_ADD_LANE";

        [SerializeField] private BoardView _boardView;
        [SerializeField] private bool      _isDevMode; // kept for scene compatibility

        // Pre-buffer up to one lookahead move (2 taps: select + commit) while a move animation plays,
        // then replay in order on completion. Only move-animation blocks enqueue (see _animating);
        // modal blocks (ad / network spend) still drop taps.
        private const int TapQueueCap = 2;

        private Board _board;
        private StageDefinition _def;
        private int  _stageIndex;
        private int  _selectedLane = -1;
        private bool _animating;           // true only while a move animation is playing → taps queue
        private readonly Queue<int> _tapQueue = new();
        private bool _subscribed;
        private bool _adRewardedThisStage; // §5.4: set on rewarded-ad watch; suppresses this stage's interstitial; reset on stage enter
        private bool _boostersUsed;        // any booster (Undo/Shuffle/AddLane) used this stage → reported on clear (boosterless seams); reset on stage enter
        private string _sessionId;         // start-issued attempt token for the current stage; echoed on clear
        private int  _failCount;           // consecutive hard-stuck fails on the current stage (FailRepeat tutorial)
        private int  _failTrackedStage = -1; // stage id the fail counter belongs to; resets _failCount on stage change

        public void Begin(int startIndex, string sessionId = null)
        {
            _sessionId  = sessionId;
            _stageIndex = startIndex;
            if (_boardView == null)
            {
                _boardView = FindObjectOfType<BoardView>();
            }
            if (!_subscribed && _boardView != null)
            {
                _boardView.OnLaneTapped    += HandleLaneTapped;
                _boardView.OnBoosterTapped += HandleBooster;
                _boardView.OnPauseTapped   += HandlePause;
                _subscribed = true;
            }
            // Escape on InGame (no popup open) opens the Pause popup.
            if (UIManager.Instance != null) UIManager.Instance.SetEscapeHandler(HandlePause);
            LoadCurrent();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevSkinSwitcher.Ensure(this, _boardView); // dev-only in-game skin/stage switcher overlay
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // DEV-ONLY: jump to a campaign stage index live (skin switcher). Bypasses the server unlock
        // gate and daily-challenge mode. Compiled out of release builds.
        public void DevLoadStage(int index)
        {
            _stageIndex = Mathf.Max(0, index);
            LoadCurrent();
        }
#endif

        private void OnDestroy()
        {
            if (UIManager.Instance != null) UIManager.Instance.ClearEscapeHandler(HandlePause);
        }

        private void HandlePause()
        {
            if (TutorialManager.ActiveBlocking) return; // overlay owns input during a tutorial step
            if (_boardView.IsInputBlocked) return;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPopup<PausePopupView>(p =>
                {
                    p.Configure(
                        onResume: () => {},
                        onRestart: LoadCurrent,
                        onStageSelect: () => BoardView.GoToScene("Lobby"),
                        onHowToPlay: TutorialManager.Recap
                    );
                });
            }
        }

        private void LoadCurrent()
        {
            var row = ResolveRow(_stageIndex);
            _def = row != null ? BuildDefinition(row) : StageLibrary.Get(_stageIndex);
            // Saved stages carry an explicit board layout (board column) — decode it directly.
            // Empty board (or no CSV row) → generate one; null generation → known-good sample fallback.
            _board = (row != null && !string.IsNullOrEmpty(row.board))
                ? BoardCodec.Decode(row.board, _def)
                : BoardFactory.Generate(_def);
            if (_board == null)
            {
                _def   = StageLibrary.Get(_stageIndex);
                _board = BoardFactory.Generate(_def);
            }
            _selectedLane = -1;
            ResetTapQueue();
            _adRewardedThisStage = false; // new stage → clear the rewarded-ad / interstitial-suppression flag
            _boostersUsed = false;        // new stage → reset booster-use tracking (boosterless clear seam)
            _boardView.Init(_board, _def);
            var progress = Game.Services.PlayerProgressService.Instance;
            _boardView.SetBestMoves(progress != null ? progress.GetBestMoves(_stageIndex + 1) : 0);
            UpdateSoftStuck();

            // Onboarding triggers: FirstLaunch (by stage id) + GimmickAppear (by present board gimmick).
            int stageId = _stageIndex + 1;
            if (stageId != _failTrackedStage) { _failTrackedStage = stageId; _failCount = 0; }
            TutorialManager.NotifyBoardReady(stageId, DetectGimmicks(_def));
        }

        // Board gimmicks present in this stage, for GimmickAppear tutorial evaluation.
        private static HashSet<TutorialGimmick> DetectGimmicks(StageDefinition def)
        {
            var set = new HashSet<TutorialGimmick>();
            if (def == null) return set;
            if (def.LaneKinds != null)
            {
                foreach (var k in def.LaneKinds)
                {
                    if (k == LaneKind.Locked)     set.Add(TutorialGimmick.Locked);
                    else if (k == LaneKind.Blind) set.Add(TutorialGimmick.Blind);
                }
            }
            if (def.OverloadType != null)                       set.Add(TutorialGimmick.Overload);
            if (def.RelayOrder != null && def.RelayOrder.Length > 0) set.Add(TutorialGimmick.Relay);
            return set;
        }

        // Stage source of truth is shared/datas/stage/stage.csv (via StageDataService). The hardcoded
        // StageLibrary remains a dev fallback when the CSV/service is unavailable or the id is missing.
        private ProjectFill.Data.Generated.Stage ResolveRow(int index)
        {
            var svc = Game.Services.StageDataService.Instance;
            return svc != null ? svc.GetStage(index + 1) : null; // stage_id is 1-based
        }

        // Maps a Stage CSV row to the runtime StageDefinition. Glyph order = SignalType order
        // (R B G Y P C O M L T = 0..9); lane kind codes N/L/B = Normal/Locked/Blind.
        private const string Glyphs = "RBGYPCOMLT";

        private static StageDefinition BuildDefinition(ProjectFill.Data.Generated.Stage s)
        {
            int laneCount = s.lane_kinds != null ? s.lane_kinds.Length : 0;
            var kinds  = new LaneKind[laneCount];
            var unlock = new SignalType[laneCount];
            for (int i = 0; i < laneCount; i++)
            {
                kinds[i]  = ParseLaneKind(s.lane_kinds[i]);
                unlock[i] = (s.lock_unlock != null && i < s.lock_unlock.Length)
                    ? ParseGlyph(s.lock_unlock[i]) : SignalType.Red;
            }

            SignalType[] relay = null;
            if (!string.IsNullOrEmpty(s.relay_order))
            {
                relay = new SignalType[s.relay_order.Length];
                for (int i = 0; i < relay.Length; i++) relay[i] = ParseGlyph(s.relay_order[i]);
            }

            return new StageDefinition
            {
                Name          = $"STAGE {s.stage_id}",
                Chapter       = s.chapter_id,
                Difficulty    = s.difficulty,
                Types         = s.types,
                LaneKinds     = kinds,
                LockUnlock    = unlock,
                OverloadType  = s.overload_type >= 0 ? (SignalType)s.overload_type : (SignalType?)null,
                RelayOrder    = relay,
            };
        }

        private static SignalType ParseGlyph(char c)
        {
            int i = Glyphs.IndexOf(c);
            return (SignalType)(i < 0 ? 0 : i);
        }

        private static LaneKind ParseLaneKind(char c) => c switch
        {
            'L' => LaneKind.Locked,
            'B' => LaneKind.Blind,
            _   => LaneKind.Normal,
        };

        // ── Input ────────────────────────────────────────────────────────────

        private void HandleLaneTapped(int lane)
        {
            // Tutorial: Tap (info) steps block the board; Select/Move (action) steps let the real tap
            // through so the player plays for real and the step advances on the action.
            if (TutorialManager.BoardInputBlocked) return;
            if (_boardView.IsInputBlocked)
            {
                // Buffer taps only while a move animation plays; replayed in order on completion.
                if (_animating && _tapQueue.Count < TapQueueCap) _tapQueue.Enqueue(lane);
                return;
            }

            if (_selectedLane == -1)
            {
                var src = _board.Lanes[lane];
                if (src.IsEmpty || src.Pending || src.Locked) return; // nothing selectable
                _selectedLane = lane;
                _boardView.SetSelection(lane);
                SfxEventBus.Play(SfxId.LaneSelected);
                TutorialManager.NotifyAction(TutorialAdvanceMode.Select); // advance a "select" tutorial step
            }
            else if (_selectedLane == lane)
            {
                _selectedLane = -1;
                _boardView.ClearHighlights();
            }
            else
            {
                int from = _selectedLane;
                _selectedLane = -1;

                if (_board.CanMoveTo(from, lane))
                {
                    var chip     = _board.Lanes[from].TopChip!.Value;
                    int count    = _board.MovableCount(from, lane);
                    int destBase = _board.Lanes[lane].Count;
                    var absorbed = _board.Move(from, lane);

                    _boardView.BlockInput(true);
                    _animating = true;
                    _boardView.AnimateMove(from, lane, chip, count, destBase, absorbed, () =>
                    {
                        _boardView.BlockInput(false);
                        _animating = false;
                        SfxEventBus.Play(SfxId.GravityLand); // chips finished dropping into dest lane
                        if (absorbed != null && absorbed.Count > 0)
                            SfxEventBus.Play(SfxId.RewardClaimed); // completed set lit the Signal Panel
                        TutorialManager.NotifyAction(TutorialAdvanceMode.Move); // advance a "move" tutorial step
                        PostMoveCheck();
                        DrainTapQueue();
                    });
                }
                else
                {
                    _boardView.ClearHighlights();
                    _boardView.PlayInvalid(lane);
                    SfxEventBus.Play(SfxId.ActionBlocked); // illegal drop onto an invalid lane
                }
            }
        }

        // Replay buffered taps in order once a move animation settles. Stops as soon as a tap starts a
        // new move (re-blocks input) — its own completion drains the rest. Cleared on stage (re)load.
        private void DrainTapQueue()
        {
            while (_tapQueue.Count > 0 && !_boardView.IsInputBlocked)
                HandleLaneTapped(_tapQueue.Dequeue());
        }

        private void ResetTapQueue() { _tapQueue.Clear(); _animating = false; }

        private void HandleBooster(BoosterType type)
        {
            if (TutorialManager.ActiveBlocking) return; // overlay owns input during a tutorial step
            if (_boardView.IsInputBlocked) return;
            _selectedLane = -1;
            _boardView.ClearHighlights();

            switch (type)
            {
                case BoosterType.Undo: // free, single-step — no inventory, no count; only the last move is reversible (Board.CanUndo)
                    if (_board.Undo()) { _boostersUsed = true; _boardView.RefreshAll(); UpdateSoftStuck(); }
                    break;
                case BoosterType.Shuffle:
                    ConsumeThen(type, () => { _boostersUsed = true; _board.Shuffle(); _boardView.RefreshAll(); UpdateSoftStuck(); });
                    break;
                case BoosterType.AddLane:
                    if (_board.AddLaneUsed) break; // max 1/stage (spec §4) — don't consume for a no-op
                    ConsumeThen(type, () => { if (_board.TryAddLane()) { _boostersUsed = true; _boardView.RefreshAll(); UpdateSoftStuck(); } });
                    break;
            }
        }

        // Inventory-backed booster use: spend one OWNED item (item.csv id), not gold. When the player
        // owns none, surface the IAP purchase flow instead (mock for now). Spends via the server
        // (/api/inventory/spend); the booster applies only on confirmation. Falls back to a local count
        // decrement when the inventory service is absent (e.g. InGame entered directly without Boot).
        private void ConsumeThen(BoosterType type, Action onUsed)
        {
            int itemId   = type.ItemId();
            var progress = PlayerProgressService.Instance;
            int owned    = progress != null ? progress.GetItemCount(itemId) : 0;
            if (owned <= 0) { BuyBoosterThenUse(type, onUsed); return; } // 0 owned → instant gold purchase, then use

            var inventory = InventoryApiService.Instance;
            if (inventory == null)
            {
                progress?.SetItemCount(itemId, owned - 1);
                onUsed();
                return;
            }

            _boardView.BlockInput(true);
            inventory.SpendItem(itemId, 1, BoosterReason(type),
                onSuccess: _   => { _boardView.BlockInput(false); onUsed(); },
                onError:   err => { _boardView.BlockInput(false); ShowSpendError(err); });
        }

        private static string BoosterReason(BoosterType type) => type switch
        {
            BoosterType.Shuffle => "booster_shuffle",
            BoosterType.AddLane => "booster_add_lane",
            _                   => "booster",
        };

        // Count-0 booster tap → gold purchase via a confirm popup. Shows a ConfirmDialog with the item name +
        // gold price; on confirm, requests the server (/api/inventory/buy charges item.csv price + syncs gold),
        // and only on success grants the item then spends it so the booster applies (buy→spend, no recursion →
        // no re-buy loop). Insufficient gold → toast, no popup. Undo never reaches here (free, bypasses
        // ConsumeThen). Price/affordability are data-driven (item.csv via ItemDataService) — no magic numbers.
        private void BuyBoosterThenUse(BoosterType type, Action onUsed)
        {
            int itemId   = type.ItemId();
            var progress = PlayerProgressService.Instance;
            var item     = ItemDataService.Instance != null ? ItemDataService.Instance.GetItem(itemId) : null;
            int price    = item?.price ?? 0;
            var loc      = LocalizationService.Instance;

            if (progress == null || price <= 0) return; // misconfigured / no progress service
            if (!progress.CanAfford(price))
            {
                UIManager.Instance?.ShowToast(loc != null ? loc.Get("toast.insufficient_gold") : "Insufficient Gold!", ToastType.Warning);
                return;
            }

            // Buy one (server charges gold) then spend it to apply — two explicit calls, no recursion.
            void DoPurchase()
            {
                var inventory = InventoryApiService.Instance;
                if (inventory == null) // offline/dev: local gold spend, then use (bought 1 + used 1 = net 0)
                {
                    if (progress.SpendGold(price)) onUsed();
                    return;
                }
                _boardView.BlockInput(true);
                inventory.BuyItem(itemId,
                    onSuccess: _ => inventory.SpendItem(itemId, 1, BoosterReason(type),
                        onSuccess: __  => { _boardView.BlockInput(false); onUsed(); },
                        onError:   err => { _boardView.BlockInput(false); ShowSpendError(err); }),
                    onError: err => { _boardView.BlockInput(false); ShowSpendError(err); });
            }

            var ui = UIManager.Instance;
            if (ui == null) { DoPurchase(); return; } // no UI (dev/no-Boot) → skip confirm

            string name = loc != null ? loc.Get(item.name_key) : type.ToString();
            string body = loc != null ? string.Format(loc.Get("popup.booster_buy.body_fmt"), name, price)
                                      : $"Buy {name} for {price} gold?";
            ui.ShowPopup<ConfirmDialogView>(p => p.Init(
                title:        loc != null ? loc.Get("popup.booster_buy.title") : "Buy Booster",
                body:         body,
                confirmLabel: loc != null ? loc.Get("common.btn_confirm")      : "Confirm",
                onConfirm:    DoPurchase,
                onCancel:     null,
                cancelLabel:  loc != null ? loc.Get("common.btn_cancel")       : "Cancel"));
        }

        private static void ShowSpendError(string err)
        {
            var loc = LocalizationService.Instance;
            UIManager.Instance?.ShowToast(loc != null ? loc.GetErrorFromResponse(err) : err, ToastType.Warning);
        }

        // ── Post-move resolution ─────────────────────────────────────────────

        private void PostMoveCheck()
        {
            _boardView.SetSoftStuck(false);

            if (_board.IsCleared)
            {
                _boardView.BlockInput(true);
                SfxEventBus.Play(SfxId.StageClear);
                _tapQueue.Clear(); // drop buffered taps — board is done, no phantom move after clear
                SubmitClear(_stageIndex + 1);
                return;
            }

            if (_board.IsHardStuck())
            {
                _boardView.BlockInput(true);
                _tapQueue.Clear(); // drop buffered taps — stuck panel owns input now
                _boardView.ShowStuckPanel(
                    addLaneAvailable: !_board.AddLaneUsed,
                    onAddLane: WatchAdForAddLane, // ad reward → free 1/stage (spec §5.1/§5.4)
                    onRetry:   LoadCurrent,       // restart the same stage from the top (no lobby round-trip)
                    onGiveUp:  () => BoardView.GoToScene("Lobby"));

                // Repeated-failure onboarding hint (FailRepeat). Counter resets on stage change (LoadCurrent).
                _failCount++;
                TutorialManager.NotifyFailRepeat(_failCount);
                return;
            }

            UpdateSoftStuck();
        }

        private void UpdateSoftStuck()
        {
            bool solvable = BoardSolver.IsSolvable(_board, SoftStuckNodeCap, resultOnCapExceeded: true);
            _boardView.SetSoftStuck(!solvable);

            // Soft stuck: legal moves remain but every path only revisits prior states (never clears).
            // Pulse alone is easy to miss, so nag on each move with the concrete recovery actions.
            if (!solvable)
            {
                var loc = LocalizationService.Instance;
                UIManager.Instance?.ShowToast(
                    loc != null ? loc.Get("toast.no_solution") : "No solution left — try Undo Shuffle or Restart.",
                    ToastType.Warning);
            }
        }

        // ── Stage clear (server-authoritative) ────────────────────────────────

        // Submits the clear to the server, which owns best-moves, ranking, first-clear / chapter-chest
        // rewards, and achievement progress. Applies the returned snapshot (gold synced in the API
        // service; unlock + best cached locally). Falls back to a local record when the API is absent.
        private void SubmitClear(int stageId)
        {
            int moves = _board.MoveCount;
            var completedTypes = new List<int>(_def.Types);
            for (int t = 0; t < _def.Types; t++) completedTypes.Add(t);

            // Stable id for this clear attempt; links the optional double-reward ad claim (logging).
            string attemptId = Guid.NewGuid().ToString("N");

            var api = StageApiService.Instance;
            if (api == null)
            {
                PlayerProgressService.Instance?.RecordBestMoves(stageId, moves);
                PlayerProgressService.Instance?.UnlockStage(stageId + 1);
                ShowClear(stageId, attemptId, Array.Empty<GrantedRewardDto>(), canDouble: false,
                    new ClearSummary(moves, moves, false));
                return;
            }

            api.ClearStage(stageId, moves, completedTypes, _sessionId, _boostersUsed,
                onSuccess: res =>
                {
                    var progress = PlayerProgressService.Instance;
                    progress?.RecordBestMoves(stageId, res.BestMovesUsed);
                    progress?.ApplyMaxClearedStage(res.MaxClearedStageId);
                    _boardView.SetBestMoves(res.BestMovesUsed);
                    // Double reward only on first clear (re-clears grant nothing to double).
                    bool canDouble = res.IsFirstClear && res.GrantedRewards != null && res.GrantedRewards.Count > 0;
                    ShowClear(stageId, attemptId, res.GrantedRewards, canDouble,
                        new ClearSummary(res.MovesUsed, res.BestMovesUsed, res.IsNewBest));
                },
                onError: err =>
                {
                    // Attempt-token rejected (missing/mismatched/expired start session): the clear can't
                    // be trusted and there is no valid in-progress stage. Force the player back to the
                    // lobby via a blocking popup rather than silently retrying.
                    if (ServerErrorCodes.IsStageSessionError(err)) { ShowSessionEndedAndExit(); return; }

                    // Other rejections (e.g. INVALID_STAGE_CLEAR): the clear is not valid, so cache
                    // NOTHING — no best-moves, no unlock. The board is already cleared locally, so
                    // reload the same stage to offer a fresh retry instead of a bogus clear screen.
                    ShowSpendError(err); // reuse server-error toast
                    _boardView.BlockInput(false);
                    LoadCurrent();
                });
        }

        private void ShowClear(int stageId, string attemptId, IReadOnlyList<GrantedRewardDto> rewards,
            bool canDouble, ClearSummary summary)
        {
            _boardView.ShowClearPanel(stageId, attemptId, rewards, canDouble, summary,
                onNext:  () => ShowInterstitialThen(AdvanceToNextStage),
                onLobby: () => ShowInterstitialThen(() => BoardView.GoToScene("Lobby")));
        }

        // Result-screen "Next": the next stage is a NEW attempt, so it must go through the same
        // server start gate that scene entry uses (unlock check + fresh attempt token). Only build the
        // next board once the server issues a session; otherwise its clear would be rejected.
        private void AdvanceToNextStage()
        {
            int nextIndex   = _stageIndex + 1;
            int nextStageId = nextIndex + 1; // stage_id is 1-based

            var api = StageApiService.Instance;
            if (api == null) // offline/dev (no Boot → no StageApiService): advance locally, no session
            {
                _sessionId  = null;
                _stageIndex = nextIndex;
                LoadCurrent();
                return;
            }

            _boardView.BlockInput(true);
            api.StartStage(nextStageId,
                onSuccess: res =>
                {
                    PlayerProgressService.Instance?.ApplyMaxClearedStage(res.MaxClearedStageId);
                    _sessionId  = res.SessionId;
                    _stageIndex = nextIndex;
                    _boardView.BlockInput(false);
                    LoadCurrent();
                },
                onError: err =>
                {
                    _boardView.BlockInput(false);
                    ShowSessionEndedAndExit();
                });
        }

        // Blocking popup → forced lobby return when the server has no valid stage session (clear
        // rejected, or next-stage start failed). A toast is too easy to miss for a flow-ending error.
        private void ShowSessionEndedAndExit()
        {
            void GoLobby() => BoardView.GoToScene("Lobby");
            var ui = UIManager.Instance;
            if (ui == null) { GoLobby(); return; }

            var loc = LocalizationService.Instance;
            ui.ShowPopup<ConfirmDialogView>(p => p.Init(
                title:        loc != null ? loc.Get("popup.session_invalid.title") : "Session Ended",
                body:         loc != null ? loc.Get("popup.session_invalid.body")  : "Your stage session has ended. Returning to the lobby.",
                confirmLabel: loc != null ? loc.Get("common.btn_ok")              : "OK",
                onConfirm:    GoLobby,
                onCancel:     GoLobby,
                cancelLabel:  loc != null ? loc.Get("common.btn_close")           : "Close"));
        }

        // ── Ads ──────────────────────────────────────────────────────────────

        // Stuck-popup Add Lane is granted only after a completed rewarded ad (spec §5.1). Sets the
        // suppression flag so this stage's post-stage interstitial won't also fire (§5.4). Re-runs the
        // stuck check after, so cancelling the ad re-presents the rescue popup.
        private void WatchAdForAddLane()
        {
            var ads = AdMobService.Instance;
            if (ads == null) // no ad service (e.g. InGame entered without Boot) → grant free so the loop isn't soft-locked
            {
                if (_board.TryAddLane()) { _boostersUsed = true; _boardView.RefreshAll(); }
                PostMoveCheck();
                return;
            }
            ads.WatchRewardedAd(PlacementAddLane, result =>
            {
                if (result.HasValue && result.Value.Earned)
                {
                    _adRewardedThisStage = true;
                    if (_board.TryAddLane()) { _boostersUsed = true; _boardView.RefreshAll(); }
                }
                PostMoveCheck();
            });
        }

        // Post-stage interstitial (spec §5.4): eligibility (min-stage / cooldown) is gated inside the
        // ad service; suppressed when a rewarded ad was watched this stage. Records the show server-side.
        private void ShowInterstitialThen(Action proceed)
        {
            var ads = AdMobService.Instance;
            if (ads == null) { proceed(); return; }
            int stageId = _stageIndex + 1;
            ads.ShowInterstitialIfEligible(stageId, _adRewardedThisStage, wasShown =>
            {
                if (wasShown)
                {
                    AdApiService.Instance?.RecordInterstitialShown(stageId);
                    AdEligibilityCache.Instance?.OnInterstitialShown();
                }
                proceed();
            });
        }
    }
}
