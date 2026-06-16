using ProjectFill.Application.Common;
using ProjectFill.Application.Currency;
using ProjectFill.Application.Logging;
using ProjectFill.Contracts.Currency;
using ProjectFill.Contracts.Rewards;
using ProjectFill.Generated.Data;
using ProjectFill.Infrastructure.Generated;

using ProjectFill.Application.Inventory;

namespace ProjectFill.Application.Rewards;

public sealed class RewardService
{
    private readonly AppDbContext _db;
    private readonly CurrencyService _currency;
    private readonly InventoryService _inventory;
    private readonly Lazy<RewardDataSet> _data;

    public RewardService(AppDbContext db, CurrencyService currency, InventoryService inventory)
    {
        _db = db;
        _currency = currency;
        _inventory = inventory;
        _data = new Lazy<RewardDataSet>(LoadData);
    }

    public async Task<RewardSourcesResponse> GetSourcesAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var result = new RewardSourcesResponse { ServerTime = now };
        foreach (var source in _data.Value.Sources.Where(x => x.is_enabled))
        {
            var periodKey = GetPeriodKey(source, now);
            var state = await _db.UserRewardClaimState.FindAsync(userId, source.source_key, periodKey, ct);
            result.Sources.Add(new RewardSourceDto
            {
                SourceId = source.source_key,
                SourceType = source.source_type,
                RewardGroupId = source.reward_group_id,
                Claimable = state is null || state.ClaimCount < source.max_claims,
                NextAvailableAt = state is null || state.ClaimCount < source.max_claims ? null : GetNextDailyResetUtc(source, now),
                UiSurface = source.ui_surface,
            });
        }

        return result;
    }

    public async Task<RewardClaimResponse> ClaimAsync(long userId, string sourceId, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var source = _data.Value.Sources.FirstOrDefault(x => x.source_key == sourceId && x.is_enabled)
            ?? throw new GameApiException(ErrorCodes.RewardSourceNotFound, "Reward source not found.");

        var periodKey = GetPeriodKey(source, now);
        var state = await _db.UserRewardClaimState.FindAsync(userId, source.source_key, periodKey, ct);
        if (state is not null && state.ClaimCount >= source.max_claims)
            throw new GameApiException(ErrorCodes.RewardAlreadyClaimed, "Reward source already claimed.");

        if (state is null)
        {
            state = new UserRewardClaimStateRow
            {
                UserId = userId,
                SourceId = source.source_key,
                PeriodKey = periodKey,
                ClaimCount = 0,
                LastClaimedAt = now,
                UpdatedAt = now,
            };
            _db.UserRewardClaimState.Insert(state);
        }

        state.ClaimCount++;
        state.LastClaimedAt = now;
        state.UpdatedAt = now;

        var items = _data.Value.Items
            .Where(x => x.reward_group_id == source.reward_group_id && x.version == source.version)
            .OrderBy(x => x.sort_order)
            .ToList();
        var granted = new List<GrantedRewardDto>();
        CurrencySnapshot? currency = null;

        foreach (var item in items)
        {
            granted.Add(new GrantedRewardDto
            {
                RewardType = item.reward_type,
                TargetId = item.target_id,
                Amount = item.amount,
                DurationSeconds = item.duration_seconds,
            });

            if (item.reward_type == "SOFT_CURRENCY")
                currency = await _currency.GrantSoftAsync(userId, item.amount, $"claim:{source.source_key}", correlationId, ct);
            else if (item.reward_type == "ITEM")
                await _inventory.GrantItemAsync(userId, item.target_id, item.amount, $"claim:{source.source_key}", correlationId, ct);
            else if (item.reward_type == "AVATAR")
                await GrantAvatarAsync(userId, item.target_id, ct);
            else if (item.reward_type == "NO_ADS")
            {
                var player = await _db.Players.FindAsync(userId, ct);
                if (player is not null) player.IsNoAds = true;
            }
        }

        _db.EventLogs.Insert(EventLogFactory.RewardClaimed(userId, correlationId, source.source_key, source.reward_group_id));
        await _db.SaveAsync(ct);

        return new RewardClaimResponse
        {
            SourceId = source.source_key,
            GrantedRewards = granted,
            Currency = currency,
            ServerTime = now,
        };
    }

    public async Task<(List<GrantedRewardDto> Granted, CurrencySnapshot? Currency)> GrantRewardGroupAsync(
        long userId,
        int rewardGroupId,
        int version,
        string correlationId,
        CancellationToken ct)
    {
        var items = _data.Value.Items
            .Where(x => x.reward_group_id == rewardGroupId && x.version == version)
            .OrderBy(x => x.sort_order)
            .ToList();

        var granted = new List<GrantedRewardDto>();
        CurrencySnapshot? currency = null;

        foreach (var item in items)
        {
            granted.Add(new GrantedRewardDto
            {
                RewardType = item.reward_type,
                TargetId = item.target_id,
                Amount = item.amount,
                DurationSeconds = item.duration_seconds,
            });

            switch (item.reward_type)
            {
                case "SOFT_CURRENCY":
                    currency = await _currency.GrantSoftAsync(userId, item.amount, $"reward_group:{rewardGroupId}", correlationId, ct);
                    break;
                case "ITEM":
                    await _inventory.GrantItemAsync(userId, item.target_id, item.amount, $"reward_group:{rewardGroupId}", correlationId, ct);
                    break;
                case "AVATAR":
                    await GrantAvatarAsync(userId, item.target_id, ct);
                    break;
                case "NO_ADS":
                    var player = await _db.Players.FindAsync(userId, ct);
                    if (player is not null) player.IsNoAds = true;
                    break;
            }
        }

        return (granted, currency);
    }

    private async Task GrantAvatarAsync(long userId, int avatarId, CancellationToken ct)
    {
        var sourceId = $"avatar_unlock:{avatarId}";
        var existing = await _db.UserRewardClaimState.FindAsync(userId, sourceId, "once", ct);
        if (existing is not null) return;

        var now = DateTimeOffset.UtcNow;
        _db.UserRewardClaimState.Insert(new UserRewardClaimStateRow
        {
            UserId = userId,
            SourceId = sourceId,
            PeriodKey = "once",
            ClaimCount = 1,
            LastClaimedAt = now,
            UpdatedAt = now,
        });
    }

    private static RewardDataSet LoadData()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "generated", "data", "reward");
        return new RewardDataSet(
            RewardSourceLoader.LoadAll(Path.Combine(root, "reward_source.csv")),
            RewardItemLoader.LoadAll(Path.Combine(root, "reward_item.csv")));
    }

    private static string GetPeriodKey(RewardSource source, DateTimeOffset utcNow)
        => source.claim_policy == "DAILY_RESET"
            ? ToKst(utcNow).ToString("yyyy-MM-dd")
            : "always";

    private static DateTimeOffset GetNextDailyResetUtc(RewardSource source, DateTimeOffset utcNow)
    {
        var kst = ToKst(utcNow);
        var next = new DateTimeOffset(kst.Year, kst.Month, kst.Day, source.reset_hour, 0, 0, kst.Offset);
        if (next <= kst) next = next.AddDays(1);
        return next.ToUniversalTime();
    }

    private static DateTimeOffset ToKst(DateTimeOffset utc)
        => TimeZoneInfo.ConvertTime(utc, GetKst());

    private static TimeZoneInfo GetKst()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"); }
    }

    private sealed record RewardDataSet(IReadOnlyList<RewardSource> Sources, IReadOnlyList<RewardItem> Items);
}
