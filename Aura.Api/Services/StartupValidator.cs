using System;
using System.IO;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// Validates critical configuration and dependencies on startup
/// Fails fast with clear error messages if validation fails
/// </summary>
public class StartupValidator
{
    private readonly ILogger<StartupValidator> _logger;
    private readonly ProviderSettings _providerSettings;

    public StartupValidator(
        ILogger<StartupValidator> logger,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _providerSettings = providerSettings;
    }

    /// <summary>
    /// Validate critical startup configuration
    /// Returns true if validation passes, false otherwise
    /// </summary>
    public bool Validate()
    {
        _logger.LogInformation("Starting configuration validation...");
        var isValid = true;

        // Validate critical directories can be created
        try
        {
            var toolsDir = _providerSettings.GetToolsDirectory();
            _logger.LogInformation("Tools directory: {ToolsDir}", toolsDir);

            var auraDataDir = _providerSettings.GetAuraDataDirectory();
            _logger.LogInformation("AuraData directory: {AuraDataDir}", auraDataDir);

            var logsDir = _providerSettings.GetLogsDirectory();
            _logger.LogInformation("Logs directory: {LogsDir}", logsDir);

            var projectsDir = _providerSettings.GetProjectsDirectory();
            _logger.LogInformation("Projects directory: {ProjectsDir}", projectsDir);

            var downloadsDir = _providerSettings.GetDownloadsDirectory();
            _logger.LogInformation("Downloads directory: {DownloadsDir}", downloadsDir);

            // Verify all directories exist (they should be created by ProviderSettings)
            if (!Directory.Exists(toolsDir))
            {
                _logger.LogError("Tools directory does not exist and could not be created: {ToolsDir}", toolsDir);
                isValid = false;
            }

            if (!Directory.Exists(auraDataDir))
            {
                _logger.LogError("AuraData directory does not exist and could not be created: {AuraDataDir}", auraDataDir);
                isValid = false;
            }

            if (!Directory.Exists(logsDir))
            {
                _logger.LogError("Logs directory does not exist and could not be created: {LogsDir}", logsDir);
                isValid = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate directory configuration");
            isValid = false;
        }

        // Validate port configuration
        try
        {
            var configuredUrl = Environment.GetEnvironmentVariable("AURA_API_URL") 
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") 
                ?? "http://127.0.0.1:5005";
            
            _logger.LogInformation("API will listen on: {Url}", configuredUrl);

            // Basic validation - ensure URL is valid
            if (!Uri.TryCreate(configuredUrl.Split(';')[0], UriKind.Absolute, out var uri))
            {
                _logger.LogError("Invalid URL configuration: {Url}", configuredUrl);
                isValid = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate port configuration");
            isValid = false;
        }

        // Validate temp directory is writable
        try
        {
            var tempPath = Path.GetTempPath();
            var testFile = Path.Combine(tempPath, $"aura-startup-{Guid.NewGuid()}.tmp");

            File.WriteAllText(testFile, "startup validation test");
            File.Delete(testFile);

            _logger.LogInformation("Temp directory is writable: {TempPath}", tempPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Temp directory is not writable: {TempPath}", Path.GetTempPath());
            isValid = false;
        }

        if (isValid)
        {
            _logger.LogInformation("✓ Configuration validation passed");
        }
        else
        {
            _logger.LogError("✗ Configuration validation failed - see errors above");
        }

        return isValid;
    }
}
