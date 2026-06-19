# Scripts/Core/UI — Animation Components & Common Popups

Namespace: `Game.Core.UI`

## Files
| file | class | role |
|------|-------|------|
| `SceneBgPalette.cs` | `SceneBgPalette`, `BackgroundMode` | Circuit/neon dark palette per bg_theme_id × BackgroundMode (Default/Lobby/Night); Lobby/Night derive from neon accent 1=cyan 2=blue 3=violet 4=magenta (matches ChapterBgTheme); static Get(themeId,mode) |
| `SceneBackgroundView.cs` | `SceneBackgroundView` | Full-screen UI Canvas background for Boot/Lobby; Bind(themeId,mode); PanTo(tabIndex) for parallax tab switch |
| `UIEasing.cs` | `UIEasing` | Static easing functions: EaseOut, EaseIn, EaseInOut, EaseOutBack, Sine |
| `UIButtonAnimator.cs` | `UIButtonAnimator` | Press/release/CTA-idle scale animation; SetInteractable(bool) |
| `UIFloatAnimation.cs` | `UIFloatAnimation` | Gentle sine float loop; amplitude, period, random phase offset |
| `UIScalePulse.cs` | `UIScalePulse` | Breathing scale loop; minScale/maxScale/period |
| `UIPanelAppear.cs` | `UIPanelAppear` | Standard appear (scale 0.85→1 + alpha) / Disappear coroutines |
| `UICountUp.cs` | `UICountUp` | Animates TMP_Text number from 0 → target with ease-out |
| `UINumberChange.cs` | `UINumberChange` | Attach to any integer TMP_Text; Set(int) plays punch-scale + color flash (red=decrease, green=increase); silent on first call; SetRaw(string) for non-integer display (∞, etc.) |
| `UIStarPop.cs` | `UIStarPop` | Sequence-pops star GameObjects with EaseOutBack + PunchScale bell-curve; unfilled stars instant; stagger=0.25s |
| `UIScreenShake.cs` | `UIScreenShake` | RectTransform sine shake: Medium (6dp/200ms) or Heavy (10dp/350ms) |
| `ConfirmDialogView.cs` | `ConfirmDialogView` | Generic binary confirm: title, body, cancel/confirm callbacks; danger variant |
| `ToastView.cs` | `ToastView` | Slide-up snackbar; 2.5s display; Warning/Success/Error icons |
| `LoadingOverlayView.cs` | `LoadingOverlayView` | Full-screen dim + spinner; 10s timeout → ShowNetworkError |
| `RewardPopupView.cs` | `RewardPopupView` | Sequential reward item pop (max 4); RewardItem struct (Icon, Quantity, NameKey, DescKey) |
| `RewardItemCellView.cs` | `RewardItemCellView` | MonoBehaviour on RewardItemCell prefab; detects 0.4s long-press; shows ItemTooltipView via UIManager |
| `ItemTooltipView.cs` | `ItemTooltipView` | Tooltip popup (Icon 96×96, Title, Desc); dynamic screen-clamped positioning; tap backdrop to close |
| `LongPressTooltipTrigger.cs` | `LongPressTooltipTrigger` | Generic 0.4s long-press → `ItemTooltipView` trigger; attach to any icon GO, `SetTooltip(nameKey,descKey)`; tooltip sprite read from assigned `_icon`. Decoupled twin of the long-press in `RewardItemCellView` |
| `NetworkErrorView.cs` | `NetworkErrorView` | Retry button; 3+ failures shows persistent message |
| `PerfectClearEffectView.cs` | `PerfectClearEffectView` | 3-star only; "Perfect!" text pop + confetti + wobble (2s) |
| `ChapterUnlockOverlayView.cs` | `ChapterUnlockOverlayView` | Full-screen 2.7s chapter unlock animation; blocks interaction |
| `LocalizedText.cs` | `LocalizedText` | Attach to TMP_Text; with stringId → text+font switch on language change; without stringId → font-only (dynamic text) |
| `UITextStyle.cs` | `UITextStyle` | Component; dynamically applies bold face dilate and drop shadows (underlays) to TMPro text based on parent background color |
| `TutorialOverlay.cs` | `TutorialOverlay` | Canvas-based full-screen tutorial spotlight/tooltip overlay |
| `TutorialTarget.cs` | `TutorialTarget` | Attach to any scene/UI GameObject; registers itself by `_targetId` so TutorialOverlay resolves targets without name coupling |
| `UIPulseGlowEffect.cs` | `UIPulseGlowEffect` | Visual micro-animation component handling scale pulsing and rotational glow |
| `UIVerticalGradient.cs` | `UIVerticalGradient` | MaskableGraphic 2-color vertical gradient via vertex colors; `SetColors(top,bottom)` |
| `UILineStrip.cs` | `UILineStrip` | MaskableGraphic polyline mesh (chapter map path): textured/scrolling/outlined/procedural-dash; `SetPoints`/`SetTexture`. `activeLength`+`inactiveColor` dim the locked tail with a hard cutoff at the boundary node — a segment lights only if its far end is within `activeLength`, so the route past the current challenge node (→ first locked node) stays fully unlit (HomeTabView lights the trace only up to the furthest unlocked node) |

## Scene Background Symbols
| symbol | kind | note |
|--------|------|------|
| `BackgroundMode` | enum | Default (Boot circuit dusk) / Lobby (chapter neon theme) / Night (darker variant; no caller yet — InGame bg is a stub) |
| `SceneBgPalette.Get(themeId,mode)` | method | Returns palette for given theme × mode; Default overrides themeId |
| `SceneBackgroundView.Bind(bgThemeId,mode)` | method | Creates gradient + decorations; starts anim coroutine |
| `SceneBackgroundView.PanTo(tabIndex,duration)` | method | Smooth parallax pan; tabIndex 0=Home 1=Shop 2=Ranking; default duration 0.65s |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UIButtonAnimator._isCTA` | SerializeField | Enables idle breathing animation on UI_CTA buttons |
| `UIButtonAnimator._clickSfxId` | SerializeField | SfxId played on pointer down; default `ConfirmClick`; set `CancelClick` for close/cancel buttons |
| `UIButtonAnimator.SetInteractable(bool)` | method | Dims opacity 40% + stops idle anim when false |
| `UIPanelAppear.Disappear(Action)` | method | Triggers disappear coroutine; calls onComplete when done |
| `UICountUp.Play(int,int,Action)` | method | Animates from→to; optional completion callback |
| `UINumberChange.Set(int)` | method | Sets text + punch+flash anim; silent on first call; debounced (no-op if same value) |
| `UINumberChange.SetRaw(string)` | method | Non-integer display (∞ etc.); resets tracking so next Set() animates |
| `UINumberChange._formatString` | SerializeField | Printf-style format; default `"{0}"`; set `"{0:N0}"` for gold |
| `UIStarPop.PlayStarSequence(GameObject[],int)` | coroutine | All stars shown (empty); earned Fill children pop left-to-right independently; EaseOutBack fill + bell-curve PunchScale on star GO; stagger=0.25s |
| `UIScreenShake.Shake(ShakeLevel)` | method | Medium or Heavy; resets to origin on complete |
| `ConfirmDialogView._closeButton` | SerializeField | Square top-right ✕ button; calls OnCancel (same as _backdropButton) |
| `ConfirmDialogView.Init(title,body,confirmLabel,onConfirm,onCancel,cancelLabel,danger)` | method | Required before showing |
| `ToastView.Show(string,ToastType)` | method | Replaces existing toast |
| `LoadingOverlayView.Show(string?)` | method | Optional message text |
| `RewardPopupView._closeButton` | SerializeField | Square top-right ✕ button; calls Close() same as _okButton |
| `RewardPopupView.Init(IReadOnlyList<RewardItem>)` | method | Required before showing; calls RewardItemCellView.Init() per cell |
| `NetworkErrorView.Show(Action)` | method | onRetry callback; increments failure counter |
| `ChapterUnlockOverlayView.Play(int,Action)` | method | chapterNumber + onComplete callback |
| `RewardItem` | struct | `Sprite Icon`, `int Quantity`, `string Label`, `string NameKey`, `string DescKey`, `Action<Image> CustomRender` (optional; when set, renders the row Icon via this delegate instead of a flat sprite — keeps Core.UI free of OutGame deps; cosmetics inject `CosmeticPreview.Build`) |
| `RewardItemCellView.Init(Sprite,string,string)` | method | Sets icon + nameKey + descKey; required for tooltip to show |
| `ItemTooltipView.Init(Sprite,string,string,Vector3)` | method | icon, nameKey, descKey, cellScreenPos; positions panel clamped to screen |
| `LongPressTooltipTrigger.SetTooltip(nameKey,descKey)` | method | Sets the localization keys shown after 0.4s hold; no-op press if both empty |
| `LongPressTooltipTrigger._icon` | SerializeField | Image whose sprite becomes the tooltip icon |
| `ToastType` | enum | Warning / Success / Error |
| `LocalizedText._stringId` | SerializeField | Key from client_string.csv; empty = font-only mode |
| `LocalizedText.RefreshAllInEditor()` | static method | Editor-only; reads CSV, updates all LocalizedText TMP text to EN preview |
| `UITextStyle.ApplyStyle()` | method | Applies face dilate and underlay drop shadow parameters (dynamic or local edit-mode material) |
| `TutorialOverlay.Init(TutorialStepSequencer)` | method | Hooks up sequencer events and displays the first/current tutorial step |
| `TutorialTarget._targetIds` | SerializeField | String array; each entry must match a `target_ui_id` in tutorial_step.csv — one component, multiple ids |
| `TutorialTarget.Find(string)` | static method | Returns registered TutorialTarget for given id; null if not registered |
| `TutorialTarget.SetIds(params string[])` | method | Runtime id (re)assignment + re-register; BoardView tags each lane `slot_lane_{n}` on spawn |

## Rules
- Attach `UIButtonAnimator` to every tappable button
- Attach `UIPanelAppear` to all popups and overlays
- `LoadingOverlayView` auto-calls `UIManager.ShowNetworkError` after 10s — do not add separate timeout
- `ChapterUnlockOverlayView.Play` blocks interaction via GraphicRaycaster disable; restores on complete
- Instantiate `TutorialOverlay` prefab and call `Init(sequencer)` to start/display a tutorial sequence overlay
- `TutorialOverlay` behavior keys on `step.advance_mode`: **Tap** (info) = `_dimLayer.raycastTarget=true` + `_fullscreenDismissButton` active → tap advances + scene blocked; **Select/Move** (action) = dim raycast OFF + dismiss OFF so the real board tap passes through, advances via `TutorialManager.NotifyAction` from the controller. World scene input gated by `TutorialManager.BoardInputBlocked` (true only on Tap steps). Spotlight resolves the target via `TutorialTarget` (UI RectTransform or World transform→`Camera.main` projection: Collider2D bounds for lanes, else **encapsulated child-renderer bounds** so a multi-part target like the whole Signal Panel is fully covered). Highlight is a **dim-hole** — 4 runtime border quads (`CreateDimBorders`) frame the target so it shows through the dim (no yellow glow pulse; legacy `_spotlightCutout`/`_spotlightGlow` forced off). `BlinkHole` does a quick 3× reveal blink (`_holeScale`) on each step appear, then holds open; no-target steps use the full `_dimLayer`. `DragPointer` content reuses `_fingerOverlay` (sprite swapped to `ui_drag_pointer`, loaded via `DynamicResourceService`) sliding `target_ui_id`→`target_ui_id_to`. Tooltip auto-places on the freer side, never overlapping the hole, clamped on-screen (`PlaceTooltip`).
- `TutorialTarget`: attach to Board, TurnsBubble, ProgressContainer etc. with `_targetId` matching CSV `target_ui_id`; lookup order in TutorialOverlay: TutorialTarget registry → board_cell_ parse → board protector/core/obstacle scan
- Attach `UIPulseGlowEffect` to active reward buttons (e.g. claimable chest glow) to animate pulses and highlight claimability

## Cross-refs
- Consumed by: `Game.Core.UIManager`, all scene entry points
