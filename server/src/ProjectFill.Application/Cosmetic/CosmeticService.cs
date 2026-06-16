using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Logging;
using ProjectFill.Contracts.Cosmetic;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Cosmetic;

public sealed class CosmeticService
{
    private readonly AppDbContext _db;
    private readonly CurrencyService _currency;
    private readonly IStaticDataService _staticData;

    public CosmeticService(AppDbContext db, CurrencyService currency, IStaticDataService staticData)
    {
        _db = db;
        _currency = currency;
        _staticData = staticData;
    }

    public async Task<CosmeticListResponse> GetListAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var owned = await OwnedSetAsync(userId, ct);
        var active = await _db.UserActiveCosmetics.FindAsync(userId, ct);

        var response = new CosmeticListResponse { ServerTime = now };
        foreach (var item in _staticData.GetAllCosmeticItems().OrderBy(x => x.Category).ThenBy(x => x.SortOrder))
        {
            response.Items.Add(new CosmeticItemDto
            {
                CosmeticId = item.CosmeticId,
                Category = (int)item.Category,
                NameKey = item.NameKey,
                DescKey = item.DescKey,
                UnlockType = (int)item.UnlockType,
                UnlockCost = item.UnlockCost,
                UnlockConditionId = item.UnlockConditionId,
                PreviewRes = item.PreviewRes,
                SortOrder = item.SortOrder,
                Unlocked = IsUnlocked(item, owned),
            });
        }

        response.Active = ToActiveDto(active);
        return response;
    }

    public async Task<(string CosmeticId, CurrencySnapshot Currency)> UnlockWithGoldAsync(
        long userId, string cosmeticId, string correlationId, CancellationToken ct)
    {
        var item = _staticData.GetCosmeticItem(cosmeticId)
            ?? throw new GameApiException(ErrorCodes.CosmeticNotFound, $"Cosmetic {cosmeticId} not found.");

        if (item.UnlockType != CosmeticUnlockType.Gold)
            throw new GameApiException(ErrorCodes.CosmeticNotPurchasable, $"Cosmetic {cosmeticId} is not purchasable with gold.");

        var existing = await _db.UserCosmetics.FindAsync(userId, cosmeticId, ct);
        if (existing is not null)
            throw new GameApiException(ErrorCodes.CosmeticAlreadyOwned, $"Cosmetic {cosmeticId} already owned.");

        var now = DateTimeOffset.UtcNow;
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var currency = await _currency.SpendSoftAsync(userId, item.UnlockCost, "unlock_cosmetic", correlationId, ct);

        _db.UserCosmetics.Insert(new UserCosmeticsRow { UserId = userId, CosmeticId = cosmeticId, UnlockedAt = now });
        _db.EventLogs.Insert(EventLogFactory.CosmeticUnlocked(userId, correlationId, cosmeticId, item.UnlockType.ToString(), string.Empty));

        await _db.SaveAsync(ct);
        await tx.CommitAsync(ct);

        return (cosmeticId, currency);
    }

    public async Task<ActiveCosmeticsDto> SetActiveAsync(long userId, SetActiveCosmeticRequest request, string correlationId, CancellationToken ct)
    {
        var owned = await OwnedSetAsync(userId, ct);

        var chip = NormalizeSlot(request.ChipSkin, CosmeticCategory.Chip, owned);
        var lane = NormalizeSlot(request.LaneSkin, CosmeticCategory.Lane, owned);
        var board = NormalizeSlot(request.BoardSkin, CosmeticCategory.Board, owned);

        var now = DateTimeOffset.UtcNow;
        var row = await _db.UserActiveCosmetics.FindAsync(userId, ct);
        if (row is null)
        {
            row = new UserActiveCosmeticsRow
            {
                UserId = userId,
                ActiveChipSkin = "chip_default",
                ActiveLaneSkin = "lane_default",
                ActiveBoardSkin = "board_default",
                UseCustomBoardSkin = false,
                UpdatedAt = now,
            };
            _db.UserActiveCosmetics.Insert(row);
        }

        row.ActiveChipSkin = chip;
        row.ActiveLaneSkin = lane;
        row.ActiveBoardSkin = board;
        row.UseCustomBoardSkin = request.UseCustomBoardSkin;
        row.UpdatedAt = now;

        _db.EventLogs.Insert(EventLogFactory.CosmeticEquipped(userId, correlationId, chip, lane, board, request.UseCustomBoardSkin));
        await _db.SaveAsync(ct);

        return ToActiveDto(row);
    }

    /// <summary>
    /// Pull-based unlock: grants every cosmetic whose unlock_condition_id matches.
    /// Called by attendance / achievement / daily-challenge milestone flows.
    /// Returns the cosmetic ids newly unlocked. Does NOT call SaveAsync — caller saves.
    /// </summary>
    public async Task<List<string>> UnlockByConditionAsync(long userId, string conditionId, string correlationId, CancellationToken ct)
    {
        var newlyUnlocked = new List<string>();
        if (string.IsNullOrEmpty(conditionId)) return newlyUnlocked;

        var now = DateTimeOffset.UtcNow;
        foreach (var item in _staticData.GetAllCosmeticItems().Where(x => x.UnlockConditionId == conditionId))
        {
            var existing = await _db.UserCosmetics.FindAsync(userId, item.CosmeticId, ct);
            if (existing is not null) continue;

            _db.UserCosmetics.Insert(new UserCosmeticsRow { UserId = userId, CosmeticId = item.CosmeticId, UnlockedAt = now });
            _db.EventLogs.Insert(EventLogFactory.CosmeticUnlocked(userId, correlationId, item.CosmeticId, item.UnlockType.ToString(), conditionId));
            newlyUnlocked.Add(item.CosmeticId);
        }
        return newlyUnlocked;
    }

    private async Task<HashSet<string>> OwnedSetAsync(long userId, CancellationToken ct)
    {
        var owned = await _db.UserCosmetics.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.CosmeticId)
            .ToListAsync(ct);
        return new HashSet<string>(owned);
    }

    private static bool IsUnlocked(CosmeticItemData item, HashSet<string> owned)
        => item.UnlockType == CosmeticUnlockType.Default || owned.Contains(item.CosmeticId);

    private string NormalizeSlot(string cosmeticId, CosmeticCategory category, HashSet<string> owned)
    {
        if (string.IsNullOrEmpty(cosmeticId))
            return DefaultIdFor(category);

        var item = _staticData.GetCosmeticItem(cosmeticId)
            ?? throw new GameApiException(ErrorCodes.CosmeticNotFound, $"Cosmetic {cosmeticId} not found.");

        if (item.Category != category)
            throw new GameApiException(ErrorCodes.CosmeticCategoryMismatch, $"Cosmetic {cosmeticId} is not a {category} skin.");

        if (!IsUnlocked(item, owned))
            throw new GameApiException(ErrorCodes.CosmeticNotOwned, $"Cosmetic {cosmeticId} is not owned.");

        return cosmeticId;
    }

    private static string DefaultIdFor(CosmeticCategory category) => category switch
    {
        CosmeticCategory.Chip => "chip_default",
        CosmeticCategory.Lane => "lane_default",
        _ => "board_default",
    };

    private static ActiveCosmeticsDto ToActiveDto(UserActiveCosmeticsRow? row) => new()
    {
        ChipSkin = row?.ActiveChipSkin ?? "chip_default",
        LaneSkin = row?.ActiveLaneSkin ?? "lane_default",
        BoardSkin = row?.ActiveBoardSkin ?? "board_default",
        UseCustomBoardSkin = row?.UseCustomBoardSkin ?? false,
    };
}
