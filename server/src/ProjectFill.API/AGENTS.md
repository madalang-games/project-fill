# ProjectFill.API

## Nav
| path | role |
|------|------|
| `Controllers/` | All HTTP endpoints | -> `Controllers/AGENTS.md` |
| `Middleware/` | Request pipeline: correlation ID, exception mapping, user-id resolution, dev-only gate, version check | -> `Middleware/AGENTS.md` |
| `Filters/` | MVC action filters, including per-user write serialization | -> `Filters/AGENTS.md` |
| `Dev/` | Dev-only cheat parser/dispatcher/catalog/docs | -> `Dev/AGENTS.md` |
| `Program.cs` | ASP.NET Core entry point: DI registrations, middleware order |
| `RankingRebuildHostedService.cs` | Startup Redis ranking rebuild from DB |
| `ProjectFillConfiguration.cs` | Strict env/config loader for deploy/runtime values |
| `UserClaims.cs` | Claim helpers for internal `user_id` and platform PID |
| `ShortSourceContextEnricher.cs` | Serilog enricher that shortens SourceContext values to class names |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UserClaims.UserId` | const | Internal server-side uid claim added by `UserIdResolutionMiddleware` |
| `UserClaims.PlatformPid` | const | JWT `sub`; platform PID, not numeric uid |
| `ProjectFillConfiguration.RateLimit.StageStartPerHour` | property | Stage-start limiter setting; env `RateLimit:StageStartPerHour`; default 720 |
| `ProjectFillConfiguration.RateLimit.TransactionalPerMinute` | property | Redis sliding-window limit for start/clear/reward per user per minute; env `RateLimit:TransactionalPerMinute`; default 30 |
| `ProjectFillConfiguration.AdReward.VerifyMode` | property | Required `AD_REWARD_VERIFY_MODE`; allowed values: `mock`, `ssv` |
| `RankingRebuildHostedService.ExecuteAsync` | method | Rebuilds Redis ranking keys on init; logs warning without blocking startup |
| `ProjectFillConfiguration.Dev.Enabled` | property | True only when `GAME_ENV == dev`; gates `/api/dev/*` (404 otherwise) |
| `ProjectFillConfiguration.Dev.CheatWhitelist` | property | Env `DEV_CHEAT_WHITELIST` (CSV of platform PIDs); empty ⇒ allow all in dev |

## Rules
- JWT `sub` is platform PID; controllers read internal `user_id` claim added by `UserIdResolutionMiddleware`.
- Unauthenticated endpoints: `GET /health` when present; auth/refresh/logout are platform-auth responsibilities.
- Middleware order in `Program.cs`: CorrelationId -> SerilogRequestLogging -> ApiException -> DevOnly -> Auth -> UserIdResolution -> Authorization -> RateLimit -> VersionCheck -> Controllers. (`DevOnly` precedes Auth so prod `/api/dev/*` 404s before any auth challenge.)
- No `SessionValidationMiddleware`; Project Fill has no local session active/revocation state.
- `LOG_LEVEL` env var overrides `appsettings.*.json` Serilog minimum level.
- `AD_REWARD_VERIFY_MODE` controls ad reward verification independently from `GAME_ENV`.
