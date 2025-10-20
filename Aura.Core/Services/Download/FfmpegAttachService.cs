using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Download;

/// <summary>
/// Service for attaching existing FFmpeg installations
/// </summary>
public class FfmpegAttachService
{
    private readonly ILogger<FfmpegAttachService> _logger;
    private readonly FileVerificationService _verificationService;

    public FfmpegAttachService(
        ILogger<FfmpegAttachService> logger,
        FileVerificationService verificationService)
    {
        _logger = logger;
        _verificationService = verificationService;
    }

    /// <summary>
    /// Scan the system for existing FFmpeg installations
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of detected FFmpeg installations</returns>
    public async Task<List<FfmpegInstallation>> ScanForInstallationsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Scanning for existing FFmpeg installations");

        var installations = new List<FfmpegInstallation>();
        var searchPaths = GetSearchPaths();

        foreach (var searchPath in searchPaths)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (!Directory.Exists(searchPath))
                {
                    continue;
                }

                _logger.LogDebug("Scanning directory: {Path}", searchPath);

                var ffmpegFiles = Directory.GetFiles(
                    searchPath,
                    "ffmpeg.exe",
                    SearchOption.AllDirectories);

                foreach (var ffmpegPath in ffmpegFiles)
                {
                    var installation = await DetectInstallationAsync(ffmpegPath, ct);
                    if (installation != null)
                    {
                        installations.Add(installation);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogDebug(ex, "Access denied to path: {Path}", searchPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scanning path: {Path}", searchPath);
            }
        }

        _logger.LogInformation("Found {Count} FFmpeg installations", installations.Count);
        return installations;
    }

    /// <summary>
    /// Detect FFmpeg installation details from a binary path
    /// </summary>
    /// <param name="ffmpegPath">Path to ffmpeg.exe</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Installation details or null if invalid</returns>
    public async Task<FfmpegInstallation?> DetectInstallationAsync(
        string ffmpegPath,
        CancellationToken ct = default)
    {
        if (!File.Exists(ffmpegPath))
        {
            return null;
        }

        _logger.LogDebug("Detecting FFmpeg at: {Path}", ffmpegPath);

        try
        {
            // Get version info
            var versionInfo = await GetVersionInfoAsync(ffmpegPath, ct);
            if (versionInfo == null)
            {
                _logger.LogDebug("Could not get version info for: {Path}", ffmpegPath);
                return null;
            }

            // Check for ffprobe
            var directory = Path.GetDirectoryName(ffmpegPath);
            var ffprobePath = directory != null
                ? Path.Combine(directory, "ffprobe.exe")
                : null;

            var hasFFprobe = ffprobePath != null && File.Exists(ffprobePath);

            // Compute checksum
            string? checksum = null;
            try
            {
                checksum = await _verificationService.ComputeSha256Async(ffmpegPath, ct);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not compute checksum for: {Path}", ffmpegPath);
            }

            return new FfmpegInstallation
            {
                FfmpegPath = ffmpegPath,
                FfprobePath = hasFFprobe ? ffprobePath : null,
                InstallDirectory = directory ?? string.Empty,
                Version = versionInfo.Version,
                VersionString = versionInfo.VersionString,
                BuildConfiguration = versionInfo.BuildConfiguration,
                Checksum = checksum,
                IsValid = true,
                DetectedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting FFmpeg at: {Path}", ffmpegPath);
            return null;
        }
    }

    /// <summary>
    /// Validate that an FFmpeg installation is functional
    /// </summary>
    /// <param name="installation">Installation to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    public async Task<FfmpegValidationResult> ValidateInstallationAsync(
        FfmpegInstallation installation,
        CancellationToken ct = default)
    {
        if (installation == null)
        {
            throw new ArgumentNullException(nameof(installation));
        }

        _logger.LogInformation("Validating FFmpeg installation: {Path}", installation.FfmpegPath);

        var result = new FfmpegValidationResult
        {
            Installation = installation
        };

        try
        {
            // Test 1: Version check
            var versionInfo = await GetVersionInfoAsync(installation.FfmpegPath, ct);
            if (versionInfo == null)
            {
                result.IsValid = false;
                result.ErrorMessage = "Could not retrieve version information";
                return result;
            }

            result.VersionCheckPassed = true;

            // Test 2: Smoke test (generate short silent audio)
            var smokeTestResult = await RunSmokeTestAsync(installation.FfmpegPath, ct);
            result.SmokeTestPassed = smokeTestResult.Success;
            if (!smokeTestResult.Success)
            {
                result.ErrorMessage = $"Smoke test failed: {smokeTestResult.Error}";
                result.IsValid = false;
                return result;
            }

            // Test 3: Check FFprobe if present
            if (!string.IsNullOrEmpty(installation.FfprobePath))
            {
                var ffprobeInfo = await GetVersionInfoAsync(installation.FfprobePath, ct);
                result.FFprobeAvailable = ffprobeInfo != null;
            }

            result.IsValid = true;
            _logger.LogInformation("FFmpeg validation successful: {Path}", installation.FfmpegPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg validation failed: {Path}", installation.FfmpegPath);
            result.IsValid = false;
            result.ErrorMessage = $"Validation error: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Check if an FFmpeg installation is compatible with the required version
    /// </summary>
    /// <param name="installation">Installation to check</param>
    /// <param name="minimumVersion">Minimum required version (e.g., "5.0")</param>
    /// <returns>True if compatible, false otherwise</returns>
    public bool IsCompatible(FfmpegInstallation installation, string? minimumVersion = null)
    {
        if (installation == null || string.IsNullOrEmpty(installation.Version))
        {
            return false;
        }

        // If no minimum version specified, any valid installation is compatible
        if (string.IsNullOrEmpty(minimumVersion))
        {
            return true;
        }

        // Try to parse version numbers
        if (TryParseVersion(installation.Version, out var installVersion) &&
            TryParseVersion(minimumVersion, out var minVersion))
        {
            return installVersion >= minVersion;
        }

        // If we can't parse versions, assume compatible
        _logger.LogDebug("Could not parse versions for compatibility check: {Version} vs {MinVersion}",
            installation.Version, minimumVersion);
        return true;
    }

    /// <summary>
    /// Get common search paths for FFmpeg installations
    /// </summary>
    private List<string> GetSearchPaths()
    {
        var paths = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Common Windows locations
            paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg"));
            paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ffmpeg"));
            paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg"));
            
            // Check PATH environment variable
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathVar))
            {
                var pathDirs = pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
                paths.AddRange(pathDirs.Where(Directory.Exists));
            }
        }

        return paths.Distinct().ToList();
    }

    /// <summary>
    /// Get version information from an FFmpeg binary
    /// </summary>
    private async Task<FfmpegVersionInfo?> GetVersionInfoAsync(string binaryPath, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = binaryPath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(ct);

            return ParseVersionInfo(output);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting version info from: {Path}", binaryPath);
            return null;
        }
    }

    /// <summary>
    /// Parse FFmpeg version information from -version output
    /// </summary>
    private FfmpegVersionInfo? ParseVersionInfo(string output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return null;
        }

        try
        {
            var firstLine = output.Split('\n')[0];
            if (!firstLine.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                return null;
            }

            var versionString = parts[2];
            var version = ExtractVersionNumber(versionString);
            var buildConfig = output.Contains("--enable", StringComparison.OrdinalIgnoreCase)
                ? "custom"
                : "standard";

            return new FfmpegVersionInfo
            {
                Version = version,
                VersionString = versionString,
                BuildConfiguration = buildConfig
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing version info");
            return null;
        }
    }

    /// <summary>
    /// Extract version number from version string (e.g., "N-111617-gdd5a56c1b5" -> "7.0")
    /// </summary>
    private string ExtractVersionNumber(string versionString)
    {
        // Try to extract major.minor version
        var parts = versionString.Split('-', '.', 'n', 'N');
        foreach (var part in parts)
        {
            if (int.TryParse(part, out var number) && number > 0 && number < 20)
            {
                return $"{number}.0";
            }
        }

        // If we can't parse it, return the original string
        return versionString;
    }

    /// <summary>
    /// Run a smoke test to ensure FFmpeg can process media
    /// </summary>
    private async Task<(bool Success, string? Error)> RunSmokeTestAsync(
        string ffmpegPath,
        CancellationToken ct)
    {
        var tempOut = Path.Combine(Path.GetTempPath(), $"ffmpeg_smoke_test_{Guid.NewGuid():N}.wav");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-hide_banner -loglevel error -f lavfi -i anullsrc=cl=stereo:r=48000 -t 0.2 -y \"{tempOut}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return (false, "Failed to start FFmpeg process");
            }

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(ct);
                return (false, $"Exit code {process.ExitCode}: {stderr}");
            }

            if (!File.Exists(tempOut))
            {
                return (false, "Output file was not created");
            }

            var fileInfo = new FileInfo(tempOut);
            if (fileInfo.Length < 100)
            {
                return (false, $"Output file too small ({fileInfo.Length} bytes)");
            }

            return (true, null);
        }
        catch (OperationCanceledException)
        {
            return (false, "Smoke test timed out");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            try
            {
                if (File.Exists(tempOut))
                {
                    File.Delete(tempOut);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Try to parse a version string into a comparable version object
    /// </summary>
    private bool TryParseVersion(string versionString, out Version version)
    {
        version = new Version();

        // Try direct parsing first
        if (Version.TryParse(versionString, out version))
        {
            return true;
        }

        // Try extracting major.minor from complex version strings
        var parts = versionString.Split('.', '-', 'n', 'N');
        if (parts.Length >= 1 && int.TryParse(parts[0], out var major))
        {
            var minor = 0;
            if (parts.Length >= 2)
            {
                int.TryParse(parts[1], out minor);
            }
            version = new Version(major, minor);
            return true;
        }

        return false;
    }
}

/// <summary>
/// Information about an FFmpeg installation
/// </summary>
public class FfmpegInstallation
{
    public string FfmpegPath { get; set; } = string.Empty;
    public string? FfprobePath { get; set; }
    public string InstallDirectory { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string VersionString { get; set; } = string.Empty;
    public string? BuildConfiguration { get; set; }
    public string? Checksum { get; set; }
    public bool IsValid { get; set; }
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// Version information parsed from FFmpeg
/// </summary>
public class FfmpegVersionInfo
{
    public string Version { get; set; } = string.Empty;
    public string VersionString { get; set; } = string.Empty;
    public string BuildConfiguration { get; set; } = string.Empty;
}

/// <summary>
/// Result of FFmpeg installation validation
/// </summary>
public class FfmpegValidationResult
{
    public FfmpegInstallation? Installation { get; set; }
    public bool IsValid { get; set; }
    public bool VersionCheckPassed { get; set; }
    public bool SmokeTestPassed { get; set; }
    public bool FFprobeAvailable { get; set; }
    public string? ErrorMessage { get; set; }
}
