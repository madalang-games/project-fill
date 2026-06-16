using System.Collections.Generic;
using Game.Services;
using UnityEngine;

namespace Game.OutGame.Lobby
{
    public class LobbyBadgeContainer : MonoBehaviour
    {
        [SerializeField] private RectTransform _eventLayoutGroup; // Left layout (Events)
        [SerializeField] private RectTransform _buyLayoutGroup;   // Right layout (BM / Buy)
        [SerializeField] private GameObject _badgePrefab;         // Template Badge Item

        private readonly List<GameObject> _spawnedBadges = new List<GameObject>();
        private LobbyView _cachedLobbyView;

        private void OnEnable()
        {
            if (PlayerProgressService.Instance != null)
                PlayerProgressService.Instance.OnNoAdsChanged += OnNoAdsStateChanged;
        }

        private void OnDisable()
        {
            if (PlayerProgressService.Instance != null)
                PlayerProgressService.Instance.OnNoAdsChanged -= OnNoAdsStateChanged;
        }

        private void OnNoAdsStateChanged(bool _)
        {
            Refresh(_cachedLobbyView);
        }

        public void Refresh(LobbyView lobbyView)
        {
            _cachedLobbyView = lobbyView;

            // Clean up existing spawned badges
            foreach (var badge in _spawnedBadges)
            {
                if (badge != null)
                {
                    Destroy(badge);
                }
            }
            _spawnedBadges.Clear();

            if (_badgePrefab == null)
            {
                Debug.LogWarning("[LobbyBadgeContainer] Badge Prefab template is missing.");
                return;
            }

            // 1. BM Badges (Right Area - BuyLayoutGroup)
            // No Ads Badge: show only if NOT purchased
            if (IAPService.Instance != null && !IAPService.Instance.IsNoAdsPurchased)
            {
                SpawnBadge(
                    parent: _buyLayoutGroup,
                    iconKey: "ui_iap_no_ads",
                    labelKey: "shop.iap.no_ads.title",
                    onClick: () =>
                    {
                        if (lobbyView != null)
                        {
                            lobbyView.GoToShopTab();
                        }
                    }
                );
            }

            // 2. Event Badges (Left Area - EventLayoutGroup)
            // Daily Attendance badge → opens attendance popup
            SpawnBadge(
                parent: _eventLayoutGroup,
                iconKey: "ui_flag_icon",
                labelKey: "home.badge.attendance",
                onClick: () => Game.Core.UIManager.Instance?.ShowPopup<AttendancePopupView>()
            );

            // Daily Challenge badge → opens challenge info popup
            SpawnBadge(
                parent: _eventLayoutGroup,
                iconKey: "nav_ranking",
                labelKey: "home.badge.challenge",
                onClick: () => Game.Core.UIManager.Instance?.ShowPopup<DailyChallengePopupView>()
            );
        }

        private void SpawnBadge(RectTransform parent, string iconKey, string labelKey, System.Action onClick)
        {
            if (parent == null) return;

            var go = Instantiate(_badgePrefab, parent);
            go.SetActive(true);
            _spawnedBadges.Add(go);

            var badgeItem = go.GetComponent<LobbyBadgeItem>();
            if (badgeItem != null)
            {
                Sprite iconSprite = null;
                if (DynamicResourceService.Instance != null)
                {
                    iconSprite = DynamicResourceService.Instance.GetSprite(iconKey);
                }

                string label = labelKey;
                if (LocalizationService.Instance != null)
                {
                    label = LocalizationService.Instance.Get(labelKey);
                }

                badgeItem.Init(iconSprite, label, onClick);
            }
        }
    }
}
