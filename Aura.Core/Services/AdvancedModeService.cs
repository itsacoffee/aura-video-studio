using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for checking and managing advanced mode state
/// </summary>
public class AdvancedModeService
{
    private readonly ILogger<AdvancedModeService> _logger;
    private readonly string _userSettingsFilePath;

    public AdvancedModeService(
        ILogger<AdvancedModeService> logger,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        var auraDataDir = providerSettings.GetAuraDataDirectory();
        _userSettingsFilePath = Path.Combine(auraDataDir, "user-settings.json");
    }

    /// <summary>
    /// Check if advanced mode is currently enabled
    /// </summary>
    public async Task<bool> IsAdvancedModeEnabledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await LoadUserSettingsAsync(cancellationToken);
            return settings.General.AdvancedModeEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking advanced mode status, defaulting to false");
            return false;
        }
    }

    /// <summary>
    /// Load user settings from file or return defaults
    /// </summary>
    private async Task<UserSettings> LoadUserSettingsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_userSettingsFilePath))
        {
            _logger.LogDebug("User settings file not found, using defaults (advanced mode disabled)");
            return new UserSettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_userSettingsFilePath, cancellationToken);
            var settings = JsonSerializer.Deserialize<UserSettings>(json);
            return settings ?? new UserSettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading user settings file, using defaults");
            return new UserSettings();
        }
    }
}
