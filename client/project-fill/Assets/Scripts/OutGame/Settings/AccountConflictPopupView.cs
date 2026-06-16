using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Settings
{
    public class AccountConflictPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;

        [Header("Local Save")]
        [SerializeField] private TMP_Text _localLabel;
        [SerializeField] private TMP_Text _localStageText;
        [SerializeField] private TMP_Text _localGoldText;
        [SerializeField] private TMP_Text _localClearedText;
        [SerializeField] private TMP_Text _localItemsText;
        [SerializeField] private Button   _keepLocalButton;

        [Header("Cloud Save")]
        [SerializeField] private TMP_Text _cloudLabel;
        [SerializeField] private TMP_Text _cloudStageText;
        [SerializeField] private TMP_Text _cloudGoldText;
        [SerializeField] private TMP_Text _cloudClearedText;
        [SerializeField] private TMP_Text _cloudItemsText;
        [SerializeField] private Button   _keepCloudButton;

        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _backdropButton;
        [SerializeField] private Button _closeButton;

        public void Init(
            int localMaxStage, long localGold, int localCleared, int localItems,
            int cloudMaxStage, long cloudGold, int cloudCleared, int cloudItems,
            Action onKeepLocal, Action onKeepCloud)
        {
            var loc = Game.Services.LocalizationService.Instance;

            if (_titleText != null)
                _titleText.text = loc?.Get("popup.account_conflict.title") ?? "Account Data Conflict";
            if (_bodyText != null)
                _bodyText.text = loc?.Get("popup.account_conflict.body") ?? "Choose which data to keep.";

            if (_localLabel != null)
                _localLabel.text = loc?.Get("popup.account_conflict.local_label") ?? "Current Data";
            if (_cloudLabel != null)
                _cloudLabel.text = loc?.Get("popup.account_conflict.cloud_label") ?? "Google Account Data";

            string stageFmt   = loc?.Get("popup.account_conflict.stage_fmt")   ?? "Stage {0}";
            string goldFmt    = loc?.Get("popup.account_conflict.gold_fmt")    ?? "Gold: {0}";
            string clearedFmt = loc?.Get("popup.account_conflict.cleared_fmt") ?? "Cleared: {0}";
            string itemsFmt   = loc?.Get("popup.account_conflict.items_fmt")   ?? "Items: {0}";

            if (_localStageText   != null) _localStageText.text   = string.Format(stageFmt,   localMaxStage);
            if (_localGoldText    != null) _localGoldText.text    = string.Format(goldFmt,    localGold);
            if (_localClearedText != null) _localClearedText.text = string.Format(clearedFmt, localCleared);
            if (_localItemsText   != null) _localItemsText.text   = string.Format(itemsFmt,   localItems);

            if (_cloudStageText   != null) _cloudStageText.text   = string.Format(stageFmt,   cloudMaxStage);
            if (_cloudGoldText    != null) _cloudGoldText.text    = string.Format(goldFmt,    cloudGold);
            if (_cloudClearedText != null) _cloudClearedText.text = string.Format(clearedFmt, cloudCleared);
            if (_cloudItemsText   != null) _cloudItemsText.text   = string.Format(itemsFmt,   cloudItems);

            _keepLocalButton?.onClick.AddListener(() => { onKeepLocal?.Invoke(); Close(); });
            _keepCloudButton?.onClick.AddListener(() => { onKeepCloud?.Invoke(); Close(); });
            _cancelButton?.onClick.AddListener(Close);
            _backdropButton?.onClick.AddListener(Close);
            _closeButton?.onClick.AddListener(Close);
        }

        private void Close()
        {
            var appear = GetComponent<Game.Core.UI.UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Game.Core.UIManager.Instance?.CloseTopPopup());
            else
                Game.Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
