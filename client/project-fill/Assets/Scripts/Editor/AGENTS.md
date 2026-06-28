# Editor — Unity Editor automation tools

## Files
| file | class | role |
|------|-------|------|
| `UIImageResourceExtractor.cs` | `UIImageResourceExtractor` | [MenuItem] batch-loads transparent UI images, parses alpha-connected components, previews and saves numbered PNG sprites |
| `BuildScript.cs` | `BuildScript` | Batch-mode Android build entry points; reads KEYSTORE_PASS/KEY_ALIAS_PASS from env; `BuildAndroidRelease` applies release size settings then builds ARM64 AAB |
| `ClearPlayerPrefs.cs` | `PlayerPrefsResetMenu` | [MenuItem] clears all PlayerPrefs for local reset/debug |
| `IconAutomator.cs` | `IconAutomator` | [MenuItem] icon DPI generator — resizes source icon and applies to all Android/iOS DPI slots |
| `GoogleMobileAdsGradleManifestPostprocessor.cs` | `GoogleMobileAdsGradleManifestPostprocessor` | Android Gradle postprocessor for GMA plugin conflicts |
| `BuildCleanupPostProcessor.cs` | `BuildCleanupPostProcessor` | IPostprocessBuildWithReport; deletes large unwanted artifacts (`_BackUpThisFolder...`, `_BurstDebugInformation...` folders + `*_mapping.txt` R8 minify file) after successful build |
| `UIColorPalette.cs` | `UIColorPalette` | **project-fill UI palette** — Dark Neon Puzzle; 10 color constants (UI_BG_DEEP/MID, UI_PRIMARY/CTA/SUCCESS/DANGER/TEXT/BORDER, DIM, UI_SHADOW); imported into UIEditorSetup via `using static`; UI-only (InGame colors → CSV → generated data) |
| `UIEditorSetup.cs` | `UIEditorSetup` | [MenuItem] one-shot prefab/scene builders; attaches LocalizedText with stringId from StringIds.cs; auto-creates missing Final variants. Now `partial` (shares private helpers with `UISpecInterpreter.cs`, `UIEditorSetup.Cheat.cs`) |
| `UIEditorSetup.Cheat.cs` | `UIEditorSetup` (partial) | DEV-ONLY cheat overlay prefab builder (`CreateCheatOverlay` → `Resources/Prefabs/UI/CheatOverlayView`); raw dev labels (TMP stringId=null → font-only, no `client_string.csv`) |
| `UISpecInterpreter.cs` | `UIEditorSetup` (partial) + `UISpec`/`UISpecElement`/`UISpecBinding` | **PROTOTYPE** declarative UI: JSON spec in `UISpecs/` → prefab via existing helpers. One menu builds all specs (adding a popup = adding a JSON file). Flat `panel>children` only; reuses Save + Final-variant path |
| `FontLocalizationConfigGenerator.cs` | `FontLocalizationConfigGenerator` | [MenuItem] reads tools/subset_tool/config.json → creates FontLocalizationConfig.asset with per-language fonts; sets TMP fallback |
| `StringIds.cs` | `StringIds` | **AUTO-GENERATED** by `gen:info` from `client_string.csv`; key constants; used by UIEditorSetup via `using static` |
| `StringCsvPostprocessor.cs` | `StringCsvPostprocessor` | AssetPostprocessor; watches `data/string/client_string.csv` reimport → calls `LocalizedText.RefreshAllInEditor()`; menu: `Tools/Localization/Refresh Editor Text Preview` |
| `UnityStageFileWatcher.cs` | `UnityStageFileWatcher` | [InitializeOnLoad] watches changes to shared `stage.csv` and auto-runs generation scripts |
| `AdMobEditorSetup.cs` | `AdMobEditorSetup` | [InitializeOnLoad] automatically ensures GOOGLE_MOBILE_ADS scripting define symbol is set for Android, iOS, and Standalone |
| `DebugSocketScale.cs` | `DebugSocketScale` | [MenuItem "Tools/UI Setup/Debug Sockets and Cells"] editor-only socket/cell scale debug helper |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UIColorPalette` | class | `internal static`; import with `using static Game.Editor.UIColorPalette` in UIEditorSetup |
| `UIColorPalette.UI_BG_DEEP` | prop | `#0B0B1E` deep midnight navy (backdrop, dismiss buttons) |
| `UIColorPalette.UI_BG_MID` | prop | `#181836` dark indigo panel fill |
| `UIColorPalette.UI_PRIMARY` | prop | `#4CC9F0` electric cyan (secondary positive buttons, tabs) |
| `UIColorPalette.UI_CTA` | prop | `#F72585` hot neon magenta (single primary action per screen) |
| `UIColorPalette.UI_SUCCESS` | prop | `#06D6A0` neon teal (success, play) |
| `UIColorPalette.UI_DANGER` | prop | `#EF233C` neon red (destructive/irreversible actions) |
| `UIColorPalette.UI_TEXT` | prop | `#E8E8FF` ice white (all text) |
| `UIColorPalette.UI_BORDER` | prop | `#7B2FBE` electric violet (default Panel outline) |
| `UIColorPalette.DIM` | prop | `rgba(0.02,0.02,0.08,0.80)` near-black overlay (non-interactive backdrops) |
| `UIColorPalette.UI_SHADOW` | prop | `rgba(0.01,0.01,0.04,0.90)` pixel art drop shadow |
| `UIImageResourceExtractor.Open()` | method | [MenuItem "Tools/UI/Image Resource Extractor"] opens the extraction window |
| `BuildScript.BuildAndroidRelease()` | method | Batch entry: Play Store AAB; applies High stripping, IL2CPP OptimizeSize, and release minify before build |
| `BuildScript.ApplyAndroidReleaseSizeSettings()` | method | Sets Android Release build type and size knobs for batch builds |
| `BuildCleanupPostProcessor.OnPostprocessBuild()` | method | Post-build cleanup entry point; scans and deletes unwanted folders |
| `PlayerPrefsResetMenu.ResetPrefs()` | method | [MenuItem "Tools/Reset PlayerPrefs"] deletes all PlayerPrefs |
| `IconAutomator.ShowWindow()` | method | [MenuItem "Tools/Icon Automator"] opens icon automation window |
| `UIEditorSetup.CreateAllPrefabs()` | method | [MenuItem "Tools/UI Setup/1"] creates all popup/overlay prefabs (incl. OutGame: cosmetic/avatar/attendance/achievement/weekly-mission) |
| `UIEditorSetup.CreateAttendancePopup()` | method | [MenuItem ".../AttendancePopup"] 7 day-card attendance popup; each card has a `RewardSlot` (108×150, NO LayoutGroup — View mounts the primary reward cell at fixed size, centered) + a hidden top-left `CountBadge` TMP ("+N" extra-kinds badge, toggled by the View) before its Dim overlay; `TodayRewardRow` HLG + `_rewardCellPrefab`=RewardItemCell.prefab wired for runtime reward previews; hidden `AlreadyClaimedText` (centered, shown by View once claimed in place of label+row) |
| `UIEditorSetup.CreateCosmeticPreviewPopup()` | method | [MenuItem ".../CosmeticPreviewPopup"] cosmetic preview/buy popup; `StateText` carries a hidden `GoldIcon` child (via `AttachGoldIcon`), toggled at runtime by `GoldPriceLabel.Set` |
| `UIEditorSetup.CreateAvatarPreviewPopup()` | method | [MenuItem ".../AvatarPreviewPopup"] avatar preview + equip/buy popup; `StateText` carries a hidden `GoldIcon` child (via `AttachGoldIcon`), toggled at runtime by `GoldPriceLabel.Set` |
| `UIEditorSetup.BuildCosmeticCell()` | method | [MenuItem ".../CosmeticItemCell"] cosmetic grid cell prefab 300×360 (Preview + Preview/SelectedHighlight + Preview/LockOverlay + NameText/StateText; StateText has a `GoldIcon` child via `AttachGoldIcon`); referenced by CosmeticSectionView, built in SetupLobby; SetupLobby grid is 3-col, cellSize 300×360, section height 1340 |
| `UIEditorSetup.BuildAvatarCard()` | method | Avatar card prefab (Visual/Icon + SelectedHighlight + LockOverlay + StateText; StateText has a `GoldIcon` child via `AttachGoldIcon`); referenced by AvatarSectionView, built in SetupLobby |
| `UIEditorSetup.AttachGoldIcon(state,iconSize)` | method | Adds a hidden, **center-anchored** `GoldIcon` child to a price TMP; leaves the text untouched (fixed-width + center + autosize → numbers and long word states both safe). **No ContentSizeFitter** (autosize makes it unreliable; also strips any stale one). Runtime `GoldPriceLabel.Set` measures the rendered text width and positions the coin flush-left of the centered number. Used by the 2 cosmetic/avatar cells + 2 preview popups |
| `UIEditorSetup.PopulateAvatarSprites(prop,resMap)` | method | Fills an `_avatarSprites` SerializedProperty from avatar.csv; shared by Shop avatar section + AccountPopup |
| `UIEditorSetup.CreateAchievementToast()` | method | [MenuItem ".../AchievementToast"] slide-down tier-badge toast |
| `UIEditorSetup.BuildAchievementCell()` | method | Achievement list cell prefab; referenced by AchievementTabView, built in SetupLobby; `TierBadge` holds inset `RewardIcon` child (representative reward sprite, set at runtime); ProgressBar uses sprite-free `AnimatedProgressBar` (width-driven Fill, no sprite/mask) |
| `UIEditorSetup.CreateWeeklyMissionPopup()` | method | [MenuItem ".../WeeklyMissionPopup"] Weekly Mission Event popup: title/days-left/EP gauge (bg+left-anchored fill + empty `TrackMarkers` container spanning the bar; View spawns milestone ticks anchored at threshold/maxThreshold so they align with the fill)/mission **ScrollView** (Viewport+RectMask2D, top-anchored Content VLG+ContentSizeFitter; rows = `WeeklyMissionItemCell` prefab built at runtime by the View)/claim CTA. Builds the cell inline via `BuildWeeklyMissionCell` + wires `_missionCellPrefab` |
| `UIEditorSetup.BuildWeeklyMissionCell()` | method | Weekly-mission list cell prefab 720×150 (StatusBadge dot + NameText/DescText + sprite-free `AnimatedProgressBar` Fill + ProgressText + EpText "+N EP"); NO per-cell claim (claim is popup-level by EP milestone); built inline in `CreateWeeklyMissionPopup`, referenced by `WeeklyMissionPopupView._missionCellPrefab` |
| `UIEditorSetup.CreateCheatOverlay()` | method | [MenuItem ".../CheatOverlay"] DEV-ONLY cheat overlay prefab: own Canvas (sort 1000) + toggled `Panel` (header/close/mode tabs/log/command pane/button pane w/ 8 `CheatDomain` tabs + target/amount inputs + 4 pooled action buttons); wires all `CheatOverlayView` SerializeFields incl. `_domainTabButtons[8]`/`_actionButtons[4]`. Helper `CheatInputField` builds TMP_InputFields. Dynamic-loaded at runtime (not via UIManager) |
| `UIEditorSetup.SetupLobby()` | method | builds 4 bottom-nav tabs (Shop/Home/Ranking/Achievement), Shop `ShopSubTabs` row (Product/Skin/Avatar) wired to ShopTabView + cosmetic section (top-pinned SectionTitle above CategoryTabs) + avatar section (responsive Flexible GridLayout, cell 180×210, top-pinned title; replaced legacy horizontal carousel), Shop title ribbon (shadow nested inside ribbon as first sibling, white text on top — fixes text hidden behind same-color shadow), Ranking tab (responsive top-down stack via `TopAnchor`: TitleText → stage/perfect/weekly tab row → per-tab DescText → MyRankText; VirtualizedScrollRect width 900 vertical-stretches directly below MyRankText; MyRankPin bottom-anchored above BottomNavBar — no element relies on center-absolute Y so nothing is clipped by HUD/nav on any aspect; legacy stars/max-stage/challenge buttons cleaned up), and the Achievement tab |
| `UIEditorSetup.BuildAllFromSpecs()` | method | [MenuItem "Tools/UI Setup/Specs/Build All From Specs"] builds every `UISpecs/*.json`. PROTOTYPE — proves data-driven popup definition; `_`-prefixed JSON skipped |
| `UIEditorSetup.ResolveColor(s)` | method | spec color string → `UIColorPalette` static property (reflection), else `Hex()` one-off |
| `UIEditorSetup.ApplyCanvasScaler(go)` | method | Enforces canonical CanvasScaler on any canvas GO: ScaleWithScreenSize, 1080×1920, MatchWidthOrHeight=0.5, referencePixelsPerUnit=100. Called by both `LoadOrCreateCanvas` (load path) and `CreateTempCanvas` (create path). |
| `UIEditorSetup.GetEditorDefaultFont()` | method | Loads EN TMP_FontAsset from FontLocalizationConfig.asset; cached after first call; null if config not yet generated. Called by `TMP()` to apply editor-time font. |
| `UIEditorSetup.Panel(parent, name, size, color, outline?)` | method | 3-layer pixel panel: returns `Content` GO (the fill layer). Creates PanelRoot (no image, size+16 to include 8px border padding each side) → Shadow (child 0, stretch, right+8 bottom+8) → Border (child 1, stretch, outline color) → Content (child 2, 8px inset, fill color). Default outline = `UI_BORDER`. |
| `UIEditorSetup.TopAnchor(rt, size, yTop, x?)` | method | Top-center anchored, fixed-size placement; `yTop` is the downward offset from the parent top edge (negative). Used by RankingTab's responsive top-down stack so title/tab-row/desc/my-rank hug the top below the HUD on any aspect ratio |
| `UIEditorSetup.PixelShadow(parent, color?)` | method | Pixel art drop shadow: stretch-stretch on parent, offsetMin/Max=(8,-8) → right+8 bottom+8; always SetAsFirstSibling; auto-called by Panel/Btn/BtnHlg/RibbonTitle |
| `UIEditorSetup.CloseBtnAt(parent, pos, size?)` | method | Square explicit close button (default **96px**); Visual = white-tinted `ui_close_button.png` sprite (no shadow, no label) + UIButtonAnimator; wire returned Button to View._backdropButton or _closeButton |
| `UIEditorSetup.Btn(parent, name, pos, size, color, label, ...)` | method | Pixel art button; size clamped to minimum 96×96 before layout; has PixelShadow + Visual layer |
| `UIEditorSetup.TryMapImageSprite()` | method | Helper to map sprite directly onto Image component target |
| `UIEditorSetup.BtnNavTab()` | method | Nav bar tab button — icon (color-tinted) above label; `_homeHighlight` etc. wire to `Visual/Icon` Image |
| `UIEditorSetup.ItemToggleRow()` | method | Toggle row with item icon on left; Label TMP is a child of Toggle GO (GetComponentInChildren finds it) |
| `UIEditorSetup.MapStarAndIconSprites()` | method | Re-maps star_empty/star_filled/lock icon on Common prefabs without recreating them |
| `UIEditorSetup.CreateForceUpdateView()` | method | [MenuItem ".../ForceUpdateView"] forced-action update popup; opaque `UI_BG_DEEP` bg, Update button wired to `ForceUpdateView._updateButton` |
| `UIEditorSetup.CreateRewardPopup()` | method | Also creates `RewardItemCell.prefab` inline (background+Icon+Quantity badge+RewardItemCellView); wires `_itemRowPrefab` and `_closeButton` on RewardPopupView; adds visual-only Backdrop child |
| `UIEditorSetup.CreateResultOverlay()` | method | [MenuItem ".../ResultOverlay"] stage-clear overlay: StatsBlock (MovesText/BestText runtime-formatted + NewBestBadge, hidden by default) + RewardCellContainer (HLG, runtime RewardItemCell rows) + DoubleReward/Next/Map; wires `ResultOverlayView` refs incl. `_statsBlock`/`_movesText`/`_bestText`/`_newBestBadge`, `_rewardCellPrefab` = `RewardItemCell.prefab` |
| `UIEditorSetup.CreateFailOverlay()` | method | [MenuItem ".../FailOverlay"] stuck-rescue overlay: AddLane(ad + "Watch Ad" badge)/Retry(CTA)+Forfeit(danger). **No Shuffle button / ShuffleConfirmPanel** (gold rescue removed). Wires `FailOverlayView` refs `_titleText`/`_addLaneRow`/`_addLaneButton`/`_retryButton`/`_forfeitButton`. AddLane button has no icon; AdBadge image is inspector-assigned (not `dynamic_resource`) |
| `UIEditorSetup.AddButtonIcon(btn,key,resMap)` | method | Adds a left-aligned non-raycast icon Image to a wide button from a `dynamic_resource` sprite key |
| `UIEditorSetup.CreateItemTooltip()` | method | Creates `ItemTooltipView.prefab` skeleton in Base/Common; missing Resources/Prefabs/UI variant is created automatically |
| `UIEditorSetup.MakeGimmickBadge(parent,name,spritePath)` | method | Builds a 96×96 icon badge (sprite from `Assets/Sprites/UI/Icons/`) with `LongPressTooltipTrigger`; used by StageInfoPopup "Special Rules" gimmick row |
| `UIEditorSetup.RegisterTutorialTarget(go, ids)` | method | Attaches `TutorialTarget` and sets `_targetIds` (tutorial_step.csv `target_ui_id`) via SerializedObject; SetupInGame registers `hud_moves_count` (MovesText) + `booster_bar` (BoosterBar) |
| `UIEditorSetup.CreateVariantIfMissing()` | method | Creates Final prefab variants from Base prefabs; skips when target variant already exists |
| `UIEditorSetup.MapHierarchyImageSprite()` | method | Maps a sprite key from resMap to an Image at `childPath` inside a loaded prefab |
| `UIEditorSetup.TMP(parent, name, rect, size, color, text, stringId, category)` | method | Creates TMP_Text with LocalizedText + UITextStyle; sizing via `ApplyAutoFontSize(tmp, category)` (see font size rules). `size` arg is legacy/unused for sizing — `category` drives the max |
| `UIEditorSetup.ApplyAutoFontSize(tmp, category)` | method | Single enforcement point of the readability convention: `enableAutoSizing=true`, `fontSizeMin=24`, `fontSizeMax` by category (Header 72 / Button 56 / Normal 40), `fontSize=max`. MANDATORY for every TMP incl. hand-built. `TMP_InputField` is the sole exception (inline fixed min=max=32). No text may have autosize off or any size <24 |
| `FontLocalizationConfigGenerator.Generate()` | method | [MenuItem "Tools/Localization/Generate Font Config"] creates FontLocalizationConfig.asset from config.json |
| `StringIds` | class | All client_string.csv key constants; import with `using static Game.Editor.StringIds` |
| `UnityStageFileWatcher` | class | Active file system watcher for local hot-reloads |
| `AdMobEditorSetup` | class | [InitializeOnLoad] editor helper setting GOOGLE_MOBILE_ADS scripting define symbol |

## Rules
- Editor-only folder — auto-excluded from player builds
- DO NOT add game logic or runtime dependencies here
- **UI Color Convention**: All UIEditorSetup color usage MUST reference `UIColorPalette` constants — never hardcode palette colors inline. Inline `Hex()` is allowed only for one-off contextual colors (e.g. product card specific overrides).
- **Panel 3-layer structure**: `Panel()` returns `Content` (the top fill layer). Hierarchy under PanelRoot: `[0] Shadow` (stretch, offset right+8 bottom+8) → `[1] Border` (stretch, outline color, 8px thick) → `[2] Content` (stretch with 8px inset, fill color). PanelRoot itself has NO Image component. Shadow must be first sibling so UGUI renders it lowest.
- **Panel size**: Pass the desired visible content size to `Panel()`; it auto-adds 16px (8px border each side) to the root RectTransform. When manually overriding a panel's Fixed() size externally, add `+16` not `+24`.
- **Shadow Convention (pixel art)**: Panel/Button/TitleRibbon/Container MUST use `PixelShadow()` — stretch-stretch anchoring, offsetMin/Max=(8,-8). Never use `Fixed()` for shadows. `BtnHlg`, `Btn`, `Panel`, `RibbonTitle` helpers call this automatically.
- **Button minimum size**: All buttons must be at least **96×96px**. `Btn()` clamps `size = Max(size, 96)` before layout. `CloseBtnAt()` defaults to 96.
- **Explicit Close Button**: Every popup and bottom-sheet MUST include a visible square `CloseButton` via `CloseBtnAt()`. Transparent backdrop-only dismiss is NOT sufficient. Place at top-right of panel or top-right of bottom-sheet. Wire to View's close handler field.
- **Responsive Layout**: Use `Stretch()` for elements that must follow parent size. Use `Fixed()` only for fixed-size content. SafeAreaHandler required on all scene canvases.
- **Text convention (readability)**: Every `TMP()` call MUST attach `LocalizedText` (stringId) + `UITextStyle`. AutoFontSize is **MANDATORY** and font size is **NEVER below 24px**. All sizing goes through `ApplyAutoFontSize(tmp, category)`:
  - `enableAutoSizing = true` — ALWAYS on; no text (incl. hand-built `Comp<TextMeshProUGUI>`) may have it off
  - `fontSizeMin = 24` — hard readability floor
  - `fontSizeMax` by `TextCategory`: **Header 72** (titles render large), **Button 56**, **Normal 40**. `fontSize` starts at max and autosize shrinks to fit down to 24
  - **Hand-built TMP** MUST call `ApplyAutoFontSize(tmp, category)` — never a bare `fontSize`/`fontSizeMin`/`fontSizeMax`. Use `Header` for prominent titles, `Normal` for body/labels
  - **`TMP_InputField`** text + placeholder: sole exception — inline fixed `min=max=32` (autosize on, no resize range so the caret/scroll isn't disturbed)
- **Button color semantics**:
  | color | semantic | examples |
  |-------|----------|---------|
  | `UI_CTA` | Single primary action per screen (irreversible or most important) | Confirm, Retry, OK, Next, Link Account |
  | `UI_PRIMARY` | Secondary positive / alternative CTA | Double Reward, Retry (result screen secondary) |
  | `UI_DANGER` | Destructive / irreversible negative action | Forfeit, Restart (PausePopup), Account Restart Confirm |
  | `UI_BG_DEEP` | Dismiss / cancel / navigation | Map (result screen), back/close buttons used as secondary |
- **Canvas Scaler**: Every scene canvas MUST use `ApplyCanvasScaler()` — `ScaleWithScreenSize`, reference `1080×1920`, `MatchWidthOrHeight=0.5`, `referencePixelsPerUnit=100`. Applied automatically by `LoadOrCreateCanvas()` on both create and reload. NEVER set CanvasScaler fields manually in scene builders.
- **Font convention**: `TMP()` applies the EN font from `FontLocalizationConfig.asset` at editor-build time via `GetEditorDefaultFont()`. Do NOT hardcode `tmp.font` elsewhere. `LocalizedText` handles runtime language switching; `Tools/Localization/Refresh Editor Text Preview` refreshes the preview. Run `FontLocalizationConfigGenerator.Generate()` before `CreateAllPrefabs()` on a fresh project.
- **Popup mandatory components**: All popup prefab roots MUST have `UIPanelAppear` + `CanvasGroup`. `UIManager.ShowPopup<T>()` requires both for appear animation and alpha fading. Missing either = silent failure (popup shows instantly, CanvasGroup alpha won't fade).
- **Text raycastTarget**: `TMP()` always sets `tmp.raycastTarget = false`. NEVER override to `true` — text blocking raycast prevents scroll and button interaction on elements beneath.
- **Backdrop rules**:
  - **Visual-only backdrop** (default for all popups): `Child(root, "Backdrop") + Stretch + Img(DIM)`. `raycastTarget=true` to block pass-through to layers below. Close via explicit `CloseButton` only. Use when: popup has a visible Cancel/Close/Resume button.
  - **Interactive backdrop** (cancel affordance): `Btn(root, "Backdrop", ...)` wired to the same handler as the Cancel button. Use ONLY when: (a) the popup has a two-action choice (Confirm/Cancel) AND (b) an explicit Cancel button is already visible — backdrop tap = secondary cancel shortcut. Never the sole dismiss mechanism.
  - **No backdrop** (forced-action modal): No backdrop child at all. Use for: network error, re-login, any state where the user MUST take an action to proceed.
- **Z-order / Sort Order**:
  - Canvas Sort Order: `0`=Scene, `10`=UIManager Popup stack, `20`=Overlay, `30`=Toast, `100`=Loading/critical. (Defined in `Core/AGENTS.md` — do not override without coordinating UIManager.)
  - Within a prefab hierarchy, sibling order = paint order (first=lowest, last=highest). Rules: Shadow → `SetAsFirstSibling`; CloseButton → `SetAsLastSibling` (must hit-test above panel content); scene Header → `SetAsLastSibling` within canvas (above TabContent).
  - Use `Canvas` component with `overrideSorting=true` only for elements that must temporarily escape parent sort order (e.g., dropdown templates). Not for normal UI layers.
- **Square button Icon**: When `Btn()` detects a square button (`size.x == size.y`), it creates a `Visual/Icon` child with `Stretch-Stretch` anchoring and `Image.preserveAspect=true`. Default idle animation: `UIIconIdleAnimator.GlowSweep(speed=2.2, amount=12)`. Set the sprite manually after creation or via `dynamic_resource.csv` + `LoadDynamicResourceMap()`. Never set `Icon` to `Fixed()` — it must follow the Visual layer size.
- **`_isCTA` auto-set**: `Btn()` with `color == UI_CTA` auto-sets `UIButtonAnimator._isCTA = true` via `SerializedObject`, enabling `CTAIdle` breathing animation (scale 1.0→1.04 sine, 2.5s period). Do not set `_isCTA` manually in prefab builders — let `Btn()` control it.
- **LayoutGroup vs Fixed() guidance**:
  | Pattern | When to use |
  |---------|-------------|
  | `Fixed(go, pos, size)` | Known size at build time; absolute position; isolated element |
  | `Stretch(go)` | Layer fills parent: Shadow, Border, Content, full-screen overlays, backdrops |
  | `HorizontalLayoutGroup` | N siblings in a row with equal/weighted distribution (nav bar, button row) |
  | `VerticalLayoutGroup` | N siblings stacked vertically with spacing (settings rows, option lists) |
  | `GridLayoutGroup` | N × M grid of uniform cells (avatar grid, reward items) |
  | `LayoutElement` | Child inside LayoutGroup that needs custom preferred size or weight |
  NEVER place a `Fixed()` element as a direct child of a LayoutGroup — it bypasses layout and causes undefined behavior.
- **ContentSizeFitter guidance**:
  | Axis | Use when |
  |------|---------|
  | `horizontalFit = PreferredSize` | Single-line label with dynamic text length (gold count, username, tag pill) — add to the label GO only |
  | `verticalFit = PreferredSize` | Multi-line body text that word-wraps and needs to push parent height (error body, confirm body) |
  | Both axes | Isolated tooltip or pill with no LayoutGroup ancestor |
  NEVER set both axes on a child inside a LayoutGroup — causes layout rebuild loop. NEVER add ContentSizeFitter to a container that has a LayoutGroup component.
- **UI Setup Icon & Layout Override Preservation**: 
  - **Preserve Sprites**: When setting sprites on UI Image components (e.g. Header Avatar/Settings), check if `image.sprite == null` first to preserve manual modifications.
  - **Reuse Existing Objects**: Avoid destroying and recreating objects (e.g., `InstantiatePrefab` or `DestroyImmediate` followed by `Child`) in hierarchy builders. Reusing existing GameObjects using `Transform.Find` keeps the `fileID` structure intact, preventing the final Variant's overrides (e.g., Active states, color, and RectTransform overrides) from breaking.
  - **Save Flushing**: Always call `AssetDatabase.SaveAssets()` after editing and saving prefabs via `PrefabUtility.SaveAsPrefabAsset` to ensure that Unity saves and remaps Variant overrides correctly on disk.
  - **Single Prefab Menu Items**: All individual UI prefab builder menu items must be defined under `Tools/UI Setup/Prefabs/...`, sorted alphabetically, and their priority indices assigned consecutively. Each builder must print a success log containing `[UIEditorSetup] Saved Base Popup → {path}` on completion.
- **Final Variant Override Preservation (idempotent builders)**: Builders edit the **Base** prefab only; the **Final variant** (`Final/Scenes/.../*_Base.prefab`, `Resources/Prefabs/UI/*.prefab`) holds the user's inspector overrides (e.g. `BoardView.Skin` sprite). Prefab Variant inheritance absorbs Base changes while keeping overrides, and `CreateVariantIfMissing` skips an existing variant — so a re-run preserves overrides **as long as fileIDs stay stable**. Two anti-patterns reset overrides and are BANNED in any builder:
  - NEVER `AssetDatabase.DeleteAsset(finalVariantPath)` to "force clean regen" — it drops every override on the variant.
  - NEVER wholesale-destroy a loaded prefab's children (`for (i = childCount-1 ...) DestroyImmediate(GetChild(i))`) — recreating them churns fileIDs and orphans child-level variant overrides.
  - NEVER `PrefabUtility.RevertPrefabInstance` a prefab instance placed in a scene (`*.unity`) — it wipes every per-instance override (e.g. `BoardView.Skin` assigned on the InGameCanvas instance in `InGame.unity`). Prefab/variant inheritance already propagates Base structure changes to the instance automatically, so the revert is unnecessary as well as destructive. Re-wire references directly on the existing instance instead.
  Instead rely on the Find-or-create helpers (`Child()`, `Comp<>`, `Img()`) which reuse existing GameObjects/components by name and keep fileIDs intact (this is what `SetupBoot`/`SetupLobby` do). Targeted removal of a specific obsolete **named** child via `Transform.Find` is allowed (legacy cleanup).
