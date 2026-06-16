using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Player;
using ProjectFill.Contracts.Player;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;
using ProjectFill.Application.Common;
using Xunit;

namespace ProjectFill.API.Tests
{
    public sealed class PlayerServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        private sealed class FakeStaticDataService : FakeStaticData
        {
            private readonly Dictionary<int, AvatarData> _avatars = new()
            {
                { 1, new AvatarData { Id = 1, ResourceName = "avatar_free_01", UnlockCost = 0, UnlockType = "free" } },
                { 2, new AvatarData { Id = 2, ResourceName = "avatar_gold_01", UnlockCost = 500, UnlockType = "gold" } },
                { 3, new AvatarData { Id = 3, ResourceName = "avatar_silver_01", UnlockCost = 250, UnlockType = "silver" } },
                { 4, new AvatarData { Id = 4, ResourceName = "avatar_achievement_01", UnlockCost = 0, UnlockType = "achievement" } }
            };

            public override AvatarData? GetAvatar(int avatar_id) => _avatars.GetValueOrDefault(avatar_id);
            public override IReadOnlyList<AvatarData> GetAllAvatars() => new List<AvatarData>(_avatars.Values);
        }

        [Fact]
        public async Task UpdateProfile_ValidNameAndFreeAvatar_Succeeds()
        {
            using var db = CreateDbContext();
            var staticData = new FakeStaticDataService();
            var service = new PlayerService(db, staticData, new CurrencyService(db));

            long userId = 1;
            db.Players.Insert(new PlayersRow
            {
                UserId = userId,
                PlatformPid = "pid1",
                DisplayName = "OldName",
                AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            });
            await db.SaveAsync();

            var req = new UserProfileUpdateRequest
            {
                DisplayName = "Valid_Name-123",
                AvatarId = 1
            };

            var res = await service.UpdateProfileAsync(userId, req, "corr-id", default);
            Assert.Equal("Valid_Name-123", res.DisplayName);
            Assert.Equal(1, res.AvatarId);

            var player = await db.Players.FindAsync(userId);
            Assert.Equal("Valid_Name-123", player?.DisplayName);
        }

        [Fact]
        public async Task UpdateProfile_InvalidNicknameChars_ThrowsException()
        {
            using var db = CreateDbContext();
            var staticData = new FakeStaticDataService();
            var service = new PlayerService(db, staticData, new CurrencyService(db));

            long userId = 1;
            db.Players.Insert(new PlayersRow
            {
                UserId = userId,
                PlatformPid = "pid1",
                DisplayName = "OldName",
                AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            });
            await db.SaveAsync();

            var req = new UserProfileUpdateRequest
            {
                DisplayName = "NameWith한글"
            };

            var ex = await Assert.ThrowsAsync<GameApiException>(() => service.UpdateProfileAsync(userId, req, "corr-id", default));
            Assert.Equal("INVALID_DISPLAY_NAME", ex.Code);
        }

        [Fact]
        public async Task UpdateProfile_GoldAvatarUnlock_DeductsGoldAndSucceeds()
        {
            using var db = CreateDbContext();
            var staticData = new FakeStaticDataService();
            var service = new PlayerService(db, staticData, new CurrencyService(db));

            long userId = 1;
            db.Players.Insert(new PlayersRow
            {
                UserId = userId,
                PlatformPid = "pid1",
                DisplayName = "PlayerName",
                AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            });
            db.UserCurrency.Insert(new UserCurrencyRow
            {
                UserId = userId,
                SoftAmount = 1000,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveAsync();

            var req = new UserProfileUpdateRequest { AvatarId = 2 }; // gold cost 500
            var res = await service.UpdateProfileAsync(userId, req, "corr-id", default);

            Assert.Equal(2, res.AvatarId);

            var currency = await db.UserCurrency.FindAsync(userId);
            Assert.Equal(500, currency?.SoftAmount);

            var isUnlocked = await db.UserRewardClaimState.Query()
                .AnyAsync(x => x.UserId == userId && x.SourceId == "avatar_unlock:2");
            Assert.True(isUnlocked);
        }

        [Fact]
        public async Task UpdateProfile_SilverAvatarUnlock_DeductsGoldAndSucceeds()
        {
            using var db = CreateDbContext();
            var staticData = new FakeStaticDataService();
            var service = new PlayerService(db, staticData, new CurrencyService(db));

            long userId = 1;
            db.Players.Insert(new PlayersRow
            {
                UserId = userId,
                PlatformPid = "pid1",
                DisplayName = "PlayerName",
                AvatarId = 1,
                AccountCreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            });
            db.UserCurrency.Insert(new UserCurrencyRow
            {
                UserId = userId,
                SoftAmount = 1000,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveAsync();

            var req = new UserProfileUpdateRequest { AvatarId = 3 }; // silver cost 250
            var res = await service.UpdateProfileAsync(userId, req, "corr-id", default);

            Assert.Equal(3, res.AvatarId);

            var currency = await db.UserCurrency.FindAsync(userId);
            Assert.Equal(750, currency?.SoftAmount);

            var isUnlocked = await db.UserRewardClaimState.Query()
                .AnyAsync(x => x.UserId == userId && x.SourceId == "avatar_unlock:3");
            Assert.True(isUnlocked);
        }

        [Fact]
        public async Task GetProgress_IncludesUnlockedAvatars()
        {
            using var db = CreateDbContext();
            var staticData = new FakeStaticDataService();
            var service = new PlayerService(db, staticData, new CurrencyService(db));

            long userId = 1;
            db.UserRewardClaimState.Insert(new UserRewardClaimStateRow
            {
                UserId = userId,
                SourceId = "avatar_unlock:2",
                PeriodKey = "once",
                ClaimCount = 1,
                LastClaimedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            db.UserRewardClaimState.Insert(new UserRewardClaimStateRow
            {
                UserId = userId,
                SourceId = "avatar_unlock:3",
                PeriodKey = "once",
                ClaimCount = 1,
                LastClaimedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveAsync();

            var progress = await service.GetProgressAsync(userId, default);
            Assert.Equal(2, progress.UnlockedAvatarIds.Count);
            Assert.Contains(2, progress.UnlockedAvatarIds);
            Assert.Contains(3, progress.UnlockedAvatarIds);
        }
    }
}
