using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.Player;
using ProjectFill.Domain.Enums;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Player;

public sealed class PlayerService
{
    private readonly AppDbContext _db;
    private readonly IStaticDataService _staticData;
    private readonly CurrencyService _currency;

    public PlayerService(AppDbContext db, IStaticDataService staticData, CurrencyService currency)
    {
        _db = db;
        _staticData = staticData;
        _currency = currency;
    }

    public async Task<PlayerProgressResponse> GetProgressAsync(long userId, CancellationToken ct)
    {
        var player = await _db.Players.FindAsync(userId, ct);

        var claims = await _db.UserRewardClaimState.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.SourceId)
            .ToListAsync(ct);

        var unlockedAvatarIds = new List<int>();

        foreach (var sourceId in claims)
        {
            if (sourceId.StartsWith(AvatarClaimKeys.UnlockPrefix))
            {
                if (int.TryParse(sourceId.Substring(AvatarClaimKeys.UnlockPrefix.Length), out var avatarId))
                {
                    unlockedAvatarIds.Add(avatarId);
                }
            }
        }

        return new PlayerProgressResponse
        {
            UnlockedAvatarIds = unlockedAvatarIds,
            IsNoAds = player?.IsNoAds ?? false
        };
    }

    public async Task<UserProfileUpdateResponse> UpdateProfileAsync(
        long userId,
        UserProfileUpdateRequest request,
        string correlationId,
        CancellationToken ct)
    {
        var player = await _db.Players.FindAsync(userId, ct);
        if (player == null)
            throw new GameApiException(ErrorCodes.PlayerNotFound, "Player not found.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        long totalSpent = 0;

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            var cleanName = request.DisplayName.Trim();
            if (cleanName.Length is < 2 or > 24)
                throw new GameApiException(ErrorCodes.InvalidDisplayNameLength, "Display name must be between 2 and 24 characters.");

            foreach (char c in cleanName)
            {
                if (!((c >= 'a' && c <= 'z') ||
                      (c >= 'A' && c <= 'Z') ||
                      (c >= '0' && c <= '9') ||
                      c == ' ' || c == '_' || c == '-'))
                {
                    throw new GameApiException(ErrorCodes.InvalidDisplayNameChar, "Display name contains invalid characters.");
                }
            }

            player.DisplayName = cleanName;
        }

        if (request.AvatarId.HasValue)
        {
            var avatarId = request.AvatarId.Value;
            if (avatarId != player.AvatarId)
            {
                var avatarData = _staticData.GetAvatar(avatarId);
                if (avatarData == null)
                    throw new GameApiException(ErrorCodes.AvatarNotFound, "Avatar not found.");

                if (avatarData.UnlockCost > 0)
                {
                    var claimKey = $"{AvatarClaimKeys.UnlockPrefix}{avatarId}";
                    var isUnlocked = await _db.UserRewardClaimState.Query()
                        .AnyAsync(x => x.UserId == userId && x.SourceId == claimKey, ct);

                    if (!isUnlocked)
                    {
                        totalSpent += avatarData.UnlockCost;
                        await _currency.SpendSoftAsync(userId, avatarData.UnlockCost, "avatar_unlock", correlationId, ct);

                        _db.UserRewardClaimState.Insert(new UserRewardClaimStateRow
                        {
                            UserId = userId,
                            SourceId = claimKey,
                            PeriodKey = "once",
                            ClaimCount = 1,
                            LastClaimedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
                        });
                    }
                }
                else if (avatarData.UnlockType == AvatarUnlockTypes.Achievement)
                {
                    var claimKey = $"{AvatarClaimKeys.UnlockPrefix}{avatarId}";
                    var isUnlocked = await _db.UserRewardClaimState.Query()
                        .AnyAsync(x => x.UserId == userId && x.SourceId == claimKey, ct);

                    if (!isUnlocked)
                        throw new GameApiException(ErrorCodes.AvatarLocked, "This avatar must be unlocked via achievements.");
                }

                player.AvatarId = avatarId;
            }
        }

        player.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveAsync(ct);

        var balanceSnapshot = await _currency.GetAsync(userId, ct);
        await tx.CommitAsync(ct);

        return new UserProfileUpdateResponse
        {
            DisplayName = player.DisplayName,
            AvatarId = player.AvatarId,
            Currency = new CurrencySnapshot { SoftAmount = balanceSnapshot.SoftAmount, SoftDelta = -totalSpent }
        };
    }
}
