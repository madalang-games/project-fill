using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Infrastructure.Generated;
using Xunit;

namespace ProjectFill.API.Tests
{
    // Guards the gold-balance read/grant/spend contract the client mirrors. The client only ever
    // shows what these snapshots carry, so a wrong 0 here surfaces as "보유 재화 0" in the lobby.
    public sealed class CurrencyServiceTests
    {
        private static AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetAsync_NoCurrencyRow_ReturnsZeroNotNull()
        {
            using var db = CreateDbContext();
            var service = new CurrencyService(db);

            var snapshot = await service.GetAsync(userId: 42, default);

            // Lazy-created row: a brand-new account has no user_currency row, so the
            // authoritative balance is 0. This is the value FetchGold pushes to the client.
            Assert.NotNull(snapshot);
            Assert.Equal(0, snapshot.SoftAmount);
        }

        [Fact]
        public async Task GetAsync_ExistingRow_ReturnsStoredBalance()
        {
            using var db = CreateDbContext();
            db.UserCurrency.Insert(new UserCurrencyRow { UserId = 7, SoftAmount = 1234, UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveAsync(default);
            var service = new CurrencyService(db);

            var snapshot = await service.GetAsync(userId: 7, default);

            Assert.Equal(1234, snapshot.SoftAmount);
        }

        [Fact]
        public async Task GrantSoftAsync_CreatesRowAndReturnsNewBalanceWithDelta()
        {
            using var db = CreateDbContext();
            var service = new CurrencyService(db);

            var granted = await service.GrantSoftAsync(userId: 5, amount: 500, reason: "test_grant", correlationId: "c1", default);
            await db.SaveAsync(default);

            Assert.Equal(500, granted.SoftAmount);
            Assert.Equal(500, granted.SoftDelta);
            Assert.Equal(500, (await service.GetAsync(5, default)).SoftAmount);
        }

        [Fact]
        public async Task SpendSoftAsync_DeductsAndReturnsNewBalance()
        {
            using var db = CreateDbContext();
            db.UserCurrency.Insert(new UserCurrencyRow { UserId = 9, SoftAmount = 300, UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveAsync(default);
            var service = new CurrencyService(db);

            var after = await service.SpendSoftAsync(userId: 9, amount: 100, reason: "test_spend", correlationId: "c2", default);

            Assert.Equal(200, after.SoftAmount);
            Assert.Equal(-100, after.SoftDelta);
            Assert.Equal(200, (await service.GetAsync(9, default)).SoftAmount);
        }

        [Fact]
        public async Task SpendSoftAsync_InsufficientBalance_Throws()
        {
            using var db = CreateDbContext();
            db.UserCurrency.Insert(new UserCurrencyRow { UserId = 11, SoftAmount = 50, UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveAsync(default);
            var service = new CurrencyService(db);

            var ex = await Assert.ThrowsAsync<GameApiException>(
                () => service.SpendSoftAsync(userId: 11, amount: 100, reason: "test_spend", correlationId: "c3", default));
            Assert.Equal(ErrorCodes.InsufficientCurrency, ex.Code);
        }
    }
}
