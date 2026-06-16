using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Logging;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Contracts.Iap;
using ProjectFill.Contracts.Rewards;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Iap;

public sealed class IapService
{
    private readonly AppDbContext _db;
    private readonly RewardService _rewards;
    private readonly CurrencyService _currency;
    private readonly IStaticDataService _staticData;

    public IapService(AppDbContext db, RewardService rewards, CurrencyService currency, IStaticDataService staticData)
    {
        _db = db;
        _rewards = rewards;
        _currency = currency;
        _staticData = staticData;
    }

    public async Task<VerifyIapResponse> VerifyIapAsync(
        long userId,
        VerifyIapRequest request,
        string correlationId,
        CancellationToken ct)
    {
        var product = _staticData.GetIapProduct(request.InfoId)
            ?? throw new GameApiException(ErrorCodes.InvalidProduct, "invalid product id");

        if (!product.IsEnabled)
            throw new GameApiException(ErrorCodes.InvalidProduct, "product not available");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var exists = await _db.IapPurchases.Query()
            .AnyAsync(x => x.Platform == request.Platform && x.OrderId == request.OrderId, ct);
        if (exists)
            throw new GameApiException(ErrorCodes.DuplicateOrder, "duplicate order");

        await ValidatePurchaseLimitAsync(userId, product, ct);

        var purchase = new IapPurchasesRow
        {
            PurchaseId = Guid.NewGuid().ToString(),
            UserId = userId,
            InfoId = product.InfoId,
            ProductId = request.StoreProductId,
            OrderId = request.OrderId,
            PurchaseToken = request.PurchaseToken,
            Price = request.Price,
            Currency = request.Currency,
            Status = "COMPLETED",
            Platform = request.Platform,
            RawReceipt = request.RawReceipt ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.IapPurchases.Insert(purchase);

        var (granted, currency) = await _rewards.GrantRewardGroupAsync(
            userId, product.RewardGroupId, 1, correlationId, ct);

        await IncrementPurchaseCountAsync(userId, product, ct);

        _db.EventLogs.Insert(EventLogFactory.IapPurchaseCompleted(
            userId, correlationId, product.InfoId, product.StoreProductId,
            request.OrderId, request.Price, request.Currency));

        await _db.SaveAsync(ct);
        await tx.CommitAsync(ct);

        var player = await _db.Players.FindAsync(userId, ct);
        var currentGold = currency?.SoftAmount ?? (await _currency.GetAsync(userId, ct)).SoftAmount;
        int? remaining = GetRemainingPurchases(product, await GetPurchaseCountAsync(userId, product.InfoId, ct));

        return new VerifyIapResponse
        {
            Success = true,
            IsNoAds = player?.IsNoAds ?? false,
            CurrentGold = currentGold,
            RemainingPurchases = RemainingToWire(remaining),
            GrantedRewards = granted,
        };
    }

    public async Task<GetIapProductsResponse> GetProductStatusesAsync(long userId, CancellationToken ct)
    {
        var products = _staticData.GetAllIapProducts()
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.SortOrder);

        var countRows = await _db.UserIapPurchaseCounts.Query()
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);

        var result = new GetIapProductsResponse();
        foreach (var p in products)
        {
            var row = countRows.FirstOrDefault(r => r.InfoId == p.InfoId);
            int count = GetEffectiveCount(p, row);
            result.Products.Add(new IapProductStatusDto
            {
                InfoId = p.InfoId,
                StoreProductId = p.StoreProductId,
                RemainingPurchases = RemainingToWire(GetRemainingPurchases(p, count)),
            });
        }

        return result;
    }

    private async Task ValidatePurchaseLimitAsync(long userId, IapProductData product, CancellationToken ct)
    {
        if (product.PurchaseLimit == 0) return;

        var row = await _db.UserIapPurchaseCounts.FindAsync(userId, product.InfoId, ct);
        int count = GetEffectiveCount(product, row);

        if (count >= product.PurchaseLimit)
            throw new GameApiException(ErrorCodes.PurchaseLimitReached, "purchase limit reached");
    }

    private async Task IncrementPurchaseCountAsync(long userId, IapProductData product, CancellationToken ct)
    {
        if (product.PurchaseLimit == 0) return;

        var now = DateTimeOffset.UtcNow;
        var row = await _db.UserIapPurchaseCounts.FindAsync(userId, product.InfoId, ct);
        var currentPeriodStart = GetCurrentPeriodStart(product.ResetPeriod, now);

        if (row is null)
        {
            _db.UserIapPurchaseCounts.Insert(new UserIapPurchaseCountsRow
            {
                UserId = userId,
                InfoId = product.InfoId,
                PurchaseCount = 1,
                PeriodStart = currentPeriodStart,
                UpdatedAt = now,
            });
        }
        else
        {
            bool periodRolled = product.ResetPeriod != PurchaseResetPeriod.None
                && (row.PeriodStart is null || row.PeriodStart < currentPeriodStart);

            row.PurchaseCount = periodRolled ? 1 : row.PurchaseCount + 1;
            row.PeriodStart = currentPeriodStart;
            row.UpdatedAt = now;
        }
    }

    private async Task<int> GetPurchaseCountAsync(long userId, int infoId, CancellationToken ct)
    {
        var row = await _db.UserIapPurchaseCounts.FindAsync(userId, infoId, ct);
        return row?.PurchaseCount ?? 0;
    }

    private static int GetEffectiveCount(IapProductData product, UserIapPurchaseCountsRow? row)
    {
        if (row is null) return 0;
        if (product.ResetPeriod == PurchaseResetPeriod.None) return row.PurchaseCount;

        var currentPeriodStart = GetCurrentPeriodStart(product.ResetPeriod, DateTimeOffset.UtcNow);
        return row.PeriodStart < currentPeriodStart ? 0 : row.PurchaseCount;
    }

    private static int? GetRemainingPurchases(IapProductData product, int count)
    {
        if (product.PurchaseLimit == 0) return null;
        return Math.Max(0, product.PurchaseLimit - count);
    }

    private static int RemainingToWire(int? remaining) => remaining ?? -1;

    private static DateTimeOffset? GetCurrentPeriodStart(PurchaseResetPeriod period, DateTimeOffset utcNow)
    {
        if (period == PurchaseResetPeriod.None) return null;
        var utc = utcNow.UtcDateTime;
        return period switch
        {
            PurchaseResetPeriod.Daily   => new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero),
            PurchaseResetPeriod.Weekly  => new DateTimeOffset(utc.AddDays(-(int)utc.DayOfWeek == 0 ? 6 : (int)utc.DayOfWeek - 1).Date, TimeSpan.Zero),
            PurchaseResetPeriod.Monthly => new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero),
            _                           => null,
        };
    }
}
