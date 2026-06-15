using ProjectFill.Application.Logging;
using ProjectFill.Contracts.Ad;
using ProjectFill.Generated.Data;
using ProjectFill.Infrastructure.Generated;

namespace ProjectFill.Application.Stage;

public sealed class AdInterstitialService
{
    private readonly AppDbContext _db;
    private readonly Lazy<AdPlacement?> _interstitialPlacement;

    public AdInterstitialService(AppDbContext db)
    {
        _db = db;
        _interstitialPlacement = new Lazy<AdPlacement?>(() =>
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "generated", "data", "ad", "ad_placement.csv");
            return AdPlacementLoader.LoadAll(path).FirstOrDefault(x => x.placement_key == "INTERSTITIAL_POST_STAGE");
        });
    }

    public async Task<AdEligibilityResponse> GetEligibilityAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var placement = _interstitialPlacement.Value;
        var state = await _db.UserInterstitialState.FindAsync(userId, ct);

        int cooldown = placement?.cooldown_seconds ?? 180;
        bool isEligible = state is null
            || (now - state.LastShownAt).TotalSeconds >= cooldown;
        int remaining = (isEligible || state is null) ? 0 : (int)(cooldown - (now - state.LastShownAt).TotalSeconds);

        return new AdEligibilityResponse
        {
            Placements = new List<AdPlacementStatus>
            {
                new()
                {
                    PlacementId = "INTERSTITIAL_POST_STAGE",
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
