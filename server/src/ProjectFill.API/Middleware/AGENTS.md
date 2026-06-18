# ProjectFill.API/Middleware

## Files
| file | class | role |
|------|-------|------|
| `CorrelationIdMiddleware.cs` | `CorrelationIdMiddleware` | Creates or propagates per-request correlation id |
| `ApiExceptionMiddleware.cs` | `ApiExceptionMiddleware` | Converts application and DB exceptions to `ErrorResponse` |
| `UserIdResolutionMiddleware.cs` | `UserIdResolutionMiddleware` | Resolves JWT `sub` platform PID to internal `user_id` claim |
| `DevOnlyMiddleware.cs` | `DevOnlyMiddleware` | Dev cheat 1st gate: 404s `/api/dev/*` before auth when `GAME_ENV != dev` |
| `VersionCheckMiddleware.cs` | `VersionCheckMiddleware` | Rejects unsupported client/protocol versions |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CorrelationIdMiddleware.InvokeAsync` | method | Writes `HttpContext.Items["CorrelationId"]` |
| `ApiExceptionMiddleware.InvokeAsync` | method | Maps `GameApiException.Code` and DB concurrency conflicts |
| `UserIdResolutionMiddleware.InvokeAsync` | method | Creates first-seen `players` row with `InsertIgnoreAsync` |
| `VersionCheckMiddleware.InvokeAsync` | method | Requires X-Client-Version and X-Protocol-Version; bypasses health, swagger, openapi, scalar, and ssv-callback |

## Rules
- Keep error codes stable; clients branch on `ErrorResponse.Code`.
- `ErrorResponse` contains only `Code` — no `message` field on the wire.
- Non-`GameApiException` paths (UNAUTHORIZED, CONCURRENT_MODIFICATION, INTERNAL_ERROR) use `ErrorCodes.*` constants.
- No local session revocation or `sessions.active` checks.
- JWT `sub` is platform PID, not uid.

## Cross-refs
- Consumed by: `ProjectFill.API.Program`
- Depends on: `ProjectFill.Infrastructure.Security.PlatformAuthClient`
