using ProjectFill.Application.Logging;
using ProjectFill.Contracts.Ad;
using ProjectFill.Domain.Enums;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Domain.StaticData;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Stage;

public sealed class AdInterstitialService
{
    private readonly AppDbContext _db;
    private readonly AdPlacementData? _interstitialPlacement;

    public AdInterstitialService(AppDbContext db, IStaticDataService staticData)
    {
        _db = db;
        _interstitialPlacement = staticData.GetAllAdPlacements()
            .FirstOrDefault(x => x.PlacementKey == AdPlacementKeys.InterstitialPostStage);
    }

    public async Task<AdEligibilityResponse> GetEligibilityAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var state = await _db.UserInterstitialState.FindAsync(userId, ct);

        int cooldown = _interstitialPlacement?.CooldownSeconds ?? 180;
        bool isEligible = state is null
            || (now - state.LastShownAt).TotalSeconds >= cooldown;
        int remaining = (isEligible || state is null) ? 0 : (int)(cooldown - (now - state.LastShownAt).TotalSeconds);

        return new AdEligibilityResponse
        {
            Placements = new List<AdPlacementStatus>
            {
                new()
                {
                    PlacementId = AdPlacementKeys.InterstitialPostStage,
                    IsEligible = isEligible,
                    CooldownRemainingSeconds = Math.Max(0, remaining),
                },
            },
            ServerTime = now,
        };
    }

    public async Task<AdInterstitialShownResponse> RecordShownAsync(long userId, int stageId, string correlationId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var row = await _db.UserInterstitialState.FindAsync(userId, ct);
        if (row is null)
        {
            row = new UserInterstitialStateRow { UserId = userId, LastShownAt = now, UpdatedAt = now };
            _db.UserInterstitialState.Insert(row);
        }
        else
        {
            row.LastShownAt = now;
            row.UpdatedAt = now;
        }

        _db.EventLogs.Insert(EventLogFactory.AdInterstitialShown(userId, correlationId, stageId));
        await _db.SaveAsync(ct);

        return new AdInterstitialShownResponse { ServerTime = now };
    }
}
