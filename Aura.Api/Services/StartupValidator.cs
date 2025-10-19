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
                _logger.LogWarning("Tools directory does not exist, attempting to create: {ToolsDir}", toolsDir);
                try
                {
                    Directory.CreateDirectory(toolsDir);
                    _logger.LogInformation("Successfully created Tools directory");
                }
                catch (Exception createEx)
                {
                    _logger.LogError(createEx, "Failed to create Tools directory: {ToolsDir}", toolsDir);
                    _logger.LogError("Please ensure you have write permissions to the parent directory");
                    isValid = false;
                }
            }

            if (!Directory.Exists(auraDataDir))
            {
                _logger.LogWarning("AuraData directory does not exist, attempting to create: {AuraDataDir}", auraDataDir);
                try
                {
                    Directory.CreateDirectory(auraDataDir);
                    _logger.LogInformation("Successfully created AuraData directory");
                }
                catch (Exception createEx)
                {
                    _logger.LogError(createEx, "Failed to create AuraData directory: {AuraDataDir}", auraDataDir);
                    _logger.LogError("Please ensure you have write permissions to the parent directory");
                    isValid = false;
                }
            }

            if (!Directory.Exists(logsDir))
            {
                _logger.LogWarning("Logs directory does not exist, attempting to create: {LogsDir}", logsDir);
                try
                {
                    Directory.CreateDirectory(logsDir);
                    _logger.LogInformation("Successfully created Logs directory");
                }
                catch (Exception createEx)
                {
                    _logger.LogError(createEx, "Failed to create Logs directory: {LogsDir}", logsDir);
                    _logger.LogError("Please ensure you have write permissions to the parent directory");
                    isValid = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate directory configuration");
            _logger.LogError("This usually indicates a permissions issue. Please check:");
            _logger.LogError("  1. You have write access to the application directory");
            _logger.LogError("  2. The application is not blocked by antivirus software");
            _logger.LogError("  3. You are not running from a read-only location (like CD-ROM)");
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
