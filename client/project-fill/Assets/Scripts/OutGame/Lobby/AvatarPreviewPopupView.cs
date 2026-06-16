using System;
using Game.Core;
using Game.Core.UI;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Lobby
{
    /// <summary>
    /// Avatar preview popup: large preview + cost/state + equip / buy-and-equip action.
    /// Buy spends gold locally and equips via PlayerApiService.UpdateProfile (existing avatar API).
    /// </summary>
    public class AvatarPreviewPopupView : MonoBehaviour
    {
        [SerializeField] private Image _previewImage;
        [SerializeField] private TMP_Text _stateText;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TMP_Text _actionLabel;
        [SerializeField] private Button _closeButton;

        private int _avatarId;
        private int _unlockCost;
        private bool _unlocked;
        private Action _onChanged;

        private void Awake()
        {
            _actionButton?.onClick.AddListener(OnAction);
            _closeButton?.onClick.AddListener(Close);
        }

        public void Init(int avatarId, Sprite sprite, int unlockCost, bool unlocked, Action onChanged)
        {
            _avatarId = avatarId;
            _unlockCost = unlockCost;
            _unlocked = unlocked;
            _onChanged = onChanged;

            if (_previewImage != null)
            {
                _previewImage.sprite = sprite;
                _previewImage.preserveAspect = true;
                _previewImage.enabled = sprite != null;
            }
            RefreshActionState();
        }

        private void RefreshActionState()
        {
            bool isEquipped = AuthService.Instance != null && AuthService.Instance.AvatarId == _avatarId;

            if (_stateText != null)
                _stateText.text = _unlocked ? "" : $"{_unlockCost}";

            if (_actionButton == null) return;

            if (isEquipped)
            {
                _actionButton.interactable = false;
                SetLabel("shop.cosmetic.applied");
            }
            else if (_unlocked)
            {
                _actionButton.interactable = true;
                SetLabel("shop.cosmetic.btn_apply");
            }
            else
            {
                _actionButton.interactable = true;
                SetLabel("shop.cosmetic.btn_buy_apply");
            }
        }

        private void OnAction()
        {
            if (_unlocked) { Equip(); return; }

            if (PlayerProgressService.Instance != null && !PlayerProgressService.Instance.CanAfford(_unlockCost))
            {
                var loc = LocalizationService.Instance;
                UIManager.Instance?.ShowToast(loc != null ? loc.Get("toast.avatar_not_enough_gold") : "", ToastType.Error);
                return;
            }
            BuyAndEquip();
        }

        private void Equip()
        {
            if (PlayerApiService.Instance == null) return;
            UIManager.Instance?.ShowLoading();
            PlayerApiService.Instance.UpdateProfile(null, _avatarId, null, (ok, res, err) =>
            {
                UIManager.Instance?.HideLoading();
                var loc = LocalizationService.Instance;
                if (ok && res != null)
                {
                    UIManager.Instance?.ShowToast(loc != null ? loc.Get("toast.avatar_equipped") : "", ToastType.Success);
                    _onChanged?.Invoke();
                    Close();
                }
                else
                {
                    UIManager.Instance?.ShowToast(loc != null ? loc.Get("toast.avatar_equip_failed") : "", ToastType.Error);
                }
            });
        }

        private void BuyAndEquip()
        {
            if (PlayerApiService.Instance == null) return;
            UIManager.Instance?.ShowLoading();
            PlayerApiService.Instance.UpdateProfile(null, _avatarId, null, (ok, res, err) =>
            {
                UIManager.Instance?.HideLoading();
                var loc = LocalizationService.Instance;
                if (ok && res != null)
                {
                    if (PlayerProgressService.Instance != null)
                    {
                        PlayerProgressService.Instance.SpendGold(_unlockCost);
                        PlayerProgressService.Instance.UnlockAvatarLocally(_avatarId);
                    }
                    _unlocked = true;
                    UIManager.Instance?.ShowToast(loc != null ? loc.Get("toast.avatar_unlocked") : "", ToastType.Success);
                    _onChanged?.Invoke();
                    Close();
                }
                else
                {
                    string msg = loc != null ? loc.GetErrorFromResponse(err) : err;
                    UIManager.Instance?.ShowToast(msg, ToastType.Error);
                }
            });
        }

        private void SetLabel(string key)
        {
            if (_actionLabel == null) return;
            var loc = LocalizationService.Instance;
            _actionLabel.text = loc != null ? loc.Get(key) : key;
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
