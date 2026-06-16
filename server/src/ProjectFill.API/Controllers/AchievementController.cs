using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Achievement;
using ProjectFill.Contracts.Achievement;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/achievements")]
public sealed class AchievementController : ControllerBaseEx
{
    private readonly AchievementService _achievements;

    public AchievementController(AchievementService achievements)
    {
        _achievements = achievements;
    }

    [HttpGet]
    public Task<AchievementListResponse> Get(CancellationToken ct)
        => _achievements.GetListAsync(PlayerId, ct);

    [HttpPost("{id}/claim")]
    public Task<ClaimAchievementResponse> Claim(string id, CancellationToken ct)
        => _achievements.ClaimAsync(PlayerId, id, CorrelationId, ct);
}
