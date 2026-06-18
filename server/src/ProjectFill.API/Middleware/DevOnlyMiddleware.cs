namespace ProjectFill.API.Middleware;

// First gate of the dev-cheat defense: when GAME_ENV != dev, every /api/dev/* route 404s before
// authentication runs, so prod looks like the endpoints don't exist (no auth-challenge leak).
public sealed class DevOnlyMiddleware
{
    private const string DevPathPrefix = "/api/dev";

    private readonly RequestDelegate _next;
    private readonly bool _enabled;

    public DevOnlyMiddleware(RequestDelegate next, ProjectFillConfiguration config)
    {
        _next = next;
        _enabled = config.Dev.Enabled;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled && context.Request.Path.StartsWithSegments(DevPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await _next(context);
    }
}
