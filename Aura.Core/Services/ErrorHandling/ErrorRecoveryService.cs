using Aura.Core.Errors;
using Aura.Core.Validation;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Aura.Core.Services.ErrorHandling;

/// <summary>
/// Service for providing error recovery guidance and automated recovery actions
/// </summary>
public class ErrorRecoveryService
{
    private readonly ILogger<ErrorRecoveryService> _logger;

    public ErrorRecoveryService(ILogger<ErrorRecoveryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate comprehensive recovery guidance for an exception
    /// </summary>
    public ErrorRecoveryGuide GenerateRecoveryGuide(Exception exception, string? correlationId = null)
    {
        var guide = new ErrorRecoveryGuide
        {
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
            Timestamp = DateTime.UtcNow,
            ExceptionType = exception.GetType().Name
        };

        // Extract information from AuraException
        if (exception is AuraException auraEx)
        {
            guide.ErrorCode = auraEx.ErrorCode;
            guide.UserFriendlyMessage = auraEx.UserMessage;
            guide.IsTransient = auraEx.IsTransient;
            guide.ManualActions = auraEx.SuggestedActions?.ToList() ?? new List<string>();
            guide.Context = auraEx.Context;
        }
        else
        {
            guide.UserFriendlyMessage = GenerateUserFriendlyMessage(exception);
            guide.ManualActions = GenerateManualActions(exception);
        }

        // Determine if automated recovery is possible
        guide.AutomatedRecovery = DetermineAutomatedRecovery(exception);
        
        // Add troubleshooting steps
        guide.TroubleshootingSteps = GenerateTroubleshootingSteps(exception);

        // Add related documentation links
        guide.DocumentationLinks = GenerateDocumentationLinks(exception);

        // Determine severity
        guide.Severity = DetermineSeverity(exception);

        return guide;
    }

    /// <summary>
    /// Attempt automated recovery for an exception
    /// </summary>
    public async Task<RecoveryAttemptResult> AttemptAutomatedRecoveryAsync(
        Exception exception,
        string? correlationId = null)
    {
        correlationId ??= Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "Attempting automated recovery for {ExceptionType} [CorrelationId: {CorrelationId}]",
            exception.GetType().Name,
            correlationId);

        var result = new RecoveryAttemptResult
        {
            CorrelationId = correlationId,
            StartTime = DateTime.UtcNow
        };

        try
        {
            var recovered = exception switch
            {
                // File access errors - retry with delay
                ResourceException { ResourceType: ResourceType.FileLocked } => 
                    await RetryWithDelay(async () => true, 3, TimeSpan.FromSeconds(2)),

                // Network errors - retry with exponential backoff
                ProviderException { SpecificErrorCode: ProviderErrorCode.NetworkError } =>
                    await RetryWithExponentialBackoff(async () => true, 3),

                // Transient provider errors
                ProviderException { IsTransient: true } =>
                    await RetryWithExponentialBackoff(async () => true, 3),

                // Rate limiting - wait and retry
                ProviderException { SpecificErrorCode: ProviderErrorCode.RateLimit } provEx =>
                    await HandleRateLimiting(provEx),

                _ => false
            };

            result.Success = recovered;
            result.EndTime = DateTime.UtcNow;
            result.Message = recovered 
                ? "Automated recovery successful" 
                : "Automated recovery not available for this error type";

            return result;
        }
        catch (Exception recoveryEx)
        {
            _logger.LogError(recoveryEx, 
                "Automated recovery failed [CorrelationId: {CorrelationId}]", 
                correlationId);

            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            result.Message = $"Recovery attempt failed: {recoveryEx.Message}";
            result.RecoveryError = recoveryEx;

            return result;
        }
    }

    private async Task<bool> RetryWithDelay(Func<Task<bool>> operation, int maxAttempts, TimeSpan delay)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            if (i > 0)
            {
                _logger.LogDebug("Retry attempt {Attempt} after {Delay}ms", i + 1, delay.TotalMilliseconds);
                await Task.Delay(delay);
            }

            try
            {
                if (await operation())
                    return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Retry attempt {Attempt} failed", i + 1);
                if (i == maxAttempts - 1)
                    throw;
            }
        }

        return false;
    }

    private async Task<bool> RetryWithExponentialBackoff(Func<Task<bool>> operation, int maxAttempts)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            if (i > 0)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, i));
                _logger.LogDebug("Retry attempt {Attempt} after {Delay}s exponential backoff", 
                    i + 1, delay.TotalSeconds);
                await Task.Delay(delay);
            }

            try
            {
                if (await operation())
                    return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Retry attempt {Attempt} failed", i + 1);
                if (i == maxAttempts - 1)
                    throw;
            }
        }

        return false;
    }

    private async Task<bool> HandleRateLimiting(ProviderException provEx)
    {
        // Extract retry-after if available
        int retryAfterSeconds = 60; // Default to 1 minute
        
        if (provEx.Context.TryGetValue("retryAfter", out var retryAfter) && 
            retryAfter is int retryAfterInt)
        {
            retryAfterSeconds = retryAfterInt;
        }

        _logger.LogInformation(
            "Rate limited by provider. Waiting {Seconds} seconds before retry",
            retryAfterSeconds);

        await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds));
        return true;
    }

    private string GenerateUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => "A required file could not be found.",
            UnauthorizedAccessException => "Access denied. Please check permissions.",
            TimeoutException => "The operation took too long and timed out.",
            HttpRequestException => "Network request failed. Please check your internet connection.",
            _ => "An unexpected error occurred. Please try again."
        };
    }

    private List<string> GenerateManualActions(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => new List<string>
            {
                "Verify the file exists at the expected location",
                "Check if the file was moved or deleted",
                "Regenerate the file if possible"
            },
            UnauthorizedAccessException => new List<string>
            {
                "Check file and directory permissions",
                "Run the application with appropriate privileges",
                "Ensure the file is not locked by another application"
            },
            TimeoutException => new List<string>
            {
                "Retry the operation",
                "Check system resources",
                "Try with simpler or shorter input"
            },
            HttpRequestException => new List<string>
            {
                "Check your internet connection",
                "Verify firewall settings",
                "Try again in a few moments",
                "Check provider service status"
            },
            _ => new List<string>
            {
                "Try the operation again",
                "Check application logs for more details",
                "Contact support if the issue persists"
            }
        };
    }

    private AutomatedRecoveryOption? DetermineAutomatedRecovery(Exception exception)
    {
        return exception switch
        {
            ResourceException { ResourceType: ResourceType.FileLocked } => new AutomatedRecoveryOption
            {
                Name = "RetryWithDelay",
                Description = "Wait and retry accessing the file",
                EstimatedTimeSeconds = 6
            },
            ProviderException { IsTransient: true } => new AutomatedRecoveryOption
            {
                Name = "RetryWithBackoff",
                Description = "Retry with exponential backoff",
                EstimatedTimeSeconds = 15
            },
            ProviderException { SpecificErrorCode: ProviderErrorCode.RateLimit } => new AutomatedRecoveryOption
            {
                Name = "WaitForRateLimit",
                Description = "Wait for rate limit to reset",
                EstimatedTimeSeconds = 60
            },
            _ => null
        };
    }

    private List<TroubleshootingStep> GenerateTroubleshootingSteps(Exception exception)
    {
        var steps = new List<TroubleshootingStep>();

        if (exception is ProviderException provEx)
        {
            steps.Add(new TroubleshootingStep
            {
                Step = 1,
                Title = "Check Provider Configuration",
                Description = $"Verify that {provEx.ProviderName} is properly configured",
                Actions = new List<string>
                {
                    "Open Settings → Providers",
                    $"Verify {provEx.ProviderName} API key is set",
                    "Test the provider connection"
                }
            });

            if (provEx.SpecificErrorCode == ProviderErrorCode.NetworkError)
            {
                steps.Add(new TroubleshootingStep
                {
                    Step = 2,
                    Title = "Check Network Connectivity",
                    Description = "Verify your internet connection and firewall settings",
                    Actions = new List<string>
                    {
                        "Test internet connection",
                        "Check firewall settings",
                        "Verify proxy configuration if applicable"
                    }
                });
            }
        }
        else if (exception is FfmpegException ffmpegEx)
        {
            steps.Add(new TroubleshootingStep
            {
                Step = 1,
                Title = "Check FFmpeg Installation",
                Description = "Verify FFmpeg is properly installed",
                Actions = new List<string>
                {
                    "Open Settings → Download Center",
                    "Check FFmpeg status",
                    "Reinstall if necessary"
                }
            });
        }
        else if (exception is ResourceException resEx)
        {
            if (resEx.ResourceType == ResourceType.DiskSpace)
            {
                steps.Add(new TroubleshootingStep
                {
                    Step = 1,
                    Title = "Free Up Disk Space",
                    Description = "Insufficient disk space available",
                    Actions = new List<string>
                    {
                        "Delete temporary files",
                        "Remove old projects",
                        "Change output directory to a drive with more space"
                    }
                });
            }
        }

        return steps;
    }

    private List<DocumentationLink> GenerateDocumentationLinks(Exception exception)
    {
        var links = new List<DocumentationLink>();

        if (exception is ProviderException provEx)
        {
            links.Add(new DocumentationLink
            {
                Title = "Provider Configuration Guide",
                Url = "https://docs.aura.studio/providers/configuration",
                Description = "Learn how to configure and troubleshoot providers"
            });

            if (provEx.SpecificErrorCode == ProviderErrorCode.AuthFailed)
            {
                links.Add(new DocumentationLink
                {
                    Title = "API Key Setup",
                    Url = "https://docs.aura.studio/providers/api-keys",
                    Description = "How to obtain and configure API keys"
                });
            }
        }
        else if (exception is FfmpegException)
        {
            links.Add(new DocumentationLink
            {
                Title = "FFmpeg Installation Guide",
                Url = "https://docs.aura.studio/setup/ffmpeg",
                Description = "How to install and configure FFmpeg"
            });
        }

        // Always add general troubleshooting
        links.Add(new DocumentationLink
        {
            Title = "Troubleshooting Guide",
            Url = "https://docs.aura.studio/troubleshooting",
            Description = "General troubleshooting tips and common solutions"
        });

        return links;
    }

    private ErrorSeverity DetermineSeverity(Exception exception)
    {
        if (exception is AuraException auraEx && auraEx.IsTransient)
            return ErrorSeverity.Warning;

        return exception switch
        {
            ArgumentException or Aura.Core.Validation.ValidationException => ErrorSeverity.Warning,
            ProviderException { IsTransient: true } => ErrorSeverity.Warning,
            ResourceException { ResourceType: ResourceType.DiskSpace or ResourceType.Memory } => ErrorSeverity.Critical,
            FfmpegException { Category: FfmpegErrorCategory.NotFound } => ErrorSeverity.Critical,
            _ => ErrorSeverity.Error
        };
    }
}

/// <summary>
/// Comprehensive error recovery guidance
/// </summary>
public class ErrorRecoveryGuide
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string UserFriendlyMessage { get; set; } = string.Empty;
    public bool IsTransient { get; set; }
    public ErrorSeverity Severity { get; set; }
    public List<string> ManualActions { get; set; } = new();
    public AutomatedRecoveryOption? AutomatedRecovery { get; set; }
    public List<TroubleshootingStep> TroubleshootingSteps { get; set; } = new();
    public List<DocumentationLink> DocumentationLinks { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Automated recovery option
/// </summary>
public class AutomatedRecoveryOption
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int EstimatedTimeSeconds { get; init; }
}

/// <summary>
/// Troubleshooting step
/// </summary>
public class TroubleshootingStep
{
    public int Step { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public List<string> Actions { get; init; } = new();
}

/// <summary>
/// Documentation link
/// </summary>
public class DocumentationLink
{
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Result of automated recovery attempt
/// </summary>
public class RecoveryAttemptResult
{
    public required string CorrelationId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Exception? RecoveryError { get; set; }
}

/// <summary>
/// Error severity levels
/// </summary>
public enum ErrorSeverity
{
    Information,
    Warning,
    Error,
    Critical
}
