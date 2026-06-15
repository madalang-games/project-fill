# Contracts/Tutorial — Tutorial DTOs

Namespace: `ProjectFill.Contracts.Tutorial`

## Files
| file | class | role |
|------|-------|------|
| `TutorialContracts.cs` | `TutorialProgressResponse` | DTO representing the list of completed tutorial IDs |
| `TutorialContracts.cs` | `TutorialProgressUpdateResponse` | DTO returned after updating tutorial completion progress |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `TutorialProgressResponse.CompletedTutorialIds` | property | List of completed tutorial group IDs |
| `TutorialProgressUpdateResponse.Success` | property | Status of the update request |
| `TutorialProgressUpdateResponse.CompletedTutorialIds` | property | Synced list of completed tutorial group IDs |

## Rules
- Contracts are shared across client and server. Re-run `tools/pkt_generator.bat` or `tools/all_generator.bat` if modifications are made.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.TutorialController`
- Consumed by: `Game.Services.TutorialApiService`
