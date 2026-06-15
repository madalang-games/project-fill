namespace ProjectFill.Application.Rewards;

public sealed class DevelopmentAdRewardVerifier : IAdRewardVerifier
{
    public Task<AdVerifyResult> VerifyAsync(string provider, string adToken, CancellationToken ct)
        => Task.FromResult(
            !string.IsNullOrWhiteSpace(provider) && !string.IsNullOrWhiteSpace(adToken)
                ? new AdVerifyResult(true, adToken)
                : new AdVerifyResult(false, string.Empty));
}
