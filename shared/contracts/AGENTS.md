# shared/contracts - C# DTO Contracts

## Overview
Target: `netstandard2.1` (Unity-compatible).
Namespace root: `ProjectFill.Contracts.[Domain]`.
File format: `[Domain]Requests.cs`, `[Domain]Responses.cs`.

Consumed by:
- Server: via `<ProjectReference>` in `ProjectFill.API.csproj` and `ProjectFill.Application.csproj`
- Client: auto-synced via `pkt_generator` -> `Assets/Scripts/Generated/Contracts/`

## Nav
| path | role |
|------|------|
| `Common/` | Shared error/result types |
| `GameTypes/` | Shared game-type enums (IAP, tutorial types) |
| `Rewards/` | Generic reward source and ad reward DTOs |
| `Currency/` | Soft currency snapshot DTO |
| `Ad/` | Ad eligibility, interstitial, double reward DTOs |
| `Ranking/` | Global and stage ranking DTOs |
| `Account/` | Guest/social login, link, profile, and auth DTOs |
| `Bootstrap/` | Client config, meta hash, and maintenance DTOs |
| `Tutorial/` | Tutorial progress update DTOs |
| `Iap/` | IAP verification and product status DTOs |
| `Inventory/` | Signal Sort booster inventory DTOs |
| `Player/` | Player profile and progress DTOs |
| `Stage/` | Signal Sort campaign stage-clear request/response DTOs |
| `Cosmetic/` | Cosmetic catalog, unlock, and active-equip DTOs |
| `Attendance/` | Daily attendance status + claim DTOs |
| `Achievement/` | Achievement list + claim DTOs |
| `DailyChallenge/` | Daily challenge today/clear/ranking/streak DTOs |

## Rules
- `netstandard2.1` only; no C# 10+ features, no nullable reference types at project level (use `#nullable enable` per file).
- No business logic in DTOs; plain properties only.
- File naming: `[Domain]Requests.cs` and `[Domain]Responses.cs` per domain.
- When adding a domain: create the subdirectory + both files + update Nav above.
- Signal Sort is the current baseline. Do not add turn-limit, stamina, tube/bottle sort, or non-circuit-theme contracts unless the design SoT (signal_sort_system_design_kr.md) changes first.

## Cross-refs
- Gen output: `client/project-fill/Assets/Scripts/Generated/Contracts/` (via `pkt_generator`)
- Consumed by: `ProjectFill.API`, `ProjectFill.Application`
