# LLM Cache and Prewarm Guide

## Overview

The LLM caching system provides deterministic response caching for LLM operations to dramatically reduce latency for repeatable steps like plan scaffolds and outline transforms. This guide covers configuration, usage, and best practices for leveraging the cache to improve user experience.

## Architecture

### Components

#### Core Layer (Aura.Core/AI/Cache/)

- **ILlmCache**: Interface defining cache operations (Get, Set, Remove, Clear, Statistics)
- **MemoryLlmCache**: In-memory implementation with LRU eviction and TTL support
- **LlmCacheKeyGenerator**: Deterministic key generation from request parameters
- **CachedLlmProviderService**: Service wrapper for cache-first LLM operations
- **LlmPrewarmService**: Preloads common prompts at application startup

#### API Layer (Aura.Api/)

- **CacheController**: REST endpoints for cache management
- **LlmCacheMaintenanceService**: Background service for periodic cache maintenance

#### Frontend (Aura.Web/)

- **cacheApi.ts**: API client for cache operations
- **cache.ts**: TypeScript type definitions

### Cache Key Generation

Cache keys are generated deterministically from:

1. **Provider name** (e.g., "OpenAI", "Anthropic")
2. **Model name** (e.g., "gpt-4", "claude-3-opus")
3. **Operation type** (e.g., "PlanScaffold", "OutlineTransform")
4. **System prompt** (if provided)
5. **User prompt** (normalized: trimmed, lowercased)
6. **Temperature** (formatted to 2 decimal places)
7. **Max tokens**
8. **Additional parameters** (sorted alphabetically for consistency)

The key is a SHA256 hash of the concatenated parameters, ensuring:
- Same inputs always produce same key
- Different inputs produce different keys
- Case-insensitive and whitespace-normalized prompts

## Configuration

### appsettings.json

```json
{
  "LlmCache": {
    "Enabled": true,
    "MaxEntries": 1000,
    "DefaultTtlSeconds": 3600,
    "UseDiskStorage": false,
    "DiskStoragePath": "./cache/llm",
    "MaxDiskSizeMB": 100
  },
  "LlmPrewarm": {
    "Enabled": true,
    "MaxConcurrentPrewarms": 3,
    "PrewarmPrompts": [
      {
        "ProviderName": "OpenAI",
        "ModelName": "gpt-4",
        "OperationType": "PlanScaffold",
        "UserPrompt": "technology tutorial",
        "Temperature": 0.2,
        "MaxTokens": 1000,
        "TtlSeconds": 7200
      }
    ]
  }
}
```

### Configuration Options

#### LlmCache

- **Enabled** (bool): Enable/disable caching globally. Default: `true`
- **MaxEntries** (int): Maximum number of entries in memory. Default: `1000`
- **DefaultTtlSeconds** (int): Default time-to-live for entries. Default: `3600` (1 hour)
- **UseDiskStorage** (bool): Enable optional disk-based storage. Default: `false`
- **DiskStoragePath** (string): Directory for disk cache. Default: `"./cache/llm"`
- **MaxDiskSizeMB** (int): Maximum disk cache size in MB. Default: `100`

#### LlmPrewarm

- **Enabled** (bool): Enable/disable prewarming. Default: `true`
- **MaxConcurrentPrewarms** (int): Max concurrent prewarm operations. Default: `3`
- **PrewarmPrompts** (array): List of prompts to prewarm on startup

## Cacheable Operations

### Operations Eligible for Caching

The following operation types are considered deterministic and cacheable:

- **PlanScaffold**: Initial plan structure generation
- **OutlineTransform**: Converting outlines to structured content
- **SceneAnalysis**: Analyzing scene importance and pacing
- **ContentComplexity**: Analyzing content difficulty
- **SceneCoherence**: Evaluating scene transitions
- **NarrativeArc**: Validating story structure
- **VisualPrompt**: Generating image generation prompts
- **TransitionText**: Creating scene transition text

### Temperature Threshold

Caching is only enabled when `temperature <= 0.3` to ensure deterministic responses. Creative operations with higher temperatures are never cached.

### Operations Not Cached

- Creative content generation (temperature > 0.3)
- Long-form narrative generation
- User-specific creative requests
- Real-time conversational responses

## API Endpoints

### GET /api/cache/stats

Returns cache statistics.

**Response:**
```json
{
  "totalEntries": 152,
  "totalHits": 487,
  "totalMisses": 203,
  "hitRate": 0.71,
  "totalSizeBytes": 1048576,
  "totalEvictions": 23,
  "totalExpirations": 15
}
```

### POST /api/cache/clear

Clears all cache entries.

**Response:**
```json
{
  "success": true,
  "message": "Cache cleared successfully. Removed 152 entries.",
  "entriesRemoved": 152
}
```

### POST /api/cache/evict-expired

Removes expired entries based on TTL.

**Response:**
```json
{
  "success": true,
  "message": "Evicted 15 expired entries.",
  "entriesRemoved": 15,
  "entriesRemaining": 137
}
```

## Usage Patterns

### Automatic Caching

The cache is consulted automatically for eligible operations. No code changes required for consumers.

```csharp
// This operation will be cached if eligible
var script = await llmProvider.DraftScriptAsync(brief, spec, ct);
```

### Explicit Cache Usage

For advanced scenarios, use `CachedLlmProviderService`:

```csharp
var cacheKey = cachedService.GenerateDraftScriptCacheKey(
    "OpenAI",
    "gpt-4", 
    brief, 
    spec, 
    temperature: 0.2);

var result = await cachedService.ExecuteWithCacheAsync(
    cacheKey,
    metadata,
    ct => llmProvider.DraftScriptAsync(brief, spec, ct),
    response => JsonSerializer.Serialize(response),
    json => JsonSerializer.Deserialize<string>(json)!,
    ct);

if (result.FromCache)
{
    logger.LogInformation("Using cached response");
}
```

## Cache Maintenance

### Automatic Maintenance

The `LlmCacheMaintenanceService` runs every 5 minutes to:

1. Evict expired entries based on TTL
2. Log cache statistics
3. Monitor cache health

### Manual Maintenance

Clear cache via API:

```bash
# Get statistics
curl http://localhost:5005/api/cache/stats

# Clear all entries
curl -X POST http://localhost:5005/api/cache/clear

# Evict expired entries
curl -X POST http://localhost:5005/api/cache/evict-expired
```

## Performance Impact

### Expected Latency Improvements

- **Cache Hit**: < 5ms (vs 2-10 seconds for LLM call)
- **First Call**: No overhead (cache miss, normal LLM latency)
- **Subsequent Calls**: 99.5% latency reduction

### Memory Usage

- **Per Entry**: ~2-10 KB (depends on response size)
- **1000 Entries**: ~2-10 MB
- **Configurable**: Adjust `MaxEntries` based on available memory

### Hit Rate Targets

- **Good**: 40-60% hit rate for typical usage
- **Excellent**: 70%+ hit rate with prewarming
- **Monitor**: Use `/api/cache/stats` to track

## Prewarming Strategy

### When to Prewarm

- **Application Startup**: Load common prompts on initialization
- **Project Open**: Load project-specific templates
- **Feature Entry**: Load feature-specific prompts when user navigates

### Prewarm Configuration

Add prompts to `appsettings.json`:

```json
{
  "LlmPrewarm": {
    "PrewarmPrompts": [
      {
        "ProviderName": "OpenAI",
        "ModelName": "gpt-4",
        "OperationType": "PlanScaffold",
        "UserPrompt": "technology tutorial",
        "Temperature": 0.2,
        "MaxTokens": 1000,
        "TtlSeconds": 7200
      },
      {
        "ProviderName": "OpenAI",
        "ModelName": "gpt-4",
        "OperationType": "OutlineTransform",
        "UserPrompt": "product demonstration",
        "Temperature": 0.2,
        "MaxTokens": 1000,
        "TtlSeconds": 7200
      }
    ]
  }
}
```

### Prewarm Best Practices

1. **Focus on Common Patterns**: Prewarm the most frequently used prompts
2. **Use Longer TTL**: Set `TtlSeconds` higher (e.g., 7200 = 2 hours) for prewarmed entries
3. **Limit Concurrency**: Keep `MaxConcurrentPrewarms` low (3-5) to avoid overwhelming providers
4. **Monitor Effectiveness**: Check hit rates to validate prewarm value

## Cache Invalidation

### Automatic Invalidation

Cache entries are automatically invalidated when:

1. **TTL Expires**: Entry exceeds configured `TtlSeconds`
2. **LRU Eviction**: Cache reaches `MaxEntries` and least recently used entries are evicted
3. **Manual Clear**: User or admin clears cache via API

### Safe Invalidation

The cache uses conservative invalidation to prevent stale results:

- **Input Changes**: Any parameter change produces a new cache key
- **Provider Changes**: Switching providers invalidates previous results
- **Model Updates**: Model version changes invalidate cache
- **Temperature Changes**: Even small temperature differences invalidate cache

### Force Refresh

To bypass cache for a specific request, temporarily disable caching:

```csharp
// Disable cache temporarily
_options.Enabled = false;
var freshResult = await llmProvider.DraftScriptAsync(brief, spec, ct);
_options.Enabled = true;
```

Or clear specific entries via API.

## Monitoring and Observability

### Logs

Cache operations are logged with structured logging:

```
[INFO] Cache HIT for PlanScaffold (provider=OpenAI, model=gpt-4, accessCount=5)
[INFO] Cached response for PlanScaffold (provider=OpenAI, model=gpt-4, ttl=3600s)
[INFO] Evicted 12 expired entries from cache
```

### Metrics

Monitor via `/api/cache/stats`:

- **Hit Rate**: Target 40-70% for good cache effectiveness
- **Evictions**: High evictions may indicate `MaxEntries` too low
- **Expirations**: Track TTL effectiveness

### Telemetry

Cache performance is included in application telemetry:

- Cache hit latency
- Cache miss latency
- Serialization overhead
- Eviction patterns

## Troubleshooting

### Low Hit Rate

**Symptoms**: Hit rate < 20%

**Causes**:
- TTL too short
- High temperature (> 0.3) disables caching
- Prompts have high variability
- Not prewarming common patterns

**Solutions**:
1. Increase `DefaultTtlSeconds`
2. Lower temperature for deterministic operations
3. Add prewarming for common prompts
4. Normalize input variations

### High Memory Usage

**Symptoms**: Memory usage growing unbounded

**Causes**:
- `MaxEntries` too high
- Large responses being cached
- Memory leak (unlikely, uses weak references)

**Solutions**:
1. Reduce `MaxEntries`
2. Enable disk storage for overflow
3. Reduce TTL to expire entries faster
4. Clear cache periodically

### Stale Results

**Symptoms**: Cached results don't reflect latest data

**Causes**:
- TTL too long
- Cache not invalidated on relevant changes
- Provider updated but cache not cleared

**Solutions**:
1. Reduce TTL for frequently changing content
2. Clear cache after provider/model updates
3. Use force refresh for critical operations

## Best Practices

### Development

1. **Enable in Dev**: Keep caching enabled during development to catch issues early
2. **Clear on Schema Changes**: Clear cache when prompt formats change
3. **Test Cache Miss Path**: Ensure code works correctly on cache misses
4. **Monitor Hit Rates**: Track effectiveness of caching strategy

### Production

1. **Conservative TTL**: Start with lower TTL (1 hour) and increase if safe
2. **Monitor Memory**: Track cache size and evictions
3. **Graceful Degradation**: Ensure system works if cache fails
4. **Regular Maintenance**: Schedule periodic cache clears if needed

### Security

1. **No Sensitive Data**: Never cache responses containing PII or secrets
2. **Access Control**: Secure cache management endpoints
3. **Audit Logging**: Log all manual cache operations
4. **Key Isolation**: Cache keys include provider/model to prevent cross-contamination

## Future Enhancements

### Planned Features

- **Disk-based Storage**: Persistent cache across restarts (currently in-memory only)
- **Distributed Cache**: Redis/Memcached support for multi-instance deployments
- **Cache Warming API**: Endpoint to trigger prewarm on demand
- **Advanced Eviction**: Frequency-based eviction in addition to LRU
- **Cache Partitioning**: Separate caches per user/tenant

### Experimental Features

- **Semantic Similarity**: Cache hits for semantically similar prompts
- **Partial Matches**: Return partial results while fetching full response
- **Predictive Prewarming**: ML-based prediction of likely next prompts

## Conclusion

The LLM caching system provides significant latency improvements for deterministic operations while maintaining safety through conservative invalidation. Monitor hit rates and adjust configuration to optimize for your workload.

For questions or issues, see:
- Issue Tracker: [GitHub Issues](https://github.com/Saiyan9001/aura-video-studio/issues)
- LLM Documentation: `LLM_IMPLEMENTATION_GUIDE.md`
- Latency Management: `LLM_LATENCY_MANAGEMENT.md`
