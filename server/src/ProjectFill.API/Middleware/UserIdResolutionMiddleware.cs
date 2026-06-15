using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectFill.API;
using ProjectFill.Application.Common;
using ProjectFill.Contracts.Common;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Domain.Utilities;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFill.API.Middleware;

public sealed class UserIdResolutionMiddleware
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30);

    private readonly RequestDelegate _next;

    public UserIdResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        AppDbContext db,
        IConnectionMultiplexer redis,
        IPlatformAuthClient authClient,
        NicknameGenerator nicknameGenerator)
    {
        if (context.User.Identity?.IsAuthenticated != true || context.User.HasClaim(claim => claim.Type == UserClaims.UserId))
        {
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(context);
            return;
        }

        var platformPid = context.User.GetPlatformPid();
        if (string.IsNullOrWhiteSpace(platformPid))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Code = ErrorCodes.PlayerUnresolved,
            });
            return;
        }

        var cache = redis.GetDatabase();
        var cacheKey = $"user_id:{platformPid}";
        long? userId = null;

        var cached = await cache.StringGetAsync(cacheKey);
        if (cached.HasValue && long.TryParse(cached, out var cachedId))
        {
            userId = cachedId;
        }
        else
        {
            userId = await db.Players.Query()
                .Where(player => player.PlatformPid == platformPid)
                .Select(player => (long?)player.UserId)
                .FirstOrDefaultAsync(context.RequestAborted);

            if (userId is null)
            {
                userId = await authClient.GetUserIdByPidAsync(platformPid, context.RequestAborted);
                if (userId is null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = ErrorCodes.PlayerUnresolved });
                    return;
                }

                var now = DateTimeOffset.UtcNow;
                await db.Players.InsertIgnoreAsync(new PlayersRow
                {
                    UserId = userId.Value,
                    PlatformPid = platformPid,
                    DisplayName = nicknameGenerator.Generate(),
                    AvatarId = 1,
                    AccountCreatedAt = now,
                    LastLoginAt = now,
                }, context.RequestAborted);
            }
            else if (await db.Players.FindAsync(userId.Value, context.RequestAborted) is { } existing)
            {
                existing.LastLoginAt = DateTimeOffset.UtcNow;
                await db.SaveAsync(context.RequestAborted);
            }

            await cache.StringSetAsync(cacheKey, userId.Value.ToString(), CacheTtl);
        }

        if (context.User.Identity is ClaimsIdentity identity)
            identity.AddClaim(new Claim(UserClaims.UserId, userId.Value.ToString()));

        await _next(context);
    }
}
