using System;
using Game.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Services;

namespace Game.OutGame.Lobby
{
    public class HeaderView : MonoBehaviour
    {
        [SerializeField] private Button   _avatarButton;
        [SerializeField] private Button   _settingsButton;
        [SerializeField] private TMP_Text _goldText;

        private void Awake()
        {
            _avatarButton.onClick.AddListener(OnAvatarTapped);
            _settingsButton?.onClick.AddListener(OnSettingsTapped);
        }

        private void Start()
        {
            if (AuthService.Instance != null)
            {
                AuthService.Instance.OnProfileChanged += UpdateAvatarUI;
            }
            UpdateAvatarUI();
        }

        private void OnDestroy()
        {
            if (AuthService.Instance != null)
            {
                AuthService.Instance.OnProfileChanged -= UpdateAvatarUI;
            }
        }

        private void UpdateAvatarUI()
        {
            if (AuthService.Instance == null) return;

            var avatarIconImg = _avatarButton.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (avatarIconImg == null) return;

            Sprite avatarSprite = null;
            var popupPrefab = Resources.Load<GameObject>("Prefabs/UI/AccountPopupView");
            if (popupPrefab != null)
            {
                var popupView = popupPrefab.GetComponent<Settings.AccountPopupView>();
                if (popupView != null)
                {
                    avatarSprite = popupView.GetAvatarSprite(AuthService.Instance.AvatarId);
                }
            }

            if (avatarSprite != null)
            {
                avatarIconImg.sprite = avatarSprite;
                avatarIconImg.preserveAspect = true;
            }
        }

        private void OnEnable()
        {
            if (CurrencyApiService.Instance != null)
                CurrencyApiService.Instance.OnGoldChanged += HandleGoldChanged;
            SetGold(PlayerProgressService.Instance?.Gold ?? 0);
        }

        private void OnDisable()
        {
            if (CurrencyApiService.Instance != null)
                CurrencyApiService.Instance.OnGoldChanged -= HandleGoldChanged;
        }

        private void HandleGoldChanged(long amount, long delta) => SetGold((int)amount);

        public void SetGold(int amount)
        {
            var anim = _goldText?.GetComponent<UINumberChange>();
            if (anim != null) anim.Set(amount);
            else if (_goldText != null) _goldText.text = amount.ToString("N0");
        }

        private void OnAvatarTapped()
        {
            Game.Core.UIManager.Instance?.ShowPopup<Game.OutGame.Settings.AccountPopupView>();
        }


        private void OnSettingsTapped()
        {
            Game.Core.UIManager.Instance?.ShowPopup<Game.OutGame.Settings.SettingsPanelView>();
        }
    }
}
