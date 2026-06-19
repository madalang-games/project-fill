using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Core.UI;
using Game.Services;
using ProjectFill.Contracts.Cosmetic;
using ProjectFill.Contracts.GameTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Cosmetic section inside the Shop tab: Chip/Lane/Board category tabs + grid of cosmetic cells.
    /// Board customization is unified here (canonical surface; AccountPopup no longer hosts board skins).
    /// </summary>
    public class CosmeticSectionView : MonoBehaviour
    {
        [SerializeField] private Button _chipTabButton;
        [SerializeField] private Button _laneTabButton;
        [SerializeField] private Button _boardTabButton;
        [SerializeField] private RectTransform _gridContainer;
        [SerializeField] private GameObject _cellPrefab;

        [SerializeField] private Color _activeTabColor = new Color(1f, 0.145f, 0.522f);   // UI_CTA
        [SerializeField] private Color _inactiveTabColor = new Color(0.094f, 0.094f, 0.212f); // UI_BG_MID

        private CosmeticCategory _currentCategory = CosmeticCategory.Chip;
        private readonly List<CosmeticItemDto> _items = new List<CosmeticItemDto>();
        private ActiveCosmeticsDto _active = new ActiveCosmeticsDto();

        private void Awake()
        {
            _chipTabButton?.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Chip));
            _laneTabButton?.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Lane));
            _boardTabButton?.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Board));
        }

        private void OnEnable() => Fetch();

        private void Fetch()
        {
            if (CosmeticApiService.Instance == null) return;
            CosmeticApiService.Instance.FetchCosmetics(resp =>
            {
                _items.Clear();
                _items.AddRange(resp.Items);
                _active = resp.Active ?? new ActiveCosmeticsDto();
                Rebuild();
            }, _ => { /* lobby flow continues if cosmetics unavailable */ });
        }

        private void SwitchCategory(CosmeticCategory category)
        {
            _currentCategory = category;
            Rebuild();
        }

        private void Rebuild()
        {
            UpdateTabColors();
            if (_gridContainer == null || _cellPrefab == null) return;

            foreach (Transform child in _gridContainer)
                Destroy(child.gameObject);

            var loc = LocalizationService.Instance;
            var cells = _items
                .Where(i => (CosmeticCategory)i.Category == _currentCategory)
                .OrderBy(i => i.SortOrder)
                .ToList();

            foreach (var item in cells)
            {
                var go = Instantiate(_cellPrefab, _gridContainer);
                go.SetActive(true);
                go.name = $"Cell_{item.CosmeticId}";

                var preview = go.transform.Find("Preview")?.GetComponent<Image>();
                if (preview != null)
                {
                    // Render the actual skin (board surface+chips / lane column / scaled chip) as layered UI,
                    // not a flat preview_res sprite — the cell shows what the player is buying.
                    preview.preserveAspect = false;
                    CosmeticPreview.Build(preview, item);
                }

                var highlight = go.transform.Find("Preview/SelectedHighlight")?.gameObject;
                if (highlight != null) highlight.SetActive(IsActive(item));

                var lockOverlay = go.transform.Find("Preview/LockOverlay")?.gameObject;
                if (lockOverlay != null) lockOverlay.SetActive(!item.Unlocked);

                var nameText = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
                if (nameText != null) nameText.text = loc != null ? loc.Get(item.NameKey) : item.NameKey;

                var stateText = go.transform.Find("StateText")?.GetComponent<TMP_Text>();
                if (stateText != null)
                {
                    stateText.text = StateLabel(item, loc);

                    // Gold icon only for an actual gold price (centered text left untouched).
                    bool showGoldPrice = !IsActive(item) && !item.Unlocked &&
                                         (CosmeticUnlockType)item.UnlockType == CosmeticUnlockType.Gold;
                    GoldPriceLabel.Set(stateText, showGoldPrice);
                }

                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    var captured = item;
                    btn.onClick.AddListener(() => OpenPreview(captured));
                }
            }

            ResizeSectionToFit(cells.Count);
        }

        // Section lives inside the Shop's vertical ScrollRect — adding a second vertical scroll here would
        // conflict. Instead size the section to the grid's row count so the outer Shop scroll absorbs overflow.
        private void ResizeSectionToFit(int count)
        {
            var glg = _gridContainer != null ? _gridContainer.GetComponent<GridLayoutGroup>() : null;
            if (glg == null) return;

            int cols = Mathf.Max(1, glg.constraintCount);
            int rows = Mathf.CeilToInt(count / (float)cols);
            float gridH = rows * glg.cellSize.y + Mathf.Max(0, rows - 1) * glg.spacing.y;

            // Grid is anchor-stretched inside the section; its insets reserve the title/tab header band.
            float topInset = -_gridContainer.offsetMax.y;
            float bottomInset = _gridContainer.offsetMin.y;
            float sectionH = gridH + topInset + bottomInset;

            var le = GetComponent<LayoutElement>();
            if (le != null) le.preferredHeight = sectionH;
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, sectionH);

            if (transform.parent is RectTransform parentRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
        }

        private string StateLabel(CosmeticItemDto item, LocalizationService loc)
        {
            if (loc == null) return "";
            if (IsActive(item)) return loc.Get("shop.cosmetic.applied");
            if (item.Unlocked) return loc.Get("shop.cosmetic.btn_apply");

            switch ((CosmeticUnlockType)item.UnlockType)
            {
                case CosmeticUnlockType.Gold: return $"{item.UnlockCost}";
                case CosmeticUnlockType.Achievement: return loc.Get("shop.cosmetic.locked_achievement");
                case CosmeticUnlockType.Attendance: return loc.Get("shop.cosmetic.locked_attendance");
                case CosmeticUnlockType.Challenge: return loc.Get("shop.cosmetic.locked_challenge");
                default: return "";
            }
        }

        private bool IsActive(CosmeticItemDto item)
        {
            switch ((CosmeticCategory)item.Category)
            {
                case CosmeticCategory.Chip: return _active.ChipSkin == item.CosmeticId;
                case CosmeticCategory.Lane: return _active.LaneSkin == item.CosmeticId;
                case CosmeticCategory.Board: return _active.BoardSkin == item.CosmeticId;
                default: return false;
            }
        }

        private void OpenPreview(CosmeticItemDto item)
        {
            UIManager.Instance?.ShowPopup<CosmeticPreviewPopupView>(v => v.Init(item, _active, Fetch));
        }

        private void UpdateTabColors()
        {
            SetTab(_chipTabButton, _currentCategory == CosmeticCategory.Chip);
            SetTab(_laneTabButton, _currentCategory == CosmeticCategory.Lane);
            SetTab(_boardTabButton, _currentCategory == CosmeticCategory.Board);
        }

        private void SetTab(Button button, bool active)
        {
            if (button != null && button.targetGraphic != null)
                button.targetGraphic.color = active ? _activeTabColor : _inactiveTabColor;
        }
    }
}
