using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Achievement;
using ProjectFill.Application.Common;
using ProjectFill.Application.Cosmetic;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Event;
using ProjectFill.Application.Inventory;
using ProjectFill.Application.Rewards;
using ProjectFill.Contracts.GameTypes;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;
using Xunit;

namespace ProjectFill.API.Tests
{
    public sealed class WeeklyMissionServiceTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private sealed class FakeWeeklyData : FakeStaticData
        {
            private readonly List<WeeklyMissionPoolData> _pool = new()
            {
                new WeeklyMissionPoolData { MissionId = "wm_stage10",   ConditionType = WeeklyMissionConditionType.StageClearCount,   ConditionValue = 10, EpReward = 200, NameKey = "n1", DescKey = "d1" },
                new WeeklyMissionPoolData { MissionId = "wm_perfect3",  ConditionType = WeeklyMissionConditionType.PerfectClearCount, ConditionValue = 3,  EpReward = 300, NameKey = "n2", DescKey = "d2" },
                new WeeklyMissionPoolData { MissionId = "wm_noboost5",  ConditionType = WeeklyMissionConditionType.BoosterlessClear,  ConditionValue = 5,  EpReward = 300, NameKey = "n3", DescKey = "d3" },
                new WeeklyMissionPoolData { MissionId = "wm_progress5", ConditionType = WeeklyMissionConditionType.ChapterProgress,   ConditionValue = 5,  EpReward = 200, NameKey = "n4", DescKey = "d4" },
                new WeeklyMissionPoolData { MissionId = "wm_best5",     ConditionType = WeeklyMissionConditionType.BestMovesRenew,    ConditionValue = 5,  EpReward = 200, NameKey = "n5", DescKey = "d5" },
            };
            private readonly List<WeeklyMissionTrackData> _track = new()
            {
                new WeeklyMissionTrackData { EpThreshold = 200,  RewardGroupId = 7001, SortOrder = 1 },
                new WeeklyMissionTrackData { EpThreshold = 500,  RewardGroupId = 7002, SortOrder = 2 },
                new WeeklyMissionTrackData { EpThreshold = 900,  RewardGroupId = 7003, SortOrder = 3 },
                new WeeklyMissionTrackData { EpThreshold = 1200, RewardGroupId = 7004, SortOrder = 4 },
            };
            private readonly List<AchievementData> _achievements = new()
            {
                new AchievementData { AchievementId = "ded_03", Category = AchievementCategory.Dedication, Tier = AchievementTier.Silver, RewardGroupId = 6015, ConditionType = AchievementConditionType.WeeklyMissionComplete, ConditionValue = 1, SortOrder = 1 },
            };

            public override WeeklyMissionPoolData? GetWeeklyMissionPool(string id) => _pool.FirstOrDefault(x => x.MissionId == id);
            public override IReadOnlyList<WeeklyMissionPoolData> GetAllWeeklyMissionPools() => _pool;
            public override WeeklyMissionTrackData? GetWeeklyMissionTrack(int t) => _track.FirstOrDefault(x => x.EpThreshold == t);
            public override IReadOnlyList<WeeklyMissionTrackData> GetAllWeeklyMissionTracks() => _track;
            public override AchievementData? GetAchievement(string id) => _achievements.FirstOrDefault(x => x.AchievementId == id);
            public override IReadOnlyList<AchievementData> GetAllAchievements() => _achievements;
        }

        private static WeeklyMissionService NewService(AppDbContext db, FakeWeeklyData data)
        {
            var currency = new CurrencyService(db);
            var inventory = new InventoryService(db, currency, data);
            var reward = new RewardService(db, currency, inventory);
            var cosmetic = new CosmeticService(db, currency, data);
            var achievement = new AchievementService(db, reward, cosmetic, data);
            return new WeeklyMissionService(db, data, reward, achievement);
        }

        private static async Task SeedPlayerAsync(AppDbContext db, long userId)
        {
            db.Players.Insert(new PlayersRow { UserId = userId, PlatformPid = $"p{userId}", DisplayName = "P", AvatarId = 1, AccountCreatedAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow });
            await db.SaveAsync();
        }

        [Fact]
        public async Task GetStatus_ReturnsFiveMissionsAndTrack()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);

            var res = await NewService(db, new FakeWeeklyData()).GetStatusAsync(1, default);

            Assert.Equal(WeeklyMissionService.CurrentWeekStart(), res.WeekStartDate);
            Assert.Equal(5, res.Missions.Count);
            Assert.Equal(4, res.Track.Count);
            Assert.Equal(0, res.TotalEp);
            Assert.InRange(res.DaysRemaining, 1, 7);
        }

        [Fact]
        public async Task ReportProgress_CompletesMissionAndAccruesEp()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeWeeklyData());

            await svc.ReportProgressAsync(1, WeeklyMissionConditionType.StageClearCount, 10, default);

            var res = await svc.GetStatusAsync(1, default);
            Assert.Equal(200, res.TotalEp);
            Assert.Contains(res.Missions, m => m.MissionId == "wm_stage10" && m.IsCompleted && m.Progress == 10);
            Assert.Contains(res.Track, t => t.EpThreshold == 200 && t.IsReached && !t.IsClaimed);
        }

        [Fact]
        public async Task ReportProgress_PartialProgress_NoEp()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeWeeklyData());

            await svc.ReportProgressAsync(1, WeeklyMissionConditionType.StageClearCount, 4, default);

            var res = await svc.GetStatusAsync(1, default);
            Assert.Equal(0, res.TotalEp);
            Assert.Contains(res.Missions, m => m.MissionId == "wm_stage10" && !m.IsCompleted && m.Progress == 4);
        }

        [Fact]
        public async Task Claim_ReachedThreshold_RecordsClaim_DuplicateThrows()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeWeeklyData());
            await svc.ReportProgressAsync(1, WeeklyMissionConditionType.StageClearCount, 10, default); // 200 EP

            var res = await svc.ClaimAsync(1, 200, "corr", default);
            Assert.Equal(200, res.EpThreshold);

            var state = await db.UserWeeklyMissionState.FindAsync(1L, WeeklyMissionService.CurrentWeekStart());
            Assert.Contains("200", state!.ClaimedThresholds);

            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.ClaimAsync(1, 200, "corr", default));
            Assert.Equal(ErrorCodes.WeeklyMissionAlreadyClaimed, ex.Code);
        }

        [Fact]
        public async Task Claim_BelowThreshold_Throws()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeWeeklyData());

            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.ClaimAsync(1, 500, "corr", default));
            Assert.Equal(ErrorCodes.WeeklyMissionThresholdNotReached, ex.Code);
        }

        [Fact]
        public async Task Claim_InvalidThreshold_Throws()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeWeeklyData());

            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.ClaimAsync(1, 123, "corr", default));
            Assert.Equal(ErrorCodes.WeeklyMissionInvalidThreshold, ex.Code);
        }

        [Fact]
        public async Task FullTrack_ReportsWeeklyMissionCompleteAchievement()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeWeeklyData());

            await svc.ReportProgressAsync(1, WeeklyMissionConditionType.StageClearCount, 10, default);
            await svc.ReportProgressAsync(1, WeeklyMissionConditionType.PerfectClearCount, 3, default);
            await svc.ReportProgressAsync(1, WeeklyMissionConditionType.BoosterlessClear, 5, default);
            await svc.ReportProgressAsync(1, WeeklyMissionConditionType.ChapterProgress, 5, default);
            await svc.ReportProgressAsync(1, WeeklyMissionConditionType.BestMovesRenew, 5, default);

            var res = await svc.GetStatusAsync(1, default);
            Assert.Equal(1200, res.TotalEp);

            var ach = await db.UserAchievements.FindAsync(1L, "ded_03");
            Assert.True(ach!.IsCompleted);
        }
    }
}
