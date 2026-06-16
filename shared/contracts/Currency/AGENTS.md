# shared/contracts/Currency

## Files
| file | class | role |
|------|-------|------|
| `CurrencyResponses.cs` | `CurrencySnapshot` | Soft currency balance snapshot DTO |
| `CurrencyRequests.cs` | `SpendSoftRequest` | Spend endpoint request DTO |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CurrencySnapshot.SoftAmount` | property | Current soft currency balance |
| `SpendSoftRequest.Amount` | property | Amount to deduct |
| `SpendSoftRequest.Reason` | property | Audit log reason (e.g. `booster_purchase`) |

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.CurrencyController`
- Consumed by: `ProjectFill.Application.Currency.CurrencyService`
- Consumed by: `Game.Services.CurrencyApiService`
