using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectFill.API;
using ProjectFill.API.Filters;
using ProjectFill.API.Middleware;
using ProjectFill.Application.Achievement;
using ProjectFill.Application.Attendance;
using ProjectFill.Application.Cosmetic;
using ProjectFill.Application.Currency;
using ProjectFill.Application.DailyChallenge;
using ProjectFill.Application.Inventory;
using ProjectFill.Application.Player;
using ProjectFill.Application.Ranking;
using ProjectFill.Application.Rewards;
using ProjectFill.Application.Stage;
using ProjectFill.Application.Tutorial;
using ProjectFill.Application.Common;
using ProjectFill.Application.Iap;
using ProjectFill.Domain.Interfaces;
using ProjectFill.Domain.Utilities;
using ProjectFill.Infrastructure.Concurrency;
using ProjectFill.Infrastructure.Data;
using ProjectFill.Infrastructure.Generated;
using ProjectFill.Infrastructure.Security;
using ProjectFill.Contracts.Common;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var config    = builder.Configuration;
var appConfig = ProjectFillConfiguration.Load(config);

if (Enum.TryParse<LogEventLevel>(appConfig.LogLevel, ignoreCase: true, out var minLevel))
{
    config["Serilog:MinimumLevel:Default"] = minLevel.ToString();
    config["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command"] = "Warning";
}

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.With<ShortSourceContextEnricher>());

builder.Services.AddControllers(options => options.Filters.AddService<UserSerializeFilter>());
builder.Services.AddEndpointsApiExplorer();

if (builder.Environment.IsDevelopment())
    builder.Services.AddSwaggerGen();

// 1. DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(appConfig.Database.ConnectionString, ServerVersion.AutoDetect(appConfig.Database.ConnectionString)));

// 2. Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(appConfig.Redis.ConnectionString));

// 3. Static data
builder.Services.AddSingleton<IStaticDataService, StaticDataService>();

// 3. Infrastructure and shared services
builder.Services.AddSingleton<UserSerializer>();
builder.Services.AddScoped<UserSerializeFilter>();
builder.Services.AddSingleton(new NicknameGenerator());
builder.Services.AddHttpClient<IPlatformAuthClient, PlatformAuthClient>();
builder.Services.AddHttpClient("jwt-public-key-cache");
builder.Services.AddSingleton(sp => new JwtPublicKeyCache(
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("jwt-public-key-cache")));
builder.Services.AddHostedService(sp => sp.GetRequiredService<JwtPublicKeyCache>());

builder.Services.AddHttpClient("admob-ssv");
builder.Services.AddSingleton(sp => new AdMobSsvKeyCache(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("admob-ssv"),
    sp.GetRequiredService<ILogger<AdMobSsvKeyCache>>()));
builder.Services.AddHostedService(sp => sp.GetRequiredService<AdMobSsvKeyCache>());

// 4. Application services
builder.Services.AddScoped<CurrencyService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<RankingService>();
builder.Services.AddScoped<StageService>();
builder.Services.AddScoped<RewardService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<TutorialService>();
builder.Services.AddScoped<AdRewardService>();
builder.Services.AddScoped<AdMobSsvCallbackService>();
builder.Services.AddScoped<AdInterstitialService>();
builder.Services.AddScoped<AdDoubleRewardService>();
builder.Services.AddScoped<IapService>();
builder.Services.AddScoped<CosmeticService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<AchievementService>();
builder.Services.AddScoped<DailyChallengeService>();
builder.Services.AddHostedService<RankingRebuildHostedService>();
builder.Services.AddSingleton<IAdRewardVerifier, AdMobSsvVerifier>();

// 5. Configuration and auth
builder.Services.AddSingleton(appConfig);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtPublicKeyCache>((options, keyCache) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, kid, _) => keyCache.GetKeysForKid(kid),
            ValidateIssuer = true,
            ValidIssuer = appConfig.Auth.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = appConfig.Auth.JwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

// 6. Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 500,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    options.AddPolicy("stage_start", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirstValue(UserClaims.UserId)
            ?? context.User.GetPlatformPid()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = appConfig.RateLimit.StageStartPerHour,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Code = ErrorCodes.RateLimited,
        }, token);
    };
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, context) =>
    {
        if (context.Items["CorrelationId"] is string correlationId)
            diagnosticContext.Set("CorrelationId", correlationId);
    };
});
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseMiddleware<UserIdResolutionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<VersionCheckMiddleware>();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference(options =>
        options.OpenApiRoutePattern = "/swagger/v1/swagger.json");
}

app.Run();
