using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Rewards;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/ad")]
[AllowAnonymous]
public sealed class AdSsvCallbackController : ControllerBase
{
    private readonly AdMobSsvCallbackService _callback;

    public AdSsvCallbackController(AdMobSsvCallbackService callback)
        => _callback = callback;

    // Google AdMob SSV callback — always return 200 (non-200 triggers retry).
    [HttpGet("ssv-callback")]
    public async Task<IActionResult> SsvCallback(CancellationToken ct)
    {
        var query = Request.QueryString.Value?.TrimStart('?') ?? string.Empty;
        await _callback.ProcessAsync(query, ct);
        return Ok();
    }
}
