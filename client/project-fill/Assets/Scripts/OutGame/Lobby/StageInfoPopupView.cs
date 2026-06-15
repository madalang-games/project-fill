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
        [SerializeField] private GameObject[] _bestStarFills;
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

        public void Init(int stageId, int bestStars, int bestMoves, Action onPlay, int difficulty = 0, bool isLocked = false)
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
                string starsText = string.Format(Game.Services.LocalizationService.Instance.Get("popup.stage_info.best_stars"), bestStars);
                string movesText = string.Format(Game.Services.LocalizationService.Instance.Get("popup.stage_info.best_moves"), bestMoves);
                _bestRecord.text = $"{starsText}\n{movesText}";
            }

            for (int i = 0; i < _bestStarFills.Length; i++)
                if (_bestStarFills[i] != null) _bestStarFills[i].SetActive(i < bestStars);
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
