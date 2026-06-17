using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectFill.Application.Achievement;
using ProjectFill.Application.Common;
using ProjectFill.Application.Cosmetic;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Inventory;
using ProjectFill.Application.Ranking;
using ProjectFill.Application.Rewards;
using ProjectFill.Application.Stage;
using ProjectFill.Contracts.Stage;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;
using Xunit;

namespace ProjectFill.API.Tests
{
    public sealed class StageServiceTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private sealed class FakeStageData : FakeStaticData
        {
            private readonly Dictionary<int, StageData> _stages;
            private readonly Dictionary<int, ChapterData> _chapters;

            public FakeStageData()
            {
                _stages = new Dictionary<int, StageData>
                {
                    [1] = new StageData { StageId = 1, ChapterId = 1, StageOrder = 1, Difficulty = 0, RewardGroupId = 2001, Types = 3 },
                    [2] = new StageData { StageId = 2, ChapterId = 1, StageOrder = 2, Difficulty = 0, RewardGroupId = 2001, Types = 3 },
                };
                _chapters = new Dictionary<int, ChapterData>
                {
                    [1] = new ChapterData { ChapterId = 1, DisplayOrder = 1, BgThemeId = 1, ChestRewardGroupId = 3001 },
                };
            }

            public override StageData? GetStage(int stage_id) => _stages.GetValueOrDefault(stage_id);
            public override IReadOnlyList<StageData> GetAllStages() => _stages.Values.ToList();
            public override ChapterData? GetChapter(int chapter_id) => _chapters.GetValueOrDefault(chapter_id);
            public override IReadOnlyList<ChapterData> GetAllChapters() => _chapters.Values.ToList();
        }

        private static StageService NewService(AppDbContext db, FakeStageData data)
        {
            var currency = new CurrencyService(db);
            var inventory = new InventoryService(db, currency, data);
            var reward = new RewardService(db, currency, inventory);
            var cosmetic = new CosmeticService(db, currency, data);
            var achievement = new AchievementService(db, reward, cosmetic, data);

            var redisMock = new Mock<IDatabase>();
            redisMock.Setup(r => r.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            redisMock.Setup(r => r.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            redisMock.Setup(r => r.SortedSetRankAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Order>(), It.IsAny<CommandFlags>())).ReturnsAsync(0L);
            var mux = new Mock<IConnectionMultiplexer>();
            mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(redisMock.Object);
            var ranking = new RankingService(db, mux.Object, NullLogger<RankingService>.Instance);

            return new StageService(db, data, reward, achievement, ranking, currency);
        }

        private static StageClearRequest Req(int moves, int types = 3)
            => new StageClearRequest { RulesetVersion = 1, MovesUsed = moves, CompletedSignalTypes = Enumerable.Range(0, types).ToList() };

        private static async Task SeedPlayerAsync(AppDbContext db, long userId)
        {
            db.Players.Insert(new PlayersRow { UserId = userId, PlatformPid = $"p{userId}", DisplayName = "P", AvatarId = 1, AccountCreatedAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow });
            await db.SaveAsync();
        }

        [Fact]
        public async Task FirstClear_RecordsProgressTotalsAndWeekly()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);

            var res = await NewService(db, new FakeStageData()).ClearStageAsync(1, 1, Req(10), "corr", default);

            Assert.True(res.IsFirstClear);
            Assert.True(res.IsNewBest);
            Assert.Equal(10, res.BestMovesUsed);
            Assert.Equal(1, res.TotalClearedStages);
            Assert.Equal(1, res.MaxClearedStageId);
            Assert.Equal(1, res.WeeklyClearedCount);
            Assert.Equal(1, res.StageRank);

            var progress = await db.UserStageProgress.FindAsync(1L, 1);
            Assert.True(progress!.StageClear);
            Assert.Equal(10, progress.BestMovesUsed);
            Assert.NotNull(progress.FirstClearedAt);
        }

        [Fact]
        public async Task ReClear_BeatsBest_NoNewFirstClear()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            await svc.ClearStageAsync(1, 1, Req(10), "corr", default);
            var res = await svc.ClearStageAsync(1, 1, Req(7), "corr", default);

            Assert.False(res.IsFirstClear);
            Assert.True(res.IsNewBest);
            Assert.Equal(7, res.BestMovesUsed);
            Assert.Equal(1, res.TotalClearedStages); // distinct stage count unchanged

            var progress = await db.UserStageProgress.FindAsync(1L, 1);
            Assert.Equal(7, progress!.BestMovesUsed);
            Assert.Equal(7, progress.LatestMovesUsed);
        }

        [Fact]
        public async Task ReClear_WorseMoves_KeepsBest()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            await svc.ClearStageAsync(1, 1, Req(7), "corr", default);
            var res = await svc.ClearStageAsync(1, 1, Req(12), "corr", default);

            Assert.False(res.IsNewBest);
            Assert.Equal(7, res.BestMovesUsed);
        }

        [Fact]
        public async Task ChapterMilestone_GrantsChestOnLastStage()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            var first = await svc.ClearStageAsync(1, 1, Req(5), "corr", default);
            Assert.False(first.ChapterCompleted);

            var second = await svc.ClearStageAsync(1, 2, Req(5), "corr", default);
            Assert.True(second.ChapterCompleted);
            Assert.Equal(3001, second.ChapterChestRewardGroupId);

            // idempotent: chest claim-state recorded once
            var chest = await db.UserRewardClaimState.FindAsync(1L, "chapter_chest:1", "once");
            Assert.NotNull(chest);
        }

        [Fact]
        public async Task InvalidCompletedSignalTypes_Throws()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            var bad = new StageClearRequest { RulesetVersion = 1, MovesUsed = 5, CompletedSignalTypes = new List<int> { 0, 1 } }; // stage types = 3
            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.ClearStageAsync(1, 1, bad, "corr", default));
            Assert.Equal(ErrorCodes.InvalidStageClear, ex.Code);
        }

        [Fact]
        public async Task RulesetVersionMismatch_Throws()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            var bad = new StageClearRequest { RulesetVersion = 99, MovesUsed = 5, CompletedSignalTypes = new List<int> { 0, 1, 2 } };
            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.ClearStageAsync(1, 1, bad, "corr", default));
            Assert.Equal(ErrorCodes.StageRulesetMismatch, ex.Code);
        }

        [Fact]
        public async Task UnknownStage_Throws()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.ClearStageAsync(1, 999, Req(5), "corr", default));
            Assert.Equal(ErrorCodes.StageNotFound, ex.Code);
        }

        [Fact]
        public async Task Start_Stage1_AlwaysUnlocked()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            var res = await svc.StartStageAsync(1, 1, default);

            Assert.Equal(1, res.StageId);
            Assert.Equal(0, res.MaxClearedStageId);
            Assert.Equal(1, res.RulesetVersion);
        }

        [Fact]
        public async Task Start_LockedStage_Throws()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.StartStageAsync(1, 2, default));
            Assert.Equal(ErrorCodes.StageLocked, ex.Code);
        }

        [Fact]
        public async Task Start_UnlockedAfterPriorClear_Succeeds()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            await svc.ClearStageAsync(1, 1, Req(5), "corr", default);
            var res = await svc.StartStageAsync(1, 2, default);

            Assert.Equal(2, res.StageId);
            Assert.Equal(1, res.MaxClearedStageId);
        }

        [Fact]
        public async Task Start_UnknownStage_Throws()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.StartStageAsync(1, 999, default));
            Assert.Equal(ErrorCodes.StageNotFound, ex.Code);
        }

        [Fact]
        public async Task Clear_LockedStage_Throws()
        {
            using var db = CreateDb();
            await SeedPlayerAsync(db, 1);
            var svc = NewService(db, new FakeStageData());

            // Stage 2 cannot be cleared before stage 1 — closes the start-validation bypass.
            var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.ClearStageAsync(1, 2, Req(5), "corr", default));
            Assert.Equal(ErrorCodes.StageLocked, ex.Code);
        }
    }
}
