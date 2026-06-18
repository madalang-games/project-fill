# ProjectFill.Domain/Logging

## Files
| file | class | role |
|------|-------|------|
| `EventLogIds.cs` | `EventLogIds` | Stable TrId constants for `event_logs` |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `EventLogIds.StageAttemptStarted` | constant | Stage attempt start event |
| `EventLogIds.StageAttemptReplaced` | constant | Existing attempt discarded by new start |
| `EventLogIds.AdRewardClaimed` | constant | Common rewarded-ad claim event |
| `EventLogIds.CheatGold`..`CheatAttendance` | constants | Dev cheat audit events `9001`–`9008` |

## Rules
- Keep values aligned with `server/db/event_log_definitions.json`.

## Cross-refs
- Consumed by: `ProjectFill.Application.Logging.EventLogFactory`
