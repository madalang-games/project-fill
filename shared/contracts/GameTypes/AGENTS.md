# shared/contracts/GameTypes

## Files
| file | class | role |
|------|-------|------|
| `GameEnums.cs` | Multiple enums | Shared game-type enums for IAP, tutorial, and UI targeting |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `IapProductType.NonConsumable` | enum value | 0 |
| `IapProductType.Consumable` | enum value | 1 |
| `PurchaseResetPeriod.None` | enum value | 0 |
| `PurchaseResetPeriod.Daily` | enum value | 1 |
| `PurchaseResetPeriod.Weekly` | enum value | 2 |
| `PurchaseResetPeriod.Monthly` | enum value | 3 |
| `TutorialTriggerType.FirstLaunch` | enum value | 0 |
| `TutorialTriggerType.GimmickAppear` | enum value | 1 |
| `TutorialTriggerType.FailRepeat` | enum value | 2 |
| `TutorialContentType.FingerOverlay` | enum value | 0 |
| `TutorialContentType.Tooltip` | enum value | 1 |
| `TutorialContentType.HighlightOnly` | enum value | 2 |
| `TargetSpaceType.UI` | enum value | 0 |
| `TargetSpaceType.World` | enum value | 1 |

## Rules
- Namespace: `ProjectFill.Contracts.GameTypes`
- No game-mechanic specific enums here — keep generic (IAP, tutorial, UI)
- When adding a new enum: update `## Symbols` above

## Cross-refs
- Consumed by: `client/Assets/Scripts/` (via pkt_generator sync)
- Consumed by: `ProjectFill.Application`, `ProjectFill.API`
