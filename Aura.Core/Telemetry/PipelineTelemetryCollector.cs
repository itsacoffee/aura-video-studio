using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Telemetry;

/// <summary>
/// Collects telemetry during pipeline execution and produces a summary record.
/// Thread-safe collector that aggregates metrics across all pipeline stages.
/// </summary>
public class PipelineTelemetryCollector
{
    private readonly ILogger<PipelineTelemetryCollector> _logger;
    private readonly string _pipelineId;
    private readonly string _correlationId;
    private readonly Stopwatch _stopwatch;
    private readonly DateTime _startedAt;
    private readonly string? _artifactsBasePath;

    private int _inputTokens;
    private int _outputTokens;
    private decimal _totalCost;
    private decimal _costSavedByCache;
    private int _cacheHits;
    private int _cacheMisses;
    private readonly Dictionary<string, decimal> _costByStage = new();
    private readonly Dictionary<string, decimal> _costByProvider = new();
    private readonly Dictionary<string, long> _stageTimingsMs = new();
    private readonly Dictionary<string, int> _operationsByProvider = new();
    private readonly Dictionary<string, int> _retryCountByProvider = new();
    private readonly object _lock = new();

    /// <summary>
    /// Video topic being generated
    /// </summary>
    public string Topic { get; set; } = "";

    /// <summary>
    /// Project ID if applicable
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Number of scenes in the video
    /// </summary>
    public int SceneCount { get; set; }

    /// <summary>
    /// Duration of the video in seconds
    /// </summary>
    public double VideoDurationSeconds { get; set; }

    /// <summary>
    /// Total characters processed by TTS
    /// </summary>
    public int TtsCharacters { get; set; }

    /// <summary>
    /// Number of images generated
    /// </summary>
    public int ImagesGenerated { get; set; }

    /// <summary>
    /// Strategy type used for generation
    /// </summary>
    public string? StrategyType { get; set; }

    /// <summary>
    /// Visual approach used
    /// </summary>
    public string? VisualApproach { get; set; }

    /// <summary>
    /// Maximum concurrency level used
    /// </summary>
    public int MaxConcurrency { get; set; }

    /// <summary>
    /// Quality score for the generated video
    /// </summary>
    public double? QualityScore { get; set; }

    /// <summary>
    /// Creates a new pipeline telemetry collector
    /// </summary>
    /// <param name="logger">Logger for telemetry events</param>
    /// <param name="correlationId">Correlation ID for tracing</param>
    /// <param name="artifactsBasePath">Optional path for persisting telemetry</param>
    public PipelineTelemetryCollector(
        ILogger<PipelineTelemetryCollector> logger,
        string correlationId,
        string? artifactsBasePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        _pipelineId = Guid.NewGuid().ToString();
        _startedAt = DateTime.UtcNow;
        _stopwatch = Stopwatch.StartNew();
        _artifactsBasePath = artifactsBasePath;

        _logger.LogDebug(
            "Pipeline telemetry collection started. PipelineId: {PipelineId}, CorrelationId: {CorrelationId}",
            _pipelineId, _correlationId);
    }

    /// <summary>
    /// Records token usage and cost for an operation
    /// </summary>
    /// <param name="stage">Pipeline stage name</param>
    /// <param name="provider">Provider name</param>
    /// <param name="inputTokens">Input tokens used</param>
    /// <param name="outputTokens">Output tokens generated</param>
    /// <param name="cost">Cost of the operation</param>
    public void RecordTokenUsage(string stage, string provider, int inputTokens, int outputTokens, decimal cost)
    {
        lock (_lock)
        {
            _inputTokens += inputTokens;
            _outputTokens += outputTokens;
            _totalCost += cost;

            _costByStage[stage] = _costByStage.GetValueOrDefault(stage) + cost;
            _costByProvider[provider] = _costByProvider.GetValueOrDefault(provider) + cost;
            _operationsByProvider[provider] = _operationsByProvider.GetValueOrDefault(provider) + 1;

            _logger.LogDebug(
                "Recorded token usage: Stage={Stage}, Provider={Provider}, InTokens={InTokens}, OutTokens={OutTokens}, Cost={Cost:F6}",
                stage, provider, inputTokens, outputTokens, cost);
        }
    }

    /// <summary>
    /// Records the duration of a pipeline stage
    /// </summary>
    /// <param name="stage">Pipeline stage name</param>
    /// <param name="duration">Duration of the stage</param>
    public void RecordStageTiming(string stage, TimeSpan duration)
    {
        lock (_lock)
        {
            _stageTimingsMs[stage] = (long)duration.TotalMilliseconds;

            _logger.LogDebug(
                "Recorded stage timing: Stage={Stage}, Duration={Duration}ms",
                stage, duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Records a cache hit, optionally with cost saved
    /// </summary>
    /// <param name="savedCost">Estimated cost that was saved by the cache hit</param>
    public void RecordCacheHit(decimal savedCost = 0)
    {
        lock (_lock)
        {
            _cacheHits++;
            _costSavedByCache += savedCost;

            _logger.LogDebug("Recorded cache hit. Total hits: {CacheHits}, Cost saved: {SavedCost:F6}", 
                _cacheHits, _costSavedByCache);
        }
    }

    /// <summary>
    /// Records a cache miss
    /// </summary>
    public void RecordCacheMiss()
    {
        lock (_lock)
        {
            _cacheMisses++;

            _logger.LogDebug("Recorded cache miss. Total misses: {CacheMisses}", _cacheMisses);
        }
    }

    /// <summary>
    /// Records a retry attempt for a provider
    /// </summary>
    /// <param name="provider">Provider name</param>
    public void RecordRetry(string provider)
    {
        lock (_lock)
        {
            _retryCountByProvider[provider] = _retryCountByProvider.GetValueOrDefault(provider) + 1;

            _logger.LogDebug("Recorded retry for provider {Provider}. Total retries: {RetryCount}", 
                provider, _retryCountByProvider[provider]);
        }
    }

    /// <summary>
    /// Records a provider operation without token/cost details
    /// </summary>
    /// <param name="provider">Provider name</param>
    public void RecordProviderOperation(string provider)
    {
        lock (_lock)
        {
            _operationsByProvider[provider] = _operationsByProvider.GetValueOrDefault(provider) + 1;

            _logger.LogDebug("Recorded operation for provider {Provider}. Total operations: {Count}", 
                provider, _operationsByProvider[provider]);
        }
    }

    /// <summary>
    /// Completes telemetry collection and returns the summary
    /// </summary>
    /// <param name="success">Whether pipeline completed successfully</param>
    /// <param name="errorMessage">Error message if failed</param>
    /// <returns>Pipeline summary telemetry record</returns>
    public PipelineSummaryTelemetry Complete(bool success, string? errorMessage = null)
    {
        _stopwatch.Stop();
        var completedAt = DateTime.UtcNow;

        var summary = new PipelineSummaryTelemetry
        {
            PipelineId = _pipelineId,
            CorrelationId = _correlationId,
            ProjectId = ProjectId,
            Topic = Topic,
            StartedAt = _startedAt,
            CompletedAt = completedAt,
            Success = success,
            ErrorMessage = errorMessage,
            TotalInputTokens = _inputTokens,
            TotalOutputTokens = _outputTokens,
            TotalCost = _totalCost,
            CostByStage = new Dictionary<string, decimal>(_costByStage),
            CostByProvider = new Dictionary<string, decimal>(_costByProvider),
            StageTimingsMs = new Dictionary<string, long>(_stageTimingsMs),
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses,
            CostSavedByCache = _costSavedByCache,
            OperationsByProvider = new Dictionary<string, int>(_operationsByProvider),
            RetryCountByProvider = new Dictionary<string, int>(_retryCountByProvider),
            SceneCount = SceneCount,
            VideoDurationSeconds = VideoDurationSeconds,
            TtsCharacters = TtsCharacters,
            ImagesGenerated = ImagesGenerated,
            StrategyType = StrategyType,
            VisualApproach = VisualApproach,
            MaxConcurrency = MaxConcurrency,
            QualityScore = QualityScore
        };

        _logger.LogInformation(
            "Pipeline {PipelineId} completed: Success={Success}, Duration={Duration}ms, " +
            "Tokens={Tokens}, Cost=${Cost:F4}, CacheHits={CacheHits}",
            _pipelineId, success, _stopwatch.ElapsedMilliseconds,
            summary.TotalTokens, _totalCost, _cacheHits);

        // Persist if artifacts path is configured
        if (_artifactsBasePath != null)
        {
            PersistSummary(summary);
        }

        return summary;
    }

    /// <summary>
    /// Persists the summary to disk
    /// </summary>
    private void PersistSummary(PipelineSummaryTelemetry summary)
    {
        try
        {
            var summaryDir = Path.Combine(_artifactsBasePath!, "pipeline-summaries");
            Directory.CreateDirectory(summaryDir);

            var filePath = Path.Combine(summaryDir, $"{_pipelineId}.json");

            var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            File.WriteAllText(filePath, json);

            _logger.LogDebug("Pipeline summary persisted to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist pipeline summary for {PipelineId}", _pipelineId);
        }
    }

    /// <summary>
    /// Loads a pipeline summary from disk
    /// </summary>
    /// <param name="artifactsBasePath">Base path for artifacts</param>
    /// <param name="pipelineId">Pipeline ID to load</param>
    /// <returns>Pipeline summary or null if not found</returns>
    public static PipelineSummaryTelemetry? LoadSummary(string artifactsBasePath, string pipelineId)
    {
        try
        {
            var filePath = Path.Combine(artifactsBasePath, "pipeline-summaries", $"{pipelineId}.json");

            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<PipelineSummaryTelemetry>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Lists recent pipeline summaries from disk
    /// </summary>
    /// <param name="artifactsBasePath">Base path for artifacts</param>
    /// <param name="limit">Maximum number of summaries to return</param>
    /// <param name="since">Optional filter for summaries since a specific date</param>
    /// <returns>List of pipeline summaries</returns>
    public static List<PipelineSummaryTelemetry> ListRecentSummaries(
        string artifactsBasePath, 
        int limit = 20, 
        DateTime? since = null)
    {
        var results = new List<PipelineSummaryTelemetry>();

        try
        {
            var summaryDir = Path.Combine(artifactsBasePath, "pipeline-summaries");

            if (!Directory.Exists(summaryDir))
            {
                return results;
            }

            var files = Directory.GetFiles(summaryDir, "*.json")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Take(limit * 2); // Get extra to account for filtering

            foreach (var file in files)
            {
                if (results.Count >= limit)
                    break;

                try
                {
                    var json = File.ReadAllText(file.FullName);
                    var summary = JsonSerializer.Deserialize<PipelineSummaryTelemetry>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (summary != null)
                    {
                        if (since == null || summary.StartedAt >= since.Value)
                        {
                            results.Add(summary);
                        }
                    }
                }
                catch
                {
                    // Skip malformed files
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return results;
    }
}
