using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Logging;
using ProjectFill.Contracts.Inventory;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Inventory;

public sealed class InventoryService
{
    private readonly AppDbContext _db;
    private readonly CurrencyService _currency;
    private readonly IStaticDataService _staticData;

    public InventoryService(AppDbContext db, CurrencyService currency, IStaticDataService staticData)
    {
        _db = db;
        _currency = currency;
        _staticData = staticData;
    }

    public async Task<InventorySnapshot> GetInventoryAsync(long userId, CancellationToken ct)
    {
        var items = await _db.UserInventory.Query()
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

        var snapshot = new InventorySnapshot();
        foreach (var item in items)
        {
            snapshot.Items.Add(new InventoryItemDto
            {
                ItemId = item.ItemId,
                Count = item.Count
            });
        }
        return snapshot;
    }

    public async Task<InventorySnapshot> SpendItemAsync(
        long userId,
        int itemId,
        int amount,
        string reason,
        string correlationId,
        CancellationToken ct)
    {
        if (amount <= 0)
            throw new GameApiException(ErrorCodes.InvalidAmount, "Amount to spend must be positive.");

        var now = DateTimeOffset.UtcNow;
        var row = await _db.UserInventory.FindAsync(userId, itemId, ct);

        if (row is null || row.Count < amount)
            throw new GameApiException(ErrorCodes.InsufficientItems, $"Insufficient inventory for item {itemId}.");

        row.Count -= amount;
        row.UpdatedAt = now;

        _db.EventLogs.Insert(EventLogFactory.InventoryChanged(userId, correlationId, itemId, -amount, reason, row.Count));
        await _db.SaveAsync(ct);

        return await GetInventoryAsync(userId, ct);
    }

    public async Task<InventorySnapshot> GrantItemAsync(
        long userId,
        int itemId,
        int amount,
        string reason,
        string correlationId,
        CancellationToken ct)
    {
        if (amount <= 0)
            throw new GameApiException(ErrorCodes.InvalidAmount, "Amount to grant must be positive.");

        var now = DateTimeOffset.UtcNow;
        var row = await _db.UserInventory.FindAsync(userId, itemId, ct);

        if (row is null)
        {
            row = new UserInventoryRow
            {
                UserId = userId,
                ItemId = itemId,
                Count = 0,
                UpdatedAt = now
            };
            _db.UserInventory.Insert(row);
        }

        row.Count += amount;
        row.UpdatedAt = now;

        _db.EventLogs.Insert(EventLogFactory.InventoryChanged(userId, correlationId, itemId, amount, reason, row.Count));
        await _db.SaveAsync(ct);

        return await GetInventoryAsync(userId, ct);
    }

    public async Task<(InventorySnapshot Inventory, ProjectFill.Contracts.Currency.CurrencySnapshot Currency)> BuyItemAsync(
        long userId,
        int itemId,
        string correlationId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var itemData = _staticData.GetItem(itemId)
            ?? throw new GameApiException(ErrorCodes.ItemNotFound, $"Item {itemId} not found.");
        var cost = itemData.Price;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var currencySnapshot = await _currency.SpendSoftAsync(userId, cost, "buy_booster", correlationId, ct);

        var invRow = await _db.UserInventory.FindAsync(userId, itemId, ct);
        if (invRow is null)
        {
            invRow = new UserInventoryRow
            {
                UserId = userId,
                ItemId = itemId,
                Count = 0,
                UpdatedAt = now
            };
            _db.UserInventory.Insert(invRow);
        }

        invRow.Count += 1;
        invRow.UpdatedAt = now;
        _db.EventLogs.Insert(EventLogFactory.InventoryChanged(userId, correlationId, itemId, 1, "buy_booster", invRow.Count));

        await _db.SaveAsync(ct);
        await tx.CommitAsync(ct);

        var inventorySnapshot = await GetInventoryAsync(userId, ct);

        return (inventorySnapshot, currencySnapshot);
    }
}
