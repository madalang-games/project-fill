# shared/contracts/Attendance

## Files
| file | class | role |
|------|-------|------|
| `AttendanceContracts.cs` | `AttendanceDayDto`, `AttendanceStatusResponse`, `AttendanceClaimResponse` | Daily attendance status + claim DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `AttendanceStatusResponse.CurrentDay` | property | Pending day to claim (1–7) |
| `AttendanceStatusResponse.ClaimedToday` | property | Whether today's reward is already claimed |
| `AttendanceClaimResponse.MilestoneRewardGroupId` | property | 0 when no cumulative milestone hit this claim |
| `AttendanceClaimResponse.UnlockedCosmetics` | property | Cosmetic ids unlocked by milestone condition |

## Rules
- Reuses `Rewards.GrantedRewardDto` and `Currency.CurrencySnapshot`; do not duplicate reward shapes.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.AttendanceController`
- Consumed by: `Game.Services.AttendanceApiService`
