using Game.Core.UI;
using Game.Services;
using Game.Utils;
using ProjectFill.Data.Generated;
using UnityEngine;

namespace Game.OutGame.Lobby
{
    public class LobbyView : MonoBehaviour
    {
        [SerializeField] private HeaderView           _header;
        [SerializeField] private BottomNavBarView     _navBar;
        [SerializeField] private GameObject           _homeTabRoot;
        [SerializeField] private GameObject           _shopTabRoot;
        [SerializeField] private GameObject           _rankingTabRoot;
        [SerializeField] private GameObject           _achievementTabRoot;
        [SerializeField] private SceneBackgroundView  _sceneBackground;

        private void Awake()
        {
            _navBar.OnTabChanged += ShowTab;
        }

        private static bool _attendanceShownThisSession;

        private void Start()
        {
            RefreshGold();
            ShowTab(LobbyTab.Home);
            FetchServerState();
            _sceneBackground?.Bind(GetCurrentBgThemeId(), BackgroundMode.Lobby);
            if (Services.Tutorial.TutorialManager.Instance != null)
            {
                Services.Tutorial.TutorialManager.Instance.CheckLobbyTriggers();
            }
            TryShowDailyAttendance();
        }

        // Auto-open the attendance popup once per session on first home entry when today's reward is unclaimed.
        private void TryShowDailyAttendance()
        {
            if (_attendanceShownThisSession || AttendanceApiService.Instance == null) return;
            _attendanceShownThisSession = true;
            AttendanceApiService.Instance.FetchStatus(status =>
            {
                if (!status.ClaimedToday)
                    Game.Core.UIManager.Instance?.ShowPopup<AttendancePopupView>();
            }, _ => { });
        }

        private void RefreshGold()
        {
            int gold = PlayerProgressService.Instance?.Gold ?? 0;
            _header?.SetGold(gold);
        }

        private void FetchServerState()
        {
            CurrencyApiService.Instance?.FetchGold();
            InventoryApiService.Instance?.FetchInventory();
        }

        private void ShowTab(LobbyTab tab)
        {
            _homeTabRoot?.SetActive(tab == LobbyTab.Home);
            _shopTabRoot?.SetActive(tab == LobbyTab.Shop);
            _rankingTabRoot?.SetActive(tab == LobbyTab.Ranking);
            _achievementTabRoot?.SetActive(tab == LobbyTab.Achievement);
            _sceneBackground?.PanTo((int)tab);
        }

        private static int GetCurrentBgThemeId()
        {
            int stageId = ScrollStateCache.LastPlayedStageId;
            if (stageId <= 0) stageId = 1;
            var stage     = StageDataService.Instance?.GetStage(stageId);
            int chapterId = stage?.chapter_id ?? 1;
            var chapters  = CsvLoader.Load<Chapter>(Chapter.ResourcePath);
            var chapter   = System.Array.Find(chapters, c => c.chapter_id == chapterId);
            return chapter != null ? (int)chapter.bg_theme_id : 1;
        }
        public void RefreshGoldDisplay()
        {
            RefreshGold();
        }

        public void GoToShopTab()
        {
            _navBar?.SelectTab(LobbyTab.Shop);
            ShowTab(LobbyTab.Shop);
        }
    }
}
