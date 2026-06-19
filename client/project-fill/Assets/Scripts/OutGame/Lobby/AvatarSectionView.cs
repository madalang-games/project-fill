using System.Collections.Generic;
using Game.Core;
using Game.Services;
using Game.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Avatar section inside the Shop tab: scrollable grid of avatar cards.
    /// Card tap → AvatarPreviewPopupView (buy + equip). Uses the existing avatar API
    /// (PlayerApiService.UpdateProfile equip/unlock, gold via PlayerProgressService) —
    /// avatar is not part of the cosmetic data/server system.
    /// </summary>
    public class AvatarSectionView : MonoBehaviour
    {
        [System.Serializable]
        public struct AvatarSpriteMapping
        {
            public int avatarId;
            public string resourceName;
            public Sprite sprite;
        }

        [SerializeField] private RectTransform _gridContainer;
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private List<AvatarSpriteMapping> _avatarSprites = new List<AvatarSpriteMapping>();

        public Sprite GetAvatarSprite(int avatarId)
        {
            foreach (var m in _avatarSprites)
                if (m.avatarId == avatarId) return m.sprite;
            return null;
        }

        private void OnEnable() => Rebuild();

        private void Rebuild()
        {
            if (_gridContainer == null || _cardPrefab == null) return;

            foreach (Transform child in _gridContainer)
                Destroy(child.gameObject);

            var avatars = CsvLoader.Load<ProjectFill.Data.Generated.Avatar>(ProjectFill.Data.Generated.Avatar.ResourcePath);
            if (avatars == null) return;

            int equipped = AuthService.Instance != null ? AuthService.Instance.AvatarId : 1;
            var loc = LocalizationService.Instance;

            int count = 0;
            foreach (var avatar in avatars)
            {
                count++;
                var go = Instantiate(_cardPrefab, _gridContainer);
                go.SetActive(true);
                go.name = $"AvatarCard_{avatar.id}";

                var icon = go.transform.Find("Visual/Icon")?.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = GetAvatarSprite(avatar.id);
                    icon.color = Color.white;
                    icon.preserveAspect = true;
                }

                bool isEquipped = avatar.id == equipped;
                var highlight = go.transform.Find("Visual/SelectedHighlight")?.gameObject;
                if (highlight != null) highlight.SetActive(isEquipped);

                bool isUnlocked = PlayerProgressService.Instance != null &&
                                  PlayerProgressService.Instance.IsAvatarUnlocked(avatar.id);
                var lockOverlay = go.transform.Find("Visual/LockOverlay")?.gameObject;
                if (lockOverlay != null) lockOverlay.SetActive(!isUnlocked);

                var stateText = go.transform.Find("StateText")?.GetComponent<TMP_Text>();
                if (stateText != null)
                {
                    if (isEquipped) stateText.text = loc != null ? loc.Get("shop.cosmetic.applied") : "";
                    else if (isUnlocked) stateText.text = loc != null ? loc.Get("shop.cosmetic.btn_apply") : "";
                    else if (avatar.unlock_cost > 0) stateText.text = $"{avatar.unlock_cost}";
                    else stateText.text = loc != null ? loc.Get("shop.avatar.locked_reward") : "";

                    // Gold icon only for an actual gold price (centered text left untouched).
                    GoldPriceLabel.Set(stateText, !isEquipped && !isUnlocked && avatar.unlock_cost > 0);
                }

                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    var captured = avatar;
                    bool capturedUnlocked = isUnlocked;
                    btn.onClick.AddListener(() => OpenPreview(captured, capturedUnlocked));
                }
            }

            ResizeSectionToFit(count);
        }

        // Section lives inside the Shop's vertical ScrollRect — size it to the grid's row count so the
        // outer Shop scroll absorbs overflow (mirrors CosmeticSectionView). Grid uses a Flexible column
        // constraint, so columns are derived from the laid-out grid width.
        private void ResizeSectionToFit(int count)
        {
            var glg = _gridContainer != null ? _gridContainer.GetComponent<GridLayoutGroup>() : null;
            if (glg == null) return;

            // Width is horizontal (independent of the height we're about to set) — rebuild parent first so
            // the stretched grid has its final width before deriving the responsive column count.
            if (transform.parent is RectTransform parentRtPre)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRtPre);

            float width = _gridContainer.rect.width;
            int cols = width > 1f
                ? Mathf.Max(1, Mathf.FloorToInt((width + glg.spacing.x) / (glg.cellSize.x + glg.spacing.x)))
                : 5;
            int rows = Mathf.CeilToInt(count / (float)cols);
            float gridH = rows * glg.cellSize.y + Mathf.Max(0, rows - 1) * glg.spacing.y;

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

        private void OpenPreview(ProjectFill.Data.Generated.Avatar avatar, bool isUnlocked)
        {
            UIManager.Instance?.ShowPopup<AvatarPreviewPopupView>(v => v.Init(
                avatar.id, GetAvatarSprite(avatar.id), avatar.unlock_cost, isUnlocked, Rebuild));
        }
    }
}
