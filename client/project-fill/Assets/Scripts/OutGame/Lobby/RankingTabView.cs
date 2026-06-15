using Game.Services;
using Game.Utils;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class RankingTabView : MonoBehaviour
    {
        [SerializeField] private Button _starsTabButton;
        [SerializeField] private Button _maxStageTabButton;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _myRankText;
        [SerializeField] private TMP_Text _entriesText;
        [SerializeField] private VirtualizedScrollRect _virtualizedScrollRect;

        [Header("Pinned My Rank Item")]
        [SerializeField] private RankingItemView _myRankPin;

        [System.Serializable]
        public struct AvatarSpriteMapping
        {
            public int avatarId;
            public string resourceName;
            public Sprite sprite;
        }

        [Header("Assets / Mapping")]
        [SerializeField] private List<AvatarSpriteMapping> _avatarSprites = new List<AvatarSpriteMapping>();
#pragma warning disable 0414
        [SerializeField] private string _starResourceKey = "star_filled";
        [SerializeField] private string _stageResourceKey = "nav_home";
#pragma warning restore 0414
        [SerializeField] private Sprite _starSprite;
        [SerializeField] private Sprite _stageSprite;

        [Header("Tab Colors")]
        [SerializeField] private Color _activeTabColor = new Color(1f, 0.3f, 0.47f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.3f, 0.14f, 0.36f);

        private const int PageLimit = 50;
        private string _rankingType = "stars";

        private void Awake()
        {
            if (_starsTabButton != null)
                _starsTabButton.onClick.AddListener(() => Select("stars"));
            if (_maxStageTabButton != null)
                _maxStageTabButton.onClick.AddListener(() => Select("max-stage"));
        }

        private void OnEnable() => Refresh();

        public Sprite GetAvatarSprite(int avatarId)
        {
            if (_avatarSprites != null)
            {
                foreach (var mapping in _avatarSprites)
                {
                    if (mapping.avatarId == avatarId)
                        return mapping.sprite;
                }
            }
            return null;
        }

        public void Refresh()
        {
            UpdateTabButtonColors();
            var api = RankingApiService.Instance;
            if (api == null)
            {
                SetUnavailable();
                return;
            }

            if (_titleText != null)
                _titleText.text = _rankingType == "stars" ? LocalizationService.Instance.Get("lobby.ranking.stars_title") : LocalizationService.Instance.Get("lobby.ranking.stage_title");
            if (_myRankText != null)
                _myRankText.text = LocalizationService.Instance.Get("lobby.ranking.my_rank_empty");
            if (_entriesText != null)
                _entriesText.text = LocalizationService.Instance.Get("lobby.ranking.loading");

            var scoreSprite = _rankingType == "stars" ? _starSprite : _stageSprite;

            api.FetchGlobalPage(_rankingType, 0, PageLimit, page =>
            {
                if (page.Entries.Count == 0)
                {
                    if (_entriesText != null) _entriesText.text = LocalizationService.Instance.Get("lobby.ranking.no_data");
                    if (_virtualizedScrollRect != null) _virtualizedScrollRect.gameObject.SetActive(false);
                    return;
                }

                if (_virtualizedScrollRect != null)
                {
                    if (_entriesText != null) _entriesText.gameObject.SetActive(false);
                    _virtualizedScrollRect.gameObject.SetActive(true);

                    var entryList = page.Entries;
                    _virtualizedScrollRect.Init(entryList.Count, (idx, go) =>
                    {
                        if (idx < 0 || idx >= entryList.Count) return;
                        var entry = entryList[idx];
                        var view = go.GetComponent<RankingItemView>();
                        if (view != null)
                        {
                            view.Bind(entry, GetAvatarSprite(entry.AvatarId), scoreSprite);
                        }
                        else
                        {
                            var rankText  = go.transform.Find("RankText")?.GetComponent<TMP_Text>();
                            var avatarIcon = go.transform.Find("AvatarIcon")?.GetComponent<Image>();
                            var nameText  = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
                            var scoreIcon  = go.transform.Find("ScoreIcon")?.GetComponent<Image>();
                            var scoreText  = go.transform.Find("ScoreText")?.GetComponent<TMP_Text>();

                            if (rankText  != null) rankText.text  = $"#{entry.Rank}";
                            if (nameText  != null) nameText.text  = entry.DisplayName;
                            if (scoreText != null) scoreText.text = entry.Score.ToString();

                            if (avatarIcon != null)
                            {
                                var spr = GetAvatarSprite(entry.AvatarId);
                                avatarIcon.sprite = spr;
                                avatarIcon.gameObject.SetActive(spr != null);
                            }
                            if (scoreIcon != null)
                            {
                                scoreIcon.sprite = scoreSprite;
                                scoreIcon.gameObject.SetActive(scoreIcon.sprite != null);
                            }
                        }
                    });
                }
                else
                {
                    if (_entriesText != null)
                    {
                        _entriesText.gameObject.SetActive(true);
                        var lines = new System.Text.StringBuilder();
                        foreach (var entry in page.Entries)
                            lines.Append('#').Append(entry.Rank).Append("  ")
                                 .Append(entry.DisplayName).Append("  ")
                                 .Append(entry.Score).AppendLine();
                        _entriesText.text = lines.ToString();
                    }
                }
            }, _ => SetUnavailable());

            api.FetchMyGlobalRank(_rankingType, mine =>
            {
                if (mine.Entry == null)
                {
                    if (_myRankPin != null) _myRankPin.gameObject.SetActive(false);
                    if (_myRankText != null)
                        _myRankText.text = LocalizationService.Instance.Get("lobby.ranking.my_rank_empty");
                }
                else
                {
                    if (_myRankPin != null)
                    {
                        _myRankPin.gameObject.SetActive(true);
                        _myRankPin.Bind(mine.Entry, GetAvatarSprite(mine.Entry.AvatarId), scoreSprite);
                        _myRankPin.SetHighlight(true);
                    }

                    if (_myRankText != null)
                        _myRankText.text = string.Format(LocalizationService.Instance.Get("lobby.ranking.my_rank_format"), mine.Entry.Rank, mine.Entry.Score);
                }
            }, _ => {
                if (_myRankPin != null) _myRankPin.gameObject.SetActive(false);
            });
        }

        private void Select(string rankingType)
        {
            if (_rankingType == rankingType)
                return;

            _rankingType = rankingType;
            Refresh();
        }

        private void UpdateTabButtonColors()
        {
            if (_starsTabButton != null && _starsTabButton.targetGraphic != null)
                _starsTabButton.targetGraphic.color = _rankingType == "stars" ? _activeTabColor : _inactiveTabColor;
            if (_maxStageTabButton != null && _maxStageTabButton.targetGraphic != null)
                _maxStageTabButton.targetGraphic.color = _rankingType == "max-stage" ? _activeTabColor : _inactiveTabColor;
        }

        private void SetUnavailable()
        {
            if (_titleText != null) _titleText.text = LocalizationService.Instance.Get("lobby.ranking.default_title");
            if (_myRankText != null) _myRankText.text = LocalizationService.Instance.Get("lobby.ranking.my_rank_empty");
            if (_entriesText != null) _entriesText.text = LocalizationService.Instance.Get("lobby.ranking.unavailable");
            if (_virtualizedScrollRect != null) _virtualizedScrollRect.gameObject.SetActive(false);
            if (_myRankPin != null) _myRankPin.gameObject.SetActive(false);
        }
    }
}
