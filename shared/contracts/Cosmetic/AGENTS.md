# shared/contracts/Cosmetic

## Files
| file | class | role |
|------|-------|------|
| `CosmeticContracts.cs` | `CosmeticItemDto`, `ActiveCosmeticsDto`, `CosmeticListResponse`, `UnlockCosmeticResponse`, `SetActiveCosmeticRequest`, `SetActiveCosmeticResponse` | Cosmetic catalog, unlock, and active-equip DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `CosmeticItemDto.Category` | property | `CosmeticCategory` int value |
| `CosmeticItemDto.UnlockType` | property | `CosmeticUnlockType` int value |
| `CosmeticItemDto.Unlocked` | property | Whether the current player owns it |
| `ActiveCosmeticsDto.UseCustomBoardSkin` | property | Override chapter theme with custom board skin |
| `UnlockCosmeticResponse.Currency` | property | Updated gold balance after a Gold unlock |

## Rules
- Cosmetic id is a string; never an int target id.
- Enum fields serialized as int (`CosmeticCategory`, `CosmeticUnlockType` in `GameTypes`).
- No pay-to-win: cosmetics never carry gameplay fields.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.CosmeticController`
- Consumed by: `Game.Services.CosmeticApiService`
