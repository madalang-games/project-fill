# ProjectFill.Application/Logging

## Files
| file | class | role |
|------|-------|------|
| `EventLogFactory.cs` | `EventLogFactory` | Creates `event_logs` rows with JSON params |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `EventLogFactory.StageAttemptReplaced` | method | Logs `replaced_by_new_attempt` |
| `EventLogFactory.AdRewardClaimed` | method | Logs common ad reward transaction result |

## Note
Currency mutations are NOT logged via EventLogFactory. They write directly to `currency_logs` table via `CurrencyService`.

## Rules
- Keep params aligned with `server/db/event_log_definitions.json`.

## Cross-refs
- Depends on: `ProjectFill.Domain.Logging.EventLogIds`
- Gen output: `ProjectFill.Infrastructure.Generated.EventLogsRow`
