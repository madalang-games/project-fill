# ProjectFill.Domain/Utilities

## Files
| file | class | role |
|------|-------|------|
| `IdHelper.cs` | `IdHelper` | Generates JSON-safe 16 digit ids |
| `NicknameGenerator.cs` | `NicknameGenerator` | Creates default display names for first-seen platform PIDs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `IdHelper.NewId` | method | Returns string id in JavaScript-safe integer range |
| `NicknameGenerator.Generate` | method | Produces non-authoritative default display name |

## Rules
- No infrastructure dependencies.
- Do not use PID or internal user id in display names.

## Cross-refs
- Consumed by: `ProjectFill.API.Middleware.UserIdResolutionMiddleware`
