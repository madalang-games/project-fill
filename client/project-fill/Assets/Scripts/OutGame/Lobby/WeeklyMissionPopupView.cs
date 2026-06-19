using System.Linq;
using Game.Core;
using Game.Core.UI;
using Game.Services;
using ProjectFill.Contracts.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Weekly Mission Event popup (replaces the old daily-challenge popup): 5-mission checklist + EP gauge
    /// + reward-track milestone summary + claim CTA for the lowest reached-but-unclaimed milestone.
    /// Progress is server-aggregated in the stage-clear flow; this popup only reads + claims (design §6.2).
    /// </summary>
    public class WeeklyMissionPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _daysLeftText;
        [SerializeField] private TMP_Text _epText;
        [SerializeField] private RectTransform _epBarFill;
        [SerializeField] private RectTransform _trackMarkerContainer;
        [SerializeField] private Transform _missionContainer;
        [SerializeField] private GameObject _missionCellPrefab;
        [SerializeField] private Button _claimButton;
        [SerializeField] private TMP_Text _claimLabel;
        [SerializeField] private Button _closeButton;

        private const float CompletedDimAlpha = 220f / 255f; // completed cell dims to ~0.86 (badge excluded)

        // Track marker tints (UI palette is editor-only; reached mirrors UI_SUCCESS, unreached is dim slate).
        private static readonly Color ReachedColor = new Color(0.024f, 0.839f, 0.627f);
        private static readonly Color UnreachedColor = new Color(0.42f, 0.42f, 0.52f);

        private int _claimableThreshold = -1;

        private void Awake()
        {
            _claimButton?.onClick.AddListener(OnClaim);
            _closeButton?.onClick.AddListener(Close);
        }

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            if (WeeklyMissionApiService.Instance == null) return;
            WeeklyMissionApiService.Instance.FetchStatus(Bind, _ => { });
        }

        private void Bind(WeeklyMissionResponse res)
        {
            var loc = LocalizationService.Instance;
            if (_titleText != null && loc != null) _titleText.text = loc.Get("popup.weekly_mission.title");
            if (_daysLeftText != null && loc != null)
                _daysLeftText.text = string.Format(loc.Get("popup.weekly_mission.days_left_fmt"), res.DaysRemaining);

            int maxThreshold = res.Track != null && res.Track.Count > 0 ? res.Track.Max(t => t.EpThreshold) : 0;
            if (_epText != null && loc != null)
                _epText.text = string.Format(loc.Get("popup.weekly_mission.ep_fmt"), res.TotalEp, maxThreshold);
            if (_epBarFill != null)
            {
                float ratio = maxThreshold > 0 ? Mathf.Clamp01((float)res.TotalEp / maxThreshold) : 0f;
                _epBarFill.anchorMax = new Vector2(ratio, 1f);
            }

            BuildTrackMarkers(res, maxThreshold);
            BuildMissionRows(res, loc);
            BuildClaim(res, loc);
        }

        // Spawns one marker per milestone, anchored at threshold/maxThreshold along the bar so each tick
        // lines up with the EP fill edge (container shares the bar's rect, matching the fill coordinate space).
        private void BuildTrackMarkers(WeeklyMissionResponse res, int maxThreshold)
        {
            if (_trackMarkerContainer == null || res.Track == null || maxThreshold <= 0) return;
            for (int i = _trackMarkerContainer.childCount - 1; i >= 0; i--)
                Destroy(_trackMarkerContainer.GetChild(i).gameObject);

            var font = LocalizationService.Instance?.GetFont(LocalizationService.Instance.CurrentLanguage);
            foreach (var t in res.Track.OrderBy(t => t.EpThreshold))
            {
                float r = Mathf.Clamp01((float)t.EpThreshold / maxThreshold);

                var marker = new GameObject("Marker", typeof(RectTransform));
                var mrt = (RectTransform)marker.transform;
                mrt.SetParent(_trackMarkerContainer, false);
                mrt.anchorMin = mrt.anchorMax = new Vector2(r, 0.5f);
                mrt.pivot = new Vector2(0.5f, 0.5f);
                mrt.anchoredPosition = Vector2.zero; mrt.sizeDelta = Vector2.zero;

                var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
                var drt = (RectTransform)dot.transform;
                drt.SetParent(mrt, false);
                drt.sizeDelta = new Vector2(18, 18); drt.anchoredPosition = Vector2.zero;
                var dotImg = dot.GetComponent<Image>();
                dotImg.color = t.IsReached ? ReachedColor : UnreachedColor;
                dotImg.raycastTarget = false;

                var label = new GameObject("Label", typeof(RectTransform));
                var lrt = (RectTransform)label.transform;
                lrt.SetParent(mrt, false);
                lrt.sizeDelta = new Vector2(96, 36); lrt.anchoredPosition = new Vector2(0, -34);
                var tmp = label.AddComponent<TextMeshProUGUI>();
                tmp.text = t.EpThreshold.ToString();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                tmp.enableAutoSizing = true; tmp.fontSizeMin = 24; tmp.fontSizeMax = 30; tmp.fontSize = 30;
                tmp.color = t.IsReached ? ReachedColor : UnreachedColor;
                if (font != null) tmp.font = font;
            }
        }

        private void BuildMissionRows(WeeklyMissionResponse res, LocalizationService loc)
        {
            if (_missionContainer == null || _missionCellPrefab == null) return;
            for (int i = _missionContainer.childCount - 1; i >= 0; i--)
                Destroy(_missionContainer.GetChild(i).gameObject);
            if (res.Missions == null) return;

            // Incomplete first (closest-to-done on top), completed sink to the bottom.
            var ordered = res.Missions
                .OrderBy(m => m.IsCompleted)
                .ThenByDescending(m => m.TargetValue > 0 ? (float)m.Progress / m.TargetValue : 0f);

            foreach (var m in ordered)
            {
                var go = Instantiate(_missionCellPrefab, _missionContainer);
                go.SetActive(true);
                go.name = $"Mission_{m.MissionId}";
                BindCell(go, m, loc);
            }
        }

        private void BindCell(GameObject go, WeeklyMissionDto m, LocalizationService loc)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = m.IsCompleted ? CompletedDimAlpha : 1f; // badge has ignoreParentGroups → stays full alpha

            var badge = go.transform.Find("StatusBadge");
            if (badge != null)
            {
                badge.gameObject.SetActive(m.IsCompleted);
                var badgeImg = badge.GetComponent<Image>();
                if (badgeImg != null) badgeImg.color = Color.white; // FFFFFF, no dim
            }

            var nameText = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null) nameText.text = loc != null ? loc.Get(m.NameKey) : m.NameKey;

            var descText = go.transform.Find("DescText")?.GetComponent<TMP_Text>();
            if (descText != null) descText.text = loc != null ? loc.Get(m.DescKey) : m.DescKey;

            var bar = go.transform.Find("ProgressBar")?.GetComponent<AnimatedProgressBar>();
            if (bar != null)
            {
                float ratio = m.TargetValue > 0 ? Mathf.Clamp01((float)m.Progress / m.TargetValue) : (m.IsCompleted ? 1f : 0f);
                bar.SetProgress(ratio, m.IsCompleted);
            }

            var progressText = go.transform.Find("ProgressText")?.GetComponent<TMP_Text>();
            if (progressText != null)
                progressText.text = string.Format(loc != null ? loc.Get("popup.weekly_mission.progress_fmt") : "{0}/{1}", Mathf.Min(m.Progress, m.TargetValue), m.TargetValue);

            var epText = go.transform.Find("EpText")?.GetComponent<TMP_Text>();
            if (epText != null)
                epText.text = string.Format(loc != null ? loc.Get("popup.weekly_mission.ep_reward_fmt") : "+{0} EP", m.EpReward);
        }

        private void BuildClaim(WeeklyMissionResponse res, LocalizationService loc)
        {
            _claimableThreshold = -1;
            if (res.Track != null)
            {
                var next = res.Track.Where(t => t.IsReached && !t.IsClaimed).OrderBy(t => t.EpThreshold).FirstOrDefault();
                if (next != null) _claimableThreshold = next.EpThreshold;
            }

            bool canClaim = _claimableThreshold > 0;
            if (_claimButton != null) _claimButton.interactable = canClaim;
            if (_claimLabel != null && loc != null)
                _claimLabel.text = canClaim
                    ? string.Format(loc.Get("popup.weekly_mission.btn_claim_fmt"), _claimableThreshold)
                    : loc.Get("popup.weekly_mission.claimed");
        }

        private void OnClaim()
        {
            if (_claimableThreshold <= 0 || WeeklyMissionApiService.Instance == null) return;
            if (_claimButton != null) _claimButton.interactable = false;
            UIManager.Instance?.ShowLoading();

            WeeklyMissionApiService.Instance.Claim(_claimableThreshold, resp =>
            {
                UIManager.Instance?.HideLoading();
                var rewards = RewardDisplay.Build(resp.GrantedRewards);
                if (rewards.Count > 0)
                    UIManager.Instance?.ShowPopup<RewardPopupView>(v => v.Init(rewards));
                Refresh();
            }, err =>
            {
                UIManager.Instance?.HideLoading();
                if (_claimButton != null) _claimButton.interactable = true;
                var loc = LocalizationService.Instance;
                UIManager.Instance?.ShowToast(loc != null ? loc.GetErrorFromResponse(err) : err, ToastType.Warning);
            });
        }

        private void Close()
        {
            var appear = GetComponent<UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => UIManager.Instance?.CloseTopPopup());
            else
                UIManager.Instance?.CloseTopPopup();
        }
    }
}
