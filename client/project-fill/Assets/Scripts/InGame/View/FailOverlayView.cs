using System;
using Game.Core;
using Game.Core.UI;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // Hard-Stuck rescue overlay (spec §5.3). Offers Add Lane (rewarded ad), Shuffle (gold), or
    // Forfeit. The only failure path in Signal Sort is being stuck, so this is the "fail" surface.
    // Shown via UIManager; backdrop DIM provides the scrim. Add Lane row hides once used, promoting
    // Shuffle to the primary action. Add Lane carries a "Watch Ad" badge; Shuffle spends gold so it
    // gates on an in-panel confirm step (no spend until the player confirms).
    public class FailOverlayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text  _titleText;
        [SerializeField] private GameObject _addLaneRow;     // ad-reward Add Lane (1st priority when available)
        [SerializeField] private Button    _addLaneButton;
        [SerializeField] private Button    _shuffleButton;
        [SerializeField] private TMP_Text  _shuffleCostText; // gold price beside the Shuffle icon
        [SerializeField] private Button    _forfeitButton;

        [Header("Shuffle confirm (gold spend gate)")]
        [SerializeField] private GameObject _shuffleConfirmPanel; // hidden until Shuffle is tapped
        [SerializeField] private TMP_Text  _shuffleConfirmBody;   // "Spend {0} Gold to shuffle?" (runtime-formatted)
        [SerializeField] private Button    _shuffleConfirmYes;
        [SerializeField] private Button    _shuffleConfirmNo;

        private Action _onShuffle;

        public void Configure(bool addLaneAvailable, Action onAddLane, Action onShuffle, Action onGiveUp)
        {
            _onShuffle = onShuffle;
            if (_addLaneRow != null) _addLaneRow.SetActive(addLaneAvailable);
            if (_shuffleCostText != null) _shuffleCostText.text = ShufflePrice().ToString();
            if (_shuffleConfirmPanel != null) _shuffleConfirmPanel.SetActive(false);

            Bind(_addLaneButton, () => { Close(); onAddLane?.Invoke(); });
            Bind(_shuffleButton, ShowShuffleConfirm);
            Bind(_forfeitButton, () => { Close(); onGiveUp?.Invoke(); });
            Bind(_shuffleConfirmYes, () => { Close(); _onShuffle?.Invoke(); });
            Bind(_shuffleConfirmNo,  HideShuffleConfirm);
        }

        // Shows the in-panel spend confirm (kept inside this popup to avoid a second UIManager popup
        // racing on the close stack). Falls back to a direct spend if the confirm panel is unwired.
        private void ShowShuffleConfirm()
        {
            if (_shuffleConfirmPanel == null) { Close(); _onShuffle?.Invoke(); return; }
            if (_shuffleConfirmBody != null)
            {
                int price = ShufflePrice();
                var loc = LocalizationService.Instance;
                _shuffleConfirmBody.text = loc != null
                    ? string.Format(loc.Get("popup.fail.shuffle_confirm_fmt"), price)
                    : $"Spend {price} Gold to shuffle the board?";
            }
            _shuffleConfirmPanel.SetActive(true);
        }

        private void HideShuffleConfirm()
        {
            if (_shuffleConfirmPanel != null) _shuffleConfirmPanel.SetActive(false);
        }

        // Shuffle gold price from item.csv (spec §4), mirroring the booster bar.
        private static int ShufflePrice()
        {
            var items = Game.Utils.CsvLoader.Load<ProjectFill.Data.Generated.Item>(
                ProjectFill.Data.Generated.Item.ResourcePath);
            int id = BoosterType.Shuffle.ItemId();
            if (items != null)
                foreach (var it in items)
                    if (it.id == id) return it.price;
            return 0;
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
