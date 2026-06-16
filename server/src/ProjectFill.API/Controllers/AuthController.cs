using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Contracts.Account;
using ProjectFill.Application.Common;
using ProjectFill.Contracts.Common;
using ProjectFill.Domain.Utilities;
using ProjectFill.Infrastructure.Generated;
using StackExchange.Redis;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30);

    private readonly ProjectFillConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly AppDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly NicknameGenerator _nicknameGenerator;

    public AuthController(
        ProjectFillConfiguration config,
        IHttpClientFactory httpFactory,
        AppDbContext db,
        IConnectionMultiplexer redis,
        NicknameGenerator nicknameGenerator)
    {
        _config = config;
        _httpFactory = httpFactory;
        _db = db;
        _redis = redis;
        _nicknameGenerator = nicknameGenerator;
    }

    [AllowAnonymous]
    [HttpPost("guest")]
    public Task<IActionResult> Guest(CancellationToken ct)
        => ProxyAndTransform("auth/guest", ct);

    [AllowAnonymous]
    [HttpPost("google")]
    public Task<IActionResult> Google(CancellationToken ct)
        => ProxyAndTransform("auth/google", ct);

    [AllowAnonymous]
    [HttpPost("refresh")]
    public Task<IActionResult> Refresh(CancellationToken ct)
        => ProxyAndTransform("auth/refresh", ct);

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var authority = _config.Auth.JwtAuthority.TrimEnd('/');
        var http = _httpFactory.CreateClient();
        
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync(ct);
        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        
        var response = await http.PostAsync($"{authority}/auth/logout", content, ct);
        if (response.IsSuccessStatusCode)
            return NoContent();
            
        var errorBody = await response.Content.ReadAsStringAsync(ct);
        return StatusCode((int)response.StatusCode, errorBody);
    }

    [HttpPost("link-oauth")]
    public async Task<IActionResult> LinkOauth([FromBody] LinkAccountRequest request, CancellationToken ct)
    {
        var authority = _config.Auth.JwtAuthority.TrimEnd('/');
        var http = _httpFactory.CreateClient();

        var payload = new
        {
            provider = request.Provider,
            idToken = request.IdToken,
            clientId = "game_client",
            guestRefreshToken = request.GuestRefreshToken
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await http.PostAsync($"{authority}/auth/{request.Provider}", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, responseBody);

        return await ProcessPlatformSessionAsync(responseBody, ct, wrapInLinkResponse: true);
    }

    [AllowAnonymous]
    [HttpPost("resolve-conflict")]
    public async Task<IActionResult> ResolveConflict([FromBody] ResolveConflictRequest request, CancellationToken ct)
    {
        var redisDb = _redis.GetDatabase();
        var cached = await redisDb.StringGetAsync($"pending_conflict:{request.ConflictToken}");
        if (!cached.HasValue)
            return BadRequest(new ErrorResponse { Code = ErrorCodes.ConflictExpired });

        var conflict = JsonSerializer.Deserialize<PendingConflictDetails>(cached!, JsonOpts);
        if (conflict is null)
            return BadRequest(new ErrorResponse { Code = ErrorCodes.InvalidConflict });

        await redisDb.KeyDeleteAsync($"pending_conflict:{request.ConflictToken}");

        long winnerUserId;

        if (request.Selection == "local")
        {
            // Keep guest data: archive cloud player, reassign cloud PID to guest player
            await using var txn = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE players SET is_active=0, platform_pid=NULL, conflict_cloud_pid={0} WHERE user_id={1}",
                    new object[] { conflict.CloudPid, conflict.CloudUserId }, ct);

                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE players SET platform_pid={0} WHERE user_id={1}",
                    new object[] { conflict.CloudPid, conflict.GuestUserId }, ct);

                await txn.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await txn.RollbackAsync(ct);
                return StatusCode(500, $"Failed to resolve conflict (local): {ex.Message} | inner: {ex.InnerException?.Message}");
            }

            // Invalidate stale Redis cache so cloudPid resolves to guestUserId immediately
            await redisDb.StringSetAsync($"user_id:{conflict.CloudPid}", conflict.GuestUserId.ToString(), CacheTtl);

            winnerUserId = conflict.GuestUserId;
        }
        else if (request.Selection == "cloud")
        {
            // Keep cloud data: archive guest player
            await using var txn = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE players SET is_active=0, platform_pid=NULL, conflict_cloud_pid={0} WHERE user_id={1}",
                    new object[] { conflict.CloudPid, conflict.GuestUserId }, ct);

                await txn.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await txn.RollbackAsync(ct);
                return StatusCode(500, $"Failed to resolve conflict (cloud): {ex.Message} | inner: {ex.InnerException?.Message}");
            }

            winnerUserId = conflict.CloudUserId;
        }
        else
        {
            return BadRequest(new ErrorResponse { Code = ErrorCodes.InvalidSelection });
        }

        // Return AuthResponse using saved PlatformSession
        var session = JsonSerializer.Deserialize<PlatformSession>(conflict.PlatformSession, JsonOpts);
        if (session is null)
            return StatusCode(500, "Failed to restore platform session.");

        var player = await _db.Players.FindAsync(winnerUserId, ct);
        var authRes = new AuthResponse
        {
            AccessToken = session.Tokens.AccessToken,
            RefreshToken = session.Tokens.RefreshToken,
            ExpiresAt = session.Tokens.AccessTokenExpiresAt,
            Profile = new AccountMeResponse
            {
                UserId = winnerUserId.ToString(),
                Pid = session.Pid,
                DisplayName = player?.DisplayName ?? "Player",
                IsGuest = false,
                LinkedProviders = new List<string> { session.AccountType },
                AvatarId = player?.AvatarId ?? 1,
                CreatedAt = (player?.AccountCreatedAt ?? DateTimeOffset.UtcNow).ToString("O")
            }
        };

        return Ok(new ResolveConflictResponse { Success = true, Auth = authRes });
    }

    private async Task<IActionResult> ProxyAndTransform(string platformPath, CancellationToken ct)
    {
        var authority = _config.Auth.JwtAuthority.TrimEnd('/');
        var http = _httpFactory.CreateClient();
        
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync(ct);
        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        
        var response = await http.PostAsync($"{authority}/{platformPath}", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, responseBody);

        return await ProcessPlatformSessionAsync(responseBody, ct);
    }

    private async Task<IActionResult> ProcessPlatformSessionAsync(string responseBody, CancellationToken ct, bool wrapInLinkResponse = false)
    {
        try
        {
            var session = JsonSerializer.Deserialize<PlatformSession>(responseBody, JsonOpts);
            if (session is null || string.IsNullOrWhiteSpace(session.Pid))
                return StatusCode(500, "Invalid platform session.");

            // Check if client is currently authenticated as guest (linking flow)
            long? guestUserId = null;
            var uidClaim = HttpContext.User.FindFirst(UserClaims.UserId);
            if (uidClaim != null && long.TryParse(uidClaim.Value, out var uid))
            {
                guestUserId = uid;
            }

            // Find if there is an existing active player mapping to this PlatformPid in our DB
            var existing = await _db.Players.Query()
                .Where(p => p.PlatformPid == session.Pid)
                .FirstOrDefaultAsync(ct);

            long resolvedUserId = existing?.UserId ?? session.UserId;

            // Conflict condition: authenticated as guest, but the linked social account userId is different
            if (guestUserId.HasValue && resolvedUserId != guestUserId.Value)
            {
                var localSave = await GetSaveSnapshotAsync(guestUserId.Value, ct);
                var cloudSave = await GetSaveSnapshotAsync(resolvedUserId, ct);
                var conflictToken = Guid.NewGuid().ToString("N");

                var conflictDetails = new PendingConflictDetails
                {
                    GuestUserId = guestUserId.Value,
                    CloudUserId = resolvedUserId,
                    CloudPid = session.Pid,
                    PlatformSession = responseBody
                };

                await _redis.GetDatabase().StringSetAsync(
                    $"pending_conflict:{conflictToken}",
                    JsonSerializer.Serialize(conflictDetails, JsonOpts),
                    TimeSpan.FromMinutes(10));

                return Ok(new LinkAccountResponse
                {
                    Success = false,
                    Conflict = true,
                    LocalSave = localSave,
                    CloudSave = cloudSave,
                    ConflictToken = conflictToken
                });
            }

            var now = DateTimeOffset.UtcNow;
            string displayName;
            if (existing is null)
            {
                displayName = _nicknameGenerator.Generate();
                await _db.Players.InsertIgnoreAsync(new PlayersRow
                {
                    UserId = session.UserId,
                    PlatformPid = session.Pid,
                    DisplayName = displayName,
                    AvatarId = 1,
                    AccountCreatedAt = now,
                    LastLoginAt = now,
                }, ct);
            }
            else
            {
                displayName = existing.DisplayName;
                existing.LastLoginAt = now;
            }
            await _db.SaveAsync(ct);

            var cacheKey = $"user_id:{session.Pid}";
            await _redis.GetDatabase().StringSetAsync(cacheKey, resolvedUserId.ToString(), CacheTtl);

            var result = new AuthResponse
            {
                AccessToken = session.Tokens.AccessToken,
                RefreshToken = session.Tokens.RefreshToken,
                ExpiresAt = session.Tokens.AccessTokenExpiresAt,
                Profile = new AccountMeResponse
                {
                    UserId = resolvedUserId.ToString(),
                    Pid = session.Pid,
                    DisplayName = displayName,
                    IsGuest = session.AccountType == "guest",
                    LinkedProviders = session.AccountType is not null and not "guest"
                        ? new List<string> { session.AccountType }
                        : new List<string>(),
                    AvatarId = existing?.AvatarId ?? 1,
                    CreatedAt = (existing?.AccountCreatedAt ?? now).ToString("O")
                }
            };

            return wrapInLinkResponse
                ? Ok(new LinkAccountResponse { Success = true, Auth = result })
                : Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal error resolving player: {ex.Message}");
        }
    }

    private async Task<SaveSnapshotDto> GetSaveSnapshotAsync(long userId, CancellationToken ct)
    {
        var maxStage = await _db.UserRankingTotals.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.MaxClearedStageId)
            .FirstOrDefaultAsync(ct);

        var gold = await _db.UserCurrency.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.SoftAmount)
            .FirstOrDefaultAsync(ct);

        var clearedStages = await _db.UserRankingTotals.Query()
            .Where(x => x.UserId == userId)
            .Select(x => x.TotalClearedStages)
            .FirstOrDefaultAsync(ct);

        var items = await _db.UserInventory.Query()
            .Where(x => x.UserId == userId)
            .SumAsync(x => x.Count, ct);

        return new SaveSnapshotDto
        {
            MaxStageId = maxStage,
            Gold = gold,
            TotalClearedStages = clearedStages,
            TotalItems = items
        };
    }

    private sealed class PendingConflictDetails
    {
        public long GuestUserId { get; set; }
        public long CloudUserId { get; set; }
        public string CloudPid { get; set; } = string.Empty;
        public string PlatformSession { get; set; } = string.Empty;
    }

    private sealed class PlatformSession
    {
        public long UserId { get; set; }
        public string Pid { get; set; } = string.Empty;
        public long SessionId { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public PlatformTokens Tokens { get; set; } = new();
    }

    private sealed class PlatformTokens
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string AccessTokenExpiresAt { get; set; } = string.Empty;
    }
}
