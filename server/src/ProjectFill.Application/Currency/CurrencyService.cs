using ProjectFill.Application.Common;
using ProjectFill.Contracts.Currency;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Currency;

public sealed class CurrencyService
{
    private readonly AppDbContext _db;

    public CurrencyService(AppDbContext db) => _db = db;

    public async Task<CurrencySnapshot> GrantSoftAsync(long userId, long amount, string reason, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var row = await _db.UserCurrency.FindAsync(userId, ct);
        if (row is null)
        {
            row = new UserCurrencyRow { UserId = userId, SoftAmount = 0, UpdatedAt = now };
            _db.UserCurrency.Insert(row);
        }

        var balanceBefore = row.SoftAmount;
        row.SoftAmount += amount;
        row.UpdatedAt = now;

        _db.CurrencyLogs.Insert(new CurrencyLogsRow
        {
            UserId = userId,
            CurrencyType = "soft",
            Delta = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = row.SoftAmount,
            Reason = reason,
            CorrelationId = correlationId,
            CreatedAt = now,
        });
        return new CurrencySnapshot { SoftAmount = row.SoftAmount, SoftDelta = amount };
    }

    public async Task<CurrencySnapshot> SpendSoftAsync(long userId, long amount, string reason, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var row = await _db.UserCurrency.FindAsync(userId, ct);

        if (row is null || row.SoftAmount < amount)
            throw new GameApiException(ErrorCodes.InsufficientCurrency, "Insufficient soft currency.");

        var balanceBefore = row.SoftAmount;
        row.SoftAmount -= amount;
        row.UpdatedAt = now;

        _db.CurrencyLogs.Insert(new CurrencyLogsRow
        {
            UserId = userId,
            CurrencyType = "soft",
            Delta = -amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = row.SoftAmount,
            Reason = reason,
            CorrelationId = correlationId,
            CreatedAt = now,
        });
        await _db.SaveAsync(ct);
        return new CurrencySnapshot { SoftAmount = row.SoftAmount, SoftDelta = -amount };
    }

    public async Task<CurrencySnapshot> GetAsync(long userId, CancellationToken ct)
    {
        var row = await _db.UserCurrency.FindAsync(userId, ct);
        return new CurrencySnapshot { SoftAmount = row?.SoftAmount ?? 0 };
    }
}
