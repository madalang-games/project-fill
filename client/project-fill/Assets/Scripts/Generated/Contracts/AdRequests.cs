#nullable enable
namespace ProjectFill.Contracts.Ad
{
    public sealed class AdDoubleRewardRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string AdToken { get; set; } = string.Empty;
        public int StageId { get; set; }
        public string AttemptId { get; set; } = string.Empty;
    }

    public sealed class AdInterstitialShownRequest
    {
        public int StageId { get; set; }
    }
}
