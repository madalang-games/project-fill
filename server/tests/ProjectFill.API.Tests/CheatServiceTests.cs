using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProjectFill.API.Dev;
using ProjectFill.Application.Cheat;
using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Inventory;
using ProjectFill.Domain.Enums;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;
using Xunit;

namespace ProjectFill.API.Tests;

public sealed class CheatServiceTests
{
    private const string Corr = "corr";

    private sealed class FakeCheatData : FakeStaticData
    {
        private readonly Dictionary<int, ItemData> _items = new()
        {
            [1] = new ItemData { Id = 1, Price = 100 },
            [2] = new ItemData { Id = 2, Price = 100 },
        };
        private readonly Dictionary<int, StageData> _stages = new()
        {
            [1] = new StageData { StageId = 1, ChapterId = 1, Types = 3 },
            [2] = new StageData { StageId = 2, ChapterId = 1, Types = 3 },
            [3] = new StageData { StageId = 3, ChapterId = 1, Types = 3 },
        };
        private readonly Dictionary<string, CosmeticItemData> _cosmetics = new()
        {
            ["chip_a"] = new CosmeticItemData { CosmeticId = "chip_a" },
            ["chip_b"] = new CosmeticItemData { CosmeticId = "chip_b" },
        };
        private readonly Dictionary<string, AchievementData> _achievements = new()
        {
            ["ach_a"] = new AchievementData { AchievementId = "ach_a", ConditionValue = 10 },
            ["ach_b"] = new AchievementData { AchievementId = "ach_b", ConditionValue = 5 },
        };

        public override ItemData? GetItem(int id) => _items.GetValueOrDefault(id);
        public override IReadOnlyList<ItemData> GetAllItems() => _items.Values.ToList();
        public override StageData? GetStage(int stage_id) => _stages.GetValueOrDefault(stage_id);
        public override IReadOnlyList<StageData> GetAllStages() => _stages.Values.ToList();
        public override CosmeticItemData? GetCosmeticItem(string cosmetic_id) => _cosmetics.GetValueOrDefault(cosmetic_id);
        public override IReadOnlyList<CosmeticItemData> GetAllCosmeticItems() => _cosmetics.Values.ToList();
        public override AchievementData? GetAchievement(string achievement_id) => _achievements.GetValueOrDefault(achievement_id);
        public override IReadOnlyList<AchievementData> GetAllAchievements() => _achievements.Values.ToList();
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static (CheatService svc, Mock<IDatabase> redis) NewService(AppDbContext db)
    {
        var data = new FakeCheatData();
        var currency = new CurrencyService(db);
        var inventory = new InventoryService(db, currency, data);

        var redis = new Mock<IDatabase>();
        redis.Setup(r => r.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        redis.Setup(r => r.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        var mux = new Mock<IConnectionMultiplexer>();
        mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(redis.Object);

        return (new CheatService(db, currency, inventory, data, mux.Object), redis);
    }

    [Fact]
    public async Task Gold_SetAddReduce_ClampsAtZero()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        Assert.Equal(5000, await svc.GoldAsync(7, CheatAction.Set, 5000, "c", Corr, default));
        Assert.Equal(5100, await svc.GoldAsync(7, CheatAction.Add, 100, "c", Corr, default));
        Assert.Equal(0, await svc.GoldAsync(7, CheatAction.Reduce, 999_999, "c", Corr, default));
    }

    [Fact]
    public async Task Gold_Set_ClampsAtMax()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        Assert.Equal(999_999_999, await svc.GoldAsync(8, CheatAction.Set, 5_000_000_000, "c", Corr, default));
    }

    [Fact]
    public async Task Item_SetSingleAndAll()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        var single = await svc.ItemAsync(9, 1, CheatAction.Set, 5, "c", Corr, default);
        Assert.Equal(5, single[1]);

        var all = await svc.ItemAsync(9, null, CheatAction.Set, 3, "c", Corr, default);
        Assert.Equal(3, all[1]);
        Assert.Equal(3, all[2]);
    }

    [Fact]
    public async Task Item_ReduceClampsAtZero()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        await svc.ItemAsync(10, 1, CheatAction.Set, 5, "c", Corr, default);
        var after = await svc.ItemAsync(10, 1, CheatAction.Reduce, 10, "c", Corr, default);
        Assert.Equal(0, after[1]);
    }

    [Fact]
    public async Task Item_UnknownId_Throws()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.ItemAsync(11, 999, CheatAction.Set, 1, "c", Corr, default));
        Assert.Equal(ErrorCodes.ItemNotFound, ex.Code);
    }

    [Fact]
    public async Task Stage_Set_ClearsUpToAndTrimsBeyond()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        Assert.Equal(2, await svc.StageAsync(12, 2, "c", Corr, default));
        var totals = await db.UserRankingTotals.FindAsync(12L);
        Assert.Equal(2, totals!.MaxClearedStageId);
        Assert.Equal(2, totals.TotalClearedStages);

        Assert.Equal(1, await svc.StageAsync(12, 1, "c", Corr, default));
        var progress = await db.UserStageProgress.Query().Where(x => x.UserId == 12).ToListAsync();
        Assert.DoesNotContain(progress, p => p.StageId == 2);
    }

    [Fact]
    public async Task Tutorial_SetClearAndAllTrueUnsupported()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        Assert.Contains(101, await svc.TutorialAsync(13, 101, true, "c", Corr, default));
        Assert.Empty(await svc.TutorialAsync(13, 101, false, "c", Corr, default));

        var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.TutorialAsync(13, null, true, "c", Corr, default));
        Assert.Equal(ErrorCodes.InvalidCommand, ex.Code);
    }

    [Fact]
    public async Task Ad_Bypass_WritesAndDeletesRedisKey()
    {
        using var db = CreateDb();
        var (svc, redis) = NewService(db);

        await svc.AdAsync(14, true, "c", Corr, default);
        Assert.Contains(redis.Invocations, i => i.Method.Name == "StringSetAsync");

        await svc.AdAsync(14, false, "c", Corr, default);
        Assert.Contains(redis.Invocations, i => i.Method.Name == "KeyDeleteAsync");
    }

    [Fact]
    public async Task Cosmetic_UnlockSingleAllAndLock()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        var single = await svc.CosmeticAsync(30, "chip_a", true, "c", Corr, default);
        Assert.Equal(new[] { "chip_a" }, single);

        var all = await svc.CosmeticAsync(30, null, true, "c", Corr, default);
        Assert.Contains("chip_a", all);
        Assert.Contains("chip_b", all);

        var afterLock = await svc.CosmeticAsync(30, "chip_a", false, "c", Corr, default);
        Assert.DoesNotContain("chip_a", afterLock);
    }

    [Fact]
    public async Task Cosmetic_UnknownId_Throws()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.CosmeticAsync(31, "nope", true, "c", Corr, default));
        Assert.Equal(ErrorCodes.CosmeticNotFound, ex.Code);
    }

    [Fact]
    public async Task Achievement_CompleteSingleAllAndReset()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        var single = await svc.AchievementAsync(32, "ach_a", true, "c", Corr, default);
        Assert.True(single["ach_a"]);
        var row = await db.UserAchievements.FindAsync(32L, "ach_a");
        Assert.Equal(10, row!.Progress);

        var all = await svc.AchievementAsync(32, null, true, "c", Corr, default);
        Assert.True(all["ach_b"]);

        var afterReset = await svc.AchievementAsync(32, "ach_a", false, "c", Corr, default);
        Assert.False(afterReset.ContainsKey("ach_a"));
    }

    [Fact]
    public async Task Achievement_UnknownId_Throws()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        var ex = await Assert.ThrowsAsync<GameApiException>(() => svc.AchievementAsync(33, "nope", true, "c", Corr, default));
        Assert.Equal(ErrorCodes.AchievementNotFound, ex.Code);
    }

    [Fact]
    public async Task Attendance_SetDayClampsAndReset()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);

        Assert.Equal(5, await svc.AttendanceAsync(34, 5, "c", Corr, default));
        Assert.Equal(7, await svc.AttendanceAsync(34, 99, "c", Corr, default));

        Assert.Equal(0, await svc.AttendanceAsync(34, null, "c", Corr, default));
        Assert.Null(await db.UserLoginAttendance.FindAsync(34L));
    }

    [Fact]
    public async Task Dispatcher_AttendanceSetDay_AppliesAndReportsSuccess()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);
        var dispatcher = new CheatDispatcher(svc);

        var resp = await dispatcher.DispatchAsync(35, CheatCommandParser.Parse("/attendance setday 3"), Corr, default);

        Assert.True(resp.Success);
        Assert.Equal(3, (await db.UserLoginAttendance.FindAsync(35L))!.CurrentDay);
    }

    [Fact]
    public async Task Dispatcher_GoldCommand_AppliesAndReportsSuccess()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);
        var dispatcher = new CheatDispatcher(svc);

        var resp = await dispatcher.DispatchAsync(20, CheatCommandParser.Parse("/gold set 777"), Corr, default);

        Assert.True(resp.Success);
        var balance = (await db.UserCurrency.FindAsync(20L))!.SoftAmount;
        Assert.Equal(777, balance);
    }

    [Fact]
    public async Task Dispatcher_BadArity_ThrowsInvalidCommand()
    {
        using var db = CreateDb();
        var (svc, _) = NewService(db);
        var dispatcher = new CheatDispatcher(svc);

        var ex = await Assert.ThrowsAsync<GameApiException>(
            () => dispatcher.DispatchAsync(20, CheatCommandParser.Parse("/gold set"), Corr, default));
        Assert.Equal(ErrorCodes.InvalidCommand, ex.Code);
    }
}
