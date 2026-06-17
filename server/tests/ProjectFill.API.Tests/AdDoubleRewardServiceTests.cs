using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Inventory;
using ProjectFill.Application.Rewards;
using ProjectFill.Application.Stage;
using ProjectFill.Contracts.Ad;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;
using Xunit;

namespace ProjectFill.API.Tests
{
    public sealed class AdDoubleRewardServiceTests
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
            private readonly Dictionary<int, StageData> _stages = new()
            {
                [1] = new StageData { StageId = 1, ChapterId = 1, StageOrder = 1, Difficulty = 0, RewardGroupId = 2001, Types = 3 },
            };

            public override StageData? GetStage(int stage_id) => _stages.GetValueOrDefault(stage_id);
            public override IReadOnlyList<StageData> GetAllStages() => _stages.Values.ToList();
        }

        // verified=true → grant; verified=false → pending (AdSsvPending path).
        private static AdDoubleRewardService NewService(AppDbContext db, bool verified)
        {
            var currency = new CurrencyService(db);
            var data = new FakeStageData();
            var inventory = new InventoryService(db, currency, data);
            var reward = new RewardService(db, currency, inventory);

            var verifier = new Mock<IAdRewardVerifier>();
            verifier.Setup(v => v.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AdVerifyResult(verified, verified ? "tx-" + Guid.NewGuid().ToString("N") : string.Empty));

            var redisDb = new Mock<IDatabase>();
            redisDb.Setup(r => r.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            var mux = new Mock<IConnectionMultiplexer>();
            mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(redisDb.Object);

            return new AdDoubleRewardService(db, mux.Object, data, reward, verifier.Object);
        }

        private static async Task SeedClearedAsync(AppDbContext db, long userId, int stageId)
        {
            db.Players.Insert(new PlayersRow { UserId = userId, PlatformPid = $"p{userId}", DisplayName = "P", AvatarId = 1, AccountCreatedAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow });
            db.UserStageProgress.Insert(new UserStageProgressRow
            {
                UserId = userId, StageId = stageId, StageClear = true,
                LatestMovesUsed = 10, BestMovesUsed = 10,
                FirstClearedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveAsync();
        }

        private static AdDoubleRewardRequest Req(int stageId = 1)
            => new AdDoubleRewardRequest { Provider = "admob", AdToken = "nonce", StageId = stageId, AttemptId = "att-1" };

        [Fact]
        public async Task Verified_GrantsAndWritesClaimStateOnce()
        {
            using var db = CreateDb();
            await SeedClearedAsync(db, 1, 1);

            var res = await NewService(db, verified: true).ClaimAsync(1, Req(), "corr", default);

            Assert.True(res.Granted);
            Assert.False(res.Duplicate);

            var claim = await db.UserRewardClaimState.FindAsync(1L, "double_reward:1", "once");
            Assert.NotNull(claim);
        }

        [Fact]
        public async Task SecondClaim_ReturnsDuplicate()
        {
            using var db = CreateDb();
            await SeedClearedAsync(db, 1, 1);

            await NewService(db, verified: true).ClaimAsync(1, Req(), "corr", default);
            var second = await NewService(db, verified: true).ClaimAsync(1, Req(), "corr", default);

            Assert.False(second.Granted);
            Assert.True(second.Duplicate);
        }

        [Fact]
        public async Task Unverified_ThrowsAdSsvPending()
        {
            using var db = CreateDb();
            await SeedClearedAsync(db, 1, 1);

            var ex = await Assert.ThrowsAsync<GameApiException>(() => NewService(db, verified: false).ClaimAsync(1, Req(), "corr", default));
            Assert.Equal(ErrorCodes.AdSsvPending, ex.Code);
        }

        [Fact]
        public async Task NotCleared_ThrowsDoubleRewardNotEligible()
        {
            using var db = CreateDb();
            db.Players.Insert(new PlayersRow { UserId = 1, PlatformPid = "p1", DisplayName = "P", AvatarId = 1, AccountCreatedAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow });
            await db.SaveAsync();

            var ex = await Assert.ThrowsAsync<GameApiException>(() => NewService(db, verified: true).ClaimAsync(1, Req(), "corr", default));
            Assert.Equal(ErrorCodes.DoubleRewardNotEligible, ex.Code);
        }

        [Fact]
        public async Task UnknownStage_ThrowsStageNotFound()
        {
            using var db = CreateDb();
            await SeedClearedAsync(db, 1, 1);

            var ex = await Assert.ThrowsAsync<GameApiException>(() => NewService(db, verified: true).ClaimAsync(1, Req(999), "corr", default));
            Assert.Equal(ErrorCodes.StageNotFound, ex.Code);
        }
    }
}
