using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectFill.Application.Ranking;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;
using Xunit;

namespace ProjectFill.API.Tests;

public sealed class RankingServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static (RankingService service, Mock<IDatabase> redisMock) CreateService(AppDbContext db)
    {
        var redisMock = new Mock<IDatabase>();
        var multiplexerMock = new Mock<IConnectionMultiplexer>();
        multiplexerMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(redisMock.Object);
        redisMock
            .Setup(r => r.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        return (new RankingService(db, multiplexerMock.Object, NullLogger<RankingService>.Instance), redisMock);
    }

    [Fact]
    public async Task GetMyGlobalRankAsync_Stars_ReturnsStarsScore()
    {
        using var db = CreateDb();
        db.Players.Insert(new PlayersRow { UserId = 1, PlatformPid = "p1", DisplayName = "Alice", AvatarId = 2, AccountCreatedAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow });
        db.UserRankingTotals.Insert(new UserRankingTotalsRow { UserId = 1, TotalEarnedStars = 42, MaxClearedStageId = 10, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveAsync();

        var (service, redis) = CreateService(db);
        redis.Setup(r => r.SortedSetRankAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Order>(), It.IsAny<CommandFlags>()))
             .ReturnsAsync(0L);

        var result = await service.GetMyGlobalRankAsync(1L, "stars", CancellationToken.None);

        Assert.NotNull(result.Entry);
        Assert.Equal(1, result.Entry!.Rank);
        Assert.Equal(42, result.Entry.Score);
        Assert.Equal("Alice", result.Entry.DisplayName);
        Assert.Equal(2, result.Entry.AvatarId);
        Assert.Equal("stars", result.RankingType);
    }

    [Fact]
    public async Task GetMyGlobalRankAsync_MaxStage_ReturnsMaxStageScore()
    {
        using var db = CreateDb();
        db.Players.Insert(new PlayersRow { UserId = 2, PlatformPid = "p2", DisplayName = "Bob", AvatarId = 3, AccountCreatedAt = DateTimeOffset.UtcNow, LastLoginAt = DateTimeOffset.UtcNow });
        db.UserRankingTotals.Insert(new UserRankingTotalsRow { UserId = 2, TotalEarnedStars = 5, MaxClearedStageId = 99, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveAsync();

        var (service, redis) = CreateService(db);
        redis.Setup(r => r.SortedSetRankAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Order>(), It.IsAny<CommandFlags>()))
             .ReturnsAsync(2L);

        var result = await service.GetMyGlobalRankAsync(2L, "max-stage", CancellationToken.None);

        Assert.NotNull(result.Entry);
        Assert.Equal(3, result.Entry!.Rank);
        Assert.Equal(99, result.Entry.Score);
        Assert.Equal("max-stage", result.RankingType);
    }

    [Fact]
    public async Task GetMyGlobalRankAsync_UserNotInRedis_ReturnsNullEntry()
    {
        using var db = CreateDb();
        var (service, redis) = CreateService(db);
        redis.Setup(r => r.SortedSetRankAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Order>(), It.IsAny<CommandFlags>()))
             .ReturnsAsync((long?)null);

        var result = await service.GetMyGlobalRankAsync(999L, "stars", CancellationToken.None);

        Assert.Null(result.Entry);
        Assert.Equal("stars", result.RankingType);
    }
}
