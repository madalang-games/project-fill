using System;
using Game.Core;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class StageInfoPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text   _stageTitle;
        [SerializeField] private TMP_Text   _bestRecord;
        [SerializeField] private Button     _playButton;
        [SerializeField] private Button     _backdropButton;
        [SerializeField] private Image      _ribbonImage;

        private int    _stageId;
        private Action _onPlay;
        private Color  _defaultRibbonColor;

        private void Awake()
        {
            _playButton.onClick.AddListener(OnPlay);
            if (_backdropButton != null) _backdropButton.onClick.AddListener(OnClose);
            if (_ribbonImage != null) _defaultRibbonColor = _ribbonImage.color;
        }

        public void Init(int stageId, int bestMoves, Action onPlay, int difficulty = 0, bool isLocked = false)
        {
            _stageId   = stageId;
            _onPlay    = onPlay;
            _playButton.interactable = !isLocked;

            if (_ribbonImage != null)
                _ribbonImage.color = DifficultyStyle.Get(difficulty, _defaultRibbonColor);

            if (_stageTitle != null)
                _stageTitle.text = string.Format(Game.Services.LocalizationService.Instance.Get("popup.stage_info.title"), stageId);
            if (_bestRecord != null)
            {
                // Best moves is the campaign ranking metric (lower is better); 0 = no record yet.
                _bestRecord.text = bestMoves > 0
                    ? string.Format(Game.Services.LocalizationService.Instance.Get("popup.stage_info.best_moves"), bestMoves)
                    : "-";
            }
        }

        private void OnPlay()
        {
            ScrollStateCache.LastPlayedStageId = _stageId;
            _onPlay?.Invoke();
            Close();
        }

        private void OnClose() => Close();

        private void Close()
        {
            var appear = GetComponent<Core.UI.UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Game.Core.UIManager.Instance?.CloseTopPopup());
            else
                Game.Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
