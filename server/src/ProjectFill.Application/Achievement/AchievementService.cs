using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Common;
using ProjectFill.Application.Cosmetic;
using ProjectFill.Application.Logging;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.Achievement;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Achievement;

public sealed class AchievementService
{
    private readonly AppDbContext _db;
    private readonly RewardService _reward;
    private readonly CosmeticService _cosmetic;
    private readonly IStaticDataService _staticData;

    public AchievementService(AppDbContext db, RewardService reward, CosmeticService cosmetic, IStaticDataService staticData)
    {
        _db = db;
        _reward = reward;
        _cosmetic = cosmetic;
        _staticData = staticData;
    }

    private readonly record struct Metrics(int TotalLogin, int LoginStreakBest, int AvatarCount, int CosmeticCount);

    public async Task<AchievementListResponse> GetListAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var metrics = await GatherMetricsAsync(userId, ct);
        var stored = await StoredMapAsync(userId, ct);

        var response = new AchievementListResponse { ServerTime = now };
        foreach (var ach in _staticData.GetAllAchievements().OrderBy(x => x.Category).ThenBy(x => x.SortOrder))
        {
            stored.TryGetValue(ach.AchievementId, out var row);
            var (progress, completed) = Evaluate(ach, row, metrics);
            response.Achievements.Add(new AchievementDto
            {
                AchievementId = ach.AchievementId,
                Category = (int)ach.Category,
                NameKey = ach.NameKey,
                DescKey = ach.DescKey,
                Tier = (int)ach.Tier,
                ConditionType = (int)ach.ConditionType,
                ConditionValue = ach.ConditionValue,
                Progress = progress,
                IsCompleted = completed,
                RewardClaimed = row?.RewardClaimed ?? false,
            });
        }
        return response;
    }

    public async Task<ClaimAchievementResponse> ClaimAsync(long userId, string achievementId, string correlationId, CancellationToken ct)
    {
        var ach = _staticData.GetAchievement(achievementId)
            ?? throw new GameApiException(ErrorCodes.AchievementNotFound, $"Achievement {achievementId} not found.");

        var metrics = await GatherMetricsAsync(userId, ct);
        var row = await _db.UserAchievements.FindAsync(userId, achievementId, ct);
        var (progress, completed) = Evaluate(ach, row, metrics);

        if (!completed)
            throw new GameApiException(ErrorCodes.AchievementNotCompleted, $"Achievement {achievementId} not completed.");
        if (row?.RewardClaimed == true)
            throw new GameApiException(ErrorCodes.AchievementAlreadyClaimed, $"Achievement {achievementId} already claimed.");

        var now = DateTimeOffset.UtcNow;
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        row = await UpsertAsync(userId, achievementId, row, now, ct);
        row.Progress = Math.Max(row.Progress, progress);
        row.IsCompleted = true;
        row.CompletedAt ??= now;
        row.RewardClaimed = true;
        row.UpdatedAt = now;

        var (granted, currency) = await _reward.GrantRewardGroupAsync(userId, ach.RewardGroupId, 1, correlationId, ct);
        var unlocked = await _cosmetic.UnlockByConditionAsync(userId, achievementId, correlationId, ct);

        _db.EventLogs.Insert(EventLogFactory.AchievementClaimed(userId, correlationId, achievementId, ach.RewardGroupId));
        await _db.SaveAsync(ct);
        await tx.CommitAsync(ct);

        return new ClaimAchievementResponse
        {
            AchievementId = achievementId,
            GrantedRewards = granted,
            UnlockedCosmetics = unlocked,
            Currency = currency ?? await CurrentCurrencyAsync(userId, ct),
            ServerTime = now,
        };
    }

    /// <summary>Seam for gameplay/challenge events that report a current value (streak/threshold). Sets progress to the max.</summary>
    public async Task ReportValueAsync(long userId, AchievementConditionType conditionType, int value, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var ach in _staticData.GetAllAchievements().Where(x => x.ConditionType == conditionType))
        {
            var row = await UpsertAsync(userId, ach.AchievementId, await _db.UserAchievements.FindAsync(userId, ach.AchievementId, ct), now, ct);
            if (value > row.Progress) row.Progress = value;
            if (!row.IsCompleted && row.Progress >= ach.ConditionValue) { row.IsCompleted = true; row.CompletedAt = now; }
            row.UpdatedAt = now;
        }
        await _db.SaveAsync(ct);
    }

    /// <summary>Seam for gameplay events that report an incremental count (stage clears, boosterless clears).</summary>
    public async Task ReportCountAsync(long userId, AchievementConditionType conditionType, int increment, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var ach in _staticData.GetAllAchievements().Where(x => x.ConditionType == conditionType))
        {
            var row = await UpsertAsync(userId, ach.AchievementId, await _db.UserAchievements.FindAsync(userId, ach.AchievementId, ct), now, ct);
            row.Progress += increment;
            if (!row.IsCompleted && row.Progress >= ach.ConditionValue) { row.IsCompleted = true; row.CompletedAt = now; }
            row.UpdatedAt = now;
        }
        await _db.SaveAsync(ct);
    }

    private async Task<UserAchievementsRow> UpsertAsync(long userId, string achievementId, UserAchievementsRow? row, DateTimeOffset now, CancellationToken ct)
    {
        if (row is not null) return row;
        row = new UserAchievementsRow
        {
            UserId = userId,
            AchievementId = achievementId,
            Progress = 0,
            IsCompleted = false,
            RewardClaimed = false,
            UpdatedAt = now,
        };
        _db.UserAchievements.Insert(row);
        return row;
    }

    private (int Progress, bool Completed) Evaluate(AchievementData ach, UserAchievementsRow? row, Metrics m)
    {
        var derived = ach.ConditionType switch
        {
            AchievementConditionType.TotalLoginDays => m.TotalLogin,
            AchievementConditionType.LoginStreak => m.LoginStreakBest,
            AchievementConditionType.AvatarUnlockCount => m.AvatarCount,
            AchievementConditionType.CosmeticUnlockCount => m.CosmeticCount,
            _ => row?.Progress ?? 0,
        };
        var progress = Math.Max(row?.Progress ?? 0, derived);
        var completed = (row?.IsCompleted ?? false) || progress >= ach.ConditionValue;
        return (progress, completed);
    }

    private async Task<Metrics> GatherMetricsAsync(long userId, CancellationToken ct)
    {
        var attendance = await _db.UserLoginAttendance.FindAsync(userId, ct);
        var avatarCount = await _db.UserRewardClaimState.Query()
            .CountAsync(x => x.UserId == userId && x.SourceId.StartsWith("avatar_unlock:"), ct);
        var cosmeticCount = await _db.UserCosmetics.Query()
            .CountAsync(x => x.UserId == userId, ct);
        return new Metrics(attendance?.TotalAttendedDays ?? 0, attendance?.BestStreak ?? 0, avatarCount, cosmeticCount);
    }

    private async Task<Dictionary<string, UserAchievementsRow>> StoredMapAsync(long userId, CancellationToken ct)
    {
        var rows = await _db.UserAchievements.Query().Where(x => x.UserId == userId).ToListAsync(ct);
        return rows.ToDictionary(x => x.AchievementId);
    }

    private async Task<CurrencySnapshot> CurrentCurrencyAsync(long userId, CancellationToken ct)
    {
        var c = await _db.UserCurrency.FindAsync(userId, ct);
        return new CurrencySnapshot { SoftAmount = c?.SoftAmount ?? 0 };
    }
}
