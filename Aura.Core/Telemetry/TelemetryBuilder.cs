using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aura.Core.Telemetry;

/// <summary>
/// Builder for creating RunTelemetryRecord instances with timing and common fields
/// </summary>
public class TelemetryBuilder
{
    private readonly Stopwatch _stopwatch = new();
    private string? _jobId;
    private string? _correlationId;
    private string? _projectId;
    private RunStage _stage;
    private int? _sceneIndex;
    private string? _modelId;
    private string? _provider;
    private SelectionSource? _selectionSource;
    private string? _fallbackReason;
    private int? _tokensIn;
    private int? _tokensOut;
    private bool? _cacheHit;
    private int _retries;
    private decimal? _costEstimate;
    private string _currency = "USD";
    private string? _pricingVersion;
    private ResultStatus _resultStatus = ResultStatus.Ok;
    private string? _errorCode;
    private string? _message;
    private Dictionary<string, object>? _metadata;
    private DateTime? _startedAt;
    
    private TelemetryBuilder()
    {
    }
    
    /// <summary>
    /// Create a new builder and start timing
    /// </summary>
    public static TelemetryBuilder Start(string jobId, string correlationId, RunStage stage)
    {
        var builder = new TelemetryBuilder
        {
            _jobId = jobId ?? throw new ArgumentNullException(nameof(jobId)),
            _correlationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId)),
            _stage = stage,
            _startedAt = DateTime.UtcNow
        };
        
        builder._stopwatch.Start();
        return builder;
    }
    
    public TelemetryBuilder WithProjectId(string? projectId)
    {
        _projectId = projectId;
        return this;
    }
    
    public TelemetryBuilder WithSceneIndex(int? sceneIndex)
    {
        _sceneIndex = sceneIndex;
        return this;
    }
    
    public TelemetryBuilder WithModel(string? modelId, string? provider)
    {
        _modelId = modelId;
        _provider = provider;
        return this;
    }
    
    public TelemetryBuilder WithSelection(SelectionSource? source, string? fallbackReason = null)
    {
        _selectionSource = source;
        _fallbackReason = fallbackReason;
        return this;
    }
    
    public TelemetryBuilder WithTokens(int? tokensIn, int? tokensOut)
    {
        _tokensIn = tokensIn;
        _tokensOut = tokensOut;
        return this;
    }
    
    public TelemetryBuilder WithCache(bool? hit)
    {
        _cacheHit = hit;
        return this;
    }
    
    public TelemetryBuilder WithRetries(int retries)
    {
        _retries = retries;
        return this;
    }
    
    public TelemetryBuilder WithCost(decimal? cost, string currency = "USD", string? pricingVersion = null)
    {
        _costEstimate = cost;
        _currency = currency;
        _pricingVersion = pricingVersion;
        return this;
    }
    
    public TelemetryBuilder WithStatus(ResultStatus status, string? errorCode = null, string? message = null)
    {
        _resultStatus = status;
        _errorCode = errorCode;
        _message = message;
        return this;
    }
    
    public TelemetryBuilder WithMetadata(Dictionary<string, object>? metadata)
    {
        _metadata = metadata;
        return this;
    }
    
    public TelemetryBuilder AddMetadata(string key, object value)
    {
        _metadata ??= new Dictionary<string, object>();
        _metadata[key] = value;
        return this;
    }
    
    /// <summary>
    /// Build the telemetry record (stops timing automatically)
    /// </summary>
    public RunTelemetryRecord Build()
    {
        _stopwatch.Stop();
        
        return new RunTelemetryRecord
        {
            JobId = _jobId ?? throw new InvalidOperationException("JobId is required"),
            CorrelationId = _correlationId ?? throw new InvalidOperationException("CorrelationId is required"),
            ProjectId = _projectId,
            Stage = _stage,
            SceneIndex = _sceneIndex,
            ModelId = _modelId,
            Provider = _provider,
            SelectionSource = _selectionSource,
            FallbackReason = _fallbackReason,
            TokensIn = _tokensIn,
            TokensOut = _tokensOut,
            CacheHit = _cacheHit,
            Retries = _retries,
            LatencyMs = _stopwatch.ElapsedMilliseconds,
            CostEstimate = _costEstimate,
            Currency = _currency,
            PricingVersion = _pricingVersion,
            ResultStatus = _resultStatus,
            ErrorCode = _errorCode,
            Message = _message,
            StartedAt = _startedAt ?? DateTime.UtcNow,
            EndedAt = DateTime.UtcNow,
            Metadata = _metadata
        };
    }
}

/// <summary>
/// Extension methods for common telemetry scenarios
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Create telemetry for an LLM operation
    /// </summary>
    public static RunTelemetryRecord CreateLlmTelemetry(
        string jobId,
        string correlationId,
        RunStage stage,
        string modelId,
        string provider,
        int tokensIn,
        int tokensOut,
        long latencyMs,
        decimal? cost,
        bool cacheHit = false,
        int retries = 0,
        ResultStatus status = ResultStatus.Ok,
        string? errorCode = null,
        string? message = null)
    {
        return new RunTelemetryRecord
        {
            JobId = jobId,
            CorrelationId = correlationId,
            Stage = stage,
            ModelId = modelId,
            Provider = provider,
            SelectionSource = SelectionSource.Default,
            TokensIn = tokensIn,
            TokensOut = tokensOut,
            CacheHit = cacheHit,
            Retries = retries,
            LatencyMs = latencyMs,
            CostEstimate = cost,
            ResultStatus = status,
            ErrorCode = errorCode,
            Message = message,
            StartedAt = DateTime.UtcNow.AddMilliseconds(-latencyMs),
            EndedAt = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Create telemetry for a TTS operation
    /// </summary>
    public static RunTelemetryRecord CreateTtsTelemetry(
        string jobId,
        string correlationId,
        int? sceneIndex,
        string provider,
        int characters,
        double durationSeconds,
        long latencyMs,
        decimal? cost,
        int retries = 0,
        ResultStatus status = ResultStatus.Ok,
        string? errorCode = null,
        string? message = null)
    {
        return new RunTelemetryRecord
        {
            JobId = jobId,
            CorrelationId = correlationId,
            Stage = RunStage.Tts,
            SceneIndex = sceneIndex,
            Provider = provider,
            SelectionSource = SelectionSource.Default,
            Retries = retries,
            LatencyMs = latencyMs,
            CostEstimate = cost,
            ResultStatus = status,
            ErrorCode = errorCode,
            Message = message,
            StartedAt = DateTime.UtcNow.AddMilliseconds(-latencyMs),
            EndedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["characters"] = characters,
                ["duration_seconds"] = durationSeconds
            }
        };
    }
}
