using Game.Core;
using Game.Core.UI;
using Game.Services;
using ProjectFill.Contracts.Achievement;
using ProjectFill.Contracts.GameTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Achievement lobby tab: 4 category tabs (Progression/Skill/Dedication/Collection) + scroll list.
    /// Each cell shows tier badge, name/desc, progress bar, and a claim button when completed.
    /// Migrated from the former AchievementListPopupView (now a bottom-nav tab right of Ranking).
    /// </summary>
    public class AchievementTabView : MonoBehaviour
    {
        [SerializeField] private Button _progressionTab;
        [SerializeField] private Button _skillTab;
        [SerializeField] private Button _dedicationTab;
        [SerializeField] private Button _collectionTab;
        [SerializeField] private RectTransform _listContainer;
        [SerializeField] private GameObject _cellPrefab;

        [SerializeField] private Color _activeTabColor = new Color(1f, 0.145f, 0.522f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.094f, 0.094f, 0.212f);

        private static readonly Color BronzeColor = new Color(0.804f, 0.498f, 0.196f);
        private static readonly Color SilverColor = new Color(0.753f, 0.753f, 0.753f);
        private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);
        private static readonly Color PlatinumColor = new Color(0.169f, 0.851f, 0.753f);

        private AchievementCategory _currentCategory = AchievementCategory.Progression;
        private AchievementListResponse _data;

        private void Awake()
        {
            _progressionTab?.onClick.AddListener(() => Switch(AchievementCategory.Progression));
            _skillTab?.onClick.AddListener(() => Switch(AchievementCategory.Skill));
            _dedicationTab?.onClick.AddListener(() => Switch(AchievementCategory.Dedication));
            _collectionTab?.onClick.AddListener(() => Switch(AchievementCategory.Collection));
        }

        private void OnEnable() => Fetch();

        private void Fetch()
        {
            if (AchievementApiService.Instance == null) return;
            AchievementApiService.Instance.FetchList(resp =>
            {
                _data = resp;
                Rebuild();
            }, _ => { });
        }

        private void Switch(AchievementCategory category)
        {
            _currentCategory = category;
            Rebuild();
        }

        private void Rebuild()
        {
            UpdateTabColors();
            if (_listContainer == null || _cellPrefab == null || _data == null) return;

            foreach (Transform child in _listContainer)
                Destroy(child.gameObject);

            var loc = LocalizationService.Instance;
            foreach (var a in _data.Achievements)
            {
                if ((AchievementCategory)a.Category != _currentCategory) continue;

                var go = Instantiate(_cellPrefab, _listContainer);
                go.SetActive(true);
                go.name = $"Cell_{a.AchievementId}";
                BindCell(go, a, loc);
            }
        }

        private void BindCell(GameObject go, AchievementDto a, LocalizationService loc)
        {
            var badge = go.transform.Find("TierBadge")?.GetComponent<Image>();
            if (badge != null) badge.color = TierColor((AchievementTier)a.Tier);

            var rewardIcon = go.transform.Find("TierBadge/RewardIcon")?.GetComponent<Image>();
            if (rewardIcon != null)
            {
                var render = RewardDisplay.RepresentativeRewardRender(a.AchievementId);
                if (render != null)
                {
                    rewardIcon.enabled = true;
                    render(rewardIcon);
                }
                else
                {
                    rewardIcon.enabled = false;
                    rewardIcon.sprite = null;
                }
            }

            var nameText = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null) nameText.text = loc != null ? loc.Get(a.NameKey) : a.NameKey;

            var descText = go.transform.Find("DescText")?.GetComponent<TMP_Text>();
            if (descText != null) descText.text = loc != null ? loc.Get(a.DescKey) : a.DescKey;

            var bar = go.transform.Find("ProgressBar")?.GetComponent<AnimatedProgressBar>();
            if (bar != null)
            {
                float ratio = a.ConditionValue > 0 ? Mathf.Clamp01((float)a.Progress / a.ConditionValue) : (a.IsCompleted ? 1f : 0f);
                bar.SetProgress(ratio, a.IsCompleted);
            }

            var progressText = go.transform.Find("ProgressText")?.GetComponent<TMP_Text>();
            if (progressText != null && loc != null)
                progressText.text = string.Format(loc.Get("achievement.progress_fmt"), Mathf.Min(a.Progress, a.ConditionValue), a.ConditionValue);

            var claimButton = go.transform.Find("ClaimButton")?.GetComponent<Button>();
            var completedLabel = go.transform.Find("CompletedLabel");

            bool claimable = a.IsCompleted && !a.RewardClaimed;
            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(claimable);
                claimButton.onClick.RemoveAllListeners();
                var captured = a;
                claimButton.onClick.AddListener(() => OnClaim(captured));
            }
            if (completedLabel != null) completedLabel.gameObject.SetActive(a.IsCompleted && a.RewardClaimed);
        }

        private void OnClaim(AchievementDto a)
        {
            UIManager.Instance?.ShowLoading();
            AchievementApiService.Instance.Claim(a.AchievementId, resp =>
            {
                UIManager.Instance?.HideLoading();
                var rewards = RewardDisplay.Build(resp.GrantedRewards);
                rewards.AddRange(RewardDisplay.BuildCosmeticUnlocks(a.AchievementId));
                if (rewards.Count > 0)
                    UIManager.Instance?.ShowPopup<RewardPopupView>(v => v.Init(rewards));
                Fetch();
            }, err =>
            {
                UIManager.Instance?.HideLoading();
                var loc = LocalizationService.Instance;
                UIManager.Instance?.ShowToast(loc != null ? loc.GetErrorFromResponse(err) : err, ToastType.Warning);
            });
        }

        private static Color TierColor(AchievementTier tier) => tier switch
        {
            AchievementTier.Bronze => BronzeColor,
            AchievementTier.Silver => SilverColor,
            AchievementTier.Gold => GoldColor,
            AchievementTier.Platinum => PlatinumColor,
            _ => SilverColor,
        };

        private void UpdateTabColors()
        {
            SetTab(_progressionTab, _currentCategory == AchievementCategory.Progression);
            SetTab(_skillTab, _currentCategory == AchievementCategory.Skill);
            SetTab(_dedicationTab, _currentCategory == AchievementCategory.Dedication);
            SetTab(_collectionTab, _currentCategory == AchievementCategory.Collection);
        }

        private void SetTab(Button button, bool active)
        {
            if (button != null && button.targetGraphic != null)
                button.targetGraphic.color = active ? _activeTabColor : _inactiveTabColor;
        }
    }
}
