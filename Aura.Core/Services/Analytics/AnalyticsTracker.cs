using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Analytics;

/// <summary>
/// Helper service for tracking analytics throughout the application
/// Provides convenient methods to record usage, costs, and performance
/// </summary>
public interface IAnalyticsTracker
{
    /// <summary>
    /// Track a generation operation with automatic cost calculation
    /// </summary>
    Task<IDisposable> TrackGenerationAsync(
        string generationType,
        string provider,
        string? model = null,
        string? projectId = null,
        string? jobId = null,
        string? featureUsed = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Track a rendering operation
    /// </summary>
    Task<IDisposable> TrackRenderingAsync(
        string? projectId = null,
        string? jobId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Complete a tracked operation
    /// </summary>
    Task CompleteOperationAsync(
        GenerationTracker tracker,
        bool success,
        long inputTokens = 0,
        long outputTokens = 0,
        string? errorMessage = null,
        int? sceneCount = null,
        double? outputDurationSeconds = null);
}

public class AnalyticsTracker : IAnalyticsTracker
{
    private readonly IUsageAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsTracker> _logger;

    public AnalyticsTracker(
        IUsageAnalyticsService analyticsService,
        ILogger<AnalyticsTracker> logger)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IDisposable> TrackGenerationAsync(
        string generationType,
        string provider,
        string? model = null,
        string? projectId = null,
        string? jobId = null,
        string? featureUsed = null,
        CancellationToken cancellationToken = default)
    {
        return new GenerationTracker(
            _analyticsService,
            _logger,
            generationType,
            provider,
            model,
            projectId,
            jobId,
            featureUsed,
            "generation");
    }

    public async Task<IDisposable> TrackRenderingAsync(
        string? projectId = null,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        return new GenerationTracker(
            _analyticsService,
            _logger,
            "rendering",
            "ffmpeg",
            null,
            projectId,
            jobId,
            "rendering",
            "rendering");
    }

    public async Task CompleteOperationAsync(
        GenerationTracker tracker,
        bool success,
        long inputTokens = 0,
        long outputTokens = 0,
        string? errorMessage = null,
        int? sceneCount = null,
        double? outputDurationSeconds = null)
    {
        await tracker.CompleteAsync(success, inputTokens, outputTokens, errorMessage, sceneCount, outputDurationSeconds).ConfigureAwait(false);
    }
}

/// <summary>
/// Tracks a single generation operation from start to finish
/// Automatically records duration and calculates costs
/// </summary>
public class GenerationTracker : IDisposable
{
    private readonly IUsageAnalyticsService _analyticsService;
    private readonly ILogger _logger;
    private readonly Stopwatch _stopwatch;
    private readonly DateTime _startTime;
    
    private readonly string _generationType;
    private readonly string _provider;
    private readonly string? _model;
    private readonly string? _projectId;
    private readonly string? _jobId;
    private readonly string? _featureUsed;
    private readonly string _operationType;
    
    private bool _completed;
    private bool _disposed;

    public GenerationTracker(
        IUsageAnalyticsService analyticsService,
        ILogger logger,
        string generationType,
        string provider,
        string? model,
        string? projectId,
        string? jobId,
        string? featureUsed,
        string operationType)
    {
        _analyticsService = analyticsService;
        _logger = logger;
        _generationType = generationType;
        _provider = provider;
        _model = model;
        _projectId = projectId;
        _jobId = jobId;
        _featureUsed = featureUsed;
        _operationType = operationType;
        
        _stopwatch = Stopwatch.StartNew();
        _startTime = DateTime.UtcNow;
    }

    public async Task CompleteAsync(
        bool success,
        long inputTokens = 0,
        long outputTokens = 0,
        string? errorMessage = null,
        int? sceneCount = null,
        double? outputDurationSeconds = null,
        bool isRetry = false,
        int? retryAttempt = null)
    {
        if (_completed)
        {
            return;
        }

        _stopwatch.Stop();
        _completed = true;

        try
        {
            // Record usage statistics
            var usage = new UsageStatisticsEntity
            {
                ProjectId = _projectId,
                JobId = _jobId,
                GenerationType = _generationType,
                Provider = _provider,
                Model = _model,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                DurationMs = _stopwatch.ElapsedMilliseconds,
                Success = success,
                ErrorMessage = errorMessage,
                FeatureUsed = _featureUsed,
                OutputDurationSeconds = outputDurationSeconds,
                SceneCount = sceneCount,
                IsRetry = isRetry,
                RetryAttempt = retryAttempt,
                Timestamp = _startTime
            };

            await _analyticsService.RecordUsageAsync(usage).ConfigureAwait(false);

            // Record cost if tokens were used
            if (inputTokens > 0 || outputTokens > 0)
            {
                var estimatedCost = await _analyticsService.EstimateCostAsync(
                    _provider,
                    _model ?? "unknown",
                    inputTokens,
                    outputTokens).ConfigureAwait(false);

                if (estimatedCost > 0)
                {
                    var cost = new CostTrackingEntity
                    {
                        ProjectId = _projectId,
                        JobId = _jobId,
                        Provider = _provider,
                        Model = _model ?? "unknown",
                        InputTokens = inputTokens,
                        OutputTokens = outputTokens,
                        TotalCost = estimatedCost,
                        YearMonth = _startTime.ToString("yyyy-MM"),
                        Timestamp = _startTime
                    };

                    await _analyticsService.RecordCostAsync(cost).ConfigureAwait(false);
                }
            }

            // Record performance metrics
            var performance = new PerformanceMetricsEntity
            {
                ProjectId = _projectId,
                JobId = _jobId,
                OperationType = _operationType,
                DurationMs = _stopwatch.ElapsedMilliseconds,
                Success = success,
                ErrorMessage = errorMessage,
                Timestamp = _startTime
            };

            await _analyticsService.RecordPerformanceAsync(performance).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete analytics tracking");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (!_completed)
        {
            // If not explicitly completed, record as failed
            _ = CompleteAsync(
                success: false,
                errorMessage: "Operation was not completed (disposed without completion)");
        }

        _disposed = true;
    }
}

/// <summary>
/// Helper extension methods for easy analytics tracking
/// </summary>
public static class AnalyticsTrackerExtensions
{
    /// <summary>
    /// Track a generation with automatic completion
    /// </summary>
    public static async Task<TResult> TrackGenerationAsync<TResult>(
        this IAnalyticsTracker tracker,
        string generationType,
        string provider,
        string? model,
        Func<Task<TResult>> operation,
        Func<TResult, (long inputTokens, long outputTokens)>? extractTokens = null,
        string? projectId = null,
        string? jobId = null,
        string? featureUsed = null)
    {
        using var tracking = await tracker.TrackGenerationAsync(
            generationType, provider, model, projectId, jobId, featureUsed).ConfigureAwait(false);

        try
        {
            var result = await operation().ConfigureAwait(false);
            
            var (inputTokens, outputTokens) = extractTokens != null 
                ? extractTokens(result) 
                : (0, 0);

            await ((GenerationTracker)tracking).CompleteAsync(
                success: true,
                inputTokens: inputTokens,
                outputTokens: outputTokens).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            await ((GenerationTracker)tracking).CompleteAsync(
                success: false,
                errorMessage: ex.Message).ConfigureAwait(false);
            throw;
        }
    }
}
