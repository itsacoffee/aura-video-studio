using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Telemetry;

/// <summary>
/// Collects and persists run telemetry records for a video generation job
/// Thread-safe collector that aggregates telemetry and writes per-run JSON files
/// </summary>
public class RunTelemetryCollector
{
    private readonly ILogger<RunTelemetryCollector> _logger;
    private readonly string _artifactsBasePath;
    private readonly object _lock = new();
    private readonly List<RunTelemetryRecord> _records = new();
    
    private string? _jobId;
    private string? _correlationId;
    private DateTime? _collectionStartedAt;
    
    public RunTelemetryCollector(
        ILogger<RunTelemetryCollector> logger,
        string artifactsBasePath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _artifactsBasePath = artifactsBasePath ?? throw new ArgumentNullException(nameof(artifactsBasePath));
    }
    
    /// <summary>
    /// Initialize collection for a new job
    /// </summary>
    public void StartCollection(string jobId, string correlationId)
    {
        lock (_lock)
        {
            _jobId = jobId;
            _correlationId = correlationId;
            _collectionStartedAt = DateTime.UtcNow;
            _records.Clear();
            
            _logger.LogInformation(
                "Started telemetry collection for job {JobId}, correlation {CorrelationId}",
                jobId, correlationId);
        }
    }
    
    /// <summary>
    /// Record a telemetry event
    /// Automatically masks sensitive data
    /// </summary>
    public void Record(RunTelemetryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        
        lock (_lock)
        {
            if (_jobId == null)
            {
                _logger.LogWarning("Attempted to record telemetry before starting collection");
                return;
            }
            
            var maskedRecord = MaskSensitiveData(record);
            _records.Add(maskedRecord);
            
            _logger.LogDebug(
                "Recorded telemetry: Job={JobId}, Stage={Stage}, Status={Status}, Latency={Latency}ms, Cost={Cost}",
                maskedRecord.JobId, maskedRecord.Stage, maskedRecord.ResultStatus, 
                maskedRecord.LatencyMs, maskedRecord.CostEstimate);
        }
    }
    
    /// <summary>
    /// End collection and persist telemetry to disk
    /// </summary>
    public string? EndCollection()
    {
        lock (_lock)
        {
            if (_jobId == null || _correlationId == null || _collectionStartedAt == null)
            {
                _logger.LogWarning("Attempted to end collection that was never started");
                return null;
            }
            
            try
            {
                var collection = new RunTelemetryCollection
                {
                    JobId = _jobId,
                    CorrelationId = _correlationId,
                    CollectionStartedAt = _collectionStartedAt.Value,
                    CollectionEndedAt = DateTime.UtcNow,
                    Records = _records.ToList(),
                    Summary = GenerateSummary()
                };
                
                var filePath = PersistTelemetry(collection);
                
                _logger.LogInformation(
                    "Telemetry collection ended for job {JobId}. {RecordCount} records saved to {FilePath}",
                    _jobId, _records.Count, filePath);
                
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist telemetry for job {JobId}", _jobId);
                return null;
            }
        }
    }
    
    /// <summary>
    /// Get current records (for in-flight monitoring)
    /// </summary>
    public IReadOnlyList<RunTelemetryRecord> GetRecords()
    {
        lock (_lock)
        {
            return _records.ToList();
        }
    }
    
    /// <summary>
    /// Load telemetry from disk for a given job
    /// </summary>
    public RunTelemetryCollection? LoadTelemetry(string jobId)
    {
        try
        {
            var filePath = GetTelemetryFilePath(jobId);
            
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Telemetry file not found for job {JobId}: {FilePath}", jobId, filePath);
                return null;
            }
            
            var json = File.ReadAllText(filePath);
            var collection = JsonSerializer.Deserialize<RunTelemetryCollection>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            _logger.LogDebug("Loaded telemetry for job {JobId} with {RecordCount} records", 
                jobId, collection?.Records?.Count ?? 0);
            
            return collection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load telemetry for job {JobId}", jobId);
            return null;
        }
    }
    
    private string PersistTelemetry(RunTelemetryCollection collection)
    {
        var jobDir = Path.Combine(_artifactsBasePath, collection.JobId);
        Directory.CreateDirectory(jobDir);
        
        var filePath = GetTelemetryFilePath(collection.JobId);
        
        var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        
        File.WriteAllText(filePath, json);
        
        return filePath;
    }
    
    private string GetTelemetryFilePath(string jobId)
    {
        return Path.Combine(_artifactsBasePath, jobId, "telemetry.json");
    }
    
    private RunTelemetrySummary GenerateSummary()
    {
        var summary = new RunTelemetrySummary
        {
            TotalOperations = _records.Count,
            SuccessfulOperations = _records.Count(r => r.ResultStatus == ResultStatus.Ok),
            FailedOperations = _records.Count(r => r.ResultStatus == ResultStatus.Error),
            TotalCost = _records.Sum(r => r.CostEstimate ?? 0),
            TotalLatencyMs = _records.Sum(r => r.LatencyMs),
            TotalTokensIn = _records.Sum(r => r.TokensIn ?? 0),
            TotalTokensOut = _records.Sum(r => r.TokensOut ?? 0),
            CacheHits = _records.Count(r => r.CacheHit == true),
            TotalRetries = _records.Sum(r => r.Retries),
            CostByStage = _records
                .GroupBy(r => r.Stage.ToString().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Sum(r => r.CostEstimate ?? 0)),
            OperationsByProvider = _records
                .Where(r => r.Provider != null)
                .GroupBy(r => r.Provider!)
                .ToDictionary(g => g.Key, g => g.Count())
        };
        
        return summary;
    }
    
    private RunTelemetryRecord MaskSensitiveData(RunTelemetryRecord record)
    {
        var message = record.Message;
        var metadata = record.Metadata;
        
        if (message != null)
        {
            message = MaskApiKeys(message);
            message = MaskTokens(message);
        }
        
        if (metadata != null)
        {
            metadata = new Dictionary<string, object>(metadata);
            
            foreach (var key in metadata.Keys.ToList())
            {
                if (key.Contains("key", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("password", StringComparison.OrdinalIgnoreCase))
                {
                    metadata[key] = "[REDACTED]";
                }
                else if (metadata[key] is string strValue)
                {
                    metadata[key] = MaskApiKeys(strValue);
                }
            }
        }
        
        return record with 
        { 
            Message = message,
            Metadata = metadata
        };
    }
    
    private static string MaskApiKeys(string text)
    {
        var patterns = new[]
        {
            @"sk-[a-zA-Z0-9]{32,}",
            @"Bearer\s+[a-zA-Z0-9\-_\.]+",
            @"[a-f0-9]{32,64}"
        };
        
        foreach (var pattern in patterns)
        {
            text = Regex.Replace(text, pattern, "[REDACTED]", RegexOptions.IgnoreCase);
        }
        
        return text;
    }
    
    private static string MaskTokens(string text)
    {
        var patterns = new[]
        {
            @"api[_-]?key['""]?\s*[:=]\s*['""]?[a-zA-Z0-9\-_\.]+",
            @"token['""]?\s*[:=]\s*['""]?[a-zA-Z0-9\-_\.]+",
            @"secret['""]?\s*[:=]\s*['""]?[a-zA-Z0-9\-_\.]+"
        };
        
        foreach (var pattern in patterns)
        {
            text = Regex.Replace(text, pattern, m => 
            {
                var parts = m.Value.Split(new[] { ':', '=' }, 2);
                return parts.Length == 2 ? $"{parts[0]}:[REDACTED]" : "[REDACTED]";
            }, RegexOptions.IgnoreCase);
        }
        
        return text;
    }
}
