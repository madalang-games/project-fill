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

    public async Task<RankingPageResponse> GetWeeklyPageAsync(int offset, int limit, CancellationToken ct)
    {
        var weekStart = CurrentWeekStart();
        offset = Math.Max(0, offset);
        limit = ClampLimit(limit);
        await EnsureWeeklyKeyAsync(weekStart, ct);

        var entries = await _redis.SortedSetRangeByRankWithScoresAsync(
            WeeklyKey(weekStart),
            offset,
            offset + limit - 1,
            Order.Ascending);

        var userIds = entries.Select(x => long.Parse(x.Element.ToString())).ToArray();
        var rows = await LoadWeeklyRowsAsync(weekStart, userIds, ct);

        var response = new RankingPageResponse
        {
            RankingType = "weekly",
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

    public async Task<MyRankingResponse> GetMyWeeklyRankAsync(long userId, CancellationToken ct)
    {
        var weekStart = CurrentWeekStart();
        await EnsureWeeklyKeyAsync(weekStart, ct);

        var rank = await _redis.SortedSetRankAsync(WeeklyKey(weekStart), userId, Order.Ascending);
        if (!rank.HasValue)
            return new MyRankingResponse { RankingType = "weekly", Entry = null };

        var rows = await LoadWeeklyRowsAsync(weekStart, new[] { userId }, ct);
        if (!rows.TryGetValue(userId, out var row))
            return new MyRankingResponse { RankingType = "weekly", Entry = null };

        return new MyRankingResponse
        {
            RankingType = "weekly",
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

    public async Task<StageRankResponse> GetStageRankAsync(long userId, int stageId, CancellationToken ct)
    {
        await EnsureStageKeyAsync(stageId, ct);

        var progress = await _db.UserStageProgress.FindAsync(userId, stageId, ct);
        if (progress is null || !progress.StageClear)
            return new StageRankResponse { StageId = stageId };

        var rank = await _redis.SortedSetRankAsync(StageKey(stageId), userId, Order.Ascending);
        return new StageRankResponse
        {
            StageId = stageId,
            Rank = rank.HasValue ? (int)rank.Value + 1 : null,
            BestMovesUsed = progress.BestMovesUsed,
        };
    }

    /// <summary>Sync Redis indexes from committed DB state after a stage clear. Called post-commit.</summary>
    public async Task RecordClearAsync(long userId, int stageId, CancellationToken ct)
    {
        var totals = await _db.UserRankingTotals.FindAsync(userId, ct);
        if (totals is not null)
        {
            if (totals.TotalClearedStages > 0)
                await _redis.SortedSetAddAsync(GlobalKey(GlobalRankingType.ClearedStages), userId,
                    ComposeGlobalScore(totals.TotalClearedStages, totals.TotalClearedAt));
            if (totals.MaxClearedStageId > 0)
                await _redis.SortedSetAddAsync(GlobalKey(GlobalRankingType.MaxStage), userId,
                    ComposeGlobalScore(totals.MaxClearedStageId, totals.MaxStageAchievedAt));
        }

        var progress = await _db.UserStageProgress.FindAsync(userId, stageId, ct);
        if (progress is not null && progress.StageClear)
            await _redis.SortedSetAddAsync(StageKey(stageId), userId, progress.BestMovesUsed);

        var weekly = await _db.UserWeeklyRanking.FindAsync(userId, ct);
        if (weekly is not null && weekly.WeeklyClearedCount > 0 && weekly.WeekStartDate == CurrentWeekStart())
            await _redis.SortedSetAddAsync(WeeklyKey(weekly.WeekStartDate), userId,
                ComposeGlobalScore(weekly.WeeklyClearedCount, weekly.WeeklyClearedAt));
    }

    public async Task RebuildAllAsync(CancellationToken ct)
    {
        var totals = await _db.UserRankingTotals.Query().ToListAsync(ct);
        await _redis.KeyDeleteAsync(GlobalKey(GlobalRankingType.ClearedStages));
        await _redis.KeyDeleteAsync(GlobalKey(GlobalRankingType.MaxStage));

        if (totals.Count > 0)
        {
            await _redis.SortedSetAddAsync(
                GlobalKey(GlobalRankingType.ClearedStages),
                totals
                    .Where(x => x.TotalClearedStages > 0)
                    .Select(x => new SortedSetEntry(x.UserId, ComposeGlobalScore(x.TotalClearedStages, x.TotalClearedAt)))
                    .ToArray());

            await _redis.SortedSetAddAsync(
                GlobalKey(GlobalRankingType.MaxStage),
                totals
                    .Where(x => x.MaxClearedStageId > 0)
                    .Select(x => new SortedSetEntry(x.UserId, ComposeGlobalScore(x.MaxClearedStageId, x.MaxStageAchievedAt)))
                    .ToArray());
        }

        await RebuildWeeklyAsync(CurrentWeekStart(), ct);
    }

    private async Task EnsureGlobalKeyAsync(GlobalRankingType type, CancellationToken ct)
    {
        if (!await _redis.KeyExistsAsync(GlobalKey(type)))
            await RebuildAllAsync(ct);
    }

    private async Task EnsureWeeklyKeyAsync(string weekStart, CancellationToken ct)
    {
        if (!await _redis.KeyExistsAsync(WeeklyKey(weekStart)))
            await RebuildWeeklyAsync(weekStart, ct);
    }

    private async Task EnsureStageKeyAsync(int stageId, CancellationToken ct)
    {
        if (!await _redis.KeyExistsAsync(StageKey(stageId)))
            await RebuildStageAsync(stageId, ct);
    }

    private async Task RebuildWeeklyAsync(string weekStart, CancellationToken ct)
    {
        await _redis.KeyDeleteAsync(WeeklyKey(weekStart));
        var rows = await _db.UserWeeklyRanking.Query()
            .Where(x => x.WeekStartDate == weekStart && x.WeeklyClearedCount > 0)
            .ToListAsync(ct);
        if (rows.Count == 0)
            return;

        await _redis.SortedSetAddAsync(
            WeeklyKey(weekStart),
            rows.Select(x => new SortedSetEntry(x.UserId, ComposeGlobalScore(x.WeeklyClearedCount, x.WeeklyClearedAt))).ToArray());
    }

    private async Task RebuildStageAsync(int stageId, CancellationToken ct)
    {
        await _redis.KeyDeleteAsync(StageKey(stageId));
        var rows = await _db.UserStageProgress.Query()
            .Where(x => x.StageId == stageId && x.StageClear)
            .Select(x => new { x.UserId, x.BestMovesUsed })
            .ToListAsync(ct);
        if (rows.Count == 0)
            return;

        await _redis.SortedSetAddAsync(
            StageKey(stageId),
            rows.Select(x => new SortedSetEntry(x.UserId, x.BestMovesUsed)).ToArray());
    }

    private async Task<Dictionary<long, RankingRow>> LoadGlobalRowsAsync(GlobalRankingType type, long[] userIds, CancellationToken ct)
    {
        if (userIds.Length == 0)
            return new Dictionary<long, RankingRow>();

        if (type == GlobalRankingType.ClearedStages)
            return await _db.UserRankingTotals.Query()
                .Where(x => userIds.Contains(x.UserId))
                .Select(x => new RankingRow(x.UserId, x.Player!.DisplayName, x.Player!.AvatarId, x.TotalClearedStages))
                .ToDictionaryAsync(x => x.UserId, ct);

        return await _db.UserRankingTotals.Query()
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new RankingRow(x.UserId, x.Player!.DisplayName, x.Player!.AvatarId, x.MaxClearedStageId))
            .ToDictionaryAsync(x => x.UserId, ct);
    }

    private async Task<Dictionary<long, RankingRow>> LoadWeeklyRowsAsync(string weekStart, long[] userIds, CancellationToken ct)
    {
        if (userIds.Length == 0)
            return new Dictionary<long, RankingRow>();

        return await _db.UserWeeklyRanking.Query()
            .Where(x => x.WeekStartDate == weekStart && userIds.Contains(x.UserId))
            .Select(x => new RankingRow(x.UserId, x.Player!.DisplayName, x.Player!.AvatarId, x.WeeklyClearedCount))
            .ToDictionaryAsync(x => x.UserId, ct);
    }

    private static double ComposeGlobalScore(int primaryScore, DateTimeOffset? achievedAt)
        => -primaryScore * GlobalScoreFactor + (achievedAt ?? DateTimeOffset.MaxValue).ToUnixTimeSeconds();

    private static int ClampLimit(int limit)
        => limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);

    private static RedisKey GlobalKey(GlobalRankingType type)
        => type == GlobalRankingType.ClearedStages ? "ranking:global:stages" : "ranking:global:max-stage";

    private static RedisKey WeeklyKey(string weekStart) => $"ranking:weekly:{weekStart}:stages";

    private static RedisKey StageKey(int stageId) => $"ranking:stage:{stageId}:moves";

    private static GlobalRankingType NormalizeGlobalType(string type)
        => type switch
        {
            "stages" => GlobalRankingType.ClearedStages,
            "max-stage" or "max_stage" => GlobalRankingType.MaxStage,
            _ => throw new GameApiException(ErrorCodes.InvalidRankingType, "Invalid ranking type."),
        };

    private static string ToContractType(GlobalRankingType type)
        => type == GlobalRankingType.ClearedStages ? "stages" : "max-stage";

    private static string CurrentWeekStart()
    {
        var today = DateTimeOffset.UtcNow.UtcDateTime.Date;
        var diff = ((int)today.DayOfWeek + 6) % 7; // Monday = 0
        return today.AddDays(-diff).ToString("yyyy-MM-dd");
    }

    private sealed record RankingRow(long UserId, string DisplayName, int AvatarId, int Score);
}
