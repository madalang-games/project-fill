# Editor — Unity Editor automation tools

## Files
| file | class | role |
|------|-------|------|
| `UIImageResourceExtractor.cs` | `UIImageResourceExtractor` | [MenuItem] batch-loads transparent UI images, parses alpha-connected components, previews and saves numbered PNG sprites |
| `BuildScript.cs` | `BuildScript` | Batch-mode Android build entry points; reads KEYSTORE_PASS/KEY_ALIAS_PASS from env; `BuildAndroidRelease` applies release size settings then builds ARM64 AAB |
| `ClearPlayerPrefs.cs` | `PlayerPrefsResetMenu` | [MenuItem] clears all PlayerPrefs for local reset/debug |
| `IconAutomator.cs` | `IconAutomator` | [MenuItem] icon DPI generator — resizes source icon and applies to all Android/iOS DPI slots |
| `GoogleMobileAdsGradleManifestPostprocessor.cs` | `GoogleMobileAdsGradleManifestPostprocessor` | Android Gradle postprocessor for GMA plugin conflicts |
| `BuildCleanupPostProcessor.cs` | `BuildCleanupPostProcessor` | IPostprocessBuildWithReport; deletes large unwanted artifacts (`_BackUpThisFolder...`, `_BurstDebugInformation...`) after successful build |
| `UIEditorSetup.cs` | `UIEditorSetup` | [MenuItem] one-shot prefab/scene builders; attaches LocalizedText with stringId from StringIds.cs; auto-creates missing Final variants |
| `StageNodeEditorSetup.cs` | `StageNodeEditorSetup` | [MenuItem] StageNodeView prefab builder |
| `FontLocalizationConfigGenerator.cs` | `FontLocalizationConfigGenerator` | [MenuItem] reads tools/subset_tool/config.json → creates FontLocalizationConfig.asset with per-language fonts; sets TMP fallback |
| `StringIds.cs` | `StringIds` | **AUTO-GENERATED** by `gen:info` from `client_string.csv`; key constants; used by UIEditorSetup via `using static` |
| `StringCsvPostprocessor.cs` | `StringCsvPostprocessor` | AssetPostprocessor; watches `Data/string/client_string.csv` reimport → calls `LocalizedText.RefreshAllInEditor()`; menu: `Tools/Localization/Refresh Editor Text Preview` |
| `UnityStageFileWatcher.cs` | `UnityStageFileWatcher` | [InitializeOnLoad] watches changes to shared `stage.csv` and auto-runs generation scripts |
| `AdMobEditorSetup.cs` | `AdMobEditorSetup` | [InitializeOnLoad] automatically ensures GOOGLE_MOBILE_ADS scripting define symbol is set for Android, iOS, and Standalone |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UIImageResourceExtractor.Open()` | method | [MenuItem "Tools/UI/Image Resource Extractor"] opens the extraction window |
| `BuildScript.BuildAndroidRelease()` | method | Batch entry: Play Store AAB; applies High stripping, IL2CPP OptimizeSize, and release minify before build |
| `BuildScript.ApplyAndroidReleaseSizeSettings()` | method | Sets Android Release build type and size knobs for batch builds |
| `BuildCleanupPostProcessor.OnPostprocessBuild()` | method | Post-build cleanup entry point; scans and deletes unwanted folders |
| `PlayerPrefsResetMenu.ResetPrefs()` | method | [MenuItem "Tools/Reset PlayerPrefs"] deletes all PlayerPrefs |
| `IconAutomator.ShowWindow()` | method | [MenuItem "Tools/Icon Automator"] opens icon automation window |
| `UIEditorSetup.CreateAllPrefabs()` | method | [MenuItem "Tools/UI Setup/1"] creates all popup/overlay prefabs |
| `UIEditorSetup.TryMapImageSprite()` | method | Helper to map sprite directly onto Image component target |
| `UIEditorSetup.BtnNavTab()` | method | Nav bar tab button — icon (color-tinted) above label; `_homeHighlight` etc. wire to `Visual/Icon` Image |
| `UIEditorSetup.ItemToggleRow()` | method | Toggle row with item icon on left; Label TMP is a child of Toggle GO (GetComponentInChildren finds it) |
| `UIEditorSetup.MapStarAndIconSprites()` | method | Re-maps star_empty/star_filled/lock icon on Common prefabs without recreating them |
| `UIEditorSetup.CreateRewardPopup()` | method | Also creates `RewardItemCell.prefab` inline (background+Icon+Quantity badge+RewardItemCellView); wires `_itemRowPrefab` on RewardPopupView |
| `UIEditorSetup.CreateItemTooltip()` | method | Creates `ItemTooltipView.prefab` skeleton in Base/Common; missing Resources/Prefabs/UI variant is created automatically |
| `UIEditorSetup.CreateVariantIfMissing()` | method | Creates Final prefab variants from Base prefabs; skips when target variant already exists |
| `UIEditorSetup.MapHierarchyImageSprite()` | method | Maps a sprite key from resMap to an Image at `childPath` inside a loaded prefab |
| `FontLocalizationConfigGenerator.Generate()` | method | [MenuItem "Tools/Localization/Generate Font Config"] creates FontLocalizationConfig.asset from config.json |
| `StringIds` | class | All client_string.csv key constants; import with `using static Game.Editor.StringIds` |
| `UnityStageFileWatcher` | class | Active file system watcher for local hot-reloads |
| `AdMobEditorSetup` | class | [InitializeOnLoad] editor helper setting GOOGLE_MOBILE_ADS scripting define symbol |

## Rules
- Editor-only folder — auto-excluded from player builds
- DO NOT add game logic or runtime dependencies here
- **UI Setup Icon & Layout Override Preservation**: 
  - **Preserve Sprites**: When setting sprites on UI Image components (e.g. Header Avatar/Settings), check if `image.sprite == null` first to preserve manual modifications.
  - **Reuse Existing Objects**: Avoid destroying and recreating objects (e.g., `InstantiatePrefab` or `DestroyImmediate` followed by `Child`) in hierarchy builders. Reusing existing GameObjects using `Transform.Find` keeps the `fileID` structure intact, preventing the final Variant's overrides (e.g., Active states, color, and RectTransform overrides) from breaking.
  - **Save Flushing**: Always call `AssetDatabase.SaveAssets()` after editing and saving prefabs via `PrefabUtility.SaveAsPrefabAsset` to ensure that Unity saves and remaps Variant overrides correctly on disk.
  - **Single Prefab Menu Items**: All individual UI prefab builder menu items must be defined under `Tools/UI Setup/Prefabs/...`, sorted alphabetically, and their priority indices assigned consecutively. Each builder must print a success log containing `[UIEditorSetup] Saved Base Popup → {path}` on completion.
