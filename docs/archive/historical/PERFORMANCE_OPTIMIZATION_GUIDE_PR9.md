# Performance Optimization Guide

## Overview

This guide documents the performance optimizations implemented in PR #9 to improve application load times, reduce memory usage, and enhance rendering efficiency.

## Key Improvements

### 1. Frontend Performance Optimizations

#### Code Splitting and Lazy Loading
- All route components are lazy-loaded using React.lazy()
- Suspense boundaries with loading spinners
- Route-level code splitting reduces initial bundle size

#### Custom Performance Hooks
Location: `src/hooks/usePerformanceOptimization.ts`

**Available Hooks:**
- `useDebounce<T>` - Delays value updates
- `useDebouncedCallback` - Debounces callback functions
- `useThrottledCallback` - Throttles callback execution
- `useLazyInit<T>` - One-time expensive initialization
- `useOptimizedMemo` - Memoization with performance tracking
- `useLazyImage` - Lazy load images with intersection observer
- `useIsInViewport` - Viewport visibility detection

**Example Usage:**
```typescript
import { useDebounce, useDebouncedCallback } from '@/hooks/usePerformanceOptimization';

// Debounce search input
const [searchTerm, setSearchTerm] = useState('');
const debouncedSearchTerm = useDebounce(searchTerm, 300);

// Debounce API calls
const handleSearch = useDebouncedCallback((query: string) => {
  searchApi(query);
}, 500);
```

#### Image Optimization
Location: `src/hooks/useImageOptimization.ts`

**Features:**
- LRU cache for loaded images (max 100 images)
- Lazy loading with intersection observer
- Progressive image loading (blur-up technique)
- Bulk image preloading

**Example Usage:**
```typescript
import { useLazyLoadImage, useProgressiveImage } from '@/hooks/useImageOptimization';

// Lazy load image
const { imgRef, isLoaded, error } = useLazyLoadImage(imageUrl);

// Progressive loading
const { currentSrc, isHighQualityLoaded } = useProgressiveImage(
  thumbnailUrl,
  fullResUrl
);
```

#### API Request Pooling
Location: `src/hooks/useApiRequestPool.ts`

Limits concurrent API requests to prevent overwhelming the backend (default: 6 concurrent requests).

**Example Usage:**
```typescript
import { useApiRequestPool } from '@/hooks/useApiRequestPool';

const { request, getStats } = useApiRequestPool(6);

// Make pooled request
const data = await request<MyType>({
  url: '/api/endpoint',
  method: 'GET'
});

// Check pool statistics
const { activeRequests, queuedRequests } = getStats();
```

#### React Component Optimization
Location: `src/components/Performance/OptimizedComponent.tsx`

**Available Utilities:**
- `withOptimizedRendering<P>` - HOC for React.memo with custom comparison
- `OptimizedListItem` - Optimized wrapper for list items
- `withSelectiveUpdates<P>` - HOC that only updates on specific props
- `shallowCompareIgnoringFunctions` - Comparison that ignores function props

**Example Usage:**
```typescript
import { withOptimizedRendering, OptimizedListItem } from '@/components/Performance/OptimizedComponent';

// Optimize expensive component
const MyExpensiveComponent = withOptimizedRendering(
  ({ data }) => {
    // Render expensive UI
  },
  (prev, next) => prev.data.id === next.data.id
);

// Use optimized list item
<OptimizedListItem key={item.id} id={item.id}>
  <ItemContent data={item} />
</OptimizedListItem>
```

### 2. Backend Performance Optimizations

#### Hardware Acceleration for FFmpeg
Location: `Aura.Core/Services/FFmpeg/HardwareAccelerationOptimizer.cs`

**Supported Hardware:**
- NVIDIA NVENC (CUDA)
- AMD AMF
- Intel QuickSync (QSV)
- Apple VideoToolbox
- VAAPI (Linux)

**Features:**
- Automatic hardware detection
- Optimal encoder selection
- Thread count optimization
- Preset selection based on capabilities

**Example Usage:**
```csharp
var optimizer = new HardwareAccelerationOptimizer(logger);

// Detect capabilities
var capabilities = await optimizer.DetectCapabilitiesAsync();

// Optimize FFmpeg command
var builder = new FFmpegCommandBuilder();
builder = await optimizer.OptimizeForHardwareAsync(builder, "h264");

// Get optimal settings
var threadCount = optimizer.GetOptimalThreadCount();
var preset = optimizer.GetOptimalPreset(capabilities);
```

#### Object Pooling
Location: `Aura.Core/Services/Performance/ObjectPool.cs`

Reduces GC pressure by reusing objects.

**Features:**
- Synchronous `ObjectPool<T>`
- Asynchronous `AsyncObjectPool<T>`
- Automatic return on dispose via `PooledObject<T>`
- Configurable max size

**Example Usage:**
```csharp
// Create pool
var pool = new ObjectPool<StringBuilder>(
    () => new StringBuilder(),
    sb => sb.Clear(),
    maxSize: 50
);

// Use with automatic return
using (var pooled = pool.Rent())
{
    pooled.Value.Append("Hello");
    // Automatically returned on dispose
}

// Or manual management
var obj = pool.Get();
try {
    // Use obj
} finally {
    pool.Return(obj);
}
```

#### Enhanced Memory Monitoring
Location: `Aura.Core/Services/Performance/EnhancedMemoryMonitor.cs`

**Features:**
- Real-time memory usage tracking
- Automatic GC optimization
- Memory leak detection
- Performance recommendations
- Configurable thresholds

**Example Usage:**
```csharp
var monitor = new EnhancedMemoryMonitor(
    logger,
    memoryLimitMb: 500,
    gcThresholdMb: 400,
    monitorIntervalSeconds: 30
);

// Check memory
var stats = monitor.GetMemoryStatistics();
bool isOk = monitor.IsMemoryUsageAcceptable();

// Force optimization
monitor.OptimizeMemoryIfNeeded();

// Detect leaks
bool hasLeak = monitor.DetectPotentialLeak();

// Get recommendations
var recommendations = monitor.GetOptimizationRecommendations();
```

### 3. Performance Monitoring Dashboard

Location: `src/components/Performance/PerformanceMonitoringDashboard.tsx`

**Features:**
- Real-time memory usage display
- Render count tracking
- Load time metrics
- Manual GC trigger (dev mode)
- Performance warnings

**Usage:**
Add to any page where performance monitoring is needed:
```typescript
import { PerformanceMonitoringDashboard } from '@/components/Performance/PerformanceMonitoringDashboard';

<PerformanceMonitoringDashboard />
```

## Performance Best Practices

### General Guidelines

1. **Use React.memo for expensive components**
   - Components that render frequently
   - Components with expensive render logic
   - List item components

2. **Debounce user inputs**
   - Search boxes
   - Auto-save functionality
   - Real-time validation

3. **Lazy load images**
   - Use `useLazyLoadImage` for off-screen images
   - Use `useProgressiveImage` for important images
   - Preload critical images on mount

4. **Limit concurrent API requests**
   - Use `useApiRequestPool` for batch operations
   - Implement pagination for large datasets
   - Cache API responses when appropriate

5. **Optimize FFmpeg operations**
   - Use hardware acceleration when available
   - Use optimal thread count
   - Choose appropriate presets
   - Clean up temporary files

6. **Monitor memory usage**
   - Enable memory monitoring in production
   - Set appropriate limits
   - Respond to memory warnings
   - Profile regularly during development

### Component Optimization Checklist

- [ ] Component is memoized if it renders frequently
- [ ] Event handlers are wrapped in useCallback
- [ ] Expensive computations use useMemo
- [ ] Images are lazy loaded
- [ ] Lists are virtualized (for 100+ items)
- [ ] API calls are debounced/throttled
- [ ] Cleanup functions remove event listeners
- [ ] No memory leaks from intervals/timeouts

### Video Generation Performance

1. **Hardware Acceleration**
   - Always detect and use available hardware
   - Fall back gracefully to software encoding
   - Log which encoder is being used

2. **Resource Management**
   - Use object pools for temporary objects
   - Clean up temporary files after use
   - Monitor memory during generation
   - Implement resource limits

3. **Parallel Processing**
   - Use optimal thread count
   - Don't exceed available cores
   - Leave headroom for the system

## Measuring Performance

### Frontend Metrics

```typescript
import { performanceMonitor } from '@/utils/performanceMonitor';

// Log current metrics
performanceMonitor.logMetrics('My Component');

// Get metrics programmatically
const metrics = performanceMonitor.getMetrics();
console.log('Memory:', metrics.memoryUsage);
console.log('Renders:', metrics.renderCount);
```

### Backend Metrics

```csharp
// Memory statistics
var stats = memoryMonitor.GetMemoryStatistics();
logger.LogInformation(
    "Memory: {MemoryMb} MB, Gen0: {Gen0}, Gen1: {Gen1}, Gen2: {Gen2}",
    stats.CurrentMemoryMb, stats.Gen0Collections, 
    stats.Gen1Collections, stats.Gen2Collections
);

// Hardware capabilities
var capabilities = await hwOptimizer.DetectCapabilitiesAsync();
logger.LogInformation(
    "Hardware acceleration: {Type}",
    capabilities.GetBestAccelerationType()
);
```

## Performance Budgets

### Load Time
- **Target:** < 3 seconds
- **Maximum:** < 5 seconds

### Memory Usage
- **Target:** < 500 MB
- **Maximum:** < 750 MB
- **Warning:** > 400 MB

### Rendering
- **Frame time:** < 16ms (60 FPS)
- **Component render:** < 100ms
- **API response:** < 200ms (fast), < 1s (slow)

## Troubleshooting

### High Memory Usage

1. Check memory monitor dashboard
2. Review recommendations
3. Force garbage collection
4. Clear image cache if needed
5. Reduce concurrent operations

### Slow Rendering

1. Profile with React DevTools
2. Check for unnecessary re-renders
3. Ensure components are memoized
4. Optimize expensive computations
5. Use virtual scrolling for large lists

### Poor Video Generation Performance

1. Verify hardware acceleration is enabled
2. Check thread count is optimal
3. Monitor memory during generation
4. Review FFmpeg command logs
5. Check for resource bottlenecks

## Future Improvements

- Implement Web Workers for heavy computations
- Add service worker for offline caching
- Implement progressive rendering
- Add performance budget CI checks
- Create automated performance tests
- Implement chunk loading optimization
