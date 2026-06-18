# ProjectFill.API/Dev

Dev-only cheat command pipeline (parse → dispatch → docs). Reachable only when `DevOnlyMiddleware`
allows `/api/dev/*` (GAME_ENV == dev) and `CheatWhitelistFilter` passes.

## Files
| file | class | role |
|------|-------|------|
| `CheatCommandCatalog.cs` | `CheatCommandCatalog`, `CheatCommandSpec` | Single source of truth for command metadata (domain/token/syntax/desc/example/response) |
| `CheatCommandParser.cs` | `CheatCommandParser`, `ParsedCheatCommand` | `/{domain} args…` → `ParsedCheatCommand`; validates domain against catalog |
| `CheatDispatcher.cs` | `CheatDispatcher` | Domain branch: per-domain arity + action-token→`CheatAction` mapping → `CheatService` calls |
| `CheatDocsPage.cs` | `CheatDocsPage` | Renders catalog to self-contained HTML for `GET /api/dev/cheat/docs` |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CheatCommandCatalog.All` | field | Catalog rows; iterated by parser/docs |
| `CheatCommandCatalog.TryGet` | method | Domain-token lookup (case-insensitive) |
| `CheatCommandParser.Parse` | method | Throws `GameApiException(InvalidCommand)` on malformed/unknown-domain input |
| `CheatDispatcher.DispatchAsync` | method | Returns `CheatCommandResponse`; all validation failures → `InvalidCommand` (400) |
| `CheatDocsPage.Render` | method | `(gameEnv, viewerPid)` → HTML string; HtmlEncodes catalog values |

## Rules
- Add a command = add one `CheatCommandSpec` row; parser/docs/client-buttons all read it (no drift).
- Branching is on `CheatDomain`/`CheatAction` enums; raw token strings live only in the catalog/parser.

## Cross-refs
- Depends on: `ProjectFill.Application.Cheat.CheatService`, `ProjectFill.Domain.Enums.CheatDomain`/`CheatAction`
- Consumed by: `ProjectFill.API.Controllers.DevCheatController`
