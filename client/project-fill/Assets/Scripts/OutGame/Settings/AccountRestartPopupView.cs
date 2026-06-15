using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Settings
{
    public class AccountRestartPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private Button   _confirmButton;

        private Action _onConfirm;

        public void Init(Action onConfirm)
        {
            var loc = Game.Services.LocalizationService.Instance;
            if (_titleText != null)
                _titleText.text = loc?.Get("popup.account_restart.title") ?? "Game Restart Required";
            if (_bodyText != null)
                _bodyText.text = loc?.Get("popup.account_restart.body") ?? "The game will now restart.";
            _onConfirm = onConfirm;
            _confirmButton?.onClick.AddListener(OnConfirm);
        }

        private void OnConfirm()
        {
            _onConfirm?.Invoke();
            var appear = GetComponent<Game.Core.UI.UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Game.Core.UIManager.Instance?.CloseTopPopup());
            else
                Game.Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
