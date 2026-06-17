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
| `StageServiceTests.cs` | `StageServiceTests` | Stage start unlock gate (stage1 open / locked / unlocked-after-clear / unknown) + clear-of-locked bypass guard; stage clear: first/re-clear, best-moves, chapter milestone chest, ruleset/types validation |
| `AdDoubleRewardServiceTests.cs` | `AdDoubleRewardServiceTests` | Result 2x reward: verified grant + claim-state once, duplicate, SSV-pending, not-cleared/unknown-stage guards |
| `TutorialServiceTests.cs` | `TutorialServiceTests` | Tutorial progress get/complete persistence |
| `FakeStaticData.cs` | `FakeStaticData` | Shared `IStaticDataService` test fake; safe empty/null defaults, override per test |
| `CosmeticServiceTests.cs` | `CosmeticServiceTests` | Cosmetic gold unlock, condition unlock, equip validation |
| `AttendanceServiceTests.cs` | `AttendanceServiceTests` | Attendance cycle/streak advance, duplicate guard, milestone cosmetic unlock |
| `AchievementServiceTests.cs` | `AchievementServiceTests` | Derived progress, claim + cosmetic unlock, report-progress seam |
| `DailyChallengeServiceTests.cs` | `DailyChallengeServiceTests` | Deterministic challenge, clear/streak, duplicate guard, ranking order |

## Rules
- Keep tests deterministic and engine-free
- Do not connect to real MySQL, Redis, or platform auth from this project
- In-memory EF database name shared across factory lifetime; seed data inserted once in CreateHost
- Per-test isolation: use Guid-named in-memory DB instances
