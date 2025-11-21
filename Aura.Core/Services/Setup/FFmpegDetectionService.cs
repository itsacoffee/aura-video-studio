using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Aura.Core.Services.Setup;

/// <summary>
/// Information about detected FFmpeg installation
/// </summary>
public record FFmpegInfo
{
    public bool IsInstalled { get; init; }
    public string? Path { get; init; }
    public string? Version { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Service for detecting FFmpeg installation on the system
/// </summary>
public interface IFFmpegDetectionService
{
    /// <summary>
    /// Detects FFmpeg installation and returns information about it
    /// </summary>
    Task<FFmpegInfo> DetectFFmpegAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached FFmpeg detection status
    /// </summary>
    Task<FFmpegInfo> GetCachedStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of FFmpeg detection service with caching
/// </summary>
public class FFmpegDetectionService : IFFmpegDetectionService
{
    private readonly ILogger<FFmpegDetectionService> _logger;
    private readonly IMemoryCache _cache;
    private readonly Aura.Core.Configuration.FFmpegConfigurationStore? _configStore;
    private const string CacheKey = "ffmpeg:detection";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public FFmpegDetectionService(
        ILogger<FFmpegDetectionService> logger,
        IMemoryCache cache,
        Aura.Core.Configuration.FFmpegConfigurationStore? configStore = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _configStore = configStore;
    }

    public async Task<FFmpegInfo> DetectFFmpegAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting FFmpeg detection");

        try
        {
            // Try to find FFmpeg in various locations
            var ffmpegPath = await FindFFmpegPathAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(ffmpegPath))
            {
                _logger.LogWarning("FFmpeg not found on system");
                var notFoundInfo = new FFmpegInfo
                {
                    IsInstalled = false,
                    Path = null,
                    Version = null,
                    Error = "FFmpeg not found in system PATH or common installation directories"
                };

                // Cache the result
                _cache.Set(CacheKey, notFoundInfo, CacheDuration);
                return notFoundInfo;
            }

            // Get FFmpeg version
            var version = await GetFFmpegVersionAsync(ffmpegPath, cancellationToken).ConfigureAwait(false);

            var info = new FFmpegInfo
            {
                IsInstalled = true,
                Path = ffmpegPath,
                Version = version,
                Error = null
            };

            _logger.LogInformation(
                "FFmpeg detected successfully at {Path}, version: {Version}",
                ffmpegPath,
                version ?? "unknown"
            );

            // Persist detected path to configuration store for future runs
            if (_configStore != null && !string.IsNullOrEmpty(ffmpegPath))
            {
                try
                {
                    var config = new Aura.Core.Configuration.FFmpegConfiguration
                    {
                        Mode = DetermineFFmpegMode(ffmpegPath),
                        Path = ffmpegPath,
                        Version = version,
                        LastValidatedAt = DateTime.UtcNow,
                        LastValidationResult = Aura.Core.Configuration.FFmpegValidationResult.Ok,
                        Source = DetermineFFmpegSource(ffmpegPath),
                        DetectionSourceType = DetermineDetectionSource(ffmpegPath),
                        LastDetectedPath = ffmpegPath,
                        LastDetectedAt = DateTime.UtcNow
                    };
                    
                    await _configStore.SaveAsync(config, cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Persisted detected FFmpeg path to configuration: {Path}", ffmpegPath);
                }
                catch (Exception persistEx)
                {
                    _logger.LogWarning(persistEx, "Failed to persist FFmpeg configuration, continuing with detection");
                }
            }

            // Cache the result
            _cache.Set(CacheKey, info, CacheDuration);
            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting FFmpeg");
            var errorInfo = new FFmpegInfo
            {
                IsInstalled = false,
                Path = null,
                Version = null,
                Error = ex.Message
            };

            // Cache the error result for a shorter duration
            _cache.Set(CacheKey, errorInfo, TimeSpan.FromMinutes(2));
            return errorInfo;
        }
    }

    public async Task<FFmpegInfo> GetCachedStatusAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        if (_cache.TryGetValue(CacheKey, out FFmpegInfo? cachedInfo) && cachedInfo != null)
        {
            _logger.LogDebug("Returning cached FFmpeg detection result");
            return cachedInfo;
        }

        // If not in cache, detect it
        _logger.LogDebug("Cache miss, performing FFmpeg detection");
        return await DetectFFmpegAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> FindFFmpegPathAsync(CancellationToken cancellationToken)
    {
        // 1. Check FFMPEG_PATH environment variable FIRST (set by Electron or user)
        var envPath = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        if (!string.IsNullOrEmpty(envPath))
        {
            // If it's a directory, look for ffmpeg executable within it
            if (Directory.Exists(envPath))
            {
                var ffmpegExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
                var fullPath = Path.Combine(envPath, ffmpegExe);
                if (File.Exists(fullPath))
                {
                    _logger.LogInformation("Found FFmpeg via FFMPEG_PATH environment variable (directory): {Path}", fullPath);
                    return fullPath;
                }
            }
            // If it's a file, use it directly
            else if (File.Exists(envPath))
            {
                _logger.LogInformation("Found FFmpeg via FFMPEG_PATH environment variable (file): {Path}", envPath);
                return envPath;
            }
            else
            {
                _logger.LogWarning("FFMPEG_PATH environment variable set but path does not exist: {Path}", envPath);
            }
        }
        
        // 2. Check application directory
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var appDirFFmpeg = Path.Combine(appDir, "ffmpeg");
        if (File.Exists(appDirFFmpeg))
        {
            _logger.LogDebug("Found FFmpeg in application directory: {Path}", appDirFFmpeg);
            return appDirFFmpeg;
        }

        // 2. Check system PATH using 'which' command (Unix) or 'where' (Windows)
        var pathResult = await FindInSystemPathAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(pathResult))
        {
            _logger.LogDebug("Found FFmpeg in system PATH: {Path}", pathResult);
            return pathResult;
        }

        // 3. Check common Linux installation paths
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var commonPaths = new[]
            {
                "/usr/bin/ffmpeg",
                "/usr/local/bin/ffmpeg",
                "/snap/bin/ffmpeg",
                "/opt/ffmpeg/bin/ffmpeg"
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogDebug("Found FFmpeg at common Linux path: {Path}", path);
                    return path;
                }
            }
        }

        // 4. Check Windows Registry for FFmpeg installations
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var registryPath = FindFfmpegInWindowsRegistry();
            if (!string.IsNullOrEmpty(registryPath))
            {
                _logger.LogDebug("Found FFmpeg via Windows Registry: {Path}", registryPath);
                return registryPath;
            }
        }

        // 5. Check common Windows installation paths
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var commonPaths = new[]
            {
                @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
                @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe",
                @"C:\ffmpeg\bin\ffmpeg.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ffmpeg", "bin", "ffmpeg.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ffmpeg", "bin", "ffmpeg.exe")
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogDebug("Found FFmpeg at common Windows path: {Path}", path);
                    return path;
                }
            }
        }

        return null;
    }

    private async Task<string?> FindInSystemPathAsync(CancellationToken cancellationToken)
    {
        try
        {
            var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
            var argument = "ffmpeg";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = argument,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // 'which' or 'where' returns the path on first line
                var path = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return path;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking system PATH for FFmpeg");
        }

        return null;
    }

    private async Task<string?> GetFFmpegVersionAsync(string ffmpegPath, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // Parse version from output (usually first line: "ffmpeg version X.Y.Z ...")
                var firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(firstLine))
                {
                    // Extract version number using regex
                    var match = Regex.Match(firstLine, @"version\s+([^\s]+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }

                // Fallback: return first line if we can't parse version
                return firstLine?.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting FFmpeg version from {Path}", ffmpegPath);
        }

        return "unknown";
    }

    /// <summary>
    /// Search Windows Registry for FFmpeg installation paths
    /// </summary>
    private string? FindFfmpegInWindowsRegistry()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            // Common registry locations where installers might register FFmpeg
            var registryPaths = new[]
            {
                // HKEY_LOCAL_MACHINE locations
                (@"SOFTWARE\FFmpeg", RegistryHive.LocalMachine),
                (@"SOFTWARE\WOW6432Node\FFmpeg", RegistryHive.LocalMachine),
                (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FFmpeg", RegistryHive.LocalMachine),
                (@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FFmpeg", RegistryHive.LocalMachine),
                
                // HKEY_CURRENT_USER locations
                (@"SOFTWARE\FFmpeg", RegistryHive.CurrentUser),
                (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FFmpeg", RegistryHive.CurrentUser)
            };

            foreach (var (subKey, hive) in registryPaths)
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(subKey);
                
                if (key != null)
                {
                    // Try common value names
                    var valueNames = new[] { "InstallLocation", "InstallPath", "Path", "BinPath" };
                    
                    foreach (var valueName in valueNames)
                    {
                        var value = key.GetValue(valueName) as string;
                        if (!string.IsNullOrEmpty(value))
                        {
                            var ffmpegPath = FindFfmpegExecutableInDirectory(value);
                            if (!string.IsNullOrEmpty(ffmpegPath))
                            {
                                _logger.LogInformation("Found FFmpeg via registry key: {Key}\\{Value}", subKey, valueName);
                                return ffmpegPath;
                            }
                        }
                    }
                }
            }
            
            // Also check 32-bit registry view on 64-bit systems
            if (Environment.Is64BitOperatingSystem)
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                using var key = baseKey.OpenSubKey(@"SOFTWARE\FFmpeg");
                
                if (key != null)
                {
                    var installLocation = key.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrEmpty(installLocation))
                    {
                        var ffmpegPath = FindFfmpegExecutableInDirectory(installLocation);
                        if (!string.IsNullOrEmpty(ffmpegPath))
                        {
                            _logger.LogInformation("Found FFmpeg via 32-bit registry");
                            return ffmpegPath;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error searching Windows Registry for FFmpeg");
        }

        return null;
    }

    /// <summary>
    /// Find ffmpeg.exe in a directory (checking bin subdirectory too)
    /// </summary>
    private string? FindFfmpegExecutableInDirectory(string directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        // Check if directory directly contains ffmpeg.exe
        var directPath = Path.Combine(directory, "ffmpeg.exe");
        if (File.Exists(directPath))
        {
            return directPath;
        }

        // Check bin subdirectory
        var binPath = Path.Combine(directory, "bin", "ffmpeg.exe");
        if (File.Exists(binPath))
        {
            return binPath;
        }

        return null;
    }
    
    /// <summary>
    /// Determine FFmpeg mode based on the path where it was found
    /// </summary>
    private Aura.Core.Configuration.FFmpegMode DetermineFFmpegMode(string ffmpegPath)
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsDir = Path.Combine(localAppData, "Aura", "Tools");
        
        if (ffmpegPath.StartsWith(appDir, StringComparison.OrdinalIgnoreCase))
        {
            return Aura.Core.Configuration.FFmpegMode.Local;
        }
        else if (ffmpegPath.StartsWith(toolsDir, StringComparison.OrdinalIgnoreCase))
        {
            return Aura.Core.Configuration.FFmpegMode.Local;
        }
        else
        {
            return Aura.Core.Configuration.FFmpegMode.System;
        }
    }
    
    /// <summary>
    /// Determine FFmpeg source description based on the path
    /// </summary>
    private string DetermineFFmpegSource(string ffmpegPath)
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsDir = Path.Combine(localAppData, "Aura", "Tools");
        
        if (ffmpegPath.StartsWith(appDir, StringComparison.OrdinalIgnoreCase))
        {
            return "Bundled";
        }
        else if (ffmpegPath.StartsWith(toolsDir, StringComparison.OrdinalIgnoreCase))
        {
            return "Managed";
        }
        else if (IsInSystemPath(ffmpegPath))
        {
            return "PATH";
        }
        else
        {
            return "System";
        }
    }
    
    /// <summary>
    /// Determine detection source type based on the path and environment
    /// </summary>
    private Aura.Core.Configuration.DetectionSource DetermineDetectionSource(string ffmpegPath)
    {
        // Check if path came from environment variable
        var envPath = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        if (!string.IsNullOrEmpty(envPath))
        {
            var normalizedEnvPath = Path.GetFullPath(envPath);
            var normalizedFfmpegPath = Path.GetFullPath(ffmpegPath);
            
            // Check if ffmpegPath is within or matches the env path
            if (normalizedFfmpegPath.Equals(normalizedEnvPath, StringComparison.OrdinalIgnoreCase) ||
                normalizedFfmpegPath.StartsWith(normalizedEnvPath, StringComparison.OrdinalIgnoreCase))
            {
                return Aura.Core.Configuration.DetectionSource.Environment;
            }
        }
        
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var toolsDir = Path.Combine(localAppData, "Aura", "Tools");
        
        if (ffmpegPath.StartsWith(toolsDir, StringComparison.OrdinalIgnoreCase))
        {
            return Aura.Core.Configuration.DetectionSource.Managed;
        }
        else if (IsInSystemPath(ffmpegPath))
        {
            return Aura.Core.Configuration.DetectionSource.System;
        }
        else
        {
            return Aura.Core.Configuration.DetectionSource.System;
        }
    }
    
    /// <summary>
    /// Check if the path is in system PATH
    /// </summary>
    private bool IsInSystemPath(string ffmpegPath)
    {
        try
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
            {
                return false;
            }
            
            var ffmpegDir = Path.GetDirectoryName(ffmpegPath);
            if (string.IsNullOrEmpty(ffmpegDir))
            {
                return false;
            }
            
            var pathDirs = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            return pathDirs.Any(dir => 
                Path.GetFullPath(dir).Equals(Path.GetFullPath(ffmpegDir), StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
