#nullable enable
namespace ProjectFill.Contracts.Rewards
{
    public sealed class RewardClaimRequest
    {
        public string SourceId { get; set; } = string.Empty;
    }

    public sealed class AdRewardClaimRequest
    {
        public string PlacementId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string AdToken { get; set; } = string.Empty;
        public string ContextType { get; set; } = string.Empty;
        public string ContextId { get; set; } = string.Empty;
    }
}
