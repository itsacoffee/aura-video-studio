using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for application settings including AI optimization
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ILogger<SettingsController> _logger;
    private readonly ProviderSettings _providerSettings;
    private readonly string _settingsFilePath;
    private readonly string _firstRunFilePath;

    public SettingsController(
        ILogger<SettingsController> logger,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _providerSettings = providerSettings;
        
        // Store AI optimization settings in AuraData directory
        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _settingsFilePath = Path.Combine(auraDataDir, "ai-optimization-settings.json");
        _firstRunFilePath = Path.Combine(auraDataDir, "first-run-status.json");
    }

    /// <summary>
    /// Get AI optimization settings
    /// </summary>
    [HttpGet("ai-optimization")]
    public async Task<IActionResult> GetAIOptimizationSettings(CancellationToken ct)
    {
        try
        {
            var settings = await LoadSettingsAsync(ct);
            
            return Ok(new
            {
                success = true,
                settings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading AI optimization settings");
            return StatusCode(500, new { error = "Failed to load settings" });
        }
    }

    /// <summary>
    /// Update AI optimization settings
    /// </summary>
    [HttpPost("ai-optimization")]
    public async Task<IActionResult> UpdateAIOptimizationSettings(
        [FromBody] AIOptimizationSettings settings,
        CancellationToken ct)
    {
        try
        {
            if (settings == null)
            {
                return BadRequest(new { error = "Settings are required" });
            }

            // Validate settings
            if (settings.MinimumQualityThreshold < 0 || settings.MinimumQualityThreshold > 100)
            {
                return BadRequest(new { error = "Quality threshold must be between 0 and 100" });
            }

            await SaveSettingsAsync(settings, ct);

            _logger.LogInformation(
                "AI optimization settings updated: Enabled={Enabled}, Level={Level}",
                settings.Enabled, settings.Level);

            return Ok(new
            {
                success = true,
                message = "Settings updated successfully",
                settings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating AI optimization settings");
            return StatusCode(500, new { error = "Failed to update settings" });
        }
    }

    /// <summary>
    /// Reset AI optimization settings to defaults
    /// </summary>
    [HttpPost("ai-optimization/reset")]
    public async Task<IActionResult> ResetAIOptimizationSettings(CancellationToken ct)
    {
        try
        {
            var defaultSettings = AIOptimizationSettings.Default;
            await SaveSettingsAsync(defaultSettings, ct);

            _logger.LogInformation("AI optimization settings reset to defaults");

            return Ok(new
            {
                success = true,
                message = "Settings reset to defaults",
                settings = defaultSettings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting AI optimization settings");
            return StatusCode(500, new { error = "Failed to reset settings" });
        }
    }

    /// <summary>
    /// Get first-run status
    /// </summary>
    [HttpGet("first-run")]
    public async Task<IActionResult> GetFirstRunStatus(CancellationToken ct)
    {
        try
        {
            var status = await LoadFirstRunStatusAsync(ct);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading first-run status");
            return StatusCode(500, new { error = "Failed to load first-run status" });
        }
    }

    /// <summary>
    /// Set first-run status
    /// </summary>
    [HttpPost("first-run")]
    public async Task<IActionResult> SetFirstRunStatus(
        [FromBody] FirstRunStatus status,
        CancellationToken ct)
    {
        try
        {
            if (status == null)
            {
                return BadRequest(new { error = "Status is required" });
            }

            await SaveFirstRunStatusAsync(status, ct);

            _logger.LogInformation(
                "First-run status updated: Completed={Completed}, Version={Version}",
                status.HasCompletedFirstRun, status.Version);

            return Ok(new
            {
                success = true,
                message = "First-run status updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating first-run status");
            return StatusCode(500, new { error = "Failed to update first-run status" });
        }
    }

    /// <summary>
    /// Reset first-run status (for testing/re-running wizard)
    /// </summary>
    [HttpPost("first-run/reset")]
    public async Task<IActionResult> ResetFirstRunStatus(CancellationToken ct)
    {
        try
        {
            var resetStatus = new FirstRunStatus
            {
                HasCompletedFirstRun = false,
                CompletedAt = null,
                Version = null
            };

            await SaveFirstRunStatusAsync(resetStatus, ct);

            _logger.LogInformation("First-run status reset");

            return Ok(new
            {
                success = true,
                message = "First-run status reset successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting first-run status");
            return StatusCode(500, new { error = "Failed to reset first-run status" });
        }
    }

    /// <summary>
    /// Load settings from file or return defaults
    /// </summary>
    private async Task<AIOptimizationSettings> LoadSettingsAsync(CancellationToken ct)
    {
        if (!System.IO.File.Exists(_settingsFilePath))
        {
            _logger.LogDebug("Settings file not found, using defaults");
            return AIOptimizationSettings.Default;
        }

        try
        {
            var json = await System.IO.File.ReadAllTextAsync(_settingsFilePath, ct);
            var settings = JsonSerializer.Deserialize<AIOptimizationSettings>(json);
            return settings ?? AIOptimizationSettings.Default;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading settings file, using defaults");
            return AIOptimizationSettings.Default;
        }
    }

    /// <summary>
    /// Save settings to file
    /// </summary>
    private async Task SaveSettingsAsync(AIOptimizationSettings settings, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(settings, options);
        await System.IO.File.WriteAllTextAsync(_settingsFilePath, json, ct);
    }

    /// <summary>
    /// Load first-run status from file or return default
    /// </summary>
    private async Task<FirstRunStatus> LoadFirstRunStatusAsync(CancellationToken ct)
    {
        if (!System.IO.File.Exists(_firstRunFilePath))
        {
            _logger.LogDebug("First-run status file not found, returning default");
            return new FirstRunStatus
            {
                HasCompletedFirstRun = false,
                CompletedAt = null,
                Version = null
            };
        }

        try
        {
            var json = await System.IO.File.ReadAllTextAsync(_firstRunFilePath, ct);
            var status = JsonSerializer.Deserialize<FirstRunStatus>(json);
            return status ?? new FirstRunStatus
            {
                HasCompletedFirstRun = false,
                CompletedAt = null,
                Version = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading first-run status file, using default");
            return new FirstRunStatus
            {
                HasCompletedFirstRun = false,
                CompletedAt = null,
                Version = null
            };
        }
    }

    /// <summary>
    /// Save first-run status to file
    /// </summary>
    private async Task SaveFirstRunStatusAsync(FirstRunStatus status, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_firstRunFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(status, options);
        await System.IO.File.WriteAllTextAsync(_firstRunFilePath, json, ct);
    }
}

/// <summary>
/// First-run status model
/// </summary>
public class FirstRunStatus
{
    public bool HasCompletedFirstRun { get; set; }
    public string? CompletedAt { get; set; }
    public string? Version { get; set; }
}
