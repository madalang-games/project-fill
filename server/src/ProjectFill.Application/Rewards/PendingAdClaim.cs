namespace ProjectFill.Application.Rewards;

public sealed class PendingAdClaim
{
    public long UserId { get; set; }
    public string PlacementId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string AdToken { get; set; } = string.Empty;
    public string ContextType { get; set; } = string.Empty;
    public string ContextId { get; set; } = string.Empty;
    public string RequestJson { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
