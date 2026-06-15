#nullable enable

namespace ProjectFill.Contracts.Bootstrap
{
    public sealed class BootstrapConfigResponse
    {
        public string ClientVersion { get; set; } = string.Empty;
        public string RequiredClientVersion { get; set; } = string.Empty;
        public string ProtocolVersion { get; set; } = string.Empty;
        public string DataSchemaVersion { get; set; } = string.Empty;
        public string MetaHash { get; set; } = string.Empty;
        public string ServerTimeUtc { get; set; } = string.Empty;
        public bool Maintenance { get; set; }
        public string? MaintenanceMessage { get; set; }
    }
}
