#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using Game.Core;
using Game.Core.UI;
using Game.InGame.View;
using Game.OutGame.Boot;
using Game.OutGame.Lobby;
using Game.OutGame.Settings;
using Game.Utils;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Game.Localization;
using static Game.Editor.StringIds;
using static Game.Editor.UIColorPalette;

namespace Game.Editor
{
    /// <summary>
    /// Tools/UI Setup — one-shot editor scripts.
    /// Generates base UI prefabs under Assets/UI/Prefabs/Base/ using project-fill Dark Neon Puzzle styling.
    /// Safe to re-run; outputs are overwritten without affecting scenes or final variants.
    /// Color palette is defined in UIColorPalette.cs.
    /// </summary>
    public static partial class UIEditorSetup
    {
        // Directory configurations
        private const string PrefabRoot      = "Assets/Resources/Prefabs/UI"; // Destination for UIManager Popups (Final Variants)
        private const string PrefabBase      = "Assets/UI/Prefabs/Base";      // Destination for Code-Generated Skeletons
        private const string BaseCommonPath  = "Assets/UI/Prefabs/Base/Common";
        private const string BaseScenesPath  = "Assets/UI/Prefabs/Base/Scenes";
        private const string PrefabFinal     = "Assets/UI/Prefabs/Final";     // Destination for Scene Canvases (Final Variants)
        private const string PrefabsGamePath = "Assets/Resources/Prefabs/Game"; // Runtime-instantiated gameplay prefabs (Chip/Lane/SignalNode)

        // True when current root was loaded via LoadPrefabContents — Save/SaveScenePrefab use this to choose cleanup
        private static bool _prefabWasLoaded;

        // EN font from FontLocalizationConfig; cached after first load; null if config not yet generated.
        private static TMP_FontAsset _editorDefaultFont;
        private static bool _editorFontLoaded;

        static TMP_FontAsset GetEditorDefaultFont()
        {
            if (_editorFontLoaded) return _editorDefaultFont;
            _editorFontLoaded = true;
            var config = AssetDatabase.LoadAssetAtPath<FontLocalizationConfig>(
                "Assets/Resources/Localization/FontLocalizationConfig.asset");
            if (config != null) _editorDefaultFont = config.GetFont(Language.EN);
            return _editorDefaultFont;
        }

        public enum TextCategory
        {
            Normal,
            Header,
            Button
        }

        // ════════════════════════════════════════════════════════════════
        //  MENU
        // ════════════════════════════════════════════════════════════════

        [MenuItem("Tools/UI Setup/1 - Create All Prefabs", false, 100)]
        static void CreateAllPrefabs()
        {
            EnsureDirs();
            CreateConfirmDialog();
            CreateToast();
            CreateLoadingOverlay();
            CreateNetworkError();
            CreateForceUpdateView();
            CreateRewardPopup();
            CreateReLoginView();
            CreateStageInfoPopup();
            CreateResultOverlay();
            CreateFailOverlay();
            CreatePausePopup();
            CreateSettingsPanel();
            CreateAccountPopup();
            CreateAccountRestartPopup();
            CreateAccountConflictPopup();
            CreateStageNodeView();
            CreateTutorialOverlay();
            CreateChapterChest();
            CreateRankingItemPrefab();
            CreateLobbyBadgeItem();
            CreateProductCardPrefab();
            CreateShopCategoryHeader();
            CreateItemTooltip();

            // OutGame systems popups (cosmetic/avatar sections + cells built inside SetupLobby)
            CreateCosmeticPreviewPopup();
            CreateAvatarPreviewPopup();
            CreateAttendancePopup();
            CreateAchievementToast();
            CreateDailyChallengePopup();

            // Signal Sort gameplay prefabs — must exist before SetupInGame wires them
            CreateInGameChip();
            CreateInGameLane();
            CreateInGameSignalNode();

            // Generate Scenes as well
            SetupBoot();
            SetupLobby();
            SetupInGame();
            
            AssetDatabase.Refresh();
            Debug.Log("[UIEditorSetup] All base popups & scenes created successfully.");

            // Open Boot scene upon successful completion
            string bootScenePath = "Assets/Scenes/Boot.unity";
            if (File.Exists(bootScenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(bootScenePath);
                Debug.Log("[UIEditorSetup] Successfully opened Boot.unity scene.");
            }
            else
            {
                Debug.LogError("[UIEditorSetup] Boot.unity scene not found!");
            }
        }

        [MenuItem("Tools/UI Setup/Prefabs/AccountConflictPopup", false, 110)]
        static void CreateAccountConflictPopupSingle() { EnsureDirs(); CreateAccountConflictPopup(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/AccountPopup",    false, 111)]
        static void CreateAccountPopupSingle()   { EnsureDirs(); CreateAccountPopup();   AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/AccountRestartPopup", false, 112)]
        static void CreateAccountRestartPopupSingle() { EnsureDirs(); CreateAccountRestartPopup(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/BootCanvas",       false, 113)]
        static void CreateBootCanvasSingle()     { EnsureDirs(); SetupBoot();            AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ChapterChest",     false, 114)]
        static void CreateChapterChestSingle()    { EnsureDirs(); CreateChapterChest();    AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ConfirmDialog",  false, 115)]
        static void CreateConfirmDialogSingle()  { EnsureDirs(); CreateConfirmDialog();  AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/FailOverlay",     false, 116)]
        static void CreateFailOverlaySingle()    { EnsureDirs(); CreateFailOverlay();    AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/IAPProductCard",  false, 117)]
        static void CreateIAPProductCardSingle() { EnsureDirs(); CreateProductCardPrefab(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ForceUpdateView", false, 119)]
        static void CreateForceUpdateViewSingle() { EnsureDirs(); CreateForceUpdateView(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/InGameCanvas",     false, 118)]
        static void CreateInGameCanvasSingle()   { EnsureDirs(); SetupInGame();          AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/InGameChip",       false, 136)]
        static void CreateInGameChipSingle()     { EnsureDirs(); CreateInGameChip();      AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/InGameLane",       false, 137)]
        static void CreateInGameLaneSingle()     { EnsureDirs(); CreateInGameLane();      AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/InGameSignalNode", false, 138)]
        static void CreateInGameSignalNodeSingle() { EnsureDirs(); CreateInGameSignalNode(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ItemTooltip",      false, 120)]
        static void CreateItemTooltipSingle()     { EnsureDirs(); CreateItemTooltip();     AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/LoadingOverlay",  false, 121)]
        static void CreateLoadingOverlaySingle() { EnsureDirs(); CreateLoadingOverlay(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/LobbyCanvas",      false, 122)]
        static void CreateLobbyCanvasSingle()    { EnsureDirs(); SetupLobby();           AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/NetworkError",    false, 123)]
        static void CreateNetworkErrorSingle()   { EnsureDirs(); CreateNetworkError();   AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/PausePopup",      false, 124)]
        static void CreatePausePopupSingle()     { EnsureDirs(); CreatePausePopup();     AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ReLoginView",     false, 125)]
        static void CreateReLoginViewSingle()    { EnsureDirs(); CreateReLoginView();    AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ResultOverlay",   false, 126)]
        static void CreateResultOverlaySingle()  { EnsureDirs(); CreateResultOverlay();  AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/RewardPopup",     false, 127)]
        static void CreateRewardPopupSingle()    { EnsureDirs(); CreateRewardPopup();    AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/SettingsPanel",   false, 128)]
        static void CreateSettingsPanelSingle()  { EnsureDirs(); CreateSettingsPanel();  AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/StageInfoPopup",  false, 129)]
        static void CreateStageInfoPopupSingle() { EnsureDirs(); CreateStageInfoPopup(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/StageNodeView",    false, 130)]
        static void CreateStageNodeViewSingle()  { EnsureDirs(); CreateStageNodeView();  AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/Toast",           false, 132)]
        static void CreateToastSingle()          { EnsureDirs(); CreateToast();          AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/ShopCategoryHeader", false, 134)]
        static void CreateShopCategoryHeaderSingle() { EnsureDirs(); CreateShopCategoryHeader(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/TutorialOverlay",  false, 133)]
        static void CreateTutorialOverlaySingle() { EnsureDirs(); CreateTutorialOverlay(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/AvatarPreviewPopup", false, 140)]
        static void CreateAvatarPreviewPopupSingle() { EnsureDirs(); CreateAvatarPreviewPopup(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/AchievementToast", false, 141)]
        static void CreateAchievementToastSingle() { EnsureDirs(); CreateAchievementToast(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/AttendancePopup", false, 142)]
        static void CreateAttendancePopupSingle() { EnsureDirs(); CreateAttendancePopup(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/CosmeticItemCell", false, 143)]
        static void CreateCosmeticItemCellSingle() { EnsureDirs(); BuildCosmeticCell(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/CosmeticPreviewPopup", false, 144)]
        static void CreateCosmeticPreviewPopupSingle() { EnsureDirs(); CreateCosmeticPreviewPopup(); AssetDatabase.Refresh(); }

        [MenuItem("Tools/UI Setup/Prefabs/DailyChallengePopup", false, 145)]
        static void CreateDailyChallengePopupSingle() { EnsureDirs(); CreateDailyChallengePopup(); AssetDatabase.Refresh(); }

        static void SetupBoot()
        {
            var (canvas, _bootLoaded) = LoadOrCreateCanvas("Boot");
            var content = Child(canvas, "Content");
            Stretch(content);
            TMP(content, "LogoText", Center(0, 200, 600, 120), 48, UI_TEXT, "FILL", AppTitle, TextCategory.Header);

            // Animated loader indicator representing spinner
            var loaderGo = Child(content, "LoaderIcon");
            Fixed(loaderGo, new Vector2(0, -100), new Vector2(100, 100));
            Img(loaderGo, UI_CTA);
            var loaderAnim = Comp<UIIconIdleAnimator>(loaderGo);
            loaderAnim.Configure(UIIconIdleAnimator.AnimationType.Rotate, 3f, 45f); // Spin loader

            TMP(content, "SpinnerText", Center(0, -200, 600, 80), 24, UI_TEXT, "Loading...", BootLoading, TextCategory.Normal);

            SaveScenePrefab(canvas, "Boot", _bootLoaded);
        }

        static void SetupLobby()
        {
            var (canvas, _lobbyLoaded) = LoadOrCreateCanvas("Lobby");
            Comp<LobbyView>(canvas);
            var resMap = LoadDynamicResourceMap();

            // SafeAreaRoot — fills Screen.safeArea
            var safeRoot = Child(canvas, "SafeAreaRoot");
            Stretch(safeRoot);
            Comp<SafeAreaHandler>(safeRoot);

            // Header — Canvas direct child so background reaches physical screen top (not safe area top)
            var header = Child(canvas, "Header");
            TopStrip(header, 180);
            Img(header, UI_BG_DEEP);
            var hv = Comp<HeaderView>(header);
            
            // Avatar Button (Square layout with C# idle floating, adjusted to width 96)
            var avatarBtn = Btn(header, "AvatarButton", new Vector2(-450, 0), new Vector2(96, 96), UI_BG_MID, "");
            var avatarIconImg = avatarBtn.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (avatarIconImg != null && avatarIconImg.sprite == null && resMap.TryGetValue("ui_avatar_default", out string avp))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(avp);
                if (spr != null) { avatarIconImg.sprite = spr; avatarIconImg.preserveAspect = true; }
            }

            // Settings Button (Matching Avatar Button on the right, width 96)
            var settingsBtn = Btn(header, "SettingsButton", new Vector2(450, 0), new Vector2(96, 96), UI_BG_MID, "");
            var settingsIconImg = settingsBtn.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (settingsIconImg != null && settingsIconImg.sprite == null && resMap.TryGetValue("ui_settings_icon", out string setip))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(setip);
                if (spr != null) { settingsIconImg.sprite = spr; settingsIconImg.preserveAspect = true; }
            }


            // Gold Container - Pill layout with gold border (adjusted position and width)
            var goldContainer = Child(header, "GoldContainer");
            Fixed(goldContainer, new Vector2(0, 0), new Vector2(280, 96));
            Img(goldContainer, UI_BG_MID);
            
            var goldBorder = Child(goldContainer, "Border");
            Stretch(goldBorder);
            var goldBorderImg = Img(goldBorder, Hex("2B003B"));
            goldBorder.transform.SetAsFirstSibling();
            goldBorderImg.rectTransform.offsetMin = new Vector2(-4, -4);
            goldBorderImg.rectTransform.offsetMax = new Vector2(4, 4);

            // Animated Gold Icon
            var goldIcon = Child(goldContainer, "Icon");
            Fixed(goldIcon, new Vector2(-85, 0), new Vector2(80, 80));
            var goldIconImg = Img(goldIcon, UI_CTA);
            if (resMap.TryGetValue("ui_gold_icon", out string gip))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(gip);
                if (spr != null) { goldIconImg.sprite = spr; goldIconImg.preserveAspect = true; }
            }
            var goldIconAnim = Comp<UIIconIdleAnimator>(goldIcon);
            goldIconAnim.Configure(UIIconIdleAnimator.AnimationType.GlowSweep, 2.2f, 12f);

            var goldText = TMP(goldContainer, "GoldText", Center(35, 0, 150, 70), 22, UI_CTA, "0", null, TextCategory.Normal);
            var goldCsf = Comp<ContentSizeFitter>(goldText.gameObject);
            goldCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var goldNumAnim = Comp<UINumberChange>(goldText.gameObject);
            var soGoldAnim = new SerializedObject(goldNumAnim);
            soGoldAnim.FindProperty("_formatString").stringValue = "{0:N0}";
            soGoldAnim.ApplyModifiedProperties();

            // BottomNavBar — bottom 160px, HorizontalLayoutGroup for tab distribution
            var navBar = Child(safeRoot, "BottomNavBar");
            BottomStrip(navBar, 160);
            Img(navBar, UI_BG_DEEP);
            var bnv = Comp<BottomNavBarView>(navBar);
            var navHlg = Comp<HorizontalLayoutGroup>(navBar);
            navHlg.childAlignment      = TextAnchor.MiddleCenter;
            navHlg.childControlWidth      = true;
            navHlg.childControlHeight     = true;
            navHlg.childForceExpandWidth  = true;
            navHlg.childForceExpandHeight = true;
            navHlg.padding = new RectOffset(0, 0, 0, 0);
            navHlg.spacing = 0;

            var shopBtn    = BtnNavTab(navBar, "ShopButton",    UI_BG_MID, "Shop", NavShop);
            var homeBtn    = BtnNavTab(navBar, "HomeButton",    UI_BG_MID, "Home", NavHome);
            var rankBtn    = BtnNavTab(navBar, "RankingButton", UI_BG_MID, "Rank", NavRanking);
            var achBtn     = BtnNavTab(navBar, "AchievementButton", UI_BG_MID, "Award", "popup.achievement.title");
            if (resMap.TryGetValue("nav_shop",    out string nsp)) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(nsp); if (s != null) { var i = shopBtn.transform.Find("Visual/Icon")?.GetComponent<Image>(); if (i != null) i.sprite = s; } }
            if (resMap.TryGetValue("nav_home",    out string nhp)) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(nhp); if (s != null) { var i = homeBtn.transform.Find("Visual/Icon")?.GetComponent<Image>(); if (i != null) i.sprite = s; } }
            if (resMap.TryGetValue("nav_ranking", out string nrp)) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(nrp); if (s != null) { var i = rankBtn.transform.Find("Visual/Icon")?.GetComponent<Image>(); if (i != null) i.sprite = s; } }
            if (resMap.TryGetValue("nav_achievement", out string nap)) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(nap); if (s != null) { var i = achBtn.transform.Find("Visual/Icon")?.GetComponent<Image>(); if (i != null) i.sprite = s; } }

            // Tab content area — Canvas direct child so top padding is in screen space (aligns with Header)
            var tabContent = Child(canvas, "TabContent");
            PaddedStretch(tabContent, 180, 160);

            // HomeTab — ScrollRect with curved zigzag map path
            var homeTab = Child(tabContent, "HomeTab"); Stretch(homeTab);
            var htv = Comp<HomeTabView>(homeTab);
            if (!homeTab.TryGetComponent<ScrollRect>(out var scrollRect))
                scrollRect = Comp<ScrollRect>(homeTab);
            scrollRect.horizontal = false; scrollRect.vertical = true;
            
            var viewportGo = Child(homeTab, "Viewport"); Stretch(viewportGo);
            Comp<RectMask2D>(viewportGo);
            var homeVpImg = Img(viewportGo, new Color(0, 0, 0, 0));
            homeVpImg.raycastTarget = true;
            
            var contentGo = Child(viewportGo, "Content");
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 3000);
            scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
            scrollRect.content  = contentRt;

            // Generate stage node placeholders in a beautiful curved S-shape path
            for (int i = 1; i <= 12; i++)
            {
                float angle = i * 1.3f;
                float x = Mathf.Sin(angle) * 260f; // Curved pathway sine-wave
                float y = -180f - (i - 1) * 230f; // Scrolling downwards
                
                var nodeGo = Child(contentGo, $"StageNode_{i}");
                Fixed(nodeGo, new Vector2(x, y), new Vector2(130f, 130f));
                Img(nodeGo, UI_PRIMARY);
                
                var btn = Comp<Button>(nodeGo);
                Comp<UIButtonAnimator>(nodeGo);
                var anim = Comp<UIIconIdleAnimator>(nodeGo);
                anim.Configure(UIIconIdleAnimator.AnimationType.Float, 1.8f, 6f); // floating nodes
                
                TMP(nodeGo, "Label", Center(0, 0, 130, 130), 22, UI_TEXT, i.ToString(), null, TextCategory.Button);
            }

            var nodeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/StageNodeView.prefab");
            if (nodeAsset == null)
                nodeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BaseCommonPath + "/StageNodeView.prefab");

            var soHtv = new SerializedObject(htv);
            soHtv.FindProperty("_scrollRect").objectReferenceValue = scrollRect;
            if (resMap.TryGetValue("toast_success", out string tsPath))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(tsPath);
                if (s != null)
                {
                    soHtv.FindProperty("_guideOrbSprite").objectReferenceValue = s;
                }
            }
            soHtv.ApplyModifiedProperties();

            // ── LobbyBadgeContainer & Groups Setup (Event Layout Left, Buy Layout Right) ──
            var badgeContainerGo = Child(homeTab, "LobbyBadgeContainer");
            Stretch(badgeContainerGo);
            var badgeContainer = Comp<LobbyBadgeContainer>(badgeContainerGo);

            var eventGroup = Child(badgeContainerGo, "EventLayoutGroup");
            var eventRt = RT(eventGroup);
            eventRt.anchorMin = new Vector2(0, 1); eventRt.anchorMax = new Vector2(0, 1);
            eventRt.pivot = new Vector2(0, 1);
            eventRt.anchoredPosition = new Vector2(30, -260); // placed below Header HUD
            eventRt.sizeDelta = new Vector2(160, 600);
            var evHlg = Comp<VerticalLayoutGroup>(eventGroup);
            evHlg.childAlignment = TextAnchor.UpperCenter;
            evHlg.spacing = 15;
            evHlg.childControlHeight = evHlg.childControlWidth = false;

            var buyGroup = Child(badgeContainerGo, "BuyLayoutGroup");
            var buyRt = RT(buyGroup);
            buyRt.anchorMin = new Vector2(1, 1); buyRt.anchorMax = new Vector2(1, 1);
            buyRt.pivot = new Vector2(1, 1);
            buyRt.anchoredPosition = new Vector2(-30, -260); // placed below Header HUD
            buyRt.sizeDelta = new Vector2(160, 600);
            var buyHlg = Comp<VerticalLayoutGroup>(buyGroup);
            buyHlg.childAlignment = TextAnchor.UpperCenter;
            buyHlg.spacing = 15;
            buyHlg.childControlHeight = buyHlg.childControlWidth = false;

            var badgeItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BaseCommonPath}/LobbyBadgeItem.prefab");

            var soBadgeContainer = new SerializedObject(badgeContainer);
            soBadgeContainer.FindProperty("_eventLayoutGroup").objectReferenceValue = eventRt;
            soBadgeContainer.FindProperty("_buyLayoutGroup").objectReferenceValue = buyRt;
            if (badgeItemPrefab != null)
            {
                soBadgeContainer.FindProperty("_badgePrefab").objectReferenceValue = badgeItemPrefab;
            }
            soBadgeContainer.ApplyModifiedProperties();

            soHtv.FindProperty("_badgeContainer").objectReferenceValue = badgeContainer;
            soHtv.ApplyModifiedProperties();

            // ── ShopTab Packages & Layout Setup ──
            var shopTab = Child(tabContent, "ShopTab");  Stretch(shopTab); shopTab.SetActive(false);
            var shopView = Comp<ShopTabView>(shopTab);

            // Redesigned: Shop Title Ribbon with 3D shadow (Top-Center Anchored to Y=0 exactly below HUD)
            var shopTitleShadow = shopTab.transform.Find("ShopTitleRibbonShadow")?.gameObject;
            if (shopTitleShadow == null)
            {
                shopTitleShadow = Child(shopTab, "ShopTitleRibbonShadow");
                var rtShadow = RT(shopTitleShadow);
                rtShadow.anchorMin = new Vector2(0.5f, 1f);
                rtShadow.anchorMax = new Vector2(0.5f, 1f);
                rtShadow.pivot = new Vector2(0.5f, 1f);
                rtShadow.anchoredPosition = new Vector2(0, -8); // Shadow Y=-8 offset relative to ribbon Y=0
                rtShadow.sizeDelta = new Vector2(640, 88);
                Img(shopTitleShadow, Hex("1A0B22"));
            }

            var shopTitleRibbon = shopTab.transform.Find("ShopTitleRibbon")?.gameObject;
            if (shopTitleRibbon == null)
            {
                shopTitleRibbon = Child(shopTab, "ShopTitleRibbon");
                var rtTitle = RT(shopTitleRibbon);
                rtTitle.anchorMin = new Vector2(0.5f, 1f);
                rtTitle.anchorMax = new Vector2(0.5f, 1f);
                rtTitle.pivot = new Vector2(0.5f, 1f);
                rtTitle.anchoredPosition = new Vector2(0, 0); // Placed exactly below HUD (starts at top of tabContent)
                rtTitle.sizeDelta = new Vector2(640, 88);
                Img(shopTitleRibbon, UI_CTA); // Amber yellow background

                var strBorder = Child(shopTitleRibbon, "Border");
                Stretch(strBorder);
                var strBImg = Img(strBorder, UI_BORDER);
                shopTitleRibbon.transform.SetAsFirstSibling();
                strBImg.rectTransform.offsetMin = new Vector2(-2, -2);
                strBImg.rectTransform.offsetMax = new Vector2(2, 2);

                var shopTitleText = TMP(shopTitleRibbon, "Text", Center(0, 0, 600, 80), 36, Hex("1A0B22"), "SHOP", "shop.iap.title", TextCategory.Header);
                shopTitleText.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                var txtTrans = shopTitleRibbon.transform.Find("Text");
                if (txtTrans != null && txtTrans.TryGetComponent<LocalizedText>(out var lt))
                {
                    var soLt = new SerializedObject(lt);
                    var stringIdProp = soLt.FindProperty("_stringId");
                    if (stringIdProp != null && string.IsNullOrEmpty(stringIdProp.stringValue))
                    {
                        stringIdProp.stringValue = "shop.iap.title";
                        soLt.ApplyModifiedProperties();
                    }
                }
            }

            // Scroll View for Shop Content (shifted down for Ribbon space)
            var shopScrollGo = Child(shopTab, "ShopScrollView");
            var ssRt = RT(shopScrollGo);
            ssRt.anchorMin = Vector2.zero; ssRt.anchorMax = Vector2.one;
            ssRt.offsetMin = new Vector2(0, 0); ssRt.offsetMax = new Vector2(0, -100); // top title margin (ribbon height 88 + shadow offset 8 = 96)

            var shopScroll = Comp<ScrollRect>(shopScrollGo);
            shopScroll.horizontal = false; shopScroll.vertical = true;

            var shopViewportGo = Child(shopScrollGo, "Viewport"); Stretch(shopViewportGo);
            Comp<RectMask2D>(shopViewportGo);
            var shopVpImg = Img(shopViewportGo, new Color(0, 0, 0, 0));
            shopVpImg.raycastTarget = true;
            shopScroll.viewport = shopViewportGo.GetComponent<RectTransform>();

            var shopContentGo = Child(shopViewportGo, "Content");
            var shopContentRt = shopContentGo.GetComponent<RectTransform>();
            shopContentRt.anchorMin = new Vector2(0, 1); shopContentRt.anchorMax = new Vector2(1, 1);
            shopContentRt.pivot = new Vector2(0.5f, 1);
            shopContentRt.sizeDelta = new Vector2(0, 0); // height driven by ContentSizeFitter below
            shopScroll.content = shopContentRt;

            var shopContentVlg = Comp<VerticalLayoutGroup>(shopContentGo);
            shopContentVlg.childAlignment      = TextAnchor.UpperCenter;
            shopContentVlg.spacing             = 16;
            shopContentVlg.padding             = new RectOffset(40, 40, 40, 40);
            shopContentVlg.childControlWidth   = true;
            shopContentVlg.childControlHeight  = true;  // uses LayoutElement.preferredHeight per child
            shopContentVlg.childForceExpandHeight = false; // spacers define section gaps; don't force-fill
            // Content height must track actual child sum (IAP cards + cosmetic 780 + avatar 380) — fixed height clipped the bottom.
            var shopContentCsf = Comp<ContentSizeFitter>(shopContentGo);
            shopContentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Cleanup only obsolete cards if they exist in the prefab (no longer created by editor script, but might be leftover)
            var leftover1 = shopContentGo.transform.Find("StarterPackCard");
            if (leftover1 != null) UnityEngine.Object.DestroyImmediate(leftover1.gameObject);
            var leftover2 = shopContentGo.transform.Find("NoAdsCard");
            if (leftover2 != null) UnityEngine.Object.DestroyImmediate(leftover2.gameObject);
            var leftover3 = shopContentGo.transform.Find("MasterBundleCard");
            if (leftover3 != null) UnityEngine.Object.DestroyImmediate(leftover3.gameObject);

            // Remove legacy static category ribbons — replaced by runtime-generated headers in ShopTabView
            var ribbonSpecial = shopContentGo.transform.Find("RibbonSpecialOffers");
            if (ribbonSpecial != null) Object.DestroyImmediate(ribbonSpecial.gameObject);
            var spaceBefore = shopContentGo.transform.Find("SpaceBeforeBundles");
            if (spaceBefore != null) Object.DestroyImmediate(spaceBefore.gameObject);
            var ribbonBundles = shopContentGo.transform.Find("RibbonBundles");
            if (ribbonBundles != null) Object.DestroyImmediate(ribbonBundles.gameObject);

            // ── Cosmetic section (appended below IAP; ShopTabView keeps it last sibling) ──
            var cosmeticCell = BuildCosmeticCell();
            var cosmeticSection = Child(shopContentGo, "CosmeticSection");
            var csRt = RT(cosmeticSection); csRt.sizeDelta = new Vector2(0, 780);
            var csLe = Comp<LayoutElement>(cosmeticSection); csLe.preferredHeight = 780; csLe.flexibleWidth = 1;
            var csView = Comp<CosmeticSectionView>(cosmeticSection);

            TMP(cosmeticSection, "SectionTitle", Center(0, 350, 600, 60), 26, UI_CTA, "Cosmetics", "shop.cosmetic.section_title", TextCategory.Header);

            var catTabs = Child(cosmeticSection, "CategoryTabs");
            var ctRt = RT(catTabs);
            ctRt.anchorMin = new Vector2(0.5f, 1f); ctRt.anchorMax = new Vector2(0.5f, 1f); ctRt.pivot = new Vector2(0.5f, 1f);
            ctRt.anchoredPosition = new Vector2(0, -90); ctRt.sizeDelta = new Vector2(720, 80);
            var ctHlg = Comp<HorizontalLayoutGroup>(catTabs);
            ctHlg.spacing = 10; ctHlg.childAlignment = TextAnchor.MiddleCenter;
            ctHlg.childControlWidth = true; ctHlg.childControlHeight = true;
            ctHlg.childForceExpandWidth = true; ctHlg.childForceExpandHeight = true;
            var chipTab = BtnHlg(catTabs, "ChipTab", UI_CTA, "Chip", "shop.cosmetic.tab_chip");
            var laneTab = BtnHlg(catTabs, "LaneTab", UI_BG_MID, "Lane", "shop.cosmetic.tab_lane");
            var boardTab = BtnHlg(catTabs, "BoardTab", UI_BG_MID, "Board", "shop.cosmetic.tab_board");

            var grid = Child(cosmeticSection, "Grid");
            var gRt = RT(grid);
            gRt.anchorMin = new Vector2(0, 0); gRt.anchorMax = new Vector2(1, 1);
            gRt.offsetMin = new Vector2(20, 20); gRt.offsetMax = new Vector2(-20, -190);
            var glg = Comp<GridLayoutGroup>(grid);
            glg.cellSize = new Vector2(210, 240); glg.spacing = new Vector2(14, 14);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; glg.constraintCount = 4;
            glg.childAlignment = TextAnchor.UpperCenter;

            var soCs = new SerializedObject(csView);
            soCs.FindProperty("_chipTabButton").objectReferenceValue = chipTab.GetComponent<Button>();
            soCs.FindProperty("_laneTabButton").objectReferenceValue = laneTab.GetComponent<Button>();
            soCs.FindProperty("_boardTabButton").objectReferenceValue = boardTab.GetComponent<Button>();
            soCs.FindProperty("_gridContainer").objectReferenceValue = gRt;
            soCs.FindProperty("_cellPrefab").objectReferenceValue = cosmeticCell;
            soCs.ApplyModifiedProperties();

            // ── Avatar section (appended below the cosmetic section; ShopTabView keeps it last sibling) ──
            var avatarCard = BuildAvatarCard();
            var avatarSection = Child(shopContentGo, "AvatarSection");
            var asRt = RT(avatarSection); asRt.sizeDelta = new Vector2(0, 380);
            var asLe = Comp<LayoutElement>(avatarSection); asLe.preferredHeight = 380; asLe.flexibleWidth = 1;
            var avView = Comp<AvatarSectionView>(avatarSection);

            TMP(avatarSection, "SectionTitle", Center(0, 150, 600, 60), 26, UI_CTA, "Avatars", PopupAccountLabelSelectAvatar, TextCategory.Header);

            // Horizontal scroll carousel of avatar cards
            var avScrollGo = Child(avatarSection, "AvatarScroll");
            var avScrollRt = RT(avScrollGo);
            avScrollRt.anchorMin = new Vector2(0, 0); avScrollRt.anchorMax = new Vector2(1, 1);
            avScrollRt.offsetMin = new Vector2(20, 20); avScrollRt.offsetMax = new Vector2(-20, -110);
            var avScroll = Comp<ScrollRect>(avScrollGo);
            avScroll.horizontal = true; avScroll.vertical = false;
            var avVp = Child(avScrollGo, "Viewport"); Stretch(avVp); Comp<RectMask2D>(avVp);
            var avVpImg = Img(avVp, new Color(0, 0, 0, 0)); avVpImg.raycastTarget = true;
            avScroll.viewport = RT(avVp);
            var avContent = Child(avVp, "Content");
            var avContentRt = RT(avContent);
            avContentRt.anchorMin = new Vector2(0, 0); avContentRt.anchorMax = new Vector2(0, 1); avContentRt.pivot = new Vector2(0, 0.5f);
            avContentRt.sizeDelta = new Vector2(0, 0);
            avScroll.content = avContentRt;
            var avHlg = Comp<HorizontalLayoutGroup>(avContent);
            avHlg.spacing = 14; avHlg.childAlignment = TextAnchor.MiddleLeft;
            avHlg.childControlWidth = false; avHlg.childControlHeight = false;
            avHlg.childForceExpandWidth = false; avHlg.childForceExpandHeight = false;
            avHlg.padding = new RectOffset(10, 10, 10, 10);
            var avCsf = Comp<ContentSizeFitter>(avContent); avCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var soAv = new SerializedObject(avView);
            soAv.FindProperty("_gridContainer").objectReferenceValue = avContentRt;
            soAv.FindProperty("_cardPrefab").objectReferenceValue = avatarCard;
            PopulateAvatarSprites(soAv.FindProperty("_avatarSprites"), resMap);
            soAv.ApplyModifiedProperties();

            // Wire ShopTabView refs
            var soShop = new SerializedObject(shopView);
            soShop.FindProperty("_contentContainer").objectReferenceValue = shopContentRt;
            soShop.FindProperty("_cosmeticSection").objectReferenceValue = csRt;
            soShop.FindProperty("_avatarSection").objectReferenceValue = asRt;
            soShop.ApplyModifiedProperties();

            var rankingTab = Child(tabContent, "RankingTab"); Stretch(rankingTab); rankingTab.SetActive(false);
            var rankingView = Comp<RankingTabView>(rankingTab);
            // Legacy cleanup: old tab buttons replaced by the stage/perfect/weekly 3-tab redesign
            foreach (var legacy in new[] { "StarsTabButton", "MaxStageTabButton", "ChallengeTabButton" })
            {
                var oldTab = rankingTab.transform.Find(legacy);
                if (oldTab != null) Object.DestroyImmediate(oldTab.gameObject);
            }
            var stageTab = Btn(rankingTab, "StageTabButton", new Vector2(-280, 700), new Vector2(260, 80), UI_PRIMARY, "Stage", LobbyRankingTabStages);
            var perfectTab = Btn(rankingTab, "PerfectTabButton", new Vector2(0, 700), new Vector2(260, 80), UI_BG_MID, "Perfect", LobbyRankingTabPerfect);
            var weeklyTab = Btn(rankingTab, "WeeklyTabButton", new Vector2(280, 700), new Vector2(260, 80), UI_BG_MID, "Weekly", LobbyRankingTabWeekly);

            var rankTitle = TMP(rankingTab, "TitleText", Center(0, 625, 760, 70), 30, UI_CTA, "Stage Ranking", LobbyRankingStagesTitle, TextCategory.Header);
            // Per-tab description, placed directly below the tab row (above the my-rank line)
            var rankDesc = TMP(rankingTab, "DescText", Center(0, 558, 820, 56), 22, UI_TEXT, "Pioneers who reached the farthest stage", LobbyRankingDescStage, TextCategory.Normal);
            var myRank = TMP(rankingTab, "MyRankText", Center(0, 478, 760, 80), 24, UI_TEXT, "My Rank: -", LobbyRankingMyRankEmpty, TextCategory.Normal);
            var entries = TMP(rankingTab, "EntriesText", Center(0, -190, 820, 1220), 20, UI_TEXT, "Ranking unavailable", LobbyRankingUnavailable, TextCategory.Normal);
            entries.alignment = TextAlignmentOptions.TopLeft;

            // Generate VirtualizedScrollRect hierarchy
            var scrollRectGo = Child(rankingTab, "VirtualizedScrollRect");
            var srRt = RT(scrollRectGo);
            srRt.anchorMin = new Vector2(0.5f, 0f);
            srRt.anchorMax = new Vector2(0.5f, 1f);
            srRt.pivot = new Vector2(0.5f, 0.5f);
            srRt.sizeDelta = new Vector2(820, -380); // height margin top = 380, bottom = 0
            srRt.anchoredPosition = new Vector2(0, -190); // y center offset = -190

            var rankScrollRect = Comp<ScrollRect>(scrollRectGo);
            rankScrollRect.horizontal = false;
            rankScrollRect.vertical = true;
            rankScrollRect.movementType = ScrollRect.MovementType.Clamped;

            var rankViewportGo = Child(scrollRectGo, "Viewport");
            Stretch(rankViewportGo);
            Comp<RectMask2D>(rankViewportGo);
            var rankVpImg = Img(rankViewportGo, new Color(0, 0, 0, 0));
            rankVpImg.raycastTarget = true;
            rankScrollRect.viewport = rankViewportGo.GetComponent<RectTransform>();

            var rankContentGo = Child(rankViewportGo, "Content");
            var rankContentRt = rankContentGo.GetComponent<RectTransform>();
            rankContentRt.anchorMin = new Vector2(0, 1);
            rankContentRt.anchorMax = new Vector2(1, 1);
            rankContentRt.pivot = new Vector2(0.5f, 1);
            rankContentRt.sizeDelta = new Vector2(0, 0);
            rankScrollRect.content = rankContentRt;

            var vScroll = Comp<VirtualizedScrollRect>(scrollRectGo);

            // Assign the prefab asset dynamically
            var prefabPath = $"{BaseCommonPath}/RankingItemPrefab.prefab";
            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var soVScroll = new SerializedObject(vScroll);
            if (itemPrefab != null)
            {
                soVScroll.FindProperty("_itemPrefab").objectReferenceValue = itemPrefab.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogWarning($"[UIEditorSetup] RankingItemPrefab not found at {prefabPath}! Run 'Create All Prefabs' to generate it.");
            }
            soVScroll.FindProperty("_itemHeight").floatValue = 90f;
            soVScroll.FindProperty("_spacing").floatValue = 6f;
            soVScroll.ApplyModifiedProperties();

            // Pinned player ranking item at the bottom — instance of Final Prefab
            var rankItemFinalAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/RankingItemPrefab.prefab")
                                  ?? AssetDatabase.LoadAssetAtPath<GameObject>($"{BaseCommonPath}/RankingItemPrefab.prefab");
            
            var myRankPinTrans = rankingTab.transform.Find("MyRankPin");
            GameObject myRankPin = myRankPinTrans != null ? myRankPinTrans.gameObject : null;
            RankingItemView myRankPinView;

            if (myRankPin == null)
            {
                if (rankItemFinalAsset != null)
                {
                    myRankPin = (GameObject)PrefabUtility.InstantiatePrefab(rankItemFinalAsset, rankingTab.transform);
                    myRankPin.name = "MyRankPin";
                }
                else
                {
                    myRankPin = Child(rankingTab, "MyRankPin");
                    Fixed(myRankPin, new Vector2(0, -700), new Vector2(860, 90));
                    Img(myRankPin, UI_BG_MID);
                    var pinBorder = Child(myRankPin, "Border");
                    Stretch(pinBorder);
                    var pinBorderImg = Img(pinBorder, Hex("2B003B"));
                    pinBorder.transform.SetAsFirstSibling();
                    pinBorderImg.rectTransform.offsetMin = new Vector2(-4, -4);
                    pinBorderImg.rectTransform.offsetMax = new Vector2(4, 4);
                    BuildRankingItemHierarchy(myRankPin, resMap);
                }
            }

            if (myRankPin != null)
            {
                var pinRt = myRankPin.GetComponent<RectTransform>();
                if (pinRt != null)
                {
                    pinRt.anchorMin        = new Vector2(0.5f, 0.5f);
                    pinRt.anchorMax        = new Vector2(0.5f, 0.5f);
                    pinRt.pivot            = new Vector2(0.5f, 0.5f);
                    pinRt.anchoredPosition = new Vector2(0, -700);
                    pinRt.sizeDelta        = new Vector2(860, 90);
                }
                var pinBg = myRankPin.GetComponent<Image>();
                if (pinBg != null) pinBg.color = UI_PRIMARY;
                var pinShadow = Comp<Shadow>(myRankPin);
                pinShadow.effectColor    = new Color(0f, 0f, 0f, 0.6f);
                pinShadow.effectDistance = new Vector2(3f, -4f);
                myRankPin.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

                myRankPinView = myRankPin.GetComponent<RankingItemView>();
                if (myRankPinView == null) myRankPinView = Comp<RankingItemView>(myRankPin);
            }
            else
            {
                myRankPinView = null;
            }


            // ── AchievementTab (bottom-nav tab, right of Ranking) — migrated from AchievementListPopupView ──
            var achievementCell = BuildAchievementCell();
            var achievementTab = Child(tabContent, "AchievementTab"); Stretch(achievementTab); achievementTab.SetActive(false);
            var achView = Comp<AchievementTabView>(achievementTab);

            TMP(achievementTab, "TitleText", Center(0, 700, 760, 70), 30, UI_CTA, "Achievements", "popup.achievement.title", TextCategory.Header);

            var achTabRow = Child(achievementTab, "TabRow");
            var achTabRowRt = RT(achTabRow);
            achTabRowRt.anchorMin = new Vector2(0.5f, 1f); achTabRowRt.anchorMax = new Vector2(0.5f, 1f); achTabRowRt.pivot = new Vector2(0.5f, 1f);
            achTabRowRt.anchoredPosition = new Vector2(0, -80); achTabRowRt.sizeDelta = new Vector2(920, 80);
            var achTabHlg = Comp<HorizontalLayoutGroup>(achTabRow);
            achTabHlg.spacing = 8; achTabHlg.childAlignment = TextAnchor.MiddleCenter;
            achTabHlg.childControlWidth = true; achTabHlg.childControlHeight = true;
            achTabHlg.childForceExpandWidth = true; achTabHlg.childForceExpandHeight = true;
            var prgTab = BtnHlg(achTabRow, "ProgressionTab", UI_CTA, "Progress", "achievement.tab.progression");
            var sklTab = BtnHlg(achTabRow, "SkillTab", UI_BG_MID, "Skill", "achievement.tab.skill");
            var dedTab = BtnHlg(achTabRow, "DedicationTab", UI_BG_MID, "Dedication", "achievement.tab.dedication");
            var colTab = BtnHlg(achTabRow, "CollectionTab", UI_BG_MID, "Collection", "achievement.tab.collection");

            var achScroll = Child(achievementTab, "ScrollView");
            var achSrt = RT(achScroll);
            achSrt.anchorMin = new Vector2(0.5f, 0f); achSrt.anchorMax = new Vector2(0.5f, 1f); achSrt.pivot = new Vector2(0.5f, 0.5f);
            achSrt.sizeDelta = new Vector2(900, -260); achSrt.anchoredPosition = new Vector2(0, -100);
            var achSr = Comp<ScrollRect>(achScroll); achSr.horizontal = false; achSr.vertical = true;
            var achVp = Child(achScroll, "Viewport"); Stretch(achVp); Comp<RectMask2D>(achVp);
            var achVpImg = Img(achVp, new Color(0, 0, 0, 0)); achVpImg.raycastTarget = true;
            achSr.viewport = RT(achVp);
            var achContent = Child(achVp, "Content");
            var achCrt = RT(achContent); achCrt.anchorMin = new Vector2(0, 1); achCrt.anchorMax = new Vector2(1, 1); achCrt.pivot = new Vector2(0.5f, 1); achCrt.sizeDelta = new Vector2(0, 0);
            achSr.content = achCrt;
            var achVlg = Comp<VerticalLayoutGroup>(achContent); achVlg.spacing = 10; achVlg.padding = new RectOffset(20, 20, 20, 20);
            achVlg.childAlignment = TextAnchor.UpperCenter; achVlg.childControlWidth = true; achVlg.childControlHeight = true;
            achVlg.childForceExpandWidth = false; achVlg.childForceExpandHeight = false;
            var achCsf = Comp<ContentSizeFitter>(achContent); achCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var soAch = new SerializedObject(achView);
            soAch.FindProperty("_progressionTab").objectReferenceValue = prgTab.GetComponent<Button>();
            soAch.FindProperty("_skillTab").objectReferenceValue = sklTab.GetComponent<Button>();
            soAch.FindProperty("_dedicationTab").objectReferenceValue = dedTab.GetComponent<Button>();
            soAch.FindProperty("_collectionTab").objectReferenceValue = colTab.GetComponent<Button>();
            soAch.FindProperty("_listContainer").objectReferenceValue = achCrt;
            soAch.FindProperty("_cellPrefab").objectReferenceValue = achievementCell;
            soAch.FindProperty("_activeTabColor").colorValue = UI_CTA;
            soAch.FindProperty("_inactiveTabColor").colorValue = UI_BG_MID;
            soAch.ApplyModifiedProperties();

            // Wire LobbyView refs
            var soLobby = new SerializedObject(canvas.GetComponent<LobbyView>());
            soLobby.FindProperty("_header").objectReferenceValue      = hv;
            soLobby.FindProperty("_navBar").objectReferenceValue      = bnv;
            soLobby.FindProperty("_homeTabRoot").objectReferenceValue = homeTab;
            soLobby.FindProperty("_shopTabRoot").objectReferenceValue = shopTab;
            soLobby.FindProperty("_rankingTabRoot").objectReferenceValue = rankingTab;
            soLobby.FindProperty("_achievementTabRoot").objectReferenceValue = achievementTab;
            soLobby.FindProperty("_rankingTabView").objectReferenceValue = rankingView;
            soLobby.ApplyModifiedProperties();

            var soRanking = new SerializedObject(rankingView);
            soRanking.FindProperty("_stageTabButton").objectReferenceValue = stageTab.GetComponent<Button>();
            soRanking.FindProperty("_perfectTabButton").objectReferenceValue = perfectTab.GetComponent<Button>();
            soRanking.FindProperty("_weeklyTabButton").objectReferenceValue = weeklyTab.GetComponent<Button>();
            soRanking.FindProperty("_titleText").objectReferenceValue = rankTitle;
            soRanking.FindProperty("_descText").objectReferenceValue = rankDesc;
            soRanking.FindProperty("_myRankText").objectReferenceValue = myRank;
            soRanking.FindProperty("_entriesText").objectReferenceValue = entries;
            soRanking.FindProperty("_virtualizedScrollRect").objectReferenceValue = vScroll;

            // Wire MyRankPin elements
            soRanking.FindProperty("_myRankPin").objectReferenceValue = myRankPinView;

            // Populate avatarSprites list in RankingTabView
            var avatarSpritesProp = soRanking.FindProperty("_avatarSprites");
            avatarSpritesProp.ClearArray();
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            var avatarCsvPath = Path.Combine(repoRoot, "shared/datas/avatar/avatar.csv");
            if (File.Exists(avatarCsvPath))
            {
                var lines = File.ReadAllLines(avatarCsvPath);
                int count = 0;
                for (int i = 4; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    var cols = line.Split(',');
                    if (cols.Length >= 2)
                    {
                        if (int.TryParse(cols[0].Trim(), out int avatarId))
                        {
                            string resKey = cols[1].Trim();
                            if (resMap.TryGetValue(resKey, out string spritePath))
                            {
                                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                                if (sprite != null)
                                {
                                    avatarSpritesProp.InsertArrayElementAtIndex(count);
                                    var element = avatarSpritesProp.GetArrayElementAtIndex(count);
                                    element.FindPropertyRelative("avatarId").intValue = avatarId;
                                    element.FindPropertyRelative("resourceName").stringValue = resKey;
                                    element.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            // Populate the Stage score icon on RankingTabView (star rating removed)
            string stageKey = soRanking.FindProperty("_stageResourceKey").stringValue;
            if (string.IsNullOrEmpty(stageKey)) stageKey = "nav_home";

            if (resMap.TryGetValue(stageKey, out string stagePath))
            {
                var stageSprite = AssetDatabase.LoadAssetAtPath<Sprite>(stagePath);
                soRanking.FindProperty("_stageSprite").objectReferenceValue = stageSprite;
            }

            soRanking.FindProperty("_activeTabColor").colorValue = UI_PRIMARY;
            soRanking.FindProperty("_inactiveTabColor").colorValue = UI_BG_MID;
            soRanking.ApplyModifiedProperties();

            // Wire BottomNavBarView
            var soNav = new SerializedObject(bnv);
            soNav.FindProperty("_shopButton").objectReferenceValue       = shopBtn.GetComponent<Button>();
            soNav.FindProperty("_homeButton").objectReferenceValue       = homeBtn.GetComponent<Button>();
            soNav.FindProperty("_rankingButton").objectReferenceValue    = rankBtn.GetComponent<Button>();
            soNav.FindProperty("_achievementButton").objectReferenceValue = achBtn.GetComponent<Button>();
            soNav.FindProperty("_shopHighlight").objectReferenceValue    = shopBtn.transform.Find("Visual/Icon").GetComponent<Image>();
            soNav.FindProperty("_homeHighlight").objectReferenceValue    = homeBtn.transform.Find("Visual/Icon").GetComponent<Image>();
            soNav.FindProperty("_rankingHighlight").objectReferenceValue = rankBtn.transform.Find("Visual/Icon").GetComponent<Image>();
            soNav.FindProperty("_achievementHighlight").objectReferenceValue = achBtn.transform.Find("Visual/Icon").GetComponent<Image>();
            soNav.ApplyModifiedProperties();

            // Wire HeaderView
            var soHeader = new SerializedObject(hv);
            soHeader.FindProperty("_avatarButton").objectReferenceValue     = avatarBtn.GetComponent<Button>();
            soHeader.FindProperty("_settingsButton").objectReferenceValue   = settingsBtn.GetComponent<Button>();
            soHeader.FindProperty("_goldText").objectReferenceValue          = goldText;
            soHeader.ApplyModifiedProperties();

            // Header must render on top of TabContent (later sibling = higher z-order in Canvas)
            header.transform.SetAsLastSibling();

            SaveScenePrefab(canvas, "Lobby", _lobbyLoaded);
        }

        // Signal Sort scene canvas: authored chrome (HUD / Signal Panel container / Lanes container /
        // booster bar / flight layer) + BoardView binder. Lanes/chips/panel nodes are instantiated at
        // runtime from the Game prefabs; stuck/clear popups route through UIManager.
        static void SetupInGame()
        {
            // Reuse the existing Base prefab + children (Child()/Comp<> are Find-or-create) so the Final
            // variant's inspector overrides (e.g. BoardView.Skin) survive regen. NEVER delete the Final
            // variant or wholesale-destroy children here — that churns fileIDs and resets overrides.
            var (canvas, _inGameLoaded) = LoadOrCreateCanvas("InGame");

            Comp<UIScreenShake>(canvas);
            var board = Comp<BoardView>(canvas);

            // SafeAreaRoot — fills Screen.safeArea
            var safeRoot = Child(canvas, "SafeAreaRoot"); Stretch(safeRoot); Comp<SafeAreaHandler>(safeRoot);

            // Background — procedural circuit applied at runtime when sprite is unset (belongs to canvas directly so it reaches physical screen edges)
            var bg = Child(canvas, "Background"); Stretch(bg);
            var bgImg = Img(bg, UI_BG_DEEP); bgImg.raycastTarget = false;
            bg.transform.SetAsFirstSibling();
            bg.SetActive(false);

            // HUD — top strip: stage label + gimmick subtitle + move counter, plus dev controls (child of safeRoot)
            var hud = Child(safeRoot, "HUD"); TopStrip(hud, 240);
            Img(hud, UI_BG_MID);

            // Positioning relative to center of the 240px HUD (vertical center is Y = -120 local)
            var stageText = TMP(hud, "StageText", Center(-200, 40, 600, 90), 40, UI_TEXT, "Stage", null, TextCategory.Header);
            stageText.alignment = TextAlignmentOptions.Left;
            Comp<LocalizedText>(stageText.gameObject);
            Comp<UITextStyle>(stageText.gameObject).ApplyStyle();

            // Live move readouts (design §6/§8): current Moves + personal Best Moves. Values are set at
            // runtime by BoardView.UpdateHud (stringId null → LocalizedText leaves the number untouched).
            TMP(hud, "MovesCaption", Center(120, 30, 200, 46), 22, Hex("A6B0C9"), "MOVES", IngameMovesLabel, TextCategory.Normal);
            var movesText = TMP(hud, "MovesText", Center(120, -32, 200, 70), 40, UI_TEXT, "0", null, TextCategory.Header);
            Comp<UITextStyle>(movesText.gameObject).ApplyStyle();

            TMP(hud, "BestCaption", Center(310, 30, 180, 46), 22, Hex("A6B0C9"), "BEST", IngameBestLabel, TextCategory.Normal);
            var bestText = TMP(hud, "BestText", Center(310, -32, 180, 70), 40, UI_TEXT, "-", null, TextCategory.Header);
            Comp<UITextStyle>(bestText.gameObject).ApplyStyle();

            // Resources mapping
            var resMap = LoadDynamicResourceMap();

            // Pause button at top right (width 96x96, square design)
            var pauseBtn = Btn(hud, "PauseButton", new Vector2(470, 0), new Vector2(96, 96), UI_BG_DEEP, "");
            var pauseIconImg = pauseBtn.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (pauseIconImg != null && resMap.TryGetValue("ui_pause_icon", out string pausePath))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(pausePath);
                if (spr != null) { pauseIconImg.sprite = spr; pauseIconImg.preserveAspect = true; }
            }

            // Dim overlay — soft-stuck nudge (alpha animated by BoardView) (child of safeRoot, padded to screen play area)
            var dim = Child(safeRoot, "Dim"); PaddedStretch(dim, 240, 200);
            var dimColor = DIM; dimColor.a = 0f;
            var dimImg = Img(dim, dimColor); dimImg.raycastTarget = false;

            // Booster bar — Undo / Shuffle / Add Lane (spec 3 boosters) (child of safeRoot)
            var bar = Child(safeRoot, "BoosterBar"); BottomStrip(bar, 200);
            Img(bar, UI_BG_MID);

            var undoBtn    = Btn(bar, "UndoButton",    new Vector2(-320, 0), new Vector2(150, 150), UI_BG_DEEP,  "");
            var shuffleBtn = Btn(bar, "ShuffleButton", new Vector2(0, 0),    new Vector2(150, 150), UI_PRIMARY,  "");
            var addLaneBtn = Btn(bar, "AddLaneButton", new Vector2(320, 0),  new Vector2(150, 150), UI_SUCCESS,  "");

            var undoVisual = undoBtn.transform.Find("Visual").gameObject;
            var undoLabelGo = Child(undoVisual, "Label");
            {
                var rt = RT(undoLabelGo);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(5, 15); rt.offsetMax = new Vector2(-5, -105);
            }
            var undoLabel = Comp<TextMeshProUGUI>(undoLabelGo);
            ApplyAutoFontSize(undoLabel, TextCategory.Normal);
            undoLabel.color = UI_TEXT;
            undoLabel.text = "× 0";
            undoLabel.alignment = TextAlignmentOptions.Center;
            Comp<LocalizedText>(undoLabelGo);
            Comp<UITextStyle>(undoLabelGo).ApplyStyle();

            var shuffleVisual = shuffleBtn.transform.Find("Visual").gameObject;
            var shuffleLabelGo = Child(shuffleVisual, "Label");
            {
                var rt = RT(shuffleLabelGo);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(5, 15); rt.offsetMax = new Vector2(-5, -105);
            }
            var shuffleLabel = Comp<TextMeshProUGUI>(shuffleLabelGo);
            ApplyAutoFontSize(shuffleLabel, TextCategory.Normal);
            shuffleLabel.color = UI_TEXT;
            shuffleLabel.text = "× 0";
            shuffleLabel.alignment = TextAlignmentOptions.Center;
            Comp<LocalizedText>(shuffleLabelGo);
            Comp<UITextStyle>(shuffleLabelGo).ApplyStyle();

            var addLaneVisual = addLaneBtn.transform.Find("Visual").gameObject;
            var addLaneLabelGo = Child(addLaneVisual, "Label");
            {
                var rt = RT(addLaneLabelGo);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(5, 15); rt.offsetMax = new Vector2(-5, -105);
            }
            var addLaneLabel = Comp<TextMeshProUGUI>(addLaneLabelGo);
            ApplyAutoFontSize(addLaneLabel, TextCategory.Normal);
            addLaneLabel.color = UI_TEXT;
            addLaneLabel.text = "× 0";
            addLaneLabel.alignment = TextAlignmentOptions.Center;
            Comp<LocalizedText>(addLaneLabelGo);
            Comp<UITextStyle>(addLaneLabelGo).ApplyStyle();

            var undoIconImg = undoBtn.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (undoIconImg != null)
            {
                var rt = undoIconImg.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(15, 25); rt.offsetMax = new Vector2(-15, -5);
                if (resMap.TryGetValue("item_undo", out string undoPath))
                {
                    var spr = AssetDatabase.LoadAssetAtPath<Sprite>(undoPath);
                    if (spr != null) { undoIconImg.sprite = spr; undoIconImg.preserveAspect = true; }
                }
            }

            var shuffleIconImg = shuffleBtn.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (shuffleIconImg != null)
            {
                var rt = shuffleIconImg.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(15, 25); rt.offsetMax = new Vector2(-15, -5);
                if (resMap.TryGetValue("item_shuffle", out string shufflePath))
                {
                    var spr = AssetDatabase.LoadAssetAtPath<Sprite>(shufflePath);
                    if (spr != null) { shuffleIconImg.sprite = spr; shuffleIconImg.preserveAspect = true; }
                }
            }

            var addLaneIconImg = addLaneBtn.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (addLaneIconImg != null)
            {
                var rt = addLaneIconImg.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(15, 25); rt.offsetMax = new Vector2(-15, -5);
                if (resMap.TryGetValue("item_add_lane", out string addLanePath))
                {
                    var spr = AssetDatabase.LoadAssetAtPath<Sprite>(addLanePath);
                    if (spr != null) { addLaneIconImg.sprite = spr; addLaneIconImg.preserveAspect = true; }
                }
            }

            // BoardWorldResizer component addition and wiring (uses dim for screen constraints)
            var resizer = Comp<BoardWorldResizer>(canvas);
            var soResizer = new SerializedObject(resizer);
            soResizer.FindProperty("_gameArea").objectReferenceValue = RT(dim);
            soResizer.FindProperty("_boardView").objectReferenceValue = board;
            soResizer.ApplyModifiedProperties();

            // Runtime gameplay prefabs (created by their own builders before this scene)
            var chipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsGamePath}/ChipView.prefab");
            var lanePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsGamePath}/LaneView.prefab");
            var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsGamePath}/SignalNodeView.prefab");

            var so = new SerializedObject(board);
            so.FindProperty("_background").objectReferenceValue     = bgImg;
            so.FindProperty("_lanesContainer").objectReferenceValue = null; // Resolved dynamically at runtime
            so.FindProperty("_panelContainer").objectReferenceValue = null; // Resolved dynamically at runtime
            so.FindProperty("_flightLayer").objectReferenceValue    = null; // Resolved dynamically at runtime
            so.FindProperty("_dim").objectReferenceValue            = dimImg;
            so.FindProperty("_stageText").objectReferenceValue      = stageText;
            so.FindProperty("_undoBtn").objectReferenceValue        = undoBtn.GetComponent<Button>();
            so.FindProperty("_shuffleBtn").objectReferenceValue     = shuffleBtn.GetComponent<Button>();
            so.FindProperty("_addLaneBtn").objectReferenceValue     = addLaneBtn.GetComponent<Button>();
            so.FindProperty("_addLaneLabel").objectReferenceValue   = addLaneLabel;
            so.FindProperty("_pauseBtn").objectReferenceValue       = pauseBtn.GetComponent<Button>();
            so.FindProperty("_lanePrefab").objectReferenceValue     = lanePrefab;
            so.FindProperty("_chipPrefab").objectReferenceValue     = chipPrefab;
            so.FindProperty("_nodePrefab").objectReferenceValue     = nodePrefab;
            so.ApplyModifiedProperties();

            SaveScenePrefab(canvas, "InGame", _inGameLoaded);

            // Configure scene camera & BoardWorldRoot in the active scene
            string scenePath = "Assets/Scenes/InGame.unity";
            if (File.Exists(scenePath))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                
                // Configure Main Camera
                var camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera", typeof(Camera));
                camGo.name = "Main Camera";
                var cam = camGo.GetComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.07f, 0.11f, 1f); // Sleek dark neon
                camGo.transform.position = new Vector3(0f, 0f, -10f);

                // Configure BoardWorldRoot
                var worldRoot = GameObject.Find("BoardWorldRoot") ?? new GameObject("BoardWorldRoot");
                worldRoot.name = "BoardWorldRoot";
                worldRoot.transform.position = Vector3.zero;

                var panelGo = worldRoot.transform.Find("SignalPanel")?.gameObject;
                if (panelGo == null)
                {
                    panelGo = new GameObject("SignalPanel");
                    panelGo.transform.SetParent(worldRoot.transform, false);
                }
                var lanesGo = worldRoot.transform.Find("LanesContainer")?.gameObject;
                if (lanesGo == null)
                {
                    lanesGo = new GameObject("LanesContainer");
                    lanesGo.transform.SetParent(worldRoot.transform, false);
                }
                var flightGo = worldRoot.transform.Find("FlightLayer")?.gameObject;
                if (flightGo == null)
                {
                    flightGo = new GameObject("FlightLayer");
                    flightGo.transform.SetParent(worldRoot.transform, false);
                }

                var canvasGo = GameObject.Find("InGameCanvas_Base");
                if (canvasGo != null && PrefabUtility.IsPartOfAnyPrefab(canvasGo))
                {
                    // NEVER RevertPrefabInstance here — it wipes every scene-instance override
                    // (e.g. BoardView.Skin assigned in InGame.unity). Prefab/variant inheritance
                    // already propagates Base structure changes to the instance automatically.
                    var controllerGo = GameObject.Find("InGameController");
                    if (controllerGo != null)
                    {
                        var controller = controllerGo.GetComponent<Game.InGame.Controller.InGameController>();
                        if (controller != null)
                        {
                            var boardViewComponent = canvasGo.GetComponent<BoardView>();
                            if (boardViewComponent != null)
                            {
                                var soController = new SerializedObject(controller);
                                soController.FindProperty("_boardView").objectReferenceValue = boardViewComponent;
                                soController.ApplyModifiedProperties();
                                Debug.Log("[UIEditorSetup] Wired InGameController._boardView reference to InGameCanvas_Base BoardView.");
                            }
                        }
                    }

                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                    Debug.Log("[UIEditorSetup] Configured Main Camera & BoardWorldRoot in InGame scene (InGameCanvas_Base instance overrides preserved).");
                }
            }
        }

        // Anchored stretch within parent by normalized fractions (no offset).
        static void AnchorRect(GameObject go, float xMin, float yMin, float xMax, float yMax)
        {
            var rt = RT(go);
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ── Signal Sort gameplay prefabs (runtime-instantiated; thin GO + self-building view) ──

        static void CreateInGameChip()       => SaveGamePrefab<ChipView>("ChipView");
        static void CreateInGameLane()        => SaveGamePrefab<LaneView>("LaneView");
        static void CreateInGameSignalNode()  => SaveGamePrefab<SignalNodeView>("SignalNodeView");

        static void SaveGamePrefab<T>(string prefabName) where T : Component
        {
            MkDir(PrefabsGamePath);
            string path = $"{PrefabsGamePath}/{prefabName}.prefab";
            var go = new GameObject(prefabName);
            go.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIEditorSetup] Saved Game Prefab → {path}");
        }

        // ════════════════════════════════════════════════════════════════
        //  PREFAB BUILDERS
        // ════════════════════════════════════════════════════════════════

        static void CreateConfirmDialog()
        {
            var root = FullScreen("ConfirmDialogView");
            Comp<ConfirmDialogView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), DIM, "", shadowAlpha: 200f / 255f);
            Stretch(backdrop);

            // Increased panel height to 680 to accommodate the structured rewards panel
            var panel = Panel(root, "Panel", new Vector2(900, 680), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Confirm", CommonBtnConfirm);
            
            // BodyText shifted further up to Y=170, and set body text styling
            var body  = TMP(panel, "BodyText", Center(0, 170, 800, 80), 20, UI_TEXT, "Are you sure?", null, TextCategory.Normal);
            body.enableWordWrapping = true;
            body.alignment = TextAlignmentOptions.Center;
            var bodyCsf = Comp<ContentSizeFitter>(body.gameObject);
            bodyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // structured reward panel
            var rewardPanel = Child(panel, "RewardPanel");
            Fixed(rewardPanel, new Vector2(0, -40), new Vector2(800, 220));
            var rpImg = Img(rewardPanel, Hex("24172E")); // Darker slate purple background
            
            // thin border for reward panel
            var rpBorder = Child(rewardPanel, "Border");
            Stretch(rpBorder);
            var rpbImg = Img(rpBorder, Hex("FF9F00")); // Orange-yellow accent highlights (UI_BORDER)
            rpBorder.transform.SetAsFirstSibling();
            rpbImg.rectTransform.offsetMin = new Vector2(-2, -2);
            rpbImg.rectTransform.offsetMax = new Vector2(2, 2);

            // CONTENTS ribbon banner
            var rpRibbon = Child(rewardPanel, "ContentsRibbon");
            Fixed(rpRibbon, new Vector2(0, 110), new Vector2(200, 36));
            var rprImg = Img(rpRibbon, UI_CTA); // Amber yellow background
            var rprBorder = Child(rpRibbon, "Border");
            Stretch(rprBorder);
            var rprBImg = Img(rprBorder, UI_BORDER);
            rprBorder.transform.SetAsFirstSibling();
            rprBImg.rectTransform.offsetMin = new Vector2(-2, -2);
            rprBImg.rectTransform.offsetMax = new Vector2(2, 2);

            var rprTxt = TMP(rpRibbon, "Text", Center(0, 0, 190, 30), 12, UI_BG_DEEP, "CONTAINS", null, TextCategory.Header);
            rprTxt.alignment = TextAlignmentOptions.Center;

            // Dynamic Reward Cell Container inside the panel
            var rewardContainer = Child(rewardPanel, "RewardCellContainer");
            Fixed(rewardContainer, new Vector2(0, -15), new Vector2(760, 160));
            var hlg = Comp<HorizontalLayoutGroup>(rewardContainer);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 15;
            hlg.childControlWidth = hlg.childControlHeight = false;

            var cancel = Btn(panel, "CancelButton",  new Vector2(-200, -250), new Vector2(320, 90), UI_BG_DEEP, "Cancel",  CommonBtnCancel);
            var confirm= Btn(panel, "ConfirmButton", new Vector2( 200, -250), new Vector2(320, 90), UI_CTA,     "Confirm", CommonBtnConfirm);
            var closeBtn = CloseBtnAt(panel, new Vector2(395, 295));

            var rewardItemCellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BaseCommonPath}/RewardItemCell.prefab");

            var so = new SerializedObject(root.GetComponent<ConfirmDialogView>());
            so.FindProperty("_titleText").objectReferenceValue         = title;
            so.FindProperty("_bodyText").objectReferenceValue          = body;
            so.FindProperty("_cancelLabel").objectReferenceValue       = cancel.transform.Find("Visual/Label").GetComponent<TMP_Text>();
            so.FindProperty("_confirmLabel").objectReferenceValue      = confirm.transform.Find("Visual/Label").GetComponent<TMP_Text>();
            so.FindProperty("_cancelButton").objectReferenceValue      = cancel.GetComponent<Button>();
            so.FindProperty("_confirmButton").objectReferenceValue     = confirm.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue    = backdrop.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue       = closeBtn;
            so.FindProperty("_confirmButtonImage").objectReferenceValue = confirm.transform.Find("Visual").GetComponent<Image>();
            so.FindProperty("_rewardCellContainer").objectReferenceValue = RT(rewardContainer);
            so.FindProperty("_rewardItemCellPrefab").objectReferenceValue = rewardItemCellPrefab;
            so.FindProperty("_rewardPanel").objectReferenceValue       = rewardPanel;
            so.ApplyModifiedProperties();

            Save(root, "ConfirmDialogView");
        }



        static void CreateToast()
        {
            string _toastPath = $"{BaseCommonPath}/ToastView.prefab";
            _prefabWasLoaded = AssetDatabase.LoadAssetAtPath<GameObject>(_toastPath) != null;
            var root = _prefabWasLoaded ? PrefabUtility.LoadPrefabContents(_toastPath) : new GameObject("ToastView");
            if (!_prefabWasLoaded) Comp<RectTransform>(root);
            TopStrip(root, 120);
            var rt = root.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -260); // sit below HUD/header
            
            // Border background for toast
            var border = Child(root, "Border");
            Stretch(border);
            var borderImg = Img(border, UI_BORDER);
            border.transform.SetAsFirstSibling();
            borderImg.rectTransform.offsetMin = new Vector2(-4, -4);
            borderImg.rectTransform.offsetMax = new Vector2(4, 4);
            
            Img(root, UI_BG_MID); Comp<ToastView>(root); Comp<CanvasGroup>(root);
            var msgTxt = TMP(root, "MessageText", Center(0, 0, 900, 80), 18, UI_TEXT, "Notification", null, TextCategory.Normal);
            msgTxt.overflowMode = TextOverflowModes.Ellipsis;
            msgTxt.enableWordWrapping = false;

            Save(root, "ToastView");
        }

        static void CreateLoadingOverlay()
        {
            var root = FullScreen("LoadingOverlayView");
            Img(root, DIM);
            Comp<LoadingOverlayView>(root);
            
            var spinner = Child(root, "Spinner"); Fixed(spinner, Vector2.zero, new Vector2(120, 120));
            Img(spinner, UI_CTA);
            var loaderAnim = Comp<UIIconIdleAnimator>(spinner);
            loaderAnim.Configure(UIIconIdleAnimator.AnimationType.Rotate, 3f, 45f);

            var msgTxt = TMP(root, "MessageText", Center(0, -140, 600, 60), 20, UI_TEXT, "Loading...", null, TextCategory.Normal);

            var so = new SerializedObject(root.GetComponent<LoadingOverlayView>());
            so.FindProperty("_messageText").objectReferenceValue = msgTxt;
            so.ApplyModifiedProperties();

            Save(root, "LoadingOverlayView");
        }

        static void CreateNetworkError()
        {
            var root = FullScreen("NetworkErrorView");
            Img(root, DIM); Comp<NetworkErrorView>(root);

            var panel = Panel(root, "Panel", new Vector2(800, 420), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Network Error", PopupNetworkErrorTitle);
            var msg   = TMP(panel, "MessageText", Center(0, -10, 680, 150), 20, UI_TEXT, "Check your network connection.", ErrorNetworkCheck, TextCategory.Normal);
            var retry = Btn(panel, "RetryButton", new Vector2(0, -140), new Vector2(320, 90), UI_CTA, "Retry", CommonBtnRetry);

            var so = new SerializedObject(root.GetComponent<NetworkErrorView>());
            so.FindProperty("_messageText").objectReferenceValue = msg;
            so.FindProperty("_retryButton").objectReferenceValue = retry.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "NetworkErrorView");
        }

        static void CreateForceUpdateView()
        {
            var root = FullScreen("ForceUpdateView");
            Img(root, UI_BG_DEEP); Comp<ForceUpdateView>(root);

            var panel = Panel(root, "Panel", new Vector2(900, 480), UI_BG_MID);
            RibbonTitle(panel, "TitleText", "Update Required", BootForceUpdateTitle);
            var msg = TMP(panel, "MessageText", Center(0, 30, 760, 130), 20, UI_TEXT, "A new version is available.", BootForceUpdateBody, TextCategory.Normal);
            msg.enableWordWrapping = true;
            msg.alignment = TextAlignmentOptions.Center;
            var btn = Btn(panel, "UpdateButton", new Vector2(0, -160), new Vector2(500, 90), UI_SUCCESS, "Update", BootForceUpdateBtn);

            var so = new SerializedObject(root.GetComponent<ForceUpdateView>());
            so.FindProperty("_updateButton").objectReferenceValue = btn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "ForceUpdateView");
        }

        static void CreateRewardPopup()
        {
            // Build RewardItemCell prefab first (background + icon + quantity badge)
            string cellPrefabPath = $"{BaseCommonPath}/RewardItemCell.prefab";
            bool _cellLoaded = AssetDatabase.LoadAssetAtPath<GameObject>(cellPrefabPath) != null;
            var cellGo = _cellLoaded ? PrefabUtility.LoadPrefabContents(cellPrefabPath) : new GameObject("RewardItemCell");
            RT(cellGo).sizeDelta = new Vector2(160, 160);
            Img(cellGo, UI_BG_DEEP);

            var cellIcon = Child(cellGo, "Icon");
            Fixed(cellIcon, new Vector2(0, 10), new Vector2(100, 100));
            var cellIconImg = Img(cellIcon, Color.white);
            cellIconImg.preserveAspect = true;

            var qtyGo = Child(cellGo, "Quantity");
            var qtyRt = RT(qtyGo);
            qtyRt.anchorMin = new Vector2(1, 0);
            qtyRt.anchorMax = new Vector2(1, 0);
            qtyRt.pivot     = new Vector2(1, 0);
            qtyRt.anchoredPosition = new Vector2(-6, 8);
            qtyRt.sizeDelta        = new Vector2(80, 36);
            var qtyTmp = Comp<TextMeshProUGUI>(qtyGo);
            ApplyAutoFontSize(qtyTmp, TextCategory.Normal);
            qtyTmp.color             = UI_CTA;
            qtyTmp.alignment         = TextAlignmentOptions.BottomRight;
            qtyTmp.text              = "× 1";
            qtyTmp.enableWordWrapping = false;
            Comp<LocalizedText>(qtyGo);
            Comp<UITextStyle>(qtyGo).ApplyStyle();
            Comp<RewardItemCellView>(cellGo);

            PrefabUtility.SaveAsPrefabAsset(cellGo, cellPrefabPath);
            if (_cellLoaded) PrefabUtility.UnloadPrefabContents(cellGo);
            else Object.DestroyImmediate(cellGo);
            CreateCommonVariantIfMissing("RewardItemCell");
            Debug.Log($"[UIEditorSetup] Saved Base Popup → {cellPrefabPath}");
            var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cellPrefabPath);

            // Build the popup
            var root = FullScreen("RewardPopupView");
            Img(root, DIM); Comp<RewardPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            // Backdrop: visual dim only — close is via explicit CloseButton below
            var backdrop = Child(root, "Backdrop");
            Stretch(backdrop);
            Img(backdrop, DIM);

            var panel = Panel(root, "Panel", new Vector2(720, 780), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Reward!", PopupRewardTitle);

            var items = Child(panel, "ItemContainer");
            Fixed(items, new Vector2(0, 20), new Vector2(400, 400));
            var glg = Comp<GridLayoutGroup>(items);
            glg.cellSize         = new Vector2(170, 170);
            glg.spacing          = new Vector2(20, 20);
            glg.padding          = new RectOffset(20, 20, 20, 20);
            glg.constraint       = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount  = 2;
            glg.childAlignment   = TextAnchor.UpperCenter;

            var ok = Btn(panel, "OkButton", new Vector2(0, -280), new Vector2(300, 90), UI_CTA, "OK", CommonBtnOk);
            var closeBtn = CloseBtnAt(panel, new Vector2(305, 355));

            var so = new SerializedObject(root.GetComponent<RewardPopupView>());
            so.FindProperty("_itemContainer").objectReferenceValue = items.transform;
            so.FindProperty("_itemRowPrefab").objectReferenceValue = cellPrefab;
            so.FindProperty("_okButton").objectReferenceValue      = ok.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue   = closeBtn;
            so.ApplyModifiedProperties();

            Save(root, "RewardPopupView");
        }

        // ── OutGame systems: Cosmetic / Attendance / Achievement / Daily-Challenge ──

        static GameObject BuildCosmeticCell()
        {
            string path = $"{BaseCommonPath}/CosmeticItemCell.prefab";
            bool loaded = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            var cell = loaded ? PrefabUtility.LoadPrefabContents(path) : new GameObject("CosmeticItemCell");
            RT(cell).sizeDelta = new Vector2(200, 230);
            var bg = Img(cell, UI_BG_DEEP);
            var btn = Comp<Button>(cell); btn.targetGraphic = bg;
            Comp<UIButtonAnimator>(cell);

            var preview = Child(cell, "Preview");
            Fixed(preview, new Vector2(0, 25), new Vector2(150, 130));
            var pImg = Img(preview, UI_BG_MID); pImg.preserveAspect = true;

            TMP(cell, "NameText", Center(0, -70, 190, 44), 18, UI_TEXT, "Name", null, TextCategory.Normal);
            TMP(cell, "StateText", Center(0, -105, 190, 40), 18, UI_CTA, "800", null, TextCategory.Normal);

            PrefabUtility.SaveAsPrefabAsset(cell, path);
            if (loaded) PrefabUtility.UnloadPrefabContents(cell); else Object.DestroyImmediate(cell);
            CreateCommonVariantIfMissing("CosmeticItemCell");
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIEditorSetup] Saved Base Popup → {path}");
            // Reference the Final variant so user customization on it is what gets instantiated at runtime.
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/CosmeticItemCell.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static void CreateCosmeticPreviewPopup()
        {
            var root = FullScreen("CosmeticPreviewPopupView");
            Img(root, DIM); Comp<CosmeticPreviewPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);
            var backdrop = Child(root, "Backdrop"); Stretch(backdrop); Img(backdrop, DIM);

            var panel = Panel(root, "Panel", new Vector2(720, 880), UI_BG_MID);
            var nameText = RibbonTitle(panel, "NameText", "Cosmetic", null);

            var preview = Child(panel, "Preview");
            Fixed(preview, new Vector2(0, 170), new Vector2(360, 300));
            var pImg = Img(preview, UI_BG_DEEP); pImg.preserveAspect = true;

            var desc = TMP(panel, "DescText", Center(0, -60, 600, 120), 22, UI_TEXT, "Description", null, TextCategory.Normal);
            var state = TMP(panel, "StateText", Center(0, -190, 600, 60), 24, UI_CTA, "800", null, TextCategory.Normal);

            var action = Btn(panel, "ActionButton", new Vector2(0, -330), new Vector2(420, 96), UI_CTA, "Buy & Apply", "shop.cosmetic.btn_buy_apply");
            var actionLabel = action.transform.Find("Visual/Label")?.GetComponent<TextMeshProUGUI>();
            var closeBtn = CloseBtnAt(panel, new Vector2(330, 405));

            var so = new SerializedObject(root.GetComponent<CosmeticPreviewPopupView>());
            so.FindProperty("_previewImage").objectReferenceValue = pImg;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_descText").objectReferenceValue = desc;
            so.FindProperty("_stateText").objectReferenceValue = state;
            so.FindProperty("_actionButton").objectReferenceValue = action.GetComponent<Button>();
            so.FindProperty("_actionLabel").objectReferenceValue = actionLabel;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();

            Save(root, "CosmeticPreviewPopupView");
        }

        static void CreateAttendancePopup()
        {
            var root = FullScreen("AttendancePopupView");
            Img(root, DIM); Comp<AttendancePopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);
            var backdrop = Child(root, "Backdrop"); Stretch(backdrop); Img(backdrop, DIM);

            var panel = Panel(root, "Panel", new Vector2(980, 720), UI_BG_MID);
            RibbonTitle(panel, "TitleText", "Daily Reward", "popup.attendance.title");

            var dayContainer = Child(panel, "DayContainer");
            Fixed(dayContainer, new Vector2(0, 60), new Vector2(920, 220));
            var hlg = Comp<HorizontalLayoutGroup>(dayContainer);
            hlg.spacing = 8; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            for (int i = 1; i <= 7; i++)
            {
                var card = Child(dayContainer, $"Day{i}");
                RT(card).sizeDelta = new Vector2(120, 200);
                var le = Comp<LayoutElement>(card); le.preferredWidth = 120; le.preferredHeight = 200;
                Img(card, i == 7 ? UI_CTA : UI_BG_DEEP);

                TMP(card, "DayLabel", Center(0, 70, 110, 40), 18, UI_TEXT, $"D{i}", null, TextCategory.Normal);

                var pulse = Child(card, "PulseRing"); Stretch(pulse);
                var pulseImg = Img(pulse, UI_PRIMARY); pulseImg.raycastTarget = false;
                Comp<UIScalePulse>(pulse);
                pulse.transform.SetAsFirstSibling();
                pulse.SetActive(false);

                var dim = Child(card, "Dim"); Stretch(dim);
                var dimImg = Img(dim, DIM); dimImg.raycastTarget = false;
                TMP(dim, "Check", Center(0, 0, 110, 110), 40, UI_SUCCESS, "✓", null, TextCategory.Header);
                dim.SetActive(false);
            }

            var todayReward = TMP(panel, "TodayRewardText", Center(0, -120, 700, 60), 24, UI_CTA, "Today: 100", null, TextCategory.Normal);

            var claim = Btn(panel, "ClaimButton", new Vector2(0, -250), new Vector2(420, 96), UI_CTA, "Claim", "popup.attendance.btn_claim");
            var claimLabel = claim.transform.Find("Visual/Label")?.GetComponent<TextMeshProUGUI>();
            var closeBtn = CloseBtnAt(panel, new Vector2(460, 325));

            var so = new SerializedObject(root.GetComponent<AttendancePopupView>());
            so.FindProperty("_dayContainer").objectReferenceValue = dayContainer.transform;
            so.FindProperty("_todayRewardText").objectReferenceValue = todayReward;
            so.FindProperty("_claimButton").objectReferenceValue = claim.GetComponent<Button>();
            so.FindProperty("_claimLabel").objectReferenceValue = claimLabel;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();

            Save(root, "AttendancePopupView");
        }

        static GameObject BuildAchievementCell()
        {
            string path = $"{BaseCommonPath}/AchievementItemCell.prefab";
            bool loaded = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            var cell = loaded ? PrefabUtility.LoadPrefabContents(path) : new GameObject("AchievementItemCell");
            RT(cell).sizeDelta = new Vector2(840, 150);
            Img(cell, UI_BG_DEEP);
            var le = Comp<LayoutElement>(cell); le.preferredHeight = 150; le.preferredWidth = 840;

            var badge = Child(cell, "TierBadge");
            Fixed(badge, new Vector2(-360, 30), new Vector2(80, 80));
            Img(badge, UI_PRIMARY);

            var nm = TMP(cell, "NameText", new Rect(40, 40, 480, 50), 22, UI_TEXT, "Name", null, TextCategory.Normal);
            nm.alignment = TextAlignmentOptions.Left;
            var desc = TMP(cell, "DescText", new Rect(40, -8, 480, 44), 18, UI_TEXT, "Desc", null, TextCategory.Normal);
            desc.alignment = TextAlignmentOptions.Left;

            var bar = Child(cell, "ProgressBar");
            Fixed(bar, new Vector2(-40, -52), new Vector2(420, 24));
            Img(bar, UI_BG_MID);
            // Sprite-free fill: width driven by anchorMax.x (no fillAmount/sprite). Left-anchored, starts empty.
            var fill = Child(bar, "Fill");
            var fillRt = RT(fill);
            fillRt.anchorMin = new Vector2(0, 0); fillRt.anchorMax = new Vector2(0, 1); fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            var fillImg = Img(fill, UI_SUCCESS);
            var barAnim = Comp<AnimatedProgressBar>(bar);
            var soBar = new SerializedObject(barAnim);
            soBar.FindProperty("_fill").objectReferenceValue = fillRt;
            soBar.FindProperty("_fillImage").objectReferenceValue = fillImg;
            soBar.ApplyModifiedProperties();

            TMP(cell, "ProgressText", new Rect(250, -52, 140, 36), 16, UI_TEXT, "0/10", null, TextCategory.Normal);

            Btn(cell, "ClaimButton", new Vector2(330, 0), new Vector2(150, 96), UI_CTA, "Claim", "achievement.btn_claim");
            var done = TMP(cell, "CompletedLabel", new Rect(330, 0, 150, 60), 18, UI_SUCCESS, "Done", "achievement.completed", TextCategory.Normal);
            done.gameObject.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(cell, path);
            if (loaded) PrefabUtility.UnloadPrefabContents(cell); else Object.DestroyImmediate(cell);
            CreateCommonVariantIfMissing("AchievementItemCell");
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIEditorSetup] Saved Base Popup → {path}");
            // Reference the Final variant so user customization on it is what gets instantiated at runtime.
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/AchievementItemCell.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // Populates an `_avatarSprites` SerializedProperty (List<{avatarId,resourceName,sprite}>) from avatar.csv.
        // Shared by the Shop avatar section and the AccountPopup avatar sprite lookup.
        static void PopulateAvatarSprites(SerializedProperty prop, Dictionary<string, string> resMap)
        {
            prop.ClearArray();
            var avatarCsvPath = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "../../..")), "shared/datas/avatar/avatar.csv");
            if (!File.Exists(avatarCsvPath)) return;

            var lines = File.ReadAllLines(avatarCsvPath);
            int count = 0;
            for (int i = 4; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                var cols = line.Split(',');
                if (cols.Length < 2 || !int.TryParse(cols[0].Trim(), out int avatarId)) continue;
                string resKey = cols[1].Trim();
                if (!resMap.TryGetValue(resKey, out string spritePath)) continue;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null) continue;

                prop.InsertArrayElementAtIndex(count);
                var el = prop.GetArrayElementAtIndex(count);
                el.FindPropertyRelative("avatarId").intValue = avatarId;
                el.FindPropertyRelative("resourceName").stringValue = resKey;
                el.FindPropertyRelative("sprite").objectReferenceValue = sprite;
                count++;
            }
        }

        static GameObject BuildAvatarCard()
        {
            string path = $"{BaseCommonPath}/AvatarCard.prefab";
            bool loaded = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            var cell = loaded ? PrefabUtility.LoadPrefabContents(path) : new GameObject("AvatarCard");
            RT(cell).sizeDelta = new Vector2(180, 210);
            var bg = Img(cell, UI_BG_DEEP);
            var btn = Comp<Button>(cell); btn.targetGraphic = bg;
            Comp<UIButtonAnimator>(cell);

            var visual = Child(cell, "Visual");
            Fixed(visual, new Vector2(0, 25), new Vector2(160, 150));

            var icon = Child(visual, "Icon");
            Fixed(icon, Vector2.zero, new Vector2(120, 120));
            var iconImg = Img(icon, UI_BG_MID); iconImg.preserveAspect = true;

            var highlight = Child(visual, "SelectedHighlight"); Stretch(highlight);
            var hlImg = Img(highlight, UI_BORDER);
            hlImg.rectTransform.offsetMin = new Vector2(-4, -4);
            hlImg.rectTransform.offsetMax = new Vector2(4, 4);
            highlight.SetActive(false);

            var lockOverlay = Child(visual, "LockOverlay"); Stretch(lockOverlay);
            Img(lockOverlay, new Color(0, 0, 0, 0.6f));
            var lockIcon = Child(lockOverlay, "LockIcon");
            Fixed(lockIcon, new Vector2(0, 10), new Vector2(40, 40));
            var lockIconImg = Img(lockIcon, Color.white); lockIconImg.preserveAspect = true;
            var resMap = LoadDynamicResourceMap();
            if (resMap.TryGetValue("ui_lock_icon", out string lockIconPath))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(lockIconPath);
                if (spr != null) lockIconImg.sprite = spr;
            }
            lockOverlay.SetActive(false);

            TMP(cell, "StateText", Center(0, -85, 170, 40), 18, UI_CTA, "0", null, TextCategory.Normal);

            PrefabUtility.SaveAsPrefabAsset(cell, path);
            if (loaded) PrefabUtility.UnloadPrefabContents(cell); else Object.DestroyImmediate(cell);
            CreateCommonVariantIfMissing("AvatarCard");
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIEditorSetup] Saved Base Popup → {path}");
            // Reference the Final variant so user customization on it is what gets instantiated at runtime.
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/AvatarCard.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static void CreateAvatarPreviewPopup()
        {
            var root = FullScreen("AvatarPreviewPopupView");
            Img(root, DIM); Comp<AvatarPreviewPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);
            var backdrop = Child(root, "Backdrop"); Stretch(backdrop); Img(backdrop, DIM);

            var panel = Panel(root, "Panel", new Vector2(720, 760), UI_BG_MID);
            RibbonTitle(panel, "TitleText", "Avatar", PopupAccountLabelSelectAvatar);

            var preview = Child(panel, "Preview");
            Fixed(preview, new Vector2(0, 140), new Vector2(320, 320));
            var pImg = Img(preview, UI_BG_DEEP); pImg.preserveAspect = true;

            var state = TMP(panel, "StateText", Center(0, -140, 600, 60), 24, UI_CTA, "800", null, TextCategory.Normal);

            var action = Btn(panel, "ActionButton", new Vector2(0, -280), new Vector2(420, 96), UI_CTA, "Buy & Equip", "shop.cosmetic.btn_buy_apply");
            var actionLabel = action.transform.Find("Visual/Label")?.GetComponent<TextMeshProUGUI>();
            var closeBtn = CloseBtnAt(panel, new Vector2(330, 345));

            var so = new SerializedObject(root.GetComponent<AvatarPreviewPopupView>());
            so.FindProperty("_previewImage").objectReferenceValue = pImg;
            so.FindProperty("_stateText").objectReferenceValue = state;
            so.FindProperty("_actionButton").objectReferenceValue = action.GetComponent<Button>();
            so.FindProperty("_actionLabel").objectReferenceValue = actionLabel;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();

            Save(root, "AvatarPreviewPopupView");
        }

        static void CreateAchievementToast()
        {
            var root = FullScreen("AchievementToastView");
            var rootImg = Img(root, new Color(0, 0, 0, 0)); rootImg.raycastTarget = false;
            Comp<AchievementToastView>(root);

            var banner = Child(root, "Banner");
            var brt = RT(banner);
            brt.anchorMin = new Vector2(0.5f, 1f); brt.anchorMax = new Vector2(0.5f, 1f); brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0, 220); brt.sizeDelta = new Vector2(820, 160);
            Img(banner, UI_BG_MID); PixelShadow(banner);

            var badge = Child(banner, "TierBadge");
            Fixed(badge, new Vector2(-330, 0), new Vector2(90, 90));
            var badgeImg = Img(badge, UI_PRIMARY);

            var nm = TMP(banner, "NameText", new Rect(40, 0, 600, 100), 24, UI_TEXT, "Achievement", null, TextCategory.Normal);
            nm.alignment = TextAlignmentOptions.Left;

            var so = new SerializedObject(root.GetComponent<AchievementToastView>());
            so.FindProperty("_banner").objectReferenceValue = brt;
            so.FindProperty("_tierBadge").objectReferenceValue = badgeImg;
            so.FindProperty("_nameText").objectReferenceValue = nm;
            so.ApplyModifiedProperties();

            Save(root, "AchievementToastView");
        }

        static void CreateDailyChallengePopup()
        {
            var root = FullScreen("DailyChallengePopupView");
            Img(root, DIM); Comp<DailyChallengePopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);
            var backdrop = Child(root, "Backdrop"); Stretch(backdrop); Img(backdrop, DIM);

            var panel = Panel(root, "Panel", new Vector2(820, 760), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Daily Challenge", "popup.challenge.title");

            var date = TMP(panel, "DateText", Center(0, 200, 700, 60), 24, UI_TEXT, "2026-01-01", null, TextCategory.Normal);
            var diff = TMP(panel, "DifficultyText", Center(0, 110, 700, 70), 30, UI_CTA, "Difficulty", null, TextCategory.Header);
            var part = TMP(panel, "ParticipantsText", Center(0, 30, 700, 50), 22, UI_TEXT, "Participants", null, TextCategory.Normal);
            var streak = TMP(panel, "StreakText", Center(0, -40, 700, 50), 22, UI_TEXT, "Streak", null, TextCategory.Normal);

            var start = Btn(panel, "StartButton", new Vector2(0, -160), new Vector2(480, 96), UI_CTA, "Start", "popup.challenge.btn_start");
            var ranking = Btn(panel, "RankingButton", new Vector2(0, -270), new Vector2(480, 90), UI_BG_DEEP, "Ranking", "popup.challenge.btn_ranking");
            var closeBtn = CloseBtnAt(panel, new Vector2(330, 345));

            var so = new SerializedObject(root.GetComponent<DailyChallengePopupView>());
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_dateText").objectReferenceValue = date;
            so.FindProperty("_difficultyText").objectReferenceValue = diff;
            so.FindProperty("_participantsText").objectReferenceValue = part;
            so.FindProperty("_streakText").objectReferenceValue = streak;
            so.FindProperty("_startButton").objectReferenceValue = start.GetComponent<Button>();
            so.FindProperty("_rankingButton").objectReferenceValue = ranking.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();

            Save(root, "DailyChallengePopupView");
        }

        static void CreateReLoginView()
        {
            var root = FullScreen("ReLoginView");
            Img(root, UI_BG_DEEP); Comp<ReLoginView>(root);

            var panel   = Panel(root, "Panel", new Vector2(900, 520), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Session Expired", PopupReloginTitle);
            
            var relogin = Btn(panel, "ReLoginButton",         new Vector2(0,  10), new Vector2(500, 90), UI_CTA,     "Re-login",          PopupReloginBtnRelogin);
            var guest   = Btn(panel, "ContinueAsGuestButton", new Vector2(0, -110), new Vector2(500, 80), UI_BG_DEEP, "Continue as Guest", PopupReloginBtnGuest);

            var so = new SerializedObject(root.GetComponent<ReLoginView>());
            so.FindProperty("_reLoginButton").objectReferenceValue         = relogin.GetComponent<Button>();
            so.FindProperty("_continueAsGuestButton").objectReferenceValue = guest.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "ReLoginView");
        }

        static void CreateStageInfoPopup()
        {
            var root = FullScreen("StageInfoPopupView");
            Img(root, DIM); Comp<StageInfoPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            // Backdrop: visual dim only — close is via explicit CloseButton below
            var backdrop = Child(root, "Backdrop");
            Stretch(backdrop);
            Img(backdrop, DIM);

            var panel = Panel(root, "Panel", new Vector2(700, 760), UI_BG_MID);
            var title = RibbonTitle(panel, "StageTitleText", "Stage 1", PopupStageInfoTitle);
            var ribbonImg = title.transform.parent.GetComponent<Image>();

            // Best-moves record (campaign ranking metric; "-" until cleared). Star rating was removed.
            var best  = TMP(panel, "BestRecordText", Center(0, 140, 600, 60), 20, UI_TEXT, "-", null, TextCategory.Normal);
            var bestCsf = Comp<ContentSizeFitter>(best.gameObject);
            bestCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Difficulty + signal-type count: text set at runtime (stringId null = font-only LocalizedText).
            var difficulty = TMP(panel, "DifficultyText", Center(0, 60, 600, 50), 18, UI_TEXT, "Normal", null, TextCategory.Normal);
            var types      = TMP(panel, "TypesText",      Center(0, 10, 600, 50), 18, UI_TEXT, "Signal Types: 4", null, TextCategory.Normal);

            // Gimmick badge row: icon-only, long-press → ItemTooltipView. Inactive badges auto-collapse via HLG.
            var gimmickRow = Child(panel, "GimmickRow");
            Fixed(gimmickRow, new Vector2(0, -95), new Vector2(600, 110));
            var hlg = Comp<HorizontalLayoutGroup>(gimmickRow);
            hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            // Badge order = chapter intro order: Locked(Ch2) → Blind(Ch3) → Relay(Ch4) → Overload(Ch5).
            var lockLaneBadge = MakeGimmickBadge(gimmickRow, "LockLaneBadge", "Assets/Sprites/UI/Icons/ui_locklane.png");
            var blindLaneBadge= MakeGimmickBadge(gimmickRow, "BlindLaneBadge","Assets/Sprites/UI/Icons/ui_blindlane.png");
            var relayBadge    = MakeGimmickBadge(gimmickRow, "RelayBadge",    "Assets/Sprites/UI/Icons/ui_relaynode.png");
            var overloadBadge = MakeGimmickBadge(gimmickRow, "OverloadBadge", "Assets/Sprites/UI/Icons/ui_overloadchip.png");

            var play = Btn(panel, "PlayButton", new Vector2(0, -285), new Vector2(400, 80), UI_CTA, "Play", CommonBtnPlay);

            // Explicit square close button — top-right of panel (convention: all popups must have one)
            var closeBtn = CloseBtnAt(panel, new Vector2(280, 310));

            var so = new SerializedObject(root.GetComponent<StageInfoPopupView>());
            so.FindProperty("_stageTitle").objectReferenceValue       = title;
            so.FindProperty("_bestRecord").objectReferenceValue       = best;
            so.FindProperty("_difficultyLabel").objectReferenceValue  = difficulty;
            so.FindProperty("_typesLabel").objectReferenceValue       = types;
            so.FindProperty("_ribbonImage").objectReferenceValue      = ribbonImg;
            so.FindProperty("_playButton").objectReferenceValue       = play.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue   = closeBtn;
            so.FindProperty("_overloadBadge").objectReferenceValue    = overloadBadge;
            so.FindProperty("_relayBadge").objectReferenceValue       = relayBadge;
            so.FindProperty("_lockLaneBadge").objectReferenceValue    = lockLaneBadge;
            so.FindProperty("_blindLaneBadge").objectReferenceValue   = blindLaneBadge;
            so.ApplyModifiedProperties();

            Save(root, "StageInfoPopupView");
        }

        // Icon-only gimmick badge with a LongPressTooltipTrigger (icon sprite is the tooltip image).
        static LongPressTooltipTrigger MakeGimmickBadge(GameObject parent, string name, string spritePath)
        {
            var go = Child(parent, name);
            var rt = RT(go);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(96, 96);

            var le = Comp<LayoutElement>(go);
            le.preferredWidth = 96; le.preferredHeight = 96;

            var img = Img(go, Color.white);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null) { img.sprite = sprite; img.preserveAspect = true; }

            var trig = Comp<LongPressTooltipTrigger>(go);
            var tso = new SerializedObject(trig);
            tso.FindProperty("_icon").objectReferenceValue = img;
            tso.ApplyModifiedProperties();
            return trig;
        }

        static void CreateResultOverlay()
        {
            var root = FullScreen("ResultOverlayView");
            Img(root, DIM); Comp<ResultOverlayView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(900, 900), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Stage Clear!", PopupResultTitle);

            // Clear summary — moves / best moves (runtime-formatted) + a new-best badge (hidden by
            // default, toggled on a personal best). Hidden entirely for the daily challenge.
            var statsBlock = Child(panel, "StatsBlock");
            Fixed(statsBlock, new Vector2(0, 250), new Vector2(700, 220));
            Img(statsBlock, Color.clear);
            var movesText = TMP(statsBlock, "MovesText", Center(0,  60, 640, 70), 30, UI_TEXT, "Moves: 0",      null, TextCategory.Normal);
            var bestText  = TMP(statsBlock, "BestText",  Center(0,  -8, 640, 70), 30, UI_TEXT, "Best Moves: 0", null, TextCategory.Normal);
            var newBestBadge = Child(statsBlock, "NewBestBadge");
            Fixed(newBestBadge, new Vector2(0, -80), new Vector2(340, 64));
            Img(newBestBadge, UI_CTA);
            TMP(newBestBadge, "Label", Center(0, 0, 320, 56), 26, UI_TEXT, "New Best", PopupResultNewBest, TextCategory.Button);
            // Left active in the prefab so it's positionable in the Final variant; runtime visibility
            // is owned by RenderStats (SetActive(isNewBest)).

            // Granted-reward cells — instantiated at runtime from the RewardItemCell prefab.
            var rewardContainer = Child(panel, "RewardCellContainer");
            Fixed(rewardContainer, new Vector2(0, 40), new Vector2(760, 170));
            var hlg = Comp<HorizontalLayoutGroup>(rewardContainer);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 15;
            hlg.childControlWidth = hlg.childControlHeight = false;

            var doubleReward = Btn(panel, "DoubleRewardButton", new Vector2(0, -170), new Vector2(440, 100), UI_PRIMARY, "Double Reward", PopupResultDoubleReward);
            var next = Btn(panel, "NextButton", new Vector2( 160, -320), new Vector2(280, 100), UI_CTA,     "Next", CommonBtnNext);
            var map  = Btn(panel, "MapButton",  new Vector2(-160, -320), new Vector2(280, 100), UI_BG_DEEP, "Map",  CommonBtnMap);

            var rewardCellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BaseCommonPath}/RewardItemCell.prefab");

            var so = new SerializedObject(root.GetComponent<ResultOverlayView>());
            so.FindProperty("_titleText").objectReferenceValue          = title;
            so.FindProperty("_statsBlock").objectReferenceValue         = statsBlock;
            so.FindProperty("_movesText").objectReferenceValue          = movesText;
            so.FindProperty("_bestText").objectReferenceValue           = bestText;
            so.FindProperty("_newBestBadge").objectReferenceValue       = newBestBadge;
            so.FindProperty("_rewardContainer").objectReferenceValue    = rewardContainer.transform;
            so.FindProperty("_rewardCellPrefab").objectReferenceValue   = rewardCellPrefab;
            so.FindProperty("_doubleRewardButton").objectReferenceValue = doubleReward.GetComponent<Button>();
            so.FindProperty("_nextButton").objectReferenceValue         = next.GetComponent<Button>();
            so.FindProperty("_mapButton").objectReferenceValue          = map.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "ResultOverlayView");
        }

        static void CreateFailOverlay()
        {
            var resMap = LoadDynamicResourceMap();
            var root = FullScreen("FailOverlayView");
            Img(root, DIM); Comp<FailOverlayView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(760, 820), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Just a bit more!", PopupResultFailed);

            // Add Lane (rewarded ad) — top priority; the row hides once Add Lane is used this stage.
            var addLaneRow = Child(panel, "AddLaneRow"); Fixed(addLaneRow, new Vector2(0, 80), new Vector2(680, 150));
            Img(addLaneRow, Color.clear);
            var addLaneBtn = Btn(addLaneRow, "AddLaneButton", Vector2.zero, new Vector2(660, 140), UI_SUCCESS, "Add Lane", PopupFailBtnAddLane);
            AddButtonIcon(addLaneBtn, "item_add_lane", resMap);
            // "Watch Ad" badge — signals Add Lane is granted by a rewarded ad, not gold.
            var adBadge = Child(addLaneBtn, "AdBadge");
            Fixed(adBadge, new Vector2(235, 42), new Vector2(180, 56));
            Img(adBadge, UI_CTA);
            TMP(adBadge, "Label", Center(0, 0, 168, 52), 24, UI_TEXT, "Watch Ad", PopupFailWatchAd, TextCategory.Button);

            // Shuffle (gold) — icon + live price label.
            var shuffleBtn = Btn(panel, "ShuffleButton", new Vector2(0, -90), new Vector2(660, 140), UI_PRIMARY, "Shuffle", PopupFailBtnShuffle);
            AddButtonIcon(shuffleBtn, "item_shuffle", resMap);
            var costGo = Child(shuffleBtn, "ShuffleCost");
            Fixed(costGo, new Vector2(210, 0), new Vector2(140, 60));
            var costTmp = Comp<TextMeshProUGUI>(costGo);
            ApplyAutoFontSize(costTmp, TextCategory.Button);
            costTmp.color = UI_TEXT; costTmp.alignment = TextAlignmentOptions.Midline; costTmp.text = "0"; costTmp.raycastTarget = false;
            Comp<LocalizedText>(costGo); Comp<UITextStyle>(costGo).ApplyStyle();
            var goldIcon = Child(shuffleBtn, "CostIcon"); Fixed(goldIcon, new Vector2(285, 0), new Vector2(48, 48));
            var goldImg = Img(goldIcon, Color.white); goldImg.preserveAspect = true; goldImg.raycastTarget = false;
            if (resMap.TryGetValue("ui_gold_icon", out var goldPath))
            {
                var gs = AssetDatabase.LoadAssetAtPath<Sprite>(goldPath);
                if (gs != null) goldImg.sprite = gs;
            }

            var forfeitBtn = Btn(panel, "ForfeitButton", new Vector2(0, -270), new Vector2(420, 110), UI_DANGER, "Give Up", PopupFailBtnForfeit);

            // Shuffle spend confirm — in-panel modal (kept inside this popup, not a second UIManager
            // popup, so closing never races on the popup stack). Hidden until Shuffle is tapped.
            var confirmPanel = Child(root, "ShuffleConfirmPanel");
            Stretch(confirmPanel);
            Img(confirmPanel, DIM);
            var confirmCard = Panel(confirmPanel, "ConfirmCard", new Vector2(640, 440), UI_BG_MID);
            RibbonTitle(confirmCard, "TitleText", "Shuffle", PopupFailBtnShuffle);
            var confirmBody = TMP(confirmCard, "BodyText", Center(0, 50, 560, 150), 28, UI_TEXT, "Spend 0 Gold to shuffle the board?", null, TextCategory.Normal);
            var confirmYes = Btn(confirmCard, "ConfirmYesButton", new Vector2( 150, -130), new Vector2(260, 100), UI_CTA,     "Confirm", CommonBtnConfirm);
            var confirmNo  = Btn(confirmCard, "ConfirmNoButton",  new Vector2(-150, -130), new Vector2(260, 100), UI_BG_DEEP, "Cancel",  CommonBtnCancel);
            confirmPanel.transform.SetAsLastSibling();
            // Left active in the prefab so the card/buttons are positionable in the Final variant;
            // runtime visibility is owned by Configure (SetActive(false) on open, shown on Shuffle tap).

            var so = new SerializedObject(root.GetComponent<FailOverlayView>());
            so.FindProperty("_titleText").objectReferenceValue       = title;
            so.FindProperty("_addLaneRow").objectReferenceValue      = addLaneRow;
            so.FindProperty("_addLaneButton").objectReferenceValue   = addLaneBtn.GetComponent<Button>();
            so.FindProperty("_shuffleButton").objectReferenceValue   = shuffleBtn.GetComponent<Button>();
            so.FindProperty("_shuffleCostText").objectReferenceValue = costTmp;
            so.FindProperty("_forfeitButton").objectReferenceValue   = forfeitBtn.GetComponent<Button>();
            so.FindProperty("_shuffleConfirmPanel").objectReferenceValue = confirmPanel;
            so.FindProperty("_shuffleConfirmBody").objectReferenceValue  = confirmBody;
            so.FindProperty("_shuffleConfirmYes").objectReferenceValue   = confirmYes.GetComponent<Button>();
            so.FindProperty("_shuffleConfirmNo").objectReferenceValue    = confirmNo.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "FailOverlayView");
        }

        // Adds a left-aligned icon Image to a wide (non-square) button from a dynamic_resource key.
        static void AddButtonIcon(GameObject btn, string key, Dictionary<string, string> resMap)
        {
            if (btn == null || resMap == null || !resMap.TryGetValue(key, out var path)) return;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) return;
            var icon = Child(btn, "Icon");
            Fixed(icon, new Vector2(-250, 0), new Vector2(64, 64));
            var img = Img(icon, Color.white);
            img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
        }

        static void CreatePausePopup()
        {
            var root = FullScreen("PausePopupView");
            Img(root, DIM); Comp<PausePopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            // Backdrop: visual dim only — close is via explicit CloseButton below
            var backdrop = Child(root, "Backdrop");
            Stretch(backdrop);
            Img(backdrop, DIM);

            var panel = Panel(root, "Panel", new Vector2(600, 500), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Paused", PopupPauseTitle);

            var resume  = Btn(panel, "ResumeButton",      new Vector2(0,   80), new Vector2(480, 96), UI_CTA,     "Resume",       PopupPauseBtnResume);
            var restart = Btn(panel, "RestartButton",     new Vector2(0,  -30), new Vector2(480, 96), UI_DANGER,  "Restart",      PopupPauseBtnRestart);
            var select  = Btn(panel, "StageSelectButton", new Vector2(0, -140), new Vector2(480, 96), UI_BG_DEEP, "Stage Select", PopupPauseBtnStageSelect);
            var closeBtn = CloseBtnAt(panel, new Vector2(250, 208));

            var so = new SerializedObject(root.GetComponent<PausePopupView>());
            so.FindProperty("_resumeButton").objectReferenceValue      = resume.GetComponent<Button>();
            so.FindProperty("_restartButton").objectReferenceValue     = restart.GetComponent<Button>();
            so.FindProperty("_stageSelectButton").objectReferenceValue = select.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue       = closeBtn;
            so.ApplyModifiedProperties();

            Save(root, "PausePopupView");
        }

        static void CreateSettingsPanel()
        {
            var root = FullScreen("SettingsPanelView");
            Img(root, DIM); Comp<SettingsPanelView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            // Backdrop: visual dim only — close is via explicit CloseButton below
            var backdrop = Child(root, "Backdrop");
            Stretch(backdrop);
            Img(backdrop, DIM);

            // Bottom-sheet: taller panel so dropdown popup opens within screen bounds
            var border = Child(root, "Border");
            BottomStrip(border, 1050);
            Img(border, UI_BG_DEEP);
            PixelShadow(border);

            var panel = Child(border, "InnerPanel");
            BottomStrip(panel, 1030);
            Img(panel, UI_BG_MID);

            var title = RibbonTitle(panel, "TitleText", "Settings", CommonSettings);

            var bgmRow    = SoundRow(panel,  "BGMRow",         new Vector2(0,  370), "BGM",          PopupSettingsBgm);
            var sfxRow    = SoundRow(panel,  "SFXRow",         new Vector2(0,  265), "SFX",          PopupSettingsSfx);
            var shakeRow  = ToggleRow(panel, "ScreenShakeRow", new Vector2(0,  160), "Screen Shake", PopupSettingsScreenShake);
            var hapticRow = ToggleRow(panel, "HapticRow",      new Vector2(0,   65), "Haptic",       PopupSettingsHaptic);

            var langDropdown = LanguageDropdownRow(panel, "LanguageRow", new Vector2(0, -45), "Language", PopupSettingsLanguage);
            var verTxt = TMP(panel, "VersionText", Center(0, -175, 600, 50), 16, UI_TEXT, "v1.0.0", null, TextCategory.Normal);

            // Explicit square close button — top-right of bottom-sheet (convention: all popups must have one)
            var closeBtn = CloseBtnAt(panel, new Vector2(460, 460));

            var so = new SerializedObject(root.GetComponent<SettingsPanelView>());
            so.FindProperty("_bgmToggle").objectReferenceValue         = bgmRow.transform.Find("Toggle").GetComponent<Toggle>();
            so.FindProperty("_sfxToggle").objectReferenceValue         = sfxRow.transform.Find("Toggle").GetComponent<Toggle>();
            so.FindProperty("_bgmSlider").objectReferenceValue         = bgmRow.transform.Find("Slider").GetComponent<Slider>();
            so.FindProperty("_sfxSlider").objectReferenceValue         = sfxRow.transform.Find("Slider").GetComponent<Slider>();
            so.FindProperty("_screenShakeToggle").objectReferenceValue = shakeRow.GetComponentInChildren<Toggle>();
            so.FindProperty("_hapticToggle").objectReferenceValue      = hapticRow.GetComponentInChildren<Toggle>();
            so.FindProperty("_langDropdown").objectReferenceValue      = langDropdown;
            so.FindProperty("_backdropButton").objectReferenceValue    = closeBtn;
            so.FindProperty("_versionText").objectReferenceValue       = verTxt;
            so.ApplyModifiedProperties();

            Save(root, "SettingsPanelView");
        }

        static void CreateAccountPopup()
        {
            var root = FullScreen("AccountPopupView");
            Img(root, DIM); Comp<AccountPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Child(root, "Backdrop");
            Stretch(backdrop);
            Img(backdrop, DIM);

            var panel = Panel(root, "Panel", new Vector2(850, 720), UI_BG_MID);
            var titleTxt = RibbonTitle(panel, "TitleText", "Account", PopupAccountTitle);

            // Avatar selection + Achievements entry moved out (Shop avatar section / lobby Achievement tab).

            // 1. Nickname Input Area (grouped in NicknameArea)
            var nicknameAreaGo = Child(panel, "NicknameArea");
            Fixed(nicknameAreaGo, new Vector2(0, 150), new Vector2(800, 160));
            
            var nickLabel = TMP(nicknameAreaGo, "NicknameLabel", Center(0, 45, 750, 50), 20, UI_TEXT, "Nickname", PopupAccountLabelNickname, TextCategory.Normal);

            var inputFieldGo = Child(nicknameAreaGo, "DisplayNameInput");
            Fixed(inputFieldGo, new Vector2(-100, -30), new Vector2(500, 80));
            var inputImg = Comp<Image>(inputFieldGo);
            inputImg.color = UI_BG_DEEP;
            var inputField = Comp<TMP_InputField>(inputFieldGo);

            var textAreaGo = Child(inputFieldGo, "TextArea");
            Stretch(textAreaGo);
            var textAreaRt = RT(textAreaGo);
            textAreaRt.offsetMin = new Vector2(15, 10);
            textAreaRt.offsetMax = new Vector2(-15, -10);
            Comp<RectMask2D>(textAreaGo);

            var textComponentGo = Child(textAreaGo, "TextComponent");
            Stretch(textComponentGo);
            var textTmp = Comp<TextMeshProUGUI>(textComponentGo);
            // InputField exception: fixed min=max=32 (autosize on, no resize range → caret/scroll safe)
            textTmp.enableAutoSizing = true;
            textTmp.fontSizeMin = 32f;
            textTmp.fontSizeMax = 32f;
            textTmp.fontSize = 32f;
            textTmp.color = UI_TEXT;
            textTmp.alignment = TextAlignmentOptions.Left;

            var placeholderGo = Child(textAreaGo, "Placeholder");
            Stretch(placeholderGo);
            var placeholderTmp = Comp<TextMeshProUGUI>(placeholderGo);
            // InputField exception: must match input text sizing (fixed 32)
            placeholderTmp.enableAutoSizing = true;
            placeholderTmp.fontSizeMin = 32f;
            placeholderTmp.fontSizeMax = 32f;
            placeholderTmp.fontSize = 32f;
            placeholderTmp.color = new Color(UI_TEXT.r, UI_TEXT.g, UI_TEXT.b, 0.5f);
            placeholderTmp.text = "Enter nickname...";
            placeholderTmp.alignment = TextAlignmentOptions.Left;
            var placeholderLt = Comp<LocalizedText>(placeholderGo);
            var soPlaceholder = new SerializedObject(placeholderLt);
            soPlaceholder.FindProperty("_stringId").stringValue = PopupAccountPlaceholderNickname;
            soPlaceholder.ApplyModifiedProperties();

            inputField.textViewport = textAreaRt;
            inputField.textComponent = textTmp;
            inputField.placeholder = placeholderTmp;

            var saveBtn = Btn(nicknameAreaGo, "SaveNicknameButton", new Vector2(250, -30), new Vector2(160, 80), UI_CTA, "Save", PopupAccountBtnSave);

            // 1b. PID Area (grouped in PidArea) — label + value text + copy button (mirrors NicknameArea)
            var pidAreaGo = Child(panel, "PidArea");
            Fixed(pidAreaGo, new Vector2(0, -20), new Vector2(800, 160));

            var pidLabel = TMP(pidAreaGo, "PidLabel", Center(0, 45, 750, 50), 20, UI_TEXT, "PID", PopupAccountLabelPid, TextCategory.Normal);

            // PID value: no stringId → LocalizedText stays inert so AccountPopupView.Awake controls the text
            var pidValueTmp = TMP(pidAreaGo, "PidValueText", Center(-100, -30, 500, 60), 20, UI_TEXT, "Guest", null, TextCategory.Normal);

            // PID copy button — right of the PID value; copies PID to clipboard
            var copyBtn = Btn(pidAreaGo, "CopyPidButton", new Vector2(250, -30), new Vector2(160, 80), UI_PRIMARY, "Copy", PopupAccountBtnCopyPid);

            // 2. Platform Account Buttons (Link for guest / Switch for OAuth — only one shown at runtime)
            var linkBtn = Btn(panel, "LinkAccountButton",   new Vector2(0, -180), new Vector2(600, 96), UI_CTA, "Link Account",   PopupAccountBtnLink);
            var swBtn   = Btn(panel, "SwitchAccountButton", new Vector2(0, -180), new Vector2(600, 96), UI_CTA, "Switch Account", PopupAccountBtnSwitch);

            // Legal links (Privacy / Terms) — open web page per environment
            var privacyBtn = Btn(panel, "PrivacyButton", new Vector2(-160, -290), new Vector2(290, 96), UI_BG_DEEP, "Privacy Policy",   PopupAccountBtnPrivacy);
            var termsBtn   = Btn(panel, "TermsButton",   new Vector2( 160, -290), new Vector2(290, 96), UI_BG_DEEP, "Terms of Service", PopupAccountBtnTerms);

            var closeBtn = CloseBtnAt(panel, new Vector2(377, 312));

            // 3. Serialize Object Wiring (avatar sprite mapping retained for HeaderView / TutorialOverlay)
            var resMap = LoadDynamicResourceMap();
            var so = new SerializedObject(root.GetComponent<AccountPopupView>());
            so.FindProperty("_pidText").objectReferenceValue             = pidValueTmp;
            so.FindProperty("_copyPidButton").objectReferenceValue       = copyBtn.GetComponent<Button>();
            so.FindProperty("_privacyButton").objectReferenceValue       = privacyBtn.GetComponent<Button>();
            so.FindProperty("_termsButton").objectReferenceValue         = termsBtn.GetComponent<Button>();
            so.FindProperty("_linkAccountButton").objectReferenceValue   = linkBtn.GetComponent<Button>();
            so.FindProperty("_switchAccountButton").objectReferenceValue = swBtn.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue         = closeBtn;
            so.FindProperty("_displayNameInput").objectReferenceValue    = inputField;
            so.FindProperty("_saveNicknameButton").objectReferenceValue   = saveBtn.GetComponent<Button>();
            so.FindProperty("_nicknameArea").objectReferenceValue         = nicknameAreaGo;

            PopulateAvatarSprites(so.FindProperty("_avatarSprites"), resMap);

            so.ApplyModifiedProperties();

            Save(root, "AccountPopupView");
        }

        static void CreateStageNodeView()
        {
            string _snvPath = $"{BaseCommonPath}/StageNodeView.prefab";
            _prefabWasLoaded = AssetDatabase.LoadAssetAtPath<GameObject>(_snvPath) != null;
            var root = _prefabWasLoaded ? PrefabUtility.LoadPrefabContents(_snvPath) : new GameObject("StageNodeView");
            var rt = RT(root);
            rt.sizeDelta = new Vector2(130f, 130f);
            
            var snv = Comp<StageNodeView>(root);
            Comp<UIButtonAnimator>(root);

            // Pulse Ring
            var pulseRing = Child(root, "PulseRing");
            Fixed(pulseRing, Vector2.zero, new Vector2(150f, 150f));
            Img(pulseRing, UI_CTA);
            Comp<UIScalePulse>(pulseRing);
            pulseRing.SetActive(false);

            // Difficulty outline (colored at runtime by DifficultyStyle; hidden for Easy)
            var diffOutline = Child(root, "DifficultyOutline");
            Fixed(diffOutline, Vector2.zero, new Vector2(148f, 148f));
            var diffOutlineImg = Img(diffOutline, Color.white);
            diffOutline.SetActive(false);

            // Visual background border
            var visual = Child(root, "Visual");
            Stretch(visual);
            var borderImg = Img(visual, Color.white);
            
            // Inner circle
            var inner = Child(visual, "Inner");
            Fixed(inner, Vector2.zero, new Vector2(110f, 110f));
            Img(inner, UI_PRIMARY);
            
            // Label
            var stageLabel = TMP(inner, "StageLabel", Center(0, 0, 110, 110), 22, UI_TEXT, "1", null, TextCategory.Button);
            
            // Cleared badge — single marker shown when the stage has been cleared (star rating removed)
            var clearedBadge = Child(root, "ClearedBadge");
            Fixed(clearedBadge, new Vector2(0f, -60f), new Vector2(36f, 36f));
            Img(clearedBadge, UI_SUCCESS);
            clearedBadge.SetActive(false);

            // Lock Overlay
            var lockOverlay = Child(root, "LockOverlay");
            Stretch(lockOverlay);
            Img(lockOverlay, new Color(0f, 0f, 0f, 0f));
            var lockIcon = Child(lockOverlay, "LockIcon");
            Fixed(lockIcon, Vector2.zero, new Vector2(40f, 40f));
            Img(lockIcon, Color.white);
            lockOverlay.SetActive(false);

            // Skull badge (Hard only — top-right corner of 130x130 root)
            var skullBadge = Child(root, "SkullBadge");
            Fixed(skullBadge, new Vector2(45f, 45f), new Vector2(40f, 40f));
            var skullBadgeImg = Img(skullBadge, Color.white);
            skullBadge.SetActive(false);

            // Button
            var btn = Comp<Button>(root);
            btn.targetGraphic = borderImg;

            // Wire StageNodeView properties
            var so = new SerializedObject(snv);
            so.FindProperty("_stageLabel").objectReferenceValue       = stageLabel;
            so.FindProperty("_lockOverlay").objectReferenceValue      = lockOverlay;
            var pulseRingProp = so.FindProperty("_pulseRing");
            if (pulseRingProp != null) pulseRingProp.objectReferenceValue = pulseRing;
            so.FindProperty("_button").objectReferenceValue           = btn;
            so.FindProperty("_border").objectReferenceValue           = borderImg;
            so.FindProperty("_difficultyOutline").objectReferenceValue = diffOutlineImg;
            so.FindProperty("_skullIcon").objectReferenceValue        = skullBadge;
            so.FindProperty("_clearedBadge").objectReferenceValue     = clearedBadge;
            so.ApplyModifiedProperties();

            var resMap = LoadDynamicResourceMap();
            var lockSpr    = resMap.TryGetValue("ui_lock_icon", out string lp) ? AssetDatabase.LoadAssetAtPath<Sprite>(lp) : null;
            var lockImgComp = lockIcon.GetComponent<Image>();
            if (lockImgComp != null && lockSpr != null) { lockImgComp.sprite = lockSpr; lockImgComp.preserveAspect = true; }
            var skullSpr = resMap.TryGetValue("ui_hard_skull", out string skulp) ? AssetDatabase.LoadAssetAtPath<Sprite>(skulp) : null;
            if (skullSpr != null) { skullBadgeImg.sprite = skullSpr; skullBadgeImg.preserveAspect = true; }

            Save(root, "StageNodeView");
        }


        static void CreateTutorialOverlay()
        {
            var root = FullScreen("TutorialOverlay");
            var overlayScript = Comp<TutorialOverlay>(root);

            // DimLayer — always-visible full-screen dim; stays active for all steps (including blocking).
            // raycastTarget=true blocks EventSystem interaction (HUD buttons etc.) during blocking steps.
            var dimGo = Child(root, "DimLayer");
            Stretch(dimGo);
            var dimImg = Img(dimGo, new Color(0.05f, 0.05f, 0.12f, 0.88f));

            // Fullscreen Dismiss Button — transparent hit area for tap-to-advance on non-blocking steps.
            // Has no background; DimLayer above provides the visual dim.
            var dismissGo = Child(root, "FullscreenDismissButton");
            Stretch(dismissGo);
            if (!dismissGo.TryGetComponent<Button>(out var dismissBtn)) dismissBtn = Comp<Button>(dismissGo);
            var dismissImg = Img(dismissGo, new Color(0, 0, 0, 0));
            dismissBtn.targetGraphic = dismissImg;
            Comp<UIButtonAnimator>(dismissGo);

            // Spotlight Cutout
            var spotlight = Child(root, "SpotlightCutout");
            Fixed(spotlight, Vector2.zero, new Vector2(150, 150));
            Img(spotlight, new Color(1, 1, 1, 0.0f)); // transparent: dim cutout perception only

            // Spotlight Glow
            var glow = Child(spotlight, "SpotlightGlow");
            Stretch(glow);
            var glowRt = RT(glow);
            glowRt.offsetMin = new Vector2(-10, -10);
            glowRt.offsetMax = new Vector2(10, 10);
            var glowImg = Img(glow, new Color(1, 0.9f, 0.4f, 0.08f)); // subtle border; runtime AnimateGlowPulse drives alpha

            // Tooltip Bubble
            var bubble = Child(root, "TooltipBubble");
            Fixed(bubble, Vector2.zero, new Vector2(800, 300));
            Img(bubble, new Color(0.1f, 0.15f, 0.25f, 0.95f));

            // Tooltip Text
            var textGo = Child(bubble, "TooltipText");
            Stretch(textGo);
            var textRt = RT(textGo);
            textRt.offsetMin = new Vector2(20, 20);
            textRt.offsetMax = new Vector2(-20, -20);
            var textTmp = textGo.GetComponent<TextMeshProUGUI>();
            if (textTmp == null) textTmp = Comp<TextMeshProUGUI>(textGo);
            ApplyAutoFontSize(textTmp, TextCategory.Normal);
            textTmp.color = Color.white;
            textTmp.text = "Tutorial Message";
            textTmp.alignment = TextAlignmentOptions.Center;
            textTmp.enableWordWrapping = true;
            Comp<LocalizedText>(textGo); // font-only: runtime sets text via TutorialOverlay

            var avatar = Child(bubble, "CharacterAvatar");
            var avatarRt = RT(avatar);
            avatarRt.anchorMin = new Vector2(0, 0.5f);
            avatarRt.anchorMax = new Vector2(0, 0.5f);
            avatarRt.pivot = new Vector2(0.5f, 0.5f);
            avatarRt.anchoredPosition = new Vector2(-60, 0);
            avatarRt.sizeDelta = new Vector2(150, 150);
            var avatarImg = Img(avatar, Color.cyan);

            // Finger Overlay
            var finger = Child(root, "FingerOverlay");
            Fixed(finger, Vector2.zero, new Vector2(100, 100));
            Img(finger, new Color(1, 0.3f, 0.3f, 0.9f));

            // Wire serialized fields
            var so = new SerializedObject(overlayScript);
            so.FindProperty("_dimLayer").objectReferenceValue = dimImg;
            so.FindProperty("_spotlightCutout").objectReferenceValue = spotlight.GetComponent<RectTransform>();
            so.FindProperty("_spotlightGlow").objectReferenceValue = glowImg;
            so.FindProperty("_tooltipBubble").objectReferenceValue = bubble.GetComponent<RectTransform>();
            so.FindProperty("_tooltipText").objectReferenceValue = textTmp;
            so.FindProperty("_fingerOverlay").objectReferenceValue = finger.GetComponent<RectTransform>();
            so.FindProperty("_characterAvatar").objectReferenceValue = avatarImg;
            so.FindProperty("_fullscreenDismissButton").objectReferenceValue = dismissBtn;
            so.ApplyModifiedProperties();

            Save(root, "TutorialOverlay");
        }

        static void CreateChapterChest()
        {
            string _chestPath = $"{BaseCommonPath}/ChapterChest.prefab";
            _prefabWasLoaded = AssetDatabase.LoadAssetAtPath<GameObject>(_chestPath) != null;
            var root = _prefabWasLoaded ? PrefabUtility.LoadPrefabContents(_chestPath) : new GameObject("ChapterChest");
            var rootRt = RT(root);
            rootRt.sizeDelta = new Vector2(120, 120);

            var chestBtn = Comp<Button>(root);
            var chestCg = Comp<CanvasGroup>(root);
            var chestView = Comp<ChapterChestView>(root);

            // GlowEffect — child 0, renders behind ChestImage
            var glow = Child(root, "GlowEffect");
            RT(glow).sizeDelta = new Vector2(180, 180);
            var glowImg = Img(glow, new Color(1, 0.85f, 0.3f, 0.5f));
            var glowEffect = Comp<UIPulseGlowEffect>(glow);
            var soGlow = new SerializedObject(glowEffect);
            soGlow.FindProperty("_rotationSpeed").floatValue = 0f;
            soGlow.ApplyModifiedProperties();

            Shader glowShader = Shader.Find("UI/PulseGlow");
            if (glowShader != null)
            {
                string matDir = "Assets/Resources/Prefabs/UI";
                MkDir(matDir);
                string matPath = $"{matDir}/PulseGlowMaterial.mat";
                Material glowMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (glowMat == null)
                {
                    glowMat = new Material(glowShader);
                    glowMat.name = "PulseGlowMaterial";
                    AssetDatabase.CreateAsset(glowMat, matPath);
                }
                glowImg.material = glowMat;
            }

            // ChestImage — child 1, renders in front of GlowEffect
            var chestImgGo = Child(root, "ChestImage");
            RT(chestImgGo).sizeDelta = new Vector2(120, 120);
            var chestImg = Img(chestImgGo, Color.white);
            chestImg.preserveAspect = true;
            chestBtn.targetGraphic = chestImg;

            // SparkleParticles — child 2
            var sparkleGo = Child(root, "SparkleParticles");
            var sparkleRt = RT(sparkleGo);
            sparkleRt.anchorMin = sparkleRt.anchorMax = new Vector2(0.5f, 0.5f);
            sparkleRt.pivot = new Vector2(0.5f, 0.5f);
            sparkleRt.anchoredPosition = Vector2.zero;
            sparkleRt.sizeDelta = Vector2.zero;

            var ps = Comp<ParticleSystem>(sparkleGo);
            var psRenderer = sparkleGo.GetComponent<ParticleSystemRenderer>();

            var psMain = ps.main;
            psMain.loop = true;
            psMain.duration = 1f;
            psMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            psMain.startSpeed = new ParticleSystem.MinMaxCurve(60f, 140f);
            psMain.startSize = new ParticleSystem.MinMaxCurve(5f, 14f);
            psMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.92f, 0.3f, 1f),
                new Color(1f, 0.55f, 0.1f, 0.9f));
            psMain.maxParticles = 40;
            psMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            psMain.gravityModifier = 0f;

            var psEmission = ps.emission;
            psEmission.rateOverTime = 20f;

            var psShape = ps.shape;
            psShape.enabled = true;
            psShape.shapeType = ParticleSystemShapeType.Circle;
            psShape.radius = 0.4f;

            psRenderer.sortingLayerName = "UI";
            psRenderer.sortingOrder = 15;
            psRenderer.material = new Material(Shader.Find("Sprites/Default"));
            sparkleGo.SetActive(false);

            // ClearedCountContainer — dark pill below chest image; shows {cleared}/{total} stages
            var starContainer = Child(root, "ClearedCountContainer");
            Fixed(starContainer, new Vector2(0, -85), new Vector2(150, 40));
            var starContainerImg = Img(starContainer, Hex("2A1635"));
            starContainerImg.raycastTarget = false;

            var starShadow = Child(starContainer, "Shadow");
            Fixed(starShadow, new Vector2(0, -3), new Vector2(150, 40));
            var starShadowImg = Img(starShadow, Hex("1A0020"));
            starShadowImg.raycastTarget = false;
            starShadow.transform.SetAsFirstSibling();

            var starTxt = TMP(starContainer, "ClearedCountText", Center(0, 1, 140, 34), 16, UI_TEXT, "0/0", null, TextCategory.Normal);
            starTxt.alignment = TextAlignmentOptions.Center;
            starTxt.enableWordWrapping = false;

            // Bind Serialized Fields on ChapterChestView
            var so = new SerializedObject(chestView);
            so.FindProperty("_chestImage").objectReferenceValue = chestImg;
            so.FindProperty("_button").objectReferenceValue = chestBtn;
            so.FindProperty("_glowEffect").objectReferenceValue = glow;
            so.FindProperty("_sparkleParticles").objectReferenceValue = ps;
            so.FindProperty("_canvasGroup").objectReferenceValue = chestCg;
            so.FindProperty("_clearedCountLabel").objectReferenceValue = starTxt;

            // Map chest sprites from dynamic_resource.csv
            var resMap = LoadDynamicResourceMap();
            TryMapSprite(so, "_inactiveSprite", "chest_inactive", resMap);
            TryMapSprite(so, "_activeSprite",   "chest_active",   resMap);
            TryMapSprite(so, "_claimedSprite",  "chest_claimed",  resMap);

            so.ApplyModifiedProperties();

            Save(root, "ChapterChest");
        }

        // ════════════════════════════════════════════════════════════════
        //  LAYOUT HELPERS
        // ════════════════════════════════════════════════════════════════

        static void Stretch(GameObject go)
        {
            var rt = RT(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        // Top-anchored fixed-height strip (pivot top-center)
        static void TopStrip(GameObject go, float h)
        {
            var rt = RT(go);
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, h); rt.anchoredPosition = Vector2.zero;
        }

        // Bottom-anchored fixed-height strip (pivot bottom-center)
        static void BottomStrip(GameObject go, float h)
        {
            var rt = RT(go);
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot     = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, h); rt.anchoredPosition = Vector2.zero;
        }

        // Stretch with uniform padding from each edge (using offsetMin/offsetMax)
        static void PaddedStretch(GameObject go, float topPad, float bottomPad)
        {
            var rt = RT(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, bottomPad);
            rt.offsetMax = new Vector2(0, -topPad);
        }

        // Fixed-size, center-pivoted, offset from parent center
        static void Fixed(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = RT(go);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

        // Center-pivot fixed rect helper for TMP
        static Rect Center(float x, float y, float w, float h) =>
            new Rect(x, y, w, h);

        // ════════════════════════════════════════════════════════════════
        //  COMPONENT / OBJECT HELPERS
        // ════════════════════════════════════════════════════════════════

        static GameObject FullScreen(string name)
        {
            string path = $"{BaseCommonPath}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                _prefabWasLoaded = true;
                return PrefabUtility.LoadPrefabContents(path);
            }
            _prefabWasLoaded = false;
            var go = new GameObject(name);
            Comp<RectTransform>(go);
            Stretch(go);
            return go;
        }

        // Pixel art drop shadow: stretch-stretch on parent, offset right+8 bottom+8.
        // Call on any Panel/Button/Container GO; shadow renders behind (SetAsFirstSibling).
        static GameObject PixelShadow(GameObject parent, Color? color = null)
        {
            var shadow = Child(parent, "Shadow");
            var rt = RT(shadow);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, -8f);
            rt.offsetMax = new Vector2(8f, -8f);
            Img(shadow, color ?? UI_SHADOW);
            shadow.transform.SetAsFirstSibling();
            return shadow;
        }

        // Square explicit close button (min 96px). Wire returned Button to View close handler.
        static Button CloseBtnAt(GameObject parent, Vector2 anchoredPos, int size = 96)
        {
            var go = Child(parent, "CloseButton");
            Fixed(go, anchoredPos, new Vector2(size, size));
            var visual = Child(go, "Visual");
            Stretch(visual);
            var img = Img(visual, Color.white);
            var spr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Icons/ui_close_button.png");
            if (spr != null) { img.sprite = spr; img.preserveAspect = true; }
            var btn = Comp<Button>(go);
            btn.targetGraphic = img;
            Comp<UIButtonAnimator>(go);
            return btn;
        }

        // Panel: 3-layer pixel art structure — Shadow (bottom) → Border outline → Content fill.
        // size = content area. Outer = size + 16 (8px border each side).
        // outline: null defaults to UI_BORDER (electric violet, contrasts dark backgrounds).
        static GameObject Panel(GameObject parent, string name, Vector2 size, Color color, Color? outline = null)
        {
            var root = Child(parent, name);
            Fixed(root, Vector2.zero, size + new Vector2(16f, 16f)); // 8px border each side

            PixelShadow(root);                              // index 0 — lowest layer

            var border = Child(root, "Border");
            Stretch(border);
            Img(border, outline ?? UI_BORDER);              // index 1 — contrasting outline frame

            var content = Child(root, "Content");
            Stretch(content);
            var contentRt = RT(content);
            contentRt.offsetMin = new Vector2(8f, 8f);
            contentRt.offsetMax = new Vector2(-8f, -8f);
            Img(content, color);                            // index 2 — panel fill
            return content;
        }

        static TMP_Text RibbonTitle(GameObject panel, string name, string text, string stringId = null)
        {
            var panelRt = RT(panel);
            float panelW = panelRt.sizeDelta.x;
            float panelH = panelRt.sizeDelta.y;

            // If anchors are stretched, sizeDelta contains margins rather than absolute size.
            // Read the parent PanelRoot size (which is content + 16px total border).
            if (panelRt.anchorMin == Vector2.zero && panelRt.anchorMax == Vector2.one)
            {
                var parentRt = panel.transform.parent.GetComponent<RectTransform>();
                if (parentRt != null)
                {
                    panelW = parentRt.sizeDelta.x - 16f; // 8px border each side = 16 total
                    panelH = parentRt.sizeDelta.y - 16f;
                }
            }

            // Safe fallback bounds
            if (panelW <= 0f) panelW = 800f;
            if (panelH <= 0f) panelH = 500f;

            // Ribbon base banner (UI_CTA) at top of panel
            var ribbon = Child(panel, name + "_Ribbon");
            Fixed(ribbon, new Vector2(0f, panelH * 0.5f), new Vector2(panelW * 0.85f, 100f));
            Img(ribbon, UI_CTA);
            PixelShadow(ribbon);

            var tmp = TMP(ribbon, "Text", new Rect(0, 0, panelW * 0.8f, 80f), 36, UI_TEXT, text, stringId, TextCategory.Header);
            return tmp;
        }

        // Button with label child. Enforces 96px minimum on both dimensions.
        static GameObject Btn(GameObject parent, string name, Vector2 pos, Vector2 size, Color color, string label, string labelStringId = null, float shadowAlpha = 1f)
        {
            size = new Vector2(Mathf.Max(size.x, 96f), Mathf.Max(size.y, 96f));
            var go = Child(parent, name); Fixed(go, pos, size);

            // Pixel art drop shadow: stretch on button bounds, offset right+8 bottom+8
            var shadowColor = UI_SHADOW; shadowColor.a = shadowAlpha;
            PixelShadow(go, shadowColor);
            
            // Visual top layer
            var visualGo = Child(go, "Visual");
            Stretch(visualGo);
            var img = Img(visualGo, color);
            
            if (!go.TryGetComponent<Button>(out var btn)) btn = Comp<Button>(go);
            btn.targetGraphic = img;
            var buttonAnim = Comp<UIButtonAnimator>(go);
            if (color == UI_CTA)
            {
                var soAnim = new SerializedObject(buttonAnim);
                soAnim.FindProperty("_isCTA").boolValue = true;
                soAnim.ApplyModifiedProperties();
            }

            // Square buttons: stretch-stretch Icon with GlowSweep idle animation.
            bool isSquare = Mathf.Approximately(size.x, size.y);
            if (isSquare)
            {
                var iconGo = Child(visualGo, "Icon");
                Stretch(iconGo);
                var iconImg = Img(iconGo, Color.white);
                iconImg.preserveAspect = true;
                var animator = Comp<UIIconIdleAnimator>(iconGo);
                animator.Configure(UIIconIdleAnimator.AnimationType.GlowSweep, 2.2f, 12f);
            }
            else if (!string.IsNullOrEmpty(label))
            {
                TMP(visualGo, "Label", Center(0, 0, size.x, size.y), 20, UI_TEXT, label, labelStringId, TextCategory.Button);
            }
            return go;
        }

        // Button for use inside LayoutGroup — uses LayoutElement
        static GameObject BtnHlg(GameObject parent, string name, Color color, string label, string labelStringId = null)
        {
            var go = Child(parent, name);
            var rt = RT(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var le = Comp<LayoutElement>(go);
            le.flexibleWidth  = 1;
            le.preferredHeight = 100;
            PixelShadow(go);
            
            // Visual top layer
            var visualGo = Child(go, "Visual");
            Stretch(visualGo);
            var img = Img(visualGo, color);
            
            if (!go.TryGetComponent<Button>(out var btn)) btn = Comp<Button>(go);
            btn.targetGraphic = img;
            Comp<UIButtonAnimator>(go);
            
            TMP(visualGo, "Label", Center(0, 0, 180, 80), 18, UI_TEXT, label, labelStringId, TextCategory.Button);
            return go;
        }

        static string StripEmojis(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in input)
            {
                int val = (int)c;
                if (val >= 0x2600 && val <= 0x27BF) continue;
                if (val >= 0xD800 && val <= 0xDFFF) continue;
                sb.Append(c);
            }
            return sb.ToString().Replace("👤", "").Replace("🪙", "").Replace("⏸", "").Replace("★", "").Trim();
        }

        static TMP_Text TMP(GameObject parent, string name, Rect rect, int size, Color color, string text, string stringId = null, TextCategory category = TextCategory.Normal)
        {
            var go = Child(parent, name);
            Fixed(go, new Vector2(rect.x, rect.y), new Vector2(rect.width, rect.height));
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = Comp<TextMeshProUGUI>(go);

            // Apply EN font from FontLocalizationConfig as editor default; LocalizedText handles runtime switching.
            var editorFont = GetEditorDefaultFont();
            if (editorFont != null) tmp.font = editorFont;

            ApplyAutoFontSize(tmp, category);
            tmp.color     = color;
            tmp.text      = StripEmojis(text);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false; // Disable raycast to prevent blocking scroll/clicks on background elements
            
            var lt = Comp<LocalizedText>(go);
            if (!string.IsNullOrEmpty(stringId))
            {
                var soLt = new SerializedObject(lt);
                soLt.FindProperty("_stringId").stringValue = stringId;
                soLt.ApplyModifiedProperties();
            }
            
            var style = Comp<UITextStyle>(go);
            style.ApplyStyle();

            return tmp;
        }

        /// <summary>
        /// Readability convention (MANDATORY for every TMP): AutoFontSize is ALWAYS on, min font
        /// size is 32 (never smaller), and `category` drives the max so titles render large:
        /// Header 32–72, Button 32–56, Normal 32–40. fontSize starts at max and shrinks to fit
        /// down to 32. Call for hand-built TMP too (TMP_InputField is the only exception — it uses
        /// a fixed min=max size inline to avoid caret/scroll glitches).
        /// </summary>
        static void ApplyAutoFontSize(TMP_Text tmp, TextCategory category)
        {
            float max;
            switch (category)
            {
                case TextCategory.Header: max = 72f; break;
                case TextCategory.Button: max = 56f; break;
                case TextCategory.Normal:
                default:                  max = 40f; break;
            }
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 32f;
            tmp.fontSizeMax = max;
            tmp.fontSize    = max;
        }

        static Image Img(GameObject go, Color color)
        {
            if (!go.TryGetComponent<Image>(out var img)) img = Comp<Image>(go);
            img.color = color; return img;
        }

        static T Comp<T>(GameObject go) where T : Component
        {
            if (!go.TryGetComponent<T>(out var c)) c = go.AddComponent<T>(); return c;
        }

        static GameObject Child(GameObject parent, string childName)
        {
            var t = parent.transform.Find(childName);
            if (t != null) return t.gameObject;
            var go = new GameObject(childName);
            Comp<RectTransform>(go);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static RectTransform RT(GameObject go)
        {
            if (!go.TryGetComponent<RectTransform>(out var rt)) rt = Comp<RectTransform>(go);
            return rt;
        }

        static GameObject ToggleRow(GameObject parent, string rowName, Vector2 pos, string label, string labelStringId = null)
        {
            var row = Child(parent, rowName); Fixed(row, pos, new Vector2(800, 80));
            TMP(row, "Label",  Center(-220, 0, 400, 60), 22, UI_TEXT, label, labelStringId, TextCategory.Normal);

            var tgo = Child(row, "Toggle"); Fixed(tgo, new Vector2(280, 0), new Vector2(100, 60));
            if (!tgo.TryGetComponent<Toggle>(out var toggle)) toggle = Comp<Toggle>(tgo);
            var bg  = Child(tgo, "Background"); Fixed(bg,   Vector2.zero, new Vector2(100, 60)); Img(bg, UI_BG_DEEP);
            var chk = Child(tgo, "Checkmark");  Fixed(chk,  Vector2.zero, new Vector2(50, 50)); Img(chk, UI_CTA);
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic       = chk.GetComponent<Image>();
            toggle.isOn          = true;
            return row;
        }

        static GameObject SoundRow(GameObject parent, string rowName, Vector2 pos, string label, string labelStringId = null)
        {
            var row = Child(parent, rowName); Fixed(row, pos, new Vector2(800, 80));
            TMP(row, "Label", Center(-260, 0, 240, 60), 22, UI_TEXT, label, labelStringId, TextCategory.Normal);

            var tgo = Child(row, "Toggle"); Fixed(tgo, new Vector2(-60, 0), new Vector2(80, 50));
            var toggle = Comp<Toggle>(tgo);
            var bg  = Child(tgo, "Background"); Fixed(bg,   Vector2.zero, new Vector2(80, 50)); Img(bg, UI_BG_DEEP);
            var chk = Child(tgo, "Checkmark");  Fixed(chk,  Vector2.zero, new Vector2(40, 40)); Img(chk, UI_CTA);
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic       = chk.GetComponent<Image>();
            toggle.isOn          = true;

            var sgo = Child(row, "Slider"); Fixed(sgo, new Vector2(200, 0), new Vector2(380, 40));
            var slider = Comp<Slider>(sgo);

            var sbg = Child(sgo, "Background"); Stretch(sbg); Img(sbg, UI_BG_DEEP);
            var sbgRt = RT(sbg);
            sbgRt.offsetMin = new Vector2(0, 15);
            sbgRt.offsetMax = new Vector2(0, -15);

            var fillArea = Child(sgo, "Fill Area"); Stretch(fillArea);
            var fillAreaRt = RT(fillArea);
            fillAreaRt.offsetMin = new Vector2(5, 0);
            fillAreaRt.offsetMax = new Vector2(-15, 0);

            var fill = Child(fillArea, "Fill");
            var fillRt = RT(fill);
            fillRt.anchorMin = new Vector2(0, 0.25f);
            fillRt.anchorMax = new Vector2(0.5f, 0.75f);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            Img(fill, UI_PRIMARY);

            var handleArea = Child(sgo, "Handle Slide Area"); Stretch(handleArea);
            var handleAreaRt = RT(handleArea);
            handleAreaRt.offsetMin = new Vector2(10, 0);
            handleAreaRt.offsetMax = new Vector2(-10, 0);

            var handle = Child(handleArea, "Handle");
            var handleRt = RT(handle);
            handleRt.anchorMin = new Vector2(0.5f, 0);
            handleRt.anchorMax = new Vector2(0.5f, 1);
            handleRt.sizeDelta = new Vector2(30, 30);
            handleRt.anchoredPosition = Vector2.zero;
            Img(handle, UI_CTA);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            return row;
        }

        static GameObject LanguageRow(GameObject parent, string rowName, Vector2 pos, string label, string labelStringId, out GameObject koBtn, out GameObject enBtn, out GameObject jaBtn)
        {
            var row = Child(parent, rowName); Fixed(row, pos, new Vector2(800, 80));
            TMP(row, "Label", Center(-260, 0, 240, 60), 22, UI_TEXT, label, labelStringId, TextCategory.Normal);

            var container = Child(row, "Buttons"); Fixed(container, new Vector2(170, 0), new Vector2(440, 70));
            var hlg = Comp<HorizontalLayoutGroup>(container);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            koBtn = BtnHlg(container, "KOButton", UI_PRIMARY, "KO");
            enBtn = BtnHlg(container, "ENButton", UI_BG_DEEP, "EN");
            jaBtn = BtnHlg(container, "JAButton", UI_BG_DEEP, "JA");

            return row;
        }

        static TMP_Dropdown LanguageDropdownRow(GameObject parent, string rowName, Vector2 pos, string label, string labelStringId)
        {
            var row = Child(parent, rowName);
            Fixed(row, pos, new Vector2(800, 80));
            TMP(row, "Label", Center(-220, 0, 400, 60), 22, UI_TEXT, label, labelStringId, TextCategory.Normal);

            // Dropdown container
            var dropGo = Child(row, "Dropdown");
            Fixed(dropGo, new Vector2(190, 0), new Vector2(360, 70));
            var dropBg = Img(dropGo, UI_BG_DEEP);

            // Caption — stretched rect; -70 right inset keeps text clear of the arrow
            var captionGo = Child(dropGo, "Label");
            var captionRt = RT(captionGo);
            captionRt.anchorMin = Vector2.zero;
            captionRt.anchorMax = Vector2.one;
            captionRt.offsetMin = new Vector2(15, 5);
            captionRt.offsetMax = new Vector2(-70, -5);
            var captionTmp = Comp<TextMeshProUGUI>(captionGo);
            ApplyAutoFontSize(captionTmp, TextCategory.Normal);
            captionTmp.color              = UI_TEXT;
            captionTmp.alignment          = TextAlignmentOptions.Left;
            captionTmp.overflowMode       = TextOverflowModes.Overflow;
            captionTmp.enableWordWrapping = false;
            captionTmp.text               = "한국어";
            Comp<LocalizedText>(captionGo);   // _stringId empty → font-only mode
            Comp<UITextStyle>(captionGo).ApplyStyle();

            // Arrow — sits in the right 70px zone
            var arrowGo = Child(dropGo, "Arrow");
            Fixed(arrowGo, new Vector2(155, 0), new Vector2(32, 32));
            Img(arrowGo, UI_TEXT);

            // Template (inactive; TMP_Dropdown repositions at runtime)
            var templateGo = Child(dropGo, "Template");
            var templateRt = RT(templateGo);
            templateRt.anchorMin        = new Vector2(0, 1);
            templateRt.anchorMax        = new Vector2(1, 1);
            templateRt.pivot            = new Vector2(0.5f, 0f);
            templateRt.anchoredPosition = new Vector2(0, 4);
            templateRt.sizeDelta        = new Vector2(0, 160);
            Img(templateGo, UI_BG_MID);

            var scrollRect = Comp<ScrollRect>(templateGo);
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;

            var viewportGo = Child(templateGo, "Viewport");
            Stretch(viewportGo);
            Comp<RectMask2D>(viewportGo);
            scrollRect.viewport = RT(viewportGo);

            var contentGo = Child(viewportGo, "Content");
            var contentRt = RT(contentGo);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 70);
            scrollRect.content  = contentRt;

            // Item template (one instance; TMP_Dropdown clones per option)
            var itemGo = Child(contentGo, "Item");
            var itemRt = RT(itemGo);
            itemRt.anchorMin = new Vector2(0, 0.5f);
            itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.sizeDelta = new Vector2(0, 65);
            var itemToggle = Comp<Toggle>(itemGo);

            var itemBgGo  = Child(itemGo, "Item Background");
            Stretch(itemBgGo);
            var itemBgImg = Img(itemBgGo, UI_BG_DEEP);
            itemToggle.targetGraphic = itemBgImg;

            var itemChkGo = Child(itemGo, "Item Checkmark");
            Fixed(itemChkGo, new Vector2(140, 0), new Vector2(28, 28));  // right side
            Img(itemChkGo, UI_CTA);
            itemToggle.graphic = itemChkGo.GetComponent<Image>();

            var itemLabelGo  = Child(itemGo, "Item Label");
            var itemLabelRt  = RT(itemLabelGo);
            itemLabelRt.anchorMin = Vector2.zero;
            itemLabelRt.anchorMax = Vector2.one;
            itemLabelRt.offsetMin = new Vector2(20, 4);
            itemLabelRt.offsetMax = new Vector2(-50, -4);  // -50 clears right-side checkmark
            var itemLabelTmp = Comp<TextMeshProUGUI>(itemLabelGo);
            ApplyAutoFontSize(itemLabelTmp, TextCategory.Normal);
            itemLabelTmp.color              = UI_TEXT;
            itemLabelTmp.alignment          = TextAlignmentOptions.Left;
            itemLabelTmp.overflowMode       = TextOverflowModes.Overflow;
            itemLabelTmp.enableWordWrapping = false;
            itemLabelTmp.text               = "Option";
            Comp<LocalizedText>(itemLabelGo);  // font-only
            Comp<UITextStyle>(itemLabelGo).ApplyStyle();

            templateGo.SetActive(false);

            var dropdown = Comp<TMP_Dropdown>(dropGo);
            dropdown.targetGraphic = dropBg;
            dropdown.template      = templateRt;
            dropdown.captionText   = captionTmp;
            dropdown.itemText      = itemLabelTmp;

            return dropdown;
        }

        // Nav tab button — icon above label; highlight targets the icon Image (color-tinted)
        static GameObject BtnNavTab(GameObject parent, string name, Color color, string label, string labelStringId = null)
        {
            var go = Child(parent, name);
            var rt = RT(go);
            rt.sizeDelta = new Vector2(360f, 160f);
            var le = Comp<LayoutElement>(go);
            le.flexibleWidth = 1;
            le.preferredHeight = 160;

            PixelShadow(go);

            var visualGo = Child(go, "Visual");
            Stretch(visualGo);
            var bgImg = Img(visualGo, color);

            if (!go.TryGetComponent<Button>(out var btn)) btn = Comp<Button>(go);
            btn.targetGraphic = bgImg;
            Comp<UIButtonAnimator>(go);

            var iconGo = Child(visualGo, "Icon");
            Fixed(iconGo, new Vector2(0, 25), new Vector2(80, 80));
            var iconImg = Img(iconGo, UI_TEXT);
            iconImg.preserveAspect = true;

            TMP(visualGo, "Label", Center(0, -42, 240, 50), 22, UI_TEXT, label, labelStringId, TextCategory.Normal);
            return go;
        }

        static GameObject ItemToggleRow(GameObject parent, string rowName, Vector2 pos, string label, string labelStringId = null)
        {
            var row = Child(parent, rowName);
            Fixed(row, pos, new Vector2(128, 128));

            var le = Comp<LayoutElement>(row);
            le.preferredWidth = le.minWidth   = 128f;
            le.preferredHeight = le.minHeight = 128f;

            var iconImg = Img(row, Color.white);
            iconImg.preserveAspect = true;

            if (!row.TryGetComponent<Toggle>(out var toggle)) toggle = Comp<Toggle>(row);
            toggle.targetGraphic = iconImg;
            toggle.isOn = false;

            var chk = Child(row, "Checkmark");
            Fixed(chk, Vector2.zero, new Vector2(128, 128));
            var chkImg = Img(chk, new Color(1f, 0.92f, 0.3f, 0.55f));
            toggle.graphic = chkImg;

            // State indicator — bottom-right corner; sprite swapped by StageInfoPopupView.OnExtraTurnsToggled
            var stateGo = Child(row, "StateIndicator");
            var stateRt = RT(stateGo);
            stateRt.anchorMin = new Vector2(1f, 0f);
            stateRt.anchorMax = new Vector2(1f, 0f);
            stateRt.pivot = new Vector2(1f, 0f);
            stateRt.anchoredPosition = Vector2.zero;
            stateRt.sizeDelta = new Vector2(44f, 44f);
            var stateImg = Img(stateGo, Color.white);
            stateImg.preserveAspect = true;

            return row;
        }

        static void Save(GameObject go, string name)
        {
            EnsureDirs();
            string path = $"{BaseCommonPath}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            if (_prefabWasLoaded) PrefabUtility.UnloadPrefabContents(go);
            else Object.DestroyImmediate(go);
            _prefabWasLoaded = false;
            CreateCommonVariantIfMissing(name);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIEditorSetup] Saved Base Popup → {path}");
        }

        private static (GameObject root, bool wasLoaded) LoadOrCreateCanvas(string sceneName)
        {
            MkDir($"{BaseScenesPath}/{sceneName}");
            string path = $"{BaseScenesPath}/{sceneName}/{sceneName}Canvas_Base.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                var loaded = PrefabUtility.LoadPrefabContents(path);
                ApplyCanvasScaler(loaded);
                return (loaded, true);
            }
            return (CreateTempCanvas("Canvas_Scene"), false);
        }

        private static void SaveScenePrefab(GameObject root, string sceneName, bool wasLoaded)
        {
            string path = $"{BaseScenesPath}/{sceneName}/{sceneName}Canvas_Base.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            if (wasLoaded) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
            CreateSceneVariantIfMissing(sceneName);
            AssetDatabase.SaveAssets();
            Debug.Log($"[UIEditorSetup] Saved Base Scene Prefab for {sceneName} → {path}");
        }

        private static void CreateCommonVariantIfMissing(string prefabName)
        {
            string basePath = $"{BaseCommonPath}/{prefabName}.prefab";
            string variantPath = $"{PrefabRoot}/{prefabName}.prefab";
            CreateVariantIfMissing(basePath, variantPath);
        }

        private static void CreateSceneVariantIfMissing(string sceneName)
        {
            string basePath = $"{BaseScenesPath}/{sceneName}/{sceneName}Canvas_Base.prefab";
            string variantPath = $"{PrefabFinal}/Scenes/{sceneName}/{sceneName}Canvas_Base.prefab";
            CreateVariantIfMissing(basePath, variantPath);
        }

        private static void CreateVariantIfMissing(string basePath, string variantPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(variantPath) != null)
            {
                Debug.Log($"[UIEditorSetup] Skipped existing Final Variant -> {variantPath}");
                return;
            }

            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null)
            {
                Debug.LogWarning($"[UIEditorSetup] Base prefab not found for Final Variant -> {basePath}");
                return;
            }

            MkDir(GetAssetDirectory(variantPath));
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            if (instance == null)
            {
                Debug.LogWarning($"[UIEditorSetup] Failed to instantiate Base prefab for Final Variant -> {basePath}");
                return;
            }

            try
            {
                PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }

            Debug.Log($"[UIEditorSetup] Created Final Variant -> {variantPath}");
        }

        private static string GetAssetDirectory(string assetPath)
        {
            int slashIndex = assetPath.LastIndexOf('/');
            return slashIndex > 0 ? assetPath.Substring(0, slashIndex) : "Assets";
        }

        // Enforces canonical CanvasScaler settings. Called on both new and loaded canvases.
        static void ApplyCanvasScaler(GameObject go)
        {
            var scaler = Comp<CanvasScaler>(go);
            scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution    = new Vector2(1080, 1920);
            scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight     = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        static GameObject CreateTempCanvas(string canvasName)
        {
            var go = new GameObject(canvasName);
            var canvas = Comp<Canvas>(go);
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            ApplyCanvasScaler(go);
            Comp<GraphicRaycaster>(go);
            return go;
        }

        static void EnsureDirs()
        {
            foreach (var path in new[] { PrefabRoot, PrefabBase, BaseCommonPath, BaseScenesPath, PrefabFinal, PrefabsGamePath })
                MkDir(path);
        }

        static void MkDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            var cur   = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out Color c); return c; }

        private static Dictionary<string, string> LoadDynamicResourceMap()
        {
            var map = new Dictionary<string, string>();
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            var csvPath = Path.Combine(repoRoot, "shared/datas/common/dynamic_resource.csv");
            if (!File.Exists(csvPath))
            {
                Debug.LogWarning($"[UIEditorSetup] dynamic_resource.csv not found at: {csvPath}");
                return map;
            }

            var lines = File.ReadAllLines(csvPath);
            for (int i = 4; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                var cols = line.Split(',');
                if (cols.Length >= 2)
                {
                    string key = cols[0].Trim();
                    string path = cols[1].Trim();
                    map[key] = path;
                }
            }
            return map;
        }

        private static void AssignItemIcon(ItemSlotView slot, string key, Dictionary<string, string> resMap)
        {
            if (slot == null || !resMap.TryGetValue(key, out string spritePath)) return;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) return;

            var soSlot = new SerializedObject(slot);
            var iconProp = soSlot.FindProperty("_icon");
            if (iconProp != null && iconProp.objectReferenceValue != null)
            {
                var iconImg = iconProp.objectReferenceValue as Image;
                if (iconImg != null)
                {
                    iconImg.sprite = sprite;
                    iconImg.preserveAspect = true;
                    EditorUtility.SetDirty(iconImg);
                }
            }
        }

        private static void TryMapSprite(SerializedObject so, string propertyName, string key, Dictionary<string, string> resMap)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null) return;
            if (string.IsNullOrEmpty(key))
            {
                prop.objectReferenceValue = null;
                return;
            }
            if (resMap.TryGetValue(key, out string path))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                prop.objectReferenceValue = sprite;
            }
        }

        private static void TryMapImageSprite(SerializedObject so, string propertyName, string key, Dictionary<string, string> resMap)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null && prop.objectReferenceValue != null && resMap.TryGetValue(key, out string path))
            {
                var img = prop.objectReferenceValue as Image;
                if (img != null)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    img.sprite = sprite;
                    img.preserveAspect = true;
                    EditorUtility.SetDirty(img);
                }
            }
        }

        [MenuItem("Tools/UI Setup/Inspect Sprites", false, 149)]
        public static void InspectSprites()
        {
            var resMap = LoadDynamicResourceMap();
            string[] keys = { "socket_default", "socket_0", "cell_0", "cell_basic" };
            foreach (var key in keys)
            {
                if (resMap.TryGetValue(key, out string path))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                    {
                        Debug.Log($"[Inspect] Key: {key}, Path: {path}, Name: {sprite.name}, Rect: {sprite.rect}, PPU: {sprite.pixelsPerUnit}, Bounds Size: {sprite.bounds.size}");
                    }
                    else
                    {
                        Debug.LogWarning($"[Inspect] Key: {key}, Path: {path} - Sprite is NULL!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Inspect] Key: {key} not found in resMap");
                }
            }
        }



        private static void MapHierarchyImageSprite(GameObject prefab, string childPath, string key, Dictionary<string, string> resMap)
        {
            if (prefab == null) return;
            var child = prefab.transform.Find(childPath);
            if (child == null) return;
            if (!resMap.TryGetValue(key, out string spritePath)) return;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) return;
            var img = child.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sprite;
            img.preserveAspect = true;
            EditorUtility.SetDirty(prefab);
        }

        private static void MapStarAndIconSprites(Dictionary<string, string> resMap)
        {
            // Star rating removed — only the stage-node lock icon needs remapping now.
            string[] stageNodePaths = {
                "Assets/Resources/Prefabs/UI/StageNodeView.prefab",
                "Assets/UI/Prefabs/Base/Common/StageNodeView.prefab"
            };

            foreach (var path in stageNodePaths)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                MapHierarchyImageSprite(p, "LockOverlay/LockIcon", "ui_lock_icon", resMap);
            }
        }

        private static void BuildRankingItemHierarchy(GameObject itemGo, Dictionary<string, string> resMap)
        {
            // RankText
            TMP(itemGo, "RankText", Center(-320, 0, 100, 60), 20, UI_CTA, "#1", null, TextCategory.Normal);

            // AvatarIcon: between RankText and NameText
            var avatarIcon = Child(itemGo, "AvatarIcon");
            Fixed(avatarIcon, new Vector2(-210, 0), new Vector2(64, 64));
            var avatarImg = Img(avatarIcon, Color.white);
            avatarImg.preserveAspect = true;
            if (resMap.TryGetValue("ui_avatar_default", out string avp))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(avp);
                if (spr != null) avatarImg.sprite = spr;
            }

            // NameText
            TMP(itemGo, "NameText", Center(-20, 0, 280, 60), 20, UI_TEXT, "Player Name", null, TextCategory.Normal);

            // ScoreIcon: immediately to the left of ScoreText
            var scoreIcon = Child(itemGo, "ScoreIcon");
            Fixed(scoreIcon, new Vector2(200, 0), new Vector2(48, 48));
            var scoreImg = Img(scoreIcon, Color.white);
            scoreImg.preserveAspect = true;
            if (resMap.TryGetValue("nav_home", out string sfp))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(sfp);
                if (spr != null) scoreImg.sprite = spr;
            }

            // ScoreText
            TMP(itemGo, "ScoreText", Center(300, 0, 120, 60), 20, UI_CTA, "100", null, TextCategory.Normal);
        }

        private static void CreateRankingItemPrefab()
        {
            string _ripPath = $"{BaseCommonPath}/RankingItemPrefab.prefab";
            _prefabWasLoaded = AssetDatabase.LoadAssetAtPath<GameObject>(_ripPath) != null;
            var root = _prefabWasLoaded ? PrefabUtility.LoadPrefabContents(_ripPath) : new GameObject("RankingItemPrefab");
            var rt = RT(root);
            rt.sizeDelta = new Vector2(820, 90);

            // Set anchors and pivot to top-center to align with VirtualizedScrollRect content mapping
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            Img(root, UI_BG_MID);

            var resMap = LoadDynamicResourceMap();
            BuildRankingItemHierarchy(root, resMap);
            Comp<RankingItemView>(root);

            Save(root, "RankingItemPrefab");
        }

        static void CreateAccountRestartPopup()
        {
            var root = FullScreen("AccountRestartPopupView");
            Img(root, DIM); Comp<AccountRestartPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(700, 500), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Game Restart Required", PopupAccountRestartTitle);

            var body = TMP(panel, "BodyText", Center(0, 50, 580, 150), 20, UI_TEXT, "The game will now restart.", PopupAccountRestartBody, TextCategory.Normal);
            body.enableWordWrapping = true;
            var bodyCsf = Comp<ContentSizeFitter>(body.gameObject);
            bodyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var confirmBtn = Btn(panel, "ConfirmButton", new Vector2(0, -140), new Vector2(500, 96), UI_DANGER, "Restart", PopupAccountRestartConfirm);

            var so = new SerializedObject(root.GetComponent<AccountRestartPopupView>());
            so.FindProperty("_titleText").objectReferenceValue   = title;
            so.FindProperty("_bodyText").objectReferenceValue    = body;
            so.FindProperty("_confirmButton").objectReferenceValue = confirmBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "AccountRestartPopupView");
        }

        static void CreateAccountConflictPopup()
        {
            var root = FullScreen("AccountConflictPopupView");
            Comp<AccountConflictPopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), DIM, "", shadowAlpha: 200f / 255f);
            Stretch(backdrop);

            var panel = Panel(root, "Panel", new Vector2(900, 1100), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Account Data Conflict", PopupAccountConflictTitle);

            var body = TMP(panel, "BodyText", Center(0, 370, 800, 100), 18, UI_TEXT, "Choose which data to keep.", PopupAccountConflictBody, TextCategory.Normal);
            body.enableWordWrapping = true;

            // Local save panel (left)
            var localPanel = Panel(panel, "LocalPanel", new Vector2(380, 550), UI_BG_DEEP);
            Fixed(localPanel.transform.parent.gameObject, new Vector2(-215, 80), new Vector2(380 + 16, 550 + 16));

            var localLabel      = TMP(localPanel, "LocalLabel",      Center(0, 240, 340, 60),  22, UI_TEXT, "Current Data",  PopupAccountConflictLocalLabel, TextCategory.Header);
            var localStageText  = TMP(localPanel, "LocalStageText",  Center(0, 150, 340, 55),  18, UI_TEXT, "Stage 0",       PopupAccountConflictStageFmt,   TextCategory.Normal);
            var localGoldText   = TMP(localPanel, "LocalGoldText",   Center(0,  80, 340, 55),  18, UI_TEXT, "Gold: 0",       PopupAccountConflictGoldFmt,    TextCategory.Normal);
            var localClearedText  = TMP(localPanel, "LocalClearedText", Center(0,  10, 340, 55),  18, UI_TEXT, "Cleared: 0",   PopupAccountConflictClearedFmt, TextCategory.Normal);
            var localItemsText  = TMP(localPanel, "LocalItemsText",  Center(0, -60, 340, 55),  18, UI_TEXT, "Items: 0",      PopupAccountConflictItemsFmt,   TextCategory.Normal);
            var keepLocalBtn    = Btn(localPanel, "KeepLocalButton", new Vector2(0, -190), new Vector2(340, 75), UI_PRIMARY, "Keep Current", PopupAccountConflictBtnKeepLocal);

            // Cloud save panel (right)
            var cloudPanel = Panel(panel, "CloudPanel", new Vector2(380, 550), UI_BG_DEEP);
            Fixed(cloudPanel.transform.parent.gameObject, new Vector2(215, 80), new Vector2(380 + 16, 550 + 16));

            var cloudLabel      = TMP(cloudPanel, "CloudLabel",      Center(0, 240, 340, 60),  22, UI_TEXT, "Google Account Data", PopupAccountConflictCloudLabel,        TextCategory.Header);
            var cloudStageText  = TMP(cloudPanel, "CloudStageText",  Center(0, 150, 340, 55),  18, UI_TEXT, "Stage 0",             PopupAccountConflictStageFmt,          TextCategory.Normal);
            var cloudGoldText   = TMP(cloudPanel, "CloudGoldText",   Center(0,  80, 340, 55),  18, UI_TEXT, "Gold: 0",             PopupAccountConflictGoldFmt,           TextCategory.Normal);
            var cloudClearedText  = TMP(cloudPanel, "CloudClearedText", Center(0,  10, 340, 55),  18, UI_TEXT, "Cleared: 0",          PopupAccountConflictClearedFmt,        TextCategory.Normal);
            var cloudItemsText  = TMP(cloudPanel, "CloudItemsText",  Center(0, -60, 340, 55),  18, UI_TEXT, "Items: 0",            PopupAccountConflictItemsFmt,          TextCategory.Normal);
            var keepCloudBtn    = Btn(cloudPanel, "KeepCloudButton", new Vector2(0, -190), new Vector2(340, 75), UI_PRIMARY, "Use Google Data", PopupAccountConflictBtnKeepCloud);

            var cancelBtn = Btn(panel, "CancelButton", new Vector2(0, -470), new Vector2(300, 70), UI_BG_DEEP, "Cancel", CommonBtnCancel);
            var closeBtn = CloseBtnAt(panel, new Vector2(395, 510));

            var so = new SerializedObject(root.GetComponent<AccountConflictPopupView>());
            so.FindProperty("_titleText").objectReferenceValue      = title;
            so.FindProperty("_bodyText").objectReferenceValue       = body;
            so.FindProperty("_localLabel").objectReferenceValue     = localLabel;
            so.FindProperty("_localStageText").objectReferenceValue = localStageText;
            so.FindProperty("_localGoldText").objectReferenceValue  = localGoldText;
            so.FindProperty("_localClearedText").objectReferenceValue = localClearedText;
            so.FindProperty("_localItemsText").objectReferenceValue = localItemsText;
            so.FindProperty("_keepLocalButton").objectReferenceValue = keepLocalBtn.GetComponent<Button>();
            so.FindProperty("_cloudLabel").objectReferenceValue     = cloudLabel;
            so.FindProperty("_cloudStageText").objectReferenceValue = cloudStageText;
            so.FindProperty("_cloudGoldText").objectReferenceValue  = cloudGoldText;
            so.FindProperty("_cloudClearedText").objectReferenceValue = cloudClearedText;
            so.FindProperty("_cloudItemsText").objectReferenceValue = cloudItemsText;
            so.FindProperty("_keepCloudButton").objectReferenceValue = keepCloudBtn.GetComponent<Button>();
            so.FindProperty("_cancelButton").objectReferenceValue   = cancelBtn.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue = backdrop.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue    = closeBtn;
            so.ApplyModifiedProperties();

            Save(root, "AccountConflictPopupView");
        }

        static void CreateLobbyBadgeItem()
        {
            string _lbiPath = $"{BaseCommonPath}/LobbyBadgeItem.prefab";
            _prefabWasLoaded = AssetDatabase.LoadAssetAtPath<GameObject>(_lbiPath) != null;
            var go = _prefabWasLoaded ? PrefabUtility.LoadPrefabContents(_lbiPath) : new GameObject("LobbyBadgeItem", typeof(RectTransform));
            var rt = RT(go);
            rt.sizeDelta = new Vector2(140f, 140f);

            var le = Comp<LayoutElement>(go);
            le.minWidth = le.preferredWidth = 140f;
            le.minHeight = le.preferredHeight = 140f;

            // Background Circle Image
            var bgImg = Img(go, UI_BG_MID);
            Comp<Button>(go);
            Comp<UIButtonAnimator>(go);
            var comp = Comp<LobbyBadgeItem>(go);

            // Icon Child
            var iconGo = Child(go, "Icon");
            Fixed(iconGo, Vector2.zero, new Vector2(80f, 80f));
            var iconImg = Img(iconGo, Color.white);

            // Label Child
            var labelGo = Child(go, "Label");
            Fixed(labelGo, new Vector2(0f, -45f), new Vector2(120f, 30f));
            var labelTxt = TMP(labelGo, "Text", Center(0, 0, 120, 30), 16, UI_TEXT, "BADGE", null, TextCategory.Normal);
            labelTxt.alignment = TextAlignmentOptions.Center;

            // Wire serialized fields
            var so = new SerializedObject(comp);
            so.FindProperty("_iconImage").objectReferenceValue = iconImg;
            so.FindProperty("_labelXml").objectReferenceValue = labelTxt;
            so.FindProperty("_clickButton").objectReferenceValue = go.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(go, "LobbyBadgeItem");
        }

        static void CreateSpace(GameObject parent, string name, float height)
        {
            var go = Child(parent, name);
            var rt = RT(go);
            rt.sizeDelta = new Vector2(0, height);
            var le = Comp<LayoutElement>(go);
            le.minHeight = le.preferredHeight = height;
        }

        static void CreateShopCategoryHeader()
        {
            string prefabPath = $"{BaseCommonPath}/ShopCategoryHeader.prefab";
            bool loaded = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
            _prefabWasLoaded = loaded;
            var root = loaded ? PrefabUtility.LoadPrefabContents(prefabPath) : new GameObject("ShopCategoryHeader");

            RT(root).sizeDelta = new Vector2(940, 44);
            var rootLe = Comp<LayoutElement>(root);
            rootLe.minHeight = rootLe.preferredHeight = 44f;
            rootLe.minWidth  = rootLe.preferredWidth  = 940f;

            // Shadow underlay — excluded from HLG flow via ignoreLayout
            var shadow = PixelShadow(root, Hex("0D0512"));
            var shadowLe = Comp<LayoutElement>(shadow);
            shadowLe.ignoreLayout = true;

            // HorizontalLayoutGroup directly on root
            // (Shadow uses ignoreLayout so HLG only sees dividers + label)
            var hlg = Comp<HorizontalLayoutGroup>(root);
            hlg.spacing              = 12f;
            hlg.padding              = new RectOffset(8, 8, 0, 0);
            hlg.childAlignment       = TextAnchor.MiddleCenter;
            hlg.childControlWidth    = true;
            hlg.childControlHeight   = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            // Left divider — UI_BORDER at 35% alpha
            var leftDiv = Child(root, "LeftDivider");
            Img(leftDiv, new Color(UI_BORDER.r, UI_BORDER.g, UI_BORDER.b, 0.35f));
            var leftLe = Comp<LayoutElement>(leftDiv);
            leftLe.flexibleWidth   = 1f;
            leftLe.preferredHeight = 2f;
            leftLe.minHeight       = 2f;

            // Category label — UI_CTA amber, Bold 14 + LocalizedText (stringId set at runtime)
            var labelGo = Child(root, "Label");
            var labelLe = Comp<LayoutElement>(labelGo);
            labelLe.flexibleWidth = 0f;
            var labelTmp = Comp<TextMeshProUGUI>(labelGo);
            labelTmp.text              = "CATEGORY";
            ApplyAutoFontSize(labelTmp, TextCategory.Normal);
            labelTmp.fontStyle         = FontStyles.Bold;
            labelTmp.color             = UI_CTA;
            labelTmp.alignment         = TextAlignmentOptions.Center;
            labelTmp.enableWordWrapping = false;
            Comp<LocalizedText>(labelGo);
            Comp<UITextStyle>(labelGo).ApplyStyle();

            // Right divider — mirrors left
            var rightDiv = Child(root, "RightDivider");
            Img(rightDiv, new Color(UI_BORDER.r, UI_BORDER.g, UI_BORDER.b, 0.35f));
            var rightLe = Comp<LayoutElement>(rightDiv);
            rightLe.flexibleWidth   = 1f;
            rightLe.preferredHeight = 2f;
            rightLe.minHeight       = 2f;

            Save(root, "ShopCategoryHeader");
        }

        static void CreateCategoryRibbon(GameObject parent, string ribbonName, Color bgCol, string textStr)
        {
            var ribbon = Child(parent, ribbonName);
            var rt = RT(ribbon);
            rt.sizeDelta = new Vector2(940, 56);
            
            // Layout Element to play nicely with VerticalLayoutGroup
            var le = Comp<LayoutElement>(ribbon);
            le.minHeight = le.preferredHeight = 56;
            le.minWidth = le.preferredWidth = 940;

            PixelShadow(ribbon, Hex("0D0512"));

            // Ribbon Body Image
            var mainBody = Child(ribbon, "Visual");
            Fixed(mainBody, Vector2.zero, new Vector2(940, 56));
            var img = Img(mainBody, bgCol);
            
            // Thin border outline (2px for sleek outline)
            var border = Child(mainBody, "Border");
            Stretch(border);
            var borderImg = Img(border, UI_BORDER);
            border.transform.SetAsFirstSibling();
            borderImg.rectTransform.offsetMin = new Vector2(-2, -2);
            borderImg.rectTransform.offsetMax = new Vector2(2, 2);

            // Ribbon Text (Choose text color based on ribbon color for optimal readability)
            bool isYellow = bgCol == UI_CTA;
            Color textCol = isYellow ? Hex("1A0522") : UI_TEXT;
            var txt = TMP(mainBody, "Text", Center(0, 0, 900, 56), 22, textCol, textStr, null, TextCategory.Header);
            txt.alignment = TextAlignmentOptions.Center;
        }

        static void CreateProductCardPrefab()
        {
            string cellPrefabPath = $"{BaseCommonPath}/IAPProductCard.prefab";
            bool loaded = AssetDatabase.LoadAssetAtPath<GameObject>(cellPrefabPath) != null;
            var card = loaded ? PrefabUtility.LoadPrefabContents(cellPrefabPath) : new GameObject("IAPProductCard");
            var rt = RT(card);
            rt.sizeDelta = new Vector2(940, 280);

            // Add LayoutElement to play nicely with VerticalLayoutGroup and guarantee spacing
            var le = Comp<LayoutElement>(card);
            le.minHeight = le.preferredHeight = 280;
            le.minWidth = le.preferredWidth = 940;

            // 1. Card Shadow (pixel art convention — stretch, offset right+8 bottom+8)
            PixelShadow(card, Hex("0C0712"));

            // 2. Card Visual (Body container)
            var visual = Child(card, "Visual");
            Stretch(visual);
            var bgImg = Img(visual, Color.white);
            
            // Clean Outline border (2px thickness)
            var border = Child(visual, "Border");
            Stretch(border);
            var borderImg = Img(border, Hex("DFD5E6")); // Sleek slate white outline
            border.transform.SetAsFirstSibling();
            borderImg.rectTransform.offsetMin = new Vector2(-2, -2);
            borderImg.rectTransform.offsetMax = new Vector2(2, 2);

            // Left-Center: Product Icon (centered vertically above PriceButton without overlap)
            var iconGo = Child(visual, "Icon");
            Fixed(iconGo, new Vector2(-320, 10), new Vector2(140, 140));
            var iconImg = Img(iconGo, Color.white);
            iconImg.preserveAspect = true;

            // Left-Bottom: Price Button (X=-320, Y=-85, size=200x56)
            var priceBtn = Btn(visual, "PriceButton", new Vector2(-320, -85), new Vector2(200, 56), UI_SUCCESS, "$0.00", null);
            var priceTxt = priceBtn.transform.Find("Visual/Label").GetComponent<TextMeshProUGUI>();
            ApplyAutoFontSize(priceTxt, TextCategory.Normal);
            priceTxt.fontStyle = FontStyles.Bold;
            priceTxt.color = UI_TEXT;

            // Right Area: Title & Description Info (아이콘 우측 공간)
            var titleGo = Child(visual, "TitleText");
            Fixed(titleGo, new Vector2(120, 60), new Vector2(600, 60));
            var titleText = Comp<TextMeshProUGUI>(titleGo);
            titleText.text = "Product Title";
            ApplyAutoFontSize(titleText, TextCategory.Header);
            titleText.color = Hex("FFDF00"); // High-readability gold/yellow for title
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Left;
            Comp<LocalizedText>(titleGo);
            Comp<UITextStyle>(titleGo).ApplyStyle();

            var descGo = Child(visual, "DescText");
            Fixed(descGo, new Vector2(120, -40), new Vector2(600, 120));
            var descText = Comp<TextMeshProUGUI>(descGo);
            descText.text = "Product description goes here. Highly readable text style.";
            ApplyAutoFontSize(descText, TextCategory.Normal);
            descText.color = UI_TEXT; // Crisp white for description
            descText.alignment = TextAlignmentOptions.Left;
            descText.enableWordWrapping = true;
            descText.lineSpacing = 10;
            Comp<LocalizedText>(descGo);
            Comp<UITextStyle>(descGo).ApplyStyle();

            // Corner Decor Tag (Top-Left corner ribbon overlap - size=220x48 & Y=-8 shadowed & left-aligned!)
            var tagShadow = Child(visual, "TagRibbonShadow");
            Fixed(tagShadow, new Vector2(-360, 102), new Vector2(220, 48));
            Img(tagShadow, Hex("0D0512")); // Shadow at Y=-8 offset relative to ribbon Y=110

            var tagGo = Child(visual, "TagRibbon");
            Fixed(tagGo, new Vector2(-360, 110), new Vector2(220, 48));
            var tagImg = Img(tagGo, UI_CTA); // Amber yellow
            
            var tagBorder = Child(tagGo, "Border");
            Stretch(tagBorder);
            var tbImg = Img(tagBorder, UI_BORDER);
            tagBorder.transform.SetAsFirstSibling();
            tbImg.rectTransform.offsetMin = new Vector2(-2, -2);
            tbImg.rectTransform.offsetMax = new Vector2(2, 2);

            var tagTxtGo = Child(tagGo, "TagText");
            Fixed(tagTxtGo, new Vector2(15, 0), new Vector2(190, 42));
            var tagTxt = Comp<TextMeshProUGUI>(tagTxtGo);
            ApplyAutoFontSize(tagTxt, TextCategory.Normal);
            tagTxt.color = Hex("1E0F27");
            tagTxt.text = "TAG";
            tagTxt.fontStyle = FontStyles.Bold;
            tagTxt.alignment = TextAlignmentOptions.Left;
            tagTxt.enableWordWrapping = false;
            Comp<LocalizedText>(tagTxtGo);
            Comp<UITextStyle>(tagTxtGo).ApplyStyle();

            // LimitText configured dynamically at runtime
            var limitGo = Child(visual, "LimitText");
            Fixed(limitGo, new Vector2(200, -85), new Vector2(300, 56));
            var limitText = Comp<TextMeshProUGUI>(limitGo);
            limitText.text = "";
            ApplyAutoFontSize(limitText, TextCategory.Normal);
            limitText.color = Hex("DFD5E6"); // Slate white/gray
            limitText.alignment = TextAlignmentOptions.Right;
            Comp<LocalizedText>(limitGo);
            Comp<UITextStyle>(limitGo).ApplyStyle();

            _prefabWasLoaded = loaded;
            Save(card, "IAPProductCard");
        }

        static void CreateItemTooltip()
        {
            // Panel content size: 340×210 (inner). Border adds 24px → outer = 364×234.
            var root = FullScreen("ItemTooltipView");
            Comp<ItemTooltipView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            // Full-screen transparent backdrop — click anywhere outside panel to close
            var backdrop = Btn(root, "Backdrop", Vector2.zero, new Vector2(1080, 1920), new Color(0f, 0f, 0f, 0f), "", shadowAlpha: 0f);
            Stretch(backdrop);

            // Outer border — this RectTransform is repositioned at runtime by ItemTooltipView
            var borderGo = Child(root, "TooltipPanel");
            Fixed(borderGo, Vector2.zero, new Vector2(364f, 234f));
            Img(borderGo, Hex("2B003B"));

            // Inner panel
            var inner = Child(borderGo, "InnerPanel");
            Stretch(inner);
            var innerRt = RT(inner);
            innerRt.offsetMin = new Vector2(12f, 12f);
            innerRt.offsetMax = new Vector2(-12f, -12f);
            Img(inner, UI_BG_MID);

            // Icon — left side, 96×96
            var iconGo = Child(inner, "Icon");
            Fixed(iconGo, new Vector2(-107f, 10f), new Vector2(96f, 96f));
            var iconImg = Img(iconGo, Color.white);
            iconImg.preserveAspect = true;

            // Title — right of icon, pixel-art pixel style
            var titleTmp = TMP(inner, "Title", Center(38f, 55f, 185f, 44f), 32, UI_TEXT, "Item Name", null, TextCategory.Normal);
            titleTmp.enableWordWrapping = false;
            titleTmp.alignment = TextAlignmentOptions.Left;

            // Desc — full width, below icon and title
            var descTmp = TMP(inner, "Desc", Center(0f, -45f, 310f, 80f), 28, new Color(0.82f, 0.78f, 0.88f, 1f), "Description", null, TextCategory.Normal);
            descTmp.enableWordWrapping = true;
            descTmp.alignment = TextAlignmentOptions.Center;

            var so = new SerializedObject(root.GetComponent<ItemTooltipView>());
            so.FindProperty("_icon").objectReferenceValue          = iconImg;
            so.FindProperty("_titleText").objectReferenceValue     = titleTmp;
            so.FindProperty("_descText").objectReferenceValue      = descTmp;
            so.FindProperty("_panel").objectReferenceValue         = RT(borderGo);
            so.FindProperty("_backdropButton").objectReferenceValue = backdrop.GetComponent<Button>();
            so.ApplyModifiedProperties();

            Save(root, "ItemTooltipView");
        }

        static GameObject InstantiateProductCard(GameObject parent, string cardName, Color bgCol, 
            string titleStr, string titleStringId, 
            string descStr, string descStringId, 
            string iconKey, 
            string tagStr, string tagStringId, 
            Dictionary<string, string> resMap)
        {
            string cellPrefabPath = $"{BaseCommonPath}/IAPProductCard.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(cellPrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            go.name = cardName;
            
            // Set dynamic properties on instantiated card
            var visual = go.transform.Find("Visual").gameObject;
            Img(visual, bgCol);
            
            var iconImg = go.transform.Find("Visual/Icon").GetComponent<Image>();
            if (resMap.TryGetValue(iconKey, out string path))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (spr != null) { iconImg.sprite = spr; iconImg.preserveAspect = true; }
            }
            
            var title = go.transform.Find("Visual/TitleText").GetComponent<TextMeshProUGUI>();
            title.text = titleStr;
            var titleLt = title.GetComponent<LocalizedText>();
            if (titleLt != null && !string.IsNullOrEmpty(titleStringId))
            {
                var soLt = new SerializedObject(titleLt);
                soLt.FindProperty("_stringId").stringValue = titleStringId;
                soLt.ApplyModifiedProperties();
            }
            var titleStyle = title.GetComponent<UITextStyle>();
            if (titleStyle != null) titleStyle.ApplyStyle();
            
            var desc = go.transform.Find("Visual/DescText").GetComponent<TextMeshProUGUI>();
            desc.text = descStr;
            var descLt = desc.GetComponent<LocalizedText>();
            if (descLt != null && !string.IsNullOrEmpty(descStringId))
            {
                var soLt = new SerializedObject(descLt);
                soLt.FindProperty("_stringId").stringValue = descStringId;
                soLt.ApplyModifiedProperties();
            }
            var descStyle = desc.GetComponent<UITextStyle>();
            if (descStyle != null) descStyle.ApplyStyle();
            
            var tagGo = go.transform.Find("Visual/TagRibbon").gameObject;
            var tagShadow = go.transform.Find("Visual/TagRibbonShadow").gameObject;
            if (!string.IsNullOrEmpty(tagStr))
            {
                tagGo.SetActive(true);
                tagShadow.SetActive(true);
                var tagTxt = tagGo.transform.Find("TagText").GetComponent<TextMeshProUGUI>();
                tagTxt.text = tagStr;
                
                var tagLt = tagTxt.GetComponent<LocalizedText>();
                if (tagLt != null && !string.IsNullOrEmpty(tagStringId))
                {
                    var soLt = new SerializedObject(tagLt);
                    soLt.FindProperty("_stringId").stringValue = tagStringId;
                    soLt.ApplyModifiedProperties();
                }
                var tagStyle = tagTxt.GetComponent<UITextStyle>();
                if (tagStyle != null) tagStyle.ApplyStyle();
            }
            else
            {
                tagGo.SetActive(false);
                tagShadow.SetActive(false);
            }
            
            return go;
        }
    }
}
#endif
