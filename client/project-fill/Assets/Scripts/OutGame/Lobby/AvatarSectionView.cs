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

            foreach (var avatar in avatars)
            {
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
                    else stateText.text = $"{avatar.unlock_cost}";
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
        }

        private void OpenPreview(ProjectFill.Data.Generated.Avatar avatar, bool isUnlocked)
        {
            UIManager.Instance?.ShowPopup<AvatarPreviewPopupView>(v => v.Init(
                avatar.id, GetAvatarSprite(avatar.id), avatar.unlock_cost, isUnlocked, Rebuild));
        }
    }
}
