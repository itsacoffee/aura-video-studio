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

    public SettingsController(
        ILogger<SettingsController> logger,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _providerSettings = providerSettings;
        
        // Store AI optimization settings in AuraData directory
        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _settingsFilePath = Path.Combine(auraDataDir, "ai-optimization-settings.json");
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
}
