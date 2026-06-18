using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace ProjectFill.Application.Rewards;

public sealed class AdMobSsvCallbackService
{
    private static readonly TimeSpan NonceTtl = TimeSpan.FromMinutes(5);

    private readonly IDatabase _redis;
    private readonly AdMobSsvKeyCache _keyCache;

    public AdMobSsvCallbackService(IConnectionMultiplexer redis, AdMobSsvKeyCache keyCache)
    {
        _redis = redis.GetDatabase();
        _keyCache = keyCache;
    }

    // rawQuery is the URL query string (without leading '?'), preserving original encoding.
    // AdMob signature is over this string excluding 'signature' and 'key_id' params.
    public async Task<bool> ProcessAsync(string rawQuery, CancellationToken ct)
    {
        var pairs = rawQuery.Split('&', StringSplitOptions.RemoveEmptyEntries);

        string? sig = null, nonce = null, txId = null;
        long keyId = 0;
        var hasKeyId = false;
        var msgBuilder = new StringBuilder();

        foreach (var pair in pairs)
        {
            var eq = pair.IndexOf('=');
            var rawKey = eq >= 0 ? pair[..eq] : pair;
            var rawVal = eq >= 0 ? pair[(eq + 1)..] : "";
            var key = Uri.UnescapeDataString(rawKey);
            var val = Uri.UnescapeDataString(rawVal);

            switch (key)
            {
                case "signature":
                    sig = val;
                    continue;
                case "key_id":
                    if (long.TryParse(val, out keyId)) hasKeyId = true;
                    continue;
                case "custom_data":
                    nonce = val;
                    break;
                case "transaction_id":
                    txId = val;
                    break;
            }

            // Google signs the URL-decoded query values, not the raw percent-encoded form received in transit.
            if (msgBuilder.Length > 0) msgBuilder.Append('&');
            msgBuilder.Append(key).Append('=').Append(val);
        }

        if (sig is null || !hasKeyId || nonce is null || txId is null) return false;

        var keyBytes = _keyCache.GetKeyBytes(keyId);
        if (keyBytes is null) return false;

        var sigBytes = Base64UrlDecode(sig);
        if (sigBytes is null) return false;

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
        if (!ecdsa.VerifyData(Encoding.UTF8.GetBytes(msgBuilder.ToString()), sigBytes, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
            return false;

        await _redis.StringSetAsync($"ssv:{nonce}", txId, NonceTtl);
        return true;
    }

    private static byte[]? Base64UrlDecode(string s)
    {
        try
        {
            var padded = s.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }
        catch { return null; }
    }
}
