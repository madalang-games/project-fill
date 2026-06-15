using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace ProjectFill.Infrastructure.Security;

public sealed class JwtPublicKeyCache : IHostedService, IDisposable
{
    private readonly HttpClient _http;
    private readonly string _jwksUrl;
    private List<SecurityKey> _keys = [];
    private Timer? _timer;

    public JwtPublicKeyCache(IConfiguration configuration, HttpClient http)
    {
        var authority = configuration["Jwt:Authority"];
        if (string.IsNullOrWhiteSpace(authority))
            throw new InvalidOperationException("Configuration error at configuration:Jwt:Authority: missing required value.");

        _http = http;
        _jwksUrl = $"{authority.TrimEnd('/')}/.well-known/jwks.json";
    }

    public IEnumerable<SecurityKey> GetKeysForKid(string? kid)
    {
        if (string.IsNullOrEmpty(kid) || _keys.Any(key => key.KeyId == kid))
            return _keys;

        try { RefreshAsync().GetAwaiter().GetResult(); }
        catch { }

        return _keys;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync();
        var initial = _keys.Count > 0 ? TimeSpan.FromHours(24) : TimeSpan.FromSeconds(30);
        _timer = new Timer(OnTick, null, initial, TimeSpan.FromSeconds(30));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private void OnTick(object? state)
    {
        _ = RefreshAndRescheduleAsync();
    }

    private async Task RefreshAndRescheduleAsync()
    {
        await RefreshAsync();
        if (_keys.Count > 0)
            _timer?.Change(TimeSpan.FromHours(24), TimeSpan.FromHours(24));
    }

    private async Task RefreshAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(_jwksUrl);
            using var doc = JsonDocument.Parse(json);
            var keys = new List<SecurityKey>();

            foreach (var key in doc.RootElement.GetProperty("keys").EnumerateArray())
            {
                if (!key.TryGetProperty("kty", out var kty) || kty.GetString() != "RSA")
                    continue;

                var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = Base64UrlEncoder.DecodeBytes(key.GetProperty("n").GetString()!),
                    Exponent = Base64UrlEncoder.DecodeBytes(key.GetProperty("e").GetString()!),
                });

                var rsaKey = new RsaSecurityKey(rsa);
                if (key.TryGetProperty("kid", out var kidProp) && kidProp.GetString() is { Length: > 0 } kid)
                    rsaKey.KeyId = kid;
                keys.Add(rsaKey);
            }

            _keys = keys;
        }
        catch
        {
            // Retain the previous key set when platform-auth is temporarily unavailable.
        }
    }

    public void Dispose() => _timer?.Dispose();
}
