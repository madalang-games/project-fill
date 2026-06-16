using Game.InGame.View;
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
            if (!_subscribed)
            {
                _boardView.OnLaneTapped    += HandleLaneTapped;
                _boardView.OnBoosterTapped += HandleBooster;
                _boardView.OnChapterCycle  += () => { _stageIndex++; LoadCurrent(); };
                _boardView.OnRestart       += LoadCurrent;
                _boardView.OnBack          += () => BoardView.GoToScene("Lobby");
                _subscribed = true;
            }
            LoadCurrent();
        }

        private void LoadCurrent()
        {
            _def   = StageLibrary.Get(_stageIndex);
            _board = BoardFactory.Generate(_def);
            _selectedLane = -1;
            _boardView.Init(_board, _def);
            UpdateSoftStuck();
        }

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
                    var chip    = _board.Lanes[from].TopChip!.Value;
                    int srcSlot = _board.Lanes[from].Count - 1;
                    var absorbed = _board.Move(from, lane);

                    _boardView.BlockInput(true);
                    _boardView.AnimateMove(from, lane, chip, srcSlot, absorbed, () =>
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
