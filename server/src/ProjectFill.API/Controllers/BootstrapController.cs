using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectFill.Contracts.Bootstrap;

namespace ProjectFill.API.Controllers;

[ApiController]
[Route("api/bootstrap")]
[AllowAnonymous]
public sealed class BootstrapController : ControllerBase
{
    private readonly ProjectFillConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public BootstrapController(ProjectFillConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var clientVersion  = Request.Headers["X-Client-Version"].FirstOrDefault() ?? string.Empty;
        var protocolVersion = Request.Headers["X-Protocol-Version"].FirstOrDefault() ?? string.Empty;

        var forceUpdate = false;

        if (!string.IsNullOrEmpty(clientVersion) && !string.IsNullOrEmpty(_config.App.AllowedClientVersion))
            forceUpdate |= IsVersionLower(clientVersion, _config.App.AllowedClientVersion);

        if (!string.IsNullOrEmpty(protocolVersion) && !string.IsNullOrEmpty(_config.App.AllowedProtocolVersion))
            forceUpdate |= protocolVersion != _config.App.AllowedProtocolVersion;

        var dataRoot = Path.Combine(_env.ContentRootPath, "generated", "data");
        if (!Directory.Exists(dataRoot))
            dataRoot = Path.Combine(AppContext.BaseDirectory, "generated", "data");

        var schemaVersion = ReadFileOrDefault(Path.Combine(dataRoot, "data_schema_version.txt"));
        var metaHash      = ReadFileOrDefault(Path.Combine(dataRoot, "meta_hash_cs.txt"));

        return Ok(new BootstrapConfigResponse
        {
            ForceUpdate       = forceUpdate,
            DataSchemaVersion = schemaVersion,
            MetaHash          = metaHash,
            ServerTimeUtc     = DateTimeOffset.UtcNow.ToString("O"),
            Maintenance       = false,
            MaintenanceMessage = null
        });
    }

    private static string ReadFileOrDefault(string path) =>
        System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path).Trim() : "unknown";

    private static bool IsVersionLower(string current, string required)
    {
        if (!Version.TryParse(current, out var cur) || !Version.TryParse(required, out var req))
            return false;
        return cur.Major < req.Major || (cur.Major == req.Major && cur.Minor < req.Minor);
    }

    [HttpGet("/api/data/bundle")]
    public IActionResult GetBundle()
    {
        var dataRoot = Path.Combine(_env.ContentRootPath, "generated", "data");
        if (!Directory.Exists(dataRoot))
            dataRoot = Path.Combine(AppContext.BaseDirectory, "generated", "data");

        var bundlePath = Path.Combine(dataRoot, "client_bundle.json");
        if (!System.IO.File.Exists(bundlePath))
            return NotFound();

        var raw = JsonSerializer.Deserialize<RawBundle>(
            System.IO.File.ReadAllText(bundlePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (raw?.Files == null)
            return StatusCode(500);

        var response = new DataBundleResponse
        {
            SchemaVersion = raw.SchemaVersion,
            MetaHash      = raw.MetaHash,
            Files         = raw.Files.Select(kv => new DataBundleFile { Path = kv.Key, Content = kv.Value }).ToArray()
        };

        return Ok(response);
    }

    private sealed class RawBundle
    {
        public string SchemaVersion { get; set; } = string.Empty;
        public string MetaHash { get; set; } = string.Empty;
        public Dictionary<string, string> Files { get; set; } = new();
    }
}
