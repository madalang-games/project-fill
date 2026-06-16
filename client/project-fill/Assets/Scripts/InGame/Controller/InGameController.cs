using Game.Core;
using Game.InGame.View;
using Game.OutGame.Settings;
using UnityEngine;

namespace Game.InGame.Controller
{
    // Orchestrates the Signal Sort loop: selection, moves, boosters, stuck handling,
    // and cycling through the per-chapter sample stages so every gimmick can be verified.
    public class InGameController : MonoBehaviour
    {
        private const int SoftStuckNodeCap = 8000;

        [SerializeField] private BoardView _boardView;
        [SerializeField] private bool      _isDevMode; // kept for scene compatibility

        private Board _board;
        private StageDefinition _def;
        private int  _stageIndex;
        private int  _selectedLane = -1;
        private bool _subscribed;

        public void Begin(int startIndex)
        {
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
            _boardView.Init(_board, _def);
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
                case BoosterType.Undo:
                    if (_board.Undo()) { _boardView.RefreshAll(); UpdateSoftStuck(); }
                    break;
                case BoosterType.Shuffle:
                    _board.Shuffle(); _boardView.RefreshAll(); UpdateSoftStuck();
                    break;
                case BoosterType.AddLane:
                    if (_board.TryAddLane()) { _boardView.RefreshAll(); UpdateSoftStuck(); }
                    break;
            }
        }

        // ── Post-move resolution ─────────────────────────────────────────────

        private void PostMoveCheck()
        {
            _boardView.SetSoftStuck(false);

            if (_board.IsCleared)
            {
                _boardView.BlockInput(true);
                _boardView.ShowClearPanel(
                    onNext:  () => { _stageIndex++; LoadCurrent(); },
                    onLobby: () => BoardView.GoToScene("Lobby"));
                return;
            }

            if (_board.IsHardStuck())
            {
                _boardView.BlockInput(true);
                _boardView.ShowStuckPanel(
                    addLaneAvailable: !_board.AddLaneUsed,
                    onAddLane: () => { _board.TryAddLane(); _boardView.RefreshAll(); PostMoveCheck(); },
                    onShuffle: () => { _board.Shuffle();    _boardView.RefreshAll(); PostMoveCheck(); },
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
    }
}
