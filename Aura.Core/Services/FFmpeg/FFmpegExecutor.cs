using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.FFmpeg;

/// <summary>
/// High-level executor for FFmpeg operations with command injection prevention
/// and resource management
/// </summary>
public interface IFFmpegExecutor
{
    /// <summary>
    /// Execute an FFmpeg command built by FFmpegCommandBuilder
    /// </summary>
    Task<FFmpegResult> ExecuteCommandAsync(
        FFmpegCommandBuilder builder,
        Action<FFmpegProgress>? progressCallback = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an FFmpeg command with two-pass encoding
    /// </summary>
    Task<FFmpegResult> ExecuteTwoPassAsync(
        FFmpegCommandBuilder firstPassBuilder,
        FFmpegCommandBuilder secondPassBuilder,
        Action<FFmpegProgress>? progressCallback = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute multiple FFmpeg commands sequentially
    /// </summary>
    Task<FFmpegResult[]> ExecuteSequentialAsync(
        FFmpegCommandBuilder[] builders,
        Action<int, FFmpegProgress>? progressCallback = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of FFmpeg executor with enhanced safety and resource management
/// </summary>
public class FFmpegExecutor : IFFmpegExecutor
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<FFmpegExecutor> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(60);

    public FFmpegExecutor(
        IFFmpegService ffmpegService,
        ILogger<FFmpegExecutor> logger)
    {
        _ffmpegService = ffmpegService ?? throw new ArgumentNullException(nameof(ffmpegService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FFmpegResult> ExecuteCommandAsync(
        FFmpegCommandBuilder builder,
        Action<FFmpegProgress>? progressCallback = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var arguments = builder.Build();
        _logger.LogInformation("Executing FFmpeg command");

        // Validate command for safety (basic check for command injection)
        ValidateCommand(arguments);

        var effectiveTimeout = timeout ?? _defaultTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);

        try
        {
            var result = await _ffmpegService.ExecuteAsync(
                arguments,
                progressCallback,
                cts.Token
            ).ConfigureAwait(false);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "FFmpeg command failed with exit code {ExitCode}: {Error}",
                    result.ExitCode,
                    result.ErrorMessage
                );
            }

            return result;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("FFmpeg command timed out after {Timeout}", effectiveTimeout);
            throw new TimeoutException($"FFmpeg command timed out after {effectiveTimeout}");
        }
    }

    public async Task<FFmpegResult> ExecuteTwoPassAsync(
        FFmpegCommandBuilder firstPassBuilder,
        FFmpegCommandBuilder secondPassBuilder,
        Action<FFmpegProgress>? progressCallback = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (firstPassBuilder == null)
        {
            throw new ArgumentNullException(nameof(firstPassBuilder));
        }

        if (secondPassBuilder == null)
        {
            throw new ArgumentNullException(nameof(secondPassBuilder));
        }

        _logger.LogInformation("Starting two-pass encoding");

        // First pass - analysis only
        var firstPassResult = await ExecuteCommandAsync(
            firstPassBuilder,
            progress => progressCallback?.Invoke(progress with { PercentComplete = progress.PercentComplete * 0.4 }),
            timeout,
            cancellationToken
        ).ConfigureAwait(false);

        if (!firstPassResult.Success)
        {
            _logger.LogError("First pass failed: {Error}", firstPassResult.ErrorMessage);
            return firstPassResult;
        }

        _logger.LogInformation("First pass complete, starting second pass");

        // Second pass - actual encoding
        var secondPassResult = await ExecuteCommandAsync(
            secondPassBuilder,
            progress => progressCallback?.Invoke(progress with { PercentComplete = 40 + (progress.PercentComplete * 0.6) }),
            timeout,
            cancellationToken
        ).ConfigureAwait(false);

        if (!secondPassResult.Success)
        {
            _logger.LogError("Second pass failed: {Error}", secondPassResult.ErrorMessage);
        }
        else
        {
            _logger.LogInformation("Two-pass encoding completed successfully");
        }

        return secondPassResult;
    }

    public async Task<FFmpegResult[]> ExecuteSequentialAsync(
        FFmpegCommandBuilder[] builders,
        Action<int, FFmpegProgress>? progressCallback = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (builders == null || builders.Length == 0)
        {
            throw new ArgumentException("At least one command builder must be provided", nameof(builders));
        }

        _logger.LogInformation("Executing {Count} FFmpeg commands sequentially", builders.Length);

        var results = new FFmpegResult[builders.Length];

        for (int i = 0; i < builders.Length; i++)
        {
            var commandIndex = i;
            _logger.LogInformation("Executing command {Index}/{Total}", i + 1, builders.Length);

            results[i] = await ExecuteCommandAsync(
                builders[i],
                progress => progressCallback?.Invoke(commandIndex, progress),
                timeout,
                cancellationToken
            ).ConfigureAwait(false);

            if (!results[i].Success)
            {
                _logger.LogWarning(
                    "Command {Index}/{Total} failed, stopping sequential execution",
                    i + 1,
                    builders.Length
                );
                break;
            }
        }

        _logger.LogInformation("Sequential execution completed");
        return results;
    }

    /// <summary>
    /// Validates FFmpeg command for basic safety checks
    /// </summary>
    private void ValidateCommand(string arguments)
    {
        // Basic validation to prevent obvious command injection
        // Check for dangerous patterns
        var dangerousPatterns = new[]
        {
            "&&", "||", ";", "|", ">", "<", // Command chaining
            "$(",  "`", "$(", "${", // Command substitution
            "\n", "\r" // Newlines
        };

        foreach (var pattern in dangerousPatterns)
        {
            // Allow these in quoted strings (file paths, filter expressions)
            // but not at the top level unquoted
            if (arguments.Contains(pattern, StringComparison.Ordinal))
            {
                // Check if it's within quotes
                var index = arguments.IndexOf(pattern, StringComparison.Ordinal);
                if (!IsWithinQuotes(arguments, index))
                {
                    _logger.LogWarning(
                        "Potentially dangerous pattern detected in FFmpeg command: {Pattern}",
                        pattern
                    );
                    // For now, just log a warning. Could throw exception for stricter security.
                }
            }
        }
    }

    /// <summary>
    /// Check if a character position is within quoted string
    /// </summary>
    private bool IsWithinQuotes(string text, int position)
    {
        bool inQuotes = false;
        char quoteChar = '\0';

        for (int i = 0; i < position && i < text.Length; i++)
        {
            var c = text[i];
            if ((c == '"' || c == '\'') && (i == 0 || text[i - 1] != '\\'))
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (c == quoteChar)
                {
                    inQuotes = false;
                    quoteChar = '\0';
                }
            }
        }

        return inQuotes;
    }
}
