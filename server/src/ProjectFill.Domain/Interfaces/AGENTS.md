# ProjectFill.Domain/Interfaces

## Files
| file | class | role |
|------|-------|------|
| `IPlatformAuthClient.cs` | `IPlatformAuthClient` | Resolves platform PID to internal user id |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `IPlatformAuthClient.GetUserIdByPidAsync` | method | Calls platform-auth internal lookup via Infrastructure implementation |

## Rules
- Interfaces only; no infrastructure dependencies.
- `sub` from JWT is platform PID, never internal user id.

## Cross-refs
- Implemented by: `ProjectFill.Infrastructure.Security.PlatformAuthClient`
- Consumed by: `ProjectFill.API.Middleware.UserIdResolutionMiddleware`
