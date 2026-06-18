using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectFill.Application.Common;
using ProjectFill.Contracts.Common;

namespace ProjectFill.API.Filters;

// Second gate (after env + auth): only whitelisted platform PIDs may run cheats. An empty whitelist
// allows all (local dev convenience); a populated one locks shared dev servers. Unlisted → 403.
public sealed class CheatWhitelistFilter : IAsyncActionFilter
{
    private readonly IReadOnlyList<string> _whitelist;

    public CheatWhitelistFilter(ProjectFillConfiguration config)
    {
        _whitelist = config.Dev.CheatWhitelist;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_whitelist.Count > 0)
        {
            var pid = context.HttpContext.User.GetPlatformPid();
            if (pid is null || !_whitelist.Contains(pid))
            {
                context.Result = new ObjectResult(new ErrorResponse { Code = ErrorCodes.Forbidden })
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
                return;
            }
        }

        await next();
    }
}
