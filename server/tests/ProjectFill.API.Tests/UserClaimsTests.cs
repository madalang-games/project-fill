using System.Security.Claims;
using ProjectFill.API;
using Xunit;

namespace ProjectFill.API.Tests;

public sealed class UserClaimsTests
{
    [Fact]
    public void GetUserIdReadsInternalUserIdClaimOnly()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(UserClaims.PlatformPid, "ABCDEF123456ABCDEF123456"),
            new Claim(UserClaims.UserId, "12345"),
        ], "test"));

        Assert.Equal(12345, principal.GetUserId());
        Assert.Equal("ABCDEF123456ABCDEF123456", principal.GetPlatformPid());
    }

    [Fact]
    public void GetUserIdDoesNotParsePlatformPidAsUid()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(UserClaims.PlatformPid, "12345"),
        ], "test"));

        Assert.Throws<UnauthorizedAccessException>(() => principal.GetUserId());
    }
}
