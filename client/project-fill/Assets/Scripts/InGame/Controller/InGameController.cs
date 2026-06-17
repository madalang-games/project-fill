using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.UI;
using Game.InGame.View;
using Game.OutGame.Settings;
using Game.Services;
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

        private Board _board;
        private StageDefinition _def;
        private int  _stageIndex;
        private int  _selectedLane = -1;
        private bool _subscribed;
        private bool _adRewardedThisStage; // §5.4: set on rewarded-ad watch; suppresses this stage's interstitial; reset on stage enter
        private bool _challenge;           // daily-challenge mode (board from ChallengeContext seed, separate clear submit)
        private float _challengeStart;     // realtime seconds at challenge board load → clear time

        public void Begin(int startIndex)
        {
            _challenge  = ChallengeContext.Active; // consume-once: clear the flag so a later campaign entry isn't hijacked
            ChallengeContext.Clear();
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
            LoadCurrent();
        }

        private void HandlePause()
        {
            if (_boardView.IsInputBlocked) return;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPopup<PausePopupView>(p =>
                {
                    p.Configure(
                        onResume: () => {},
                        onRestart: LoadCurrent,
                        onStageSelect: () => BoardView.GoToScene("Lobby")
                    );
                });
            }
        }

        private void LoadCurrent()
        {
            if (_challenge) { LoadChallenge(); return; }
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
            _adRewardedThisStage = false; // new stage → clear the rewarded-ad / interstitial-suppression flag
            _boardView.Init(_board, _def);
            var progress = Game.Services.PlayerProgressService.Instance;
            _boardView.SetBestMoves(progress != null ? progress.GetBestMoves(_stageIndex + 1) : 0);
            UpdateSoftStuck();
        }

        // Daily challenge: board comes from the server-provided seed/params (identical worldwide).
        private void LoadChallenge()
        {
            _def   = ChallengeContext.BuildDefinition();
            _board = BoardFactory.Generate(_def);
            if (_board == null) { _def = StageLibrary.Get(0); _board = BoardFactory.Generate(_def); }
            _selectedLane = -1;
            _adRewardedThisStage = false;
            _challengeStart = Time.realtimeSinceStartup;
            _boardView.Init(_board, _def);
            _boardView.SetBestMoves(0); // challenge has no per-stage personal best surface
            UpdateSoftStuck();
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
                Name          = $"STAGE {s.chapter_id}-{s.stage_order}",
                Chapter       = s.chapter_id,
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
            if (_boardView.IsInputBlocked) return;

            if (_selectedLane == -1)
            {
                var src = _board.Lanes[lane];
                if (src.IsEmpty || src.Pending || src.Locked) return; // nothing selectable
                _selectedLane = lane;
                _boardView.SetSelection(lane);
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
                    _boardView.AnimateMove(from, lane, chip, count, destBase, absorbed, () =>
                    {
                        _boardView.BlockInput(false);
                        PostMoveCheck();
                    });
                }
                else
                {
                    _boardView.ClearHighlights();
                    _boardView.PlayInvalid(lane);
                }
            }
        }

        private void HandleBooster(BoosterType type)
        {
            if (_boardView.IsInputBlocked) return;
            _selectedLane = -1;
            _boardView.ClearHighlights();

            switch (type)
            {
                case BoosterType.Undo: // free + unlimited (spec §4)
                    if (_board.Undo()) { _boardView.RefreshAll(); UpdateSoftStuck(); }
                    break;
                case BoosterType.Shuffle:
                    SpendThen(type, () => { _board.Shuffle(); _boardView.RefreshAll(); UpdateSoftStuck(); });
                    break;
                case BoosterType.AddLane:
                    if (_board.AddLaneUsed) break; // max 1/stage (spec §4) — don't charge for a no-op
                    SpendThen(type, () => { if (_board.TryAddLane()) { _boardView.RefreshAll(); UpdateSoftStuck(); } });
                    break;
            }
        }

        // Server-authoritative gold spend (spec §4 prices live in item.csv). Pre-checks the local
        // balance for instant feedback, then POSTs /api/currency/spend; the booster applies only on
        // server confirmation. Falls back to a local deduction when the currency service is absent
        // (e.g. InGame scene entered directly without Boot) so dev play still works.
        private void SpendThen(BoosterType type, Action onPaid)
        {
            int cost = BoosterCost(type);
            var progress = PlayerProgressService.Instance;
            if (progress != null && !progress.CanAfford(cost)) { ShowInsufficientGold(); return; }

            var currency = CurrencyApiService.Instance;
            if (currency == null)
            {
                if (progress != null && !progress.SpendGold(cost)) { ShowInsufficientGold(); return; }
                onPaid();
                return;
            }

            _boardView.BlockInput(true);
            currency.SpendGold(cost, BoosterReason(type),
                onSuccess: _   => { _boardView.BlockInput(false); onPaid(); },
                onError:   err => { _boardView.BlockInput(false); ShowSpendError(err); });
        }

        private static int BoosterCost(BoosterType type)
        {
            var items = Game.Utils.CsvLoader.Load<ProjectFill.Data.Generated.Item>(
                ProjectFill.Data.Generated.Item.ResourcePath);
            int id = type.ItemId();
            if (items != null)
                foreach (var it in items)
                    if (it.id == id) return it.price;
            return 0;
        }

        private static string BoosterReason(BoosterType type) => type switch
        {
            BoosterType.Shuffle => "booster_shuffle",
            BoosterType.AddLane => "booster_add_lane",
            _                   => "booster",
        };

        private static void ShowInsufficientGold()
        {
            var loc = LocalizationService.Instance;
            UIManager.Instance?.ShowToast(loc != null ? loc.Get("toast.insufficient_gold") : "Insufficient Gold!", ToastType.Warning);
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
                if (_challenge) SubmitChallengeClear();
                else            SubmitClear(_stageIndex + 1);
                return;
            }

            if (_board.IsHardStuck())
            {
                _boardView.BlockInput(true);
                _boardView.ShowStuckPanel(
                    addLaneAvailable: !_board.AddLaneUsed,
                    onAddLane: WatchAdForAddLane, // ad reward → free 1/stage (spec §5.1/§5.4)
                    onShuffle: () => SpendThen(BoosterType.Shuffle, () => { _board.Shuffle(); _boardView.RefreshAll(); PostMoveCheck(); }),
                    onGiveUp:  () => BoardView.GoToScene("Lobby"));
                return;
            }

            UpdateSoftStuck();
        }

        private void UpdateSoftStuck()
        {
            bool solvable = BoardSolver.IsSolvable(_board, SoftStuckNodeCap, resultOnCapExceeded: true);
            _boardView.SetSoftStuck(!solvable);
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

            api.ClearStage(stageId, moves, completedTypes,
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
                    PlayerProgressService.Instance?.RecordBestMoves(stageId, moves);
                    PlayerProgressService.Instance?.UnlockStage(stageId + 1);
                    ShowSpendError(err); // reuse server-error toast
                    ShowClear(stageId, attemptId, Array.Empty<GrantedRewardDto>(), canDouble: false,
                        new ClearSummary(moves, moves, false));
                });
        }

        private void ShowClear(int stageId, string attemptId, IReadOnlyList<GrantedRewardDto> rewards,
            bool canDouble, ClearSummary summary)
        {
            _boardView.ShowClearPanel(stageId, attemptId, rewards, canDouble, summary,
                onNext:  () => ShowInterstitialThen(() => { _stageIndex++; LoadCurrent(); }),
                onLobby: () => ShowInterstitialThen(() => BoardView.GoToScene("Lobby")));
        }

        // Daily-challenge clear: server records moves + clear time for the global ranking (one attempt/day).
        // No "next stage" — both popup actions return to the lobby.
        private void SubmitChallengeClear()
        {
            int moves   = _board.MoveCount;
            int seconds = System.Math.Max(0, Mathf.RoundToInt(Time.realtimeSinceStartup - _challengeStart));
            DailyChallengeApiService.Instance?.SubmitClear(moves, seconds, _ => { }, _ => { });
            // Daily challenge has no per-stage clear reward group → no reward list, no double-reward.
            _boardView.ShowClearPanel(0, string.Empty, Array.Empty<GrantedRewardDto>(), canDouble: false,
                summary: null, onNext: ReturnFromChallenge, onLobby: ReturnFromChallenge);
        }

        private void ReturnFromChallenge()
        {
            ChallengeContext.Clear();
            BoardView.GoToScene("Lobby");
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
                if (_board.TryAddLane()) _boardView.RefreshAll();
                PostMoveCheck();
                return;
            }
            ads.WatchRewardedAd(PlacementAddLane, result =>
            {
                if (result.HasValue && result.Value.Earned)
                {
                    _adRewardedThisStage = true;
                    if (_board.TryAddLane()) _boardView.RefreshAll();
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
