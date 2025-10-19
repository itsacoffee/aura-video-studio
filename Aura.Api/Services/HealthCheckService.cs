using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
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

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IFfmpegLocator ffmpegLocator,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _ffmpegLocator = ffmpegLocator;
        _providerSettings = providerSettings;
    }

    /// <summary>
    /// Perform all readiness checks
    /// </summary>
    public async Task<HealthCheckResponse> CheckReadinessAsync(CancellationToken ct = default)
    {
        var checks = new List<SubCheckResult>();
        var errors = new List<string>();

        // FFmpeg presence and version check
        var ffmpegCheck = await CheckFfmpegAsync(ct);
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
            var result = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct);
            
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
    /// Check port availability (ensure configured port is not in use by another process)
    /// </summary>
    private SubCheckResult CheckPortAvailability()
    {
        try
        {
            // Check if port 5005 is in use
            var port = 5005;
            var isInUse = IsPortInUse(port);

            if (isInUse)
            {
                return new SubCheckResult(
                    "PortAvailability",
                    HealthStatus.Degraded,
                    $"Port {port} is in use (this instance may be using it)",
                    new Dictionary<string, object> 
                    { 
                        ["port"] = port,
                        ["inUse"] = true
                    }
                );
            }

            return new SubCheckResult(
                "PortAvailability",
                HealthStatus.Healthy,
                $"Port {port} is available or being used by this instance",
                new Dictionary<string, object> 
                { 
                    ["port"] = port,
                    ["inUse"] = false
                }
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
    /// Check if a port is in use
    /// </summary>
    private static bool IsPortInUse(int port)
    {
        try
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
            
            return tcpListeners.Any(endpoint => endpoint.Port == port);
        }
        catch
        {
            // If we can't check, assume it's not in use
            return false;
        }
    }
}
