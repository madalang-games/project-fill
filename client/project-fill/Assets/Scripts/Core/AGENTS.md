# Scripts/Core — App Lifecycle & Singletons

## Nav
| path | role |
|------|------|
| `UI/` | Animation components, common popup views | → `UI/AGENTS.md` |

## Files
| file | class | role |
|------|-------|------|
| `UIManager.cs` | `UIManager` | DDOL singleton; owns 4 canvases; ShowPopup/ShowOverlay/ShowToast/ShowLoading API |
| `SafeAreaHandler.cs` | `SafeAreaHandler` | Adjusts RectTransform anchors to Screen.safeArea on OnEnable + layout change |
| `SceneTransition.cs` | `SceneTransition` | DDOL singleton; FadeToScene, SlideUpToScene, SlideDownToScene with overlay animation |
| `GameConfig.cs` | `GameConfig` | Static constants: ContinueCost, LoadingTimeoutSec, StageNodePoolSize |
| `AppEnvironment.cs` | `AppEnvironment`, `AppConfig` | Env enum (Dev/Prod) + static config: server URLs, GoogleWebClientId |
| `Language.cs` | `Language` | Shared enum for all 15 supported locales (EN KO ZH_CN ZH_TW JA RU ES PT FR DE TH AR IT TR ID) |
| `GoogleSignInBridge.cs` | `GoogleSignInBridge` | DDOL singleton; Android JNI bridge to `GoogleSignInPlugin.java`; SignIn/SignOut; UnitySendMessage callbacks |
| `SfxCatalog.cs` | `SfxId`, `SfxEntry`, `SfxCatalog`, `SfxEventBus` | SFX enum (10 ids), ScriptableObject catalog, static event bus |
| `DifficultyStyle.cs` | `DifficultyStyle` | Static color helper: Normal=#4488FF neon blue, Hard=#FF4757 coral red; Get(difficulty, fallback) |
| `FileLogger.cs` | `FileLogger` | Static; hooks `Application.logMessageReceived` via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`; writes to `persistentDataPath/logs/game_YYYYMMDD_HHmmss.log`; no-op in Editor |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UIManager.Instance` | prop | DDOL singleton |
| `UIManager.ShowPopup<T>(Action<T>)` | method | Instantiates T from Resources/Prefabs/UI/{T.Name}; pushes onto popup stack |
| `UIManager.ShowOverlay<T>(Action<T>)` | method | Instantiates T; destroys existing overlay first |
| `UIManager.ShowToast(msg,type)` | method | Activates ToastView |
| `UIManager.ShowLoading()` | method | Activates LoadingOverlayView |
| `UIManager.HideLoading()` | method | Deactivates LoadingOverlayView |
| `UIManager.ShowNetworkError(onRetry)` | method | Hides loading, shows NetworkErrorView |
| `UIManager.CloseTopPopup()` | method | Destroys top of popup stack |
| `UIManager.CloseOverlay()` | method | Destroys current overlay |
| `UIManager.GetCurrentOverlay<T>()` | method | Returns T from current overlay; null if none |
| `SafeAreaHandler` | component | Attach to Lobby Header, BottomNavBar, InGame HUD, Canvas_Loading panel |
| `SceneTransition.FadeToScene(scene,cb)` | method | Alpha fade out → LoadScene → fade in |
| `SceneTransition.SlideUpToScene(scene,cb)` | method | Slide up → LoadScene → slide in |
| `SceneTransition.SlideDownToScene(scene,cb)` | method | Slide down → LoadScene → slide in |
| `AppEnvironment` | enum | Dev / Prod |
| `AppConfig.DevGameServerUrl` | const | Dev server base URL |
| `AppConfig.ProdGameServerUrl` | const | Prod server base URL |
| `AppConfig.GoogleWebClientId` | const | Google OAuth2 web client ID — fill in before release |
| `AppConfig.AdMobAndroidAppId` | const | Google Mobile Ads Android App ID (test ID by default) |
| `AppConfig.AdMobIOSAppId` | const | Google Mobile Ads iOS App ID (test ID by default) |
| `AppConfig.AdMobAndroidAppIdFree` | const | Google Mobile Ads Android Interstitial Ad Unit ID |
| `AppConfig.AdMobAndroidAppIdReward` | const | Google Mobile Ads Android Rewarded Ad Unit ID |
| `GoogleSignInBridge.Instance` | prop | DDOL singleton; auto-created at BeforeSceneLoad on Android |
| `GoogleSignInBridge.SignIn(webClientId,cb)` | method | Starts Google Sign-In flow; cb(idToken, errorCode) |
| `GoogleSignInBridge.SignOut(webClientId)` | method | Clears Google account session |
| `SfxId` | enum | ConfirmClick/CancelClick/RewardClaimed/StageClear/StageFail/CellGroupRemoved/BoardRotated/ToastError/ItemUsed/ActionBlocked/GravityLand — NOTE: CellGroupRemoved/BoardRotated/GravityLand are legacy names; rename to Signal Sort semantics (e.g. CompleteSetClear) when implementing InGame audio |
| `SfxEntry` | class | id + clip + volume + pitchRange + cooldownSeconds |
| `SfxCatalog.TryGet(id, out entry)` | method | ScriptableObject; linear search; loaded from Resources/SfxCatalog |
| `SfxEventBus.Play(id)` | static method | Fire-and-forget; SoundManager subscribes |
| `DifficultyStyle.Get(difficulty, easyFallback)` | static method | 0→easyFallback, 1→Normal(#4488FF), 2→Hard(#FF4757) |
| `FileLogger.Init()` | static method | `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`; no-op in Editor; opens log file and subscribes to log events |

## Rules
- UIManager canvases: Sort Order 10/20/30/100. Scene canvases always Sort 0.
- All popup/overlay prefabs must be in Resources/Prefabs/UI/ named exactly {ClassName}.prefab
- Static instances (Toast, LoadingOverlay, NetworkError): pre-instantiated at Awake; Show/Hide only
- Dynamic instances (Popup, Overlay): Instantiate/Destroy per call

## Cross-refs
- Consumed by: all scene entry points, all view scripts
