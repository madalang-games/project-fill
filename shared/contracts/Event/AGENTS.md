# shared/contracts/Event

## Files
| file | class | role |
|------|-------|------|
| `WeeklyMissionContracts.cs` | `WeeklyMissionResponse`, `WeeklyMissionDto`, `WeeklyMissionMilestoneDto`, `ClaimWeeklyMissionResponse` | Weekly Mission Event status (5 missions + progress + cumulative EP + track claim state) and milestone-claim result |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `WeeklyMissionResponse.WeekStartDate` | property | `yyyy-MM-dd` Monday UTC; same boundary as weekly ranking |
| `WeeklyMissionResponse.DaysRemaining` | property | Whole days until Sunday 24:00 UTC expiry |
| `WeeklyMissionDto.ConditionType` | property | `WeeklyMissionConditionType` as int |
| `WeeklyMissionDto.Progress` / `TargetValue` | property | Current aggregate / completion target |
| `WeeklyMissionMilestoneDto.IsReached` | property | `TotalEp >= EpThreshold` |
| `WeeklyMissionMilestoneDto.IsClaimed` | property | Milestone reward already claimed this week |

## Rules
- `netstandard2.1` only; DTOs only, no logic.
- Reuses `Rewards.GrantedRewardDto` and `Currency.CurrencySnapshot`.
- No mission-submit DTO: progress is aggregated server-side in the stage-clear flow.
- Claim threshold is passed in the route (`/claim/{threshold}`), not a request body.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.WeeklyMissionController`
- Consumed by: `Game.Services.WeeklyMissionApiService`
- Gen output: `client/project-fill/Assets/Scripts/Generated/Contracts/` (via `pkt_generator`)
