using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Validation;

/// <summary>
/// Configuration for validation timeout settings
/// </summary>
public class ValidationTimeoutSettings
{
    /// <summary>
    /// Timeout for FFmpeg check in seconds
    /// </summary>
    public int FfmpegCheckTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Timeout for provider check in seconds
    /// </summary>
    public int ProviderCheckTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Timeout for hardware detection in seconds
    /// </summary>
    public int HardwareCheckTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Total validation timeout in seconds
    /// </summary>
    public int TotalValidationTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Validates system readiness before starting video generation
/// </summary>
public class PreGenerationValidator
{
    private readonly ILogger<PreGenerationValidator> _logger;
    private readonly IFfmpegLocator _ffmpegLocator;
    private readonly FFmpegResolver _ffmpegResolver;
    private readonly IHardwareDetector _hardwareDetector;
    private readonly IProviderReadinessService _providerReadiness;
    private readonly ValidationTimeoutSettings _timeoutSettings;

    public PreGenerationValidator(
        ILogger<PreGenerationValidator> logger,
        IFfmpegLocator ffmpegLocator,
        FFmpegResolver ffmpegResolver,
        IHardwareDetector hardwareDetector,
        IProviderReadinessService providerReadiness,
        ValidationTimeoutSettings? timeoutSettings = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ffmpegLocator = ffmpegLocator ?? throw new ArgumentNullException(nameof(ffmpegLocator));
        _ffmpegResolver = ffmpegResolver ?? throw new ArgumentNullException(nameof(ffmpegResolver));
        _hardwareDetector = hardwareDetector ?? throw new ArgumentNullException(nameof(hardwareDetector));
        _providerReadiness = providerReadiness ?? throw new ArgumentNullException(nameof(providerReadiness));
        _timeoutSettings = timeoutSettings ?? new ValidationTimeoutSettings();
    }

    /// <summary>
    /// Executes a task with a timeout, returning a default value if timeout occurs
    /// </summary>
    private async Task<(T? Result, bool TimedOut)> ExecuteWithTimeoutAsync<T>(
        Task<T> task,
        TimeSpan timeout,
        string operationName,
        CancellationToken ct)
    {
        try
        {
            // Use WaitAsync with both timeout and cancellation token
            // WaitAsync will throw TimeoutException if timeout expires
            // or OperationCanceledException if ct is cancelled
            var result = await task.WaitAsync(timeout, ct).ConfigureAwait(false);
            return (result, false);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("{Operation} timed out after {Timeout}s", operationName, timeout.TotalSeconds);
            return (default, true);
        }
    }

    /// <summary>
    /// Validates system readiness for video generation
    /// </summary>
    /// <param name="brief">The video brief</param>
    /// <param name="planSpec">The plan specification</param>
    /// <param name="progress">Optional progress reporter for validation sub-steps</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with issues if any</returns>
    public virtual async Task<ValidationResult> ValidateSystemReadyAsync(
        Brief brief,
        PlanSpec planSpec,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var issues = new List<string>();

        // FFmpeg validation with timeout
        progress?.Report("Validating FFmpeg installation...");
        try
        {
            var ffmpegTimeout = TimeSpan.FromSeconds(_timeoutSettings.FfmpegCheckTimeoutSeconds);
            var (resolutionResult, timedOut) = await ExecuteWithTimeoutAsync(
                _ffmpegResolver.ResolveAsync(null, forceRefresh: false, ct),
                ffmpegTimeout,
                "FFmpeg resolution",
                ct).ConfigureAwait(false);

            if (timedOut)
            {
                issues.Add($"FFmpeg validation timed out after {_timeoutSettings.FfmpegCheckTimeoutSeconds} seconds. This may indicate FFmpeg is not properly installed or is taking too long to respond.");
            }
            else if (resolutionResult == null || !resolutionResult.Found || !resolutionResult.IsValid)
            {
                issues.Add("FFmpeg is required but not found or invalid. Install managed FFmpeg or configure the path in Settings.");
                _logger.LogWarning("FFmpeg validation failed: {Error}", resolutionResult?.Error ?? "Not found");
            }
            else if (!string.IsNullOrEmpty(resolutionResult.Path))
            {
                // Double-check file existence for non-PATH sources
                if (resolutionResult.Source != "PATH" && !File.Exists(resolutionResult.Path))
                {
                    issues.Add($"FFmpeg path configured but file does not exist: {resolutionResult.Path}. Install FFmpeg or update the path in Settings.");
                    _logger.LogWarning("FFmpeg validation failed: Path does not exist - {Path}", resolutionResult.Path);
                }
                else
                {
                    _logger.LogInformation("FFmpeg validation passed: {Path} (Source: {Source})",
                        resolutionResult.Path, resolutionResult.Source);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // Rethrow if user cancelled
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking FFmpeg availability");
            issues.Add("Unable to verify FFmpeg installation. Please ensure FFmpeg is installed and accessible.");
        }

        // Validate disk space
        progress?.Report("Checking available disk space...");
        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AuraVideos"
            );

            // Get the drive for the output path
            var rootPath = Path.GetPathRoot(outputPath);
            if (!string.IsNullOrEmpty(rootPath))
            {
                var driveInfo = new DriveInfo(rootPath);
                if (driveInfo.IsReady)
                {
                    double freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    _logger.LogInformation("Disk space check: {FreeSpace:F2}GB available on {Drive}", freeSpaceGB, rootPath);

                    if (freeSpaceGB < 1.0)
                    {
                        issues.Add($"Insufficient disk space on {rootPath}: {freeSpaceGB:F1}GB free, need at least 1GB. Free up disk space and try again.");
                        _logger.LogWarning("Disk space validation failed: {FreeSpace:F2}GB < 1GB", freeSpaceGB);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking disk space");
            // Don't fail validation if we can't check disk space
        }

        // Validate Brief topic
        progress?.Report("Validating brief content...");
        if (string.IsNullOrWhiteSpace(brief.Topic))
        {
            issues.Add("Topic is required. Please provide a topic for your video.");
            _logger.LogWarning("Brief validation failed: Topic is null or empty");
        }
        else if (brief.Topic.Trim().Length < 3)
        {
            issues.Add($"Topic '{brief.Topic}' is too short (minimum 3 characters). Please provide a more descriptive topic.");
            _logger.LogWarning("Brief validation failed: Topic too short - '{Topic}'", brief.Topic);
        }
        else
        {
            _logger.LogInformation("Brief topic validation passed: '{Topic}'", brief.Topic);
        }

        // Validate PlanSpec duration
        if (planSpec.TargetDuration.TotalSeconds < 10)
        {
            issues.Add($"Duration {planSpec.TargetDuration.TotalSeconds:F1}s is too short. Minimum duration is 10 seconds.");
            _logger.LogWarning("Duration validation failed: {Duration}s < 10s", planSpec.TargetDuration.TotalSeconds);
        }
        else if (planSpec.TargetDuration.TotalMinutes > 30)
        {
            issues.Add($"Duration {planSpec.TargetDuration.TotalMinutes:F1} minutes is too long. Maximum duration is 30 minutes.");
            _logger.LogWarning("Duration validation failed: {Duration}min > 30min", planSpec.TargetDuration.TotalMinutes);
        }
        else
        {
            _logger.LogInformation("Duration validation passed: {Duration:F1}s", planSpec.TargetDuration.TotalSeconds);
        }

        // Validate system hardware with timeout
        progress?.Report("Detecting system hardware...");
        try
        {
            var hardwareTimeout = TimeSpan.FromSeconds(_timeoutSettings.HardwareCheckTimeoutSeconds);
            var (systemProfile, hardwareTimedOut) = await ExecuteWithTimeoutAsync(
                _hardwareDetector.DetectSystemAsync(),
                hardwareTimeout,
                "Hardware detection",
                ct).ConfigureAwait(false);

            if (hardwareTimedOut)
            {
                _logger.LogWarning("Hardware detection timed out after {Timeout}s, continuing with default assumptions", 
                    _timeoutSettings.HardwareCheckTimeoutSeconds);
                // Don't fail validation if hardware detection times out - continue with defaults
            }
            else if (systemProfile != null)
            {
                _logger.LogInformation("System hardware detected: {Cores} cores, {Ram}GB RAM",
                    systemProfile.LogicalCores, systemProfile.RamGB);

                // Check CPU cores
                if (systemProfile.LogicalCores < 2)
                {
                    issues.Add($"Insufficient CPU cores: {systemProfile.LogicalCores} core(s) detected, need at least 2 cores for video generation.");
                    _logger.LogWarning("Hardware validation failed: {Cores} cores < 2", systemProfile.LogicalCores);
                }

                // Check RAM
                if (systemProfile.RamGB < 4)
                {
                    issues.Add($"Insufficient RAM: {systemProfile.RamGB:F1}GB detected, need at least 4GB for video generation.");
                    _logger.LogWarning("Hardware validation failed: {Ram}GB < 4GB", systemProfile.RamGB);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // Rethrow if user cancelled
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting hardware");
            // Don't fail validation if we can't detect hardware
        }

        // Validate provider readiness (LLM, TTS, Images) with timeout
        progress?.Report("Validating provider configuration...");
        try
        {
            var providerTimeout = TimeSpan.FromSeconds(_timeoutSettings.ProviderCheckTimeoutSeconds);
            var (readiness, providerTimedOut) = await ExecuteWithTimeoutAsync(
                _providerReadiness.ValidateRequiredProvidersAsync(ct),
                providerTimeout,
                "Provider readiness validation",
                ct).ConfigureAwait(false);

            if (providerTimedOut)
            {
                issues.Add($"Provider validation timed out after {_timeoutSettings.ProviderCheckTimeoutSeconds} seconds. Check network connectivity and provider API availability.");
            }
            else if (readiness != null && !readiness.IsReady)
            {
                foreach (var issue in readiness.Issues)
                {
                    issues.Add(issue);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // Rethrow if user cancelled
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating provider readiness");
            issues.Add("Unable to verify provider readiness. Check provider configuration and try again.");
        }

        progress?.Report("Validation complete");

        var validationResult = new ValidationResult(issues.Count == 0, issues);

        if (validationResult.IsValid)
        {
            _logger.LogInformation("System validation passed: All checks successful");
        }
        else
        {
            _logger.LogWarning("System validation failed with {IssueCount} issues", issues.Count);
        }

        return validationResult;
    }
}
