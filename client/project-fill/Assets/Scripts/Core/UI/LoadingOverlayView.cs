using System.Collections;
using Game.Services;
using TMPro;
using UnityEngine;

namespace Game.Core.UI
{
    public class LoadingOverlayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private float _dotInterval = 0.4f;

        private Coroutine _timeoutCoroutine;
        private Coroutine _dotCoroutine;

        private void OnEnable()
        {
            if (_messageText != null) _messageText.gameObject.SetActive(false);
            _timeoutCoroutine = StartCoroutine(TimeoutWatch());
        }

        private void OnDisable()
        {
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            if (_dotCoroutine != null) StopCoroutine(_dotCoroutine);
        }

        public void Show(string message = null)
        {
            gameObject.SetActive(true);
            if (_messageText == null) return;

            if (_dotCoroutine != null) StopCoroutine(_dotCoroutine);

            if (string.IsNullOrEmpty(message))
            {
                _messageText.gameObject.SetActive(true);
                _dotCoroutine = StartCoroutine(DotAnimation());
            }
            else
            {
                _messageText.text = message;
                _messageText.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator DotAnimation()
        {
            int dots = 0;
            while (true)
            {
                dots = (dots % 3) + 1;
                string loadingBase = LocalizationService.Instance?.Get("common.loading") ?? "Loading";
                _messageText.text = loadingBase + new string('.', dots);
                yield return new WaitForSeconds(_dotInterval);
            }
        }

        private IEnumerator TimeoutWatch()
        {
            yield return new WaitForSeconds(Game.Core.GameConfig.LoadingTimeoutSec);
            UIManager.Instance?.ShowNetworkError(null);
        }
    }
}
