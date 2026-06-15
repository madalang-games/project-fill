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
        var dataRoot = Path.Combine(_env.ContentRootPath, "generated", "data");
        if (!Directory.Exists(dataRoot))
        {
            // Fallback for bin directory execution
            dataRoot = Path.Combine(AppContext.BaseDirectory, "generated", "data");
        }

        var schemaVersionPath = Path.Combine(dataRoot, "data_schema_version.txt");
        var metaHashPath = Path.Combine(dataRoot, "meta_hash_cs.txt");

        var schemaVersion = System.IO.File.Exists(schemaVersionPath)
            ? System.IO.File.ReadAllText(schemaVersionPath).Trim()
            : "unknown";

        var metaHash = System.IO.File.Exists(metaHashPath)
            ? System.IO.File.ReadAllText(metaHashPath).Trim()
            : "unknown";

        var response = new BootstrapConfigResponse
        {
            ClientVersion = _config.App.AllowedClientVersion,
            RequiredClientVersion = _config.App.RequiredClientVersion,
            ProtocolVersion = _config.App.AllowedProtocolVersion,
            DataSchemaVersion = schemaVersion,
            MetaHash = metaHash,
            ServerTimeUtc = DateTimeOffset.UtcNow.ToString("O"),
            Maintenance = false, // Configurable flag if needed in future AppOptions
            MaintenanceMessage = null
        };

        return Ok(response);
    }
}
