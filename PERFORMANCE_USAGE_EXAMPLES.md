# Performance Optimization - Usage Examples

This document provides practical examples of how to use the new performance optimization features.

## Table of Contents
- [Pagination](#pagination)
- [Caching](#caching)
- [Cache Monitoring](#cache-monitoring)
- [ETags](#etags)
- [Query Performance](#query-performance)

---

## Pagination

### Basic Pagination

```csharp
using Aura.Core.Extensions;
using Aura.Core.Models.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    private readonly AuraDbContext _dbContext;

    public ExampleController(AuraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get paginated list of media items
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<MediaEntity>>> GetMediaPaginated(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc")
    {
        var pagination = new PaginationParams
        {
            PageNumber = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        // Apply pagination with sorting
        var query = _dbContext.MediaItems
            .Include(m => m.Tags)
            .Include(m => m.Collection)
            .AsNoTracking();

        var result = await query.ToPagedResultAsync(pagination);
        
        return Ok(result);
    }
}
```

**Response Example**:
```json
{
  "items": [
    {
      "id": "abc-123",
      "name": "Image 1",
      "type": "Image"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8,
  "hasPreviousPage": false,
  "hasNextPage": true,
  "firstItemIndex": 1,
  "lastItemIndex": 20
}
```

### Pagination with Filtering

```csharp
[HttpGet("search")]
public async Task<ActionResult<PagedResult<MediaEntity>>> SearchMedia(
    [FromQuery] string? query,
    [FromQuery] string? type,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    var mediaQuery = _dbContext.MediaItems.AsQueryable();

    // Apply filters
    if (!string.IsNullOrEmpty(query))
    {
        mediaQuery = mediaQuery.Where(m => 
            m.Name.Contains(query) || m.Description.Contains(query));
    }

    if (!string.IsNullOrEmpty(type))
    {
        mediaQuery = mediaQuery.Where(m => m.Type == type);
    }

    // Apply pagination
    var result = await mediaQuery
        .AsNoTracking()
        .ToPagedResultAsync(page, pageSize, "CreatedAt", descending: true);

    return Ok(result);
}
```

### Frontend Usage (TypeScript/React)

```typescript
import { useState, useEffect } from 'react';

interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

function MediaList() {
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PagedResult<MediaItem> | null>(null);

  useEffect(() => {
    fetch(`/api/media?page=${page}&pageSize=20&sortBy=CreatedAt&sortDirection=desc`)
      .then(res => res.json())
      .then(setData);
  }, [page]);

  return (
    <div>
      {data?.items.map(item => (
        <MediaCard key={item.id} item={item} />
      ))}
      
      <Pagination
        currentPage={data?.pageNumber ?? 1}
        totalPages={data?.totalPages ?? 1}
        onPageChange={setPage}
      />
    </div>
  );
}
```

---

## Caching

### Using Distributed Cache Service

```csharp
using Aura.Core.Services.Caching;

public class TemplateService
{
    private readonly IDistributedCacheService _cacheService;
    private readonly AuraDbContext _dbContext;

    public TemplateService(
        IDistributedCacheService cacheService,
        AuraDbContext dbContext)
    {
        _cacheService = cacheService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get template with caching
    /// </summary>
    public async Task<TemplateEntity?> GetTemplateAsync(string id, CancellationToken ct)
    {
        var cacheKey = $"template:{id}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async cancellationToken =>
            {
                var template = await _dbContext.Templates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

                return template;
            },
            TimeSpan.FromMinutes(30),
            ct
        );
    }

    /// <summary>
    /// Invalidate cache when template is updated
    /// </summary>
    public async Task UpdateTemplateAsync(string id, TemplateEntity template)
    {
        _dbContext.Templates.Update(template);
        await _dbContext.SaveChangesAsync();

        // Invalidate cache
        await _cacheService.RemoveAsync($"template:{id}");
    }
}
```

### Manual Cache Operations

```csharp
public class ExampleService
{
    private readonly IDistributedCacheService _cacheService;

    // Set cache value
    public async Task SetCacheAsync()
    {
        var data = new MyData { Id = 1, Name = "Test" };
        await _cacheService.SetAsync(
            "my-key",
            data,
            TimeSpan.FromMinutes(10)
        );
    }

    // Get cache value
    public async Task<MyData?> GetCacheAsync()
    {
        return await _cacheService.GetAsync<MyData>("my-key");
    }

    // Check if key exists
    public async Task<bool> CacheExistsAsync()
    {
        return await _cacheService.ExistsAsync("my-key");
    }

    // Remove from cache
    public async Task RemoveCacheAsync()
    {
        await _cacheService.RemoveAsync("my-key");
    }
}
```

### Cache Key Strategies

```csharp
public class CacheKeyHelper
{
    // User-specific cache
    public static string UserKey(string userId, string resource)
        => $"user:{userId}:{resource}";

    // Resource cache with version
    public static string ResourceKey(string type, string id, int version)
        => $"{type}:{id}:v{version}";

    // Query cache with parameters
    public static string QueryKey(string queryName, params object[] parameters)
        => $"query:{queryName}:{string.Join(":", parameters)}";

    // Time-based cache
    public static string TimeBasedKey(string resource, DateTime dateTime)
        => $"{resource}:{dateTime:yyyyMMdd-HHmm}";
}

// Usage
var cacheKey = CacheKeyHelper.UserKey("user-123", "preferences");
var data = await _cacheService.GetOrCreateAsync(
    cacheKey,
    async ct => await LoadUserPreferences("user-123", ct),
    TimeSpan.FromMinutes(15)
);
```

---

## Cache Monitoring

### Check Cache Statistics

```bash
# Get current cache stats
curl http://localhost:5005/api/cache/monitoring/stats

# Response
{
  "distributed": {
    "hits": 8542,
    "misses": 1458,
    "errors": 12,
    "hitRate": 85.42,
    "backendType": "Hybrid (Redis + Memory)",
    "totalRequests": 10000
  },
  "memory": {
    "currentEntryCount": 256,
    "currentEstimatedSize": 12582912,
    "totalHits": 1247,
    "totalMisses": 189,
    "hitRate": 86.84
  },
  "health": {
    "status": "healthy",
    "targetHitRate": 0.80,
    "currentHitRate": 0.854,
    "recommendation": "Cache performance is optimal"
  }
}
```

### Get Detailed Metrics

```bash
# Get performance metrics
curl http://localhost:5005/api/cache/monitoring/metrics

# Response
{
  "timestamp": "2025-11-10T15:30:00Z",
  "cache": {
    "hits": 8542,
    "misses": 1458,
    "errors": 12,
    "totalRequests": 10000,
    "hitRate": 85.42,
    "missRate": 14.58,
    "errorRate": 0.12
  },
  "performance": {
    "status": "optimal",
    "backendType": "Hybrid (Redis + Memory)",
    "recommendations": [
      "Cache performance is optimal. No action needed."
    ]
  }
}
```

### Clear Cache (Admin Operation)

```bash
# Clear all cache entries
curl -X POST http://localhost:5005/api/cache/monitoring/clear

# Response
{
  "message": "Cache cleared successfully",
  "timestamp": "2025-11-10T15:30:00Z"
}
```

### Monitoring in Code

```csharp
public class CacheHealthService
{
    private readonly IDistributedCacheService _cacheService;
    private readonly ILogger<CacheHealthService> _logger;

    public async Task CheckCacheHealthAsync()
    {
        var stats = _cacheService.GetStatistics();

        if (stats.HitRate < 0.70)
        {
            _logger.LogWarning(
                "Cache hit rate is low: {HitRate:P2}. " +
                "Hits: {Hits}, Misses: {Misses}",
                stats.HitRate, stats.Hits, stats.Misses
            );
        }

        if (stats.Errors > 100)
        {
            _logger.LogError(
                "High cache error count: {Errors}",
                stats.Errors
            );
        }
    }
}
```

---

## ETags

ETags are automatically handled by the `ETagMiddleware`. No code changes needed!

### How It Works

**First Request**:
```http
GET /api/templates/popular HTTP/1.1
Host: localhost:5005

HTTP/1.1 200 OK
ETag: "abc123xyz"
Cache-Control: private, max-age=3600
Content-Type: application/json

[{ "id": 1, "name": "Template 1" }]
```

**Subsequent Request** (content unchanged):
```http
GET /api/templates/popular HTTP/1.1
Host: localhost:5005
If-None-Match: "abc123xyz"

HTTP/1.1 304 Not Modified
ETag: "abc123xyz"
Cache-Control: private, max-age=3600
```

**Client-Side Usage** (JavaScript):
```javascript
// Browser automatically handles ETags
const response = await fetch('/api/templates/popular', {
  headers: {
    'If-None-Match': previousETag
  }
});

if (response.status === 304) {
  // Use cached data
  return cachedData;
} else {
  // Update cache
  const data = await response.json();
  const newETag = response.headers.get('ETag');
  return data;
}
```

### Custom Cache Control

```csharp
[HttpGet]
[ResponseCache(Duration = 3600)] // 1 hour
public async Task<ActionResult> GetStaticData()
{
    var data = await _service.GetStaticDataAsync();
    return Ok(data);
}

[HttpGet]
[ResponseCache(NoStore = true)] // No caching
public async Task<ActionResult> GetRealtimeData()
{
    var data = await _service.GetRealtimeDataAsync();
    return Ok(data);
}
```

---

## Query Performance

Query performance is automatically monitored by the `QueryPerformanceMiddleware`.

### Response Time Headers

Every API response includes timing information:

```http
HTTP/1.1 200 OK
X-Response-Time-Ms: 245
X-Correlation-Id: 8b3f7e4d-9a2c-4f1e-8b6a-1c4d7e9f3a2b
```

### Monitoring Slow Queries

Slow requests are automatically logged:

```log
[2025-11-10 15:30:00.000 +00:00] [WRN] [abc-123] [trace-id] [span-id] 
Very slow request detected: GET /api/projects/export took 5234ms (threshold: 5000ms). 
StatusCode: 200, CorrelationId: abc-123
```

### Query Optimization Tips

```csharp
// ❌ BAD: N+1 query problem
public async Task<List<ProjectDto>> GetProjectsAsync()
{
    var projects = await _dbContext.Projects.ToListAsync();
    
    foreach (var project in projects)
    {
        // This executes a separate query for EACH project!
        project.Scenes = await _dbContext.Scenes
            .Where(s => s.ProjectId == project.Id)
            .ToListAsync();
    }
    
    return projects;
}

// ✅ GOOD: Eager loading with Include
public async Task<List<ProjectDto>> GetProjectsAsync()
{
    var projects = await _dbContext.Projects
        .Include(p => p.Scenes)
        .Include(p => p.Assets)
        .AsNoTracking() // Don't track if read-only
        .ToListAsync();
    
    return projects;
}

// ✅ BETTER: With pagination
public async Task<PagedResult<ProjectDto>> GetProjectsAsync(int page, int pageSize)
{
    var query = _dbContext.Projects
        .Include(p => p.Scenes)
        .Include(p => p.Assets)
        .AsNoTracking();
    
    return await query.ToPagedResultAsync(page, pageSize, "UpdatedAt");
}
```

### Performance Profiling in Tests

```csharp
[Fact]
public async Task Query_Should_Complete_Within_Threshold()
{
    // Arrange
    var stopwatch = Stopwatch.StartNew();
    
    // Act
    var result = await _dbContext.MediaItems
        .Include(m => m.Tags)
        .AsNoTracking()
        .ToListAsync();
    
    stopwatch.Stop();
    
    // Assert
    Assert.True(
        stopwatch.ElapsedMilliseconds < 100,
        $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms"
    );
}
```

---

## Configuration Best Practices

### Development Environment

```json
{
  "Database": {
    "Performance": {
      "EnablePoolingStats": true,
      "SqliteCacheSizeKB": 32000
    }
  },
  "Caching": {
    "Enabled": true,
    "UseRedis": false,
    "EnableCacheWarming": true
  }
}
```

### Production Environment

```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "ConnectionString": "Host=prod-db;Database=aura;...",
    "Performance": {
      "MaxPoolSize": 100,
      "MinPoolSize": 20,
      "EnablePoolingStats": true
    }
  },
  "Caching": {
    "Enabled": true,
    "UseRedis": true,
    "RedisConnection": "redis-cluster:6379,password=...,ssl=true",
    "EnableCacheWarming": true,
    "Strategies": {
      "QueryResultsSeconds": 120,
      "TemplatesSeconds": 3600
    }
  }
}
```

---

## Performance Checklist

### Before Deployment
- [ ] Enable Redis caching in production
- [ ] Configure appropriate cache TTLs
- [ ] Set up database connection pooling
- [ ] Review and optimize slow queries
- [ ] Add pagination to large list endpoints
- [ ] Test with realistic data volumes

### After Deployment
- [ ] Monitor cache hit rates
- [ ] Check response time metrics
- [ ] Review slow request logs
- [ ] Validate memory usage
- [ ] Test under load

### Ongoing Maintenance
- [ ] Weekly review of slow queries
- [ ] Monthly cache statistics review
- [ ] Adjust cache TTLs based on usage
- [ ] Add indexes for new query patterns
- [ ] Update pagination as data grows

---

## Troubleshooting

### Low Cache Hit Rate

**Symptoms**: Cache hit rate < 70%

**Solutions**:
1. Increase cache TTLs
2. Implement cache warming for frequently accessed data
3. Review cache key generation (ensure consistency)
4. Check Redis connection stability

### Slow Query Performance

**Symptoms**: Queries taking > 100ms

**Solutions**:
1. Add missing database indexes
2. Use `.AsNoTracking()` for read-only queries
3. Implement eager loading with `.Include()`
4. Add pagination to reduce result set size
5. Enable query splitting for complex queries

### High Memory Usage

**Symptoms**: Memory continuously growing

**Solutions**:
1. Reduce in-memory cache size
2. Lower cache TTLs
3. Ensure proper disposal of DbContext
4. Check for memory leaks in long-running operations

---

## Support

For issues or questions:
- Check the main implementation doc: `PERFORMANCE_OPTIMIZATION_IMPLEMENTATION.md`
- Review cache statistics: `GET /api/cache/monitoring/stats`
- Check application logs for slow request warnings
- Contact the development team
