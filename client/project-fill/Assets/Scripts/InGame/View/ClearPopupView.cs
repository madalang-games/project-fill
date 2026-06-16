using System;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // Stage Clear popup. Prefab in Resources/Prefabs/UI, shown via UIManager.
    // Backdrop (DIM) provides the scrim per UI convention.
    public class ClearPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _movesText;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _lobbyButton;

        public void Configure(int moves, Action onNext, Action onLobby)
        {
            if (_movesText != null) _movesText.text = moves.ToString();

            Bind(_nextButton,  () => { Close(); onNext?.Invoke(); });
            Bind(_lobbyButton, () => { Close(); onLobby?.Invoke(); });
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
