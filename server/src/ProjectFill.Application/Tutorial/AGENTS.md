# ProjectFill.Application.Tutorial — Tutorial Service

Namespace: `ProjectFill.Application.Tutorial`

## Files
| file | class | role |
|------|-------|------|
| `TutorialService.cs` | `TutorialService` | Application service querying and saving tutorial progress to DB |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `TutorialService.GetCompletedTutorialIdsAsync` | method | Returns list of completed tutorial group/step IDs |
| `TutorialService.CompleteTutorialAsync` | method | Saves a new completed tutorial ID to DB |

## Rules
- DB persistence uses injected `AppDbContext`.
- Standard async patterns with CancellationToken.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.TutorialController`
- Depends on: `ProjectFill.Infrastructure.Generated.AppDbContext`
