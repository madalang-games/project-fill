# ProjectFill.API/Filters

## Files
| file | class | role |
|------|-------|------|
| `UserSerializeFilter.cs` | `UserSerializeFilter` | Serializes non-GET controller actions per platform PID |
| `CheatWhitelistFilter.cs` | `CheatWhitelistFilter` | Dev cheat 2nd gate: rejects unlisted PIDs with 403 `FORBIDDEN` (empty whitelist ⇒ allow all) |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UserSerializeFilter.OnActionExecutionAsync` | method | Returns `USER_LOCK_TIMEOUT` after 10s lock wait |
| `CheatWhitelistFilter.OnActionExecutionAsync` | method | Reads `ProjectFillConfiguration.Dev.CheatWhitelist`; 403 on unlisted PID |

## Rules
- Lock key is JWT `sub` platform PID.
- Do not read user ids from request bodies.

## Cross-refs
- Depends on: `ProjectFill.Infrastructure.Concurrency.UserSerializer`
- Consumed by: `ProjectFill.API.Program`
