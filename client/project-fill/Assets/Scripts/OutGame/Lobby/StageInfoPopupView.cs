using System;
using Game.Core;
using Game.Core.UI;
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
        [SerializeField] private TMP_Text   _difficultyLabel;
        [SerializeField] private TMP_Text   _typesLabel;
        [SerializeField] private Button     _playButton;
        [SerializeField] private Button     _backdropButton;
        [SerializeField] private Image      _ribbonImage;

        // Gimmick badges: icon-only, long-press shows ItemTooltipView. Toggled per stage gimmick presence.
        [SerializeField] private LongPressTooltipTrigger _overloadBadge;
        [SerializeField] private LongPressTooltipTrigger _relayBadge;
        [SerializeField] private LongPressTooltipTrigger _lockLaneBadge;
        [SerializeField] private LongPressTooltipTrigger _blindLaneBadge;

        // lane_kinds glyphs (see shared/datas/stage/AGENTS.md): N=Normal, L=Locked, B=Blind.
        private const char LockedLaneGlyph = 'L';
        private const char BlindLaneGlyph  = 'B';
        private const int  NoOverload      = -1;

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

            var stage = StageDataService.Instance?.GetStage(stageId);
            ApplyStageDetails(difficulty, stage);
        }

        // Difficulty label + signal-type count + gimmick badges, all sourced from stage.csv.
        private void ApplyStageDetails(int difficulty, ProjectFill.Data.Generated.Stage stage)
        {
            if (_difficultyLabel != null)
                _difficultyLabel.text = LocalizationService.Instance.Get(DifficultyKey(difficulty));

            if (_typesLabel != null && stage != null)
                _typesLabel.text = string.Format(LocalizationService.Instance.Get("popup.stage_info.types_fmt"), stage.types);

            string laneKinds = stage?.lane_kinds ?? string.Empty;
            SetBadge(_overloadBadge,  stage != null && stage.overload_type != NoOverload, "gimmick.overload_chip.name", "gimmick.overload_chip.desc");
            SetBadge(_relayBadge,     stage != null && !string.IsNullOrEmpty(stage.relay_order), "gimmick.relay_node.name", "gimmick.relay_node.desc");
            SetBadge(_lockLaneBadge,  laneKinds.IndexOf(LockedLaneGlyph) >= 0, "gimmick.locked_lane.name", "gimmick.locked_lane.desc");
            SetBadge(_blindLaneBadge, laneKinds.IndexOf(BlindLaneGlyph)  >= 0, "gimmick.blind_lane.name", "gimmick.blind_lane.desc");
        }

        private static void SetBadge(LongPressTooltipTrigger badge, bool present, string nameKey, string descKey)
        {
            if (badge == null) return;
            badge.gameObject.SetActive(present);
            if (present) badge.SetTooltip(nameKey, descKey);
        }

        private static string DifficultyKey(int difficulty)
        {
            switch (difficulty)
            {
                case 1:  return "popup.stage_info.difficulty_normal";
                case 2:  return "popup.stage_info.difficulty_hard";
                default: return "popup.stage_info.difficulty_easy";
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
