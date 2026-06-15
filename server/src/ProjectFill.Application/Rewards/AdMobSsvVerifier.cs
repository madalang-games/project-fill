using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ProjectFill.Application.Rewards;

public sealed class AdMobSsvVerifier : IAdRewardVerifier
{
    private const int MaxProviderTxIdLength = 128;
    private const int MaxProviderPartLength = 32;

    private readonly IDatabase _redis;
    private readonly bool _useMockVerifier;

    public AdMobSsvVerifier(IConnectionMultiplexer redis, IConfiguration config, ILogger<AdMobSsvVerifier> logger)
    {
        _redis = redis.GetDatabase();
        _useMockVerifier = config["AdReward:VerifyMode"] == "mock";
        if (_useMockVerifier)
            logger.LogWarning("AD_REWARD_VERIFY_MODE=mock; ad reward SSV verification is bypassed.");
    }

    public async Task<AdVerifyResult> VerifyAsync(string provider, string adToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(adToken))
            return new AdVerifyResult(false, string.Empty);

        if (_useMockVerifier)
        {
            return new AdVerifyResult(true, BuildMockProviderTxId(provider, adToken));
        }

        var txId = await _redis.StringGetDeleteAsync($"ssv:{adToken}");
        return txId.HasValue
            ? new AdVerifyResult(true, txId.ToString())
            : new AdVerifyResult(false, string.Empty);
    }

    private static string BuildMockProviderTxId(string provider, string adToken)
    {
        var raw = $"mock:{provider}:{adToken}";
        if (raw.Length <= MaxProviderTxIdLength)
            return raw;

        var providerPart = provider.Length <= MaxProviderPartLength
            ? provider
            : provider[..MaxProviderPartLength];
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return $"mock:{providerPart}:{hash}";
    }
}
