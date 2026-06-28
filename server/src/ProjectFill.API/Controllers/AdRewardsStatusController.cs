using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.Rewards;

namespace ProjectFill.API.Controllers
{
    [ApiController]
    [Route("api/ad-rewards")]
    public sealed class AdRewardsStatusController : ControllerBaseEx
    {
        private readonly AdRewardGrantCoordinator _coordinator;

        public AdRewardsStatusController(AdRewardGrantCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        // Polled by the client while a claim is AD_SSV_PENDING. Reward granting is callback-driven
        // (see conventions/ad-reward-ssv-system.md); this endpoint only reports state and, if the
        // SSV nonce has since arrived, triggers the deferred grant. A poll timeout is NOT a failure —
        // the SSV callback grants independently.
        [HttpGet("status/{adToken}")]
        public async Task<IActionResult> GetStatus(string adToken, CancellationToken ct)
        {
            var (outcome, response) = await _coordinator.TryGrantPendingAsync(adToken, ct);

            return outcome switch
            {
                AdGrantOutcome.Granted => Ok(response),
                AdGrantOutcome.StillPending => Ok(new AdRewardStatusResponse
                {
                    Status = "PENDING",
                    ServerTime = DateTimeOffset.UtcNow,
                }),
                // NotReady: no pending_claim — already granted (e.g. by the SSV callback) or none exists.
                _ => Ok(new AdRewardStatusResponse
                {
                    Status = "GRANTED",
                    PlacementId = "UNKNOWN",
                    ServerTime = DateTimeOffset.UtcNow,
                }),
            };
        }
    }
}
