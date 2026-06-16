using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.DailyChallenge;
using ProjectFill.Contracts.DailyChallenge;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/daily-challenge")]
public sealed class DailyChallengeController : ControllerBaseEx
{
    private readonly DailyChallengeService _challenge;

    public DailyChallengeController(DailyChallengeService challenge)
    {
        _challenge = challenge;
    }

    [HttpGet("today")]
    public Task<DailyChallengeTodayResponse> Today(CancellationToken ct)
        => _challenge.GetTodayAsync(PlayerId, ct);

    [HttpPost("today/attempt")]
    public Task<DailyChallengeTodayResponse> Attempt(CancellationToken ct)
        => _challenge.GetTodayAsync(PlayerId, ct);

    [HttpPost("today/clear")]
    public Task<SubmitChallengeClearResponse> Clear([FromBody] SubmitChallengeClearRequest request, CancellationToken ct)
        => _challenge.SubmitClearAsync(PlayerId, request.MovesUsed, request.ClearTimeSeconds, CorrelationId, ct);

    [HttpGet("today/ranking")]
    public Task<ChallengeRankingResponse> Ranking([FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
        => _challenge.GetRankingAsync(PlayerId, page, pageSize, ct);

    [HttpGet("today/me")]
    public Task<DailyChallengeTodayResponse> Me(CancellationToken ct)
        => _challenge.GetTodayAsync(PlayerId, ct);

    [HttpGet("streak")]
    public Task<ChallengeStreakResponse> Streak(CancellationToken ct)
        => _challenge.GetStreakAsync(PlayerId, ct);
}
