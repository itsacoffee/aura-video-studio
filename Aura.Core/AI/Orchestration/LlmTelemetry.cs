using System;
using System.Collections.Generic;
using System.Linq;

namespace Aura.Core.AI.Orchestration;

/// <summary>
/// Telemetry data for a single LLM operation
/// </summary>
public record LlmOperationTelemetry
{
    /// <summary>
    /// Unique operation identifier
    /// </summary>
    public string OperationId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Session/job identifier this operation belongs to
    /// </summary>
    public string SessionId { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of operation performed
    /// </summary>
    public LlmOperationType OperationType { get; init; }
    
    /// <summary>
    /// Provider name (OpenAI, Anthropic, etc.)
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;
    
    /// <summary>
    /// Model name used
    /// </summary>
    public string ModelName { get; init; } = string.Empty;
    
    /// <summary>
    /// Input tokens used
    /// </summary>
    public int TokensIn { get; init; }
    
    /// <summary>
    /// Output tokens generated
    /// </summary>
    public int TokensOut { get; init; }
    
    /// <summary>
    /// Total tokens (in + out)
    /// </summary>
    public int TotalTokens => TokensIn + TokensOut;
    
    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; init; }
    
    /// <summary>
    /// Latency in milliseconds
    /// </summary>
    public long LatencyMs { get; init; }
    
    /// <summary>
    /// Whether the operation completed successfully
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Whether cache was hit
    /// </summary>
    public bool CacheHit { get; init; }
    
    /// <summary>
    /// Estimated cost in USD
    /// </summary>
    public decimal EstimatedCost { get; init; }
    
    /// <summary>
    /// Timestamp when operation started
    /// </summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when operation completed
    /// </summary>
    public DateTime CompletedAt { get; init; }
    
    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Temperature used
    /// </summary>
    public double Temperature { get; init; }
    
    /// <summary>
    /// Top-p value used
    /// </summary>
    public double TopP { get; init; }
}

/// <summary>
/// Aggregated telemetry statistics
/// </summary>
public class LlmTelemetryStatistics
{
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public double CacheHitRate => TotalOperations > 0 ? (double)CacheHits / TotalOperations : 0.0;
    public long TotalTokensIn { get; set; }
    public long TotalTokensOut { get; set; }
    public long TotalTokens => TotalTokensIn + TotalTokensOut;
    public decimal TotalEstimatedCost { get; set; }
    public long AverageLatencyMs { get; set; }
    public long MedianLatencyMs { get; set; }
    public long P95LatencyMs { get; set; }
    public long P99LatencyMs { get; set; }
    public int TotalRetries { get; set; }
    public Dictionary<string, int> OperationsByProvider { get; set; } = new();
    public Dictionary<LlmOperationType, int> OperationsByType { get; set; } = new();
    public DateTime? FirstOperationAt { get; set; }
    public DateTime? LastOperationAt { get; set; }
    
    public static LlmTelemetryStatistics FromOperations(IEnumerable<LlmOperationTelemetry> operations)
    {
        var opList = operations.ToList();
        var latencies = opList.Select(o => o.LatencyMs).OrderBy(l => l).ToList();
        
        var stats = new LlmTelemetryStatistics
        {
            TotalOperations = opList.Count,
            SuccessfulOperations = opList.Count(o => o.Success),
            FailedOperations = opList.Count(o => !o.Success),
            CacheHits = opList.Count(o => o.CacheHit),
            CacheMisses = opList.Count(o => !o.CacheHit),
            TotalTokensIn = opList.Sum(o => (long)o.TokensIn),
            TotalTokensOut = opList.Sum(o => (long)o.TokensOut),
            TotalEstimatedCost = opList.Sum(o => o.EstimatedCost),
            TotalRetries = opList.Sum(o => o.RetryCount),
            AverageLatencyMs = opList.Any() ? (long)opList.Average(o => o.LatencyMs) : 0,
            MedianLatencyMs = latencies.Any() ? latencies[latencies.Count / 2] : 0,
            P95LatencyMs = latencies.Any() ? latencies[(int)(latencies.Count * 0.95)] : 0,
            P99LatencyMs = latencies.Any() ? latencies[(int)(latencies.Count * 0.99)] : 0,
            FirstOperationAt = opList.Any() ? opList.Min(o => o.StartedAt) : null,
            LastOperationAt = opList.Any() ? opList.Max(o => o.CompletedAt) : null
        };
        
        foreach (var op in opList)
        {
            stats.OperationsByProvider.TryGetValue(op.ProviderName, out var count);
            stats.OperationsByProvider[op.ProviderName] = count + 1;
            
            stats.OperationsByType.TryGetValue(op.OperationType, out var typeCount);
            stats.OperationsByType[op.OperationType] = typeCount + 1;
        }
        
        return stats;
    }
}

/// <summary>
/// Collector for LLM operation telemetry
/// </summary>
public class LlmTelemetryCollector
{
    private readonly List<LlmOperationTelemetry> _operations = new();
    private readonly object _lock = new();
    private readonly int _maxOperations;
    
    public LlmTelemetryCollector(int maxOperations = 1000)
    {
        _maxOperations = maxOperations;
    }
    
    /// <summary>
    /// Records telemetry for an operation
    /// </summary>
    public void Record(LlmOperationTelemetry telemetry)
    {
        lock (_lock)
        {
            _operations.Add(telemetry);
            
            if (_operations.Count > _maxOperations)
            {
                _operations.RemoveAt(0);
            }
        }
    }
    
    /// <summary>
    /// Gets all recorded operations
    /// </summary>
    public IReadOnlyList<LlmOperationTelemetry> GetOperations()
    {
        lock (_lock)
        {
            return _operations.ToList();
        }
    }
    
    /// <summary>
    /// Gets operations for a specific session
    /// </summary>
    public IReadOnlyList<LlmOperationTelemetry> GetSessionOperations(string sessionId)
    {
        lock (_lock)
        {
            return _operations.Where(o => o.SessionId == sessionId).ToList();
        }
    }
    
    /// <summary>
    /// Gets statistics for all operations
    /// </summary>
    public LlmTelemetryStatistics GetStatistics()
    {
        lock (_lock)
        {
            return LlmTelemetryStatistics.FromOperations(_operations);
        }
    }
    
    /// <summary>
    /// Gets statistics for a specific session
    /// </summary>
    public LlmTelemetryStatistics GetSessionStatistics(string sessionId)
    {
        lock (_lock)
        {
            var sessionOps = _operations.Where(o => o.SessionId == sessionId);
            return LlmTelemetryStatistics.FromOperations(sessionOps);
        }
    }
    
    /// <summary>
    /// Clears all recorded operations
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _operations.Clear();
        }
    }
    
    /// <summary>
    /// Clears operations for a specific session
    /// </summary>
    public void ClearSession(string sessionId)
    {
        lock (_lock)
        {
            _operations.RemoveAll(o => o.SessionId == sessionId);
        }
    }
}
