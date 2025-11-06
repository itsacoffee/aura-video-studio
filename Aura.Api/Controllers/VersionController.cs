using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for version information
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    private readonly ILogger<VersionController> _logger;
    private static VersionInfo? _cachedVersionInfo;
    private static readonly object _lock = new object();

    public VersionController(ILogger<VersionController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get application version information
    /// </summary>
    /// <returns>Version information including semantic version, build date, and assembly info</returns>
    [HttpGet]
    public async Task<ActionResult<VersionInfo>> GetVersion()
    {
        try
        {
            if (_cachedVersionInfo == null)
            {
                _cachedVersionInfo = await LoadVersionInfoAsync();
            }

            _logger.LogInformation("Version info requested: {Version}", _cachedVersionInfo.SemanticVersion);
            return Ok(_cachedVersionInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading version information");
            return StatusCode(500, new { error = "Failed to load version information" });
        }
    }

    private async Task<VersionInfo> LoadVersionInfoAsync()
    {
        var versionFilePath = Path.Combine(AppContext.BaseDirectory, "version.json");
        
        VersionInfo versionInfo;

        if (System.IO.File.Exists(versionFilePath))
        {
            var json = await System.IO.File.ReadAllTextAsync(versionFilePath);
            var versionData = JsonSerializer.Deserialize<JsonElement>(json);
            
            versionInfo = new VersionInfo
            {
                SemanticVersion = versionData.GetProperty("version").GetString() ?? "1.0.0",
                BuildDate = versionData.GetProperty("buildDate").GetString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                InformationalVersion = versionData.TryGetProperty("informationalVersion", out var infoVersion) 
                    ? infoVersion.GetString() ?? "1.0.0"
                    : versionData.GetProperty("version").GetString() ?? "1.0.0",
                Description = versionData.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? "Aura Video Studio"
                    : "Aura Video Studio"
            };
        }
        else
        {
            _logger.LogWarning("version.json not found, using assembly version");
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyVersion = assembly.GetName().Version?.ToString() ?? "1.0.0";
            
            versionInfo = new VersionInfo
            {
                SemanticVersion = assemblyVersion,
                BuildDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                InformationalVersion = assemblyVersion,
                Description = "Aura Video Studio"
            };
        }

        versionInfo.AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        versionInfo.RuntimeVersion = Environment.Version.ToString();

        return versionInfo;
    }
}

/// <summary>
/// Version information model
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// Semantic version (e.g., "1.0.0")
    /// </summary>
    public string SemanticVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Build date (e.g., "2025-11-06")
    /// </summary>
    public string BuildDate { get; set; } = string.Empty;

    /// <summary>
    /// Informational version with metadata (e.g., "1.0.0+abc123")
    /// </summary>
    public string InformationalVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Assembly version
    /// </summary>
    public string AssemblyVersion { get; set; } = string.Empty;

    /// <summary>
    /// .NET runtime version
    /// </summary>
    public string RuntimeVersion { get; set; } = string.Empty;

    /// <summary>
    /// Application description
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
