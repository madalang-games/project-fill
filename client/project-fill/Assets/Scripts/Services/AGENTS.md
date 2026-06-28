# Scripts/Services - Client Service Boundaries

Namespace: `Game.Services`

## Nav
| path | role |
|------|------|
| `Tutorial/` | Client-side tutorial manager and step sequencer | → `Tutorial/AGENTS.md` |

## Files
| file | class | role |
|------|-------|------|
| `StageDataService.cs` | `StageDataService` | DDOL singleton (lazy-instantiated if not in scene, e.g. InGame entered without Boot); loads Stage CSV via CsvLoader; GetStage(id), GetAll(), MaxStageId() |
| `DynamicResourceService.cs` | `DynamicResourceService` | DDOL singleton; loads dynamic_resource CSV; GetSprite(resourceKey) via Resources.Load (Resources-path entries only) |
| `ResourceKeys.cs` | `ResourceKeys` | `public static` const `dynamic_resource.csv` keys referenced by fixed key in code (IAP icons, lobby event badges); avoids magic-string typos. Data-driven keys (item/currency/iap icon columns) stay in CSV |
| `CurrencyDataService.cs` | `CurrencyDataService` | DDOL singleton; loads currency CSV; GetByRewardType(string) |
| `PlayerProgressService.cs` | `PlayerProgressService` | DDOL singleton; gold balance, per-stage best-moves/unlock (server-authoritative, cached), booster inventories |
| `AuthService.cs` | `AuthService` | DDOL singleton; auth result enum; Initialize(callback); stub until real HTTP auth wiring |
| `LocalizationService.cs` | `LocalizationService` | DDOL singleton; loads string/error CSV tables; Get(key), GetError(code), SetLanguage(Language), GetFont(Language) |
| `IAdService.cs` | `IAdService`, `AdWatchResult` | Ad service interface; WatchRewardedAd(placementId, cb); ShowInterstitialIfEligible(stageId, suppress, cb) |
| `AdMobService.cs` | `AdMobService` | DDOL singleton; implements IAdService; multi-placement rewarded ads + interstitial; SSV nonce set before Show() |
| `AdEligibilityCache.cs` | `AdEligibilityCache` | DDOL singleton; GET /api/ad/eligibility on session start; IsEligible(placementId); OnInterstitialShown() |
| `AdApiService.cs` | `AdApiService` | API client for ad operations: interstitial shown recording; result-screen double-reward claim |
| `NetworkService.cs` | `NetworkService` | DDOL singleton; centralised HTTP client; Get/Post; injects Application.version + authToken headers; configurable log level |
| `BootstrapService.cs` | `BootstrapService` | DDOL singleton; pre-auth boot gate: GET `/api/bootstrap/config` → force-update / schema-mismatch / OTA meta-hash patch via `/api/data/bundle` + `CsvLoader.ApplyPatchAtomic`; uses raw `UnityWebRequest` (pre-auth, custom headers/timeout) |
| `StageApiService.cs` | `StageApiService` | `POST /api/stages/{id}/start` — stage-entry unlock gate (STAGE_LOCKED on locked) + returns `SessionId` attempt token; `POST /api/stages/{id}/clear` — server-authoritative stage-clear submit (echoes `SessionId`); syncs gold + returns best/rank/first-clear/chapter-chest/rewards |
| `RankingApiService.cs` | `RankingApiService` | Optional server ranking page/my-rank fetcher |
| `CurrencyApiService.cs` | `CurrencyApiService` | Server soft currency fetch + spend; syncs `PlayerProgressService.Gold` on response |
| `InventoryApiService.cs` | `InventoryApiService` | Server-backed items fetch + spend API client |
| `RewardsApiService.cs` | `RewardsApiService` | Server rewards list fetch + claim API client |
| `TutorialApiService.cs` | `TutorialApiService` | Server-backed tutorial progress fetch + complete API client |
| `ErrorResponseJson.cs` | `ErrorResponseJson` | Serializable helper for server error code extraction |
| `ServerErrorCodes.cs` | `ServerErrorCodes` | Client-side mirror of server error codes the client must BRANCH on (not display): stage attempt-token failures; `Parse`/`IsStageSessionError` helpers |
| `PlayerApiService.cs` | `PlayerApiService` | DDOL singleton; `GET /api/player/progress` fetch; deserializes to `PlayerProgressResponse` |
| `CosmeticApiService.cs` | `CosmeticApiService` | DDOL singleton; `/api/cosmetics` list/unlock/active client; syncs gold on unlock; caches active cosmetics into `CosmeticState` on Fetch/SetActive |
| `CosmeticState.cs` | `CosmeticState` | Static session cache of active cosmetics (chip/lane/board skin + useCustomBoardSkin); `ResolveBoardSkin()` → active board skin or `board_default`; read by `InGameSceneBackgroundView` without re-fetch; `DevOverride(board,chip,lane)` (`#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`) forces active cosmetics for the dev skin switcher |
| `AttendanceApiService.cs` | `AttendanceApiService` | DDOL singleton; `/api/attendance` status + claim client; syncs gold on claim |
| `AchievementApiService.cs` | `AchievementApiService` | DDOL singleton; `/api/achievements` list + claim client; syncs gold on claim |
| `WeeklyMissionApiService.cs` | `WeeklyMissionApiService` | DDOL singleton; `/api/events/weekly-mission` status fetch + `claim/{threshold}`; syncs gold on claim |
| `IAPService.cs` | `IAPService` | DDOL singleton; `IDetailedStoreListener` Unity Purchasing adapter. Initializes store from enabled IAP catalog, drives `InitiatePurchase`, verifies real store receipt server-side (Pending→ConfirmPendingPurchase, redelivery-safe), retries on `IAP_VERIFY_PENDING`; Editor routes through mock verification; applies gold/items/no-ads to `PlayerProgressService` |
| `NetworkRetryOptions.cs` | `NetworkRetryOptions` | Options class for HTTP retries with exponential backoff, jitter, loading overlay, and toast messages |
| `SoundManager.cs` | `SoundManager` | DDOL singleton; BGM + SFX playback; SfxCatalog-based PlaySfx(SfxId) with pitch/cooldown; volume + mute in PlayerPrefs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `SoundManager.Instance` | prop | DDOL singleton |
| `SoundManager.PlayBGM(AudioClip)` | method | Plays loopable background music |
| `SoundManager.PlaySFX(AudioClip)` | method | Plays one-shot sound effect via raw AudioClip |
| `SoundManager.PlaySfx(SfxId)` | method | Catalog-based SFX with pitch variation + cooldown; also SfxEventBus subscriber |
| `SoundManager.BGMVolume` | prop | 0..1; persisted to PlayerPrefs |
| `SoundManager.SFXVolume` | prop | 0..1; persisted to PlayerPrefs |
| `SoundManager.BGMMute` | prop | bool; persisted to PlayerPrefs |
| `SoundManager.SFXMute` | prop | bool; blocks all SFX including catalog-based |
| `NetworkService.Instance` | prop | DDOL singleton; lazy-instantiated if not in scene |
| `NetworkService.BaseUrl` | prop | public; env-resolved server base URL (Dev/Prod) |
| `NetworkService.ProtocolVersion` | prop | public; `_protocolVersion` Inspector value sent as `X-Protocol-Version` |
| `NetworkService.SetAuthToken(string)` | method | Called by AuthService after login; injects Bearer token into all subsequent requests |
| `BootstrapService.Instance` | prop | DDOL singleton; place GameObject in Boot scene |
| `BootstrapService.Initialize(Action<BootstrapResult>)` | method | Runs boot gate coroutine; callback OK / ForceUpdate / PatchFailed |
| `BootstrapService.BootstrapResult` | enum | OK / ForceUpdate / PatchFailed |
| `NetworkService.Get(string,Action<bool,string>)` | method | HTTP GET; path relative to AppConfig BaseUrl |
| `NetworkService.Post(string,string,Action<bool,string>)` | method | HTTP POST with JSON body |
| `NetworkLogLevel` | enum | None / ErrorOnly / Normal / Verbose — controls log output granularity |
| `LocalizationService.Instance` | prop | DDOL singleton |
| `LocalizationService.CurrentLanguage` | prop | Active Language enum value |
| `LocalizationService.OnLanguageChanged` | event | Fired after SetLanguage(); LocalizedText subscribes |
| `LocalizationService.Get(string)` | method | Returns localized string for key; falls back to EN then key itself |
| `LocalizationService.GetError(string)` | method | Returns localized error message for server errorCode |
| `LocalizationService.SetLanguage(Language)` | method | Reloads tables + fires OnLanguageChanged; saves to PlayerPrefs |
| `LocalizationService.GetFont(Language)` | method | Returns TMP_FontAsset from FontLocalizationConfig; null if config missing |
| `StageDataService.GetStage(int)` | method | Returns Stage or null |
| `StageDataService.GetAll()` | method | Returns Stage[] |
| `StageApiService.StartStage(int,onSuccess,onError)` | method | `POST /api/stages/{id}/start`; onError carries STAGE_LOCKED when unreachable; onSuccess carries server `MaxClearedStageId` + `SessionId` attempt token |
| `StageApiService.ClearStage(stageId,moves,types,sessionId,boostersUsed,onSuccess,onError)` | method | `POST /api/stages/{id}/clear`; `sessionId` = the matching StartStage token (server rejects missing/mismatch/expired with `INVALID_STAGE_ATTEMPT`); `boostersUsed` drives the boosterless achievement + weekly-mission seams |
| `ServerErrorCodes.IsStageSessionError(err)` | method | True when a clear failure body is `INVALID_STAGE_ATTEMPT`/`STAGE_ATTEMPT_EXPIRED` (→ force-exit to lobby) |
| `DynamicResourceService.Instance` | prop | DDOL singleton |
| `DynamicResourceService.GetSprite(string)` | method | Loads Sprite via Resources.Load; caches result; returns null for non-Resources paths |
| `CurrencyDataService.Instance` | prop | DDOL singleton |
| `CurrencyDataService.GetByRewardType(string)` | method | Returns Currency matching reward_type_key; null if not found |
| `PlayerProgressService.Gold` | prop | Current gold balance |
| `PlayerProgressService.CanAfford(int)` | method | Gold >= cost check |
| `PlayerProgressService.SpendGold(int)` | method | Returns false if insufficient gold |
| `PlayerProgressService.AddGold(int)` | method | Persists to PlayerPrefs |
| `PlayerProgressService.SetGold(int)` | method | Overwrite gold to server-authoritative value; persists to PlayerPrefs |
| `PlayerProgressService.GetBestMoves(int)` | method | Personal best move count for a stage (0 = none); lazy-loaded from PlayerPrefs (`bestmoves_`); also the "cleared" proxy (`>0`) |
| `PlayerProgressService.RecordBestMoves(int,int)` | method | Stores a new best when it beats the record (lower wins); returns true if improved |
| `PlayerProgressService.ApplyMaxClearedStage(int)` | method | Unlocks stages 1..maxClearedStageId+1 from the server-authoritative campaign reach |
| `PlayerProgressService.IsStageUnlocked(int)` | method | Stage 1 always true; lazy-loaded |
| `PlayerProgressService.UnlockStage(int)` | method | Marks a single stage unlocked (persisted) |
| `AuthService.IsGuest` | prop | true until OAuth link |
| `AuthService.UserId` | prop | Device UUID or OAuth ID |
| `AuthService.Pid` | prop | Player ID used in JWT; reads PlayerPrefs `auth_pid`; exposed for AccountPopup copy |
| `AuthService.PendingBootMessage` | static prop | Optional toast message to show after Boot redirect (currently unused) |
| `AuthService.Initialize(Action<AuthResult>)` | method | Guest by default; fires NetworkError if refresh is rate-limited (429); fires ReLoginRequired if token is invalid; fires NewGuestCreated if account switch detected |
| `AuthService.ContinueAsGuest(Action<AuthResult>)` | method | Explicit guest login path — skips token check, calls LoginGuest directly; use from ReLoginView "Continue as Guest" only |
| `AuthService.LinkGoogle(idToken,nonce,cb)` | method | `POST /api/auth/link-oauth` with `guestRefreshToken`; cb(ok, err, LinkAccountResponseJson) |
| `AuthService.ResolveConflict(token,selection,cb)` | method | `POST /api/auth/resolve-conflict`; calls CompleteSession with returned auth tokens |
| `AuthService.Logout()` | method | Clears all auth prefs; kept for internal use only — no UI button |
| `AuthResult` | enum | Authenticated / Guest / ReLoginRequired / NewGuestCreated |
| `AuthService.LinkAccountResponseJson` | class | `{success, conflict, localSave, cloudSave, conflictToken}` — public inner class |
| `AuthService.SaveSnapshotJson` | class | `{maxStageId, gold, totalStars, totalItems}` — public inner class |
| `AdWatchResult.Earned` | field | true if user earned the reward |
| `AdWatchResult.AdToken` | field | SSV nonce; pass to server POST endpoint for reward claim |
| `IAdService.WatchRewardedAd(string,Action<AdWatchResult?>)` | method | null result = cancel/fail/no-ad loaded |
| `IAdService.ShowInterstitialIfEligible(int,bool,Action<bool>)` | method | bool wasShown; caller posts /api/ad/interstitial/shown if true |
| `AdMobService.Instance` | prop | DDOL singleton |
| `AdEligibilityCache.Instance` | prop | DDOL singleton |
| `AdEligibilityCache.Refresh()` | method | No-arg overload; uses NetworkService for base URL + auth token |
| `AdEligibilityCache.Refresh(string,string)` | method | Legacy overload; baseUrl, optional authToken |
| `AdEligibilityCache.IsEligible(string)` | method | Returns false if placement not in cache |
| `AdEligibilityCache.GetCooldownSeconds(string)` | method | Returns 0 if not in cache |
| `AdEligibilityCache.OnInterstitialShown()` | method | Optimistically marks INTERSTITIAL_POST_STAGE ineligible until next Refresh |
| `AdApiService.Instance` | prop | DDOL singleton |
| `AdApiService.RecordInterstitialShown(int,Action,Action<string>)` | method | Records interstitial ad display on server |
| `AdApiService.ClaimDoubleReward(stageId,attemptId,provider,adToken,onSuccess,onError)` | method | `POST /api/ad/double-reward`; parses `AdDoubleRewardGrantResponse` (granted/duplicate/rewards/currency); syncs gold via `CurrencyApiService.UpdateGold` |

| `RankingApiService.FetchGlobalPage` | method | GET paged global ranking |
| `RankingApiService.FetchMyGlobalRank` | method | GET current user's ranking card (`stage`/`perfect`) |
| `RankingApiService.FetchWeeklyPage` | method | GET paged weekly ranking (`/api/rankings/weekly`) |
| `RankingApiService.FetchMyWeeklyRank` | method | GET current user's weekly ranking card (`/api/rankings/weekly/me`) |
| `RankingApiService.FetchMyStageRank` | method | GET current user's stage rank |
| `CurrencyApiService.OnGoldChanged` | event | `Action<long, long>` (amount, delta); fires on any server-confirmed gold change |
| `CurrencyApiService.UpdateGold` | method | Canonical gold setter (single chokepoint): calls `PlayerProgressService.SetGold` + fires `OnGoldChanged`; call this from any service receiving `CurrencySnapshot`. Skips an empty/no-op snapshot (`SoftAmount==0 && SoftDelta==0`) so a balance-less response never wipes gold to 0. `authoritative:true` (FetchGold/SpendGold + post-buy/IAP balances) bypasses the skip so a real 0 applies |
| `CurrencyApiService.FetchGold` | method | GET `/api/currency`; calls `UpdateGold` on success |
| `CurrencyApiService.SpendGold` | method | POST `/api/currency/spend`; calls `UpdateGold` on success |
| `InventoryApiService.FetchInventory` | method | GET `/api/inventory`; updates progress cache on success |
| `InventoryApiService.SpendItem` | method | POST `/api/inventory/spend`; deducts server-side item balance |
| `RewardsApiService.FetchRewardSources` | method | GET `/api/rewards/sources`; fetch generic reward milestones status |
| `RewardsApiService.ClaimReward` | method | POST `/api/rewards/claim`; claims milestone reward (gold/boosters) and syncs inventory |
| `PlayerProgressService.GetItemCount` | method | Returns count of booster itemId |
| `PlayerProgressService.SetItemCount` | method | Sets local count of booster itemId |
| `PlayerProgressService.SetInventory` | method | Updates all item counts in cache from snapshot |
| `PlayerProgressService.LoadFromServer` | method | Overwrites unlock/star cache with server-authoritative `PlayerProgressResponse`; clears stale PlayerPrefs |
| `PlayerApiService.Instance` | prop | DDOL singleton |
| `PlayerApiService.FetchProgress` | method | GET /api/player/progress; callback (bool ok, PlayerProgressResponse) |
| `AuthResult.NewGuestCreated` | enum value | Fired by Initialize() when a new guest account replaced the previous one (PID mismatch detected) |

| `NetworkRetryOptions.None` | prop | Preset that disables retries |
| `NetworkRetryOptions.LobbyAndSave` | prop | Preset for important lobby and save requests (3 retries, overlay, and toast error messages) |
| `NetworkService.Get(string,NetworkRetryOptions,Action<bool,string>)` | method | HTTP GET with custom retry options |
| `NetworkService.Post(string,string,NetworkRetryOptions,Action<bool,string>)` | method | HTTP POST with custom retry options |

## Error Toast Convention (Server Error → UI)

Server responses on failure: `{ "code": "ERROR_CODE" }` — no `message` field.

### Showing a toast from a server error

```csharp
// When onComplete callback receives (false, rawText):
string msg = LocalizationService.Instance.GetErrorFromResponse(rawText);
// → parses JSON code, looks up error_messages.csv, falls back to code string

UIManager.Instance?.ShowToast(msg, ToastType.Warning);
```

Use `GetError(code)` when the error code is already extracted:
```csharp
string msg = LocalizationService.Instance.GetError(errCode);
// Falls back to errCode string if not in error_messages.csv
if (msg == errCode)
    msg = LocalizationService.Instance.Get("common.error_generic"); // optional UI fallback
UIManager.Instance?.ShowToast(msg, ToastType.Warning);
```

### Do NOT
- Branch on `err == "SOME_STRING"` raw checks for toast text — use `GetError` instead.
- Show `err` or raw JSON directly in toast.
- Use `client_string.csv` keys for server error display — that's `error_messages.csv` territory.

### Adding a new server error code
1. Add constant to `ErrorCodes.cs` (server).
2. Add row to `shared/datas/string/error_messages.csv` (EN + KO).
3. Run `tools/info_generator.bat` to regenerate CSVs.

## Rules
- All services are DDOL; place GameObjects in Boot scene only.
- PlayerPrefs keys must not clash: prefix `auth_`, `gold`, `stars_`, `unlocked_`, `bestmoves_`, `lang`.
- AuthService is a stub; server-side auth is Phase 2.
- LocalizationService must initialize before any LocalizedText.Awake(); place it first in Boot scene.
- AdEligibilityCache.Refresh() must be called after auth is available (token set via NetworkService).
- StageApiService and RankingApiService are optional until full server auth wiring lands; local flow must continue if absent.
- AdMobService uses test IDs in Dev environment / Editor, and uses production AppConfig.AdMobAndroidAppIdFree / AppConfig.AdMobAndroidAppIdReward in Prod environment.
- AdMobService SDK-missing stub must return `null` for rewarded ads; reward success is verified or mocked only server-side.
- pkt_generator must be run to sync Ad + Currency contracts to Generated/Contracts/ before ad flows work.
- **NetworkService owns all HTTP transmission**: do NOT add UnityWebRequest code to individual services.
- **Gold sync = one path only**: route every server-confirmed gold change through `CurrencyApiService.UpdateGold` — never call `PlayerProgressService.SetGold` directly from API callbacks. Guard the call on the **raw parsed JSON** currency field (`if (json.currency != null)`), NOT the mapped contract `response.Currency` (the contract default + `?? new CurrencySnapshot()` fallback make it never-null, so a balance-less response would wipe gold to 0). Use `authoritative:true` only for direct balance reads (FetchGold / post-spend / post-buy / IAP) where a real 0 must apply.
- NetworkService._enableLogging respects the Inspector value in all environments; disable manually in Prod if log suppression is needed.

## Cross-refs
- Depends on: `Game.Utils.CsvLoader`, `ProjectFill.Data.Generated.Stage`, `Game.Localization.FontLocalizationConfig`
- Depends on: `GoogleMobileAds.Api` (Google Mobile Ads SDK)
- Consumed by: Boot scene, Lobby scene, InGame scene entry, `Game.Core.UI.LocalizedText`
