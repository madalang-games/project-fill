using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ProjectFill.Application.Rewards;

public sealed class AdMobSsvCallbackService
{
    // Long TTL so a reward is never lost to SSV latency or an offline client (see conventions/ad-reward-ssv-system.md).
    private static readonly TimeSpan NonceTtl = TimeSpan.FromHours(24);

    private readonly IDatabase _redis;
    private readonly AdMobSsvKeyCache _keyCache;
    private readonly AdRewardGrantCoordinator _coordinator;
    private readonly ILogger<AdMobSsvCallbackService> _logger;

    public AdMobSsvCallbackService(IConnectionMultiplexer redis, AdMobSsvKeyCache keyCache, AdRewardGrantCoordinator coordinator, ILogger<AdMobSsvCallbackService> logger)
    {
        _redis = redis.GetDatabase();
        _keyCache = keyCache;
        _coordinator = coordinator;
        _logger = logger;
    }

    // rawQuery is the URL query string (without leading '?'), preserving original encoding.
    // AdMob signature is over this string excluding 'signature' and 'key_id' params.
    public async Task<bool> ProcessAsync(string rawQuery, CancellationToken ct)
    {
        try
        {
            return await VerifyAndStoreAsync(rawQuery, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AdMob SSV callback failed with unexpected exception. queryLength={QueryLength}", rawQuery.Length);
            return false;
        }
    }

    private async Task<bool> VerifyAndStoreAsync(string rawQuery, CancellationToken ct)
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

        if (sig is null || !hasKeyId || nonce is null || txId is null)
        {
            _logger.LogWarning(
                "AdMob SSV callback rejected: malformed query. hasSig={HasSig} hasKeyId={HasKeyId} hasNonce={HasNonce} hasTxId={HasTxId} queryLength={QueryLength}",
                sig is not null, hasKeyId, nonce is not null, txId is not null, rawQuery.Length);
            return false;
        }

        var keyBytes = _keyCache.GetKeyBytes(keyId);
        if (keyBytes is null)
        {
            _logger.LogWarning(
                "AdMob SSV callback rejected: key_id not found in key cache. keyId={KeyId} nonce={Nonce}",
                keyId, Short(nonce));
            return false;
        }

        var sigBytes = Base64UrlDecode(sig);
        if (sigBytes is null)
        {
            _logger.LogWarning(
                "AdMob SSV callback rejected: signature decode failed. keyId={KeyId} nonce={Nonce}",
                keyId, Short(nonce));
            return false;
        }

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
        if (!ecdsa.VerifyData(Encoding.UTF8.GetBytes(msgBuilder.ToString()), sigBytes, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
        {
            _logger.LogWarning(
                "AdMob SSV callback rejected: signature verification failed. keyId={KeyId} nonce={Nonce} tx={TransactionId}",
                keyId, Short(nonce), Short(txId));
            return false;
        }

        await _redis.StringSetAsync($"ssv:{nonce}", txId, NonceTtl);
        _logger.LogInformation(
            "AdMob SSV callback accepted. keyId={KeyId} nonce={Nonce} tx={TransactionId}",
            keyId, Short(nonce), Short(txId));

        // Callback-driven grant: if the client already issued a claim (pending_claim exists),
        // grant now so the reward does not depend on the client still polling.
        var (outcome, _) = await _coordinator.TryGrantPendingAsync(nonce, ct);
        _logger.LogInformation("AdMob SSV grant trigger: nonce={Nonce} outcome={Outcome}", Short(nonce), outcome);
        return true;
    }

    private static string Short(string value)
        => string.IsNullOrEmpty(value) ? "" : (value.Length <= 8 ? value : value[..8]);

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
