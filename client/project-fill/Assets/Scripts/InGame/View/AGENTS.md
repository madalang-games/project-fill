# Scripts/InGame/View

Signal Sort board views. UI Chrome (HUD, Booster Bar, Stuck/Clear/Pause popups) is **authored on the InGame scene canvas via `UIEditorSetup.SetupInGame`**. The core gameplay board (Lanes, Chips, Signal Panel) is placed in **World Space** under `BoardWorldRoot`, managed by `BoardWorldResizer` matching Canvas screen constraints. Visuals stay procedural via `WorldUtil` using `SpriteRenderer` and `TextMeshPro` 3D.

## Files
| file | class | role |
|------|-------|------|
| `BoardView.cs` | `BoardView` | Binder on InGame canvas: handles dynamic resolution of scene World Space containers, runs moves and completion sweeps, and implements BoxCollider2D physics raycast input checks with EventSystem overlay protection. |
| `BoardWorldResizer.cs` | `BoardWorldResizer` | Resolves screen corners of `GameArea` (RectTransform), maps them to world coords via main Orthographic camera, repositions `BoardWorldRoot` and sizes Lanes/Panel heights dynamically. |
| `WorldUtil.cs` | `WorldUtil` | Static helpers to build World Space GameObjects, sliced `SpriteRenderer` layers, and 3D `TextMeshPro` text components procedurally. |
| `LaneView.cs` | `LaneView` | One World Space lane: has a `BoxCollider2D` for click raycasts, procedurally spawns child sockets, locks, markers, and stacked `ChipView`s. |
| `ChipView.cs` | `ChipView` | One World Space chip: composed of `SpriteRenderer` layers (glow, outline, fill, blind circuit-back) and a 3D `TextMeshPro` glyph label. |
| `SignalNodeView.cs` | `SignalNodeView` | One World Space Signal Panel node: glowing pad with 3D text and connector line `SpriteRenderer` layers. |
| `SignalPanelView.cs` | `SignalPanelView` | Binder on the World Space panel; spawns and positions nodes based on world dimensions. |
| `ResultOverlayView.cs` | `ResultOverlayView`, `ClearSummary` | Stage-clear overlay: clear summary (moves/best/new-best badge via `ClearSummary?`, hidden for daily challenge), granted rewards (RewardItemCell rows via `RewardDisplay`), Double-Reward rewarded-ad (`AdMobService`→`AdApiService.ClaimDoubleReward`, doubles amounts on grant), Next/Map; `Configure(stageId,attemptId,rewards,canDouble,summary,onNext,onLobby)`; UIManager popup. |
| `FailOverlayView.cs` | `FailOverlayView` | Hard-Stuck rescue overlay (AddLane-ad / Shuffle+price / Forfeit); `Configure(addLaneAvailable,onAddLane,onShuffle,onGiveUp)`; UIManager popup. AddLane carries a "Watch Ad" badge; Shuffle spends gold → gates on an in-panel confirm (`_shuffleConfirmPanel`, no second UIManager popup → no close-stack race). The only fail path in Signal Sort is being stuck. |
| `PausePopupView.cs` | `PausePopupView` | Pause popup (Resume / Restart / Stage Select); `Configure(...)`; UIManager popup. |
| `TextureFactory.cs` | `TextureFactory` | Cached procedural sprites: RoundedRect/Outline/Glow/Disc/Ring/Circuit. |
| `BoardSkin.cs` | `BoardSkin`, `SpriteSet` | Optional sprite overrides (asset slot-in); `SpriteSet.Resolve` falls back to TextureFactory. |
| `UiUtil.cs` | `UiUtil` | Rect/Image/Label/Stretch/Anchors UI helpers (still used by HUD, popups, and fallbacks). |
| `BoardBackground.cs` | `BoardBackground` | Legacy serialized bg config (kept; referenced by `Editor/DebugSocketScale`). |
| `BoardBgTheme.cs` | `BoardBgTheme`, `BoardDecoKind` | Board-skin cosmetic palette + deco template per `board_*` cosmetic_id (Top/Bottom/Accent + Kind NeonGrid/Minimal/Quantum/Retro); `Get(boardSkinId)`, unknown→`board_default` |
| `InGameSceneBackgroundView.cs` | `InGameSceneBackgroundView` | InGame board background: renders active Board Skin (`BoardBgTheme`) as gradient + ambient deco (grid/motes/scanline/twinkle) per Kind; sprites via DynamicResourceService (`deco_mote`/`deco_scanline`/`led_star`) + code fallback; `Apply(boardSkinId)`, self-resolves `CosmeticState.ResolveBoardSkin()` on Start (fetch-if-uncached). Must sit on a Canvas rendering behind the World board. |
| `CellView.cs` `ItemSlotView.cs` `HUDView.cs` `ItemBuyConfirmPopupView.cs` | — | Legacy stubs (CellView used by `Core/UI/TutorialOverlay` via `BoardView.GetCellView`). |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BoardView.Init(board, def)` | method | Initializes resizer, builds lanes, and starts node lighting |
| `BoardView.SetWorldDimensions(w, ph, lh)` | method | Caches calculated World Space dimensions from the resizer |
| `BoardView.AnimateMove(from,to,chip,count,destBase,absorbed,onComplete)` | method | Teleport FX: source run shrinks-to-center + white flash then warps out (`VanishAll`); dest chips flash in white above slot, restore color (`MaterializeAll`), then gravity-drop (`DropAll`, ease-in fall, bottom-first stagger); landing slot read live (no snap); refreshes dest the instant chips land; then runs completion sweeps |
| `ChipView.SetFlash(a)` | method | White-tint overlay (0=normal,1=white) for the move teleport FX; `_flashing` makes `Update` cede visual control |
| `BoardView.ShowStuckPanel(...)` | method | Facade → `UIManager.ShowPopup<FailOverlayView>` (runtime fallback overlay if no UIManager) |
| `BoardView.ShowClearPanel(stageId,attemptId,rewards,canDouble,summary,onNext,onLobby)` | method | Facade → `UIManager.ShowPopup<ResultOverlayView>`; passes granted rewards + double-reward eligibility + `ClearSummary?` (moves/best/new-best; null for daily challenge) |
| `BoardView.SetBestMoves(int)` | method | Caches the stage personal best (controller-pushed) then redraws HUD; `UpdateHud` writes live `MoveCount`→`MovesText` and best→`BestText` (`-` if none) |
| `BoardView.On{LaneTapped,BoosterTapped,PauseTapped}` | event | Taps on lanes are caught via 2D physics raycasts |
| `LaneView.Initialize(i,sprites,lane,chipPrefab,size)` | method | Sets up BoxCollider2D size and builds procedural World layouts |
| `LaneView.SlotWorldPos(i)` / `ChipPixelSize()` | method | Return world-coordinate targets for moving chips |
| `SignalPanelView.Initialize(sprites,nodePrefab,types,relayOrder,w,h)` | method | Lays out nodes in World Space using panel dimensions |
| `SignalPanelView.TryGetTraceTarget(type,out from,out to)` | method | World segment node→next-node for the register light-pulse trace; false for last node |

## Rules
- **Hybrid Canvas/World Architecture**: Canvas owns UI Chrome (HUD, BoosterBar, Stuck/Clear/Pause popups). The gameplay board (Lanes, Chips, SignalPanel) lives in the 3D World Space scene under the `BoardWorldRoot` root object.
- **Aspect Sizing**: `BoardWorldResizer` dynamically moves and scales `BoardWorldRoot` and its children relative to `GameArea` Canvas bounds. Never hardcode absolute world bounds.
- **Input Checking**: Raycasting is handled by `BoardView.Update()` checking click position on `LaneView` colliders. Input is ignored if `EventSystem.current.IsPointerOverGameObject()` is true to prevent UI overlaps (e.g. Pause button, Booster Bar) from clicking world lanes.
- **Completed lane stays (spec A-R05)**: Emptied + reusable, never removed. Empty only after the sweep.
- `BuildLanes` lays lanes out as a **fixed-size grid**: lane W/H/gap are serialized constants (`_laneWidth`/`_laneHeight`/`_laneGap`), never stretched to fill. Columns wrap past `_maxColumns` into balanced rows (each row centered by its own count). The whole grid is then **uniformly shrunk** (never enlarged) to fit the container via `_lanesContainer.localScale` (`_lanesScale`). Flight FX spawn on the unscaled `_flightLayer`, so chip flight size is multiplied by `_lanesScale`.
- Blind/Pending markers sit above the lane frame (not over the top slot) so they never cover the revealed top chip.
- **Gimmick state is text-free (icon + FX only)** — no localized/hardcoded words, no emoji (avoids font-subset tofu): **Locked** = `TextureFactory.Padlock` (slot-in `BoardSkin.lockSeal`) tinted to `UnlockType` color (color = which signal opens it); unlock plays `UnlockRoutine` shrink-dissolve via `Refresh` lock→unlock transition. **Blind** = "?" disc badge + `_scanLine` sweep animated over the masked stack in `Update`. **Relay pending** = `_pips` queue dots (filled = `PendingNumber` steps until next; ≤1 = green "up next", clamps at `MaxPips`) + amber border pulse; order itself is read from the Signal Panel node sequence. **Overload** = chip-level orange glow + orange pins (`ChipView`, already text-free).
- Selection is highlight-only (glow/scale + border) — no vertical chip lift, so a batch pour starts evenly from slot positions. Select fires a one-shot scale **punch** (`ChipView._punch`) that decays into the steady glow/scale pulse.
- `TextureFactory.Glow` is a **soft radial** sprite (small bright core → transparent at the edge, NOT 9-sliced) so it reads as a compact halo; an oversized/hard glow blooms over neighbors. Tint-able white. Glow render size lives at the call site (proportional to node/chip size).
- **Signal Panel nodes are fixed LED-size**: `SignalPanelView` caps node diameter to `panelHeight * 0.42` (clamped to cell width), NOT stretched to fill the wide cell — keeps lit disc/ring/glow compact. `SignalNodeView` glow is `size * 1.2` and the register pop is a gentle blink (`*0.18`), not a swell.
- **`SignalNodeView` disc/ring scale must use the captured base scale** (`_bodyBase`/`_ringBase`), never `Vector3.one`: they are NON-9-sliced sprites, so `WorldUtil.CreateSprite` sets their `localScale` to fit `size` (≈0.3). Writing `Vector3.one` (old `PopRoutine` bug) blew the LED up ~3× into neighbors. State changes lerp colors toward targets (`_tBody`/`_tRing`/…) in `Update` for a smooth neon transition; register fires a decaying glow `_flash`. **Lit (completed) nodes shimmer**: `Update` oscillates the body/ring white-blend within the signal color's range (`Mathf.Sin`) for a breathing finished state, and the lit glyph recedes (`_tGlyph` faint alpha + `_glyphBase*0.7` shrink) so the colorblind legend stays without fighting the color FX.
- Impact accents (all procedural sprite coroutines on `_flightLayer`, self-destroy; no Unity ParticleSystem):
  - **Chip landing** (`DropAll`): `SpawnRing` with `flattenY` (ground-hugging shockwave) + `SpawnMotes` (small upward dust cone, arcs down via `BurstParticle` gravity). Kept subtle — fires per chip, high frequency.
  - **Node register** (`CompleteSweep`): double `SpawnRing` (inner crisp + outer wide) + `SpawnShards` (rounded-square pixel "data bits" burst radially, spin + fall) + `SpawnTrace` (light pulse runs along the connector to the next node via `SignalPanelView.TryGetTraceTarget`, false for last node) + node `PlayRegister` glow. Climactic, low frequency.
  - `SpawnRing(...flattenY=1f)` squashes Y when <1; `BurstParticle` is shared (motes); `ShardParticle`/`TraceRoutine` are register-only.
- `LaneView.ShakeRoutine` (invalid-move) captures home position **live** at shake start, never a cached `_homePos` — Construct runs before BoardView assigns the lane position, so a cached home snaps the lane to the board origin (corrupts layout + tap colliders).
- Art direction is **color-token + pixel/casual**: `ChipView` renders the token body in the signal color with a bold dark outline (IC side-pins hidden); `SignalNodeView` lights as a glowing bulb (disc core + white-hot ring). Real sprites slotted into `BoardSkin`/`SpriteSet` drive the shape; these colors still tint them.
- Chip outline and lane outline are **separate `BoardSkin` slots**: `chipOutline` → `ChipView` border only; `laneOutline` → `LaneView` frame/sockets/insert. Both fall back to the same `RoundedOutline` but slot independently.
- Keep legacy stub views — removing `CellView`/`BoardView.GetCellView` breaks `Core/UI/TutorialOverlay`.
