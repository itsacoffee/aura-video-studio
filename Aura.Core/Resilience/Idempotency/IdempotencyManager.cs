using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Resilience.Idempotency;

/// <summary>
/// Manages idempotency keys and cached responses to prevent duplicate operations
/// </summary>
public class IdempotencyManager
{
    private readonly ILogger<IdempotencyManager> _logger;
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _records = new();
    private readonly TimeSpan _defaultTtl = TimeSpan.FromHours(24);

    public IdempotencyManager(ILogger<IdempotencyManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Checks if an operation with the given idempotency key has already been executed
    /// </summary>
    public bool TryGetResult<TResult>(string idempotencyKey, out TResult? result)
    {
        if (_records.TryGetValue(idempotencyKey, out var record))
        {
            if (record.IsExpired)
            {
                _records.TryRemove(idempotencyKey, out _);
                result = default;
                return false;
            }

            if (record.Result is TResult typedResult)
            {
                result = typedResult;
                
                _logger.LogInformation(
                    "Idempotent operation detected: returning cached result for key {IdempotencyKey}",
                    idempotencyKey);
                
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Stores the result of an operation with an idempotency key
    /// </summary>
    public void StoreResult<TResult>(string idempotencyKey, TResult result, TimeSpan? ttl = null)
    {
        var record = new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            Result = result!,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow + (ttl ?? _defaultTtl)
        };

        _records.AddOrUpdate(idempotencyKey, record, (_, _) => record);

        _logger.LogDebug(
            "Stored idempotent result for key {IdempotencyKey} (expires at {ExpiresAt})",
            idempotencyKey,
            record.ExpiresAt);
    }

    /// <summary>
    /// Executes an operation with idempotency protection
    /// </summary>
    public async Task<TResult> ExecuteIdempotentAsync<TResult>(
        string idempotencyKey,
        Func<Task<TResult>> operation,
        TimeSpan? ttl = null)
    {
        // Check if we already have a result
        if (TryGetResult<TResult>(idempotencyKey, out var cachedResult))
        {
            return cachedResult!;
        }

        // Execute the operation
        var result = await operation();

        // Store the result
        StoreResult(idempotencyKey, result, ttl);

        return result;
    }

    /// <summary>
    /// Removes expired records
    /// </summary>
    public int CleanupExpired()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _records
            .Where(kvp => kvp.Value.ExpiresAt < now)
            .Select(kvp => kvp.Key)
            .ToList();

        var removed = 0;
        foreach (var key in expiredKeys)
        {
            if (_records.TryRemove(key, out _))
            {
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} expired idempotency records",
                removed);
        }

        return removed;
    }

    /// <summary>
    /// Gets the total number of stored records
    /// </summary>
    public int GetRecordCount() => _records.Count;

    /// <summary>
    /// Clears all records
    /// </summary>
    public void Clear()
    {
        _records.Clear();
        _logger.LogInformation("Cleared all idempotency records");
    }
}

/// <summary>
/// Record of an idempotent operation
/// </summary>
public class IdempotencyRecord
{
    public required string IdempotencyKey { get; init; }
    public required object Result { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;
}
