using System;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    public class PausePopupView : MonoBehaviour
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _stageSelectButton;
        [SerializeField] private Button _closeButton;

        public void Configure(Action onResume, Action onRestart, Action onStageSelect)
        {
            Bind(_resumeButton, () => { Close(); onResume?.Invoke(); });
            Bind(_closeButton, () => { Close(); onResume?.Invoke(); });
            Bind(_restartButton, () => { Close(); onRestart?.Invoke(); });
            Bind(_stageSelectButton, () => { Close(); onStageSelect?.Invoke(); });
        }

        private static void Bind(Button btn, Action action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => action());
        }

        private void Close()
        {
            if (UIManager.Instance != null) UIManager.Instance.CloseTopPopup();
            else Destroy(gameObject);
        }
    }
}
