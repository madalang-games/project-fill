# server/tests

## Nav
| path | role |
|------|------|
| `ProjectFill.API.Tests/` | ASP.NET Core API auth and integration tests |

## Rules
- Tests run outside Unity and must not require the Unity editor
- Prefer in-memory fakes for API boundary tests unless the test explicitly validates Docker infrastructure
