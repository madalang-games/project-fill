# shared/contracts/Player

## Files
| file | class | role |
|------|-------|------|
| `ProfileContracts.cs` | `UserProfileUpdateRequest`, `UserProfileUpdateResponse` | Profile update DTOs |
| `PlayerProgressResponses.cs` | `PlayerProgressResponse` | Player-owned cosmetic and no-ads progress DTO |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `UserProfileUpdateRequest.DisplayName` | property | Optional display name update |
| `UserProfileUpdateRequest.AvatarId` | property | Optional avatar selection update |
| `PlayerProgressResponse.UnlockedAvatarIds` | property | Cosmetic avatar unlock ids |
| `PlayerProgressResponse.IsNoAds` | property | No-ads entitlement flag |

## Rules
- Player contracts are account/profile focused; stage progress is handled by ranking/progression services.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.PlayerController`
- Consumed by: `Game.Services.PlayerApiService`
