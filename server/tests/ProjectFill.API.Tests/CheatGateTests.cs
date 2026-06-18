using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using ProjectFill.API.Filters;
using ProjectFill.API.Middleware;
using ProjectFill.Application.Common;
using Xunit;

namespace ProjectFill.API.Tests;

public sealed class CheatGateTests
{
    // --- CheatWhitelistFilter ---

    [Fact]
    public async Task Whitelist_Empty_AllowsAnyone()
    {
        var (ctx, next, wasCalled) = MakeActionContext(pid: "anyone");
        var filter = new CheatWhitelistFilter(TestConfig.Create(true, Array.Empty<string>()));

        await filter.OnActionExecutionAsync(ctx, next);

        Assert.True(wasCalled());
        Assert.Null(ctx.Result);
    }

    [Fact]
    public async Task Whitelist_Populated_RejectsUnlistedPidWith403()
    {
        var (ctx, next, wasCalled) = MakeActionContext(pid: "intruder");
        var filter = new CheatWhitelistFilter(TestConfig.Create(true, new[] { "owner" }));

        await filter.OnActionExecutionAsync(ctx, next);

        Assert.False(wasCalled());
        var result = Assert.IsType<ObjectResult>(ctx.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Whitelist_Populated_AllowsListedPid()
    {
        var (ctx, next, wasCalled) = MakeActionContext(pid: "owner");
        var filter = new CheatWhitelistFilter(TestConfig.Create(true, new[] { "owner" }));

        await filter.OnActionExecutionAsync(ctx, next);

        Assert.True(wasCalled());
        Assert.Null(ctx.Result);
    }

    // --- DevOnlyMiddleware ---

    [Fact]
    public async Task DevOnly_Disabled_DevPath_Returns404AndShortCircuits()
    {
        var called = false;
        var mw = new DevOnlyMiddleware(_ => { called = true; return Task.CompletedTask; }, TestConfig.Create(false, Array.Empty<string>()));
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/dev/cheat/command";

        await mw.InvokeAsync(ctx);

        Assert.Equal(StatusCodes.Status404NotFound, ctx.Response.StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task DevOnly_Disabled_NonDevPath_PassesThrough()
    {
        var called = false;
        var mw = new DevOnlyMiddleware(_ => { called = true; return Task.CompletedTask; }, TestConfig.Create(false, Array.Empty<string>()));
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/currency";

        await mw.InvokeAsync(ctx);

        Assert.True(called);
    }

    [Fact]
    public async Task DevOnly_Enabled_DevPath_PassesThrough()
    {
        var called = false;
        var mw = new DevOnlyMiddleware(_ => { called = true; return Task.CompletedTask; }, TestConfig.Create(true, Array.Empty<string>()));
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/dev/cheat/command";

        await mw.InvokeAsync(ctx);

        Assert.True(called);
    }

    private static (ActionExecutingContext ctx, ActionExecutionDelegate next, Func<bool> wasCalled) MakeActionContext(string pid)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", pid) }, "test")),
        };
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: null!);

        var called = false;
        ActionExecutionDelegate next = () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller: null!));
        };
        return (ctx, next, () => called);
    }
}
