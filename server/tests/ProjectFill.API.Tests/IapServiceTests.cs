using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Inventory;
using ProjectFill.Application.Iap;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Contracts.Iap;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;
using Xunit;

namespace ProjectFill.API.Tests
{
    public sealed class IapServiceTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private sealed class FakeIapData : FakeStaticData
        {
            private readonly Dictionary<int, IapProductData> _products;
            public FakeIapData(IapProductData product) => _products = new() { [product.InfoId] = product };
            public override IapProductData? GetIapProduct(int info_id) => _products.GetValueOrDefault(info_id);
            public override IReadOnlyList<IapProductData> GetAllIapProducts() => _products.Values.ToList();
        }

        private static IConfiguration Config(string env)
            => new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Game:Environment"] = env })
                .Build();

        private static IapProductData Product(
            IapProductType type = IapProductType.Consumable, int limit = 0)
            => new()
            {
                InfoId = 1001,
                StoreProductId = "sku_test",
                ProductType = type,
                RewardGroupId = 5001,
                PurchaseLimit = limit,
                ResetPeriod = PurchaseResetPeriod.None,
                SortOrder = 1,
                IsEnabled = true,
            };

        private static IapService NewService(AppDbContext db, IapProductData product, string env)
        {
            var currency = new CurrencyService(db);
            var data = new FakeIapData(product);
            var inventory = new InventoryService(db, currency, data);
            var reward = new RewardService(db, currency, inventory);
            var verifier = new GooglePlayVerifier(Config(string.Empty), NullLogger<GooglePlayVerifier>.Instance);
            return new IapService(db, reward, currency, data, verifier, Config(env));
        }

        private static async Task SeedPlayerAsync(AppDbContext db, long userId)
        {
            db.Players.Insert(new PlayersRow
            {
                UserId = userId, PlatformPid = $"p{userId}", DisplayName = "P", AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow,
            });
            await db.SaveAsync();
        }

        private static VerifyIapRequest MockReq(string orderId = "MOCK_1")
            => new()
            {
                InfoId = 1001, StoreProductId = "sku_test", OrderId = orderId, PurchaseToken = "tok",
                Price = 0.99, Currency = "USD", Platform = "mock", RawReceipt = "{}",
            };

        [Fact]
        public async Task MockPlatform_InProduction_IsRejected()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, Product(), env: "prod");

            var ex = await Assert.ThrowsAsync<GameApiException>(
                () => svc.VerifyIapAsync(1, MockReq(), "corr", default));
            Assert.Equal(ErrorCodes.IapVerificationFailed, ex.Code);
            Assert.False(db.IapPurchases.Query().Any());
        }

        [Fact]
        public async Task MockPlatform_InDev_Succeeds()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, Product(), env: "dev");

            var res = await svc.VerifyIapAsync(1, MockReq(), "corr", default);

            Assert.True(res.Success);
            Assert.True(db.IapPurchases.Query().Any(x => x.OrderId == "MOCK_1"));
        }

        [Fact]
        public async Task NonConsumable_FlipsNoAdsFlag()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, Product(IapProductType.NonConsumable), env: "dev");

            var res = await svc.VerifyIapAsync(1, MockReq(), "corr", default);

            Assert.True(res.IsNoAds);
            var player = await db.Players.FindAsync(1L);
            Assert.True(player!.IsNoAds);
        }

        [Fact]
        public async Task DuplicateOrderId_IsRejected()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, Product(), env: "dev");

            await svc.VerifyIapAsync(1, MockReq("DUP"), "corr", default);

            var ex = await Assert.ThrowsAsync<GameApiException>(
                () => svc.VerifyIapAsync(1, MockReq("DUP"), "corr", default));
            Assert.Equal(ErrorCodes.DuplicateOrder, ex.Code);
        }

        [Fact]
        public async Task PurchaseLimit_IsEnforced()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, Product(limit: 1), env: "dev");

            await svc.VerifyIapAsync(1, MockReq("ORD_A"), "corr", default);

            var ex = await Assert.ThrowsAsync<GameApiException>(
                () => svc.VerifyIapAsync(1, MockReq("ORD_B"), "corr", default));
            Assert.Equal(ErrorCodes.PurchaseLimitReached, ex.Code);
        }
    }
}
