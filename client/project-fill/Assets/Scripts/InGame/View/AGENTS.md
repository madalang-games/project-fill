# Scripts/InGame/View

Runtime-built Signal Sort board UI + procedural art. The board canvas is generated entirely in code
(no `.prefab`), so these views are NOT created via UIEditorSetup.

## Files
| file | class | role |
|------|-------|------|
| `BoardView.cs` | `BoardView` | Builds canvas/HUD/panel/lanes/boosters; move flight + complete-sweep + particle FX; stuck/clear overlays; chapter-cycle dev buttons |
| `LaneView.cs` | `LaneView` | One lane: card-rack body, 4 sockets, stacked `ChipView`s, lock/blind/pending decorations, shake/unlock anims |
| `ChipView.cs` | `ChipView` | One chip: tinted rounded body, neon outline+glow, glyph, blind circuit-back + flip, overload pulse |
| `SignalPanelView.cs` | `SignalPanelView` | Top panel nodes; lights per registered type; relay pending blink; register pop+glow |
| `TextureFactory.cs` | `TextureFactory` | Cached procedural sprites: RoundedRect/Outline/Glow/Disc/Ring/Circuit |
| `BoardSkin.cs` | `BoardSkin`, `SpriteSet` | Optional sprite overrides (asset slot-in); `SpriteSet.Resolve` falls back to TextureFactory |
| `UiUtil.cs` | `UiUtil` | Rect/Image/Label/Stretch/Anchors helpers |
| `BoardBackground.cs` | `BoardBackground` | Legacy serialized bg config (kept for scene link) |
| `InGameSceneBackgroundView.cs` | `InGameSceneBackgroundView` | Stub (scene compat) |
| `CellView.cs` `ItemSlotView.cs` `HUDView.cs` `ResultOverlayView.cs` `FailOverlayView.cs` `PausePopupView.cs` `ItemBuyConfirmPopupView.cs` | — | Legacy stubs (CellView used by `Core/UI/TutorialOverlay` via `BoardView.GetCellView`) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `BoardView.Init(board, def)` | method | (Re)builds the whole board for a stage |
| `BoardView.AnimateMove(from,to,chip,srcSlot,absorbed,onComplete)` | method | Flight + sweep FX, then callback |
| `BoardView.On{LaneTapped,BoosterTapped,ChapterCycle,Restart,Back}` | event | Consumed by InGameController |
| `BoardView.GetCellView(r,c)` | method | Legacy tutorial hook — always null (Signal Sort has no grid) |
| `LaneView.SlotWorldPos(i)` / `ChipPixelSize()` | method | Flight geometry source |
| `SpriteSet.Resolve(skin)` | method | Skin override else procedural fallback |

## Rules
- Board UI is code-generated at runtime — do NOT add a `.prefab` or route through UIEditorSetup.
- All decorative `Image`s use `UiUtil.Image` (raycastTarget off); button graphics must set `raycastTarget = true`.
- Procedural sprites are white/neutral and tinted via `Image.color`; cache via TextureFactory.
- Keep legacy stub views — removing `CellView`/`BoardView.GetCellView` breaks `Core/UI/TutorialOverlay`.
