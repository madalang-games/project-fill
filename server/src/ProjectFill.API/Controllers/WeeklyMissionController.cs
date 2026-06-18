using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Event;
using ProjectFill.Contracts.Event;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/events/weekly-mission")]
public sealed class WeeklyMissionController : ControllerBaseEx
{
    private readonly WeeklyMissionService _weeklyMission;

    public WeeklyMissionController(WeeklyMissionService weeklyMission)
    {
        _weeklyMission = weeklyMission;
    }

    [HttpGet]
    public Task<WeeklyMissionResponse> Get(CancellationToken ct)
        => _weeklyMission.GetStatusAsync(PlayerId, ct);

    [HttpPost("claim/{threshold:int}")]
    public Task<ClaimWeeklyMissionResponse> Claim(int threshold, CancellationToken ct)
        => _weeklyMission.ClaimAsync(PlayerId, threshold, CorrelationId, ct);
}
