# ProjectFill.Infrastructure/Concurrency

## Files
| file | class | role |
|------|-------|------|
| `UserSerializer.cs` | `UserSerializer` | In-process per-PID async lock for user-modifying requests |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UserSerializer.AcquireAsync` | method | Returns async disposable lease or throws `TimeoutException` |

## Rules
- Lock key is platform PID from JWT `sub`; never use request body user ids.
- This is a single-process guard, not distributed locking.

## Cross-refs
- Consumed by: `ProjectFill.API.Filters.UserSerializeFilter`
