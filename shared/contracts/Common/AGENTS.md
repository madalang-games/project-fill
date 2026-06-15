# shared/contracts/Common

## Files
| file | class | role |
|------|-------|------|
| `ErrorResponse.cs` | `ErrorResponse` | Standard API error response DTO |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `ErrorResponse.Code` | property | Server error code matching `string/error_messages.csv` |

## Rules
- DTOs only; no transport or localization logic.

## Cross-refs
- Consumed by: `ProjectFill.API`
- Consumed by: `Game.Services.LocalizationService`
