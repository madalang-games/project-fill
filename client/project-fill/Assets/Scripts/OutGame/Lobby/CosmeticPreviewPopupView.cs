using System;
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
    /// Cosmetic preview popup: large preview + name/desc + buy-and-apply / apply action.
    /// </summary>
    public class CosmeticPreviewPopupView : MonoBehaviour
    {
        [SerializeField] private Image _previewImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descText;
        [SerializeField] private TMP_Text _stateText;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionLabel;
        [SerializeField] private Button _closeButton;

        private CosmeticItemDto _item;
        private ActiveCosmeticsDto _active;
        private Action _onChanged;

        private void Awake()
        {
            _actionButton?.onClick.AddListener(OnAction);
            _closeButton?.onClick.AddListener(Close);
        }

        public void Init(CosmeticItemDto item, ActiveCosmeticsDto active, Action onChanged)
        {
            _item = item;
            _active = active ?? new ActiveCosmeticsDto();
            _onChanged = onChanged;

            var loc = LocalizationService.Instance;
            if (_previewImage != null)
            {
                var spr = DynamicResourceService.Instance?.GetSprite(item.PreviewRes);
                _previewImage.sprite = spr;
                _previewImage.preserveAspect = true;
                _previewImage.enabled = spr != null;
            }
            if (_nameText != null) _nameText.text = loc != null ? loc.Get(item.NameKey) : item.NameKey;
            if (_descText != null) _descText.text = loc != null ? loc.Get(item.DescKey) : item.DescKey;

            RefreshActionState();
        }

        private void RefreshActionState()
        {
            var loc = LocalizationService.Instance;
            bool isActive = IsActive(_item);
            bool owned = _item.Unlocked;
            var unlockType = (CosmeticUnlockType)_item.UnlockType;

            if (_stateText != null)
            {
                if (unlockType == CosmeticUnlockType.Gold && !owned)
                    _stateText.text = $"{_item.UnlockCost}";
                else
                    _stateText.text = "";
            }

            if (_actionButton == null) return;

            if (isActive)
            {
                _actionButton.interactable = false;
                SetLabel("shop.cosmetic.applied");
            }
            else if (owned)
            {
                _actionButton.interactable = true;
                SetLabel("shop.cosmetic.btn_apply");
            }
            else if (unlockType == CosmeticUnlockType.Gold)
            {
                _actionButton.interactable = true;
                SetLabel("shop.cosmetic.btn_buy_apply");
            }
            else
            {
                // achievement / attendance / challenge unlock — not purchasable here
                _actionButton.interactable = false;
                SetLabel(LockedKey(unlockType));
            }
        }

        private void OnAction()
        {
            if (_item.Unlocked) { Apply(); return; }

            UIManager.Instance?.ShowLoading();
            CosmeticApiService.Instance.UnlockCosmetic(_item.CosmeticId, _ =>
            {
                _item.Unlocked = true;
                Apply();
            }, err =>
            {
                UIManager.Instance?.HideLoading();
                ShowError(err);
            });
        }

        private void Apply()
        {
            var category = (CosmeticCategory)_item.Category;
            var req = new SetActiveCosmeticRequest
            {
                ChipSkin = category == CosmeticCategory.Chip ? _item.CosmeticId : _active.ChipSkin,
                LaneSkin = category == CosmeticCategory.Lane ? _item.CosmeticId : _active.LaneSkin,
                BoardSkin = category == CosmeticCategory.Board ? _item.CosmeticId : _active.BoardSkin,
                UseCustomBoardSkin = category == CosmeticCategory.Board ? true : _active.UseCustomBoardSkin,
            };

            CosmeticApiService.Instance.SetActive(req, active =>
            {
                UIManager.Instance?.HideLoading();
                _active = active;
                var loc = LocalizationService.Instance;
                UIManager.Instance?.ShowToast(loc != null ? loc.Get("shop.cosmetic.applied") : "Applied", ToastType.Success);
                _onChanged?.Invoke();
                Close();
            }, err =>
            {
                UIManager.Instance?.HideLoading();
                ShowError(err);
            });
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

        private static string LockedKey(CosmeticUnlockType type) => type switch
        {
            CosmeticUnlockType.Achievement => "shop.cosmetic.locked_achievement",
            CosmeticUnlockType.Attendance => "shop.cosmetic.locked_attendance",
            CosmeticUnlockType.Challenge => "shop.cosmetic.locked_challenge",
            _ => "",
        };

        private void SetLabel(string key)
        {
            if (_actionLabel == null) return;
            var loc = LocalizationService.Instance;
            _actionLabel.text = loc != null ? loc.Get(key) : key;
        }

        private void ShowError(string raw)
        {
            var loc = LocalizationService.Instance;
            string msg = loc != null ? loc.GetErrorFromResponse(raw) : raw;
            UIManager.Instance?.ShowToast(msg, ToastType.Warning);
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
