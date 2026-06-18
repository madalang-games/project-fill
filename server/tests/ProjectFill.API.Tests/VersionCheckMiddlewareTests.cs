using System;
using Microsoft.AspNetCore.Http;
using ProjectFill.API;
using ProjectFill.API.Middleware;
using Xunit;

namespace ProjectFill.API.Tests;

public sealed class VersionCheckMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncRejectsMissingClientVersion()
    {
        var middleware = new VersionCheckMiddleware(_ => Task.CompletedTask, CreateConfig());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status426UpgradeRequired, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsyncAllowsMatchingVersions()
    {
        var called = false;
        var middleware = new VersionCheckMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, CreateConfig());

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Client-Version"] = "1.0.0";
        context.Request.Headers["X-Protocol-Version"] = "1";

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    private static ProjectFillConfiguration CreateConfig()
        => new()
        {
            GameEnvironment = "test",
            LogLevel = "Information",
            Database = new ProjectFillConfiguration.DatabaseOptions
            {
                Host = "localhost",
                Port = 3306,
                Name = "ProjectFill_test",
                User = "test",
                Password = "test",
            },
            Redis = new ProjectFillConfiguration.RedisOptions
            {
                Host = "localhost",
                Port = 6379,
            },
            Auth = new ProjectFillConfiguration.AuthOptions
            {
                JwtAuthority = "http://platform-auth",
                JwtIssuer = "http://platform-auth",
                JwtAudience = "platform-games",
            },
            AdReward = new ProjectFillConfiguration.AdRewardOptions
            {
                VerifyMode = "mock",
            },
            App = new ProjectFillConfiguration.AppOptions
            {
                ClientId = "project-fill",
                AllowedClientVersion = "1.0.0",
                RequiredClientVersion = "1.0.0",
                AllowedProtocolVersion = "1",
            },
            RateLimit = new ProjectFillConfiguration.RateLimitOptions
            {
                StageStartPerHour = 720,
                TransactionalPerMinute = 60,
            },
            Dev = new ProjectFillConfiguration.DevOptions
            {
                Enabled = false,
                CheatWhitelist = Array.Empty<string>(),
            },
        };
}
