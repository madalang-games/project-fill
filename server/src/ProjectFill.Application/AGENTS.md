# ProjectFill.Application

## Nav
| path | role | link |
|------|------|------|
| `Common/` | Error codes and API exception type | `Common/AGENTS.md` |
| `Logging/` | Event log row factory | `Logging/AGENTS.md` |
| `Stage/` | Interstitial ad cooldown and double reward ad services | `Stage/AGENTS.md` |
| `Ranking/` | Global ranking Redis indexes and rebuilds | `Ranking/AGENTS.md` |
| `Rewards/` | Generic reward source, ad reward claim services, SSV infrastructure | `Rewards/AGENTS.md` |
| `Currency/` | Soft currency balance service | `Currency/AGENTS.md` |
| `Iap/` | IAP purchase verification and product status | `Iap/AGENTS.md` |
| `Inventory/` | Booster item inventory (sync, spend, grant, buy) | `Inventory/AGENTS.md` |
| `Tutorial/` | Tutorial progress saving and retrieving service | `Tutorial/AGENTS.md` |
| `Player/` | Player profile and progress (stage unlock, avatars, no-ads state) | `Player/AGENTS.md` |
| `Cosmetic/` | Cosmetic catalog, gold unlock, equip, condition-based unlock | `Cosmetic/AGENTS.md` |
| `Attendance/` | Daily attendance status + claim, milestone rewards | `Attendance/AGENTS.md` |
| `Achievement/` | Achievement progress, claim, derived evaluation + report seams | `Achievement/AGENTS.md` |
| `DailyChallenge/` | Daily challenge today/clear/ranking/streak | `DailyChallenge/AGENTS.md` |

## Rules
- Use-case layer: persistence via direct `AppDbContext` injection.
- Services return contract DTOs (`ProjectFill.Contracts.*`); never expose generated DB rows.
- Controllers provide internal `user_id`; services never resolve JWT claims.
- `async/await` throughout; CancellationToken passed to all async methods.
