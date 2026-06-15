using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ProjectFill.Application.Common;
using ProjectFill.Contracts.Common;
using StackExchange.Redis;

namespace ProjectFill.API.Middleware
{
    public sealed class RateLimitingMiddleware
    {
        private static readonly Regex StageStartRegex = new Regex(@"^/api/stages/(?<stageId>\d+)/attempts/start$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex StageClearRegex = new Regex(@"^/api/stages/(?<stageId>\d+)/attempts/[^/]+/clear$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly RequestDelegate _next;
        private readonly ProjectFillConfiguration _config;

        public RateLimitingMiddleware(RequestDelegate next, ProjectFillConfiguration config)
        {
            _next   = next;
            _config = config;
        }

        public async Task InvokeAsync(HttpContext context, IConnectionMultiplexer redis)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;

            // 1. Identify transactional endpoints & check early stage bypass
            bool isTransactional = false;
            int stageId = 0;

            if (method == "POST")
            {
                var startMatch = StageStartRegex.Match(path);
                if (startMatch.Success)
                {
                    isTransactional = true;
                    int.TryParse(startMatch.Groups["stageId"].Value, out stageId);
                }
                else
                {
                    var clearMatch = StageClearRegex.Match(path);
                    if (clearMatch.Success)
                    {
                        isTransactional = true;
                        int.TryParse(clearMatch.Groups["stageId"].Value, out stageId);
                    }
                    else if (path.Equals("/api/rewards/claim", StringComparison.OrdinalIgnoreCase) ||
                             path.Equals("/api/ad-rewards/claim", StringComparison.OrdinalIgnoreCase))
                    {
                        isTransactional = true;
                    }
                }
            }

            // If it is stage attempt start/clear, but stage_id <= 10, bypass rate limiting
            if (isTransactional && stageId > 0 && stageId <= 10)
            {
                await _next(context);
                return;
            }

            if (isTransactional)
            {
                // Extract user ID
                var userIdClaim = context.User.FindFirst(UserClaims.UserId);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                {
                    var db = redis.GetDatabase();
                    var limitKey = $"ratelimit:{userId}:transactional";
                    var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var windowStart = nowSeconds - 60;

                    // Remove old requests in sliding window
                    await db.SortedSetRemoveRangeByScoreAsync(limitKey, double.NegativeInfinity, windowStart);

                    // Count current requests
                    var currentRequestCount = await db.SortedSetLengthAsync(limitKey);

                    if (currentRequestCount >= _config.RateLimit.TransactionalPerMinute)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests; // 429
                        await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = ErrorCodes.RateLimited });
                        return;
                    }

                    // Add new request to ZSET
                    var uniqueId = Guid.NewGuid().ToString("N");
                    await db.SortedSetAddAsync(limitKey, uniqueId, nowSeconds);
                    await db.KeyExpireAsync(limitKey, TimeSpan.FromSeconds(90));
                }
            }

            await _next(context);
        }
    }
}
