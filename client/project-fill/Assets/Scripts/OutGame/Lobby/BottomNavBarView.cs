using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public enum LobbyTab { Home, Shop, Ranking }

    public class BottomNavBarView : MonoBehaviour
    {
        [SerializeField] private Button   _homeButton;
        [SerializeField] private Button   _shopButton;
        [SerializeField] private Button   _rankingButton;
        [SerializeField] private Image    _homeHighlight;
        [SerializeField] private Image    _shopHighlight;
        [SerializeField] private Image    _rankingHighlight;

        public event Action<LobbyTab> OnTabChanged;

        private static readonly Color ActiveColor   = new Color(0.91f, 0.63f, 0.125f); // UI_CTA
        private static readonly Color InactiveColor = new Color(0.94f, 0.92f, 0.84f);  // UI_TEXT

        private void Awake()
        {
            _homeButton.onClick.AddListener(    () => SelectTab(LobbyTab.Home));
            _shopButton.onClick.AddListener(    () => SelectTab(LobbyTab.Shop));
            _rankingButton.onClick.AddListener( () => SelectTab(LobbyTab.Ranking));

            SetTabHighlight(LobbyTab.Home);
        }

        public void SelectTab(LobbyTab tab)
        {
            SetTabHighlight(tab);
            OnTabChanged?.Invoke(tab);
        }

        private void SetTabHighlight(LobbyTab active)
        {
            SetHighlight(_homeHighlight,    active == LobbyTab.Home);
            SetHighlight(_shopHighlight,    active == LobbyTab.Shop);
            SetHighlight(_rankingHighlight, active == LobbyTab.Ranking);
        }

        private static void SetHighlight(Image img, bool on)
        {
            if (img != null) img.color = on ? ActiveColor : InactiveColor;
        }
    }
}
