using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Ranking;
using ProjectFill.Contracts.Ranking;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/rankings")]
public sealed class RankingController : ControllerBaseEx
{
    private readonly RankingService _ranking;

    public RankingController(RankingService ranking)
    {
        _ranking = ranking;
    }

    [HttpGet("global/{type}")]
    public Task<RankingPageResponse> GetGlobal(string type, [FromQuery] int offset = 0, [FromQuery] int limit = 50, CancellationToken ct = default)
        => _ranking.GetGlobalPageAsync(type, offset, limit, ct);

    [HttpGet("global/{type}/me")]
    public Task<MyRankingResponse> GetMyGlobal(string type, CancellationToken ct)
        => _ranking.GetMyGlobalRankAsync(PlayerId, type, ct);

    [HttpGet("stages/{stageId:int}/me")]
    public Task<StageRankResponse> GetMyStageRank(int stageId, CancellationToken ct)
        => Task.FromResult(new StageRankResponse { StageId = stageId });

    [HttpPost("admin/rebuild")]
    public async Task<RankingRebuildResponse> Rebuild(CancellationToken ct)
    {
        await _ranking.RebuildAllAsync(ct);
        return new RankingRebuildResponse
        {
            Rebuilt = true,
            ServerTime = DateTimeOffset.UtcNow,
        };
    }
}
