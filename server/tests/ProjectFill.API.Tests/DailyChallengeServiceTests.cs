using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Achievement;
using ProjectFill.Application.Common;
using ProjectFill.Application.Cosmetic;
using ProjectFill.Application.Currency;
using ProjectFill.Application.DailyChallenge;
using ProjectFill.Application.Inventory;
using ProjectFill.Application.Rewards;
using ProjectFill.Infrastructure.Generated;
using Xunit;

namespace ProjectFill.API.Tests
{
    public sealed class DailyChallengeServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private static DailyChallengeService NewService(AppDbContext db)
        {
            var data = new FakeStaticData();
            var currency = new CurrencyService(db);
            var inventory = new InventoryService(db, currency, data);
            var reward = new RewardService(db, currency, inventory);
            var cosmetic = new CosmeticService(db, currency, data);
            var achievement = new AchievementService(db, reward, cosmetic, data);
            return new DailyChallengeService(db, reward, cosmetic, achievement);
        }

        private static string TodayKey() => DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyy-MM-dd");
        private static string YesterdayKey() => DateTimeOffset.UtcNow.UtcDateTime.AddDays(-1).ToString("yyyy-MM-dd");

        private static async Task AddPlayerAsync(AppDbContext db, long userId)
        {
            db.Players.Insert(new PlayersRow
            {
                UserId = userId, PlatformPid = $"pid{userId}", DisplayName = $"P{userId}", AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow,
            });
            await db.SaveAsync();
        }

        [Fact]
        public async Task GetToday_CreatesDeterministicChallenge()
        {
            using var db = CreateDbContext();
            var svc = NewService(db);

            var a = await svc.GetTodayAsync(1, default);
            var b = await svc.GetTodayAsync(2, default);

            Assert.Equal(TodayKey(), a.ChallengeDate);
            Assert.Equal(a.StageSeed, b.StageSeed);
            Assert.InRange(a.SignalTypeCount, 5, 6);
        }

        [Fact]
        public async Task SubmitClear_First_SetsStreak1Rank1()
        {
            using var db = CreateDbContext();
            await AddPlayerAsync(db, 1);

            var res = await NewService(db).SubmitClearAsync(1, 25, 90, "corr", default);

            Assert.Equal(1, res.CurrentStreak);
            Assert.Equal(1, res.Rank);
            var rec = await db.UserDailyChallengeRecords.FindAsync(1L, TodayKey());
            Assert.True(rec?.IsCleared);
        }

        [Fact]
        public async Task SubmitClear_Duplicate_Throws()
        {
            using var db = CreateDbContext();
            await AddPlayerAsync(db, 1);
            var svc = NewService(db);
            await svc.SubmitClearAsync(1, 25, 90, "corr", default);

            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.SubmitClearAsync(1, 20, 80, "corr", default));
            Assert.Equal(ErrorCodes.ChallengeAlreadyPlayed, ex.Code);
        }

        [Fact]
        public async Task SubmitClear_ConsecutiveDay_IncrementsStreak()
        {
            using var db = CreateDbContext();
            await AddPlayerAsync(db, 1);
            db.UserChallengeStreaks.Insert(new UserChallengeStreaksRow
            {
                UserId = 1, CurrentStreak = 4, BestStreak = 4, LastClearDate = YesterdayKey(), UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveAsync();

            var res = await NewService(db).SubmitClearAsync(1, 30, 100, "corr", default);
            Assert.Equal(5, res.CurrentStreak);
        }

        [Fact]
        public async Task Ranking_OrdersByMovesThenTime()
        {
            using var db = CreateDbContext();
            await AddPlayerAsync(db, 1);
            await AddPlayerAsync(db, 2);
            var svc = NewService(db);
            await svc.SubmitClearAsync(1, 30, 100, "corr", default);
            await svc.SubmitClearAsync(2, 20, 100, "corr", default);

            var ranking = await svc.GetRankingAsync(1, 0, 50, default);
            Assert.Equal(2, ranking.TotalCount);
            Assert.Equal(2L, ranking.Entries[0].UserId); // fewer moves ranks first
            Assert.Equal(1, ranking.Entries[0].Rank);
            Assert.True(ranking.Entries[0].IsMe == false);
        }
    }
}
