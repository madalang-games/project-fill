using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Core.UI;
using Game.Services;
using Game.Utils;
using ProjectFill.Contracts.Attendance;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GeneratedRewardItem = ProjectFill.Data.Generated.RewardItem;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Daily attendance popup: 7 day cards (claimed/today/future), today's reward preview, claim CTA.
    /// 7-day non-punitive cycle (design daily-login §2). Auto-opened from HomeTab on first daily entry.
    /// </summary>
    public class AttendancePopupView : MonoBehaviour
    {
        [SerializeField] private Transform _dayContainer;
        [SerializeField] private TMP_Text _todayRewardText;
        [SerializeField] private Button _claimButton;
        [SerializeField] private TMP_Text _claimLabel;
        [SerializeField] private Button _closeButton;

        private static List<GeneratedRewardItem> _rewardItems;

        private void Awake()
        {
            _claimButton?.onClick.AddListener(OnClaim);
            _closeButton?.onClick.AddListener(Close);
            EnsureRewardItems();
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
                }
            }

            var loc = LocalizationService.Instance;
            if (_todayRewardText != null && loc != null)
            {
                int todayGroup = status.Days?.FirstOrDefault(d => d.IsToday)?.RewardGroupId ?? 0;
                int gold = GoldOfGroup(todayGroup);
                _todayRewardText.text = string.Format(loc.Get("popup.attendance.today_reward"), gold);
            }

            if (_claimButton != null) _claimButton.interactable = !status.ClaimedToday;
            if (_claimLabel != null && loc != null) _claimLabel.text = loc.Get("popup.attendance.btn_claim");
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

        private static void EnsureRewardItems()
        {
            if (_rewardItems != null) return;
            try
            {
                var loaded = CsvLoader.Load<GeneratedRewardItem>(GeneratedRewardItem.ResourcePath);
                _rewardItems = loaded != null ? new List<GeneratedRewardItem>(loaded) : new List<GeneratedRewardItem>();
            }
            catch { _rewardItems = new List<GeneratedRewardItem>(); }
        }

        private static int GoldOfGroup(int groupId)
        {
            if (_rewardItems == null || groupId == 0) return 0;
            var row = _rewardItems.FirstOrDefault(r => r.reward_group_id == groupId && r.reward_type == "SOFT_CURRENCY");
            return row?.amount ?? 0;
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
