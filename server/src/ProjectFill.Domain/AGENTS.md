# ProjectFill.Domain

## Nav
| path | role |
|------|------|
| `Enums/` | Server-internal enum types and string-constant discriminators | -> `Enums/AGENTS.md` |
| `Interfaces/` | Domain-owned interfaces consumed by API/Infrastructure | -> `Interfaces/AGENTS.md` |
| `Logging/` | EventLogIds constants | -> `Logging/AGENTS.md` |
| `StaticData/` | Generated static-data POCOs (DO NOT EDIT) | -> `StaticData/AGENTS.md` |
| `Utilities/` | Pure helper types | -> `Utilities/AGENTS.md` |

## Logging Convention
All user-modifying API calls must produce an `event_logs` row. Reward and ad changes link related records with `correlation_id`.

| artifact | path |
|----------|------|
| TrId master document | `server/db/event_log_definitions.json` |
| TrId constants | `server/src/ProjectFill.Domain/Logging/EventLogIds.cs` |
| Factory | `server/src/ProjectFill.Application/Logging/EventLogFactory.cs` |

## Auth Rules
- JWT `sub` is platform PID; it is not internal uid.
- Internal uid stays server-side and is injected as a claim by API middleware.

## Rules
- No external dependencies; pure domain layer.
- Entities, interfaces, constants, and pure helpers only.
- No EF Core attributes; mapping is generated in Infrastructure.
