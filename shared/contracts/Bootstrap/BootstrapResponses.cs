#nullable enable

namespace ProjectFill.Contracts.Bootstrap
{
    public sealed class BootstrapConfigResponse
    {
        public bool ForceUpdate { get; set; }
        public string DataSchemaVersion { get; set; } = string.Empty;
        public string MetaHash { get; set; } = string.Empty;
        public string ServerTimeUtc { get; set; } = string.Empty;
        public bool Maintenance { get; set; }
        public string? MaintenanceMessage { get; set; }
    }

    public sealed class DataBundleResponse
    {
        public string SchemaVersion { get; set; } = string.Empty;
        public string MetaHash { get; set; } = string.Empty;
        public DataBundleFile[] Files { get; set; } = System.Array.Empty<DataBundleFile>();
    }

    public sealed class DataBundleFile
    {
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
