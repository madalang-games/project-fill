using Game.Core;
using Game.InGame.View;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.InGame.Controller
{
    public class InGameController : MonoBehaviour
    {
        [SerializeField] private BoardView   _boardView;
        [SerializeField] private GameObject  _itemTrayView;    // legacy field — keep for scene compat
        [SerializeField] private GameObject  _rowShiftOverlay; // legacy field — keep for scene compat
        [SerializeField] private bool        _isDevMode;

        private Board _board;
        private int   _selectedLane = -1;
        private bool  _hintActive;
        private int   _stageId;

        public void Init(Board board, int stageId)
        {
            _board        = board;
            _stageId      = stageId;
            _selectedLane = -1;
            _hintActive   = false;

            _boardView.Init(board, stageId);
            _boardView.OnLaneTapped    += HandleLaneTapped;
            _boardView.OnBoosterTapped += HandleBoosterTapped;
        }

        private void HandleLaneTapped(int laneIndex)
        {
            if (_boardView.IsInputBlocked) return;

            // Clear hint highlight on any tap
            if (_hintActive) { _hintActive = false; _boardView.ClearHighlights(); }

            if (_selectedLane == -1)
            {
                // Select
                if (_board.Lanes[laneIndex].IsEmpty) return;
                _selectedLane = laneIndex;
                _boardView.SetSelection(laneIndex);
                _boardView.SetHighlightValidLanes(laneIndex);
            }
            else if (_selectedLane == laneIndex)
            {
                // Deselect
                _selectedLane = -1;
                _boardView.ClearSelection();
            }
            else
            {
                int from = _selectedLane;
                _selectedLane = -1;
                _boardView.ClearSelection();

                if (_board.CanMoveTo(from, laneIndex))
                {
                    SignalType completedType = _board.Lanes[from].TopChip!.Value;
                    int prevSets = _board.CompletedSets;

                    _board.Move(from, laneIndex);
                    _boardView.UpdateMoves(_board.MoveCount);
                    _boardView.RefreshLane(from);

                    if (_board.CompletedSets > prevSets)
                    {
                        _boardView.PlayCompleteSetEffect(laneIndex, completedType, () =>
                        {
                            _boardView.UpdateSignalPanel(_board.CompletedSets);
                            _boardView.RefreshLane(laneIndex);
                            PostMoveCheck();
                        });
                    }
                    else
                    {
                        _boardView.RefreshLane(laneIndex);
                        PostMoveCheck();
                    }
                }
                else
                {
                    _boardView.PlayInvalidEffect(laneIndex);
                }
            }
        }

        private void PostMoveCheck()
        {
            if (_board.IsCleared)
            {
                _boardView.BlockInput(true);
                _boardView.ShowClearPanel(
                    stageId: _stageId,
                    onNext:  GoToNextStage,
                    onLobby: GoToLobby);
            }
            else if (_board.IsStuck())
            {
                _boardView.BlockInput(true);
                _boardView.ShowStuckPanel(
                    onShuffle: OnStuckShuffle,
                    onRestart: OnStuckRestart,
                    onLobby:   GoToLobby);
            }
        }

        private void HandleBoosterTapped(BoosterType type)
        {
            if (_boardView.IsInputBlocked) return;

            _selectedLane = -1;
            _boardView.ClearSelection();
            _hintActive = false;

            switch (type)
            {
                case BoosterType.Undo:
                    if (_board.Undo())
                    {
                        _boardView.RefreshAll();
                        _boardView.UpdateMoves(_board.MoveCount);
                        _boardView.UpdateSignalPanel(_board.CompletedSets);
                    }
                    break;

                case BoosterType.Hint:
                    var hint = _board.GetHint();
                    if (hint.HasValue)
                    {
                        _hintActive = true;
                        _boardView.SetHint(hint.Value.from, hint.Value.to);
                    }
                    break;

                case BoosterType.Shuffle:
                    _board.Shuffle();
                    _boardView.RefreshAll();
                    break;

                case BoosterType.AddLane:
                    if (_board.TryAddLane())
                        _boardView.AddLaneView();
                    break;
            }
        }

        private void OnStuckShuffle()
        {
            _board.Shuffle();
            _boardView.RefreshAll();
        }

        private void OnStuckRestart()
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.FadeToScene("InGame");
            else
                SceneManager.LoadScene("InGame");
        }

        private void GoToNextStage()
        {
            // For MVP: reload same scene (next stage wiring is future work)
            OnStuckRestart();
        }

        private void GoToLobby()
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.FadeToScene("Lobby");
            else
                SceneManager.LoadScene("Lobby");
        }
    }
}
