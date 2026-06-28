using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Common;
using ProjectFill.Application.Logging;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.Ad;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.Rewards;
using ProjectFill.Domain.Enums;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFill.Application.Stage;

// Result-screen 2x reward: re-grants the stage's clear reward group once after a verified rewarded
// ad. Doubling is allowed only when the stage was actually cleared (reward earned) and only once per
// stage (idempotent via the `double_reward:{stageId}` claim-state row). Ad verification reuses the
// generic SSV pipeline (IAdRewardVerifier); an unverified token is stashed for the SSV-callback
// polling path handled by AdRewardsStatusController.
public sealed class AdDoubleRewardService
{
    private const int RewardVersion = 1; // mirrors StageService reward group version

    private readonly AppDbContext _db;
    private readonly IDatabase _redis;
    private readonly IStaticDataService _staticData;
    private readonly RewardService _reward;
    private readonly IAdRewardVerifier _adVerifier;

    public AdDoubleRewardService(
        AppDbContext db,
        IConnectionMultiplexer redis,
        IStaticDataService staticData,
        RewardService reward,
        IAdRewardVerifier adVerifier)
    {
        _db = db;
        _redis = redis.GetDatabase();
        _staticData = staticData;
        _reward = reward;
        _adVerifier = adVerifier;
    }

    public async Task<AdDoubleRewardGrantResponse> ClaimAsync(long userId, AdDoubleRewardRequest request, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var stage = _staticData.GetStage(request.StageId)
            ?? throw new GameApiException(ErrorCodes.StageNotFound, $"Stage {request.StageId} not found.");

        // Can only double a reward that was actually earned (stage first-cleared).
        var progress = await _db.UserStageProgress.FindAsync(userId, request.StageId, ct);
        if (progress is null || !progress.StageClear)
            throw new GameApiException(ErrorCodes.DoubleRewardNotEligible, "Stage has not been cleared.");

        // Idempotency: one double-reward grant per stage, ever.
        var claimKey = $"double_reward:{request.StageId}";
        var alreadyDoubled = await _db.UserRewardClaimState.FindAsync(userId, claimKey, "once", ct);
        if (alreadyDoubled is not null)
            return Duplicate(now);

        // Verify the rewarded ad. Unverified → stash pending for the SSV-callback polling path
        // (AdRewardsStatusController re-drives this claim once the SSV nonce lands in Redis).
        var verify = await _adVerifier.VerifyAsync(request.Provider, request.AdToken, ct);
        if (!verify.Verified)
        {
            var pending = new PendingAdClaim
            {
                UserId = userId,
                PlacementId = AdPlacementKeys.DoubleRewardStageClear,
                Provider = request.Provider,
                AdToken = request.AdToken,
                ContextType = "double_reward",
                ContextId = request.StageId.ToString(),
                RequestJson = System.Text.Json.JsonSerializer.Serialize(new { stageId = request.StageId, attemptId = request.AttemptId }),
                CorrelationId = correlationId,
            };
            await _redis.StringSetAsync($"pending_claim:{request.AdToken}", System.Text.Json.JsonSerializer.Serialize(pending), TimeSpan.FromHours(24));
            throw new GameApiException(ErrorCodes.AdSsvPending, "Ad SSV callback not yet received.");
        }

        // Provider-tx replay guard: the same verified ad token cannot grant twice.
        var existingTx = await _db.AdRewardTransactions.Query()
            .FirstOrDefaultAsync(x => x.Provider == request.Provider && x.ProviderTxId == verify.ProviderTxId, ct);
        if (existingTx is not null)
            return Duplicate(now);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var (granted, currency) = await _reward.GrantRewardGroupAsync(userId, stage.RewardGroupId, RewardVersion, correlationId, ct);

        var adTxId = Guid.NewGuid().ToString("N");
        _db.AdRewardTransactions.Insert(new AdRewardTransactionsRow
        {
            Id = adTxId,
            UserId = userId,
            PlacementId = AdPlacementKeys.DoubleRewardStageClear,
            RewardType = "REWARD_GROUP",
            RewardValue = stage.RewardGroupId,
            ContextType = "double_reward",
            ContextId = request.StageId.ToString(),
            Provider = request.Provider,
            ProviderTxId = verify.ProviderTxId,
            Status = "granted",
            CorrelationId = correlationId,
            CreatedAt = now,
            VerifiedAt = now,
            GrantedAt = now,
        });

        _db.UserRewardClaimState.Insert(new UserRewardClaimStateRow
        {
            UserId = userId,
            SourceId = claimKey,
            PeriodKey = "once",
            ClaimCount = 1,
            LastClaimedAt = now,
            UpdatedAt = now,
        });

        _db.EventLogs.Insert(EventLogFactory.AdDoubleRewardGranted(userId, correlationId, adTxId, request.StageId, request.AttemptId));

        await _db.SaveAsync(ct);
        await tx.CommitAsync(ct);

        return new AdDoubleRewardGrantResponse
        {
            Granted = true,
            Duplicate = false,
            Rewards = granted,
            Currency = currency,
            ServerTime = now,
        };
    }

    private static AdDoubleRewardGrantResponse Duplicate(DateTimeOffset now) => new()
    {
        Granted = false,
        Duplicate = true,
        Rewards = new List<GrantedRewardDto>(),
        ServerTime = now,
    };
}
