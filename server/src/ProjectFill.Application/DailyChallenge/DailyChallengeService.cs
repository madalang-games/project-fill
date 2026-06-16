using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Achievement;
using ProjectFill.Application.Common;
using ProjectFill.Application.Cosmetic;
using ProjectFill.Application.Logging;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.DailyChallenge;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.DailyChallenge;

public sealed class DailyChallengeService
{
    private const int BaseRewardGroup = 7001;
    private readonly AppDbContext _db;
    private readonly RewardService _reward;
    private readonly CosmeticService _cosmetic;
    private readonly AchievementService _achievement;

    public DailyChallengeService(AppDbContext db, RewardService reward, CosmeticService cosmetic, AchievementService achievement)
    {
        _db = db;
        _reward = reward;
        _cosmetic = cosmetic;
        _achievement = achievement;
    }

    private static string TodayKey() => DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyy-MM-dd");
    private static string YesterdayKey() => DateTimeOffset.UtcNow.UtcDateTime.AddDays(-1).ToString("yyyy-MM-dd");

    public async Task<DailyChallengeTodayResponse> GetTodayAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var key = TodayKey();
        var challenge = await EnsureChallengeAsync(key, ct);
        var record = await _db.UserDailyChallengeRecords.FindAsync(userId, key, ct);
        var streak = await _db.UserChallengeStreaks.FindAsync(userId, ct);

        var participants = await _db.UserDailyChallengeRecords.Query()
            .CountAsync(x => x.ChallengeDate == key && x.IsCleared, ct);

        return new DailyChallengeTodayResponse
        {
            ChallengeDate = challenge.ChallengeDate,
            StageSeed = challenge.StageSeed,
            SignalTypeCount = challenge.SignalTypeCount,
            LaneCount = challenge.LaneCount,
            GimmickId = challenge.GimmickId ?? -1,
            Played = record is not null,
            IsCleared = record?.IsCleared ?? false,
            MyMovesUsed = record?.MovesUsed ?? 0,
            MyRank = record?.IsCleared == true ? await RankOfAsync(key, record.MovesUsed, record.ClearTimeSeconds, ct) : 0,
            CurrentStreak = streak?.CurrentStreak ?? 0,
            ParticipantCount = participants,
            ServerTime = now,
        };
    }

    public async Task<SubmitChallengeClearResponse> SubmitClearAsync(long userId, int movesUsed, int clearTimeSeconds, string correlationId, CancellationToken ct)
    {
        if (movesUsed <= 0 || clearTimeSeconds < 0)
            throw new GameApiException(ErrorCodes.InvalidAmount, "Invalid challenge result.");

        var now = DateTimeOffset.UtcNow;
        var key = TodayKey();
        await EnsureChallengeAsync(key, ct);

        var record = await _db.UserDailyChallengeRecords.FindAsync(userId, key, ct);
        if (record?.IsCleared == true)
            throw new GameApiException(ErrorCodes.ChallengeAlreadyPlayed, "Today's challenge already played.");

        var streak = await _db.UserChallengeStreaks.FindAsync(userId, ct);
        var newStreak = streak?.LastClearDate == YesterdayKey() ? streak.CurrentStreak + 1 : 1;
        var bestStreak = Math.Max(streak?.BestStreak ?? 0, newStreak);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        if (streak is null)
        {
            streak = new UserChallengeStreaksRow { UserId = userId };
            _db.UserChallengeStreaks.Insert(streak);
        }
        streak.CurrentStreak = newStreak;
        streak.BestStreak = bestStreak;
        streak.LastClearDate = key;
        streak.UpdatedAt = now;

        if (record is null)
        {
            record = new UserDailyChallengeRecordsRow { UserId = userId, ChallengeDate = key };
            _db.UserDailyChallengeRecords.Insert(record);
        }
        record.MovesUsed = movesUsed;
        record.ClearTimeSeconds = clearTimeSeconds;
        record.IsCleared = true;
        record.StreakAtClear = newStreak;
        record.UpdatedAt = now;

        var response = new SubmitChallengeClearResponse { MovesUsed = movesUsed, CurrentStreak = newStreak, BestStreak = bestStreak };

        var (granted, currency) = await _reward.GrantRewardGroupAsync(userId, BaseRewardGroup, 1, correlationId, ct);
        response.GrantedRewards.AddRange(granted);

        var bonusGroup = StreakBonusGroup(newStreak);
        if (bonusGroup != 0)
        {
            var (bGranted, bCurrency) = await _reward.GrantRewardGroupAsync(userId, bonusGroup, 1, correlationId, ct);
            response.StreakRewardGroupId = bonusGroup;
            response.GrantedRewards.AddRange(bGranted);
            if (bCurrency is not null) currency = bCurrency;

            var cosmeticCondition = StreakCosmeticCondition(newStreak);
            if (cosmeticCondition is not null)
                response.UnlockedCosmetics.AddRange(await _cosmetic.UnlockByConditionAsync(userId, cosmeticCondition, correlationId, ct));
        }

        record.RewardClaimed = true;
        _db.EventLogs.Insert(EventLogFactory.ChallengeCleared(userId, correlationId, key, movesUsed, clearTimeSeconds, newStreak));
        await _db.SaveAsync(ct);

        var rank = await RankOfAsync(key, movesUsed, clearTimeSeconds, ct);
        response.Rank = rank;

        await _achievement.ReportValueAsync(userId, AchievementConditionType.ChallengeClearStreak, newStreak, ct);
        if (rank == 1)
            await _achievement.ReportValueAsync(userId, AchievementConditionType.ChallengeRankFirst, 1, ct);

        await tx.CommitAsync(ct);

        response.Currency = currency ?? await CurrentCurrencyAsync(userId, ct);
        response.ServerTime = now;
        return response;
    }

    public async Task<ChallengeRankingResponse> GetRankingAsync(long userId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(0, page);
        pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100);
        var now = DateTimeOffset.UtcNow;
        var key = TodayKey();

        var recordsQuery = _db.UserDailyChallengeRecords.Query()
            .Where(x => x.ChallengeDate == key && x.IsCleared)
            .OrderBy(x => x.MovesUsed)
            .ThenBy(x => x.ClearTimeSeconds);

        var total = await recordsQuery.CountAsync(ct);
        var rows = await recordsQuery.Skip(page * pageSize).Take(pageSize).ToListAsync(ct);

        var userIds = rows.Select(r => r.UserId).ToList();
        var players = await _db.Players.Query()
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, ct);

        var response = new ChallengeRankingResponse { Page = page, PageSize = pageSize, TotalCount = total, ServerTime = now };
        for (int i = 0; i < rows.Count; i++)
        {
            var rec = rows[i];
            players.TryGetValue(rec.UserId, out var player);
            response.Entries.Add(new ChallengeRankingEntryDto
            {
                Rank = page * pageSize + i + 1,
                UserId = rec.UserId,
                DisplayName = player?.DisplayName ?? string.Empty,
                AvatarId = player?.AvatarId ?? 0,
                MovesUsed = rec.MovesUsed,
                ClearTimeSeconds = rec.ClearTimeSeconds,
                IsMe = rec.UserId == userId,
            });
        }
        return response;
    }

    public async Task<ChallengeStreakResponse> GetStreakAsync(long userId, CancellationToken ct)
    {
        var streak = await _db.UserChallengeStreaks.FindAsync(userId, ct);
        return new ChallengeStreakResponse
        {
            CurrentStreak = streak?.CurrentStreak ?? 0,
            BestStreak = streak?.BestStreak ?? 0,
            LastClearDate = streak?.LastClearDate ?? string.Empty,
            ServerTime = DateTimeOffset.UtcNow,
        };
    }

    private async Task<DailyChallengesRow> EnsureChallengeAsync(string key, CancellationToken ct)
    {
        var row = await _db.DailyChallenges.FindAsync(key, ct);
        if (row is not null) return row;

        var hash = StableHash(key);
        row = new DailyChallengesRow
        {
            ChallengeDate = key,
            StageSeed = key,
            SignalTypeCount = 5 + (int)(hash % 2),       // 5–6 signal types (Ch.3 level)
            LaneCount = 7 + (int)(hash % 2),              // 1–2 empty lanes over signal count
            GimmickId = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.DailyChallenges.Insert(row);
        await _db.SaveAsync(ct);
        return row;
    }

    private async Task<int> RankOfAsync(string key, int movesUsed, int clearTimeSeconds, CancellationToken ct)
    {
        var better = await _db.UserDailyChallengeRecords.Query()
            .CountAsync(x => x.ChallengeDate == key && x.IsCleared &&
                (x.MovesUsed < movesUsed || (x.MovesUsed == movesUsed && x.ClearTimeSeconds < clearTimeSeconds)), ct);
        return better + 1;
    }

    private async Task<CurrencySnapshot> CurrentCurrencyAsync(long userId, CancellationToken ct)
    {
        var c = await _db.UserCurrency.FindAsync(userId, ct);
        return new CurrencySnapshot { SoftAmount = c?.SoftAmount ?? 0 };
    }

    private static int StreakBonusGroup(int streak) => streak switch
    {
        3 => 7003,
        7 => 7007,
        30 => 7030,
        100 => 7100,
        _ => 0,
    };

    private static string? StreakCosmeticCondition(int streak) => streak switch
    {
        30 => "streak_30",
        100 => "streak_100",
        _ => null,
    };

    private static uint StableHash(string s)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var c in s) { hash ^= c; hash *= 16777619; }
            return hash;
        }
    }
}
