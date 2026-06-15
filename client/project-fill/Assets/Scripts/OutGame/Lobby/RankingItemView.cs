using ProjectFill.Contracts.Ranking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class RankingItemView : MonoBehaviour
    {
        private static readonly Color HighlightColor = new Color(1f, 0.302f, 0.475f, 1f); // FF4D79

        private TMP_Text _rankText;
        private Image    _avatarIcon;
        private TMP_Text _nameText;
        private Image    _scoreIcon;
        private TMP_Text _scoreText;
        private Image    _background;
        private Color    _normalColor;

        private void Awake()
        {
            _rankText   = transform.Find("RankText")?.GetComponent<TMP_Text>();
            _avatarIcon = transform.Find("AvatarIcon")?.GetComponent<Image>();
            _nameText   = transform.Find("NameText")?.GetComponent<TMP_Text>();
            _scoreIcon  = transform.Find("ScoreIcon")?.GetComponent<Image>();
            _scoreText  = transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            _background = GetComponent<Image>();
            if (_background != null) _normalColor = _background.color;
        }

        public void Bind(RankingEntryDto entry, Sprite avatarSprite, Sprite scoreSprite)
        {
            if (_rankText  != null) _rankText.text  = $"#{entry.Rank}";
            if (_nameText  != null) _nameText.text  = entry.DisplayName;
            if (_scoreText != null) _scoreText.text = entry.Score.ToString();

            if (_avatarIcon != null)
            {
                _avatarIcon.sprite = avatarSprite;
                _avatarIcon.gameObject.SetActive(avatarSprite != null);
            }
            if (_scoreIcon != null)
            {
                _scoreIcon.sprite = scoreSprite;
                _scoreIcon.gameObject.SetActive(scoreSprite != null);
            }
        }

        public void SetHighlight(bool on)
        {
            if (_background != null)
                _background.color = on ? HighlightColor : _normalColor;
        }
    }
}
