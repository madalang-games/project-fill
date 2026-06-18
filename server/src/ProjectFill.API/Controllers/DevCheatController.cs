using Microsoft.AspNetCore.Mvc;
using ProjectFill.API.Dev;
using ProjectFill.API.Filters;
using ProjectFill.Contracts.Cheat;

namespace ProjectFill.API.Controllers;

// Dev-only cheat endpoints. Reachable only when DevOnlyMiddleware lets /api/dev/* through
// (GAME_ENV == dev); [Authorize] (via ControllerBaseEx) + CheatWhitelistFilter then apply.
[ApiController]
[Route("api/dev/cheat")]
[ServiceFilter(typeof(CheatWhitelistFilter))]
public sealed class DevCheatController : ControllerBaseEx
{
    private readonly CheatDispatcher _dispatcher;
    private readonly ProjectFillConfiguration _config;

    public DevCheatController(CheatDispatcher dispatcher, ProjectFillConfiguration config)
    {
        _dispatcher = dispatcher;
        _config = config;
    }

    [HttpPost("command")]
    public Task<CheatCommandResponse> Command([FromBody] CheatCommandRequest request, CancellationToken ct)
    {
        var parsed = CheatCommandParser.Parse(request.Command);
        return _dispatcher.DispatchAsync(PlayerId, parsed, CorrelationId, ct);
    }

    [HttpGet("docs")]
    public ContentResult Docs()
        => new()
        {
            Content = CheatDocsPage.Render(_config.GameEnvironment, User.GetPlatformPid()),
            ContentType = "text/html",
            StatusCode = StatusCodes.Status200OK,
        };
}
