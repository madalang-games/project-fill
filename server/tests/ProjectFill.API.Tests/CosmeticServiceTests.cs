using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Common;
using ProjectFill.Application.Cosmetic;
using ProjectFill.Application.Currency;
using ProjectFill.Contracts.Cosmetic;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;
using Xunit;

namespace ProjectFill.API.Tests
{
    public sealed class CosmeticServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private sealed class FakeCosmeticData : FakeStaticData
        {
            private readonly Dictionary<string, CosmeticItemData> _items = new()
            {
                ["chip_default"] = new CosmeticItemData { CosmeticId = "chip_default", Category = CosmeticCategory.Chip, UnlockType = CosmeticUnlockType.Default, UnlockCost = 0, UnlockConditionId = "" },
                ["chip_hex"] = new CosmeticItemData { CosmeticId = "chip_hex", Category = CosmeticCategory.Chip, UnlockType = CosmeticUnlockType.Gold, UnlockCost = 800, UnlockConditionId = "" },
                ["chip_platinum"] = new CosmeticItemData { CosmeticId = "chip_platinum", Category = CosmeticCategory.Chip, UnlockType = CosmeticUnlockType.Achievement, UnlockCost = 0, UnlockConditionId = "prg_04" },
                ["lane_default"] = new CosmeticItemData { CosmeticId = "lane_default", Category = CosmeticCategory.Lane, UnlockType = CosmeticUnlockType.Default, UnlockCost = 0, UnlockConditionId = "" },
                ["board_default"] = new CosmeticItemData { CosmeticId = "board_default", Category = CosmeticCategory.Board, UnlockType = CosmeticUnlockType.Default, UnlockCost = 0, UnlockConditionId = "" },
            };

            public override CosmeticItemData? GetCosmeticItem(string cosmetic_id) => _items.GetValueOrDefault(cosmetic_id);
            public override IReadOnlyList<CosmeticItemData> GetAllCosmeticItems() => new List<CosmeticItemData>(_items.Values);
        }

        private static CosmeticService NewService(AppDbContext db) => new(db, new CurrencyService(db), new FakeCosmeticData());

        [Fact]
        public async Task UnlockWithGold_SufficientGold_DeductsAndOwns()
        {
            using var db = CreateDbContext();
            long userId = 1;
            db.UserCurrency.Insert(new UserCurrencyRow { UserId = userId, SoftAmount = 1000, UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveAsync();

            var (id, currency) = await NewService(db).UnlockWithGoldAsync(userId, "chip_hex", "corr", default);

            Assert.Equal("chip_hex", id);
            Assert.Equal(200, currency.SoftAmount);
            Assert.NotNull(await db.UserCosmetics.FindAsync(userId, "chip_hex"));
        }

        [Fact]
        public async Task UnlockWithGold_NotGoldType_Throws()
        {
            using var db = CreateDbContext();
            long userId = 1;
            db.UserCurrency.Insert(new UserCurrencyRow { UserId = userId, SoftAmount = 1000, UpdatedAt = DateTimeOffset.UtcNow });
            await db.SaveAsync();

            var ex = await Assert.ThrowsAsync<GameApiException>(() => NewService(db).UnlockWithGoldAsync(userId, "chip_platinum", "corr", default));
            Assert.Equal(ErrorCodes.CosmeticNotPurchasable, ex.Code);
        }

        [Fact]
        public async Task UnlockByCondition_GrantsMatchingCosmetics()
        {
            using var db = CreateDbContext();
            long userId = 1;

            var unlocked = await NewService(db).UnlockByConditionAsync(userId, "prg_04", "corr", default);
            await db.SaveAsync();

            Assert.Contains("chip_platinum", unlocked);
            Assert.NotNull(await db.UserCosmetics.FindAsync(userId, "chip_platinum"));
        }

        [Fact]
        public async Task SetActive_UnownedSkin_Throws()
        {
            using var db = CreateDbContext();
            long userId = 1;

            var req = new SetActiveCosmeticRequest { ChipSkin = "chip_hex" };
            var ex = await Assert.ThrowsAsync<GameApiException>(() => NewService(db).SetActiveAsync(userId, req, "corr", default));
            Assert.Equal(ErrorCodes.CosmeticNotOwned, ex.Code);
        }

        [Fact]
        public async Task SetActive_DefaultSkin_Succeeds()
        {
            using var db = CreateDbContext();
            long userId = 1;

            var req = new SetActiveCosmeticRequest { ChipSkin = "chip_default" };
            var active = await NewService(db).SetActiveAsync(userId, req, "corr", default);

            Assert.Equal("chip_default", active.ChipSkin);
            Assert.Equal("lane_default", active.LaneSkin);
        }

        [Fact]
        public async Task GetList_DefaultCosmeticsAlwaysUnlocked()
        {
            using var db = CreateDbContext();
            long userId = 1;

            var list = await NewService(db).GetListAsync(userId, default);

            Assert.Contains(list.Items, x => x.CosmeticId == "chip_default" && x.Unlocked);
            Assert.Contains(list.Items, x => x.CosmeticId == "chip_hex" && !x.Unlocked);
        }
    }
}
