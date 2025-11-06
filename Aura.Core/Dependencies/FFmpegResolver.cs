using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Dependencies;

/// <summary>
/// Centralized FFmpeg resolution with managed install precedence and version validation with caching
/// </summary>
public class FFmpegResolver
{
    private readonly ILogger<FFmpegResolver> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _managedInstallRoot;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly string CacheKey = "ffmpeg-resolution-result";

    public FFmpegResolver(ILogger<FFmpegResolver> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _managedInstallRoot = Path.Combine(localAppData, "AuraVideoStudio", "ffmpeg");
    }

    /// <summary>
    /// Resolve effective FFmpeg path with precedence and caching
    /// Precedence: Managed install > User-configured path > PATH lookup
    /// </summary>
    public async Task<FfmpegResolutionResult> ResolveAsync(
        string? configuredPath = null,
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        if (!forceRefresh && _cache.TryGetValue(CacheKey, out FfmpegResolutionResult? cached))
        {
            _logger.LogDebug("Returning cached FFmpeg resolution result");
            return cached!;
        }

        _logger.LogInformation("Resolving FFmpeg path with precedence: Managed > Configured > PATH");

        FfmpegResolutionResult result;

        try
        {
            // 1. Check managed install first (highest priority)
            result = await CheckManagedInstallAsync(ct);
            if (result.Found && result.IsValid)
            {
                _logger.LogInformation("Using managed FFmpeg install: {Path}", result.Path);
                CacheResult(result);
                return result;
            }

            // 2. Check user-configured path
            if (!string.IsNullOrEmpty(configuredPath))
            {
                result = await CheckConfiguredPathAsync(configuredPath, ct);
                if (result.Found && result.IsValid)
                {
                    _logger.LogInformation("Using configured FFmpeg path: {Path}", result.Path);
                    CacheResult(result);
                    return result;
                }
            }

            // 3. Check PATH environment
            result = await CheckPathEnvironmentAsync(ct);
            if (result.Found && result.IsValid)
            {
                _logger.LogInformation("Using FFmpeg from PATH");
                CacheResult(result);
                return result;
            }

            // Nothing found
            result = new FfmpegResolutionResult
            {
                Found = false,
                IsValid = false,
                Source = "None",
                Error = "FFmpeg not found in any location. Install managed FFmpeg or configure path in Settings."
            };

            CacheResult(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving FFmpeg path");
            result = new FfmpegResolutionResult
            {
                Found = false,
                IsValid = false,
                Source = "None",
                Error = $"Error during FFmpeg resolution: {ex.Message}"
            };
            return result;
        }
    }

    /// <summary>
    /// Check managed install location
    /// </summary>
    private async Task<FfmpegResolutionResult> CheckManagedInstallAsync(CancellationToken ct)
    {
        _logger.LogDebug("Checking managed install root: {Root}", _managedInstallRoot);

        if (!Directory.Exists(_managedInstallRoot))
        {
            return new FfmpegResolutionResult
            {
                Found = false,
                Source = "Managed",
                Error = "Managed install directory does not exist"
            };
        }

        // Find the latest version directory
        var versionDirs = Directory.GetDirectories(_managedInstallRoot);
        if (versionDirs.Length == 0)
        {
            return new FfmpegResolutionResult
            {
                Found = false,
                Source = "Managed",
                Error = "No managed FFmpeg versions installed"
            };
        }

        // Sort by directory name (descending) to get latest version
        Array.Sort(versionDirs, StringComparer.OrdinalIgnoreCase);
        Array.Reverse(versionDirs);

        foreach (var versionDir in versionDirs)
        {
            var manifestPath = Path.Combine(versionDir, "install.json");
            if (!File.Exists(manifestPath))
            {
                _logger.LogDebug("No manifest in {Dir}, skipping", versionDir);
                continue;
            }

            try
            {
                var manifestJson = await File.ReadAllTextAsync(manifestPath, ct);
                var manifest = JsonSerializer.Deserialize<FfmpegInstallMetadata>(manifestJson);

                if (manifest == null || string.IsNullOrEmpty(manifest.FfmpegPath))
                {
                    _logger.LogWarning("Invalid manifest in {Dir}", versionDir);
                    continue;
                }

                if (!File.Exists(manifest.FfmpegPath))
                {
                    _logger.LogWarning("Manifest points to missing file: {Path}", manifest.FfmpegPath);
                    continue;
                }

                // Validate the binary
                var validation = await ValidateFFmpegBinaryAsync(manifest.FfmpegPath, ct);
                if (validation.success)
                {
                    return new FfmpegResolutionResult
                    {
                        Found = true,
                        IsValid = true,
                        Path = manifest.FfmpegPath,
                        Version = manifest.Version,
                        Source = "Managed",
                        ValidationOutput = validation.output
                    };
                }
                else
                {
                    _logger.LogWarning("Managed FFmpeg validation failed: {Error}", validation.error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking managed install in {Dir}", versionDir);
            }
        }

        return new FfmpegResolutionResult
        {
            Found = false,
            Source = "Managed",
            Error = "No valid managed FFmpeg installations found"
        };
    }

    /// <summary>
    /// Check user-configured path
    /// </summary>
    private async Task<FfmpegResolutionResult> CheckConfiguredPathAsync(string configuredPath, CancellationToken ct)
    {
        _logger.LogDebug("Checking configured path: {Path}", configuredPath);

        string resolvedPath;

        // Handle the case where configuredPath is just "ffmpeg" (indicates PATH lookup previously worked)
        if (configuredPath == "ffmpeg" || configuredPath == "ffmpeg.exe")
        {
            _logger.LogDebug("Configured path is '{Path}', treating as PATH lookup", configuredPath);
            return await CheckPathEnvironmentAsync(ct);
        }

        // Resolve to actual executable path
        if (File.Exists(configuredPath))
        {
            resolvedPath = configuredPath;
        }
        else if (Directory.Exists(configuredPath))
        {
            var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
            var exePath = Path.Combine(configuredPath, exeName);

            if (File.Exists(exePath))
            {
                resolvedPath = exePath;
            }
            else
            {
                var binPath = Path.Combine(configuredPath, "bin", exeName);
                if (File.Exists(binPath))
                {
                    resolvedPath = binPath;
                }
                else
                {
                    return new FfmpegResolutionResult
                    {
                        Found = false,
                        Source = "Configured",
                        Error = $"FFmpeg executable not found in configured directory: {configuredPath}"
                    };
                }
            }
        }
        else
        {
            return new FfmpegResolutionResult
            {
                Found = false,
                Source = "Configured",
                Error = $"Configured path does not exist: {configuredPath}"
            };
        }

        var validation = await ValidateFFmpegBinaryAsync(resolvedPath, ct);
        if (validation.success)
        {
            return new FfmpegResolutionResult
            {
                Found = true,
                IsValid = true,
                Path = resolvedPath,
                Version = ExtractVersionString(validation.output),
                Source = "Configured",
                ValidationOutput = validation.output
            };
        }
        else
        {
            return new FfmpegResolutionResult
            {
                Found = true,
                IsValid = false,
                Path = resolvedPath,
                Source = "Configured",
                Error = $"FFmpeg validation failed: {validation.error}"
            };
        }
    }

    /// <summary>
    /// Check PATH environment variable
    /// </summary>
    private async Task<FfmpegResolutionResult> CheckPathEnvironmentAsync(CancellationToken ct)
    {
        _logger.LogDebug("Checking PATH environment for FFmpeg");

        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";

        var validation = await ValidateFFmpegBinaryAsync(exeName, ct);
        if (validation.success)
        {
            return new FfmpegResolutionResult
            {
                Found = true,
                IsValid = true,
                Path = exeName,
                Version = ExtractVersionString(validation.output),
                Source = "PATH",
                ValidationOutput = validation.output
            };
        }
        else
        {
            return new FfmpegResolutionResult
            {
                Found = false,
                Source = "PATH",
                Error = "FFmpeg not found on PATH"
            };
        }
    }

    /// <summary>
    /// Validate FFmpeg binary by running -version
    /// </summary>
    private async Task<(bool success, string? output, string? error)> ValidateFFmpegBinaryAsync(
        string ffmpegPath,
        CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return (false, null, "Failed to start FFmpeg process");
            }

            await process.WaitForExitAsync(ct);

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);

            if (process.ExitCode != 0)
            {
                return (false, null, $"Exit code {process.ExitCode}: {stderr}");
            }

            if (string.IsNullOrEmpty(stdout) || !stdout.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                return (false, stdout, "Output does not contain version information");
            }

            return (true, stdout, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Extract version string from ffmpeg -version output
    /// </summary>
    private string? ExtractVersionString(string? output)
    {
        if (string.IsNullOrEmpty(output))
            return null;

        try
        {
            var firstLine = output.Split('\n')[0];
            if (firstLine.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    return parts[2];
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract version string");
        }

        return null;
    }

    /// <summary>
    /// Cache the result with TTL
    /// </summary>
    private void CacheResult(FfmpegResolutionResult result)
    {
        _cache.Set(CacheKey, result, CacheDuration);
        _logger.LogDebug("Cached FFmpeg resolution result for {Duration}", CacheDuration);
    }

    /// <summary>
    /// Clear cached resolution result (call after install/uninstall)
    /// </summary>
    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("FFmpeg resolution cache invalidated");
    }
}

/// <summary>
/// Result of FFmpeg path resolution
/// </summary>
public class FfmpegResolutionResult
{
    public bool Found { get; set; }
    public bool IsValid { get; set; }
    public string? Path { get; set; }
    public string? Version { get; set; }
    public string Source { get; set; } = "None";
    public string? ValidationOutput { get; set; }
    public string? Error { get; set; }
}
