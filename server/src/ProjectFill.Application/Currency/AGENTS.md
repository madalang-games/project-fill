# ProjectFill.Application/Currency

## Files
| file | class | role |
|------|-------|------|
| `CurrencyService.cs` | `CurrencyService` | Soft currency balance read, grant, and spend |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CurrencyService.GrantSoftAsync` | method | Adds amount to `user_currency`, inserts `currency_logs` row with balance_before/after; no SaveAsync — caller saves; returns `CurrencySnapshot` with `SoftDelta = amount` |
| `CurrencyService.SpendSoftAsync` | method | Deducts amount; throws `InsufficientCurrency` if insufficient; inserts `currency_logs` row; calls SaveAsync; returns `CurrencySnapshot` with `SoftDelta = -amount` |
| `CurrencyService.GetAsync` | method | Returns `CurrencySnapshot` (0 if no row) |

## Rules
- `user_currency` row created lazily on first grant.
- ALL soft currency mutations must go through `GrantSoftAsync` or `SpendSoftAsync` — direct `user_currency` mutation bypasses audit trail.
- `GrantSoftAsync` does NOT call SaveAsync; `SpendSoftAsync` does. Callers inside transactions may safely call SpendSoftAsync — intermediate SaveAsync is within the open transaction.
- Spend throws `GameApiException(InsufficientCurrency)` — never floors to 0.
- `currency_type` is always `"soft"` until multi-currency is introduced.

## Cross-refs
- Depends on: `ProjectFill.Infrastructure.Generated.UserCurrencyRow`, `ProjectFill.Infrastructure.Generated.CurrencyLogsRow`
- Consumed by: `ProjectFill.Application.Rewards.RewardService`, `ProjectFill.Application.Inventory.InventoryService`, `ProjectFill.Application.Player.PlayerService`
