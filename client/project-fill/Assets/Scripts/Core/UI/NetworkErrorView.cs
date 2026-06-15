using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    public class NetworkErrorView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Button   _retryButton;

        private Action _onRetry;
        private int    _failureCount;

        private const string DefaultMessage  = "Check your network connection.";
        private const string PersistentMessage = "Please try again later.";
        private const int    PersistentThreshold = 3;

        private void Awake()
        {
            _retryButton.onClick.AddListener(OnRetry);
        }

        public void Show(Action onRetry)
        {
            _failureCount++;
            _onRetry = onRetry;
            gameObject.SetActive(true);
            if (_messageText != null)
                _messageText.text = _failureCount >= PersistentThreshold ? PersistentMessage : DefaultMessage;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnRetry()
        {
            Hide();
            _onRetry?.Invoke();
        }
    }
}
