using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.Rewards;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/rewards")]
public sealed class RewardsController : ControllerBaseEx
{
    private readonly RewardService _rewards;

    public RewardsController(RewardService rewards)
    {
        _rewards = rewards;
    }

    [HttpGet("sources")]
    public Task<RewardSourcesResponse> Sources(CancellationToken ct)
        => _rewards.GetSourcesAsync(PlayerId, ct);

    [HttpPost("claim")]
    public Task<RewardClaimResponse> Claim([FromBody] RewardClaimRequest request, CancellationToken ct)
        => _rewards.ClaimAsync(PlayerId, request.SourceId, CorrelationId, ct);
}
