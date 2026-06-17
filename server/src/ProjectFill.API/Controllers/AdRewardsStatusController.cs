using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ProjectFill.Application.Common;
using ProjectFill.Application.Rewards;
using ProjectFill.Application.Stage;
using ProjectFill.Contracts.Rewards;
using ProjectFill.Contracts.Ad;
using ProjectFill.Domain.Enums;
using StackExchange.Redis;

namespace ProjectFill.API.Controllers
{
    [ApiController]
    [Route("api/ad-rewards")]
    public sealed class AdRewardsStatusController : ControllerBaseEx
    {
        private readonly IDatabase _redis;
        private readonly AdRewardService _ads;
        private readonly AdDoubleRewardService _doubleRewards;

        public AdRewardsStatusController(
            IConnectionMultiplexer redis,
            AdRewardService ads,
            AdDoubleRewardService doubleRewards)
        {
            _redis = redis.GetDatabase();
            _ads = ads;
            _doubleRewards = doubleRewards;
        }

        [HttpGet("status/{adToken}")]
        public async Task<IActionResult> GetStatus(string adToken, CancellationToken ct)
        {
            var pendingKey = $"pending_claim:{adToken}";
            var ssvKey = $"ssv:{adToken}";

            var pendingData = await _redis.StringGetAsync(pendingKey);
            if (!pendingData.HasValue)
            {
                return Ok(new AdRewardStatusResponse
                {
                    Status = "GRANTED",
                    PlacementId = "UNKNOWN",
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            var pending = JsonSerializer.Deserialize<PendingAdClaim>(pendingData!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (pending is null)
                return BadRequest("Invalid pending claim payload.");

            var ssvTxId = await _redis.StringGetAsync(ssvKey);
            if (!ssvTxId.HasValue)
            {
                return Ok(new AdRewardStatusResponse
                {
                    Status = "PENDING",
                    PlacementId = pending.PlacementId,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }

            try
            {
                if (pending.PlacementId == AdPlacementKeys.AddLane)
                {
                    var res = await _ads.ClaimAsync(pending.UserId, new AdRewardClaimRequest
                    {
                        PlacementId = pending.PlacementId,
                        Provider = pending.Provider,
                        AdToken = pending.AdToken
                    }, pending.CorrelationId, ct);
                    await _redis.KeyDeleteAsync(pendingKey);

                    return Ok(new AdRewardStatusResponse
                    {
                        Status = "GRANTED",
                        PlacementId = pending.PlacementId,
                        GrantedRewards = res.GrantedRewards,
                        ServerTime = DateTimeOffset.UtcNow
                    });
                }
                else if (pending.PlacementId == AdPlacementKeys.DoubleRewardStageClear)
                {
                    var reqData = JsonDocument.Parse(pending.RequestJson);
                    int stageId = reqData.RootElement.GetProperty("stageId").GetInt32();
                    string attemptId = reqData.RootElement.GetProperty("attemptId").GetString() ?? string.Empty;

                    var req = new AdDoubleRewardRequest
                    {
                        StageId = stageId,
                        AttemptId = attemptId,
                        Provider = pending.Provider,
                        AdToken = pending.AdToken
                    };

                    var res = await _doubleRewards.ClaimAsync(pending.UserId, req, pending.CorrelationId, ct);
                    await _redis.KeyDeleteAsync(pendingKey);

                    return Ok(new AdRewardStatusResponse
                    {
                        Status = "GRANTED",
                        PlacementId = pending.PlacementId,
                        GrantedRewards = res.Rewards,
                        Currency = res.Currency,
                        ServerTime = DateTimeOffset.UtcNow
                    });
                }

                return BadRequest("Unsupported placement ID in pending claim.");
            }
            catch (GameApiException ex) when (ex.Code == ErrorCodes.AdSsvPending)
            {
                return Ok(new AdRewardStatusResponse
                {
                    Status = "PENDING",
                    PlacementId = pending.PlacementId,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (Exception)
            {
                await _redis.KeyDeleteAsync(pendingKey);
                throw;
            }
        }
    }
}
