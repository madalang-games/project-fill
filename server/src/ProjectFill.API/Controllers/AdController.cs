using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Stage;
using ProjectFill.Contracts.Ad;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/ad")]
public sealed class AdController : ControllerBaseEx
{
    private readonly AdInterstitialService _interstitial;

    public AdController(AdInterstitialService interstitial)
    {
        _interstitial = interstitial;
    }

    [HttpGet("eligibility")]
    public Task<AdEligibilityResponse> GetEligibility(CancellationToken ct)
        => _interstitial.GetEligibilityAsync(PlayerId, ct);

    [HttpPost("interstitial/shown")]
    public Task<AdInterstitialShownResponse> InterstitialShown([FromBody] AdInterstitialShownRequest request, CancellationToken ct)
        => _interstitial.RecordShownAsync(PlayerId, request.StageId, CorrelationId, ct);
}
