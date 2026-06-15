namespace ProjectFill.Application.Rewards;

public readonly record struct AdVerifyResult(bool Verified, string ProviderTxId);

public interface IAdRewardVerifier
{
    Task<AdVerifyResult> VerifyAsync(string provider, string adToken, CancellationToken ct);
}
