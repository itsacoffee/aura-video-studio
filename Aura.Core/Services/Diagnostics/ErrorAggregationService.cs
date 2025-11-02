using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Aura.Core.Services.Diagnostics;

/// <summary>
/// Service for aggregating and tracking errors by signature
/// </summary>
public class ErrorAggregationService
{
    private readonly ILogger<ErrorAggregationService> _logger;
    private readonly ConcurrentDictionary<string, ErrorSignature> _errorSignatures = new();
    private readonly int _maxStoredSignatures = 1000;

    public ErrorAggregationService(ILogger<ErrorAggregationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Record an error occurrence
    /// </summary>
    public void RecordError(Exception exception, string? correlationId = null, Dictionary<string, object>? context = null)
    {
        var signature = GenerateSignature(exception);
        
        _errorSignatures.AddOrUpdate(
            signature,
            _ => new ErrorSignature
            {
                Signature = signature,
                ExceptionType = exception.GetType().Name,
                Message = exception.Message,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow,
                Count = 1,
                SampleStackTrace = exception.StackTrace,
                SampleCorrelationId = correlationId,
                SampleContext = context
            },
            (_, existing) =>
            {
                existing.LastSeen = DateTime.UtcNow;
                existing.Count++;
                return existing;
            });

        // Trim if too many signatures
        if (_errorSignatures.Count > _maxStoredSignatures)
        {
            var oldestSignatures = _errorSignatures
                .OrderBy(kvp => kvp.Value.LastSeen)
                .Take(_errorSignatures.Count - _maxStoredSignatures)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sig in oldestSignatures)
            {
                _errorSignatures.TryRemove(sig, out _);
            }
        }
    }

    /// <summary>
    /// Get aggregated errors filtered by time window
    /// </summary>
    public List<ErrorSignature> GetAggregatedErrors(TimeSpan? since = null, int? limit = null)
    {
        var cutoffTime = since.HasValue ? DateTime.UtcNow - since.Value : DateTime.MinValue;

        IEnumerable<ErrorSignature> errors = _errorSignatures.Values
            .Where(e => e.LastSeen >= cutoffTime)
            .OrderByDescending(e => e.Count)
            .ThenByDescending(e => e.LastSeen);

        if (limit.HasValue)
        {
            errors = errors.Take(limit.Value);
        }

        return errors.ToList();
    }

    /// <summary>
    /// Get error statistics
    /// </summary>
    public ErrorStatistics GetStatistics(TimeSpan? since = null)
    {
        var cutoffTime = since.HasValue ? DateTime.UtcNow - since.Value : DateTime.MinValue;
        var relevantErrors = _errorSignatures.Values.Where(e => e.LastSeen >= cutoffTime).ToList();

        return new ErrorStatistics
        {
            TotalUniqueErrors = relevantErrors.Count,
            TotalOccurrences = relevantErrors.Sum(e => e.Count),
            MostFrequentError = relevantErrors.OrderByDescending(e => e.Count).FirstOrDefault(),
            ErrorTypes = relevantErrors
                .GroupBy(e => e.ExceptionType)
                .OrderByDescending(g => g.Sum(e => e.Count))
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Count))
        };
    }

    /// <summary>
    /// Clear old errors beyond retention period
    /// </summary>
    public int ClearOldErrors(TimeSpan retentionPeriod)
    {
        var cutoffTime = DateTime.UtcNow - retentionPeriod;
        var keysToRemove = _errorSignatures
            .Where(kvp => kvp.Value.LastSeen < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        int removed = 0;
        foreach (var key in keysToRemove)
        {
            if (_errorSignatures.TryRemove(key, out _))
            {
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogInformation("Cleared {RemovedCount} old error signatures beyond retention period", removed);
        }

        return removed;
    }

    /// <summary>
    /// Generate unique signature for an error based on type and message
    /// </summary>
    private string GenerateSignature(Exception exception)
    {
        var signatureData = $"{exception.GetType().FullName}:{SanitizeMessage(exception.Message)}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
        return Convert.ToHexString(hash)[..16]; // First 16 chars of hash
    }

    /// <summary>
    /// Sanitize error message by removing variable parts (IDs, timestamps, etc.)
    /// </summary>
    private string SanitizeMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        // Replace GUIDs
        message = System.Text.RegularExpressions.Regex.Replace(message, 
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", 
            "[GUID]");

        // Replace numbers that might be IDs or counts
        message = System.Text.RegularExpressions.Regex.Replace(message, 
            @"\b\d{4,}\b", 
            "[NUM]");

        // Replace file paths
        message = System.Text.RegularExpressions.Regex.Replace(message, 
            @"[A-Za-z]:\\[^\s]+", 
            "[PATH]");

        return message;
    }
}

/// <summary>
/// Represents an aggregated error signature
/// </summary>
public class ErrorSignature
{
    public string Signature { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public long Count { get; set; }
    public string? SampleStackTrace { get; set; }
    public string? SampleCorrelationId { get; set; }
    public Dictionary<string, object>? SampleContext { get; set; }
}

/// <summary>
/// Statistics about errors
/// </summary>
public class ErrorStatistics
{
    public int TotalUniqueErrors { get; set; }
    public long TotalOccurrences { get; set; }
    public ErrorSignature? MostFrequentError { get; set; }
    public Dictionary<string, long> ErrorTypes { get; set; } = new();
}
