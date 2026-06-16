using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectFill.Application.Common;
using ProjectFill.Contracts.Ranking;
using ProjectFill.Domain.Enums;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFill.Application.Ranking;

public sealed class RankingService
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;
    private const double GlobalScoreFactor = 10_000_000_000d;

    private readonly AppDbContext _db;
    private readonly IDatabase _redis;
    private readonly ILogger<RankingService> _logger;

    public RankingService(AppDbContext db, IConnectionMultiplexer redis, ILogger<RankingService> logger)
    {
        _db = db;
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<RankingPageResponse> GetGlobalPageAsync(string type, int offset, int limit, CancellationToken ct)
    {
        var rankingType = NormalizeGlobalType(type);
        offset = Math.Max(0, offset);
        limit = ClampLimit(limit);
        await EnsureGlobalKeyAsync(rankingType, ct);

        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(
            GlobalKey(rankingType),
            offset,
            offset + limit - 1,
            Order.Ascending);

        var userIds = entries.Select(x => long.Parse(x.Element.ToString())).ToArray();
        var rows = await LoadGlobalRowsAsync(rankingType, userIds, ct);

        var response = new RankingPageResponse
        {
            RankingType = ToContractType(rankingType),
            Offset = offset,
            Limit = limit,
        };

        for (var i = 0; i < userIds.Length; i++)
        {
            if (!rows.TryGetValue(userIds[i], out var row))
                continue;

            response.Entries.Add(new RankingEntryDto
            {
                UserId = userIds[i],
                DisplayName = row.DisplayName,
                AvatarId = row.AvatarId,
                Rank = offset + i + 1,
                Score = row.Score,
            });
        }

        return response;
    }

    public async Task<MyRankingResponse> GetMyGlobalRankAsync(long userId, string type, CancellationToken ct)
    {
        var rankingType = NormalizeGlobalType(type);
        await EnsureGlobalKeyAsync(rankingType, ct);

        var rank = await _redis.SortedSetRankAsync(GlobalKey(rankingType), userId, Order.Ascending);
        if (!rank.HasValue)
        {
            return new MyRankingResponse
            {
                RankingType = ToContractType(rankingType),
                Entry = null,
            };
        }

        var rows = await LoadGlobalRowsAsync(rankingType, new[] { userId }, ct);
        if (!rows.TryGetValue(userId, out var row))
            return new MyRankingResponse { RankingType = ToContractType(rankingType), Entry = null };

        return new MyRankingResponse
        {
            RankingType = ToContractType(rankingType),
            Entry = new RankingEntryDto
            {
                UserId = userId,
                DisplayName = row.DisplayName,
                AvatarId = row.AvatarId,
                Rank = (int)rank.Value + 1,
                Score = row.Score,
            },
        };
    }

    public async Task RebuildAllAsync(CancellationToken ct)
    {
        var totals = await _db.UserRankingTotals.Query().ToListAsync(ct);
        await _redis.KeyDeleteAsync(GlobalKey(GlobalRankingType.Stars));
        await _redis.KeyDeleteAsync(GlobalKey(GlobalRankingType.MaxStage));

        if (totals.Count == 0)
            return;

        await _redis.SortedSetAddAsync(
            GlobalKey(GlobalRankingType.Stars),
            totals
                .Where(x => x.TotalEarnedStars > 0)
                .Select(x => new SortedSetEntry(x.UserId, ComposeGlobalScore(x.TotalEarnedStars, x.TotalStarsAchievedAt)))
                .ToArray());

        await _redis.SortedSetAddAsync(
            GlobalKey(GlobalRankingType.MaxStage),
            totals
                .Where(x => x.MaxClearedStageId > 0)
                .Select(x => new SortedSetEntry(x.UserId, ComposeGlobalScore(x.MaxClearedStageId, x.MaxStageAchievedAt)))
                .ToArray());
    }

    private async Task EnsureGlobalKeyAsync(GlobalRankingType type, CancellationToken ct)
    {
        if (!await _redis.KeyExistsAsync(GlobalKey(type)))
            await RebuildAllAsync(ct);
    }

    private async Task<Dictionary<long, GlobalRankingRow>> LoadGlobalRowsAsync(GlobalRankingType type, long[] userIds, CancellationToken ct)
    {
        if (userIds.Length == 0)
            return new Dictionary<long, GlobalRankingRow>();

        if (type == GlobalRankingType.Stars)
            return await _db.UserRankingTotals.Query()
                .Where(x => userIds.Contains(x.UserId))
                .Select(x => new GlobalRankingRow(x.UserId, x.Player!.DisplayName, x.Player!.AvatarId, x.TotalEarnedStars))
                .ToDictionaryAsync(x => x.UserId, ct);

        return await _db.UserRankingTotals.Query()
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new GlobalRankingRow(x.UserId, x.Player!.DisplayName, x.Player!.AvatarId, x.MaxClearedStageId))
            .ToDictionaryAsync(x => x.UserId, ct);
    }

    private static double ComposeGlobalScore(int primaryScore, DateTimeOffset? achievedAt)
        => -primaryScore * GlobalScoreFactor + (achievedAt ?? DateTimeOffset.MaxValue).ToUnixTimeSeconds();

    private static int ClampLimit(int limit)
        => limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);

    private static RedisKey GlobalKey(GlobalRankingType type)
        => type == GlobalRankingType.Stars ? "ranking:global:stars" : "ranking:global:max_stage";

    private static GlobalRankingType NormalizeGlobalType(string type)
        => type switch
        {
            "stars" => GlobalRankingType.Stars,
            "max-stage" or "max_stage" => GlobalRankingType.MaxStage,
            _ => throw new GameApiException(ErrorCodes.InvalidRankingType, "Invalid ranking type."),
        };

    private static string ToContractType(GlobalRankingType type)
        => type == GlobalRankingType.Stars ? "stars" : "max-stage";

    private sealed record GlobalRankingRow(long UserId, string DisplayName, int AvatarId, int Score);
}
