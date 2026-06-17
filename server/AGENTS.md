# server - ASP.NET Core 8 | C# | Entity Framework Core

## Stack
ASP.NET Core 8 Web API | C# | Entity Framework Core 8 (ORM only, no migrations) | Pomelo MySQL | StackExchange.Redis | JWT Bearer | Scalar

## Nav
| path | role |
|------|------|
| `db/` | DB schema definition + migration history | -> `db/AGENTS.md` |
| `src/ProjectFill.sln` | Solution file |
| `src/ProjectFill.Domain/` | Entities, interfaces, pure helpers | -> `src/ProjectFill.Domain/AGENTS.md` |
| `src/ProjectFill.Application/` | Use cases (commands/queries) | -> `src/ProjectFill.Application/AGENTS.md` |
| `src/ProjectFill.Infrastructure/` | EF Core DbContext, Redis, JWKS/auth clients | -> `src/ProjectFill.Infrastructure/AGENTS.md` |
| `src/ProjectFill.API/` | Startup, controllers, middleware, filters, Dockerfile | -> `src/ProjectFill.API/AGENTS.md` |
| `tests/` | Engine-free server/API test projects | -> `tests/AGENTS.md` |
| `generated/` | Auto-generated - DO NOT edit |

## Rules
- NEVER edit `*/generated/*` - source is in `shared/`
- EF Core is used as ORM ONLY - never run `dotnet ef migrations` or `dotnet ef database update`
- DB schema managed by `npm run gen:orm` (reads `server/db/schema.json`)
- NEVER commit `.env.dev` or `.env.prod` - use `.env.dev.example` / `.env.prod.example`

## Project References
API -> Application -> Domain
Infrastructure -> Domain
API -> Infrastructure

## Auth Rules
- Project Fill validates platform access JWTs statelessly with JWKS.
- JWT `sub` is platform PID, never internal uid.
- `UserIdResolutionMiddleware` resolves PID to internal `user_id` claim before controllers run.
- Controllers and services never accept uid from request bodies.
- Platform-auth owns refresh, logout, session-family state, account identity, and token revocation.
- Game server does not maintain `sessions.active` or implement local session revocation.

## Conventions
- Namespaces: `ProjectFill.{Layer}` or `ProjectFill.{Layer}.{Domain}`
- No comments unless WHY is non-obvious
- `async/await` throughout - no `.Result` or `.Wait()`
- CancellationToken passed through all async methods
- Column names mapped to snake_case in `OnModelCreating`

## Build/Test Verification
Both gates must pass (**exit code 0**) for server work — run them separately:

| Gate | Command | Verifies |
|------|---------|----------|
| **Build** | `tools/server_generator.bat` | `docker compose --build` of dev stack: compiles server image + brings containers up |
| **Unit tests** | `tools/server_test.bat` | `dotnet test` of `tests/ProjectFill.API.Tests` (xUnit) |

- Always run **both**; `server_test.bat` is independent of `server_generator.bat` (no Docker needed, just the .NET SDK).
- `server_generator.bat` prereqs: Docker running + `.env.dev`; ends with interactive `pause`.
- `server_test.bat` honors `GEN_BATCH_NO_PAUSE=1` to skip its pause for non-interactive/CI runs.
- Completion Gate #1 (new endpoint → add test) still applies; `server_test.bat` is how those tests are run.

## Enum Policy
NEVER define `private enum` or inline enum inside a class or service file.

| Scope | File location | Namespace |
|-------|--------------|-----------|
| Server + Client shared | `shared/contracts/GameTypes/GameEnums.cs` | `ProjectFill.Contracts.GameTypes` |
| Server-internal (cross-domain) | `src/ProjectFill.Domain/Enums/ServerEnums.cs` | `ProjectFill.Domain.Enums` |
| Server-internal (domain-specific) | `src/ProjectFill.Domain/Enums/[Domain]Enums.cs` | `ProjectFill.Domain.Enums` |

Decision: if both server request/response DTO and client need the value → shared. If only server logic uses it → Domain/Enums/.

## Completion Gate
Evaluate before declaring server work complete. Output each result (`✓ applied` / `N/A`):

| # | Check | Trigger | Required Action |
|---|-------|---------|-----------------|
| 1 | new endpoint | New controller action added | Add corresponding test in `tests/` |
| 2 | DB schema change | `server/db/schema.json` modified | FLAG `tools/db_generator.bat` |
| 3 | new contract used | New contract type in request/response | Verify exists in `shared/contracts/`; if missing → expand task scope |
| 4 | auth compliance | Any controller or handler modified | No uid from request body; `CancellationToken` passed through; `async/await` throughout; no `.Result`/`.Wait()` |
| 5 | AGENTS.md | New file/class/symbol added | Update `## Files` + `## Symbols` in affected leaf `AGENTS.md` |

## Cross-refs
| type | refs |
|------|------|
| Depends on | `docs/refs/platform-auth.md` |
| External API | `platform-auth:GET /.well-known/jwks.json`, `platform-auth:GET /api/internal/users/{pid}/uid` |
