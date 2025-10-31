using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for application settings including AI optimization and user settings
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ILogger<SettingsController> _logger;
    private readonly ProviderSettings _providerSettings;
    private readonly string _settingsFilePath;
    private readonly string _firstRunFilePath;
    private readonly string _userSettingsFilePath;

    public SettingsController(
        ILogger<SettingsController> logger,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _providerSettings = providerSettings;
        
        // Store settings in AuraData directory
        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _settingsFilePath = Path.Combine(auraDataDir, "ai-optimization-settings.json");
        _firstRunFilePath = Path.Combine(auraDataDir, "first-run-status.json");
        _userSettingsFilePath = Path.Combine(auraDataDir, "user-settings.json");
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

    /// <summary>
    /// Get comprehensive user settings
    /// </summary>
    [HttpGet("user")]
    public async Task<IActionResult> GetUserSettings(CancellationToken ct)
    {
        try
        {
            var settings = await LoadUserSettingsAsync(ct);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user settings");
            return StatusCode(500, new { error = "Failed to load user settings" });
        }
    }

    /// <summary>
    /// Save comprehensive user settings
    /// </summary>
    [HttpPost("user")]
    public async Task<IActionResult> SaveUserSettings(
        [FromBody] UserSettings settings,
        CancellationToken ct)
    {
        try
        {
            if (settings == null)
            {
                return BadRequest(new { error = "Settings are required" });
            }

            // Update timestamp
            settings.LastUpdated = DateTime.UtcNow;

            await SaveUserSettingsAsync(settings, ct);

            _logger.LogInformation("User settings saved successfully");

            return Ok(new
            {
                success = true,
                message = "Settings saved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user settings");
            return StatusCode(500, new { error = "Failed to save user settings" });
        }
    }

    /// <summary>
    /// Reset user settings to defaults
    /// </summary>
    [HttpPost("user/reset")]
    public async Task<IActionResult> ResetUserSettings(CancellationToken ct)
    {
        try
        {
            var defaults = new UserSettings();
            await SaveUserSettingsAsync(defaults, ct);

            _logger.LogInformation("User settings reset to defaults");

            return Ok(new
            {
                success = true,
                message = "Settings reset to defaults",
                settings = defaults
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting user settings");
            return StatusCode(500, new { error = "Failed to reset user settings" });
        }
    }

    /// <summary>
    /// Test API key for a specific provider
    /// </summary>
    [HttpPost("test-api-key/{provider}")]
    public async Task<IActionResult> TestApiKey(
        string provider,
        [FromBody] TestApiKeyRequest request,
        CancellationToken ct)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return Ok(new
                {
                    success = false,
                    message = "API key is required"
                });
            }

            // Provider-specific validation
            var result = provider.ToLowerInvariant() switch
            {
                "openai" => ValidateOpenAIKey(request.ApiKey),
                "anthropic" => ValidateAnthropicKey(request.ApiKey),
                "stabilityai" => ValidateStabilityAIKey(request.ApiKey),
                "elevenlabs" => ValidateElevenLabsKey(request.ApiKey),
                _ => new { success = false, message = "Unknown provider" }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API key for {Provider}", provider);
            return Ok(new
            {
                success = false,
                message = $"Error testing API key: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Validate file or directory path
    /// </summary>
    [HttpPost("validate-path")]
    public IActionResult ValidatePath([FromBody] ValidatePathRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return Ok(new
                {
                    valid = false,
                    message = "Path is required"
                });
            }

            // Check if it's a file
            if (System.IO.File.Exists(request.Path))
            {
                return Ok(new
                {
                    valid = true,
                    message = "File exists"
                });
            }

            // Check if it's a directory
            if (Directory.Exists(request.Path))
            {
                return Ok(new
                {
                    valid = true,
                    message = "Directory exists"
                });
            }

            return Ok(new
            {
                valid = false,
                message = "Path does not exist"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating path");
            return Ok(new
            {
                valid = false,
                message = $"Error validating path: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Load user settings from file or return defaults
    /// </summary>
    private async Task<UserSettings> LoadUserSettingsAsync(CancellationToken ct)
    {
        if (!System.IO.File.Exists(_userSettingsFilePath))
        {
            _logger.LogDebug("User settings file not found, using defaults");
            return new UserSettings();
        }

        try
        {
            var json = await System.IO.File.ReadAllTextAsync(_userSettingsFilePath, ct);
            var settings = JsonSerializer.Deserialize<UserSettings>(json);
            return settings ?? new UserSettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading user settings file, using defaults");
            return new UserSettings();
        }
    }

    /// <summary>
    /// Save user settings to file
    /// </summary>
    private async Task SaveUserSettingsAsync(UserSettings settings, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_userSettingsFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(settings, options);
        await System.IO.File.WriteAllTextAsync(_userSettingsFilePath, json, ct);
    }

    /// <summary>
    /// Validate OpenAI API key format
    /// </summary>
    private object ValidateOpenAIKey(string apiKey)
    {
        if (!apiKey.StartsWith("sk-") || apiKey.Length < 20)
        {
            return new
            {
                success = false,
                message = "Invalid OpenAI API key format. Must start with 'sk-' and be at least 20 characters."
            };
        }

        return new
        {
            success = true,
            message = "API key format is valid"
        };
    }

    /// <summary>
    /// Validate Anthropic API key format
    /// </summary>
    private object ValidateAnthropicKey(string apiKey)
    {
        if (!apiKey.StartsWith("sk-ant-") || apiKey.Length < 20)
        {
            return new
            {
                success = false,
                message = "Invalid Anthropic API key format. Must start with 'sk-ant-' and be at least 20 characters."
            };
        }

        return new
        {
            success = true,
            message = "API key format is valid"
        };
    }

    /// <summary>
    /// Validate Stability AI API key format
    /// </summary>
    private object ValidateStabilityAIKey(string apiKey)
    {
        if (!apiKey.StartsWith("sk-") || apiKey.Length < 20)
        {
            return new
            {
                success = false,
                message = "Invalid Stability AI API key format. Must start with 'sk-' and be at least 20 characters."
            };
        }

        return new
        {
            success = true,
            message = "API key format is valid"
        };
    }

    /// <summary>
    /// Validate ElevenLabs API key format
    /// </summary>
    private object ValidateElevenLabsKey(string apiKey)
    {
        if (apiKey.Length < 32)
        {
            return new
            {
                success = false,
                message = "Invalid ElevenLabs API key format. Must be at least 32 characters."
            };
        }

        return new
        {
            success = true,
            message = "API key format is valid"
        };
    }

    /// <summary>
    /// Get current Ollama model setting
    /// </summary>
    [HttpGet("ollama/model")]
    public IActionResult GetOllamaModel()
    {
        try
        {
            var model = _providerSettings.GetOllamaModel();
            _logger.LogInformation("Retrieved Ollama model setting: {Model}", model);
            
            return Ok(new
            {
                success = true,
                model
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Ollama model setting");
            return StatusCode(500, new { error = "Failed to get Ollama model setting" });
        }
    }

    /// <summary>
    /// Set Ollama model
    /// </summary>
    [HttpPost("ollama/model")]
    public IActionResult SetOllamaModel([FromBody] SetOllamaModelRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Model))
            {
                return BadRequest(new { error = "Model name is required" });
            }

            _providerSettings.SetOllamaModel(request.Model);
            _logger.LogInformation("Ollama model setting updated to: {Model}", request.Model);
            
            return Ok(new
            {
                success = true,
                message = "Ollama model updated successfully",
                model = request.Model
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Ollama model");
            return StatusCode(500, new { error = "Failed to set Ollama model" });
        }
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

/// <summary>
/// Request model for testing API keys
/// </summary>
public class TestApiKeyRequest
{
    public string ApiKey { get; set; } = "";
}

/// <summary>
/// Request model for validating file paths
/// </summary>
public class ValidatePathRequest
{
    public string Path { get; set; } = "";
}

/// <summary>
/// Request model for setting Ollama model
/// </summary>
public class SetOllamaModelRequest
{
    public string Model { get; set; } = "";
}

