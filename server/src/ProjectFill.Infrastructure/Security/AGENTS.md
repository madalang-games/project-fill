# ProjectFill.Infrastructure/Security

## Files
| file | class | role |
|------|-------|------|
| `JwtPublicKeyCache.cs` | `JwtPublicKeyCache` | Fetches and caches platform-auth JWKS for JWT signature validation |
| `PlatformAuthClient.cs` | `PlatformAuthClient` | Resolves platform PID to internal uid through platform-auth |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `JwtPublicKeyCache.GetKeysForKid` | method | Refreshes keys once when an unknown `kid` is encountered |
| `PlatformAuthClient.GetUserIdByPidAsync` | method | Returns null on lookup failure; caller maps to 401 |

## Rules
- Game server validates access JWTs statelessly with JWKS.
- Platform-auth owns refresh, logout, session-family state, and account identity.

## Cross-refs
- Consumed by: `ProjectFill.API.Program`
- Consumed by: `ProjectFill.API.Middleware.UserIdResolutionMiddleware`
