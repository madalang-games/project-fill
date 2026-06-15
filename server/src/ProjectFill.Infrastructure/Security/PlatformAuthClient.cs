using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ProjectFill.Domain.Interfaces;

namespace ProjectFill.Infrastructure.Security;

public sealed class PlatformAuthClient : IPlatformAuthClient
{
    private readonly HttpClient _http;
    private readonly string _authority;

    public PlatformAuthClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _authority = (configuration["Jwt:Authority"]
            ?? throw new InvalidOperationException("Jwt:Authority not configured.")).TrimEnd('/');
    }

    public async Task<long?> GetUserIdByPidAsync(string pid, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync($"{_authority}/api/internal/users/{pid}/uid", ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<UidResult>(cancellationToken: ct);
            return result?.UserId;
        }
        catch
        {
            return null;
        }
    }

    private sealed class UidResult
    {
        public long UserId { get; set; }
    }
}
