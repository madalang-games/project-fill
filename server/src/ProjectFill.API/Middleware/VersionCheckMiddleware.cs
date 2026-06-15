using ProjectFill.API;
using ProjectFill.Application.Common;
using ProjectFill.Contracts.Common;

namespace ProjectFill.API.Middleware;

public sealed class VersionCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _requiredClient;
    private readonly string _allowedProtocol;

    public VersionCheckMiddleware(RequestDelegate next, ProjectFillConfiguration config)
    {
        _next = next;
        _requiredClient = config.App.RequiredClientVersion;
        _allowedProtocol = config.App.AllowedProtocolVersion;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/ad/ssv-callback", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var clientVersion = context.Request.Headers["X-Client-Version"].FirstOrDefault() ?? "";
        var protocolVersion = context.Request.Headers["X-Protocol-Version"].FirstOrDefault() ?? "";

        if (IsVersionLower(clientVersion, _requiredClient))
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = ErrorCodes.VersionMismatch });
            return;
        }

        if (protocolVersion != _allowedProtocol)
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = ErrorCodes.ProtocolMismatch });
            return;
        }

        await _next(context);
    }

    private static bool IsVersionLower(string current, string required)
    {
        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(required))
            return true;

        if (!Version.TryParse(current, out var cur) || !Version.TryParse(required, out var req))
            return current != required;

        return cur.Major < req.Major || (cur.Major == req.Major && cur.Minor < req.Minor);
    }
}
