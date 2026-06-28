using System;
using ProjectFill.API;

namespace ProjectFill.API.Tests;

// Builds a fully-populated ProjectFillConfiguration for filter/middleware tests, varying only the
// dev-cheat gating knobs.
internal static class TestConfig
{
    public static ProjectFillConfiguration Create(bool devEnabled, string[] whitelist)
        => new()
        {
            GameEnvironment = devEnabled ? "dev" : "prod",
            LogLevel = "Information",
            Database = new ProjectFillConfiguration.DatabaseOptions
            {
                Host = "localhost",
                Port = 3306,
                Name = "ProjectFill_test",
                User = "test",
                Password = "test",
            },
            Redis = new ProjectFillConfiguration.RedisOptions { Host = "localhost", Port = 6379 },
            Auth = new ProjectFillConfiguration.AuthOptions
            {
                JwtAuthority = "http://platform-auth",
                JwtIssuer = "http://platform-auth",
                JwtAudience = "platform-games",
            },
            AdReward = new ProjectFillConfiguration.AdRewardOptions { VerifyMode = "mock" },
            GooglePlay = new ProjectFillConfiguration.GooglePlayOptions { PackageName = "", ServiceAccountJson = "" },
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
                Enabled = devEnabled,
                CheatWhitelist = whitelist,
            },
        };
}
