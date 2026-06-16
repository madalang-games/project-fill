using System;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // Hard Stuck rescue popup (spec 5.3). Prefab in Resources/Prefabs/UI, shown via UIManager.
    // Backdrop (DIM) provides the scrim per UI convention. Add Lane (ad) row hides once used,
    // promoting Shuffle to the primary action.
    public class StuckPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private GameObject _addLaneRow;   // ad-reward Add Lane (1st priority when available)
        [SerializeField] private Button _addLaneButton;
        [SerializeField] private Button _shuffleButton;
        [SerializeField] private Button _forfeitButton;

        public void Configure(bool addLaneAvailable, Action onAddLane, Action onShuffle, Action onGiveUp)
        {
            if (_addLaneRow != null) _addLaneRow.SetActive(addLaneAvailable);

            Bind(_addLaneButton, () => { Close(); onAddLane?.Invoke(); });
            Bind(_shuffleButton, () => { Close(); onShuffle?.Invoke(); });
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
            if (UIManager.Instance != null) UIManager.Instance.CloseTopPopup();
            else Destroy(gameObject);
        }
    }
}
