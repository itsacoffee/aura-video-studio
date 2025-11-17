using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
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
    private readonly FFmpegConfigurationStore? _configStore;
    private readonly string _managedInstallRoot;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly string CacheKey = "ffmpeg-resolution-result";

    public FFmpegResolver(
        ILogger<FFmpegResolver> logger,
        IMemoryCache cache,
        FFmpegConfigurationStore? configStore = null)
    {
        _logger = logger;
        _cache = cache;
        _configStore = configStore;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _managedInstallRoot = Path.Combine(localAppData, "Aura", "Tools", "ffmpeg");
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
        var envOverridePaths = GetEnvironmentOverridePaths();
        var hasEnvOverrides = envOverridePaths.Count > 0;
        
        if (hasEnvOverrides)
        {
            _logger.LogInformation("Environment FFmpeg overrides detected: {Paths}", string.Join(", ", envOverridePaths));
        }

        if (!forceRefresh && _cache.TryGetValue(CacheKey, out FfmpegResolutionResult? cached))
        {
            if (!hasEnvOverrides)
            {
                _logger.LogDebug("Returning cached FFmpeg resolution result");
                return cached!;
            }

            if (cached != null && cached.Source == "Environment" && cached.Found && cached.IsValid)
            {
                _logger.LogDebug("Returning cached environment FFmpeg resolution result");
                return cached;
            }

            _logger.LogDebug("Environment overrides detected, bypassing cached FFmpeg result");
        }

        _logger.LogInformation("Resolving FFmpeg path with precedence: Environment > Managed > Configured > PATH");

        var attemptedPaths = new List<string>();
        FfmpegResolutionResult result;

        try
        {
            // 0. Environment overrides (Electron desktop bundle, CI, etc.)
            result = await CheckEnvironmentOverridesAsync(envOverridePaths, ct).ConfigureAwait(false);
            attemptedPaths.AddRange(result.AttemptedPaths);
            if (result.Found && result.IsValid)
            {
                _logger.LogInformation("Using environment FFmpeg path: {Path}", result.Path);
                result.AttemptedPaths = attemptedPaths;
                CacheResult(result);
                return result;
            }

            // 1. Check managed install first (highest priority)
            result = await CheckManagedInstallAsync(ct).ConfigureAwait(false);
            attemptedPaths.AddRange(result.AttemptedPaths);
            if (result.Found && result.IsValid)
            {
                _logger.LogInformation("Using managed FFmpeg install: {Path}", result.Path);
                result.AttemptedPaths = attemptedPaths;
                CacheResult(result);
                return result;
            }

            // 2. Check user-configured path
            if (!string.IsNullOrEmpty(configuredPath))
            {
                result = await CheckConfiguredPathAsync(configuredPath, ct).ConfigureAwait(false);
                attemptedPaths.AddRange(result.AttemptedPaths);
                if (result.Found && result.IsValid)
                {
                    _logger.LogInformation("Using configured FFmpeg path: {Path}", result.Path);
                    result.AttemptedPaths = attemptedPaths;
                    CacheResult(result);
                    return result;
                }
            }

            // 3. Check PATH environment
            result = await CheckPathEnvironmentAsync(ct).ConfigureAwait(false);
            attemptedPaths.AddRange(result.AttemptedPaths);
            if (result.Found && result.IsValid)
            {
                _logger.LogInformation("Using FFmpeg from PATH");
                result.AttemptedPaths = attemptedPaths;
                CacheResult(result);
                return result;
            }

            // Nothing found
            result = new FfmpegResolutionResult
            {
                Found = false,
                IsValid = false,
                Source = "None",
                Error = "FFmpeg not found in any location. Install managed FFmpeg or configure path in Settings.",
                AttemptedPaths = attemptedPaths
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
                Error = $"Error during FFmpeg resolution: {ex.Message}",
                AttemptedPaths = attemptedPaths
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

        var attemptedPaths = new List<string> { _managedInstallRoot };

        if (!Directory.Exists(_managedInstallRoot))
        {
            return new FfmpegResolutionResult
            {
                Found = false,
                Source = "Managed",
                Error = "Managed install directory does not exist",
                AttemptedPaths = attemptedPaths
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
                Error = "No managed FFmpeg versions installed",
                AttemptedPaths = attemptedPaths
            };
        }

        // Sort by directory name (descending) to get latest version
        Array.Sort(versionDirs, StringComparer.OrdinalIgnoreCase);
        Array.Reverse(versionDirs);

        foreach (var versionDir in versionDirs)
        {
            var manifestPath = Path.Combine(versionDir, "install.json");
            attemptedPaths.Add(manifestPath);
            
            if (!File.Exists(manifestPath))
            {
                _logger.LogDebug("No manifest in {Dir}, skipping", versionDir);
                continue;
            }

            try
            {
                var manifestJson = await File.ReadAllTextAsync(manifestPath, ct).ConfigureAwait(false);
                var manifest = JsonSerializer.Deserialize<FfmpegInstallMetadata>(manifestJson);

                if (manifest == null || string.IsNullOrEmpty(manifest.FfmpegPath))
                {
                    _logger.LogWarning("Invalid manifest in {Dir}: manifest is null or missing FFmpegPath", versionDir);
                    continue;
                }

                attemptedPaths.Add(manifest.FfmpegPath);

                if (!File.Exists(manifest.FfmpegPath))
                {
                    _logger.LogWarning("Manifest points to missing file: {Path}", manifest.FfmpegPath);
                    continue;
                }

                // Validate the binary
                var validation = await ValidateFFmpegBinaryAsync(manifest.FfmpegPath, ct).ConfigureAwait(false);
                if (validation.success)
                {
                    return new FfmpegResolutionResult
                    {
                        Found = true,
                        IsValid = true,
                        Path = manifest.FfmpegPath,
                        Version = manifest.Version,
                        Source = "Managed",
                        ValidationOutput = validation.output,
                        AttemptedPaths = attemptedPaths
                    };
                }
                else
                {
                    _logger.LogWarning("Managed FFmpeg validation failed: {Error}", validation.error);
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "Corrupted manifest file in {Dir}. Delete this directory and reinstall FFmpeg.", versionDir);
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
            Error = "No valid managed FFmpeg installations found",
            AttemptedPaths = attemptedPaths
        };
    }

    /// <summary>
    /// Check user-configured path
    /// </summary>
    private async Task<FfmpegResolutionResult> CheckConfiguredPathAsync(string configuredPath, CancellationToken ct)
    {
        _logger.LogDebug("Checking configured path: {Path}", configuredPath);

        var attemptedPaths = new List<string> { configuredPath };
        string resolvedPath;

        // Handle the case where configuredPath is just "ffmpeg" (indicates PATH lookup previously worked)
        if (configuredPath == "ffmpeg" || configuredPath == "ffmpeg.exe")
        {
            _logger.LogDebug("Configured path is '{Path}', treating as PATH lookup", configuredPath);
            return await CheckPathEnvironmentAsync(ct).ConfigureAwait(false);
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
            attemptedPaths.Add(exePath);

            if (File.Exists(exePath))
            {
                resolvedPath = exePath;
            }
            else
            {
                var binPath = Path.Combine(configuredPath, "bin", exeName);
                attemptedPaths.Add(binPath);
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
                        Error = $"FFmpeg executable not found in configured directory: {configuredPath}",
                        AttemptedPaths = attemptedPaths
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
                Error = $"Configured path does not exist: {configuredPath}",
                AttemptedPaths = attemptedPaths
            };
        }

        var validation = await ValidateFFmpegBinaryAsync(resolvedPath, ct).ConfigureAwait(false);
        if (validation.success)
        {
            return new FfmpegResolutionResult
            {
                Found = true,
                IsValid = true,
                Path = resolvedPath,
                Version = ExtractVersionString(validation.output),
                Source = "Configured",
                ValidationOutput = validation.output,
                AttemptedPaths = attemptedPaths
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
                Error = $"FFmpeg validation failed: {validation.error}",
                AttemptedPaths = attemptedPaths
            };
        }
    }

    /// <summary>
    /// Check PATH environment variable and common installation directories
    /// </summary>
    private async Task<FfmpegResolutionResult> CheckPathEnvironmentAsync(CancellationToken ct)
    {
        _logger.LogDebug("Checking PATH environment and common directories for FFmpeg");

        var attemptedPaths = new List<string>();
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";

        // First, try PATH
        attemptedPaths.Add($"PATH/{exeName}");
        var validation = await ValidateFFmpegBinaryAsync(exeName, ct).ConfigureAwait(false);
        if (validation.success)
        {
            return new FfmpegResolutionResult
            {
                Found = true,
                IsValid = true,
                Path = exeName,
                Version = ExtractVersionString(validation.output),
                Source = "PATH",
                ValidationOutput = validation.output,
                AttemptedPaths = attemptedPaths
            };
        }

        // If not found on PATH, check common installation directories (Windows only)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var commonPaths = new List<string>
            {
                @"C:\ffmpeg\bin\ffmpeg.exe",
                @"C:\ffmpeg\ffmpeg.exe",
                @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
                @"C:\Program Files\ffmpeg\ffmpeg.exe",
                @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe",
                @"C:\Program Files (x86)\ffmpeg\ffmpeg.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ffmpeg", "bin", "ffmpeg.exe"),
            };

            // Also check Chocolatey install location
            var chocolateyPath = Environment.GetEnvironmentVariable("ChocolateyInstall");
            if (!string.IsNullOrEmpty(chocolateyPath))
            {
                commonPaths.Add(Path.Combine(chocolateyPath, "bin", "ffmpeg.exe"));
            }

            // Check each common path
            foreach (var path in commonPaths.Distinct())
            {
                attemptedPaths.Add(path);
                if (!File.Exists(path))
                    continue;

                _logger.LogDebug("Checking common path: {Path}", path);
                var pathValidation = await ValidateFFmpegBinaryAsync(path, ct).ConfigureAwait(false);
                if (pathValidation.success)
                {
                    _logger.LogInformation("Found FFmpeg at common installation path: {Path}", path);
                    return new FfmpegResolutionResult
                    {
                        Found = true,
                        IsValid = true,
                        Path = path,
                        Version = ExtractVersionString(pathValidation.output),
                        Source = "Common Directory",
                        ValidationOutput = pathValidation.output,
                        AttemptedPaths = attemptedPaths
                    };
                }
            }
        }

        return new FfmpegResolutionResult
        {
            Found = false,
            Source = "PATH",
            Error = "FFmpeg not found on PATH or common installation directories",
            AttemptedPaths = attemptedPaths
        };
    }

    /// <summary>
    /// Check environment-provided overrides such as Electron's bundled FFmpeg.
    /// </summary>
    private async Task<FfmpegResolutionResult> CheckEnvironmentOverridesAsync(
        IReadOnlyCollection<string> envPaths,
        CancellationToken ct)
    {
        var attemptedPaths = new List<string>();
        
        if (envPaths.Count == 0)
        {
            _logger.LogDebug("No environment FFmpeg overrides configured");
            return new FfmpegResolutionResult
            {
                Found = false,
                IsValid = false,
                Source = "Environment",
                AttemptedPaths = attemptedPaths,
                Error = "No environment FFmpeg overrides configured."
            };
        }

        foreach (var envPath in envPaths)
        {
            _logger.LogInformation("Checking environment FFmpeg override path: {Path}", envPath);
            attemptedPaths.Add(envPath);

            var result = await CheckConfiguredPathAsync(envPath, ct).ConfigureAwait(false);
            result.Source = "Environment";
            result.AttemptedPaths = new List<string>(attemptedPaths);

            if (result.Found && result.IsValid)
            {
                _logger.LogInformation("Found valid FFmpeg via environment variable: {Path}", result.Path);
                return result;
            }
            else
            {
                _logger.LogWarning("Environment path {Path} did not contain valid FFmpeg: {Error}", envPath, result.Error);
            }
        }

        return new FfmpegResolutionResult
        {
            Found = false,
            IsValid = false,
            Source = "Environment",
            AttemptedPaths = attemptedPaths,
            Error = envPaths.Count > 0
                ? "Environment FFmpeg overrides were invalid."
                : "No environment FFmpeg overrides configured."
        };
    }

    private static IReadOnlyList<string> GetEnvironmentOverridePaths()
    {
        var paths = new[]
            {
                Environment.GetEnvironmentVariable("FFMPEG_PATH"),
                Environment.GetEnvironmentVariable("FFMPEG_BINARIES_PATH"),
                Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH")
            }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        
        return paths;
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

            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);

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
    
    /// <summary>
    /// Persist resolution result to configuration store
    /// </summary>
    public async Task PersistConfigurationAsync(
        FfmpegResolutionResult result,
        FFmpegMode mode,
        CancellationToken ct = default)
    {
        if (_configStore == null)
        {
            _logger.LogWarning("Configuration store not available, cannot persist");
            return;
        }
        
        var config = new FFmpegConfiguration
        {
            Mode = mode,
            Path = result.Path,
            Version = result.Version,
            LastValidatedAt = result.IsValid ? DateTime.UtcNow : null,
            LastValidationResult = result.IsValid
                ? FFmpegValidationResult.Ok
                : string.IsNullOrEmpty(result.Path)
                    ? FFmpegValidationResult.NotFound
                    : FFmpegValidationResult.InvalidBinary,
            LastValidationError = result.Error,
            Source = result.Source,
            ValidationOutput = result.ValidationOutput
        };
        
        await _configStore.SaveAsync(config, ct).ConfigureAwait(false);
        _logger.LogInformation(
            "Persisted FFmpeg configuration: Mode={Mode}, Path={Path}",
            mode,
            result.Path ?? "null"
        );
    }
    
    /// <summary>
    /// Load persisted configuration
    /// </summary>
    public async Task<FFmpegConfiguration?> LoadConfigurationAsync(CancellationToken ct = default)
    {
        if (_configStore == null)
        {
            return null;
        }
        
        return await _configStore.LoadAsync(ct).ConfigureAwait(false);
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
    public List<string> AttemptedPaths { get; set; } = new();
}
