using System;
using System.IO;

namespace Aura.Core.Configuration;

/// <summary>
/// Centralized resolver for Aura runtime paths that honors Electron-provided environment variables.
/// </summary>
public static class AuraEnvironmentPaths
{
    private const string DefaultDataFolderName = "Aura";

    /// <summary>
    /// Returns the data root supplied via environment (AURA_DATA_PATH) if available.
    /// </summary>
    public static string? TryGetDataRootFromEnvironment()
    {
        return Normalize(Environment.GetEnvironmentVariable("AURA_DATA_PATH"));
    }

    /// <summary>
    /// Resolve the root directory for writable application data.
    /// </summary>
    /// <param name="fallback">
    /// Optional fallback path when no environment override is provided.
    /// If null, defaults to %LOCALAPPDATA%\Aura (on Windows) or the OS equivalent.
    /// </param>
    public static string ResolveDataRoot(string? fallback)
    {
        var envPath = TryGetDataRootFromEnvironment();
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return EnsureDirectory(envPath);
        }

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return EnsureDirectory(fallback);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var defaultPath = Path.Combine(localAppData, DefaultDataFolderName);
        return EnsureDirectory(defaultPath);
    }

    /// <summary>
    /// Resolve the logs directory, honoring AURA_LOGS_PATH when provided.
    /// </summary>
    public static string ResolveLogsPath(string? fallback = null)
    {
        var envPath = Normalize(Environment.GetEnvironmentVariable("AURA_LOGS_PATH"));
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return EnsureDirectory(envPath);
        }

        var defaultPath = fallback ?? Path.Combine(ResolveDataRoot(null), "logs");
        return EnsureDirectory(defaultPath);
    }

    /// <summary>
    /// Resolve the temp directory, honoring AURA_TEMP_PATH when provided.
    /// </summary>
    public static string ResolveTempPath(string? fallback = null)
    {
        var envPath = Normalize(Environment.GetEnvironmentVariable("AURA_TEMP_PATH"));
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return EnsureDirectory(envPath);
        }

        var defaultPath = fallback ?? Path.Combine(Path.GetTempPath(), DefaultDataFolderName);
        return EnsureDirectory(defaultPath);
    }

    /// <summary>
    /// Ensure a directory exists and return the normalized absolute path.
    /// </summary>
    public static string EnsureDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
        }

        var fullPath = Normalize(path) ?? throw new ArgumentException("Path cannot be resolved.", nameof(path));
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    private static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var trimmed = path.Trim();
        var expanded = Environment.ExpandEnvironmentVariables(trimmed);
        return Path.GetFullPath(expanded);
    }
}

