using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFill.API;

namespace ProjectFill.API.Controllers;

[Authorize]
public abstract class ControllerBaseEx : ControllerBase
{
    protected long PlayerId
    {
        get
        {
            var value = User.FindFirstValue(UserClaims.UserId);
            if (long.TryParse(value, out var playerId)) return playerId;
            throw new UnauthorizedAccessException("internal user_id claim is required.");
        }
    }

    protected string CorrelationId
        => HttpContext.Items.TryGetValue("CorrelationId", out var value) && value is string id
            ? id
            : HttpContext.TraceIdentifier;
}
