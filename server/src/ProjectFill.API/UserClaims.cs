using System.Security.Claims;

namespace ProjectFill.API;

public static class UserClaims
{
    public const string UserId = "user_id";
    public const string PlatformPid = "sub";

    public static long GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(UserId);
        if (long.TryParse(value, out var userId))
            return userId;

        throw new UnauthorizedAccessException("Missing internal user_id claim.");
    }

    public static string? GetPlatformPid(this ClaimsPrincipal user)
        => user.FindFirstValue(PlatformPid) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
}
