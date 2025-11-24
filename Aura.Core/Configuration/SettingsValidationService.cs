using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Configuration;

/// <summary>
/// Comprehensive validation service for all application configuration and dependencies
/// Validates critical dependencies (FFmpeg, database, directories) and optional ones (Ollama, API keys)
/// </summary>
public class SettingsValidationService
{
    private readonly ILogger<SettingsValidationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IFfmpegLocator? _ffmpegLocator;
    private readonly OllamaDetectionService? _ollamaDetection;
    private readonly ProviderSettings? _providerSettings;
    private readonly string _databasePath;
    private readonly string _outputDirectory;

    public SettingsValidationService(
        ILogger<SettingsValidationService> logger,
        IConfiguration configuration,
        IFfmpegLocator? ffmpegLocator = null,
        OllamaDetectionService? ollamaDetection = null,
        ProviderSettings? providerSettings = null)
    {
        _logger = logger;
        _configuration = configuration;
        _ffmpegLocator = ffmpegLocator;
        _ollamaDetection = ollamaDetection;
        _providerSettings = providerSettings;

        // Resolve database path
        var sqliteFileName = _configuration.GetValue<string>("Database:SQLiteFileName") ?? "aura.db";
        var configuredSqlitePath = _configuration.GetValue<string>("Database:SQLitePath");
        var envSqlitePath = Environment.GetEnvironmentVariable("AURA_DATABASE_PATH");
        var defaultUserDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura");
        _databasePath = !string.IsNullOrWhiteSpace(configuredSqlitePath)
            ? configuredSqlitePath
            : !string.IsNullOrWhiteSpace(envSqlitePath)
                ? envSqlitePath
                : Path.Combine(defaultUserDataRoot, sqliteFileName);
        _databasePath = Path.GetFullPath(_databasePath);

        // Resolve output directory
        var outputDir = _configuration["OutputDirectory"];
        if (string.IsNullOrWhiteSpace(outputDir) && _providerSettings != null)
        {
            outputDir = _providerSettings.GetOutputDirectory();
        }
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            outputDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AuraVideoStudio", "Output");
        }
        _outputDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(outputDir));
    }

    /// <summary>
    /// Validates all configuration and dependencies
    /// </summary>
    public async Task<ValidationResult> ValidateAllAsync(CancellationToken ct = default)
    {
        var criticalIssues = new List<ValidationIssue>();
        var warnings = new List<ValidationIssue>();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Starting comprehensive configuration validation...");

        // Critical validations - these must pass for the app to start
        await ValidateFfmpegAsync(criticalIssues, ct).ConfigureAwait(false);
        ValidateOutputDirectories(criticalIssues);
        ValidateDatabasePath(criticalIssues);

        // Warning validations - these are optional but should be checked
        await ValidateOllamaAsync(warnings, ct).ConfigureAwait(false);
        ValidateApiKeys(warnings);
        ValidateTtsProvider(warnings);

        var duration = DateTime.UtcNow - startTime;
        var canStart = criticalIssues.Count == 0;

        _logger.LogInformation(
            "Configuration validation completed in {Duration}ms. CanStart: {CanStart}, Critical: {CriticalCount}, Warnings: {WarningCount}",
            duration.TotalMilliseconds, canStart, criticalIssues.Count, warnings.Count);

        return new ValidationResult(
            CanStart: canStart,
            CriticalIssues: criticalIssues,
            Warnings: warnings,
            ValidationDuration: duration
        );
    }

    /// <summary>
    /// Validates FFmpeg installation - CRITICAL
    /// </summary>
    private async Task ValidateFfmpegAsync(List<ValidationIssue> issues, CancellationToken ct)
    {
        try
        {
            if (_ffmpegLocator == null)
            {
                issues.Add(new ValidationIssue(
                    Category: "FFmpeg",
                    Code: "FFMPEG_LOCATOR_MISSING",
                    Message: "FFmpeg locator service not available",
                    Resolution: "Ensure FFmpeg locator is registered in dependency injection"
                ));
                return;
            }

            var configuredPath = _configuration.GetValue<string>("FFmpeg:ExecutablePath");
            var envPath = Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH");

            // Try to get effective path
            try
            {
                var effectivePath = await _ffmpegLocator.GetEffectiveFfmpegPathAsync(configuredPath ?? envPath, ct).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(effectivePath))
                {
                    throw new InvalidOperationException("FFmpeg path resolution returned empty result");
                }

                // Verify the path exists (unless it's on PATH)
                if (!effectivePath.Equals("ffmpeg", StringComparison.OrdinalIgnoreCase) && !File.Exists(effectivePath))
                {
                    issues.Add(new ValidationIssue(
                        Category: "FFmpeg",
                        Code: "FFMPEG_NOT_FOUND",
                        Message: $"FFmpeg executable not found at: {effectivePath}",
                        Resolution: "Install FFmpeg via Download Center, attach an existing installation, or add FFmpeg to system PATH"
                    ));
                    return;
                }

                // Validate the binary works
                var validationResult = await _ffmpegLocator.ValidatePathAsync(effectivePath, ct).ConfigureAwait(false);
                if (!validationResult.Found)
                {
                    issues.Add(new ValidationIssue(
                        Category: "FFmpeg",
                        Code: "FFMPEG_INVALID",
                        Message: $"FFmpeg binary validation failed: {validationResult.Reason}",
                        Resolution: "Reinstall FFmpeg or verify the installation is not corrupted"
                    ));
                    return;
                }

                _logger.LogInformation("FFmpeg validation passed: {Path} (Version: {Version})",
                    effectivePath, validationResult.VersionString ?? "unknown");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("FFmpeg not found"))
            {
                issues.Add(new ValidationIssue(
                    Category: "FFmpeg",
                    Code: "FFMPEG_NOT_FOUND",
                    Message: "FFmpeg binary not found in any configured location",
                    Resolution: "Install FFmpeg via Download Center, attach an existing installation, or add FFmpeg to system PATH. " +
                              "Download: https://ffmpeg.org/download.html"
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating FFmpeg");
            issues.Add(new ValidationIssue(
                Category: "FFmpeg",
                Code: "FFMPEG_VALIDATION_ERROR",
                Message: $"FFmpeg validation failed: {ex.Message}",
                Resolution: "Check FFmpeg installation and configuration. Ensure FFmpeg is accessible."
            ));
        }
    }

    /// <summary>
    /// Validates output directories - CRITICAL
    /// </summary>
    private void ValidateOutputDirectories(List<ValidationIssue> issues)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_outputDirectory))
            {
                issues.Add(new ValidationIssue(
                    Category: "OutputDirectory",
                    Code: "OUTPUT_DIR_NOT_CONFIGURED",
                    Message: "Output directory is not configured",
                    Resolution: "Configure output directory in settings or appsettings.json"
                ));
                return;
            }

            // Expand environment variables
            var expandedPath = Environment.ExpandEnvironmentVariables(_outputDirectory);
            var fullPath = Path.GetFullPath(expandedPath);

            // Check if directory exists or can be created
            if (!Directory.Exists(fullPath))
            {
                try
                {
                    Directory.CreateDirectory(fullPath);
                    _logger.LogInformation("Created output directory: {Path}", fullPath);
                }
                catch (Exception ex)
                {
                    issues.Add(new ValidationIssue(
                        Category: "OutputDirectory",
                        Code: "OUTPUT_DIR_CREATE_FAILED",
                        Message: $"Cannot create output directory: {fullPath}",
                        Resolution: $"Check file system permissions. Error: {ex.Message}"
                    ));
                    return;
                }
            }

            // Check if directory is writable
            if (!IsDirectoryWritable(fullPath))
            {
                issues.Add(new ValidationIssue(
                    Category: "OutputDirectory",
                    Code: "OUTPUT_DIR_NOT_WRITABLE",
                    Message: $"Output directory is not writable: {fullPath}",
                    Resolution: "Check file system permissions and ensure the application has write access"
                ));
                return;
            }

            _logger.LogInformation("Output directory validation passed: {Path}", fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating output directory");
            issues.Add(new ValidationIssue(
                Category: "OutputDirectory",
                Code: "OUTPUT_DIR_VALIDATION_ERROR",
                Message: $"Output directory validation failed: {ex.Message}",
                Resolution: "Check output directory configuration and file system permissions"
            ));
        }
    }

    /// <summary>
    /// Validates database path - CRITICAL
    /// </summary>
    private void ValidateDatabasePath(List<ValidationIssue> issues)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_databasePath))
            {
                issues.Add(new ValidationIssue(
                    Category: "Database",
                    Code: "DATABASE_PATH_NOT_CONFIGURED",
                    Message: "Database path is not configured",
                    Resolution: "Configure database path in appsettings.json or via AURA_DATABASE_PATH environment variable"
                ));
                return;
            }

            var dbDirectory = Path.GetDirectoryName(_databasePath);
            if (string.IsNullOrWhiteSpace(dbDirectory))
            {
                issues.Add(new ValidationIssue(
                    Category: "Database",
                    Code: "DATABASE_DIR_INVALID",
                    Message: $"Invalid database directory: {_databasePath}",
                    Resolution: "Configure a valid database path in appsettings.json"
                ));
                return;
            }

            // Ensure directory exists
            if (!Directory.Exists(dbDirectory))
            {
                try
                {
                    Directory.CreateDirectory(dbDirectory);
                    _logger.LogInformation("Created database directory: {Path}", dbDirectory);
                }
                catch (Exception ex)
                {
                    issues.Add(new ValidationIssue(
                        Category: "Database",
                        Code: "DATABASE_DIR_CREATE_FAILED",
                        Message: $"Cannot create database directory: {dbDirectory}",
                        Resolution: $"Check file system permissions. Error: {ex.Message}"
                    ));
                    return;
                }
            }

            // Check if directory is writable
            if (!IsDirectoryWritable(dbDirectory))
            {
                issues.Add(new ValidationIssue(
                    Category: "Database",
                    Code: "DATABASE_DIR_NOT_WRITABLE",
                    Message: $"Database directory is not writable: {dbDirectory}",
                    Resolution: "Check file system permissions and ensure the application has write access"
                ));
                return;
            }

            _logger.LogInformation("Database path validation passed: {Path}", _databasePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating database path");
            issues.Add(new ValidationIssue(
                Category: "Database",
                Code: "DATABASE_VALIDATION_ERROR",
                Message: $"Database validation failed: {ex.Message}",
                Resolution: "Check database configuration and file system permissions"
            ));
        }
    }

    /// <summary>
    /// Validates Ollama availability - WARNING (optional)
    /// </summary>
    private async Task ValidateOllamaAsync(List<ValidationIssue> warnings, CancellationToken ct)
    {
        try
        {
            if (_ollamaDetection == null)
            {
                // Ollama detection service not available - not critical
                return;
            }

            // Wait for initial detection with timeout
            var detectionCompleted = await _ollamaDetection.WaitForInitialDetectionAsync(
                TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);

            if (!detectionCompleted)
            {
                warnings.Add(new ValidationIssue(
                    Category: "Ollama",
                    Code: "OLLAMA_DETECTION_TIMEOUT",
                    Message: "Ollama detection timed out",
                    Resolution: "Ollama may be starting up. Check if Ollama is running: 'ollama serve'"
                ));
                return;
            }

            var status = await _ollamaDetection.GetStatusAsync(ct).ConfigureAwait(false);
            if (!status.IsRunning)
            {
                warnings.Add(new ValidationIssue(
                    Category: "Ollama",
                    Code: "OLLAMA_NOT_RUNNING",
                    Message: "Ollama service is not running",
                    Resolution: "Start Ollama service: 'ollama serve'. Ollama is optional - the app will use other LLM providers if available."
                ));
            }
            else
            {
                _logger.LogInformation("Ollama validation passed: service is running");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating Ollama");
            warnings.Add(new ValidationIssue(
                Category: "Ollama",
                Code: "OLLAMA_VALIDATION_ERROR",
                Message: $"Ollama validation failed: {ex.Message}",
                Resolution: "Ollama is optional. Check Ollama configuration if you want to use local models."
            ));
        }
    }

    /// <summary>
    /// Validates API keys format - WARNING (optional)
    /// </summary>
    private void ValidateApiKeys(List<ValidationIssue> warnings)
    {
        try
        {
            if (_providerSettings == null)
            {
                return;
            }

            // Check OpenAI API key format if provided
            var openAiKey = _providerSettings.GetOpenAiApiKey();
            if (!string.IsNullOrWhiteSpace(openAiKey))
            {
                if (!openAiKey.StartsWith("sk-", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add(new ValidationIssue(
                        Category: "APIKeys",
                        Code: "OPENAI_KEY_INVALID_FORMAT",
                        Message: "OpenAI API key format appears invalid (should start with 'sk-')",
                        Resolution: "Verify your OpenAI API key. Get a new key at https://platform.openai.com/api-keys"
                    ));
                }
            }

            // Check other API keys if needed
            var anthropicKey = _providerSettings.GetAnthropicKey();
            if (!string.IsNullOrWhiteSpace(anthropicKey) && anthropicKey.Length < 20)
            {
                warnings.Add(new ValidationIssue(
                    Category: "APIKeys",
                    Code: "ANTHROPIC_KEY_INVALID_FORMAT",
                    Message: "Anthropic API key appears too short",
                    Resolution: "Verify your Anthropic API key. Get a new key at https://console.anthropic.com/"
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating API keys");
        }
    }

    /// <summary>
    /// Validates TTS provider configuration - WARNING (optional)
    /// </summary>
    private void ValidateTtsProvider(List<ValidationIssue> warnings)
    {
        try
        {
            // Check if any TTS provider is configured
            var ttsProvider = _configuration["TTS:Provider"];
            if (string.IsNullOrWhiteSpace(ttsProvider))
            {
                warnings.Add(new ValidationIssue(
                    Category: "TTS",
                    Code: "TTS_PROVIDER_NOT_CONFIGURED",
                    Message: "No TTS provider configured",
                    Resolution: "Configure a TTS provider in settings. Options: ElevenLabs, Azure, Piper, Mimic3"
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating TTS provider");
        }
    }

    /// <summary>
    /// Checks if a directory is writable
    /// </summary>
    private bool IsDirectoryWritable(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $".write_test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Individual check methods for health endpoints

    /// <summary>
    /// Check FFmpeg availability
    /// </summary>
    public async Task<DependencyCheckResult> CheckFfmpegAsync(CancellationToken ct = default)
    {
        try
        {
            if (_ffmpegLocator == null)
            {
                return new DependencyCheckResult(false, "FFmpeg locator service not available", null);
            }

            var configuredPath = _configuration.GetValue<string>("FFmpeg:ExecutablePath");
            var envPath = Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH");
            var effectivePath = await _ffmpegLocator.GetEffectiveFfmpegPathAsync(configuredPath ?? envPath, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(effectivePath))
            {
                return new DependencyCheckResult(false, "FFmpeg not found", null);
            }

            var validationResult = await _ffmpegLocator.ValidatePathAsync(effectivePath, ct).ConfigureAwait(false);
            return new DependencyCheckResult(
                validationResult.Found,
                validationResult.Found ? "Available" : validationResult.Reason ?? "Unknown error",
                validationResult.VersionString
            );
        }
        catch (Exception ex)
        {
            return new DependencyCheckResult(false, ex.Message, null);
        }
    }

    /// <summary>
    /// Check database accessibility
    /// </summary>
    public DependencyCheckResult CheckDatabase()
    {
        try
        {
            var dbDirectory = Path.GetDirectoryName(_databasePath);
            if (string.IsNullOrWhiteSpace(dbDirectory) || !Directory.Exists(dbDirectory))
            {
                return new DependencyCheckResult(false, "Database directory does not exist", null);
            }

            if (!IsDirectoryWritable(dbDirectory))
            {
                return new DependencyCheckResult(false, "Database directory is not writable", null);
            }

            return new DependencyCheckResult(true, "Available", _databasePath);
        }
        catch (Exception ex)
        {
            return new DependencyCheckResult(false, ex.Message, null);
        }
    }

    /// <summary>
    /// Check output directory accessibility
    /// </summary>
    public DependencyCheckResult CheckOutputDirectory()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_outputDirectory))
            {
                return new DependencyCheckResult(false, "Output directory not configured", null);
            }

            var expandedPath = Environment.ExpandEnvironmentVariables(_outputDirectory);
            var fullPath = Path.GetFullPath(expandedPath);

            if (!Directory.Exists(fullPath))
            {
                try
                {
                    Directory.CreateDirectory(fullPath);
                }
                catch (Exception ex)
                {
                    return new DependencyCheckResult(false, $"Cannot create directory: {ex.Message}", null);
                }
            }

            if (!IsDirectoryWritable(fullPath))
            {
                return new DependencyCheckResult(false, "Output directory is not writable", null);
            }

            return new DependencyCheckResult(true, "Available", fullPath);
        }
        catch (Exception ex)
        {
            return new DependencyCheckResult(false, ex.Message, null);
        }
    }
}

/// <summary>
/// Result of configuration validation
/// </summary>
public record ValidationResult(
    bool CanStart,
    List<ValidationIssue> CriticalIssues,
    List<ValidationIssue> Warnings,
    TimeSpan ValidationDuration
);

/// <summary>
/// A single validation issue
/// </summary>
public record ValidationIssue(
    string Category,
    string Code,
    string Message,
    string? Resolution
);

/// <summary>
/// Result of a dependency check
/// </summary>
public record DependencyCheckResult(
    bool IsAvailable,
    string Message,
    string? Details
);

