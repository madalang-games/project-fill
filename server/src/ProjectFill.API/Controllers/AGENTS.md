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
| `BootstrapController.cs` | `BootstrapController` | `/api/bootstrap` configurations and schema/meta hash checks |
| `CurrencyController.cs` | `CurrencyController` | `GET /api/currency` balance fetch; `POST /api/currency/spend` soft currency deduct |
| `TutorialController.cs` | `TutorialController` | `/api/tutorial` progress saving and retrieving endpoints |
| `PlayerController.cs` | `PlayerController` | `GET /api/player/progress` — stage unlock state and best stars for the current player |
| `AdRewardsStatusController.cs` | `AdRewardsStatusController` | `/api/ad-rewards/status/{adToken}` polling check for verified SSV rewards |
| `InventoryController.cs` | `InventoryController` | `/api/inventory` endpoints for syncing, spending, and buying booster items |
| `IapController.cs` | `IapController` | `/api/iap` purchase verification and product status listing |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ControllerBaseEx.PlayerId` | property | Reads internal `user_id` claim resolved from JWT `sub` |
| `RankingController.GetGlobal` | method | Paged `/api/rankings/global/{type}` list |
| `RankingController.GetMyGlobal` | method | Current user's global rank card |
| `RankingController.Rebuild` | method | `POST /api/rankings/admin/rebuild`; auth-gated Redis rebuild trigger |
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

## Rules
- Do not accept `user_id` from request bodies; use `ControllerBaseEx.PlayerId`.
- Do not parse JWT `sub` as a numeric id; it is platform PID.
- Controllers return contract DTOs only.

## Cross-refs
- Depends on: `ProjectFill.Application.Ranking.RankingService`
- Depends on: `ProjectFill.Application.Rewards.RewardService`
- Depends on: `ProjectFill.Application.Tutorial.TutorialService`
