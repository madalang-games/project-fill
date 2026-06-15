# ProjectFill.Infrastructure

## Nav
| path | role |
|------|------|
| `Generated/` | Auto-generated EF Core entities + AppDbContext (DO NOT EDIT) |
| `Security/` | JWT key cache + platform-auth UID lookup | -> `Security/AGENTS.md` |
| `Concurrency/` | Per-user request serialization helpers | -> `Concurrency/AGENTS.md` |
| `Data/` | Generated static-data service partials (DO NOT EDIT) | -> `Data/AGENTS.md` |

## Rules
- NEVER edit `Generated/` - re-run `npm run gen:orm`.
- Column names are mapped to snake_case in `AppDbContext.OnModelCreating`.
- Do not implement account identity, refresh-token, logout, or session revocation logic here.
