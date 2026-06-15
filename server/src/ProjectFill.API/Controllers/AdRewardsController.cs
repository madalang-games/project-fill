using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.Rewards;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/ad-rewards")]
public sealed class AdRewardsController : ControllerBaseEx
{
    private readonly AdRewardService _ads;

    public AdRewardsController(AdRewardService ads)
    {
        _ads = ads;
    }

    [HttpPost("claim")]
    public Task<AdRewardClaimResponse> Claim([FromBody] AdRewardClaimRequest request, CancellationToken ct)
        => _ads.ClaimAsync(PlayerId, request, CorrelationId, ct);
}
