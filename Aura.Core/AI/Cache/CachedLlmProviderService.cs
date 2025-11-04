using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.Core.AI.Cache;

/// <summary>
/// Service wrapper for cache-first LLM operations
/// </summary>
public class CachedLlmProviderService
{
    private readonly ILogger<CachedLlmProviderService> _logger;
    private readonly ILlmCache _cache;
    private readonly LlmCacheOptions _options;
    
    public CachedLlmProviderService(
        ILogger<CachedLlmProviderService> logger,
        ILlmCache cache,
        IOptions<LlmCacheOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }
    
    /// <summary>
    /// Executes an LLM operation with cache-first strategy
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="metadata">Cache metadata</param>
    /// <param name="operation">Operation to execute on cache miss</param>
    /// <param name="serializer">Function to serialize response to string</param>
    /// <param name="deserializer">Function to deserialize string to response</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cached result with metadata</returns>
    public async Task<CachedResult<T>> ExecuteWithCacheAsync<T>(
        string cacheKey,
        CacheMetadata metadata,
        Func<CancellationToken, Task<T>> operation,
        Func<T, string> serializer,
        Func<string, T> deserializer,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            var result = await operation(ct);
            return new CachedResult<T> { Result = result, FromCache = false };
        }
        
        var cached = await _cache.GetAsync(cacheKey, ct);
        
        if (cached != null)
        {
            try
            {
                var deserialized = deserializer(cached.Response);
                
                return new CachedResult<T>
                {
                    Result = deserialized,
                    FromCache = true,
                    CacheAge = DateTime.UtcNow - cached.CachedAt,
                    AccessCount = cached.AccessCount,
                    CacheKey = cacheKey
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached response for key {KeyHash}, executing operation", GetKeyHash(cacheKey));
            }
        }
        
        var freshResult = await operation(ct);
        var serialized = serializer(freshResult);
        
        await _cache.SetAsync(cacheKey, serialized, metadata, ct);
        
        return new CachedResult<T>
        {
            Result = freshResult,
            FromCache = false,
            CacheKey = cacheKey
        };
    }
    
    /// <summary>
    /// Checks if an operation is cacheable based on parameters
    /// </summary>
    /// <param name="operationType">Type of operation</param>
    /// <param name="temperature">Temperature parameter</param>
    /// <returns>True if operation is cacheable</returns>
    public bool IsCacheable(string operationType, double temperature)
    {
        if (!_options.Enabled)
        {
            return false;
        }
        
        return LlmCacheKeyGenerator.IsCacheable(operationType) && LlmCacheKeyGenerator.IsTemperatureSuitable(temperature);
    }
    
    /// <summary>
    /// Generates cache key for draft script operation
    /// </summary>
    public string GenerateDraftScriptCacheKey(
        string providerName,
        string modelName,
        string brief,
        object spec,
        double temperature)
    {
        return LlmCacheKeyGenerator.GenerateKey(
            providerName,
            modelName,
            "DraftScript",
            null,
            brief,
            temperature,
            2000);
    }
    
    /// <summary>
    /// Generates cache key for plan scaffold operation
    /// </summary>
    public string GeneratePlanScaffoldCacheKey(
        string providerName,
        string modelName,
        string brief,
        double temperature)
    {
        return LlmCacheKeyGenerator.GenerateKey(
            providerName,
            modelName,
            "PlanScaffold",
            null,
            brief,
            temperature,
            1000);
    }
    
    /// <summary>
    /// Generates cache key for outline transform operation
    /// </summary>
    public string GenerateOutlineTransformCacheKey(
        string providerName,
        string modelName,
        string outline,
        double temperature)
    {
        return LlmCacheKeyGenerator.GenerateKey(
            providerName,
            modelName,
            "OutlineTransform",
            null,
            outline,
            temperature,
            1000);
    }
    
    private static string GetKeyHash(string key)
    {
        return key.Length > 16 ? key.Substring(0, 16) + "..." : key;
    }
}

/// <summary>
/// Result from cached LLM operation
/// </summary>
/// <typeparam name="T">Result type</typeparam>
public class CachedResult<T>
{
    public required T Result { get; init; }
    public bool FromCache { get; init; }
    public TimeSpan? CacheAge { get; init; }
    public int AccessCount { get; init; }
    public string? CacheKey { get; init; }
}
