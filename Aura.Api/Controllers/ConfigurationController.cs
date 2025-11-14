using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Validation;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for comprehensive configuration management, validation, and import/export
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConfigurationValidator _validator;
    private readonly ProviderSettings _providerSettings;
    private readonly string _configDirectory;

    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        IConfiguration configuration,
        ConfigurationValidator validator,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _configuration = configuration;
        _validator = validator;
        _providerSettings = providerSettings;
        
        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _configDirectory = Path.Combine(auraDataDir, "config");
        Directory.CreateDirectory(_configDirectory);
    }

    /// <summary>
    /// Validate current configuration
    /// </summary>
    [HttpPost("validate")]
    public IActionResult ValidateConfiguration()
    {
        try
        {
            _logger.LogInformation("Validating configuration");
            var result = _validator.Validate();
            
            return Ok(new
            {
                isValid = result.IsValid,
                issues = result.Issues.Select(i => new
                {
                    key = i.Key,
                    message = i.Message,
                    severity = i.Severity.ToString()
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration");
            return StatusCode(500, new { error = "Failed to validate configuration" });
        }
    }

    /// <summary>
    /// Get complete configuration schema with metadata
    /// </summary>
    [HttpGet("schema")]
    public IActionResult GetConfigurationSchema()
    {
        try
        {
            var schema = new
            {
                version = "1.0.0",
                sections = new object[]
                {
                    new
                    {
                        name = "General",
                        settings = new object[]
                        {
                            new { key = "DefaultProjectSaveLocation", type = "string", required = false, default_ = "" },
                            new { key = "AutosaveIntervalSeconds", type = "number", required = false, default_ = 300, min = 30, max = 3600 },
                            new { key = "AutosaveEnabled", type = "boolean", required = false, default_ = true },
                            new { key = "Language", type = "string", required = false, default_ = "en-US" },
                            new { key = "Theme", type = "enum", required = false, default_ = "Auto", values = new[] { "Light", "Dark", "Auto" } }
                        }
                    },
                    new
                    {
                        name = "FileLocations",
                        settings = new object[]
                        {
                            new { key = "FFmpegPath", type = "string", required = false, default_ = "", validate = "path" },
                            new { key = "FFprobePath", type = "string", required = false, default_ = "", validate = "path" },
                            new { key = "OutputDirectory", type = "string", required = false, default_ = "", validate = "directory" },
                            new { key = "TempDirectory", type = "string", required = false, default_ = "", validate = "directory" }
                        }
                    },
                    new
                    {
                        name = "VideoDefaults",
                        settings = new object[]
                        {
                            new { key = "DefaultResolution", type = "enum", required = false, default_ = "1920x1080", values = new[] { "1280x720", "1920x1080", "2560x1440", "3840x2160" } },
                            new { key = "DefaultFrameRate", type = "number", required = false, default_ = 30, min = 24, max = 120 },
                            new { key = "DefaultCodec", type = "enum", required = false, default_ = "libx264", values = new[] { "libx264", "libx265", "h264_nvenc", "hevc_nvenc" } },
                            new { key = "DefaultBitrate", type = "string", required = false, default_ = "5M" }
                        }
                    },
                    new
                    {
                        name = "Hardware",
                        settings = new object[]
                        {
                            new { key = "PreferredGPU", type = "string", required = false, default_ = "auto" },
                            new { key = "HardwareAcceleration", type = "boolean", required = false, default_ = true },
                            new { key = "EncoderPreference", type = "enum", required = false, default_ = "auto", values = new[] { "auto", "nvenc", "amf", "qsv", "software" } }
                        }
                    },
                    new
                    {
                        name = "Advanced",
                        settings = new object[]
                        {
                            new { key = "OfflineMode", type = "boolean", required = false, default_ = false },
                            new { key = "StableDiffusionUrl", type = "string", required = false, default_ = "http://127.0.0.1:7860", validate = "url" },
                            new { key = "OllamaUrl", type = "string", required = false, default_ = "http://127.0.0.1:11434", validate = "url" },
                            new { key = "EnableTelemetry", type = "boolean", required = false, default_ = false }
                        }
                    }
                }
            };

            return Ok(schema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration schema");
            return StatusCode(500, new { error = "Failed to get configuration schema" });
        }
    }

    /// <summary>
    /// Get configuration value by key
    /// </summary>
    [HttpGet("value/{key}")]
    public IActionResult GetConfigValue(string key)
    {
        try
        {
            var value = _configuration[key];
            return Ok(new { key, value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting config value for key: {Key}", key);
            return StatusCode(500, new { error = $"Failed to get configuration value: {ex.Message}" });
        }
    }

    /// <summary>
    /// Export complete configuration as JSON
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportConfiguration(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Exporting configuration");

            var exportData = new
            {
                version = "1.0.0",
                exportedAt = DateTime.UtcNow,
                machineId = GetMachineIdentifier(),
                configuration = new
                {
                    urls = _configuration["Urls"],
                    engines = _configuration.GetSection("Engines").AsEnumerable().Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    ffmpeg = _configuration.GetSection("FFmpeg").AsEnumerable().Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    performance = _configuration.GetSection("Performance").AsEnumerable().Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    llmTimeouts = _configuration.GetSection("LlmTimeouts").AsEnumerable().Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    promptEngineering = _configuration.GetSection("PromptEngineering").AsEnumerable().Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                }
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"aura-config-{DateTime.UtcNow:yyyy-MM-dd}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting configuration");
            return StatusCode(500, new { error = "Failed to export configuration" });
        }
    }

    /// <summary>
    /// Import and validate configuration from JSON
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportConfiguration([FromBody] JsonElement configData, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Importing configuration");

            if (!configData.TryGetProperty("version", out var versionElement))
            {
                return BadRequest(new { error = "Invalid configuration file: missing version" });
            }

            var version = versionElement.GetString();
            if (version != "1.0.0")
            {
                return BadRequest(new { error = $"Unsupported configuration version: {version}" });
            }

            var warnings = new List<string>();
            var imported = 0;

            if (configData.TryGetProperty("configuration", out var config))
            {
                _logger.LogInformation("Configuration import would apply {Count} settings", imported);
                warnings.Add("Configuration import is read-only in this version. Settings must be applied manually.");
            }

            return Ok(new
            {
                success = true,
                imported,
                warnings,
                message = "Configuration validated successfully. Manual application required."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing configuration");
            return StatusCode(500, new { error = $"Failed to import configuration: {ex.Message}" });
        }
    }

    /// <summary>
    /// Backup current configuration
    /// </summary>
    [HttpPost("backup")]
    public async Task<IActionResult> BackupConfiguration(CancellationToken ct)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
            var backupFile = Path.Combine(_configDirectory, $"backup-{timestamp}.json");

            var backupData = new
            {
                version = "1.0.0",
                backedUpAt = DateTime.UtcNow,
                source = "automatic",
                configuration = _configuration.AsEnumerable().Where(kvp => kvp.Value != null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(backupFile, json, ct).ConfigureAwait(false);

            _logger.LogInformation("Configuration backed up to {BackupFile}", backupFile);

            return Ok(new
            {
                success = true,
                backupFile,
                timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error backing up configuration");
            return StatusCode(500, new { error = "Failed to backup configuration" });
        }
    }

    /// <summary>
    /// List available configuration backups
    /// </summary>
    [HttpGet("backups")]
    public IActionResult ListBackups()
    {
        try
        {
            var backupFiles = Directory.GetFiles(_configDirectory, "backup-*.json")
                .Select(f => new
                {
                    filename = Path.GetFileName(f),
                    path = f,
                    createdAt = System.IO.File.GetCreationTimeUtc(f),
                    sizeBytes = new FileInfo(f).Length
                })
                .OrderByDescending(b => b.createdAt)
                .ToList();

            return Ok(new
            {
                backups = backupFiles,
                totalCount = backupFiles.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backups");
            return StatusCode(500, new { error = "Failed to list backups" });
        }
    }

    /// <summary>
    /// Get environment variables that affect configuration
    /// </summary>
    [HttpGet("environment")]
    public IActionResult GetEnvironmentVariables()
    {
        try
        {
            var envVars = new Dictionary<string, string?>
            {
                { "AURA_API_URL", Environment.GetEnvironmentVariable("AURA_API_URL") },
                { "ASPNETCORE_URLS", Environment.GetEnvironmentVariable("ASPNETCORE_URLS") },
                { "ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") },
                { "DOTNET_ENVIRONMENT", Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") },
                { "OPENAI_API_KEY", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")) ? "***SET***" : null },
                { "ANTHROPIC_API_KEY", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")) ? "***SET***" : null },
                { "ELEVENLABS_API_KEY", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY")) ? "***SET***" : null },
                { "STABILITY_API_KEY", !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STABILITY_API_KEY")) ? "***SET***" : null }
            };

            return Ok(new
            {
                variables = envVars.Where(kvp => !string.IsNullOrEmpty(kvp.Value)),
                totalCount = envVars.Count(kvp => !string.IsNullOrEmpty(kvp.Value))
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting environment variables");
            return StatusCode(500, new { error = "Failed to get environment variables" });
        }
    }

    /// <summary>
    /// Reset configuration to defaults (with confirmation)
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetConfiguration([FromBody] ResetConfigRequest request, CancellationToken ct)
    {
        try
        {
            if (!request.Confirm)
            {
                return BadRequest(new { error = "Confirmation required to reset configuration" });
            }

            await BackupConfiguration(ct).ConfigureAwait(false);

            _logger.LogWarning("Configuration reset requested - backup created");

            return Ok(new
            {
                success = true,
                message = "Configuration backup created. Manual reset required by editing appsettings.json"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting configuration");
            return StatusCode(500, new { error = "Failed to reset configuration" });
        }
    }

    private string GetMachineIdentifier()
    {
        try
        {
            return Environment.MachineName;
        }
        catch
        {
            return "unknown";
        }
    }
}

/// <summary>
/// Request model for configuration reset
/// </summary>
public class ResetConfigRequest
{
    public bool Confirm { get; set; }
}
