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

        [MenuItem("Tools/UI Setup/Prefabs/InGameCanvas",     false, 118)]
        static void CreateInGameCanvasSingle()   { EnsureDirs(); SetupInGame();          AssetDatabase.Refresh(); }

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
            if (resMap.TryGetValue("nav_shop",    out string nsp)) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(nsp); if (s != null) { var i = shopBtn.transform.Find("Visual/Icon")?.GetComponent<Image>(); if (i != null) i.sprite = s; } }
            if (resMap.TryGetValue("nav_home",    out string nhp)) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(nhp); if (s != null) { var i = homeBtn.transform.Find("Visual/Icon")?.GetComponent<Image>(); if (i != null) i.sprite = s; } }
            if (resMap.TryGetValue("nav_ranking", out string nrp)) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(nrp); if (s != null) { var i = rankBtn.transform.Find("Visual/Icon")?.GetComponent<Image>(); if (i != null) i.sprite = s; } }

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
            shopContentRt.sizeDelta = new Vector2(0, 1500); // height room
            shopScroll.content = shopContentRt;

            var shopContentVlg = Comp<VerticalLayoutGroup>(shopContentGo);
            shopContentVlg.childAlignment      = TextAnchor.UpperCenter;
            shopContentVlg.spacing             = 16;
            shopContentVlg.padding             = new RectOffset(40, 40, 40, 40);
            shopContentVlg.childControlWidth   = true;
            shopContentVlg.childControlHeight  = true;  // uses LayoutElement.preferredHeight per child
            shopContentVlg.childForceExpandHeight = false; // spacers define section gaps; don't force-fill

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

            // Wire ShopTabView refs
            var soShop = new SerializedObject(shopView);
            soShop.FindProperty("_contentContainer").objectReferenceValue = shopContentRt;
            soShop.ApplyModifiedProperties();

            var rankingTab = Child(tabContent, "RankingTab"); Stretch(rankingTab); rankingTab.SetActive(false);
            var rankingView = Comp<RankingTabView>(rankingTab);
            var starsTab = Btn(rankingTab, "StarsTabButton", new Vector2(-230, 700), new Vector2(300, 80), UI_PRIMARY, "Stars", LobbyRankingTabStars);
            var maxStageTab = Btn(rankingTab, "MaxStageTabButton", new Vector2(230, 700), new Vector2(300, 80), UI_BG_MID, "Max Stage", LobbyRankingTabMaxStage);
            
            var rankTitle = TMP(rankingTab, "TitleText", Center(0, 580, 760, 70), 30, UI_CTA, "Star Ranking", LobbyRankingStarsTitle, TextCategory.Header);
            var myRank = TMP(rankingTab, "MyRankText", Center(0, 490, 760, 80), 24, UI_TEXT, "My Rank: -", LobbyRankingMyRankEmpty, TextCategory.Normal);
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


            // Wire LobbyView refs
            var soLobby = new SerializedObject(canvas.GetComponent<LobbyView>());
            soLobby.FindProperty("_header").objectReferenceValue      = hv;
            soLobby.FindProperty("_navBar").objectReferenceValue      = bnv;
            soLobby.FindProperty("_homeTabRoot").objectReferenceValue = homeTab;
            soLobby.FindProperty("_shopTabRoot").objectReferenceValue = shopTab;
            soLobby.FindProperty("_rankingTabRoot").objectReferenceValue = rankingTab;
            soLobby.FindProperty("_rankingTabView").objectReferenceValue = rankingView;
            soLobby.ApplyModifiedProperties();

            var soRanking = new SerializedObject(rankingView);
            soRanking.FindProperty("_starsTabButton").objectReferenceValue = starsTab.GetComponent<Button>();
            soRanking.FindProperty("_maxStageTabButton").objectReferenceValue = maxStageTab.GetComponent<Button>();
            soRanking.FindProperty("_titleText").objectReferenceValue = rankTitle;
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

            // Populate Star and Stage icons on RankingTabView using specified resource keys
            string starKey = soRanking.FindProperty("_starResourceKey").stringValue;
            if (string.IsNullOrEmpty(starKey)) starKey = "star_filled";
            string stageKey = soRanking.FindProperty("_stageResourceKey").stringValue;
            if (string.IsNullOrEmpty(stageKey)) stageKey = "nav_home";

            if (resMap.TryGetValue(starKey, out string starPath))
            {
                var starSprite = AssetDatabase.LoadAssetAtPath<Sprite>(starPath);
                soRanking.FindProperty("_starSprite").objectReferenceValue = starSprite;
            }
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
            soNav.FindProperty("_shopHighlight").objectReferenceValue    = shopBtn.transform.Find("Visual/Icon").GetComponent<Image>();
            soNav.FindProperty("_homeHighlight").objectReferenceValue    = homeBtn.transform.Find("Visual/Icon").GetComponent<Image>();
            soNav.FindProperty("_rankingHighlight").objectReferenceValue = rankBtn.transform.Find("Visual/Icon").GetComponent<Image>();
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

        static void SetupInGame()
        {
            var (canvas, _inGameLoaded) = LoadOrCreateCanvas("InGame");
            Comp<UIScreenShake>(canvas);
            var resMapInGame = LoadDynamicResourceMap();

            // HUD — top 240px fixed area (generous for casual game feel)
            var hud = Child(canvas, "HUD");
            TopStrip(hud, 240);
            Img(hud, new Color(0, 0, 0, 0)); // Transparent container
            Comp<HUDView>(hud);

            // Pause button — top-left square button
            var pauseBtn = Btn(hud, "PauseButton", new Vector2(-460, -90), new Vector2(100, 100), UI_PRIMARY, "");
            var pauseIconImg = pauseBtn.transform.Find("Visual/Icon")?.GetComponent<Image>();
            if (pauseIconImg != null && resMapInGame.TryGetValue("ui_pause_icon", out string pip))
            {
                var spr = AssetDatabase.LoadAssetAtPath<Sprite>(pip);
                if (spr != null) { pauseIconImg.sprite = spr; pauseIconImg.preserveAspect = true; }
            }

            // StageInfo — shows stage number; background colored by DifficultyStyle at runtime; SkullBadge for Hard
            var stageInfo = Child(hud, "StageInfo");
            Fixed(stageInfo, new Vector2(-290f, -90f), new Vector2(160f, 80f));
            var stageInfoImg = Img(stageInfo, Color.clear);

            var stageNumTxt = TMP(stageInfo, "StageNumberText", Center(0, 0, 140, 70), 28, UI_TEXT, "1", null, TextCategory.Header);
            stageNumTxt.alignment = TextAlignmentOptions.Center;

            // Skull badge nested inside (Hard only)
            var hudSkull = Child(stageInfo, "SkullBadge");
            Fixed(hudSkull, new Vector2(55f, 10f), new Vector2(30f, 30f));
            var hudSkullImg = Img(hudSkull, Color.white);
            hudSkull.SetActive(false);

            // BoardContainer (anchor for world-space board)
            var board = Child(canvas, "BoardContainer"); Stretch(board);

            // SafeAreaRoot
            var safeRoot = Child(canvas, "SafeAreaRoot");
            Stretch(safeRoot);
            Comp<SafeAreaHandler>(safeRoot);

            SaveScenePrefab(canvas, "InGame", _inGameLoaded);
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
            qtyTmp.enableAutoSizing  = true;
            qtyTmp.fontSizeMin       = 24f;
            qtyTmp.fontSizeMax       = 36f;
            qtyTmp.fontSize          = 30f;
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

            var best  = TMP(panel, "BestRecordText", Center(0, 195, 600, 55), 20, UI_TEXT, "Best: 2 Stars", null, TextCategory.Normal);
            var bestCsf = Comp<ContentSizeFitter>(best.gameObject);
            bestCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 3 star placeholders — empty always visible, Fill child shown per bestStars
            var starsRoot = Child(panel, "Stars"); Fixed(starsRoot, new Vector2(0, 90), new Vector2(450, 130));
            var hlg = Comp<HorizontalLayoutGroup>(starsRoot);
            hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var s0 = StarGO(starsRoot, "Star0", 110f); var s1 = StarGO(starsRoot, "Star1", 110f); var s2 = StarGO(starsRoot, "Star2", 110f);

            var play = Btn(panel, "PlayButton", new Vector2(0, -285), new Vector2(400, 80), UI_CTA, "Play", CommonBtnPlay);

            // Explicit square close button — top-right of panel (convention: all popups must have one)
            var closeBtn = CloseBtnAt(panel, new Vector2(280, 310));

            var so = new SerializedObject(root.GetComponent<StageInfoPopupView>());
            so.FindProperty("_stageTitle").objectReferenceValue       = title;
            so.FindProperty("_bestRecord").objectReferenceValue       = best;
            so.FindProperty("_ribbonImage").objectReferenceValue      = ribbonImg;
            so.FindProperty("_playButton").objectReferenceValue       = play.GetComponent<Button>();
            so.FindProperty("_backdropButton").objectReferenceValue   = closeBtn;
            var starsArr = so.FindProperty("_bestStarFills");
            starsArr.arraySize = 3;
            starsArr.GetArrayElementAtIndex(0).objectReferenceValue = s0.transform.Find("Fill").gameObject;
            starsArr.GetArrayElementAtIndex(1).objectReferenceValue = s1.transform.Find("Fill").gameObject;
            starsArr.GetArrayElementAtIndex(2).objectReferenceValue = s2.transform.Find("Fill").gameObject;

            var resMap = LoadDynamicResourceMap();
            var starEmpty  = resMap.TryGetValue("star_empty",  out string ep) ? AssetDatabase.LoadAssetAtPath<Sprite>(ep)  : null;
            var starFilled = resMap.TryGetValue("star_filled", out string fp) ? AssetDatabase.LoadAssetAtPath<Sprite>(fp) : null;
            so.ApplyModifiedProperties();

            foreach (var star in new[] { s0, s1, s2 })
            {
                if (starEmpty  != null) { star.GetComponent<Image>().sprite = starEmpty; }
                var fillImg = star.transform.Find("Fill")?.GetComponent<Image>();
                if (fillImg != null && starFilled != null) fillImg.sprite = starFilled;
            }

            Save(root, "StageInfoPopupView");
        }

        static void CreateResultOverlay()
        {
            var resMap = LoadDynamicResourceMap();
            var root = FullScreen("ResultOverlayView");
            Img(root, DIM); Comp<ResultOverlayView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(900, 900), UI_BG_MID);
            RibbonTitle(panel, "TitleText", "Stage Clear!", PopupResultTitle);

            var retry = Btn(panel, "RetryButton", new Vector2(-270, -330), new Vector2(230, 90), UI_PRIMARY, "Retry",        CommonBtnRetry);
            var next  = Btn(panel, "NextButton",  new Vector2(   0, -330), new Vector2(230, 90), UI_CTA,     "Next",         CommonBtnNext);
            var map   = Btn(panel, "MapButton",   new Vector2( 270, -330), new Vector2(230, 90), UI_BG_DEEP, "Map",          CommonBtnMap);
            var doubleReward = Btn(panel, "DoubleRewardButton", new Vector2(0, -250), new Vector2(380, 85), UI_PRIMARY, "Double Reward", PopupResultDoubleReward);

            Save(root, "ResultOverlayView");
        }

        static void CreateFailOverlay()
        {
            var root = FullScreen("FailOverlayView");
            Img(root, DIM); Comp<FailOverlayView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            var panel = Panel(root, "Panel", new Vector2(700, 780), UI_BG_MID);
            RibbonTitle(panel, "TitleText", "Just a bit more!", PopupFailTitle);

            var forfBtn = Btn(panel, "ForfeitButton",  new Vector2(0, -285), new Vector2(280, 96), UI_DANGER, "Give Up", PopupFailBtnForfeit);

            Save(root, "FailOverlayView");
        }

        static void CreatePausePopup()
        {
            var root = FullScreen("PausePopupView");
            Img(root, DIM); Comp<PausePopupView>(root); Comp<UIPanelAppear>(root); Comp<CanvasGroup>(root);

            // Backdrop: visual dim only — close is via explicit CloseButton below
            var backdrop = Child(root, "Backdrop");
            Stretch(backdrop);
            Img(backdrop, DIM);

            var panel = Panel(root, "Panel", new Vector2(600, 600), UI_BG_MID);
            var title = RibbonTitle(panel, "TitleText", "Paused", PopupPauseTitle);

            var resume  = Btn(panel, "ResumeButton",      new Vector2(0,  120), new Vector2(480, 96), UI_CTA,     "Resume",       PopupPauseBtnResume);
            var restart = Btn(panel, "RestartButton",     new Vector2(0,   10), new Vector2(480, 96), UI_DANGER,  "Restart",      PopupPauseBtnRestart);
            var settings= Btn(panel, "SettingsButton",    new Vector2(0, -100), new Vector2(480, 96), UI_BG_DEEP, "Settings",     CommonSettings);
            var select  = Btn(panel, "StageSelectButton", new Vector2(0, -210), new Vector2(480, 96), UI_BG_DEEP, "Stage Select", PopupPauseBtnStageSelect);
            var closeBtn = CloseBtnAt(panel, new Vector2(250, 258));

            var so = new SerializedObject(root.GetComponent<PausePopupView>());
            so.FindProperty("_resumeButton").objectReferenceValue      = resume.GetComponent<Button>();
            so.FindProperty("_restartButton").objectReferenceValue     = restart.GetComponent<Button>();
            so.FindProperty("_settingsButton").objectReferenceValue    = settings.GetComponent<Button>();
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

            var panel = Panel(root, "Panel", new Vector2(850, 1200), UI_BG_MID);
            var uidTxt = RibbonTitle(panel, "UserIdText", "Guest", CommonGuest);

            // Tabs setup (y = 505)
            var avatarTabBtn = Btn(panel, "AvatarTabButton", new Vector2(-200, 505), new Vector2(320, 75), UI_PRIMARY, "Avatars", PopupAccountTabAvatars);
            var themeTabBtn = Btn(panel, "BoardThemeTabButton", new Vector2(200, 505), new Vector2(320, 75), UI_BG_MID, "Board Skins", PopupAccountTabBoardSkins);

            // 1. Nickname Input Area (grouped in NicknameArea)
            var nicknameAreaGo = Child(panel, "NicknameArea");
            Fixed(nicknameAreaGo, new Vector2(0, 380), new Vector2(800, 160));
            
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
            textTmp.fontSize = 28f;
            textTmp.color = UI_TEXT;
            textTmp.alignment = TextAlignmentOptions.Left;

            var placeholderGo = Child(textAreaGo, "Placeholder");
            Stretch(placeholderGo);
            var placeholderTmp = Comp<TextMeshProUGUI>(placeholderGo);
            placeholderTmp.fontSize = 28f;
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

            // 2. Avatar & Theme Grid Area
            var gridLabelTxt = TMP(panel, "GridLabelText", Center(0, 220, 750, 50), 20, UI_TEXT, "Choose Avatar", PopupAccountLabelSelectAvatar, TextCategory.Normal);

            var scrollViewGo = Child(panel, "AvatarScrollView");
            Fixed(scrollViewGo, new Vector2(0, -60), new Vector2(750, 420));
            var scrollRect = Comp<ScrollRect>(scrollViewGo);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var viewportGo = Child(scrollViewGo, "Viewport");
            Stretch(viewportGo);
            Comp<RectMask2D>(viewportGo);
            var viewportImg = Comp<Image>(viewportGo);
            viewportImg.color = new Color(0, 0, 0, 0.01f);
            scrollRect.viewport = RT(viewportGo);

            var contentGo = Child(viewportGo, "Content");
            var contentRt = RT(contentGo);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 420);
            scrollRect.content = contentRt;

            var grid = Comp<GridLayoutGroup>(contentGo);
            grid.cellSize = new Vector2(130, 130);
            grid.spacing = new Vector2(12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(15, 15, 15, 15);

            var csf = Comp<ContentSizeFitter>(contentGo);
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Template Slot for Grid
            var templateGo = Child(contentGo, "AvatarSlotTemplate");
            RT(templateGo).sizeDelta = new Vector2(130, 130);
            var slotBg = Comp<Image>(templateGo);
            slotBg.color = UI_BG_DEEP;
            var templateBtn = Comp<Button>(templateGo);
            templateBtn.targetGraphic = slotBg;
            Comp<UIButtonAnimator>(templateGo);

            var visualGo = Child(templateGo, "Visual");
            Stretch(visualGo);

            var iconGo = Child(visualGo, "Icon");
            Fixed(iconGo, Vector2.zero, new Vector2(90, 90));
            var iconImg = Comp<Image>(iconGo);
            iconImg.preserveAspect = true;

            var hlGo = Child(visualGo, "SelectedHighlight");
            Stretch(hlGo);
            var hlImg = Comp<Image>(hlGo);
            hlImg.color = UI_BORDER;
            RT(hlGo).offsetMin = new Vector2(-4, -4);
            RT(hlGo).offsetMax = new Vector2(4, 4);
            hlGo.SetActive(false);

            var lockGo = Child(visualGo, "LockOverlay");
            Stretch(lockGo);
            var lockBg = Comp<Image>(lockGo);
            lockBg.color = new Color(0, 0, 0, 0.6f);

            var lockIconGo = Child(lockGo, "LockIcon");
            Fixed(lockIconGo, new Vector2(0, 15), new Vector2(40, 40));
            var lockIconImg = Comp<Image>(lockIconGo);
            lockIconImg.color = Color.white;
            lockIconImg.preserveAspect = true;

            var costTextGo = Child(lockGo, "CostText");
            Fixed(costTextGo, new Vector2(0, -35), new Vector2(120, 35));
            var costTxt = Comp<TextMeshProUGUI>(costTextGo);
            costTxt.fontSize = 20f;
            costTxt.color = UI_CTA;
            costTxt.alignment = TextAlignmentOptions.Center;
            costTxt.text = "0";

            lockGo.SetActive(false);

            // 3. Platform Account Buttons
            var linkBtn = Btn(panel, "LinkAccountButton",   new Vector2(0, -340), new Vector2(600, 96), UI_CTA,     "Link Account",   PopupAccountBtnLink);
            var swBtn   = Btn(panel, "SwitchAccountButton", new Vector2(0, -340), new Vector2(600, 96), UI_CTA,     "Switch Account", PopupAccountBtnSwitch);
            var closeBtn = CloseBtnAt(panel, new Vector2(377, 552));

            // 4. Map Avatar Sprites
            var resMap = LoadDynamicResourceMap();
            var avatarCsvPath = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "../../..")), "shared/datas/avatar/avatar.csv");
            var avatarMappings = new List<AccountPopupView.AvatarSpriteMapping>();

            if (File.Exists(avatarCsvPath))
            {
                var lines = File.ReadAllLines(avatarCsvPath);
                for (int i = 4; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    var cols = line.Split(',');
                    if (cols.Length >= 2)
                    {
                        if (int.TryParse(cols[0].Trim(), out int avatarId))
                        {
                            string resourceName = cols[1].Trim();
                            Sprite sprite = null;
                            if (resMap.TryGetValue(resourceName, out string spritePath))
                            {
                                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                            }
                            
                            avatarMappings.Add(new AccountPopupView.AvatarSpriteMapping
                            {
                                avatarId = avatarId,
                                resourceName = resourceName,
                                sprite = sprite
                            });
                        }
                    }
                }
            }

            // Map Board Theme Sprites
            var themeCsvPath = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "../../..")), "shared/datas/board_theme/board_theme.csv");
            var themeMappings = new List<AccountPopupView.BoardThemeSpriteMapping>();

            if (File.Exists(themeCsvPath))
            {
                var lines = File.ReadAllLines(themeCsvPath);
                for (int i = 4; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    var cols = line.Split(',');
                    if (cols.Length >= 2)
                    {
                        if (int.TryParse(cols[0].Trim(), out int themeId))
                        {
                            string resourceName = cols[1].Trim();
                            Sprite borderSprite = null;
                            Sprite socketSprite = null;
                            if (resMap.TryGetValue($"{resourceName}_border", out string borderPath))
                            {
                                borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(borderPath);
                            }
                            if (resMap.TryGetValue($"{resourceName}_socket", out string socketPath))
                            {
                                socketSprite = AssetDatabase.LoadAssetAtPath<Sprite>(socketPath);
                            }
                            
                            themeMappings.Add(new AccountPopupView.BoardThemeSpriteMapping
                            {
                                themeId = themeId,
                                resourceName = resourceName,
                                borderSprite = borderSprite,
                                socketSprite = socketSprite
                            });
                        }
                    }
                }
            }

            if (resMap.TryGetValue("ui_lock_icon", out string lockIconPath))
            {
                var lockIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(lockIconPath);
                if (lockIconSprite != null)
                {
                    lockIconImg.sprite = lockIconSprite;
                    EditorUtility.SetDirty(lockIconImg);
                }
            }

            // 5. Serialize Object Wiring
            var so = new SerializedObject(root.GetComponent<AccountPopupView>());
            so.FindProperty("_userIdText").objectReferenceValue          = uidTxt;
            so.FindProperty("_linkAccountButton").objectReferenceValue   = linkBtn.GetComponent<Button>();
            so.FindProperty("_switchAccountButton").objectReferenceValue = swBtn.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue         = closeBtn;

            so.FindProperty("_displayNameInput").objectReferenceValue    = inputField;
            so.FindProperty("_saveNicknameButton").objectReferenceValue   = saveBtn.GetComponent<Button>();
            so.FindProperty("_avatarGridParent").objectReferenceValue     = contentRt;
            so.FindProperty("_avatarSlotTemplate").objectReferenceValue   = templateGo;

            so.FindProperty("_avatarTabButton").objectReferenceValue     = avatarTabBtn.GetComponent<Button>();
            so.FindProperty("_boardThemeTabButton").objectReferenceValue  = themeTabBtn.GetComponent<Button>();
            so.FindProperty("_nicknameArea").objectReferenceValue         = nicknameAreaGo;
            so.FindProperty("_gridLabelText").objectReferenceValue        = gridLabelTxt;

            var avatarSpritesProp = so.FindProperty("_avatarSprites");
            avatarSpritesProp.ClearArray();
            avatarSpritesProp.arraySize = avatarMappings.Count;
            for (int i = 0; i < avatarMappings.Count; i++)
            {
                var elem = avatarSpritesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("avatarId").intValue = avatarMappings[i].avatarId;
                elem.FindPropertyRelative("resourceName").stringValue = avatarMappings[i].resourceName;
                elem.FindPropertyRelative("sprite").objectReferenceValue = avatarMappings[i].sprite;
            }

            var themeSpritesProp = so.FindProperty("_boardThemeSprites");
            themeSpritesProp.ClearArray();
            themeSpritesProp.arraySize = themeMappings.Count;
            for (int i = 0; i < themeMappings.Count; i++)
            {
                var elem = themeSpritesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("themeId").intValue = themeMappings[i].themeId;
                elem.FindPropertyRelative("resourceName").stringValue = themeMappings[i].resourceName;
                elem.FindPropertyRelative("borderSprite").objectReferenceValue = themeMappings[i].borderSprite;
                elem.FindPropertyRelative("socketSprite").objectReferenceValue = themeMappings[i].socketSprite;
            }

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
            
            // Stars container — empty sprite always visible; Fill child shown per earned stars
            var starsRoot = Child(root, "Stars");
            Fixed(starsRoot, new Vector2(0f, -60f), new Vector2(110f, 32f));
            var hlg = Comp<HorizontalLayoutGroup>(starsRoot);
            hlg.spacing = 4; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            var s0 = StarGO(starsRoot, "Star0", 30f);
            var s1 = StarGO(starsRoot, "Star1", 30f);
            var s2 = StarGO(starsRoot, "Star2", 30f);

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

            var starFillsArr = so.FindProperty("_starFills");
            starFillsArr.arraySize = 3;
            starFillsArr.GetArrayElementAtIndex(0).objectReferenceValue = s0.transform.Find("Fill").gameObject;
            starFillsArr.GetArrayElementAtIndex(1).objectReferenceValue = s1.transform.Find("Fill").gameObject;
            starFillsArr.GetArrayElementAtIndex(2).objectReferenceValue = s2.transform.Find("Fill").gameObject;
            so.ApplyModifiedProperties();

            var resMap = LoadDynamicResourceMap();
            var starEmpty  = resMap.TryGetValue("star_empty",  out string ep) ? AssetDatabase.LoadAssetAtPath<Sprite>(ep)  : null;
            var starFilled = resMap.TryGetValue("star_filled", out string fp) ? AssetDatabase.LoadAssetAtPath<Sprite>(fp) : null;
            var lockSpr    = resMap.TryGetValue("ui_lock_icon", out string lp) ? AssetDatabase.LoadAssetAtPath<Sprite>(lp) : null;
            foreach (var star in new[] { s0, s1, s2 })
            {
                if (starEmpty  != null) { star.GetComponent<Image>().sprite = starEmpty; }
                var fillImg = star.transform.Find("Fill")?.GetComponent<Image>();
                if (fillImg != null && starFilled != null) fillImg.sprite = starFilled;
            }
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
            textTmp.fontSizeMin = 24f;
            textTmp.fontSizeMax = 36f;
            textTmp.fontSize = 32f;
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

            // StarCountContainer — dark pill below chest image; shows {current}/{max} stars
            var starContainer = Child(root, "StarCountContainer");
            Fixed(starContainer, new Vector2(0, -85), new Vector2(150, 40));
            var starContainerImg = Img(starContainer, Hex("2A1635"));
            starContainerImg.raycastTarget = false;

            var starShadow = Child(starContainer, "Shadow");
            Fixed(starShadow, new Vector2(0, -3), new Vector2(150, 40));
            var starShadowImg = Img(starShadow, Hex("1A0020"));
            starShadowImg.raycastTarget = false;
            starShadow.transform.SetAsFirstSibling();

            var starTxt = TMP(starContainer, "StarCountText", Center(0, 1, 140, 34), 16, UI_TEXT, "0/0", null, TextCategory.Normal);
            starTxt.alignment = TextAlignmentOptions.Center;
            starTxt.enableWordWrapping = false;

            // Bind Serialized Fields on ChapterChestView
            var so = new SerializedObject(chestView);
            so.FindProperty("_chestImage").objectReferenceValue = chestImg;
            so.FindProperty("_button").objectReferenceValue = chestBtn;
            so.FindProperty("_glowEffect").objectReferenceValue = glow;
            so.FindProperty("_sparkleParticles").objectReferenceValue = ps;
            so.FindProperty("_canvasGroup").objectReferenceValue = chestCg;
            so.FindProperty("_starCountLabel").objectReferenceValue = starTxt;

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
            PixelShadow(go);
            var visual = Child(go, "Visual");
            Stretch(visual);
            var img = Img(visual, UI_BG_DEEP);
            var btn = Comp<Button>(go);
            btn.targetGraphic = img;
            Comp<UIButtonAnimator>(go);
            TMP(visual, "Label", Center(0, 0, size, size), 32, UI_TEXT, "✕", null, TextCategory.Button);
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

            // AutoFontSize: applied only for size >= 28. Below 28 uses fixed size.
            if (size < 28)
            {
                tmp.enableAutoSizing = false;
                tmp.fontSize = size;
            }
            else
            {
                tmp.enableAutoSizing = true;
                switch (category)
                {
                    case TextCategory.Header:
                        tmp.fontSizeMin = 48f; tmp.fontSizeMax = 72f;
                        break;
                    case TextCategory.Button:
                        tmp.fontSizeMin = 36f; tmp.fontSizeMax = 56f;
                        break;
                    case TextCategory.Normal:
                    default:
                        tmp.fontSizeMin = 28f; tmp.fontSizeMax = 36f;
                        break;
                }
                tmp.fontSize = tmp.fontSizeMax;
            }
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

        static GameObject StarGO(GameObject parent, string name, float size = 70f)
        {
            var go = Child(parent, name);
            RT(go).sizeDelta = new Vector2(size, size);
            var le = Comp<LayoutElement>(go);
            le.minWidth = le.preferredWidth = size;
            le.minHeight = le.preferredHeight = size;
            var emptyImg = Img(go, Color.white);
            emptyImg.preserveAspect = true;
            emptyImg.raycastTarget = false;
            var fill = Child(go, "Fill");
            Stretch(fill);
            var fillImg = Img(fill, Color.white);
            fillImg.preserveAspect = true;
            fillImg.raycastTarget = false;
            fill.SetActive(false);
            return go;
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
            captionTmp.enableAutoSizing   = true;
            captionTmp.fontSizeMin        = 28f;
            captionTmp.fontSizeMax        = 38f;
            captionTmp.fontSize           = 38f;
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
            itemLabelTmp.enableAutoSizing   = true;
            itemLabelTmp.fontSizeMin        = 28f;
            itemLabelTmp.fontSizeMax        = 38f;
            itemLabelTmp.fontSize           = 38f;
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
            foreach (var path in new[] { PrefabRoot, PrefabBase, BaseCommonPath, BaseScenesPath, PrefabFinal })
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
            string[] stageInfoPaths = {
                "Assets/Resources/Prefabs/UI/StageInfoPopupView.prefab",
                "Assets/UI/Prefabs/Base/Common/StageInfoPopupView.prefab"
            };
            string[] resultPaths = {
                "Assets/Resources/Prefabs/UI/ResultOverlayView.prefab",
                "Assets/UI/Prefabs/Base/Common/ResultOverlayView.prefab"
            };
            string[] stageNodePaths = {
                "Assets/Resources/Prefabs/UI/StageNodeView.prefab",
                "Assets/UI/Prefabs/Base/Common/StageNodeView.prefab"
            };

            for (int i = 0; i < 3; i++)
            {
                foreach (var path in stageInfoPaths)
                {
                    var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    MapHierarchyImageSprite(p, $"Panel/InnerPanel/Stars/Star{i}", "star_empty", resMap);
                    MapHierarchyImageSprite(p, $"Panel/InnerPanel/Stars/Star{i}/Fill", "star_filled", resMap);
                }
                foreach (var path in resultPaths)
                {
                    var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    MapHierarchyImageSprite(p, $"Panel/InnerPanel/Stars/Star{i}", "star_empty", resMap);
                    MapHierarchyImageSprite(p, $"Panel/InnerPanel/Stars/Star{i}/Fill", "star_filled", resMap);
                }
                foreach (var path in stageNodePaths)
                {
                    var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    MapHierarchyImageSprite(p, $"Stars/Star{i}", "star_empty", resMap);
                    MapHierarchyImageSprite(p, $"Stars/Star{i}/Fill", "star_filled", resMap);
                }
            }

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
            if (resMap.TryGetValue("star_filled", out string sfp))
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
            var localStarsText  = TMP(localPanel, "LocalStarsText",  Center(0,  10, 340, 55),  18, UI_TEXT, "★ 0",          PopupAccountConflictStarsFmt,   TextCategory.Normal);
            var localItemsText  = TMP(localPanel, "LocalItemsText",  Center(0, -60, 340, 55),  18, UI_TEXT, "Items: 0",      PopupAccountConflictItemsFmt,   TextCategory.Normal);
            var keepLocalBtn    = Btn(localPanel, "KeepLocalButton", new Vector2(0, -190), new Vector2(340, 75), UI_PRIMARY, "Keep Current", PopupAccountConflictBtnKeepLocal);

            // Cloud save panel (right)
            var cloudPanel = Panel(panel, "CloudPanel", new Vector2(380, 550), UI_BG_DEEP);
            Fixed(cloudPanel.transform.parent.gameObject, new Vector2(215, 80), new Vector2(380 + 16, 550 + 16));

            var cloudLabel      = TMP(cloudPanel, "CloudLabel",      Center(0, 240, 340, 60),  22, UI_TEXT, "Google Account Data", PopupAccountConflictCloudLabel,        TextCategory.Header);
            var cloudStageText  = TMP(cloudPanel, "CloudStageText",  Center(0, 150, 340, 55),  18, UI_TEXT, "Stage 0",             PopupAccountConflictStageFmt,          TextCategory.Normal);
            var cloudGoldText   = TMP(cloudPanel, "CloudGoldText",   Center(0,  80, 340, 55),  18, UI_TEXT, "Gold: 0",             PopupAccountConflictGoldFmt,           TextCategory.Normal);
            var cloudStarsText  = TMP(cloudPanel, "CloudStarsText",  Center(0,  10, 340, 55),  18, UI_TEXT, "★ 0",                PopupAccountConflictStarsFmt,          TextCategory.Normal);
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
            so.FindProperty("_localStarsText").objectReferenceValue = localStarsText;
            so.FindProperty("_localItemsText").objectReferenceValue = localItemsText;
            so.FindProperty("_keepLocalButton").objectReferenceValue = keepLocalBtn.GetComponent<Button>();
            so.FindProperty("_cloudLabel").objectReferenceValue     = cloudLabel;
            so.FindProperty("_cloudStageText").objectReferenceValue = cloudStageText;
            so.FindProperty("_cloudGoldText").objectReferenceValue  = cloudGoldText;
            so.FindProperty("_cloudStarsText").objectReferenceValue = cloudStarsText;
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
            labelTmp.fontSize          = 14f;
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
            priceTxt.fontSize = 18;
            priceTxt.fontStyle = FontStyles.Bold;
            priceTxt.color = UI_TEXT;

            // Right Area: Title & Description Info (아이콘 우측 공간)
            var titleGo = Child(visual, "TitleText");
            Fixed(titleGo, new Vector2(120, 60), new Vector2(600, 60));
            var titleText = Comp<TextMeshProUGUI>(titleGo);
            titleText.text = "Product Title";
            titleText.fontSize = 30;
            titleText.color = Hex("FFDF00"); // High-readability gold/yellow for title
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Left;
            Comp<LocalizedText>(titleGo);
            Comp<UITextStyle>(titleGo).ApplyStyle();

            var descGo = Child(visual, "DescText");
            Fixed(descGo, new Vector2(120, -40), new Vector2(600, 120));
            var descText = Comp<TextMeshProUGUI>(descGo);
            descText.text = "Product description goes here. Highly readable text style.";
            descText.fontSize = 18;
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

            // TagText configured manually to prevent massive font sizing issues caused by TextCategory.Header
            var tagTxtGo = Child(tagGo, "TagText");
            Fixed(tagTxtGo, new Vector2(15, 0), new Vector2(190, 42));
            var tagTxt = Comp<TextMeshProUGUI>(tagTxtGo);
            tagTxt.enableAutoSizing = true;
            tagTxt.fontSizeMin = 12f;
            tagTxt.fontSizeMax = 20f;
            tagTxt.fontSize = 18f;
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
            limitText.fontSize = 18;
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
            var titleTmp = TMP(inner, "Title", Center(38f, 55f, 185f, 44f), 18, UI_TEXT, "Item Name", null, TextCategory.Normal);
            titleTmp.fontSizeMin = 20f;
            titleTmp.fontSizeMax = 32f;
            titleTmp.enableWordWrapping = false;
            titleTmp.alignment = TextAlignmentOptions.Left;

            // Desc — full width, below icon and title
            var descTmp = TMP(inner, "Desc", Center(0f, -45f, 310f, 80f), 14, new Color(0.82f, 0.78f, 0.88f, 1f), "Description", null, TextCategory.Normal);
            descTmp.fontSizeMin = 14f;
            descTmp.fontSizeMax = 22f;
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
