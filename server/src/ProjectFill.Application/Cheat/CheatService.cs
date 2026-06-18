using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Inventory;
using ProjectFill.Application.Logging;
using ProjectFill.Domain.Enums;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFill.Application.Cheat;

// Dev-only state forcing. Reuses existing domain services/repositories so cheats share the same
// audit trail (currency_logs / InventoryChanged) as real gameplay, plus a Cheat* event_logs row.
public sealed class CheatService
{
    private const long MaxSoftGold = 999_999_999;
    private const string CheatReason = "cheat";
    private const string AdBypassValue = "1";
    private const int AttendanceCycleDays = 7;

    private readonly AppDbContext _db;
    private readonly CurrencyService _currency;
    private readonly InventoryService _inventory;
    private readonly IStaticDataService _staticData;
    private readonly IDatabase _redis;

    public CheatService(
        AppDbContext db,
        CurrencyService currency,
        InventoryService inventory,
        IStaticDataService staticData,
        IConnectionMultiplexer redis)
    {
        _db = db;
        _currency = currency;
        _inventory = inventory;
        _staticData = staticData;
        _redis = redis.GetDatabase();
    }

    public static string AdBypassKey(long userId) => $"cheat:ad:{userId}";

    public async Task<long> GoldAsync(long userId, CheatAction action, long amount, string command, string correlationId, CancellationToken ct)
    {
        if (amount < 0)
            throw new GameApiException(ErrorCodes.InvalidCommand, "Gold amount must be non-negative.");

        var row = await _db.UserCurrency.FindAsync(userId, ct);
        var current = row?.SoftAmount ?? 0;
        var target = action switch
        {
            CheatAction.Add => current + amount,
            CheatAction.Reduce => current - amount,
            CheatAction.Set => amount,
            _ => throw new GameApiException(ErrorCodes.InvalidCommand, "Unsupported gold action."),
        };
        var final = Math.Clamp(target, 0, MaxSoftGold);

        // Route the delta through CurrencyService so currency_logs records the mutation.
        await _currency.GrantSoftAsync(userId, final - current, CheatReason, correlationId, ct);
        _db.EventLogs.Insert(EventLogFactory.CheatGold(userId, correlationId, command, final));
        await _db.SaveAsync(ct);
        return final;
    }

    public async Task<Dictionary<int, int>> ItemAsync(long userId, int? itemId, CheatAction action, int amount, string command, string correlationId, CancellationToken ct)
    {
        if (amount < 0)
            throw new GameApiException(ErrorCodes.InvalidCommand, "Item amount must be non-negative.");

        var targets = itemId.HasValue
            ? new[] { itemId.Value }
            : _staticData.GetAllItems().Select(i => i.Id).ToArray();

        if (itemId.HasValue && _staticData.GetItem(itemId.Value) is null)
            throw new GameApiException(ErrorCodes.ItemNotFound, $"Item {itemId} not found.");

        foreach (var id in targets)
            await ApplyItemAsync(userId, id, action, amount, correlationId, ct);

        _db.EventLogs.Insert(EventLogFactory.CheatItem(userId, correlationId, command, itemId?.ToString() ?? "all"));
        await _db.SaveAsync(ct);
        return await SnapshotItemsAsync(userId, ct);
    }

    private async Task ApplyItemAsync(long userId, int itemId, CheatAction action, int amount, string correlationId, CancellationToken ct)
    {
        var current = (await _db.UserInventory.FindAsync(userId, itemId, ct))?.Count ?? 0;
        var desired = action switch
        {
            CheatAction.Add => current + amount,
            CheatAction.Reduce => Math.Max(0, current - amount),
            CheatAction.Set => amount,
            _ => throw new GameApiException(ErrorCodes.InvalidCommand, "Unsupported item action."),
        };
        var delta = desired - current;
        if (delta > 0)
            await _inventory.GrantItemAsync(userId, itemId, delta, CheatReason, correlationId, ct);
        else if (delta < 0)
            await _inventory.SpendItemAsync(userId, itemId, -delta, CheatReason, correlationId, ct);
    }

    private async Task<Dictionary<int, int>> SnapshotItemsAsync(long userId, CancellationToken ct)
        => await _db.UserInventory.Query()
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.ItemId, x => x.Count, ct);

    public async Task<int> StageAsync(long userId, int stageId, string command, string correlationId, CancellationToken ct)
    {
        if (stageId < 0)
            throw new GameApiException(ErrorCodes.InvalidCommand, "Stage id must be non-negative.");

        var now = DateTimeOffset.UtcNow;
        var existing = await _db.UserStageProgress.Query()
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
        var existingById = existing.ToDictionary(x => x.StageId);

        // Drop progress for stages beyond the requested ceiling.
        foreach (var row in existing.Where(x => x.StageId > stageId))
            _db.UserStageProgress.Delete(row);

        var clearedCount = 0;
        foreach (var sid in _staticData.GetAllStages().Select(s => s.StageId).Where(s => s >= 1 && s <= stageId))
        {
            if (!existingById.TryGetValue(sid, out var row))
            {
                row = new UserStageProgressRow { UserId = userId, StageId = sid };
                _db.UserStageProgress.Insert(row);
            }
            row.StageClear = true;
            row.FirstClearedAt ??= now;
            row.UpdatedAt = now;
            clearedCount++;
        }

        var totals = await _db.UserRankingTotals.FindAsync(userId, ct);
        if (totals is null)
        {
            totals = new UserRankingTotalsRow { UserId = userId };
            _db.UserRankingTotals.Insert(totals);
        }
        totals.MaxClearedStageId = stageId;
        totals.TotalClearedStages = clearedCount;
        totals.MaxStageAchievedAt = now;
        totals.TotalClearedAt = now;
        totals.UpdatedAt = now;

        _db.EventLogs.Insert(EventLogFactory.CheatStage(userId, correlationId, command, stageId));
        await _db.SaveAsync(ct);
        return totals.MaxClearedStageId;
    }

    public async Task<List<int>> TutorialAsync(long userId, int? tutorialId, bool seen, string command, string correlationId, CancellationToken ct)
    {
        if (tutorialId.HasValue)
        {
            var existing = await _db.UserTutorialProgress.FindAsync(userId, tutorialId.Value, ct);
            if (seen && existing is null)
                _db.UserTutorialProgress.Insert(new UserTutorialProgressRow { UserId = userId, TutorialId = tutorialId.Value, ViewedAt = DateTimeOffset.UtcNow });
            else if (!seen && existing is not null)
                _db.UserTutorialProgress.Delete(existing);
        }
        else
        {
            // No server-side tutorial id catalog, so "all" only supports clearing.
            if (seen)
                throw new GameApiException(ErrorCodes.InvalidCommand, "tutorial 'all true' is unsupported; specify an id.");

            var rows = await _db.UserTutorialProgress.Query().Where(x => x.UserId == userId).ToListAsync(ct);
            foreach (var row in rows)
                _db.UserTutorialProgress.Delete(row);
        }

        _db.EventLogs.Insert(EventLogFactory.CheatTutorial(userId, correlationId, command, tutorialId?.ToString() ?? "all", seen));
        await _db.SaveAsync(ct);
        return await _db.UserTutorialProgress.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.TutorialId)
            .ToListAsync(ct);
    }

    public async Task AdAsync(long userId, bool bypass, string command, string correlationId, CancellationToken ct)
    {
        var key = AdBypassKey(userId);
        if (bypass)
            await _redis.StringSetAsync(key, AdBypassValue);
        else
            await _redis.KeyDeleteAsync(key);

        _db.EventLogs.Insert(EventLogFactory.CheatAd(userId, correlationId, command, bypass));
        await _db.SaveAsync(ct);
    }

    // Cosmetics are visual-only and free here: directly toggle user_cosmetics rows (no gold/condition
    // gate). null id = every cosmetic. Returns the owned-cosmetic id list after the change.
    public async Task<List<string>> CosmeticAsync(long userId, string? cosmeticId, bool unlock, string command, string correlationId, CancellationToken ct)
    {
        if (cosmeticId is not null && _staticData.GetCosmeticItem(cosmeticId) is null)
            throw new GameApiException(ErrorCodes.CosmeticNotFound, $"Cosmetic {cosmeticId} not found.");

        var targets = cosmeticId is not null
            ? new[] { cosmeticId }
            : _staticData.GetAllCosmeticItems().Select(c => c.CosmeticId).ToArray();

        var now = DateTimeOffset.UtcNow;
        foreach (var id in targets)
        {
            var existing = await _db.UserCosmetics.FindAsync(userId, id, ct);
            if (unlock && existing is null)
                _db.UserCosmetics.Insert(new UserCosmeticsRow { UserId = userId, CosmeticId = id, UnlockedAt = now });
            else if (!unlock && existing is not null)
                _db.UserCosmetics.Delete(existing);
        }

        _db.EventLogs.Insert(EventLogFactory.CheatCosmetic(userId, correlationId, command, cosmeticId ?? "all", unlock));
        await _db.SaveAsync(ct);
        return await _db.UserCosmetics.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.CosmeticId)
            .ToListAsync(ct);
    }

    // Force achievement progress: complete sets progress to the threshold + IsCompleted; reset drops
    // the row. null id = every achievement. Reward claiming stays a separate user action.
    public async Task<Dictionary<string, bool>> AchievementAsync(long userId, string? achievementId, bool complete, string command, string correlationId, CancellationToken ct)
    {
        if (achievementId is not null && _staticData.GetAchievement(achievementId) is null)
            throw new GameApiException(ErrorCodes.AchievementNotFound, $"Achievement {achievementId} not found.");

        var targets = achievementId is not null
            ? new[] { _staticData.GetAchievement(achievementId)! }
            : _staticData.GetAllAchievements().ToArray();

        var now = DateTimeOffset.UtcNow;
        foreach (var ach in targets)
        {
            var row = await _db.UserAchievements.FindAsync(userId, ach.AchievementId, ct);
            if (complete)
            {
                if (row is null)
                {
                    row = new UserAchievementsRow { UserId = userId, AchievementId = ach.AchievementId, UpdatedAt = now };
                    _db.UserAchievements.Insert(row);
                }
                row.Progress = ach.ConditionValue;
                row.IsCompleted = true;
                row.CompletedAt ??= now;
                row.UpdatedAt = now;
            }
            else if (row is not null)
            {
                _db.UserAchievements.Delete(row);
            }
        }

        _db.EventLogs.Insert(EventLogFactory.CheatAchievement(userId, correlationId, command, achievementId ?? "all", complete));
        await _db.SaveAsync(ct);
        return await _db.UserAchievements.Query()
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.AchievementId, x => x.IsCompleted, ct);
    }

    // Force attendance cycle day (clamped 1..7, cleared claim flags so it is immediately re-claimable),
    // or null day = full reset (delete row). Returns the day after the change (0 on reset).
    public async Task<int> AttendanceAsync(long userId, int? day, string command, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var row = await _db.UserLoginAttendance.FindAsync(userId, ct);

        int dayAfter;
        if (day.HasValue)
        {
            dayAfter = Math.Clamp(day.Value, 1, AttendanceCycleDays);
            if (row is null)
            {
                row = new UserLoginAttendanceRow { UserId = userId, CurrentCycle = 1 };
                _db.UserLoginAttendance.Insert(row);
            }
            row.CurrentDay = dayAfter;
            row.LastAttendedDate = null;
            row.RewardClaimedToday = false;
            row.UpdatedAt = now;
        }
        else
        {
            dayAfter = 0;
            if (row is not null)
                _db.UserLoginAttendance.Delete(row);
        }

        _db.EventLogs.Insert(EventLogFactory.CheatAttendance(userId, correlationId, command, dayAfter));
        await _db.SaveAsync(ct);
        return dayAfter;
    }
}
