using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.UI;
using Game.Utils;

namespace Game.OutGame.Settings
{
    public class AccountPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text       _userIdText;
        [SerializeField] private Button         _linkAccountButton;
        [SerializeField] private Button         _switchAccountButton;
        [SerializeField] private Button         _closeButton;

        [Header("Profile Modifications")]
        [SerializeField] private TMP_InputField _displayNameInput;
        [SerializeField] private Button         _saveNicknameButton;
        [SerializeField] private RectTransform  _avatarGridParent;
        [SerializeField] private GameObject     _avatarSlotTemplate;

        [Header("Tabs")]
        [SerializeField] private Button         _avatarTabButton;
        [SerializeField] private Button         _boardThemeTabButton;
        [SerializeField] private GameObject     _nicknameArea;
        [SerializeField] private TMP_Text       _gridLabelText;

        [System.Serializable]
        public struct AvatarSpriteMapping
        {
            public int avatarId;
            public string resourceName;
            public Sprite sprite;
        }

        [SerializeField] private List<AvatarSpriteMapping> _avatarSprites = new List<AvatarSpriteMapping>();

        [System.Serializable]
        public struct BoardThemeSpriteMapping
        {
            public int themeId;
            public string resourceName;
            public Sprite borderSprite;
            public Sprite socketSprite;
        }

        [SerializeField] private List<BoardThemeSpriteMapping> _boardThemeSprites = new List<BoardThemeSpriteMapping>();

        private enum Tab { Avatars, BoardThemes }
        private static readonly Color TabPrimaryColor = new Color(1f, 0.3019608f, 0.4745098f);
        private static readonly Color TabSecondaryColor = new Color(0.3019608f, 0.1372549f, 0.3647059f);
        private Tab _currentTab = Tab.Avatars;

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

        public Sprite GetBoardThemeBorderSprite(int themeId)
        {
            if (_boardThemeSprites != null)
            {
                foreach (var mapping in _boardThemeSprites)
                {
                    if (mapping.themeId == themeId)
                        return mapping.borderSprite;
                }
            }
            return null;
        }

        private void Awake()
        {
            var auth = AuthService.Instance;
            bool isGuest = auth == null || auth.IsGuest;

            if (_userIdText != null)
                _userIdText.text = isGuest ? (LocalizationService.Instance?.Get("common.guest") ?? "Guest") : auth.UserId;

            if (_linkAccountButton   != null) _linkAccountButton.gameObject.SetActive(isGuest);
            if (_switchAccountButton != null) _switchAccountButton.gameObject.SetActive(!isGuest);

            _linkAccountButton?.onClick.AddListener(OnLinkAccount);
            _switchAccountButton?.onClick.AddListener(OnSwitchAccount);
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);

            // Bind nickname
            if (_displayNameInput != null && auth != null)
            {
                _displayNameInput.text = auth.DisplayName;
            }
            _saveNicknameButton?.onClick.AddListener(OnSaveNickname);

            // Bind tabs
            _avatarTabButton?.onClick.AddListener(() => SwitchTab(Tab.Avatars));
            _boardThemeTabButton?.onClick.AddListener(() => SwitchTab(Tab.BoardThemes));

            // Default view
            SwitchTab(Tab.Avatars);
        }

        private void SwitchTab(Tab tab)
        {
            _currentTab = tab;

            if (tab == Tab.Avatars)
            {
                if (_nicknameArea != null) _nicknameArea.SetActive(true);
                if (_gridLabelText != null) _gridLabelText.text = LocalizationService.Instance.Get("popup.account.choose_avatar");

                SetTabVisuals();
                PopulateAvatars();
            }
            else
            {
                if (_nicknameArea != null) _nicknameArea.SetActive(false);
                if (_gridLabelText != null) _gridLabelText.text = LocalizationService.Instance.Get("popup.account.choose_board_theme");

                SetTabVisuals();
                PopulateBoardThemes();
            }
        }

        private void SetTabVisuals()
        {
            bool avatarActive = _currentTab == Tab.Avatars;
            SetTabButtonColor(_avatarTabButton, avatarActive ? TabPrimaryColor : TabSecondaryColor);
            SetTabButtonColor(_boardThemeTabButton, avatarActive ? TabSecondaryColor : TabPrimaryColor);
        }

        private void SetTabButtonColor(Button button, Color color)
        {
            if (button == null) return;

            if (button.targetGraphic != null)
                button.targetGraphic.color = color;

            var textStyle = button.GetComponentInChildren<UITextStyle>();
            if (textStyle != null)
                textStyle.ApplyStyle();
        }

        private void OnSaveNickname()
        {
            if (_displayNameInput == null || PlayerApiService.Instance == null) return;
            string nickname = _displayNameInput.text.Trim();

            if (nickname.Length < 2 || nickname.Length > 24)
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.nickname_length_error"), Core.UI.ToastType.Error);
                return;
            }

            // ASCII validation: alphanumeric, space, underscore, hyphen
            foreach (char c in nickname)
            {
                if (!((c >= 'a' && c <= 'z') ||
                      (c >= 'A' && c <= 'Z') ||
                      (c >= '0' && c <= '9') ||
                      c == ' ' || c == '_' || c == '-'))
                {
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.nickname_char_error"), Core.UI.ToastType.Error);
                    return;
                }
            }

            UIManager.Instance?.ShowLoading();
            PlayerApiService.Instance.UpdateProfile(nickname, null, null, (ok, res, err) =>
            {
                UIManager.Instance?.HideLoading();
                if (ok && res != null)
                {
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.nickname_updated"), Core.UI.ToastType.Success);
                }
                else
                {
                    string errorMsg = LocalizationService.Instance.Get("toast.nickname_update_failed");
                    if (!string.IsNullOrEmpty(err))
                    {
                        errorMsg = LocalizationService.Instance != null 
                            ? LocalizationService.Instance.GetError(err) 
                            : err;
                    }
                    Game.Core.UIManager.Instance?.ShowToast(errorMsg, Core.UI.ToastType.Error);
                }
            });
        }

        private void PopulateAvatars()
        {
            if (_avatarGridParent == null || _avatarSlotTemplate == null) return;

            // Clear existing spawned slots (skip template)
            foreach (Transform child in _avatarGridParent)
            {
                if (child.gameObject != _avatarSlotTemplate)
                {
                    Destroy(child.gameObject);
                }
            }
            _avatarSlotTemplate.SetActive(false);

            var avatars = CsvLoader.Load<ProjectFill.Data.Generated.Avatar>(ProjectFill.Data.Generated.Avatar.ResourcePath);
            if (avatars == null) return;

            int currentEquippedAvatarId = AuthService.Instance != null ? AuthService.Instance.AvatarId : 1;

            foreach (var avatar in avatars)
            {
                var slotGo = Instantiate(_avatarSlotTemplate, _avatarGridParent);
                slotGo.SetActive(true);

                var iconImg = slotGo.transform.Find("Visual/Icon")?.GetComponent<Image>();
                var selectHighlight = slotGo.transform.Find("Visual/SelectedHighlight")?.gameObject;
                var lockOverlay = slotGo.transform.Find("Visual/LockOverlay")?.gameObject;
                var costText = slotGo.transform.Find("Visual/LockOverlay/CostText")?.GetComponent<TMP_Text>();
                var btn = slotGo.GetComponent<Button>();

                // Set sprite
                if (iconImg != null)
                {
                    iconImg.sprite = GetAvatarSprite(avatar.id);
                    iconImg.color = Color.white;
                    iconImg.preserveAspect = true;
                }

                // Equip highlight
                bool isEquipped = avatar.id == currentEquippedAvatarId;
                if (selectHighlight != null) selectHighlight.SetActive(isEquipped);

                // Lock state
                bool isUnlocked = PlayerProgressService.Instance != null && PlayerProgressService.Instance.IsAvatarUnlocked(avatar.id);
                if (lockOverlay != null) lockOverlay.SetActive(!isUnlocked);

                if (!isUnlocked && costText != null)
                {
                    costText.text = $"{avatar.unlock_cost}";
                }

                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnAvatarClicked(avatar, isUnlocked));
                }
            }
        }

        private void PopulateBoardThemes()
        {
            if (_avatarGridParent == null || _avatarSlotTemplate == null) return;

            // Clear existing spawned slots (skip template)
            foreach (Transform child in _avatarGridParent)
            {
                if (child.gameObject != _avatarSlotTemplate)
                {
                    Destroy(child.gameObject);
                }
            }
            _avatarSlotTemplate.SetActive(false);

            var themes = CsvLoader.Load<ProjectFill.Data.Generated.BoardTheme>(ProjectFill.Data.Generated.BoardTheme.ResourcePath);
            if (themes == null) return;

            int currentEquippedThemeId = PlayerProgressService.Instance != null ? PlayerProgressService.Instance.EquippedBoardThemeId : 1;

            foreach (var theme in themes)
            {
                var slotGo = Instantiate(_avatarSlotTemplate, _avatarGridParent);
                slotGo.SetActive(true);

                var iconImg = slotGo.transform.Find("Visual/Icon")?.GetComponent<Image>();
                var selectHighlight = slotGo.transform.Find("Visual/SelectedHighlight")?.gameObject;
                var lockOverlay = slotGo.transform.Find("Visual/LockOverlay")?.gameObject;
                var costText = slotGo.transform.Find("Visual/LockOverlay/CostText")?.GetComponent<TMP_Text>();
                var btn = slotGo.GetComponent<Button>();

                // Set sprite or color tint
                if (iconImg != null)
                {
                    Sprite customBorder = GetBoardThemeBorderSprite(theme.theme_id);
                    if (customBorder != null)
                    {
                        iconImg.sprite = customBorder;
                        iconImg.color = Color.white;
                    }
                    else
                    {
                        iconImg.sprite = null;
                        if (theme.theme_id == 1) iconImg.color = new Color(0.055f, 0.071f, 0.118f); // Classic
                        else if (theme.theme_id == 2) iconImg.color = new Color(0.15f, 0.95f, 1f); // Neon
                        else if (theme.theme_id == 3) iconImg.color = new Color(0.25f, 0.15f, 0.08f); // Wood
                        else if (theme.theme_id == 4) iconImg.color = new Color(1f, 0.78f, 0f); // Cyberpunk
                    }
                    iconImg.preserveAspect = true;
                }

                // Equip highlight
                bool isEquipped = theme.theme_id == currentEquippedThemeId;
                if (selectHighlight != null) selectHighlight.SetActive(isEquipped);

                // Lock state
                bool isUnlocked = PlayerProgressService.Instance != null && PlayerProgressService.Instance.IsThemeUnlocked(theme.theme_id);
                if (lockOverlay != null) lockOverlay.SetActive(!isUnlocked);

                if (!isUnlocked && costText != null)
                {
                    costText.text = $"{theme.unlock_cost}";
                }

                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnThemeClicked(theme, isUnlocked));
                }
            }
        }

        private void OnThemeClicked(ProjectFill.Data.Generated.BoardTheme theme, bool isUnlocked)
        {
            if (isUnlocked)
            {
                EquipBoardTheme(theme.theme_id);
            }
            else
            {
                if (PlayerProgressService.Instance != null && !PlayerProgressService.Instance.CanAfford(theme.unlock_cost))
                {
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.theme_not_enough_gold"), Core.UI.ToastType.Error);
                    return;
                }

                Game.Core.UIManager.Instance?.ShowPopup<Core.UI.ConfirmDialogView>(v => v.Init(
                    title:        LocalizationService.Instance.Get("popup.account.confirm_unlock_theme_title"),
                    body:         string.Format(LocalizationService.Instance.Get("popup.account.confirm_unlock_theme_body_fmt"), theme.unlock_cost),
                    confirmLabel: LocalizationService.Instance.Get("common.btn_unlock"),
                    onConfirm:    () => BuyAndEquipBoardTheme(theme.theme_id, theme.unlock_cost),
                    danger:       false));
            }
        }

        private void EquipBoardTheme(int themeId)
        {
            UIManager.Instance?.ShowLoading();
            PlayerApiService.Instance.UpdateProfile(null, null, themeId, (ok, res, err) =>
            {
                UIManager.Instance?.HideLoading();
                if (ok && res != null)
                {
                    PopulateBoardThemes(); // Refresh grid
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.theme_equipped"), Core.UI.ToastType.Success);
                }
                else
                {
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.theme_equip_failed"), Core.UI.ToastType.Error);
                }
            });
        }

        private void BuyAndEquipBoardTheme(int themeId, int cost)
        {
            UIManager.Instance?.ShowLoading();
            PlayerApiService.Instance.UpdateProfile(null, null, themeId, (ok, res, err) =>
            {
                UIManager.Instance?.HideLoading();
                if (ok && res != null)
                {
                    if (PlayerProgressService.Instance != null)
                    {
                        PlayerProgressService.Instance.SpendGold(cost);
                        PlayerProgressService.Instance.UnlockThemeLocally(themeId);
                    }
                    PopulateBoardThemes(); // Refresh grid
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.theme_unlocked"), Core.UI.ToastType.Success);
                }
                else
                {
                    string errorMsg = LocalizationService.Instance.Get("popup.account.confirm_failed_unlock_theme");
                    if (!string.IsNullOrEmpty(err))
                    {
                        errorMsg = LocalizationService.Instance != null 
                            ? LocalizationService.Instance.GetError(err) 
                            : err;
                    }
                    Game.Core.UIManager.Instance?.ShowToast(errorMsg, Core.UI.ToastType.Error);
                }
            });
        }

        private void OnAvatarClicked(ProjectFill.Data.Generated.Avatar avatar, bool isUnlocked)
        {
            if (isUnlocked)
            {
                EquipAvatar(avatar.id);
            }
            else
            {
                if (PlayerProgressService.Instance != null && !PlayerProgressService.Instance.CanAfford(avatar.unlock_cost))
                {
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.avatar_not_enough_gold"), Core.UI.ToastType.Error);
                    return;
                }

                Game.Core.UIManager.Instance?.ShowPopup<Core.UI.ConfirmDialogView>(v => v.Init(
                    title:        LocalizationService.Instance.Get("popup.account.confirm_unlock_avatar_title"),
                    body:         string.Format(LocalizationService.Instance.Get("popup.account.confirm_unlock_avatar_body_fmt"), avatar.unlock_cost),
                    confirmLabel: LocalizationService.Instance.Get("common.btn_unlock"),
                    onConfirm:    () => BuyAndEquipAvatar(avatar.id, avatar.unlock_cost),
                    danger:       false));
            }
        }

        private void EquipAvatar(int avatarId)
        {
            UIManager.Instance?.ShowLoading();
            PlayerApiService.Instance.UpdateProfile(null, avatarId, null, (ok, res, err) =>
            {
                UIManager.Instance?.HideLoading();
                if (ok && res != null)
                {
                    PopulateAvatars(); // Refresh grid
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.avatar_equipped"), Core.UI.ToastType.Success);
                }
                else
                {
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.avatar_equip_failed"), Core.UI.ToastType.Error);
                }
            });
        }

        private void BuyAndEquipAvatar(int avatarId, int cost)
        {
            UIManager.Instance?.ShowLoading();
            PlayerApiService.Instance.UpdateProfile(null, avatarId, null, (ok, res, err) =>
            {
                UIManager.Instance?.HideLoading();
                if (ok && res != null)
                {
                    if (PlayerProgressService.Instance != null)
                    {
                        PlayerProgressService.Instance.SpendGold(cost);
                        PlayerProgressService.Instance.UnlockAvatarLocally(avatarId);
                    }
                    PopulateAvatars(); // Refresh grid
                    Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.avatar_unlocked"), Core.UI.ToastType.Success);
                }
                else
                {
                    string errorMsg = LocalizationService.Instance.Get("popup.account.confirm_failed_unlock_avatar");
                    if (!string.IsNullOrEmpty(err))
                    {
                        errorMsg = LocalizationService.Instance != null 
                            ? LocalizationService.Instance.GetError(err) 
                            : err;
                    }
                    Game.Core.UIManager.Instance?.ShowToast(errorMsg, Core.UI.ToastType.Error);
                }
            });
        }

        private void OnLinkAccount()
        {
            var webClientId = Game.Core.AppConfig.GoogleWebClientId;
            if (string.IsNullOrEmpty(webClientId))
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_not_configured"), Core.UI.ToastType.Error);
                return;
            }

#if UNITY_ANDROID
            var bridge = Game.Core.GoogleSignInBridge.Instance;
            if (bridge == null)
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_unavailable"), Core.UI.ToastType.Error);
                return;
            }

            Game.Core.UIManager.Instance?.ShowLoading();
            bridge.SignIn(webClientId, (idToken, error) =>
            {
                if (string.IsNullOrEmpty(idToken))
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    if (error != "GOOGLE_SIGN_IN_CANCELLED")
                    {
                        var msg = LocalizationService.Instance?.GetError(error) ?? error;
                        Game.Core.UIManager.Instance?.ShowToast(msg, Core.UI.ToastType.Error);
                    }
                    return;
                }

                AuthService.Instance.LinkGoogle(idToken, null, (ok, err, linkResp) =>
                {
                    Debug.Log($"[LinkGoogle CB] ok={ok} err={err} | conflict={linkResp?.conflict} success={linkResp?.success} conflictToken={linkResp?.conflictToken}");
                    Game.Core.UIManager.Instance?.HideLoading();
                    if (!ok)
                    {
                        var msg = LocalizationService.Instance?.GetError(err) ?? err;
                        Game.Core.UIManager.Instance?.ShowToast(msg, Core.UI.ToastType.Error);
                        return;
                    }

                    if (linkResp != null && linkResp.conflict)
                    {
                        var local = linkResp.localSave;
                        var cloud = linkResp.cloudSave;
                        var token = linkResp.conflictToken;
                        void ShowConflict()
                        {
                            Game.Core.UIManager.Instance?.CloseTopPopup();
                            Game.Core.UIManager.Instance?.ShowPopup<AccountConflictPopupView>(v => v.Init(
                                localMaxStage: local?.maxStageId ?? 0,
                                localGold:     local?.gold ?? 0,
                                localStars:    local?.totalStars ?? 0,
                                localItems:    local?.totalItems ?? 0,
                                cloudMaxStage: cloud?.maxStageId ?? 0,
                                cloudGold:     cloud?.gold ?? 0,
                                cloudStars:    cloud?.totalStars ?? 0,
                                cloudItems:    cloud?.totalItems ?? 0,
                                onKeepLocal: () => ResolveConflict(token, "local"),
                                onKeepCloud: () => ResolveConflict(token, "cloud")
                            ));
                        }
                        var appear = GetComponent<Core.UI.UIPanelAppear>();
                        if (appear != null)
                            appear.Disappear(ShowConflict);
                        else
                            ShowConflict();
                    }
                    else
                    {
                        // No conflict — new Google account, link completed server-side
                        // CompleteSession will show restart popup if PID changed
                        // If no popup was shown, auth succeeded without switch (shouldn't happen for link flow)
                        Close();
                    }
                });
            });
#else
            Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_android_only"), Core.UI.ToastType.Error);
#endif
        }

        private void ResolveConflict(string conflictToken, string selection)
        {
            Game.Core.UIManager.Instance?.ShowLoading();
            AuthService.Instance.ResolveConflict(conflictToken, selection, (ok, err) =>
            {
                Game.Core.UIManager.Instance?.HideLoading();
                if (!ok)
                {
                    var msg = LocalizationService.Instance?.GetError(err) ?? err;
                    Game.Core.UIManager.Instance?.ShowToast(msg, Core.UI.ToastType.Error);
                }
                // On success: CompleteSession shows AccountRestartPopupView -> Boot redirect
            });
        }

        private void OnSwitchAccount()
        {
            Game.Core.UIManager.Instance?.ShowPopup<Core.UI.ConfirmDialogView>(v => v.Init(
                title:        LocalizationService.Instance.Get("popup.account.confirm_switch_title"),
                body:         LocalizationService.Instance.Get("popup.account.confirm_switch_body"),
                confirmLabel: LocalizationService.Instance.Get("common.btn_switch"),
                onConfirm:    DoSwitchAccount,
                danger:       false));
        }

        private void DoSwitchAccount()
        {
            var webClientId = Game.Core.AppConfig.GoogleWebClientId;
            if (string.IsNullOrEmpty(webClientId))
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_not_configured"), Core.UI.ToastType.Error);
                return;
            }

#if UNITY_ANDROID
            var bridge = Game.Core.GoogleSignInBridge.Instance;
            if (bridge == null)
            {
                Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_unavailable"), Core.UI.ToastType.Error);
                return;
            }

            Game.Core.UIManager.Instance?.ShowLoading();
            bridge.SignOut(webClientId);
            bridge.SignIn(webClientId, (idToken, error) =>
            {
                if (string.IsNullOrEmpty(idToken))
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    if (error != "GOOGLE_SIGN_IN_CANCELLED")
                    {
                        var msg = LocalizationService.Instance?.GetError(error) ?? error;
                        Game.Core.UIManager.Instance?.ShowToast(msg, Core.UI.ToastType.Error);
                    }
                    return;
                }

                AuthService.Instance.SwitchGoogle(idToken, null, (ok, err) =>
                {
                    Game.Core.UIManager.Instance?.HideLoading();
                    if (ok)
                    {
                        // Same account selected — no PID mismatch occurred
                        Game.Core.UIManager.Instance?.ShowToast(
                            LocalizationService.Instance.Get("toast.account_already_active"), Core.UI.ToastType.Warning);
                        Close();
                    }
                    else
                    {
                        var msg = LocalizationService.Instance?.GetError(err) ?? err;
                        Game.Core.UIManager.Instance?.ShowToast(msg, Core.UI.ToastType.Error);
                    }
                    // If PID mismatch detected: CompleteSession shows AccountRestartPopupView -> never reaches here
                });
            });
#else
            Game.Core.UIManager.Instance?.ShowToast(LocalizationService.Instance.Get("toast.google_signin_android_only"), Core.UI.ToastType.Error);
#endif
        }

        private void Close()
        {
            var appear = GetComponent<Core.UI.UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => Game.Core.UIManager.Instance?.CloseTopPopup());
            else
                Game.Core.UIManager.Instance?.CloseTopPopup();
        }
    }
}
