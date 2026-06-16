# ProjectFill.API/Controllers

## Files
| file | class | role |
|------|-------|------|
| `ControllerBaseEx.cs` | `ControllerBaseEx` | Shared internal user id and correlation id helpers |
| `RankingController.cs` | `RankingController` | Global ranking endpoints |
| `RewardsController.cs` | `RewardsController` | Generic reward source listing and claim endpoints |
| `AdRewardsController.cs` | `AdRewardsController` | Generic rewarded-ad claim endpoint |
| `AdSsvCallbackController.cs` | `AdSsvCallbackController` | `[AllowAnonymous]` GET `/api/ad/ssv-callback` — AdMob SSV callback |
| `AdController.cs` | `AdController` | `/api/ad` eligibility, interstitial shown |
| `AuthController.cs` | `AuthController` | `/api/auth` proxy endpoints for guest, google, refresh, logout |
| `BootstrapController.cs` | `BootstrapController` | `/api/bootstrap/config` force-update + schema/meta-hash; `GET /api/data/bundle` OTA CSV bundle from `generated/data/client_bundle.json` |
| `CurrencyController.cs` | `CurrencyController` | `GET /api/currency` balance fetch; `POST /api/currency/spend` soft currency deduct |
| `TutorialController.cs` | `TutorialController` | `/api/tutorial` progress saving and retrieving endpoints |
| `PlayerController.cs` | `PlayerController` | `GET /api/player/progress` — unlocked avatars + no-ads state; `POST /api/player/profile` — name/avatar update |
| `AdRewardsStatusController.cs` | `AdRewardsStatusController` | `/api/ad-rewards/status/{adToken}` polling check for verified SSV rewards |
| `InventoryController.cs` | `InventoryController` | `/api/inventory` endpoints for syncing, spending, and buying booster items |
| `IapController.cs` | `IapController` | `/api/iap` purchase verification and product status listing |
| `CosmeticController.cs` | `CosmeticController` | `/api/cosmetics` list, gold unlock, and active equip endpoints |
| `AttendanceController.cs` | `AttendanceController` | `/api/attendance` daily status + claim endpoints |
| `AchievementController.cs` | `AchievementController` | `/api/achievements` list + claim endpoints |
| `DailyChallengeController.cs` | `DailyChallengeController` | `/api/daily-challenge` today/attempt/clear/ranking/me/streak endpoints |
| `StageController.cs` | `StageController` | `POST /api/stages/{stageId}/clear` — Signal Sort campaign stage-clear submission |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ControllerBaseEx.PlayerId` | property | Reads internal `user_id` claim resolved from JWT `sub` |
| `BootstrapController.GetConfig` | method | `GET /api/bootstrap/config`; compares `X-Client-Version`/`X-Protocol-Version` headers to `App.Allowed*` → `ForceUpdate`; returns schema version + meta hash |
| `BootstrapController.GetBundle` | method | `GET /api/data/bundle`; serves `client_bundle.json` as `DataBundleResponse`; 404 if missing |
| `RankingController.GetGlobal` | method | Paged `/api/rankings/global/{type}` list (`stages`, `max-stage`) |
| `RankingController.GetMyGlobal` | method | Current user's global rank card |
| `RankingController.GetWeekly` | method | `GET /api/rankings/weekly` — paged current-week cleared-stage ranking |
| `RankingController.GetMyWeekly` | method | `GET /api/rankings/weekly/me` — current user's weekly rank card |
| `RankingController.GetMyStageRank` | method | `GET /api/rankings/stages/{stageId}/me` — my best-moves rank for a stage |
| `RankingController.Rebuild` | method | `POST /api/rankings/admin/rebuild`; auth-gated Redis rebuild trigger |
| `StageController.Clear` | method | `POST /api/stages/{stageId}/clear` — submit clear; returns best/rank/first-clear/milestone |
| `RewardsController.Claim` | method | Generic source claim |
| `AdRewardsController.Claim` | method | Generic ad reward claim for supported placements |
| `AdSsvCallbackController.SsvCallback` | method | `GET /api/ad/ssv-callback` — always returns 200 |
| `AdController.GetEligibility` | method | `GET /api/ad/eligibility` — interstitial cooldown state |
| `AdController.InterstitialShown` | method | `POST /api/ad/interstitial/shown` — records shown timestamp |
| `CurrencyController.Get` | method | `GET /api/currency` — current soft currency balance |
| `CurrencyController.Spend` | method | `POST /api/currency/spend` — deduct soft currency; 400 if insufficient |
| `TutorialController.GetProgress` | method | `GET /api/tutorial/progress` — fetch user completed tutorial list |
| `TutorialController.CompleteTutorial` | method | `POST /api/tutorial/progress/{tutorialId}` — save completed tutorial step |
| `PlayerController.GetProgress` | method | `GET /api/player/progress` — returns `PlayerProgressResponse` with unlocked avatars and NoAds state |
| `AdRewardsStatusController.GetStatus` | method | GET status of pending reward verification |
| `InventoryController.Sync` | method | GET current player inventory |
| `InventoryController.Spend` | method | POST spend items |
| `InventoryController.Buy` | method | POST buy items with Gold |
| `IapController.Verify` | method | `POST /api/iap/verify` — validates purchase, grants reward group |
| `IapController.GetProducts` | method | `GET /api/iap/products` — enabled products with remaining purchase counts |
| `CosmeticController.Get` | method | `GET /api/cosmetics` — catalog + unlock state + active equip |
| `CosmeticController.Unlock` | method | `POST /api/cosmetics/{id}/unlock` — gold unlock |
| `CosmeticController.SetActive` | method | `POST /api/cosmetics/active` — equip chip/lane/board skins |
| `AttendanceController.Status` | method | `GET /api/attendance/status` — current attendance state + 7 day cards |
| `AttendanceController.Claim` | method | `POST /api/attendance/claim` — claim today's attendance reward |
| `AchievementController.Get` | method | `GET /api/achievements` — list with progress + claim state |
| `AchievementController.Claim` | method | `POST /api/achievements/{id}/claim` — claim completed achievement |
| `DailyChallengeController.Today` | method | `GET /api/daily-challenge/today` — today's puzzle seed + my state |
| `DailyChallengeController.Clear` | method | `POST /api/daily-challenge/today/clear` — submit result, streak + rewards |
| `DailyChallengeController.Ranking` | method | `GET /api/daily-challenge/today/ranking` — paged global ranking |
| `DailyChallengeController.Streak` | method | `GET /api/daily-challenge/streak` — my streak |

## Rules
- Do not accept `user_id` from request bodies; use `ControllerBaseEx.PlayerId`.
- Do not parse JWT `sub` as a numeric id; it is platform PID.
- Controllers return contract DTOs only.

## Cross-refs
- Depends on: `ProjectFill.Application.Ranking.RankingService`
- Depends on: `ProjectFill.Application.Rewards.RewardService`
- Depends on: `ProjectFill.Application.Tutorial.TutorialService`
