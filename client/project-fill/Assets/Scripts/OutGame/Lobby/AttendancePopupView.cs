using System.Linq;
using Game.Core;
using Game.Core.UI;
using Game.Services;
using ProjectFill.Contracts.Attendance;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Daily attendance popup: 7 day cards each previewing that day's reward (RewardItemCell,
    /// long-press tooltip), today's full reward row, claim CTA.
    /// 7-day non-punitive cycle (design daily-login §2). Auto-opened from HomeTab on first daily entry.
    /// </summary>
    public class AttendancePopupView : MonoBehaviour
    {
        [SerializeField] private Transform _dayContainer;
        [SerializeField] private TMP_Text _todayRewardText;
        [SerializeField] private Transform _todayRewardRow;
        [SerializeField] private GameObject _rewardCellPrefab;
        [SerializeField] private Button _claimButton;
        [SerializeField] private TMP_Text _claimLabel;
        [SerializeField] private Button _closeButton;

        private const float DayCellSize = 104f;         // fixed day-card icon size (no per-count scaling)
        private const float TodayCellFootprint = 110f;  // today row has horizontal room; no scaling

        private void Awake()
        {
            _claimButton?.onClick.AddListener(OnClaim);
            _closeButton?.onClick.AddListener(Close);
        }

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            if (AttendanceApiService.Instance == null) return;
            AttendanceApiService.Instance.FetchStatus(Bind, _ => { });
        }

        private void Bind(AttendanceStatusResponse status)
        {
            if (_dayContainer != null && status.Days != null)
            {
                foreach (var d in status.Days)
                {
                    var card = _dayContainer.Find($"Day{d.Day}");
                    if (card == null) continue;
                    SetActive(card, "Dim", d.IsClaimed);
                    SetActive(card, "PulseRing", d.IsToday && !d.IsClaimed);
                    BuildDayReward(card, d.RewardGroupId);
                }
            }

            var loc = LocalizationService.Instance;
            if (_todayRewardText != null && loc != null)
                _todayRewardText.text = loc.Get("popup.attendance.today_reward");

            int todayGroup = status.Days?.FirstOrDefault(d => d.IsToday)?.RewardGroupId ?? 0;
            BuildTodayRow(todayGroup);

            if (_claimButton != null) _claimButton.interactable = !status.ClaimedToday;
            if (_claimLabel != null && loc != null) _claimLabel.text = loc.Get("popup.attendance.btn_claim");
        }

        // Narrow card shows ONLY the primary reward at a fixed size; a "+N" badge denotes extra
        // reward kinds. Full group lives in TodayRewardRow + the cell's long-press tooltip — so
        // every day-card is the same size regardless of group count (no per-count scaling).
        private void BuildDayReward(Transform card, int rewardGroupId)
        {
            var slot = card.Find("RewardSlot") as RectTransform;
            if (slot == null) return;
            ClearChildren(slot);

            var badge = card.Find("CountBadge");
            var rewards = RewardDisplay.BuildFromGroup(rewardGroupId);
            int n = rewards.Count;
            if (n == 0)
            {
                if (badge != null) badge.gameObject.SetActive(false);
                return;
            }

            var cell = SpawnCell(slot, rewards[n - 1], DayCellSize);
            if (cell != null)
                cell.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            if (badge != null)
            {
                bool hasExtra = n > 1;
                badge.gameObject.SetActive(hasExtra);
                if (hasExtra)
                {
                    var t = badge.GetComponent<TMP_Text>();
                    if (t != null) t.text = $"+{n - 1}";
                }
            }
        }

        private void BuildTodayRow(int rewardGroupId)
        {
            if (_todayRewardRow == null) return;
            ClearChildren(_todayRewardRow);

            // HLG owns layout; cells keep native footprint (icon fits, no scaling needed).
            foreach (var r in RewardDisplay.BuildFromGroup(rewardGroupId))
                SpawnCell(_todayRewardRow, r, TodayCellFootprint);
        }

        private GameObject SpawnCell(Transform parent, RewardItem reward, float footprint)
        {
            if (_rewardCellPrefab == null) return null;
            var cell = Instantiate(_rewardCellPrefab, parent);

            var rt = cell.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(footprint, footprint);
                rt.localScale = Vector3.one;
            }

            var icon = cell.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null) icon.sprite = reward.Icon;
            var qty = cell.transform.Find("Quantity")?.GetComponent<TMP_Text>();
            if (qty != null) qty.text = $"× {reward.Quantity}";

            cell.GetComponent<RewardItemCellView>()?.Init(reward.Icon, reward.NameKey, reward.DescKey);
            return cell;
        }

        private void OnClaim()
        {
            if (_claimButton != null) _claimButton.interactable = false;
            UIManager.Instance?.ShowLoading();
            AttendanceApiService.Instance.Claim(resp =>
            {
                UIManager.Instance?.HideLoading();

                var rewards = RewardDisplay.Build(resp.GrantedRewards);
                rewards.AddRange(RewardDisplay.Build(resp.MilestoneRewards));
                if (rewards.Count > 0)
                    UIManager.Instance?.ShowPopup<RewardPopupView>(v => v.Init(rewards));

                if (resp.UnlockedCosmetics != null && resp.UnlockedCosmetics.Count > 0)
                {
                    var loc = LocalizationService.Instance;
                    UIManager.Instance?.ShowToast(loc != null ? loc.Get("shop.cosmetic.applied") : "Cosmetic unlocked", ToastType.Success);
                }

                Refresh();
            }, err =>
            {
                UIManager.Instance?.HideLoading();
                if (_claimButton != null) _claimButton.interactable = true;
                var loc = LocalizationService.Instance;
                UIManager.Instance?.ShowToast(loc != null ? loc.GetErrorFromResponse(err) : err, ToastType.Warning);
            });
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private static void SetActive(Transform card, string childName, bool active)
        {
            var child = card.Find(childName);
            if (child != null) child.gameObject.SetActive(active);
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
