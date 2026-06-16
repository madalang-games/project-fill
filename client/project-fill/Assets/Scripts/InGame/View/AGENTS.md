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
| `StuckPopupView.cs` | `StuckPopupView` | Hard-Stuck rescue popup (AddLane-ad / Shuffle / Forfeit); `Configure(...)`; UIManager popup. |
| `ClearPopupView.cs` | `ClearPopupView` | Stage-clear popup (moves, Next / Lobby); `Configure(moves,onNext,onLobby)`; UIManager popup. |
| `PausePopupView.cs` | `PausePopupView` | Pause popup (Resume / Restart / Stage Select); `Configure(...)`; UIManager popup. |
| `TextureFactory.cs` | `TextureFactory` | Cached procedural sprites: RoundedRect/Outline/Glow/Disc/Ring/Circuit. |
| `BoardSkin.cs` | `BoardSkin`, `SpriteSet` | Optional sprite overrides (asset slot-in); `SpriteSet.Resolve` falls back to TextureFactory. |
| `UiUtil.cs` | `UiUtil` | Rect/Image/Label/Stretch/Anchors UI helpers (still used by HUD, popups, and fallbacks). |
| `BoardBackground.cs` | `BoardBackground` | Legacy serialized bg config (kept; referenced by `Editor/DebugSocketScale`). |
| `InGameSceneBackgroundView.cs` | `InGameSceneBackgroundView` | Stub (scene compat). |
| `CellView.cs` `ItemSlotView.cs` `HUDView.cs` `ResultOverlayView.cs` `FailOverlayView.cs` `ItemBuyConfirmPopupView.cs` | — | Legacy stubs (CellView used by `Core/UI/TutorialOverlay` via `BoardView.GetCellView`). |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BoardView.Init(board, def)` | method | Initializes resizer, builds lanes, and starts node lighting |
| `BoardView.SetWorldDimensions(w, ph, lh)` | method | Caches calculated World Space dimensions from the resizer |
| `BoardView.AnimateMove(from,to,chip,count,destBase,absorbed,onComplete)` | method | Teleport FX: source run shrinks-to-center + white flash then warps out (`VanishAll`); dest chips flash in white above slot, restore color (`MaterializeAll`), then gravity-drop (`DropAll`, ease-in fall, bottom-first stagger); landing slot read live (no snap); refreshes dest the instant chips land; then runs completion sweeps |
| `ChipView.SetFlash(a)` | method | White-tint overlay (0=normal,1=white) for the move teleport FX; `_flashing` makes `Update` cede visual control |
| `BoardView.Show{Stuck,Clear}Panel(...)` | method | Facade → `UIManager.ShowPopup<{Stuck,Clear}PopupView>` |
| `BoardView.SetBestMoves(int)` | method | Caches the stage personal best (controller-pushed) then redraws HUD; `UpdateHud` writes live `MoveCount`→`MovesText` and best→`BestText` (`-` if none) |
| `BoardView.On{LaneTapped,BoosterTapped,PauseTapped}` | event | Taps on lanes are caught via 2D physics raycasts |
| `LaneView.Initialize(i,sprites,lane,chipPrefab,size)` | method | Sets up BoxCollider2D size and builds procedural World layouts |
| `LaneView.SlotWorldPos(i)` / `ChipPixelSize()` | method | Return world-coordinate targets for moving chips |
| `SignalPanelView.Initialize(sprites,nodePrefab,types,relayOrder,w,h)` | method | Lays out nodes in World Space using panel dimensions |

## Rules
- **Hybrid Canvas/World Architecture**: Canvas owns UI Chrome (HUD, BoosterBar, Stuck/Clear/Pause popups). The gameplay board (Lanes, Chips, SignalPanel) lives in the 3D World Space scene under the `BoardWorldRoot` root object.
- **Aspect Sizing**: `BoardWorldResizer` dynamically moves and scales `BoardWorldRoot` and its children relative to `GameArea` Canvas bounds. Never hardcode absolute world bounds.
- **Input Checking**: Raycasting is handled by `BoardView.Update()` checking click position on `LaneView` colliders. Input is ignored if `EventSystem.current.IsPointerOverGameObject()` is true to prevent UI overlaps (e.g. Pause button, Booster Bar) from clicking world lanes.
- **Completed lane stays (spec A-R05)**: Emptied + reusable, never removed. Empty only after the sweep.
- `BuildLanes` lays lanes out as a **grid**: lane width is clamped to ≈0.30·container height (square chips, few lanes); when lanes would drop below ≈0.16·height they wrap into additional rows (balanced cols×rows), each row centered by its own count.
- Blind/Pending markers sit above the lane frame (not over the top slot) so they never cover the revealed top chip.
- Selection is highlight-only (glow/scale + border) — no vertical chip lift, so a batch pour starts evenly from slot positions.
- `LaneView.ShakeRoutine` (invalid-move) captures home position **live** at shake start, never a cached `_homePos` — Construct runs before BoardView assigns the lane position, so a cached home snaps the lane to the board origin (corrupts layout + tap colliders).
- Art direction is **color-token + pixel/casual**: `ChipView` renders the token body in the signal color with a bold dark outline (IC side-pins hidden); `SignalNodeView` lights as a glowing bulb (disc core + white-hot ring). Real sprites slotted into `BoardSkin`/`SpriteSet` drive the shape; these colors still tint them.
- Keep legacy stub views — removing `CellView`/`BoardView.GetCellView` breaks `Core/UI/TutorialOverlay`.
