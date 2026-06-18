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
        [SerializeField] private TMP_Text _trackText;
        [SerializeField] private Transform _missionContainer;
        [SerializeField] private Button _claimButton;
        [SerializeField] private TMP_Text _claimLabel;
        [SerializeField] private Button _closeButton;

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

            BuildTrackText(res, loc);
            BuildMissionRows(res, loc);
            BuildClaim(res, loc);
        }

        private void BuildTrackText(WeeklyMissionResponse res, LocalizationService loc)
        {
            if (_trackText == null || res.Track == null) return;
            var sb = new System.Text.StringBuilder();
            foreach (var t in res.Track.OrderBy(t => t.EpThreshold))
            {
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(t.IsReached ? '●' : '○').Append(t.EpThreshold);
            }
            _trackText.text = sb.ToString();
        }

        private void BuildMissionRows(WeeklyMissionResponse res, LocalizationService loc)
        {
            if (_missionContainer == null) return;
            for (int i = _missionContainer.childCount - 1; i >= 0; i--)
                Destroy(_missionContainer.GetChild(i).gameObject);
            if (res.Missions == null) return;

            foreach (var m in res.Missions)
            {
                string glyph = m.IsCompleted ? "✓" : (m.Progress > 0 ? "▶" : "○");
                string name = loc != null ? loc.Get(m.NameKey) : m.NameKey;
                string progress = string.Format(loc != null ? loc.Get("popup.weekly_mission.progress_fmt") : "{0}/{1}", m.Progress, m.TargetValue);
                CreateRow($"{glyph}  {name}   {progress}   +{m.EpReward} EP");
            }
        }

        private void CreateRow(string text)
        {
            var go = new GameObject("MissionRow", typeof(RectTransform));
            go.transform.SetParent(_missionContainer, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 64;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 32;
            tmp.fontSizeMax = 40;
            tmp.fontSize = 40;
            tmp.alignment = TextAlignmentOptions.Left;
            var font = LocalizationService.Instance?.GetFont(LocalizationService.Instance.CurrentLanguage);
            if (font != null) tmp.font = font;
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
