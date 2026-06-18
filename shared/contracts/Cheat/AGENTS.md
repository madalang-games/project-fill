# shared/contracts/Cheat

## Files
| file | class | role |
|------|-------|------|
| `CheatRequests.cs` | `CheatCommandRequest` | Dev cheat command request DTO |
| `CheatResponses.cs` | `CheatCommandResponse` | Dev cheat command result DTO |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CheatCommandRequest.Command` | property | Raw `/domain [target] action [value]` command string |
| `CheatCommandResponse.Success` | property | Always true on 200 (failures are `GameApiException` → 400) |
| `CheatCommandResponse.Command` | property | Echo of the executed command |
| `CheatCommandResponse.Message` | property | Human-readable summary for the overlay log |
| `CheatCommandResponse.Data` | property | Per-domain post-change values (e.g. `{ balanceAfter }`); null when none |

## Rules
- Dev-only system; gated by `DevOnlyMiddleware` (env) + `CheatWhitelistFilter` (PID) on the server.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.DevCheatController`
- Consumed by: `ProjectFill.API.Dev.CheatDispatcher`
