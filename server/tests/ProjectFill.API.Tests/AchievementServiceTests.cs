using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Achievement;
using ProjectFill.Application.Common;
using ProjectFill.Application.Cosmetic;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Inventory;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;
using Xunit;

namespace ProjectFill.API.Tests
{
    public sealed class AchievementServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private sealed class FakeAchievementData : FakeStaticData
        {
            private readonly List<AchievementData> _achievements = new()
            {
                new AchievementData { AchievementId = "login_30", Category = AchievementCategory.Dedication, Tier = AchievementTier.Silver, RewardGroupId = 6018, ConditionType = AchievementConditionType.TotalLoginDays, ConditionValue = 30, SortOrder = 1 },
                new AchievementData { AchievementId = "login_100", Category = AchievementCategory.Dedication, Tier = AchievementTier.Platinum, RewardGroupId = 6018, ConditionType = AchievementConditionType.TotalLoginDays, ConditionValue = 100, SortOrder = 2 },
                new AchievementData { AchievementId = "chal_7", Category = AchievementCategory.Dedication, Tier = AchievementTier.Silver, RewardGroupId = 6015, ConditionType = AchievementConditionType.ChallengeClearStreak, ConditionValue = 7, SortOrder = 3 },
            };
            private readonly Dictionary<string, CosmeticItemData> _cosmetics = new()
            {
                ["board_circuit"] = new CosmeticItemData { CosmeticId = "board_circuit", Category = CosmeticCategory.Board, UnlockType = CosmeticUnlockType.Achievement, UnlockConditionId = "login_30" },
            };

            public override AchievementData? GetAchievement(string id) => _achievements.FirstOrDefault(x => x.AchievementId == id);
            public override IReadOnlyList<AchievementData> GetAllAchievements() => _achievements;
            public override CosmeticItemData? GetCosmeticItem(string id) => _cosmetics.GetValueOrDefault(id);
            public override IReadOnlyList<CosmeticItemData> GetAllCosmeticItems() => new List<CosmeticItemData>(_cosmetics.Values);
        }

        private static AchievementService NewService(AppDbContext db, FakeAchievementData data)
        {
            var currency = new CurrencyService(db);
            var inventory = new InventoryService(db, currency, data);
            var reward = new RewardService(db, currency, inventory);
            var cosmetic = new CosmeticService(db, currency, data);
            return new AchievementService(db, reward, cosmetic, data);
        }

        private static async Task SeedAttendanceAsync(AppDbContext db, long userId, int totalDays)
        {
            db.UserLoginAttendance.Insert(new UserLoginAttendanceRow
            {
                UserId = userId, CurrentDay = 1, CurrentCycle = 1, CurrentStreak = 1, BestStreak = 1,
                TotalAttendedDays = totalDays, UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveAsync();
        }

        [Fact]
        public async Task GetList_DerivedProgress_ReflectsAttendance()
        {
            using var db = CreateDbContext();
            await SeedAttendanceAsync(db, 1, 30);

            var list = await NewService(db, new FakeAchievementData()).GetListAsync(1, default);

            Assert.Contains(list.Achievements, a => a.AchievementId == "login_30" && a.IsCompleted && a.Progress == 30);
            Assert.Contains(list.Achievements, a => a.AchievementId == "login_100" && !a.IsCompleted);
        }

        [Fact]
        public async Task Claim_Completed_UnlocksCosmeticAndMarksClaimed()
        {
            using var db = CreateDbContext();
            await SeedAttendanceAsync(db, 1, 30);

            var res = await NewService(db, new FakeAchievementData()).ClaimAsync(1, "login_30", "corr", default);

            Assert.Contains("board_circuit", res.UnlockedCosmetics);
            var row = await db.UserAchievements.FindAsync(1L, "login_30");
            Assert.True(row?.RewardClaimed);
        }

        [Fact]
        public async Task Claim_NotCompleted_Throws()
        {
            using var db = CreateDbContext();
            await SeedAttendanceAsync(db, 1, 30);

            var ex = await Assert.ThrowsAsync<GameApiException>(() => NewService(db, new FakeAchievementData()).ClaimAsync(1, "login_100", "corr", default));
            Assert.Equal(ErrorCodes.AchievementNotCompleted, ex.Code);
        }

        [Fact]
        public async Task ReportValue_CompletesGameplayAchievement()
        {
            using var db = CreateDbContext();
            var svc = NewService(db, new FakeAchievementData());

            await svc.ReportValueAsync(1, AchievementConditionType.ChallengeClearStreak, 7, default);

            var list = await svc.GetListAsync(1, default);
            Assert.Contains(list.Achievements, a => a.AchievementId == "chal_7" && a.IsCompleted);
        }
    }
}
