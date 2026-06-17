using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.UI;
using Game.OutGame.Lobby;
using Game.Services;
using ProjectFill.Contracts.Rewards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InGame.View
{
    // Per-clear summary shown on the result overlay (design §6/§8). Built from the server clear
    // response; null for the daily challenge (no campaign best surface), which hides the stat block.
    public readonly struct ClearSummary
    {
        public readonly int  Moves;
        public readonly int  Best;
        public readonly bool IsNewBest;
        public ClearSummary(int moves, int best, bool isNewBest)
        {
            Moves = moves; Best = best; IsNewBest = isNewBest;
        }
    }

    // Stage-clear result overlay. Shows the clear summary (moves / best moves / new-best badge),
    // lists the granted rewards (RewardItemCell rows) and offers a rewarded-ad "Double Reward" that
    // re-grants the stage's reward group server-side, then doubles the displayed amounts. Shown via
    // UIManager; backdrop DIM provides the scrim.
    public class ResultOverlayView : MonoBehaviour
    {
        // Mirror shared/datas/ad/ad_placement.csv (REWARDED placement keys).
        private const string PlacementDoubleReward = "DOUBLE_REWARD_STAGE_CLEAR";
        private const string AdProvider            = "admob";

        [SerializeField] private TMP_Text  _titleText;
        [SerializeField] private GameObject _statsBlock;    // hidden when no summary (daily challenge)
        [SerializeField] private TMP_Text  _movesText;      // "Moves: {0}" (runtime-formatted)
        [SerializeField] private TMP_Text  _bestText;       // "Best Moves: {0}" (runtime-formatted)
        [SerializeField] private GameObject _newBestBadge;  // shown only on a new personal best
        [SerializeField] private Transform _rewardContainer;
        [SerializeField] private GameObject _rewardCellPrefab;
        [SerializeField] private Button    _doubleRewardButton;
        [SerializeField] private Button    _nextButton;
        [SerializeField] private Button    _mapButton;

        private int    _stageId;
        private string _attemptId;
        private List<RewardItem> _rewards = new();
        private bool _doubled;

        public void Configure(int stageId, string attemptId, IReadOnlyList<GrantedRewardDto> rewards,
            bool canDouble, ClearSummary? summary, Action onNext, Action onLobby)
        {
            _stageId   = stageId;
            _attemptId = attemptId;
            _doubled   = false;

            RenderStats(summary);

            _rewards = RewardDisplay.Build(rewards);
            RenderRewards();

            Bind(_nextButton, () => { Close(); onNext?.Invoke(); });
            Bind(_mapButton,  () => { Close(); onLobby?.Invoke(); });

            // Double reward requires earned rewards + an available ad service. Hidden otherwise.
            bool showDouble = canDouble && _rewards.Count > 0 && AdMobService.Instance != null;
            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.gameObject.SetActive(showDouble);
                if (showDouble)
                {
                    _doubleRewardButton.onClick.RemoveAllListeners();
                    _doubleRewardButton.onClick.AddListener(OnDoubleRewardTapped);
                }
            }
        }

        // Renders the moves / best-moves rows and toggles the new-best badge. summary == null
        // (daily challenge) hides the whole stat block. Format strings come from client_string.csv.
        private void RenderStats(ClearSummary? summary)
        {
            bool show = summary.HasValue;
            if (_statsBlock != null) _statsBlock.SetActive(show);
            if (!show) return;

            var s   = summary.Value;
            var loc = LocalizationService.Instance;
            if (_movesText != null)
                _movesText.text = loc != null ? string.Format(loc.Get("popup.result.moves"), s.Moves) : $"Moves: {s.Moves}";
            if (_bestText != null)
                _bestText.text = loc != null ? string.Format(loc.Get("popup.result.best_moves"), s.Best) : $"Best: {s.Best}";
            if (_newBestBadge != null) _newBestBadge.SetActive(s.IsNewBest);
        }

        private void RenderRewards()
        {
            if (_rewardContainer == null || _rewardCellPrefab == null) return;
            for (int i = _rewardContainer.childCount - 1; i >= 0; i--)
                Destroy(_rewardContainer.GetChild(i).gameObject);

            int count = Mathf.Min(_rewards.Count, 4);
            for (int i = 0; i < count; i++)
            {
                var row = Instantiate(_rewardCellPrefab, _rewardContainer);
                var icon = row.transform.Find("Icon")?.GetComponent<Image>();
                var qty  = row.transform.Find("Quantity")?.GetComponent<TMP_Text>();
                if (icon != null) icon.sprite = _rewards[i].Icon;
                if (qty  != null) qty.text    = $"× {_rewards[i].Quantity}";
                row.GetComponent<RewardItemCellView>()?.Init(_rewards[i].Icon, _rewards[i].NameKey, _rewards[i].DescKey);
            }
        }

        private void OnDoubleRewardTapped()
        {
            if (_doubled) return;
            var ads = AdMobService.Instance;
            if (ads == null) return;

            SetDoubleInteractable(false);
            ads.WatchRewardedAd(PlacementDoubleReward, result =>
            {
                if (result is { Earned: true })
                {
                    AdApiService.Instance.ClaimDoubleReward(_stageId, _attemptId, AdProvider, result.Value.AdToken,
                        onSuccess: res =>
                        {
                            if (res != null && (res.Granted || res.Duplicate)) ApplyDoubled();
                            else SetDoubleInteractable(true);
                        },
                        onError: err =>
                        {
                            ShowError(err);
                            SetDoubleInteractable(true);
                        });
                }
                else
                {
                    // Cancelled / no ad — let the player try again.
                    SetDoubleInteractable(true);
                }
            });
        }

        // Doubles the displayed amounts (server re-granted the same group) and locks the button.
        private void ApplyDoubled()
        {
            if (_doubled) return;
            _doubled = true;
            for (int i = 0; i < _rewards.Count; i++)
            {
                var r = _rewards[i];
                r.Quantity *= 2;
                _rewards[i] = r;
            }
            RenderRewards();
            if (_doubleRewardButton != null) _doubleRewardButton.gameObject.SetActive(false);
        }

        private void SetDoubleInteractable(bool on)
        {
            if (_doubleRewardButton != null) _doubleRewardButton.interactable = on;
        }

        private static void ShowError(string err)
        {
            var loc = LocalizationService.Instance;
            UIManager.Instance?.ShowToast(loc != null ? loc.GetErrorFromResponse(err) : err, ToastType.Warning);
        }

        private static void Bind(Button btn, Action action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => action());
        }

        private void Close()
        {
            var appear = GetComponent<UIPanelAppear>();
            if (appear != null) appear.Disappear(() => UIManager.Instance?.CloseTopPopup());
            else UIManager.Instance?.CloseTopPopup();
        }
    }
}
