using Aura.Core.Errors;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ErrorHandling;

/// <summary>
/// Service for implementing graceful degradation strategies when primary operations fail
/// </summary>
public class GracefulDegradationService
{
    private readonly ILogger<GracefulDegradationService> _logger;

    public GracefulDegradationService(ILogger<GracefulDegradationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Execute an operation with fallback strategies
    /// </summary>
    public async Task<DegradationResult<T>> ExecuteWithFallbackAsync<T>(
        Func<Task<T>> primaryOperation,
        List<FallbackStrategy<T>> fallbackStrategies,
        string operationName,
        string? correlationId = null)
    {
        var attemptHistory = new List<AttemptResult>();
        correlationId ??= Guid.NewGuid().ToString("N");

        // Try primary operation
        try
        {
            _logger.LogInformation("Executing primary operation: {Operation} [CorrelationId: {CorrelationId}]", 
                operationName, correlationId);
            
            var result = await primaryOperation();
            
            attemptHistory.Add(new AttemptResult
            {
                Strategy = "Primary",
                Success = true,
                Timestamp = DateTime.UtcNow
            });

            return new DegradationResult<T>
            {
                Success = true,
                Result = result,
                UsedFallback = false,
                AttemptHistory = attemptHistory,
                CorrelationId = correlationId
            };
        }
        catch (Exception primaryEx)
        {
            _logger.LogWarning(primaryEx, 
                "Primary operation failed: {Operation} [CorrelationId: {CorrelationId}]", 
                operationName, correlationId);

            attemptHistory.Add(new AttemptResult
            {
                Strategy = "Primary",
                Success = false,
                Error = primaryEx.Message,
                Timestamp = DateTime.UtcNow
            });

            // Try fallback strategies in order
            foreach (var fallback in fallbackStrategies)
            {
                // Check if this fallback is applicable for the error
                if (!fallback.IsApplicable(primaryEx))
                {
                    _logger.LogDebug("Fallback strategy {Strategy} not applicable for error type {ErrorType}",
                        fallback.Name, primaryEx.GetType().Name);
                    continue;
                }

                try
                {
                    _logger.LogInformation("Attempting fallback strategy: {Strategy} [CorrelationId: {CorrelationId}]",
                        fallback.Name, correlationId);

                    var fallbackResult = await fallback.Execute(primaryEx);

                    attemptHistory.Add(new AttemptResult
                    {
                        Strategy = fallback.Name,
                        Success = true,
                        Timestamp = DateTime.UtcNow,
                        QualityDegradation = fallback.QualityDegradation
                    });

                    return new DegradationResult<T>
                    {
                        Success = true,
                        Result = fallbackResult,
                        UsedFallback = true,
                        FallbackStrategy = fallback.Name,
                        QualityDegradation = fallback.QualityDegradation,
                        AttemptHistory = attemptHistory,
                        CorrelationId = correlationId,
                        UserNotification = fallback.UserNotification
                    };
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogWarning(fallbackEx, 
                        "Fallback strategy {Strategy} failed [CorrelationId: {CorrelationId}]",
                        fallback.Name, correlationId);

                    attemptHistory.Add(new AttemptResult
                    {
                        Strategy = fallback.Name,
                        Success = false,
                        Error = fallbackEx.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            // All strategies failed
            return new DegradationResult<T>
            {
                Success = false,
                Error = primaryEx,
                AttemptHistory = attemptHistory,
                CorrelationId = correlationId
            };
        }
    }

    /// <summary>
    /// Create a fallback strategy for FFmpeg not found
    /// </summary>
    public FallbackStrategy<T> CreateFfmpegFallback<T>(
        Func<Task<T>> fallbackOperation,
        string userNotification = "FFmpeg not found. Using alternative rendering method with reduced quality.")
    {
        return new FallbackStrategy<T>
        {
            Name = "FFmpegFallback",
            Execute = _ => fallbackOperation(),
            IsApplicable = ex => ex is FfmpegException ffEx && 
                (ffEx.Category == FfmpegErrorCategory.NotFound || 
                 ffEx.Category == FfmpegErrorCategory.Corrupted),
            QualityDegradation = QualityDegradation.Moderate,
            UserNotification = userNotification
        };
    }

    /// <summary>
    /// Create a fallback strategy for GPU failures
    /// </summary>
    public FallbackStrategy<T> CreateGpuToC puFallback<T>(
        Func<Task<T>> cpuOperation,
        string userNotification = "GPU rendering failed. Using CPU rendering (slower but reliable).")
    {
        return new FallbackStrategy<T>
        {
            Name = "GpuToCpuFallback",
            Execute = _ => cpuOperation(),
            IsApplicable = ex => 
                ex.Message.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("CUDA", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("NVENC", StringComparison.OrdinalIgnoreCase) ||
                ex is RenderException renderEx && renderEx.Category == RenderErrorCategory.HardwareEncoderFailed,
            QualityDegradation = QualityDegradation.Minor,
            UserNotification = userNotification
        };
    }

    /// <summary>
    /// Create a fallback strategy for provider failures
    /// </summary>
    public FallbackStrategy<T> CreateProviderFallback<T>(
        Func<Task<T>> alternativeProviderOperation,
        string alternativeProviderName,
        string userNotification = null!)
    {
        userNotification ??= $"Primary provider failed. Using alternative provider: {alternativeProviderName}";

        return new FallbackStrategy<T>
        {
            Name = $"ProviderFallback-{alternativeProviderName}",
            Execute = _ => alternativeProviderOperation(),
            IsApplicable = ex => ex is ProviderException,
            QualityDegradation = QualityDegradation.Minor,
            UserNotification = userNotification
        };
    }

    /// <summary>
    /// Create a fallback strategy with reduced quality/preview mode
    /// </summary>
    public FallbackStrategy<T> CreateLowQualityFallback<T>(
        Func<Task<T>> lowQualityOperation,
        string userNotification = "Rendering in low quality mode to save resources.")
    {
        return new FallbackStrategy<T>
        {
            Name = "LowQualityFallback",
            Execute = _ => lowQualityOperation(),
            IsApplicable = ex => 
                ex is ResourceException ||
                ex.Message.Contains("memory", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("disk space", StringComparison.OrdinalIgnoreCase),
            QualityDegradation = QualityDegradation.Significant,
            UserNotification = userNotification
        };
    }

    /// <summary>
    /// Create a partial save fallback that saves whatever was completed successfully
    /// </summary>
    public FallbackStrategy<T> CreatePartialSaveFallback<T>(
        Func<Exception, Task<T>> partialSaveOperation,
        string userNotification = "Saving partial results. Some content may be incomplete.")
    {
        return new FallbackStrategy<T>
        {
            Name = "PartialSave",
            Execute = partialSaveOperation,
            IsApplicable = _ => true, // Always applicable as last resort
            QualityDegradation = QualityDegradation.Severe,
            UserNotification = userNotification
        };
    }
}

/// <summary>
/// Represents a fallback strategy for graceful degradation
/// </summary>
public class FallbackStrategy<T>
{
    public required string Name { get; init; }
    public required Func<Exception, Task<T>> Execute { get; init; }
    public required Func<Exception, bool> IsApplicable { get; init; }
    public QualityDegradation QualityDegradation { get; init; }
    public string? UserNotification { get; init; }
}

/// <summary>
/// Result of an operation with graceful degradation
/// </summary>
public class DegradationResult<T>
{
    public bool Success { get; init; }
    public T? Result { get; init; }
    public Exception? Error { get; init; }
    public bool UsedFallback { get; init; }
    public string? FallbackStrategy { get; init; }
    public QualityDegradation? QualityDegradation { get; init; }
    public List<AttemptResult> AttemptHistory { get; init; } = new();
    public string? CorrelationId { get; init; }
    public string? UserNotification { get; init; }
}

/// <summary>
/// Result of a single attempt (primary or fallback)
/// </summary>
public class AttemptResult
{
    public required string Strategy { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
    public DateTime Timestamp { get; init; }
    public QualityDegradation? QualityDegradation { get; init; }
}

/// <summary>
/// Degree of quality degradation in fallback
/// </summary>
public enum QualityDegradation
{
    None,
    Minor,      // e.g., CPU vs GPU
    Moderate,   // e.g., Alternative rendering
    Significant, // e.g., Low quality mode
    Severe      // e.g., Partial/incomplete results
}
