using Game.Services;
using Game.Utils;
using TMPro;
using System.Collections.Generic;
using ProjectFill.Contracts.Ranking;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    public class RankingTabView : MonoBehaviour
    {
        [SerializeField] private Button _stageTabButton;
        [SerializeField] private Button _perfectTabButton;
        [SerializeField] private Button _weeklyTabButton;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descText;
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
        [SerializeField] private string _stageResourceKey = "nav_home";
#pragma warning restore 0414
        [SerializeField] private Sprite _stageSprite;

        [Header("Tab Colors")]
        [SerializeField] private Color _activeTabColor = new Color(1f, 0.3f, 0.47f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.3f, 0.14f, 0.36f);

        private const int PageLimit = 50;
        private string _rankingType = "stage";

        private void Awake()
        {
            if (_stageTabButton != null)
                _stageTabButton.onClick.AddListener(() => Select("stage"));
            if (_perfectTabButton != null)
                _perfectTabButton.onClick.AddListener(() => Select("perfect"));
            if (_weeklyTabButton != null)
                _weeklyTabButton.onClick.AddListener(() => Select("weekly"));
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
            var loc = LocalizationService.Instance;
            var api = RankingApiService.Instance;
            if (api == null)
            {
                SetUnavailable();
                return;
            }

            if (_titleText != null) _titleText.text = loc.Get(TitleKey(_rankingType));
            if (_descText != null) _descText.text = loc.Get(DescKey(_rankingType));
            if (_myRankText != null) _myRankText.text = loc.Get("lobby.ranking.my_rank_empty");
            if (_entriesText != null) _entriesText.text = loc.Get("lobby.ranking.loading");

            var scoreSprite = _stageSprite;

            System.Action<RankingPageResponse> renderPage = page =>
            {
                if (page.Entries.Count == 0)
                {
                    if (_entriesText != null) _entriesText.text = loc.Get("lobby.ranking.no_data");
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
            };

            System.Action<MyRankingResponse> renderMine = mine =>
            {
                if (mine.Entry == null)
                {
                    if (_myRankPin != null) _myRankPin.gameObject.SetActive(false);
                    if (_myRankText != null)
                        _myRankText.text = loc.Get("lobby.ranking.my_rank_empty");
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
                        _myRankText.text = string.Format(loc.Get("lobby.ranking.my_rank_format"), mine.Entry.Rank, mine.Entry.Score);
                }
            };

            System.Action<string> onMineError = _ => { if (_myRankPin != null) _myRankPin.gameObject.SetActive(false); };

            if (_rankingType == "weekly")
            {
                api.FetchWeeklyPage(0, PageLimit, renderPage, _ => SetUnavailable());
                api.FetchMyWeeklyRank(renderMine, onMineError);
            }
            else
            {
                api.FetchGlobalPage(_rankingType, 0, PageLimit, renderPage, _ => SetUnavailable());
                api.FetchMyGlobalRank(_rankingType, renderMine, onMineError);
            }
        }

        private static string TitleKey(string rankingType) => rankingType switch
        {
            "perfect" => "lobby.ranking.perfect_title",
            "weekly"  => "lobby.ranking.weekly_title",
            _          => "lobby.ranking.stages_title",
        };

        private static string DescKey(string rankingType) => rankingType switch
        {
            "perfect" => "lobby.ranking.desc_perfect",
            "weekly"  => "lobby.ranking.desc_weekly",
            _          => "lobby.ranking.desc_stage",
        };

        private void Select(string rankingType)
        {
            if (_rankingType == rankingType)
                return;

            _rankingType = rankingType;
            Refresh();
        }

        private void UpdateTabButtonColors()
        {
            if (_stageTabButton != null && _stageTabButton.targetGraphic != null)
                _stageTabButton.targetGraphic.color = _rankingType == "stage" ? _activeTabColor : _inactiveTabColor;
            if (_perfectTabButton != null && _perfectTabButton.targetGraphic != null)
                _perfectTabButton.targetGraphic.color = _rankingType == "perfect" ? _activeTabColor : _inactiveTabColor;
            if (_weeklyTabButton != null && _weeklyTabButton.targetGraphic != null)
                _weeklyTabButton.targetGraphic.color = _rankingType == "weekly" ? _activeTabColor : _inactiveTabColor;
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
