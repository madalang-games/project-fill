# OutGame/Lobby - Lobby Scene

Namespace: `Game.OutGame.Lobby`

## Files
| file | class | role |
|------|-------|------|
| `LobbyView.cs` | `LobbyView` | Root lobby controller; shows/hides tabs; refreshes gold |
| `HeaderView.cs` | `HeaderView` | Avatar tap → AccountPopup; Gold display |
| `BottomNavBarView.cs` | `BottomNavBarView` | 3-tab nav; fires OnTabChanged |
| `RankingTabView.cs` | `RankingTabView` | Ranking tab UI; 3 tabs stage(max reach)/perfect(perfect-clear count)/weekly(this-week clears) + per-tab desc line, my rank, virtualized list via VirtualizedScrollRect |
| `RankingItemView.cs` | `RankingItemView` | Component on RankingItemPrefab; Bind(entry,avatar,score) + SetHighlight(bool) for MyRankPin highlight |
| `HomeTabView.cs` | `HomeTabView` | Chapter/stage scroll with object pool; milestone chest rendering + claim API; out-of-life ad prompt uses AdMobService token |
| `ChapterChestView.cs` | `ChapterChestView` | Milestone chest node displaying Locked (INACTIVE), Claim! (ACTIVE), or Cleared (CLAIMED) states |
| `StageNodeView.cs` | `StageNodeView` | Pooled node: `ui_stage_node` sprite tinted per chapter (white lerped 0.55 toward chapter PathColor; static desaturated "power-off" color when locked) + difficulty glow aura behind (Easy none / Normal teal / Hard crimson) shown in both states — unlocked color-pulses (hue base↔brightened, fixed alpha), locked is steady (static); Bind(id,unlocked,chapterId,difficulty). Cleared badge / lock icon / pulse ring / skull removed — position + path + guide orb convey progress |
| `StageInfoPopupView.cs` | `StageInfoPopupView` | Stage info popup: title, best moves (campaign ranking metric, label-prefixed via `best_moves`/`best_none`), `difficulty_fmt`-wrapped difficulty label, color-type count (`types_fmt`), "Special Rules" gimmick section (header + icon-only badges, long-press → name+effect tooltip; whole section hidden when no gimmick), PLAY button |
| `ScrollStateCache.cs` | `ScrollStateCache` | Static session memory: HomeScrollPosition, LastPlayedStageId |
| `ChapterBgTheme.cs` | `ChapterBgTheme` | Static theme config per `bg_theme_id`; colors + particle params |
| `ChapterBackgroundView.cs` | `ChapterBackgroundView` | Chapter scroll decoration tinted per theme: blinking star LEDs + drifting glow motes + scanline sweep band (gradient itself drawn by HomeTabView). Sprites via DynamicResourceService (led_star/deco_mote/deco_scanline), code fallback each |
| `ShopTabView.cs` | `ShopTabView` | Shop tab UI; 3 top-level sub-tabs (`ShopSubTab` Product/Skin/Avatar) toggle which content shows — Product=IAP cards (grouped by `iap_category`, divider+label headers, purchase limits/dynamic status), Skin=`_cosmeticSection`, Avatar=`_avatarSection`. Card visibility gated on Product tab in `RefreshUI` |
| `LobbyEnums.cs` | `ShopSubTab` | Client-internal enum: Shop tab sub-tabs (Product/Skin/Avatar) |
| `RewardDisplay.cs` | `RewardDisplay` | Static helper; `Build(GrantedRewardDto[])` (claim results) + `BuildFromGroup(rewardGroupId)` (static reward_group preview, sort_order-ordered) + `BuildCosmeticUnlocks(achievementId)` (cosmetics reverse-reference the gating achievement via `cosmetic_item.unlock_condition_id`; renders procedurally through `CosmeticPreview.Build` via `RewardItem.CustomRender`, no flat sprite) → `RewardPopupView.RewardItem[]`. `ResolveVisual(rewardType,targetId)` is the single source for reward icon-key + name/desc lookup (gold via `currency.icon_name`, item via `item.icon_name`, avatar, no-ads) — reused by `ShopTabView` preview rows. `RepresentativeRewardRender(achievementId)` returns an `Action<Image>` for the achievement cell icon — cosmetic unlocks first (procedural), else reward group's highest-`sort_order` flat item. Shared by attendance/achievement/challenge/chest flows |
| `CosmeticSectionView.cs` | `CosmeticSectionView` | Shop cosmetic section: Chip/Lane/Board category tabs + grid; canonical board-customization surface (unified — replaced AccountPopup board tab). Cell preview built by `CosmeticPreview.Build` (layered procedural skin, replaces the old `preview_res`/`ApplyProceduralPreview` swatch path) |
| `CosmeticPreview.cs` | `CosmeticPreview` | Static UI composer: builds a layered procedural preview of a cosmetic skin (board surface+chips / lane column / scaled-down chip) under a `Preview` Image at runtime, from the same `TextureFactory` sprites + `BoardTheme` tokens the skin uses in-game. Resolution-independent (fractional anchors); no RenderTexture, no prefab edit. Shared by the grid cell + the preview popup |
| `CosmeticPreviewPopupView.cs` | `CosmeticPreviewPopupView` | Cosmetic preview popup: preview (`CosmeticPreview.Build`) + name/desc + buy-and-apply / apply via `CosmeticApiService` |
| `GoldPriceLabel.cs` | `GoldPriceLabel` | Static `Set(stateText,isGoldPrice)`: for a gold price, measures the rendered glyph width (`ForceMeshUpdate`+`textBounds`) and places the center-anchored `GoldIcon` flush-left of the centered number; otherwise hides it. Never alters the StateText (stays fixed-width+center+autosize → no overflow on long word states); no ContentSizeFitter (incompatible with TMP autosize). Shared by both cosmetic/avatar sections + both preview popups |
| `AvatarSectionView.cs` | `AvatarSectionView` | Shop avatar section: responsive vertical grid of avatar cards (Flexible-column GridLayout, cell 180×210; `ResizeSectionToFit` sizes the section to row count so the outer Shop scroll absorbs overflow — mirrors CosmeticSectionView). Loads `avatar.csv`; card tap → `AvatarPreviewPopupView`. Uses existing avatar API (NOT cosmetic system); holds `_avatarSprites` mapping |
| `AvatarPreviewPopupView.cs` | `AvatarPreviewPopupView` | Avatar preview popup: preview + cost/state + equip / buy-and-equip via `PlayerApiService.UpdateProfile` + local gold. Reward-only avatars (`unlock_cost==0` && locked) show `shop.avatar.locked_reward` and disable the buy button (no gold path) |
| `AttendancePopupView.cs` | `AttendancePopupView` | Daily attendance popup: 7 day cards (claimed/today/future) each showing ONLY the primary reward of that day's group as a fixed-size runtime `RewardItemCell` (long-press tooltip) in `RewardSlot` + a `CountBadge` "+N" when the group has extra kinds (uniform card size, no per-count scaling), + `TodayRewardRow` (HLG) showing today's full reward group, claim CTA → RewardPopup; rewards resolved via `RewardDisplay.BuildFromGroup`; auto-opened from `LobbyView`. Once `ClaimedToday`, hides `TodayRewardText`+`TodayRewardRow` and shows `AlreadyClaimedText` ("come back tomorrow") filler instead of a bare label over an empty row |
| `AchievementTabView.cs` | `AchievementTabView` | Achievement lobby tab (right of Ranking): 4 category tabs + scroll list (tier badge, progress bar, claim). Migrated from former AchievementListPopupView |
| `AchievementToastView.cs` | `AchievementToastView` | Slide-down achievement-unlocked toast (tier badge + name, 3s); `Show(nameKey,tier)` via ShowOverlay — gameplay trigger is InGame seam |
| `AnimatedProgressBar.cs` | `AnimatedProgressBar` | Sprite-free progress bar (width-driven via anchorMax.x, no fillAmount); animated fill-up + ratio gradient color + soft continuous glow (breathe toward white, stronger when completed); wired by `UIEditorSetup.BuildAchievementCell` |
| `WeeklyMissionPopupView.cs` | `WeeklyMissionPopupView` | Weekly Mission Event popup: scrollable mission list (instantiates `WeeklyMissionItemCell` prefab per mission — white completed-only status badge (hidden until cleared) + name/desc + `AnimatedProgressBar` + EP reward; completed cell dimmed via CanvasGroup alpha ~0.86, badge excluded via own `ignoreParentGroups` CanvasGroup; incomplete sorted to top by progress, completed sink to bottom) + EP gauge with track milestone markers (spawned per milestone, anchored at threshold/maxThreshold so each tick aligns with the fill edge; reached=green, unreached=slate) + claim CTA (lowest reached-unclaimed milestone); reads `WeeklyMissionApiService`, reward display via `RewardDisplay` (design §6.2) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `LobbyView._header` | SerializeField | HeaderView ref |
| `LobbyView._rankingTabRoot` | SerializeField | Ranking tab root toggled by nav |
| `BottomNavBarView.OnTabChanged` | event | `Action<LobbyTab>` |
| `BottomNavBarView.SelectTab(LobbyTab)` | method | Public; sets highlight |
| `BottomNavBarView._achievementButton` | SerializeField | 4th nav tab (right of Ranking); fires `LobbyTab.Achievement` |
| `RankingTabView.Refresh` | method | Fetches page + my rank via `RankingApiService` |
| `RankingTabView._myRankPin` | SerializeField | `RankingItemView` ref; SetHighlight(true) distinguishes from list items |
| `RankingItemView.Bind(entry,avatarSprite,scoreSprite)` | method | Populates all text/icon fields from RankingEntryDto |
| `RankingItemView.SetHighlight(bool)` | method | Switches background color: normal purple vs gold CTA highlight |
| `HomeTabView` | component | OnEnable refreshes pool; OnDisable saves scroll position |
| `HomeTabView.Refresh()` | method | Re-pull current stage + re-bind visible nodes live (mirrors OnEnable render); used after a server-side progress change (dev `/stage` cheat) |
| `StageNodeView.Bind(stageId,unlocked,chapterId,difficulty)` | method | `_node` tint: unlocked → `Color.Lerp(white, ChapterBgTheme.PathColor, 0.55)`, locked → desaturated power-off slate. `_glow` difficulty aura (0=Easy none, 1=Normal teal, 2=Hard crimson) in BOTH states: unlocked → color pulse (hue base↔brightened, fixed alpha), locked → steady static. No cleared/lock-icon/current visuals (conveyed by position + path dim + guide orb) |
| `StageNodeView.OnTapped` | event | `Action<int>` stageId |
| `StageInfoPopupView.Init(stageId,bestMoves,onPlay,difficulty,isLocked)` | method | Required before showing; bestMoves 0 → `best_none` ("최고 기록: 없음"); isLocked=true keeps PLAY interactable but `OnPlay` surfaces `toast.stage_locked` instead of entering (no dead button); difficulty tints ribbon: 0=amber(default), 1=teal, 2=crimson. Queries `StageDataService.GetStage` for difficulty label (`difficulty_fmt`) + color-type count (`types_fmt`) + gimmick badge toggles (overload/relay/locked-lane/blind-lane); toggles `_gimmickSection` off when none present |
| `ScrollStateCache.HomeScrollPosition` | prop | Float 0..1; save on leave, restore on enter |
| `ScrollStateCache.LastPlayedStageId` | prop | Set before entering InGame scene |
| `LobbyTab` | enum | Home / Shop / Ranking / Achievement |
| `LobbyView._achievementTabRoot` | SerializeField | Achievement tab root toggled by nav; `AchievementTabView` fetches OnEnable |
| `LobbyView.TryShowDailyAttendance()` | method | Once-per-session; auto-opens `AttendancePopupView` if today unclaimed (Start) |
| `LobbyView.ShowExitConfirm()` | method | Escape fallback (`UIManager.SetEscapeHandler` in OnEnable, cleared OnDestroy): `ConfirmDialogView` (`popup.exit.title`/`.body`, `common.btn_exit`/`btn_cancel`, danger) → confirm = `Application.Quit` |
| `RankingTabView._stageTabButton` / `_perfectTabButton` / `_weeklyTabButton` | SerializeField | 3 ranking tabs; types `stage`/`perfect`/`weekly`; share VirtualizedScrollRect + RankingItemView |
| `RankingTabView._descText` | SerializeField | Per-tab description line under the tab row; set in `Refresh` via `DescKey(rankingType)` |
| `CosmeticSectionView.Rebuild()` | method | Builds grid for current `CosmeticCategory`; preview built by `CosmeticPreview.Build` (layered procedural skin); toggles `Preview/SelectedHighlight` (IsActive) + `Preview/LockOverlay` (!Unlocked) per cell + `GoldPriceLabel.Set` (StateText GoldIcon, gold-unlock price only); cell tap → CosmeticPreviewPopupView |
| `CosmeticPreview.Build(img,item)` | method | Static; clears+rebuilds a `SkinPreview` child under the `Preview` Image (set first sibling, behind highlight/lock). Per category: Board=surface+edge+glints+mini chips, Lane=backlit column+stacked chips, Chip=central scaled-down chip (glow/fill/finish/rim). Fractional anchors → size-independent; chip kept to the central ~40% so the cell stays compact |
| `CosmeticSectionView.ResizeSectionToFit(count)` | method | Sizes section `LayoutElement.preferredHeight` to grid row count (rows×cellSize.y + spacing + header insets read from grid offsetMax/Min); outer Shop ScrollRect absorbs overflow — NO nested vertical scroll (same-axis conflict). Called per Rebuild so each category fits its own item count |
| `AvatarSectionView.GetAvatarSprite(int)` | method | Resolves avatar sprite from `_avatarSprites` mapping (populated by UIEditorSetup from avatar.csv) |
| `AvatarSectionView.ResizeSectionToFit(count)` | method | Sizes section `LayoutElement.preferredHeight` to grid row count; derives responsive columns from laid-out grid width (Flexible constraint); outer Shop scroll absorbs overflow |
| `AvatarPreviewPopupView.Init(avatarId,sprite,unlockCost,unlocked,onChanged)` | method | Required before showing; equip / buy-and-equip; `unlockCost<=0` && locked → reward-only (no buy); onChanged → section Rebuild. `GoldPriceLabel.Set` shows the StateText GoldIcon only on the gold-price branch (`!unlocked && !IsRewardOnly`) |
| `AchievementTabView.BindCell()` | method | Tier-colored badge + `RewardIcon` (representative reward via `RewardDisplay.RepresentativeRewardRender`; cosmetic unlocks prioritized), claim button vs completed label (no alpha dim); progress via `AnimatedProgressBar.SetProgress` |
| `RewardDisplay.RepresentativeRewardRender(achievementId)` | method | `Action<Image>` applied to the cell RewardIcon: cosmetics the achievement unlocks take priority (procedural `CosmeticPreview.Build`, no flat sprite), else reward group highest-`sort_order` flat icon; both pick highest `sort_order` (DESC); null if none |
| `AnimatedProgressBar.SetProgress(ratio,completed)` | method | Coroutine-lerps fill width, sets gradient base color; Update breathes a soft glow toward white; safe when inactive (applies instantly) |
| `LobbyBadgeContainer` | component | EventLayoutGroup spawns attendance + weekly-mission badges (open `AttendancePopupView` / `WeeklyMissionPopupView`) |
| `ShopTabView` | component | Shop screen; loads `iap_category` + `iap_product` CSVs; renders per-category section headers (divider+label) and product cards; hides exhausted/owned items. Hosts Product/Skin/Avatar sub-tabs |
| `ShopTabView.SwitchTab(ShopSubTab)` | method | Sets `_currentTab` + `ApplyTabVisibility` (cosmetic/avatar section SetActive + RefreshUI gate + tab colors); wired to `_productTabButton`/`_skinTabButton`/`_avatarTabButton` in Awake |
| `ShopTabView.InitializeCards()` | method | Rebuilds card tree grouped by category; creates headers, spacers, and cards in sort_order sequence |
| `ShopTabView.CreateCategoryHeader(cat)` | method | Programmatic HLG: left-divider + TMP label + right-divider; uses LocalizedText for language reload |
| `ShopTabView.CreateSpacer(height)` | method | Empty GO with LayoutElement.preferredHeight; separates category sections |
| `ParticleDir` | enum | Upward / Downward / Horizontal — particle movement direction per theme |
| `ChapterBgTheme.Get(themeId)` | method | 10-chapter loop: `((themeId-1)%10)+1` (11→1, 12→2…); themeId<=0 → neutral default. Chapters share one continuous bottom→top gradient (chN.Top == chN+1.Bottom, no seam jump) forming a full aurora hue wheel that wraps seamlessly (ch10.Top == ch1.Bottom): 1=cyan-blue 2=deep blue 3=violet 4=magenta 5=red-orange 6=amber 7=lime 8=green 9=teal-green 10=cyan→wrap. PathColor/ParticleColor neon-matched. ch5-10 use global `path_chapter` texture (no per-chapter asset) tinted by PathColor |
| `ChapterBackgroundView.CreateStars()` | method | Scatters Clamp(_height/110,16,70) star Images; sprite via `DynamicResourceService.GetSprite("led_star")`, fallback diamond (45°-rotated square); tint = theme.ParticleColor lerped toward white |
| `ChapterBackgroundView.CreateMotes()` | method | Clamp(_height/200,6,24) drift motes (key `deco_mote`, fallback square); slow upward drift + sway, wrap at top, soft twinkle alpha |
| `ChapterBackgroundView.CreateScanline()` | method | One full-width band (key `deco_scanline`, fallback box) sweeping top→bottom over 7–11s, sin alpha fade; color = theme.PathColor |
| `ChapterBackgroundView.AnimLoop()` | coroutine | Drives stars (sharp pulse), motes (drift), scanline (sweep); stops when culled via enabled toggle |
| `ChapterBackgroundView.YTop` | prop | Chapter top Y in content-root space; used by HomeTabView for viewport culling |
| `ChapterBackgroundView.YBot` | prop | Chapter bottom Y in content-root space |
| `ChapterBackgroundView.Bind(chapterId,bgThemeId,yAnchoredTop,height)` | method | Positions bg, creates decorations, starts animation coroutine |
| `HomeTabView.BuildChapterBackgrounds(positions,count)` | method | Groups stages by chapter_id, instantiates ChapterBackgroundView per chapter at sibling index 0 |
| `HomeTabView.StartSignalPulses(positions,count)` | method | 3 ambient pulse dots travelling node 0..currentStage along the straight trace (loop, per-segment chapter PathColor tint, fade at ends); sprite key `deco_pulse`→`led_star` fallback; restarted in OnEnable like the guide orb |
| `HomeTabView.CreateChestNode` | method | Instantiates a ChapterChestView prefab near the chapter-end stage node |
| `HomeTabView.OnChestTapped` | method | Invokes reward claim API; AddGold for SOFT_CURRENCY then builds RewardItems via `RewardDisplay.Build`; shows RewardPopupView |
| `ChapterChestView.SetState(ChestState)` | method | Configures sprites, button interactability, and glow overlays; Claimed blocks raycasts but keeps alpha=1 (no dim) |
| `ChapterChestView.SetClearedInfo(int,int)` | method | Updates `_clearedCountLabel` text to `"{cleared}/{total}"` |
| `ChapterChestView._clearedCountLabel` | SerializeField | TMP_Text in ClearedCountContainer child; shows chapter cleared-stage progress |
| `HomeTabView.GetChapterClearInfo(int)` | method | Returns (cleared, total) stages for chapterNum; used by RefreshChestNodes |

## Rules
- Scroll position must be saved in HomeTabView.OnDisable and restored in HomeTabView.OnEnable.
- StageNodeView pool size = GameConfig.StageNodePoolSize (24). Virtual scroll: OnScrolled binds visible nodes; position math uses full _stages.Length.
- Ranking tab is active; if `RankingApiService` is absent, show unavailable state without breaking lobby flow.

## Cross-refs
- Depends on: `Game.Core.UIManager`, `Game.Services.StageDataService`, `Game.Services.PlayerProgressService`, `Game.Services.RankingApiService`, `Game.Services.AdMobService`, `Game.InGame.View.BoardTheme`/`TextureFactory` (via `CosmeticPreview` — layered procedural cosmetic previews)
- Consumed by: InGame scene (reads ScrollStateCache.LastPlayedStageId)
