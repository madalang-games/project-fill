using System;
using Game.Core;
using Game.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // Hard-Stuck rescue overlay (spec §5.3). The only failure path in Signal Sort is being stuck, so
    // this is the "fail" surface. Offers Add Lane (rewarded ad) to continue the run, or restart from
    // the top (Retry) / give up to lobby (Forfeit) — there is no gold-spend rescue. Shown via
    // UIManager; backdrop DIM provides the scrim. The Add Lane row hides once used (1/stage), leaving
    // Retry/Forfeit as the only paths. Add Lane carries a "Watch Ad" badge (granted by ad, not gold).
    public class FailOverlayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text  _titleText;
        [SerializeField] private GameObject _addLaneRow;     // ad-reward Add Lane (1st priority when available)
        [SerializeField] private Button    _addLaneButton;
        [SerializeField] private Button    _retryButton;      // restart the same stage from the top
        [SerializeField] private Button    _forfeitButton;

        public void Configure(bool addLaneAvailable, Action onAddLane, Action onRetry, Action onGiveUp)
        {
            if (_addLaneRow != null) _addLaneRow.SetActive(addLaneAvailable);

            Bind(_addLaneButton, () => { Close(); onAddLane?.Invoke(); });
            Bind(_retryButton,   () => { Close(); onRetry?.Invoke(); });
            Bind(_forfeitButton, () => { Close(); onGiveUp?.Invoke(); });
        }

        private static void Bind(Button btn, Action action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => action());
        }

        private void Close()
        {
            var appear = GetComponent<UIPanelAppear>();
            if (appear != null) appear.Disappear(() => UIManager.Instance?.CloseTopPopup());
            else UIManager.Instance?.CloseTopPopup();
        }
    }
}
