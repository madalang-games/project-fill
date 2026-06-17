using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Stage;
using ProjectFill.Contracts.Ad;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/ad")]
public sealed class AdController : ControllerBaseEx
{
    private readonly AdInterstitialService _interstitial;
    private readonly AdDoubleRewardService _doubleReward;

    public AdController(AdInterstitialService interstitial, AdDoubleRewardService doubleReward)
    {
        _interstitial = interstitial;
        _doubleReward = doubleReward;
    }

    [HttpGet("eligibility")]
    public Task<AdEligibilityResponse> GetEligibility(CancellationToken ct)
        => _interstitial.GetEligibilityAsync(PlayerId, ct);

    [HttpPost("interstitial/shown")]
    public Task<AdInterstitialShownResponse> InterstitialShown([FromBody] AdInterstitialShownRequest request, CancellationToken ct)
        => _interstitial.RecordShownAsync(PlayerId, request.StageId, CorrelationId, ct);

    [HttpPost("double-reward")]
    public Task<AdDoubleRewardGrantResponse> DoubleReward([FromBody] AdDoubleRewardRequest request, CancellationToken ct)
        => _doubleReward.ClaimAsync(PlayerId, request, CorrelationId, ct);
}
