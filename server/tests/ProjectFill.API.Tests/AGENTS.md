# ProjectFill.API.Tests

## Files
| file | class | role |
|------|-------|------|
| `ProjectFill.API.Tests.csproj` | project | xUnit API integration test project |
| `UserClaimsTests.cs` | `UserClaimsTests` | Verifies internal uid claim is separate from JWT `sub` PID |
| `VersionCheckMiddlewareTests.cs` | `VersionCheckMiddlewareTests` | Verifies version/protocol gate behavior |

## Rules
- Keep tests deterministic and engine-free
- Do not connect to real MySQL, Redis, or platform auth from this project
- In-memory EF database name shared across factory lifetime; seed data inserted once in CreateHost
- Per-test isolation: use Guid-named in-memory DB instances
