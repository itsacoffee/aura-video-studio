using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Helpers;
using Aura.Api.Models;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// Service for performing health checks on application dependencies and configuration
/// </summary>
public class HealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IFfmpegLocator _ffmpegLocator;
    private readonly ProviderSettings _providerSettings;
    private readonly TtsProviderFactory _ttsProviderFactory;

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IFfmpegLocator ffmpegLocator,
        ProviderSettings providerSettings,
        TtsProviderFactory ttsProviderFactory)
    {
        _logger = logger;
        _ffmpegLocator = ffmpegLocator;
        _providerSettings = providerSettings;
        _ttsProviderFactory = ttsProviderFactory;
    }

    /// <summary>
    /// Perform all readiness checks
    /// </summary>
    public async Task<HealthCheckResponse> CheckReadinessAsync(CancellationToken ct = default)
    {
        var checks = new List<SubCheckResult>();
        var errors = new List<string>();

        // FFmpeg presence and version check
        var ffmpegCheck = await CheckFfmpegAsync(ct).ConfigureAwait(false);
        checks.Add(ffmpegCheck);
        if (ffmpegCheck.Status != HealthStatus.Healthy)
        {
            errors.Add(ffmpegCheck.Message ?? "FFmpeg check failed");
        }

        // Temp directory writable check
        var tempDirCheck = CheckTempDirectoryWritable();
        checks.Add(tempDirCheck);
        if (tempDirCheck.Status != HealthStatus.Healthy)
        {
            errors.Add(tempDirCheck.Message ?? "Temp directory check failed");
        }

        // Provider registry booted check
        var providerCheck = CheckProviderRegistry();
        checks.Add(providerCheck);
        if (providerCheck.Status != HealthStatus.Healthy)
        {
            errors.Add(providerCheck.Message ?? "Provider registry check failed");
        }

        // Port availability check
        var portCheck = CheckPortAvailability();
        checks.Add(portCheck);
        if (portCheck.Status != HealthStatus.Healthy)
        {
            errors.Add(portCheck.Message ?? "Port availability check failed");
        }

        // Disk space check
        var diskSpaceCheck = CheckDiskSpace();
        checks.Add(diskSpaceCheck);
        if (diskSpaceCheck.Status != HealthStatus.Healthy)
        {
            errors.Add(diskSpaceCheck.Message ?? "Disk space check failed");
        }

        // TTS provider check
        var ttsCheck = await CheckTtsProvidersAsync(ct).ConfigureAwait(false);
        checks.Add(ttsCheck);
        if (ttsCheck.Status != HealthStatus.Healthy)
        {
            errors.Add(ttsCheck.Message ?? "TTS provider check failed");
        }

        // Determine overall status
        var status = errors.Count == 0 
            ? HealthStatus.Healthy 
            : checks.All(c => c.Status == HealthStatus.Degraded) 
                ? HealthStatus.Degraded 
                : HealthStatus.Unhealthy;

        return new HealthCheckResponse(status, checks, errors);
    }

    /// <summary>
    /// Perform basic liveness check
    /// </summary>
    public HealthCheckResponse CheckLiveness()
    {
        // Basic liveness - just check if the app is running
        var checks = new List<SubCheckResult>
        {
            new SubCheckResult("Application", HealthStatus.Healthy, "Application is running")
        };

        return new HealthCheckResponse(HealthStatus.Healthy, checks, Array.Empty<string>());
    }

    /// <summary>
    /// Check FFmpeg presence and version
    /// </summary>
    private async Task<SubCheckResult> CheckFfmpegAsync(CancellationToken ct)
    {
        try
        {
            var result = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct).ConfigureAwait(false);
            
            if (!result.Found || string.IsNullOrEmpty(result.FfmpegPath))
            {
                return new SubCheckResult(
                    "FFmpeg",
                    HealthStatus.Unhealthy,
                    result.Reason ?? "FFmpeg not found",
                    new Dictionary<string, object> 
                    { 
                        ["attemptedPaths"] = result.AttemptedPaths
                    }
                );
            }

            return new SubCheckResult(
                "FFmpeg",
                HealthStatus.Healthy,
                $"FFmpeg found at {result.FfmpegPath}",
                new Dictionary<string, object> 
                { 
                    ["path"] = result.FfmpegPath,
                    ["version"] = result.VersionString ?? "unknown"
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking FFmpeg");
            return new SubCheckResult(
                "FFmpeg",
                HealthStatus.Unhealthy,
                $"Error checking FFmpeg: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Check if temp directory is writable
    /// </summary>
    private SubCheckResult CheckTempDirectoryWritable()
    {
        try
        {
            var tempPath = Path.GetTempPath();
            var testFile = Path.Combine(tempPath, $"aura-health-check-{Guid.NewGuid()}.tmp");

            try
            {
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return new SubCheckResult(
                    "TempDirectory",
                    HealthStatus.Healthy,
                    $"Temp directory is writable: {tempPath}",
                    new Dictionary<string, object> { ["path"] = tempPath }
                );
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking temp directory");
            return new SubCheckResult(
                "TempDirectory",
                HealthStatus.Unhealthy,
                $"Temp directory is not writable: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Check provider registry is booted
    /// </summary>
    private SubCheckResult CheckProviderRegistry()
    {
        try
        {
            // Check if critical directories exist
            var toolsDir = _providerSettings.GetToolsDirectory();
            var auraDataDir = _providerSettings.GetAuraDataDirectory();

            if (!Directory.Exists(toolsDir))
            {
                return new SubCheckResult(
                    "ProviderRegistry",
                    HealthStatus.Degraded,
                    $"Tools directory does not exist: {toolsDir}",
                    new Dictionary<string, object> 
                    { 
                        ["toolsDirectory"] = toolsDir,
                        ["exists"] = false
                    }
                );
            }

            if (!Directory.Exists(auraDataDir))
            {
                return new SubCheckResult(
                    "ProviderRegistry",
                    HealthStatus.Degraded,
                    $"AuraData directory does not exist: {auraDataDir}",
                    new Dictionary<string, object> 
                    { 
                        ["auraDataDirectory"] = auraDataDir,
                        ["exists"] = false
                    }
                );
            }

            return new SubCheckResult(
                "ProviderRegistry",
                HealthStatus.Healthy,
                "Provider registry initialized",
                new Dictionary<string, object> 
                { 
                    ["toolsDirectory"] = toolsDir,
                    ["auraDataDirectory"] = auraDataDir
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking provider registry");
            return new SubCheckResult(
                "ProviderRegistry",
                HealthStatus.Unhealthy,
                $"Error checking provider registry: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Check port availability with process ownership detection
    /// </summary>
    private SubCheckResult CheckPortAvailability()
    {
        try
        {
            // Check if port 5005 is in use
            var port = 5005;
            var portCheck = NetworkUtility.CheckPort(port);

            if (!portCheck.IsInUse)
            {
                return new SubCheckResult(
                    "PortAvailability",
                    HealthStatus.Healthy,
                    $"Port {port} is available",
                    new Dictionary<string, object> 
                    { 
                        ["port"] = port,
                        ["inUse"] = false
                    }
                );
            }

            if (portCheck.IsOwnedByCurrentProcess)
            {
                return new SubCheckResult(
                    "PortAvailability",
                    HealthStatus.Healthy,
                    $"Port {port} is being used by this application",
                    new Dictionary<string, object> 
                    { 
                        ["port"] = port,
                        ["inUse"] = true,
                        ["ownedByCurrentProcess"] = true
                    }
                );
            }

            // Port is in use by another process
            var message = NetworkUtility.GetPortStatusMessage(port, portCheck);
            var remediation = NetworkUtility.GetPortRemediationMessage(port, portCheck);

            var details = new Dictionary<string, object> 
            { 
                ["port"] = port,
                ["inUse"] = true,
                ["ownedByCurrentProcess"] = false
            };

            if (portCheck.OwningProcessId.HasValue)
            {
                details["owningProcessId"] = portCheck.OwningProcessId.Value;
            }

            if (portCheck.OwningProcessName != null)
            {
                details["owningProcessName"] = portCheck.OwningProcessName;
            }

            if (!string.IsNullOrEmpty(remediation))
            {
                details["remediation"] = remediation;
            }

            return new SubCheckResult(
                "PortAvailability",
                HealthStatus.Unhealthy,
                message,
                details
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking port availability");
            return new SubCheckResult(
                "PortAvailability",
                HealthStatus.Degraded,
                $"Unable to check port availability: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Check disk space availability
    /// </summary>
    private SubCheckResult CheckDiskSpace()
    {
        try
        {
            var outputDir = _providerSettings.GetOutputDirectory();
            var driveInfo = new DriveInfo(Path.GetPathRoot(outputDir) ?? "/");
            
            var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            var totalSpaceGB = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);
            var freeSpacePercent = (freeSpaceGB / totalSpaceGB) * 100;

            var details = new Dictionary<string, object>
            {
                ["freeSpaceGB"] = Math.Round(freeSpaceGB, 2),
                ["totalSpaceGB"] = Math.Round(totalSpaceGB, 2),
                ["freeSpacePercent"] = Math.Round(freeSpacePercent, 1),
                ["outputDirectory"] = outputDir
            };

            // Critical if less than 1 GB or less than 5% free
            if (freeSpaceGB < 1.0 || freeSpacePercent < 5)
            {
                return new SubCheckResult(
                    "DiskSpace",
                    HealthStatus.Unhealthy,
                    $"Low disk space: {freeSpaceGB:F2} GB free ({freeSpacePercent:F1}% of {totalSpaceGB:F2} GB)",
                    details
                );
            }

            // Warning if less than 5 GB or less than 10% free
            if (freeSpaceGB < 5.0 || freeSpacePercent < 10)
            {
                return new SubCheckResult(
                    "DiskSpace",
                    HealthStatus.Degraded,
                    $"Disk space getting low: {freeSpaceGB:F2} GB free ({freeSpacePercent:F1}% of {totalSpaceGB:F2} GB)",
                    details
                );
            }

            return new SubCheckResult(
                "DiskSpace",
                HealthStatus.Healthy,
                $"Sufficient disk space: {freeSpaceGB:F2} GB free ({freeSpacePercent:F1}% of {totalSpaceGB:F2} GB)",
                details
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking disk space");
            return new SubCheckResult(
                "DiskSpace",
                HealthStatus.Degraded,
                $"Unable to check disk space: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Check TTS provider availability
    /// </summary>
    private async Task<SubCheckResult> CheckTtsProvidersAsync(CancellationToken ct)
    {
        try
        {
            var availableProviders = _ttsProviderFactory.CreateAvailableProviders();
            
            if (availableProviders.Count == 0)
            {
                return new SubCheckResult(
                    "TtsProviders",
                    HealthStatus.Degraded,
                    "No TTS providers available",
                    new Dictionary<string, object>
                    {
                        ["availableProviders"] = Array.Empty<string>(),
                        ["count"] = 0
                    }
                );
            }

            var providerNames = availableProviders.Keys.ToList();
            return new SubCheckResult(
                "TtsProviders",
                HealthStatus.Healthy,
                $"{providerNames.Count} TTS provider(s) available: {string.Join(", ", providerNames)}",
                new Dictionary<string, object>
                {
                    ["availableProviders"] = providerNames.ToArray(),
                    ["count"] = providerNames.Count
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking TTS providers");
            return new SubCheckResult(
                "TtsProviders",
                HealthStatus.Degraded,
                $"Unable to check TTS providers: {ex.Message}"
            );
        }
    }
}
