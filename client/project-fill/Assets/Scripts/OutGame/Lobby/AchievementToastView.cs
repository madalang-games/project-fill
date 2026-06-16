using System.Collections;
using Game.Core;
using Game.Core.UI;
using Game.Services;
using ProjectFill.Contracts.GameTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Slide-down achievement-unlocked toast: tier badge + name, auto-dismiss after 3s (design achievement §4.1).
    /// Shown via UIManager.ShowOverlay&lt;AchievementToastView&gt;(v => v.Show(nameKey, tier)).
    /// Trigger source (gameplay achievement completion) is the InGame seam — wire from stage-clear flow.
    /// </summary>
    public class AchievementToastView : MonoBehaviour
    {
        [SerializeField] private RectTransform _banner;
        [SerializeField] private Image _tierBadge;
        [SerializeField] private TMP_Text _nameText;

        private const float SlideDuration = 0.3f;
        private const float HoldSeconds = 3f;
        private const float HiddenY = 220f;

        private static readonly Color BronzeColor = new Color(0.804f, 0.498f, 0.196f);
        private static readonly Color SilverColor = new Color(0.753f, 0.753f, 0.753f);
        private static readonly Color GoldColor = new Color(1f, 0.843f, 0f);
        private static readonly Color PlatinumColor = new Color(0.169f, 0.851f, 0.753f);

        public void Show(string nameKey, AchievementTier tier)
        {
            var loc = LocalizationService.Instance;
            if (_nameText != null)
                _nameText.text = loc != null ? string.Format(loc.Get("toast.achievement_unlocked_fmt"), loc.Get(nameKey)) : nameKey;
            if (_tierBadge != null) _tierBadge.color = TierColor(tier);
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            if (_banner == null) { yield return new WaitForSeconds(HoldSeconds); UIManager.Instance?.CloseOverlay(); yield break; }

            yield return Slide(HiddenY, 0f);
            yield return new WaitForSeconds(HoldSeconds);
            yield return Slide(0f, HiddenY);
            UIManager.Instance?.CloseOverlay();
        }

        private IEnumerator Slide(float fromY, float toY)
        {
            float t = 0f;
            var pos = _banner.anchoredPosition;
            while (t < SlideDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = UIEasing.EaseOutBack(Mathf.Clamp01(t / SlideDuration));
                _banner.anchoredPosition = new Vector2(pos.x, Mathf.Lerp(fromY, toY, k));
                yield return null;
            }
            _banner.anchoredPosition = new Vector2(pos.x, toY);
        }

        private static Color TierColor(AchievementTier tier) => tier switch
        {
            AchievementTier.Bronze => BronzeColor,
            AchievementTier.Silver => SilverColor,
            AchievementTier.Gold => GoldColor,
            AchievementTier.Platinum => PlatinumColor,
            _ => SilverColor,
        };
    }
}
