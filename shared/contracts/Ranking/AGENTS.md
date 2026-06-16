# shared/contracts/Ranking

## Files
| file | class | role |
|------|-------|------|
| `RankingRequests.cs` | `RankingPageRequest` | Ranking paging request shape |
| `RankingResponses.cs` | `RankingPageResponse`, `MyRankingResponse`, `StageRankResponse`, `RankingRebuildResponse`, `RankingEntryDto` | Ranking API response DTOs |

## Symbols
| symbol | kind | note |
|--------|------|------|
| `RankingEntryDto.Rank` | property | Competition rank with deterministic tie-break for global rankings |
| `RankingEntryDto.Score` | property | Stars or max cleared stage depending on ranking type |
| `RankingRebuildResponse.Rebuilt` | property | True when admin-triggered Redis rebuild completed |
| `StageRankResponse.Rank` | property | Competition rank by best moves used; null when no clear record exists |
| `StageRankResponse.BestMovesUsed` | property | Best Signal Sort move count for this stage; null when no clear record exists |

## Rules
- DTOs only; no ranking logic.
- Global ranking types: `stars`, `max_stage`.
- `StageRankResponse` / `StageRank*` are retained stubs; per-stage ranking was removed from `RankingService` — do not wire until a new stage-ranking mechanic is designed.

## Cross-refs
- Consumed by: `ProjectFill.API.Controllers.RankingController`
