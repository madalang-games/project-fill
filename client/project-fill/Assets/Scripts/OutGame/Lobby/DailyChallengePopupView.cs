using Game.Core;
using Game.Core.UI;
using Game.Services;
using ProjectFill.Contracts.DailyChallenge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Daily challenge entry popup: today's puzzle info, difficulty, participants, streak.
    /// [Start] = coming-soon (InGame challenge play is out of OutGame scope — wire board flow later).
    /// [Ranking] routes to the Ranking tab Challenge sub-tab.
    /// </summary>
    public class DailyChallengePopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _dateText;
        [SerializeField] private TMP_Text _difficultyText;
        [SerializeField] private TMP_Text _participantsText;
        [SerializeField] private TMP_Text _streakText;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _rankingButton;
        [SerializeField] private Button _closeButton;

        private void Awake()
        {
            _startButton?.onClick.AddListener(OnStart);
            _rankingButton?.onClick.AddListener(OnRanking);
            _closeButton?.onClick.AddListener(Close);
        }

        private void OnEnable() => Fetch();

        private void Fetch()
        {
            if (DailyChallengeApiService.Instance == null) return;
            DailyChallengeApiService.Instance.FetchToday(Bind, _ => { });
        }

        private void Bind(DailyChallengeTodayResponse today)
        {
            var loc = LocalizationService.Instance;
            if (_dateText != null) _dateText.text = today.ChallengeDate;
            if (_difficultyText != null) _difficultyText.text = DifficultyStars(today.SignalTypeCount);
            if (_participantsText != null && loc != null)
                _participantsText.text = string.Format(loc.Get("popup.challenge.participants_fmt"), today.ParticipantCount);
            if (_streakText != null && loc != null)
                _streakText.text = string.Format(loc.Get("popup.challenge.streak_fmt"), today.CurrentStreak);
        }

        private void OnStart()
        {
            // InGame challenge play is out of scope — board flow not yet wired.
            var loc = LocalizationService.Instance;
            UIManager.Instance?.ShowToast(loc != null ? loc.Get("shop.coming_soon") : "Coming soon", ToastType.Warning);
        }

        private void OnRanking()
        {
            var lobby = FindObjectOfType<LobbyView>();
            Close();
            lobby?.GoToRankingChallenge();
        }

        private static string DifficultyStars(int signalTypeCount)
        {
            int filled = Mathf.Clamp(signalTypeCount - 2, 1, 5);
            var sb = new System.Text.StringBuilder(5);
            for (int i = 0; i < 5; i++) sb.Append(i < filled ? '★' : '☆');
            return sb.ToString();
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
