using ProjectFill.Contracts.Ad;

namespace ProjectFill.Application.Stage;

// Stub: DOUBLE_REWARD_STAGE_CLEAR placement removed — not in Signal Sort design.
public sealed class AdDoubleRewardService
{
    public Task<AdDoubleRewardGrantResponse> ClaimAsync(long userId, AdDoubleRewardRequest request, string correlationId, CancellationToken ct)
        => throw new NotImplementedException("Double reward feature is not supported in Signal Sort.");
}
