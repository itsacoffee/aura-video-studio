using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            // Use shorter timeout to prevent hanging - providers should validate quickly
            var providerTimeout = TimeSpan.FromSeconds(Math.Min(_timeoutSettings.ProviderCheckTimeoutSeconds, 10));
            var (readiness, providerTimedOut) = await ExecuteWithTimeoutAsync(
                _providerReadiness.ValidateRequiredProvidersAsync(ct),
                providerTimeout,
                "Provider readiness validation",
                ct).ConfigureAwait(false);

            if (providerTimedOut)
            {
                _logger.LogWarning("Provider validation timed out after {Timeout}s - continuing with available providers",
                    providerTimeout.TotalSeconds);
                // Don't fail validation on timeout - allow pipeline to continue with available providers
                issues.Add($"Provider validation timed out. Some providers may be slow or unreachable, but generation will continue with available providers.");
            }
            else if (readiness != null && !readiness.IsReady)
            {
                // CRITICAL: Check LLM category status directly, not by parsing issue messages
                // This is safer than relying on string matching in issue messages
                var llmCategoryStatus = readiness.CategoryStatuses
                    .FirstOrDefault(status => string.Equals(status.Category, "LLM", StringComparison.OrdinalIgnoreCase));

                if (llmCategoryStatus != null && !llmCategoryStatus.Ready)
                {
                    // LLM is not ready - this is critical, fail validation
                    var llmIssue = llmCategoryStatus.Message ??
                        $"No LLM providers are available. Configure at least one LLM provider (Ollama, OpenAI, etc.) in Settings.";

                    _logger.LogError(
                        "CRITICAL: LLM provider category is not ready. Category: {Category}, Ready: {Ready}, Message: {Message}",
                        llmCategoryStatus.Category, llmCategoryStatus.Ready, llmCategoryStatus.Message);

                    issues.Add(llmIssue);

                    // Add suggestions if available
                    if (llmCategoryStatus.Suggestions != null && llmCategoryStatus.Suggestions.Count > 0)
                    {
                        foreach (var suggestion in llmCategoryStatus.Suggestions)
                        {
                            issues.Add($"  → {suggestion}");
                        }
                    }
                }
                else if (llmCategoryStatus != null && llmCategoryStatus.Ready)
                {
                    // LLM is ready - check for other non-critical provider issues
                    var nonCriticalIssues = readiness.CategoryStatuses
                        .Where(status => !status.Ready &&
                               !string.Equals(status.Category, "LLM", StringComparison.OrdinalIgnoreCase))
                        .Select(status => status.Message ?? $"Category {status.Category} is not ready")
                        .ToList();

                    if (nonCriticalIssues.Count > 0)
                    {
                        _logger.LogInformation(
                            "LLM is ready ({Provider}), but some optional providers are not ready: {Issues}. Generation can proceed.",
                            llmCategoryStatus.Provider, string.Join(", ", nonCriticalIssues));
                    }
                    else
                    {
                        _logger.LogInformation("LLM is ready ({Provider}). All required providers are available.",
                            llmCategoryStatus.Provider);
                    }
                }
                else
                {
                    // LLM category status not found - this is unexpected, be conservative and fail
                    _logger.LogError(
                        "CRITICAL: Could not determine LLM provider status. CategoryStatuses: {Categories}. " +
                        "This may indicate a configuration issue. Failing validation to be safe.",
                        string.Join(", ", readiness.CategoryStatuses.Select(s => $"{s.Category}:{s.Ready}")));

                    issues.Add("Unable to verify LLM provider status. Please check your provider configuration.");

                    // Add all issues as fallback
                    foreach (var issue in readiness.Issues)
                    {
                        issues.Add(issue);
                    }
                }
            }
            else if (readiness != null && readiness.IsReady)
            {
                _logger.LogInformation("All required providers are ready");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // Rethrow if user cancelled
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating provider readiness - continuing anyway");
            // Don't fail validation on error - allow pipeline to continue
            // Only warn if LLM is definitely not available
            issues.Add("Unable to verify all provider readiness. Generation will attempt to proceed with configured providers.");
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

    // Constants for disk space validation
    private const long MinimumDiskSpaceBytes = 1L * 1024 * 1024 * 1024; // 1 GB

    /// <summary>
    /// Performs comprehensive preflight validation with detailed status for each component.
    /// This method validates FFmpeg, Ollama, TTS, disk space, and image providers with
    /// actionable error messages and suggested fixes.
    /// </summary>
    /// <param name="systemProfile">System hardware profile (optional, will be detected if null)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed preflight report with individual check results</returns>
    public virtual async Task<PreflightReport> ValidateAsync(SystemProfile? systemProfile, CancellationToken ct = default)
    {
        var report = new PreflightReport();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting comprehensive preflight validation");

        // Run all checks in parallel for better performance
        var ffmpegTask = ValidateFFmpegAsync(ct);
        var ollamaTask = ValidateOllamaAsync(ct);
        var diskSpaceTask = ValidateDiskSpaceAsync(ct);
        var ttsTask = ValidateTTSAsync(ct);
        var imageProviderTask = ValidateImageProviderAsync(ct);

        try
        {
            await Task.WhenAll(ffmpegTask, ollamaTask, diskSpaceTask, ttsTask, imageProviderTask).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "One or more preflight checks failed during parallel execution");
        }

        // Collect results
        report.FFmpeg = await ffmpegTask.ConfigureAwait(false);
        if (!report.FFmpeg.Passed && !report.FFmpeg.Skipped)
        {
            report.AddError("FFmpeg", report.FFmpeg.Details ?? report.FFmpeg.Status);
        }

        report.Ollama = await ollamaTask.ConfigureAwait(false);
        if (!report.Ollama.Passed && !report.Ollama.Skipped)
        {
            report.AddError("Ollama", report.Ollama.Details ?? report.Ollama.Status);
        }

        report.DiskSpace = await diskSpaceTask.ConfigureAwait(false);
        if (!report.DiskSpace.Passed && !report.DiskSpace.Skipped)
        {
            report.AddError("DiskSpace", report.DiskSpace.Details ?? report.DiskSpace.Status);
        }

        report.TTS = await ttsTask.ConfigureAwait(false);
        if (!report.TTS.Passed && !report.TTS.Skipped)
        {
            report.AddError("TTS", report.TTS.Details ?? report.TTS.Status);
        }

        // Image provider is optional - missing is a warning, not an error
        report.ImageProvider = await imageProviderTask.ConfigureAwait(false);
        if (!report.ImageProvider.Passed && !report.ImageProvider.Skipped)
        {
            report.AddWarning("ImageProvider", report.ImageProvider.Details ?? report.ImageProvider.Status);
        }

        stopwatch.Stop();
        report.DurationMs = (int)stopwatch.ElapsedMilliseconds;

        if (report.Ok)
        {
            _logger.LogInformation("Preflight validation passed in {DurationMs}ms", report.DurationMs);
        }
        else
        {
            _logger.LogWarning("Preflight validation failed with {ErrorCount} errors and {WarningCount} warnings in {DurationMs}ms",
                report.Errors.Count, report.Warnings.Count, report.DurationMs);
        }

        return report;
    }

    /// <summary>
    /// Backward-compatible method signature that returns the legacy format.
    /// </summary>
    /// <param name="systemProfile">System hardware profile</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="legacyFormat">Must be true to use this overload</param>
    /// <returns>Tuple of (IsValid, Errors)</returns>
    public async Task<(bool IsValid, List<string> Errors)> ValidateAsync(SystemProfile? systemProfile, CancellationToken ct, bool legacyFormat)
    {
        var report = await ValidateAsync(systemProfile, ct).ConfigureAwait(false);
        return (report.Ok, report.Errors);
    }

    /// <summary>
    /// Validates that FFmpeg is installed and actually works by running ffmpeg -version.
    /// </summary>
    private async Task<PreflightCheckResult> ValidateFFmpegAsync(CancellationToken ct)
    {
        var result = new PreflightCheckResult();

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
                result.Status = "Timeout";
                result.Details = $"FFmpeg validation timed out after {_timeoutSettings.FfmpegCheckTimeoutSeconds} seconds.";
                result.SuggestedAction = "Ensure FFmpeg is properly installed and not blocked by antivirus software.";
                return result;
            }

            if (resolutionResult == null || !resolutionResult.Found)
            {
                result.Status = "Not found";
                result.Details = "FFmpeg executable not found on this system.";
                result.SuggestedAction = "Install FFmpeg from https://ffmpeg.org/download.html or use the Download Center in Settings.";
                return result;
            }

            if (!resolutionResult.IsValid)
            {
                result.Status = "Invalid";
                result.Details = $"FFmpeg found but not valid: {resolutionResult.Error}";
                result.SuggestedAction = "Reinstall FFmpeg or update to a newer version from https://ffmpeg.org/download.html";
                return result;
            }

            // Actually run ffmpeg -version to verify it works
            var (success, output) = await RunProcessAsync("ffmpeg", "-version", 5000, ct).ConfigureAwait(false);

            if (!success)
            {
                result.Status = "Not working";
                result.Details = "FFmpeg found but failed to execute. It may be corrupted or missing dependencies.";
                result.SuggestedAction = "Reinstall FFmpeg from https://ffmpeg.org/download.html or check if all dependencies are installed.";
                return result;
            }

            // Extract version from output
            var versionLine = output?.Split('\n').FirstOrDefault(l => l.Contains("ffmpeg version"));
            var version = versionLine ?? resolutionResult.Version ?? "Unknown";

            result.Passed = true;
            result.Status = "Available";
            result.Details = $"FFmpeg is installed and working. Path: {resolutionResult.Path}. Version: {version}";
            _logger.LogInformation("FFmpeg validation passed: {Path} ({Version})", resolutionResult.Path, version);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating FFmpeg");
            result.Status = "Error";
            result.Details = $"Error checking FFmpeg: {ex.Message}";
            result.SuggestedAction = "Check FFmpeg installation and ensure it's accessible.";
        }

        return result;
    }

    /// <summary>
    /// Validates that Ollama is running and has at least one model installed.
    /// </summary>
    private async Task<PreflightCheckResult> ValidateOllamaAsync(CancellationToken ct)
    {
        var result = new PreflightCheckResult();

        try
        {
            // Check if Ollama is running by calling ollama list
            var (listSuccess, listOutput) = await RunProcessAsync("ollama", "list", 5000, ct).ConfigureAwait(false);

            if (!listSuccess)
            {
                // Try to check if ollama exists but isn't running
                var (versionSuccess, _) = await RunProcessAsync("ollama", "--version", 3000, ct).ConfigureAwait(false);

                if (versionSuccess)
                {
                    result.Status = "Not running";
                    result.Details = "Ollama is installed but not running.";
                    result.SuggestedAction = "Start Ollama by running: ollama serve";
                }
                else
                {
                    result.Status = "Not installed";
                    result.Details = "Ollama is not installed or not in PATH.";
                    result.SuggestedAction = "Install Ollama from https://ollama.com/download";
                }
                return result;
            }

            // Parse the output to check if any models are installed
            var lines = listOutput?.Split('\n', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            // Skip the header line if present
            var modelLines = lines.Where(l => !l.StartsWith("NAME") && !string.IsNullOrWhiteSpace(l)).ToList();

            if (modelLines.Count == 0)
            {
                result.Status = "No models";
                result.Details = "Ollama is running but no models are installed.";
                result.SuggestedAction = "Install a model by running: ollama pull llama3.1:8b";
                return result;
            }

            result.Passed = true;
            result.Status = "Available";
            result.Details = $"Ollama is running with {modelLines.Count} model(s) available.";
            _logger.LogInformation("Ollama validation passed: {ModelCount} models available", modelLines.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating Ollama");
            result.Status = "Error";
            result.Details = $"Error checking Ollama: {ex.Message}";
            result.SuggestedAction = "Check if Ollama is installed and accessible.";
        }

        return result;
    }

    /// <summary>
    /// Validates that at least 1GB of disk space is available on the temp drive.
    /// </summary>
    private Task<PreflightCheckResult> ValidateDiskSpaceAsync(CancellationToken ct)
    {
        var result = new PreflightCheckResult();

        try
        {
            var tempPath = Path.GetTempPath();
            var rootPath = Path.GetPathRoot(tempPath);

            if (string.IsNullOrEmpty(rootPath))
            {
                result.Status = "Unknown";
                result.Details = "Could not determine the root path for the temp directory.";
                result.Skipped = true;
                return Task.FromResult(result);
            }

            var driveInfo = new DriveInfo(rootPath);

            if (!driveInfo.IsReady)
            {
                result.Status = "Not ready";
                result.Details = $"Drive {rootPath} is not ready.";
                result.SuggestedAction = "Ensure the drive is accessible and not in use by another application.";
                return Task.FromResult(result);
            }

            var availableBytes = driveInfo.AvailableFreeSpace;
            var availableGB = availableBytes / (1024.0 * 1024.0 * 1024.0);

            if (availableBytes < MinimumDiskSpaceBytes)
            {
                result.Status = "Insufficient";
                result.Details = $"Only {availableGB:F2}GB free on {rootPath}. Need at least 1GB for video generation.";
                result.SuggestedAction = "Free up disk space by deleting temporary files or moving data to another drive.";
                return Task.FromResult(result);
            }

            result.Passed = true;
            result.Status = "Sufficient";
            result.Details = $"{availableGB:F2}GB available on {rootPath}.";
            _logger.LogInformation("Disk space validation passed: {FreeSpace:F2}GB available on {Drive}", availableGB, rootPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating disk space");
            result.Status = "Error";
            result.Details = $"Error checking disk space: {ex.Message}";
            result.Skipped = true;
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Validates that TTS providers are configured and have voices available.
    /// </summary>
    private async Task<PreflightCheckResult> ValidateTTSAsync(CancellationToken ct)
    {
        var result = new PreflightCheckResult();

        try
        {
            var providerTimeout = TimeSpan.FromSeconds(_timeoutSettings.ProviderCheckTimeoutSeconds);
            var (readiness, timedOut) = await ExecuteWithTimeoutAsync(
                _providerReadiness.ValidateRequiredProvidersAsync(ct),
                providerTimeout,
                "TTS validation",
                ct).ConfigureAwait(false);

            if (timedOut)
            {
                result.Status = "Timeout";
                result.Details = "TTS validation timed out.";
                result.SuggestedAction = "Check your TTS provider configuration and network connectivity.";
                return result;
            }

            if (readiness == null)
            {
                result.Status = "Error";
                result.Details = "Could not validate TTS providers.";
                result.SuggestedAction = "Check your TTS provider configuration in Settings.";
                return result;
            }

            var ttsStatus = readiness.CategoryStatuses.FirstOrDefault(s =>
                string.Equals(s.Category, "TTS", StringComparison.OrdinalIgnoreCase));

            if (ttsStatus == null)
            {
                result.Status = "Not configured";
                result.Details = "No TTS providers are configured.";
                result.SuggestedAction = "Configure a TTS provider (ElevenLabs, Piper, Windows TTS) in Settings.";
                return result;
            }

            if (!ttsStatus.Ready)
            {
                result.Status = "Not available";
                result.Details = ttsStatus.Message ?? "No TTS provider is available.";
                result.SuggestedAction = ttsStatus.Suggestions.FirstOrDefault() ?? "Configure a working TTS provider in Settings.";
                return result;
            }

            result.Passed = true;
            result.Status = "Available";
            result.Details = $"TTS provider '{ttsStatus.Provider}' is ready.";
            _logger.LogInformation("TTS validation passed: {Provider}", ttsStatus.Provider);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating TTS");
            result.Status = "Error";
            result.Details = $"Error checking TTS: {ex.Message}";
            result.SuggestedAction = "Check your TTS provider configuration.";
        }

        return result;
    }

    /// <summary>
    /// Validates that an image provider is available (optional - missing is a warning only).
    /// </summary>
    private async Task<PreflightCheckResult> ValidateImageProviderAsync(CancellationToken ct)
    {
        var result = new PreflightCheckResult();

        try
        {
            var providerTimeout = TimeSpan.FromSeconds(_timeoutSettings.ProviderCheckTimeoutSeconds);
            var (readiness, timedOut) = await ExecuteWithTimeoutAsync(
                _providerReadiness.ValidateRequiredProvidersAsync(ct),
                providerTimeout,
                "Image provider validation",
                ct).ConfigureAwait(false);

            if (timedOut)
            {
                result.Status = "Timeout";
                result.Details = "Image provider validation timed out.";
                result.SuggestedAction = "Check your image provider configuration and network connectivity.";
                result.Skipped = true;
                return result;
            }

            if (readiness == null)
            {
                result.Status = "Unknown";
                result.Details = "Could not validate image providers.";
                result.Skipped = true;
                return result;
            }

            var imageStatus = readiness.CategoryStatuses.FirstOrDefault(s =>
                string.Equals(s.Category, "Images", StringComparison.OrdinalIgnoreCase));

            if (imageStatus == null)
            {
                result.Status = "Not configured";
                result.Details = "No image providers are configured. Videos will use placeholder visuals.";
                result.SuggestedAction = "Configure an image provider (Stable Diffusion, Pexels, etc.) for better visuals.";
                return result;
            }

            if (!imageStatus.Ready)
            {
                result.Status = "Not available";
                result.Details = imageStatus.Message ?? "No image provider is available. Videos will use placeholder visuals.";
                result.SuggestedAction = imageStatus.Suggestions.FirstOrDefault() ?? "Configure a working image provider in Settings.";
                return result;
            }

            result.Passed = true;
            result.Status = "Available";
            result.Details = $"Image provider '{imageStatus.Provider}' is ready.";
            _logger.LogInformation("Image provider validation passed: {Provider}", imageStatus.Provider);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating image provider");
            result.Status = "Error";
            result.Details = $"Error checking image provider: {ex.Message}";
            result.Skipped = true;
        }

        return result;
    }

    /// <summary>
    /// Runs a process with the specified arguments and timeout, returning success status and output.
    /// </summary>
    private async Task<(bool Success, string Output)> RunProcessAsync(
        string fileName,
        string arguments,
        int timeoutMs,
        CancellationToken ct)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            try
            {
                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timeout - kill the process
                try 
                { 
                    process.Kill(); 
                } 
                catch (Exception killEx) 
                { 
                    // Process may have already exited or we may not have permission to kill it
                    _logger.LogDebug(killEx, "Failed to kill timed-out process - it may have already exited");
                }
                return (false, "Process timed out");
            }

            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);

            return (process.ExitCode == 0, string.IsNullOrEmpty(output) ? error : output);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
