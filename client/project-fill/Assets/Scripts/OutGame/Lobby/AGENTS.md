# OutGame/Lobby - Lobby Scene

Namespace: `Game.OutGame.Lobby`

## Files
| file | class | role |
|------|-------|------|
| `LobbyView.cs` | `LobbyView` | Root lobby controller; shows/hides tabs; refreshes gold |
| `HeaderView.cs` | `HeaderView` | Avatar tap → AccountPopup; Gold display |
| `BottomNavBarView.cs` | `BottomNavBarView` | 3-tab nav; fires OnTabChanged |
| `RankingTabView.cs` | `RankingTabView` | Ranking tab UI; 3 tabs stage(max reach)/perfect(perfect-clear count)/weekly(this-week clears) + per-tab desc line, my rank, virtualized list via VirtualizedScrollRect. `challenge` is an internal mode (no tab button) reached only via DailyChallengePopup |
| `RankingItemView.cs` | `RankingItemView` | Component on RankingItemPrefab; Bind(entry,avatar,score) + SetHighlight(bool) for MyRankPin highlight |
| `HomeTabView.cs` | `HomeTabView` | Chapter/stage scroll with object pool; milestone chest rendering + claim API; out-of-life ad prompt uses AdMobService token |
| `ChapterChestView.cs` | `ChapterChestView` | Milestone chest node displaying Locked (INACTIVE), Claim! (ACTIVE), or Cleared (CLAIMED) states |
| `StageNodeView.cs` | `StageNodeView` | Pooled node: stage label, cleared badge, lock overlay, pulse ring; Bind(id,cleared,unlocked,current) |
| `StageInfoPopupView.cs` | `StageInfoPopupView` | Stage info popup: title, best moves (campaign ranking metric), difficulty label, signal-type count, gimmick badges (icon-only, long-press tooltip), PLAY button |
| `ScrollStateCache.cs` | `ScrollStateCache` | Static session memory: HomeScrollPosition, LastPlayedStageId |
| `ChapterBgTheme.cs` | `ChapterBgTheme` | Static theme config per `bg_theme_id`; colors + particle params |
| `ChapterBackgroundView.cs` | `ChapterBackgroundView` | Chapter scroll decoration tinted per theme: blinking star LEDs + drifting glow motes + scanline sweep band (gradient itself drawn by HomeTabView). Sprites via DynamicResourceService (led_star/deco_mote/deco_scanline), code fallback each |
| `ShopTabView.cs` | `ShopTabView` | Shop tab UI; groups IAP cards by `iap_category`, renders divider+label headers, handles purchase limits and dynamic status; keeps `_cosmeticSection` then `_avatarSection` last siblings below IAP cards |
| `RewardDisplay.cs` | `RewardDisplay` | Static helper; converts `GrantedRewardDto[]` → `RewardPopupView.RewardItem[]` (gold/item/avatar/no-ads icons). Shared by attendance/achievement/challenge claim flows |
| `CosmeticSectionView.cs` | `CosmeticSectionView` | Shop cosmetic section: Chip/Lane/Board category tabs + grid; canonical board-customization surface (unified — replaced AccountPopup board tab) |
| `CosmeticPreviewPopupView.cs` | `CosmeticPreviewPopupView` | Cosmetic preview popup: preview/name/desc + buy-and-apply / apply via `CosmeticApiService` |
| `AvatarSectionView.cs` | `AvatarSectionView` | Shop avatar section: horizontal scroll carousel of avatar cards (loads `avatar.csv`); card tap → `AvatarPreviewPopupView`. Uses existing avatar API (NOT cosmetic system); holds `_avatarSprites` mapping |
| `AvatarPreviewPopupView.cs` | `AvatarPreviewPopupView` | Avatar preview popup: preview + cost/state + equip / buy-and-equip via `PlayerApiService.UpdateProfile` + local gold |
| `AttendancePopupView.cs` | `AttendancePopupView` | Daily attendance popup: 7 day cards (claimed/today/future), today reward preview, claim CTA → RewardPopup; auto-opened from `LobbyView` |
| `AchievementTabView.cs` | `AchievementTabView` | Achievement lobby tab (right of Ranking): 4 category tabs + scroll list (tier badge, progress bar, claim). Migrated from former AchievementListPopupView |
| `AchievementToastView.cs` | `AchievementToastView` | Slide-down achievement-unlocked toast (tier badge + name, 3s); `Show(nameKey,tier)` via ShowOverlay — gameplay trigger is InGame seam |
| `AnimatedProgressBar.cs` | `AnimatedProgressBar` | Sprite-free progress bar (width-driven via anchorMax.x, no fillAmount); animated fill-up + ratio gradient color + soft continuous glow (breathe toward white, stronger when completed); wired by `UIEditorSetup.BuildAchievementCell` |
| `DailyChallengePopupView.cs` | `DailyChallengePopupView` | Daily challenge entry popup: date/difficulty/participants/streak; [Start]→`ChallengeContext.Set` + load InGame (disabled if already cleared today), [Ranking]→Ranking Challenge tab |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `LobbyView._header` | SerializeField | HeaderView ref |
| `LobbyView._rankingTabRoot` | SerializeField | Ranking tab root toggled by nav |
| `LobbyView._rankingTabView` | SerializeField | Ranking tab refresh target |
| `BottomNavBarView.OnTabChanged` | event | `Action<LobbyTab>` |
| `BottomNavBarView.SelectTab(LobbyTab)` | method | Public; sets highlight |
| `BottomNavBarView._achievementButton` | SerializeField | 4th nav tab (right of Ranking); fires `LobbyTab.Achievement` |
| `RankingTabView.Refresh` | method | Fetches page + my rank via `RankingApiService` |
| `RankingTabView._myRankPin` | SerializeField | `RankingItemView` ref; SetHighlight(true) distinguishes from list items |
| `RankingItemView.Bind(entry,avatarSprite,scoreSprite)` | method | Populates all text/icon fields from RankingEntryDto |
| `RankingItemView.SetHighlight(bool)` | method | Switches background color: normal purple vs gold CTA highlight |
| `HomeTabView` | component | OnEnable refreshes pool; OnDisable saves scroll position |
| `StageNodeView.Bind(id,cleared,unlocked,isCurrent,chapterId,difficulty)` | method | Updates visual states; `_clearedBadge` on `cleared && unlocked`; toggles `_lockOverlay` on `!unlocked`; difficulty 0=Easy(no outline), 1=Normal(neon blue), 2=Hard(neon red+skull) |
| `StageNodeView.OnTapped` | event | `Action<int>` stageId |
| `StageInfoPopupView.Init(stageId,bestMoves,onPlay,difficulty,isLocked)` | method | Required before showing; bestMoves 0 → "-"; isLocked=true disables PlayButton; difficulty tints ribbon: 0=amber(default), 1=neon blue, 2=coral red. Queries `StageDataService.GetStage` for difficulty label + signal-type count + gimmick badge toggles (overload/relay/locked-lane/blind-lane) |
| `ScrollStateCache.HomeScrollPosition` | prop | Float 0..1; save on leave, restore on enter |
| `ScrollStateCache.LastPlayedStageId` | prop | Set before entering InGame scene |
| `LobbyTab` | enum | Home / Shop / Ranking / Achievement |
| `LobbyView._achievementTabRoot` | SerializeField | Achievement tab root toggled by nav; `AchievementTabView` fetches OnEnable |
| `LobbyView.GoToRankingChallenge()` | method | Switches to Ranking tab + selects Challenge sub-tab; called by `DailyChallengePopupView` |
| `LobbyView.TryShowDailyAttendance()` | method | Once-per-session; auto-opens `AttendancePopupView` if today unclaimed (Start) |
| `RankingTabView.SelectChallenge()` | method | Public; switches to internal `_rankingType="challenge"` (daily-challenge ranking via `DailyChallengeApiService`, score=moves_used); no tab button — entry only from DailyChallengePopup |
| `RankingTabView._stageTabButton` / `_perfectTabButton` / `_weeklyTabButton` | SerializeField | 3 ranking tabs; types `stage`/`perfect`/`weekly`; share VirtualizedScrollRect + RankingItemView |
| `RankingTabView._descText` | SerializeField | Per-tab description line under the tab row; set in `Refresh` via `DescKey(rankingType)` |
| `CosmeticSectionView.Rebuild()` | method | Builds grid for current `CosmeticCategory`; cell tap → CosmeticPreviewPopupView |
| `AvatarSectionView.GetAvatarSprite(int)` | method | Resolves avatar sprite from `_avatarSprites` mapping (populated by UIEditorSetup from avatar.csv) |
| `AvatarPreviewPopupView.Init(avatarId,sprite,unlockCost,unlocked,onChanged)` | method | Required before showing; equip / buy-and-equip; onChanged → section Rebuild |
| `AchievementTabView.BindCell()` | method | Tier-colored badge, claim button vs completed label (no alpha dim); progress via `AnimatedProgressBar.SetProgress` |
| `AnimatedProgressBar.SetProgress(ratio,completed)` | method | Coroutine-lerps fill width, sets gradient base color; Update breathes a soft glow toward white; safe when inactive (applies instantly) |
| `LobbyBadgeContainer` | component | EventLayoutGroup now spawns attendance + challenge badges (open respective popups) |
| `ShopTabView` | component | Shop screen; loads `iap_category` + `iap_product` CSVs; renders per-category section headers (divider+label) and product cards; hides exhausted/owned items |
| `ShopTabView.InitializeCards()` | method | Rebuilds card tree grouped by category; creates headers, spacers, and cards in sort_order sequence |
| `ShopTabView.CreateCategoryHeader(cat)` | method | Programmatic HLG: left-divider + TMP label + right-divider; uses LocalizedText for language reload |
| `ShopTabView.CreateSpacer(height)` | method | Empty GO with LayoutElement.preferredHeight; separates category sections |
| `ParticleDir` | enum | Upward / Downward / Horizontal — particle movement direction per theme |
| `ChapterBgTheme.Get(themeId)` | method | Returns theme config; chapters share one continuous bottom→top gradient (chN.Top == chN+1.Bottom, no seam jump). Circuit/aurora hue sweep: 1=cyan-blue 2=deep blue 3=violet 4=magenta→pink. PathColor/ParticleColor are neon-matched |
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
| `HomeTabView.OnChestTapped` | method | Invokes reward claim API; builds RewardItems via CurrencyDataService+ItemDataService+DynamicResourceService; shows RewardPopupView |
| `ChapterChestView.SetState(ChestState)` | method | Configures sprites, button interactability, and glow overlays; Claimed blocks raycasts but keeps alpha=1 (no dim) |
| `ChapterChestView.SetClearedInfo(int,int)` | method | Updates `_clearedCountLabel` text to `"{cleared}/{total}"` |
| `ChapterChestView._clearedCountLabel` | SerializeField | TMP_Text in ClearedCountContainer child; shows chapter cleared-stage progress |
| `HomeTabView.GetChapterClearInfo(int)` | method | Returns (cleared, total) stages for chapterNum; used by RefreshChestNodes |

## Rules
- Scroll position must be saved in HomeTabView.OnDisable and restored in HomeTabView.OnEnable.
- StageNodeView pool size = GameConfig.StageNodePoolSize (24). Virtual scroll: OnScrolled binds visible nodes; position math uses full _stages.Length.
- Ranking tab is active; if `RankingApiService` is absent, show unavailable state without breaking lobby flow.

## Cross-refs
- Depends on: `Game.Core.UIManager`, `Game.Services.StageDataService`, `Game.Services.PlayerProgressService`, `Game.Services.RankingApiService`, `Game.Services.AdMobService`
- Consumed by: InGame scene (reads ScrollStateCache.LastPlayedStageId)
