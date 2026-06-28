using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectFill.Application.Common;
using ProjectFill.Application.Stage;
using ProjectFill.Contracts.Ad;
using ProjectFill.Contracts.Rewards;
using ProjectFill.Domain.Enums;
using StackExchange.Redis;

namespace ProjectFill.Application.Rewards;

public enum AdGrantOutcome
{
    /// No pending_claim for this nonce — the callback arrived before the client claim, or the reward was already granted.
    NotReady,
    /// Reward granted (or already granted; idempotent).
    Granted,
    /// pending_claim exists but the SSV nonce has not been verified yet.
    StillPending,
}

/// <summary>
/// Single grant dispatcher shared by the SSV callback and the status endpoint.
/// Grants a pending rewarded-ad reward keyed by nonce, idempotently, regardless of which
/// trigger (callback or client claim/poll) reaches it first. See
/// conventions/ad-reward-ssv-system.md (Model B).
/// </summary>
public sealed class AdRewardGrantCoordinator
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IDatabase _redis;
    private readonly AdRewardService _ads;
    private readonly AdDoubleRewardService _doubleRewards;
    private readonly ILogger<AdRewardGrantCoordinator> _logger;

    public AdRewardGrantCoordinator(
        IConnectionMultiplexer redis,
        AdRewardService ads,
        AdDoubleRewardService doubleRewards,
        ILogger<AdRewardGrantCoordinator> logger)
    {
        _redis = redis.GetDatabase();
        _ads = ads;
        _doubleRewards = doubleRewards;
        _logger = logger;
    }

    public async Task<(AdGrantOutcome Outcome, AdRewardStatusResponse? Response)> TryGrantPendingAsync(string nonce, CancellationToken ct)
    {
        var pendingKey = $"pending_claim:{nonce}";
        var pendingData = await _redis.StringGetAsync(pendingKey);
        if (!pendingData.HasValue)
            return (AdGrantOutcome.NotReady, null);

        var pending = JsonSerializer.Deserialize<PendingAdClaim>(pendingData!, JsonOpts);
        if (pending is null)
        {
            await _redis.KeyDeleteAsync(pendingKey);
            return (AdGrantOutcome.NotReady, null);
        }

        try
        {
            AdRewardStatusResponse response;
            if (pending.PlacementId == AdPlacementKeys.DoubleRewardStageClear)
            {
                using var reqData = JsonDocument.Parse(pending.RequestJson);
                int stageId = reqData.RootElement.GetProperty("stageId").GetInt32();
                string attemptId = reqData.RootElement.GetProperty("attemptId").GetString() ?? string.Empty;

                var req = new AdDoubleRewardRequest
                {
                    StageId = stageId,
                    AttemptId = attemptId,
                    Provider = pending.Provider,
                    AdToken = pending.AdToken,
                };
                var res = await _doubleRewards.ClaimAsync(pending.UserId, req, pending.CorrelationId, ct);
                response = new AdRewardStatusResponse
                {
                    Status = "GRANTED",
                    PlacementId = pending.PlacementId,
                    GrantedRewards = res.Rewards,
                    Currency = res.Currency,
                    ServerTime = DateTimeOffset.UtcNow,
                };
            }
            else
            {
                // Generic ad reward placements (e.g. AddLane).
                var res = await _ads.ClaimAsync(pending.UserId, new AdRewardClaimRequest
                {
                    PlacementId = pending.PlacementId,
                    Provider = pending.Provider,
                    AdToken = pending.AdToken,
                }, pending.CorrelationId, ct);
                response = new AdRewardStatusResponse
                {
                    Status = "GRANTED",
                    PlacementId = pending.PlacementId,
                    GrantedRewards = res.GrantedRewards,
                    ServerTime = DateTimeOffset.UtcNow,
                };
            }

            await _redis.KeyDeleteAsync(pendingKey);
            _logger.LogInformation("Ad reward granted. placementId={PlacementId} userId={UserId}", pending.PlacementId, pending.UserId);
            return (AdGrantOutcome.Granted, response);
        }
        catch (GameApiException ex) when (ex.Code == ErrorCodes.AdSsvPending)
        {
            // SSV nonce not verified yet; the placement service re-created pending_claim.
            // Leave it for the next trigger (callback arrival or later poll).
            return (AdGrantOutcome.StillPending, null);
        }
    }
}
