using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Common;
using ProjectFill.Application.Inventory;
using ProjectFill.Contracts.Rewards;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFill.Application.Rewards;

public sealed class AdRewardService
{
    private readonly AppDbContext _db;
    private readonly IDatabase _redis;
    private readonly InventoryService _inventory;
    private readonly IAdRewardVerifier _adVerifier;

    public AdRewardService(AppDbContext db, IConnectionMultiplexer redis, InventoryService inventory, IAdRewardVerifier adVerifier)
    {
        _db = db;
        _redis = redis.GetDatabase();
        _inventory = inventory;
        _adVerifier = adVerifier;
    }

    public async Task<AdRewardClaimResponse> ClaimAsync(long userId, AdRewardClaimRequest request, string correlationId, CancellationToken ct)
    {
        if (request.PlacementId == "ADD_LANE")
        {
            var verifyResult = await _adVerifier.VerifyAsync(request.Provider, request.AdToken, ct);
            if (!verifyResult.Verified)
            {
                var pending = new PendingAdClaim
                {
                    UserId = userId,
                    PlacementId = "ADD_LANE",
                    Provider = request.Provider,
                    AdToken = request.AdToken,
                    ContextType = "item_grant",
                    ContextId = "1",
                    RequestJson = "{}",
                    CorrelationId = correlationId
                };
                await _redis.StringSetAsync($"pending_claim:{request.AdToken}", System.Text.Json.JsonSerializer.Serialize(pending), TimeSpan.FromMinutes(5));
                throw new GameApiException(ErrorCodes.AdSsvPending, "Ad SSV callback not yet received.");
            }

            var existing = await _db.AdRewardTransactions.Query()
                .FirstOrDefaultAsync(x => x.Provider == request.Provider && x.ProviderTxId == verifyResult.ProviderTxId, ct);
            if (existing is not null)
            {
                return new AdRewardClaimResponse
                {
                    Granted = false,
                    Duplicate = true,
                    PlacementId = request.PlacementId,
                    GrantedRewards = new List<GrantedRewardDto>(),
                    ServerTime = DateTimeOffset.UtcNow,
                };
            }

            var tx = new AdRewardTransactionsRow
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = userId,
                PlacementId = "ADD_LANE",
                RewardType = "ITEM",
                RewardValue = 1,
                ContextType = "item_grant",
                ContextId = "1",
                Provider = request.Provider,
                ProviderTxId = verifyResult.ProviderTxId,
                Status = "granted",
                CorrelationId = correlationId,
                CreatedAt = DateTimeOffset.UtcNow,
                VerifiedAt = DateTimeOffset.UtcNow,
                GrantedAt = DateTimeOffset.UtcNow,
            };
            _db.AdRewardTransactions.Insert(tx);

            // Grant 1 Add Lane item (item ID 1)
            await _inventory.GrantItemAsync(userId, 1, 1, $"ad_claim:{request.PlacementId}", correlationId, ct);
            await _db.SaveAsync(ct);

            return new AdRewardClaimResponse
            {
                Granted = true,
                Duplicate = false,
                PlacementId = request.PlacementId,
                GrantedRewards = new List<GrantedRewardDto>
                {
                    new()
                    {
                        RewardType = "ITEM",
                        TargetId = 1,
                        Amount = 1,
                        DurationSeconds = 0,
                    },
                },
                ServerTime = DateTimeOffset.UtcNow,
            };
        }

        throw new GameApiException("AD_PLACEMENT_NOT_SUPPORTED", "Ad placement is not supported by the generic ad reward API.");
    }
}
