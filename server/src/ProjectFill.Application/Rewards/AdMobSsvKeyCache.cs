using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectFill.Application.Rewards;

public sealed class AdMobSsvKeyCache : IHostedService, IDisposable
{
    private const string KeysUrl = "https://www.gstatic.com/admob/reward/verifier-keys.json";
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);

    private readonly HttpClient _http;
    private readonly ILogger<AdMobSsvKeyCache> _logger;
    private volatile IReadOnlyDictionary<long, byte[]> _keys = new Dictionary<long, byte[]>();
    private Timer? _timer;

    public AdMobSsvKeyCache(HttpClient http, ILogger<AdMobSsvKeyCache> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _timer = new Timer(_ => _ = RefreshAsync(), null, TimeSpan.Zero, RefreshInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();

    public byte[]? GetKeyBytes(long keyId) => _keys.TryGetValue(keyId, out var b) ? b : null;

    private async Task RefreshAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(KeysUrl);
            using var doc = JsonDocument.Parse(json);
            var newKeys = new Dictionary<long, byte[]>();
            foreach (var entry in doc.RootElement.GetProperty("keys").EnumerateArray())
            {
                var id = entry.GetProperty("keyId").GetInt64();
                var b64 = entry.GetProperty("base64").GetString()!;
                newKeys[id] = Convert.FromBase64String(b64);
            }
            _keys = newKeys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh AdMob SSV keys.");
        }
    }
}
