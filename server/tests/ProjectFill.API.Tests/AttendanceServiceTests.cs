using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Attendance;
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
    public sealed class AttendanceServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private sealed class FakeAttendanceData : FakeStaticData
        {
            private readonly List<DailyLoginRewardData> _rewards = new();
            private readonly List<DailyLoginMilestoneData> _milestones = new()
            {
                new DailyLoginMilestoneData { Id = 1, ThresholdDays = 30, RewardGroupId = 4101, CosmeticConditionId = "day_30" },
            };
            private readonly Dictionary<string, CosmeticItemData> _cosmetics = new()
            {
                ["board_circuit"] = new CosmeticItemData { CosmeticId = "board_circuit", Category = CosmeticCategory.Board, UnlockType = CosmeticUnlockType.Attendance, UnlockConditionId = "day_30" },
            };

            public FakeAttendanceData()
            {
                int id = 1;
                for (int d = 1; d <= 7; d++) _rewards.Add(new DailyLoginRewardData { Id = id++, CycleType = AttendanceCycleType.First, Day = d, RewardGroupId = 4000 + d, SortOrder = d });
                for (int d = 1; d <= 7; d++) _rewards.Add(new DailyLoginRewardData { Id = id++, CycleType = AttendanceCycleType.Repeat, Day = d, RewardGroupId = 4010 + d, SortOrder = d });
            }

            public override IReadOnlyList<DailyLoginRewardData> GetAllDailyLoginRewards() => _rewards;
            public override IReadOnlyList<DailyLoginMilestoneData> GetAllDailyLoginMilestones() => _milestones;
            public override CosmeticItemData? GetCosmeticItem(string cosmetic_id) => _cosmetics.GetValueOrDefault(cosmetic_id);
            public override IReadOnlyList<CosmeticItemData> GetAllCosmeticItems() => new List<CosmeticItemData>(_cosmetics.Values);
        }

        private static AttendanceService NewService(AppDbContext db, FakeAttendanceData data)
        {
            var currency = new CurrencyService(db);
            var inventory = new InventoryService(db, currency, data);
            var reward = new RewardService(db, currency, inventory);
            var cosmetic = new CosmeticService(db, currency, data);
            return new AttendanceService(db, reward, cosmetic, data);
        }

        private static DateTimeOffset DayOffset(DateTime date) => new(date, TimeSpan.Zero);

        [Fact]
        public async Task FirstClaim_SetsDay1Cycle1Streak1()
        {
            using var db = CreateDbContext();
            var res = await NewService(db, new FakeAttendanceData()).ClaimAsync(1, "corr", default);

            Assert.Equal(1, res.Day);
            Assert.Equal(1, res.Cycle);
            Assert.Equal(1, res.Streak);
            Assert.Equal(1, res.TotalAttendedDays);
        }

        [Fact]
        public async Task DuplicateClaimSameDay_Throws()
        {
            using var db = CreateDbContext();
            var svc = NewService(db, new FakeAttendanceData());
            await svc.ClaimAsync(1, "corr", default);

            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.ClaimAsync(1, "corr", default));
            Assert.Equal(ErrorCodes.AttendanceAlreadyClaimed, ex.Code);
        }

        [Fact]
        public async Task ConsecutiveDay_AdvancesDayAndStreak()
        {
            using var db = CreateDbContext();
            var today = DateTime.UtcNow.Date;
            db.UserLoginAttendance.Insert(new UserLoginAttendanceRow
            {
                UserId = 1, CurrentDay = 1, CurrentCycle = 1, CurrentStreak = 1, BestStreak = 1,
                TotalAttendedDays = 1, LastAttendedDate = DayOffset(today.AddDays(-1)), UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveAsync();

            var res = await NewService(db, new FakeAttendanceData()).ClaimAsync(1, "corr", default);
            Assert.Equal(2, res.Day);
            Assert.Equal(2, res.Streak);
        }

        [Fact]
        public async Task CycleWrap_Day7RollsToDay1Cycle2()
        {
            using var db = CreateDbContext();
            var today = DateTime.UtcNow.Date;
            db.UserLoginAttendance.Insert(new UserLoginAttendanceRow
            {
                UserId = 1, CurrentDay = 7, CurrentCycle = 1, CurrentStreak = 7, BestStreak = 7,
                TotalAttendedDays = 7, LastAttendedDate = DayOffset(today.AddDays(-1)), UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveAsync();

            var res = await NewService(db, new FakeAttendanceData()).ClaimAsync(1, "corr", default);
            Assert.Equal(1, res.Day);
            Assert.Equal(2, res.Cycle);
            Assert.Equal(8, res.Streak);
        }

        [Fact]
        public async Task MissedDay_StreakResets()
        {
            using var db = CreateDbContext();
            var today = DateTime.UtcNow.Date;
            db.UserLoginAttendance.Insert(new UserLoginAttendanceRow
            {
                UserId = 1, CurrentDay = 3, CurrentCycle = 1, CurrentStreak = 3, BestStreak = 3,
                TotalAttendedDays = 3, LastAttendedDate = DayOffset(today.AddDays(-3)), UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveAsync();

            var res = await NewService(db, new FakeAttendanceData()).ClaimAsync(1, "corr", default);
            Assert.Equal(4, res.Day);
            Assert.Equal(1, res.Streak);
        }

        [Fact]
        public async Task MilestoneHit_UnlocksCosmetic()
        {
            using var db = CreateDbContext();
            var today = DateTime.UtcNow.Date;
            db.UserLoginAttendance.Insert(new UserLoginAttendanceRow
            {
                UserId = 1, CurrentDay = 5, CurrentCycle = 5, CurrentStreak = 5, BestStreak = 10,
                TotalAttendedDays = 29, LastAttendedDate = DayOffset(today.AddDays(-1)), UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveAsync();

            var res = await NewService(db, new FakeAttendanceData()).ClaimAsync(1, "corr", default);
            Assert.Equal(30, res.TotalAttendedDays);
            Assert.Contains("board_circuit", res.UnlockedCosmetics);
            Assert.NotNull(await db.UserCosmetics.FindAsync(1L, "board_circuit"));
        }
    }
}
