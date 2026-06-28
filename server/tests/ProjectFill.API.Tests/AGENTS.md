# ProjectFill.API.Tests

## Files
| file | class | role |
|------|-------|------|
| `ProjectFill.API.Tests.csproj` | project | xUnit API integration test project |
| `UserClaimsTests.cs` | `UserClaimsTests` | Verifies internal uid claim is separate from JWT `sub` PID |
| `VersionCheckMiddlewareTests.cs` | `VersionCheckMiddlewareTests` | Verifies version/protocol gate behavior |
| `AccountConflictTests.cs` | `AccountConflictTests` | Guest→Google link conflict and resolve-conflict flow |
| `PlayerServiceTests.cs` | `PlayerServiceTests` | Player progress query (max cleared stage, best stars) |
| `RankingServiceTests.cs` | `RankingServiceTests` | Global ranking my-rank (stage/perfect) and not-in-redis null entry |
| `StageServiceTests.cs` | `StageServiceTests` | Stage start unlock gate (stage1 open / locked / unlocked-after-clear / unknown) + start issues `SessionId`; clear-of-locked bypass guard + attempt-token validation (missing / mismatched → `InvalidStageAttempt`); stage clear: first/re-clear, best-moves, chapter milestone chest, ruleset/types validation |
| `AdDoubleRewardServiceTests.cs` | `AdDoubleRewardServiceTests` | Result 2x reward: verified grant + claim-state once, duplicate, SSV-pending, not-cleared/unknown-stage guards |
| `IapServiceTests.cs` | `IapServiceTests` | IAP verify: mock-platform rejected in prod / allowed in dev, NonConsumable→no-ads flag, duplicate order guard, purchase-limit enforcement |
| `CurrencyServiceTests.cs` | `CurrencyServiceTests` | Soft currency read/grant/spend: lazy-row read returns 0 (not null), stored balance read, grant new-balance+delta, spend deduct+delta, insufficient throws — locks the balance the client mirrors (guards "보유 재화 0") |
| `TutorialServiceTests.cs` | `TutorialServiceTests` | Tutorial progress get/complete persistence |
| `FakeStaticData.cs` | `FakeStaticData` | Shared `IStaticDataService` test fake; safe empty/null defaults, override per test |
| `CosmeticServiceTests.cs` | `CosmeticServiceTests` | Cosmetic gold unlock, condition unlock, equip validation |
| `AttendanceServiceTests.cs` | `AttendanceServiceTests` | Attendance cycle/streak advance, duplicate guard, milestone cosmetic unlock |
| `AchievementServiceTests.cs` | `AchievementServiceTests` | Derived progress, claim + cosmetic unlock, report-progress seam |
| `WeeklyMissionServiceTests.cs` | `WeeklyMissionServiceTests` | Status shape, progress/EP accrual, milestone claim + duplicate/below/invalid guards, full-track achievement seam |
| `CheatCommandParserTests.cs` | `CheatCommandParserTests` | Parser valid/malformed (INVALID_COMMAND), catalog domain coverage, docs HTML render |
| `CheatGateTests.cs` | `CheatGateTests` | `CheatWhitelistFilter` allow/403, `DevOnlyMiddleware` 404/passthrough |
| `CheatServiceTests.cs` | `CheatServiceTests` | gold clamp, item single/all/clamp/unknown, stage set/trim, tutorial set/clear/all-true guard, ad redis toggle, cosmetic unlock/all/lock/unknown, achievement complete/all/reset/unknown, attendance setday-clamp/reset, dispatcher integration |
| `TestConfig.cs` | `TestConfig` | Shared `ProjectFillConfiguration` builder varying dev-gate knobs |

## Rules
- Keep tests deterministic and engine-free
- Do not connect to real MySQL, Redis, or platform auth from this project
- In-memory EF database name shared across factory lifetime; seed data inserted once in CreateHost
- Per-test isolation: use Guid-named in-memory DB instances
