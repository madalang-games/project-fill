using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class LobbyBadgeItem : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _labelXml;
        [SerializeField] private Button _clickButton;

        private Action _onClickAction;

        private void Awake()
        {
            if (_clickButton != null)
            {
                _clickButton.onClick.AddListener(OnClicked);
            }
        }

        public void Init(Sprite icon, string label, Action onClick)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.preserveAspect = true;
            }

            if (_labelXml != null)
            {
                _labelXml.text = label;
            }

            _onClickAction = onClick;
        }

        private void OnClicked()
        {
            _onClickAction?.Invoke();
        }
    }
}
