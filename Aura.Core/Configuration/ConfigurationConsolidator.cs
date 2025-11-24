using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Aura.Core.Configuration;

/// <summary>
/// Consolidates configuration from multiple sources with priority order:
/// 1. Environment variables (highest priority)
/// 2. appsettings.{Environment}.json
/// 3. appsettings.json
/// 4. Defaults (lowest priority)
///
/// Also handles environment variable expansion in paths (%LOCALAPPDATA%, ~, etc.)
/// </summary>
public class ConfigurationConsolidator
{
    private readonly IConfiguration _configuration;
    private readonly string _environmentName;

    public ConfigurationConsolidator(IConfiguration configuration, string? environmentName = null)
    {
        _configuration = configuration;
        _environmentName = environmentName ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    }

    /// <summary>
    /// Gets a consolidated configuration value with priority order
    /// </summary>
    public string? GetValue(string key, string? defaultValue = null)
    {
        // 1. Check environment variable (highest priority)
        var envKey = key.Replace(":", "__").ToUpperInvariant();
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return ExpandEnvironmentVariables(envValue);
        }

        // 2. Check configuration (appsettings.{env}.json or appsettings.json)
        var configValue = _configuration[key];
        if (!string.IsNullOrWhiteSpace(configValue))
        {
            return ExpandEnvironmentVariables(configValue);
        }

        // 3. Return default
        return defaultValue != null ? ExpandEnvironmentVariables(defaultValue) : null;
    }

    /// <summary>
    /// Gets a consolidated path value with expansion and validation
    /// </summary>
    public string? GetPathValue(string key, string? defaultValue = null, bool createIfMissing = false)
    {
        var path = GetValue(key, defaultValue);
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Expand environment variables
        path = ExpandEnvironmentVariables(path);

        // Resolve to full path
        try
        {
            var fullPath = Path.GetFullPath(path);

            // Create directory if requested
            if (createIfMissing && !Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath;
        }
        catch (Exception)
        {
            // Return original path if expansion fails
            return path;
        }
    }

    /// <summary>
    /// Gets all configuration values for a section
    /// </summary>
    public Dictionary<string, string?> GetSection(string sectionKey)
    {
        var result = new Dictionary<string, string?>();

        // Get from configuration
        var section = _configuration.GetSection(sectionKey);
        foreach (var item in section.GetChildren())
        {
            var key = $"{sectionKey}:{item.Key}";
            result[item.Key] = GetValue(key);
        }

        return result;
    }

    /// <summary>
    /// Expands environment variables in a path string
    /// Handles %VAR%, ~ (home directory), and standard environment variables
    /// </summary>
    public static string ExpandEnvironmentVariables(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        // Expand standard environment variables (%VAR% on Windows, $VAR on Unix)
        var expanded = Environment.ExpandEnvironmentVariables(path);

        // Handle ~ (home directory) on Unix-like systems
        if (expanded.StartsWith("~/") || expanded == "~")
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            expanded = expanded.Replace("~", homeDir);
        }

        return expanded;
    }

    /// <summary>
    /// Validates that a path exists (for file-based settings)
    /// </summary>
    public bool ValidatePathExists(string path, bool isFile = false)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var expanded = ExpandEnvironmentVariables(path);
            var fullPath = Path.GetFullPath(expanded);

            if (isFile)
            {
                return File.Exists(fullPath);
            }
            else
            {
                return Directory.Exists(fullPath);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets consolidated FFmpeg configuration
    /// </summary>
    public ConsolidatedFfmpegConfig GetFfmpegConfiguration()
    {
        var executablePath = GetValue("FFmpeg:ExecutablePath");
        var probePath = GetValue("FFmpeg:ProbeExecutablePath");
        var envPath = Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH");

        // Priority: AURA_FFMPEG_PATH > FFmpeg:ExecutablePath > null
        var effectivePath = !string.IsNullOrWhiteSpace(envPath)
            ? ExpandEnvironmentVariables(envPath)
            : executablePath;

        return new ConsolidatedFfmpegConfig
        {
            Path = effectivePath,
            ProbePath = probePath,
            Mode = !string.IsNullOrWhiteSpace(effectivePath) ? FFmpegMode.Custom : FFmpegMode.System,
            Source = !string.IsNullOrWhiteSpace(envPath) ? "Environment" : "Configuration"
        };
    }

    /// <summary>
    /// Gets consolidated database configuration
    /// </summary>
    public DatabaseConfiguration GetDatabaseConfiguration()
    {
        var provider = GetValue("Database:Provider", "SQLite");
        var sqliteFileName = GetValue("Database:SQLiteFileName", "aura.db");
        var sqlitePath = GetValue("Database:SQLitePath");
        var envPath = Environment.GetEnvironmentVariable("AURA_DATABASE_PATH");

        // Priority: AURA_DATABASE_PATH > Database:SQLitePath > default
        var effectivePath = !string.IsNullOrWhiteSpace(envPath)
            ? ExpandEnvironmentVariables(envPath)
            : !string.IsNullOrWhiteSpace(sqlitePath)
                ? ExpandEnvironmentVariables(sqlitePath)
                : GetDefaultDatabasePath(sqliteFileName);

        return new DatabaseConfiguration
        {
            Provider = provider,
            SqlitePath = effectivePath,
            ConnectionString = GetValue("Database:ConnectionString")
        };
    }

    /// <summary>
    /// Gets consolidated output directory configuration
    /// </summary>
    public string GetOutputDirectory()
    {
        var outputDir = GetValue("OutputDirectory");
        if (!string.IsNullOrWhiteSpace(outputDir))
        {
            return ExpandEnvironmentVariables(outputDir);
        }

        // Default output directory
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, "AuraVideoStudio", "Output");
    }

    private string GetDefaultDatabasePath(string fileName)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Aura", fileName);
    }
}

/// <summary>
/// Consolidated FFmpeg configuration (for internal use)
/// </summary>
public class ConsolidatedFfmpegConfig
{
    public string? Path { get; set; }
    public string? ProbePath { get; set; }
    public FFmpegMode Mode { get; set; }
    public string? Source { get; set; }
}

/// <summary>
/// Consolidated database configuration
/// </summary>
public class DatabaseConfiguration
{
    public string Provider { get; set; } = "SQLite";
    public string? SqlitePath { get; set; }
    public string? ConnectionString { get; set; }
}

