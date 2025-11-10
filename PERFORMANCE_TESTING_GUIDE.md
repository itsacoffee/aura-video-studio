# Performance Testing Guide

This guide provides comprehensive instructions for testing the performance optimizations implemented in PR #10.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Backend Performance Testing](#backend-performance-testing)
3. [Database Performance Testing](#database-performance-testing)
4. [Frontend Performance Testing](#frontend-performance-testing)
5. [Cache Performance Testing](#cache-performance-testing)
6. [Load Testing](#load-testing)
7. [Acceptance Criteria Validation](#acceptance-criteria-validation)

## Prerequisites

### Required Tools

```bash
# Install k6 for load testing
brew install k6  # macOS
# OR
choco install k6  # Windows
# OR
sudo snap install k6  # Linux

# Install Apache Bench (alternative)
sudo apt-get install apache2-utils  # Linux
brew install httpd  # macOS

# Install Lighthouse for frontend testing
npm install -g @lhci/cli lighthouse

# SQLite tools for database testing
sudo apt-get install sqlite3  # Linux
brew install sqlite3  # macOS
```

### Setup

```bash
# Start the application with performance logging
cd Aura.Api
dotnet run --configuration Release

# In another terminal, start frontend (if testing E2E)
cd Aura.Web
npm run build:prod
```

## Backend Performance Testing

### 1. API Response Time Testing

#### Test Individual Endpoints

```bash
# Test health endpoint (baseline)
time curl http://localhost:5005/api/health/live

# Test cached vs uncached endpoint
# First request (cache miss)
time curl http://localhost:5005/api/settings

# Second request (cache hit - should be faster)
time curl http://localhost:5005/api/settings
```

#### Expected Results
- **Health endpoint**: <50ms
- **Settings (cached)**: <20ms
- **Settings (uncached)**: <100ms

### 2. Compression Testing

```bash
# Test with compression
curl -H "Accept-Encoding: gzip, br" \
  -w "@curl-format.txt" \
  http://localhost:5005/api/settings

# curl-format.txt content:
cat > curl-format.txt << 'EOF'
    time_namelookup:  %{time_namelookup}\n
       time_connect:  %{time_connect}\n
    time_appconnect:  %{time_appconnect}\n
      time_redirect:  %{time_redirect}\n
   time_starttransfer: %{time_starttransfer}\n
                     ----------\n
         time_total:  %{time_total}\n
      size_download:  %{size_download}\n
EOF

# Compare with uncompressed
curl http://localhost:5005/api/settings | wc -c  # Uncompressed
curl -H "Accept-Encoding: gzip" http://localhost:5005/api/settings | wc -c  # Compressed
```

#### Expected Results
- **Compression Ratio**: 60-80% size reduction for JSON
- **Response Time**: Similar or faster (due to less data transfer)

### 3. Memory Usage Testing

```bash
# Monitor memory before load test
curl http://localhost:5005/api/performance/metrics | jq '.processMetrics.workingSetBytes'

# Run some operations
# ... perform operations ...

# Check memory after
curl http://localhost:5005/api/performance/metrics | jq '.processMetrics.workingSetBytes'

# Memory should be stable, not growing continuously
```

### 4. Cache Performance Testing

```bash
# Clear cache
curl -X POST http://localhost:5005/api/performance/cache/clear

# Get initial cache stats
curl http://localhost:5005/api/performance/cache/stats

# Make 100 identical requests
for i in {1..100}; do
  curl -s http://localhost:5005/api/settings > /dev/null
done

# Check cache stats
curl http://localhost:5005/api/performance/cache/stats | jq

# Expected: hitRate > 0.8 (80% hit rate)
```

## Database Performance Testing

### 1. Query Performance

```bash
# Connect to SQLite database
sqlite3 Aura.Api/aura.db

# Enable query timing
.timer on

# Test query with index
EXPLAIN QUERY PLAN 
SELECT * FROM ProjectStates 
WHERE Status = 'InProgress' 
ORDER BY UpdatedAt DESC;

# Should show: USING INDEX idx_projectstates_status_updatedAt

# Run actual query and measure time
SELECT * FROM ProjectStates 
WHERE Status = 'InProgress' 
ORDER BY UpdatedAt DESC;
```

#### Expected Results
- **Simple queries**: <10ms
- **Join queries**: <50ms
- **All queries should use indexes** (verify with EXPLAIN QUERY PLAN)

### 2. Connection Pooling Test

```bash
# Test concurrent connections (should not block)
for i in {1..20}; do
  curl -s http://localhost:5005/api/projects &
done
wait

# All requests should complete without connection errors
```

### 3. WAL Mode Verification

```sql
-- In SQLite
PRAGMA journal_mode;
-- Should return: wal

PRAGMA synchronous;
-- Should return: 1 (NORMAL)

PRAGMA cache_size;
-- Should return: -64000 (64MB)
```

## Frontend Performance Testing

### 1. Bundle Size Analysis

```bash
cd Aura.Web

# Build for production
npm run build:prod

# Check bundle sizes
du -sh dist/assets/*.js

# Expected:
# - react-vendor-*.js: < 200KB
# - ffmpeg-vendor-*.js: < 500KB
# - fluentui-components-*.js: < 250KB
# - main entry chunk: < 300KB
# - Total: < 1.5MB
```

### 2. Lighthouse Testing

```bash
# Start the application
cd Aura.Api
dotnet run --configuration Release

# In another terminal, run Lighthouse
lighthouse http://localhost:5005 \
  --output html \
  --output-path ./lighthouse-report.html \
  --preset=desktop

# Check scores (should be >90 for performance)
```

#### Expected Lighthouse Scores
- **Performance**: >90
- **First Contentful Paint**: <1.5s
- **Time to Interactive**: <3.0s
- **Speed Index**: <2.5s
- **Largest Contentful Paint**: <2.5s

### 3. Lazy Loading Verification

```bash
# Open browser DevTools
# Go to Network tab
# Navigate to http://localhost:5005

# Verify:
# 1. Initial load only loads critical chunks
# 2. Lazy chunks load on-demand when navigating
# 3. No duplicate chunks loaded

# Chrome DevTools > Coverage tab
# Shows how much code is unused on initial load
# Should be >60% unused (loaded lazily)
```

### 4. Cache Testing (Service Worker)

```typescript
// In browser console
// Check cache storage
caches.keys().then(console.log);

// Check if assets are cached
caches.open('aura-cache-v1').then(cache => {
  cache.keys().then(console.log);
});
```

## Cache Performance Testing

### 1. Redis Cache Test (if enabled)

```bash
# Connect to Redis
redis-cli

# Monitor cache operations
MONITOR

# In another terminal, make API requests
curl http://localhost:5005/api/settings

# Should see SET and GET operations in MONITOR output
```

### 2. Cache Hit Rate Test

```bash
# Test script
#!/bin/bash

# Clear cache
curl -X POST http://localhost:5005/api/performance/cache/clear

echo "Making 1000 requests..."
for i in {1..1000}; do
  curl -s http://localhost:5005/api/settings > /dev/null
  if [ $((i % 100)) -eq 0 ]; then
    echo "Progress: $i/1000"
  fi
done

# Check final hit rate
echo "Cache Statistics:"
curl http://localhost:5005/api/performance/cache/stats | jq
```

#### Expected Results
```json
{
  "hits": 999,
  "misses": 1,
  "errors": 0,
  "hitRate": 0.999,
  "backendType": "Hybrid (Redis + Memory)"
}
```

### 3. Cache Invalidation Test

```bash
# Get data (should be cached)
curl http://localhost:5005/api/settings > before.json

# Update settings
curl -X POST http://localhost:5005/api/settings \
  -H "Content-Type: application/json" \
  -d '{"key": "value"}'

# Get data again (should be updated)
curl http://localhost:5005/api/settings > after.json

# Compare
diff before.json after.json
# Should show differences (cache was invalidated)
```

## Load Testing

### 1. k6 Load Test

Create `performance-test.js`:

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '30s', target: 10 },  // Ramp up to 10 users
    { duration: '1m', target: 50 },   // Ramp up to 50 users
    { duration: '2m', target: 50 },   // Stay at 50 users
    { duration: '30s', target: 100 }, // Ramp up to 100 users
    { duration: '1m', target: 100 },  // Stay at 100 users
    { duration: '30s', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'], // 95% of requests < 1s
    errors: ['rate<0.01'],              // <1% error rate
  },
};

export default function () {
  const endpoints = [
    '/api/health/live',
    '/api/settings',
    '/api/providers',
    '/api/templates',
  ];

  endpoints.forEach(endpoint => {
    const res = http.get(`http://localhost:5005${endpoint}`);
    
    const success = check(res, {
      'status is 200': (r) => r.status === 200,
      'response time < 1s': (r) => r.timings.duration < 1000,
    });
    
    errorRate.add(!success);
    sleep(0.1);
  });
}
```

Run the test:

```bash
k6 run performance-test.js
```

#### Expected Results
```
     ✓ status is 200
     ✓ response time < 1s

     checks.........................: 100.00% ✓ 12000 ✗ 0
     data_received..................: 24 MB   200 kB/s
     data_sent......................: 960 kB  8.0 kB/s
     http_req_duration..............: avg=85ms  p(95)=245ms
     http_reqs......................: 12000   100/s
     errors.........................: 0.00%   ✓ 0 ✗ 12000
```

### 2. Apache Bench Test

```bash
# Test with 1000 requests, 10 concurrent
ab -n 1000 -c 10 http://localhost:5005/api/health/live

# Test with 10000 requests, 100 concurrent (stress test)
ab -n 10000 -c 100 http://localhost:5005/api/settings
```

#### Expected Results
```
Requests per second:    500-1000 [#/sec]
Time per request:       10-20 [ms] (mean)
Time per request:       1-2 [ms] (mean, across all concurrent requests)
Failed requests:        0
```

### 3. Endurance Test

```bash
# Run continuous load for 1 hour
k6 run --duration 1h --vus 20 performance-test.js

# Monitor memory throughout
watch -n 10 'curl -s http://localhost:5005/api/performance/metrics | jq .processMetrics'
```

#### Expected Behavior
- **Memory**: Should stabilize, not grow continuously
- **Response Time**: Should remain consistent
- **Error Rate**: Should stay <1%
- **GC**: Gen2 collections should be infrequent

## Acceptance Criteria Validation

### Criterion 1: 50% reduction in p95 latency

```bash
# Baseline (before optimization)
# Run load test and note p95 latency
k6 run baseline-test.js | grep "http_req_duration.*p(95)"

# After optimization
k6 run performance-test.js | grep "http_req_duration.*p(95)"

# Calculate: (baseline - optimized) / baseline * 100
# Should be >= 50%
```

**Target**: p95 < 1000ms (from ~2000ms baseline)

### Criterion 2: Database query count reduced by 30%

```bash
# Enable SQLite logging
# In appsettings.json, enable EF Core logging

# Count queries for a typical operation
# Before: ~100 queries per request
# After: < 70 queries per request

# Verify with:
grep "Executed DbCommand" logs/aura-api-*.log | wc -l
```

**Target**: <70 queries per typical request

### Criterion 3: Frontend bundle under 500KB

```bash
cd Aura.Web
npm run build:prod

# Check main entry chunk
ls -lh dist/assets/index-*.js

# Should be < 500KB (gzipped)
```

**Target**: Main bundle < 500KB

### Criterion 4: Time to interactive under 3s

```bash
# Use Lighthouse
lighthouse http://localhost:5005 \
  --only-categories=performance \
  --preset=desktop \
  | grep "Time to Interactive"
```

**Target**: TTI < 3000ms

### Criterion 5: Memory usage stable under load

```bash
# Run endurance test and monitor memory
# Memory should not grow more than 20% over 1 hour

# Script to monitor memory
#!/bin/bash
START_MEM=$(curl -s http://localhost:5005/api/performance/metrics | jq .processMetrics.workingSetBytes)
echo "Starting memory: $START_MEM bytes"

sleep 3600  # 1 hour

END_MEM=$(curl -s http://localhost:5005/api/performance/metrics | jq .processMetrics.workingSetBytes)
echo "Ending memory: $END_MEM bytes"

GROWTH=$(echo "scale=2; ($END_MEM - $START_MEM) / $START_MEM * 100" | bc)
echo "Memory growth: $GROWTH%"

# Should be < 20%
```

**Target**: Memory growth < 20% after 1 hour under load

## Performance Regression Testing

### Automated Test Suite

```bash
# Create test-performance.sh
#!/bin/bash

echo "=== Performance Regression Test Suite ==="
echo ""

# Test 1: API Response Time
echo "Test 1: API Response Time"
RESPONSE_TIME=$(curl -o /dev/null -s -w '%{time_total}\n' http://localhost:5005/api/health/live)
if (( $(echo "$RESPONSE_TIME < 0.050" | bc -l) )); then
  echo "✓ PASS: Response time ${RESPONSE_TIME}s < 0.050s"
else
  echo "✗ FAIL: Response time ${RESPONSE_TIME}s >= 0.050s"
fi
echo ""

# Test 2: Cache Hit Rate
echo "Test 2: Cache Hit Rate"
for i in {1..100}; do
  curl -s http://localhost:5005/api/settings > /dev/null
done
HIT_RATE=$(curl -s http://localhost:5005/api/performance/cache/stats | jq -r .hitRate)
if (( $(echo "$HIT_RATE > 0.80" | bc -l) )); then
  echo "✓ PASS: Cache hit rate $HIT_RATE > 0.80"
else
  echo "✗ FAIL: Cache hit rate $HIT_RATE <= 0.80"
fi
echo ""

# Test 3: Bundle Size
echo "Test 3: Bundle Size"
BUNDLE_SIZE=$(find Aura.Web/dist/assets -name "index-*.js" -exec stat -f%z {} \;)
if [ $BUNDLE_SIZE -lt 512000 ]; then
  echo "✓ PASS: Bundle size ${BUNDLE_SIZE} bytes < 500KB"
else
  echo "✗ FAIL: Bundle size ${BUNDLE_SIZE} bytes >= 500KB"
fi
echo ""

# Test 4: Memory Stability
echo "Test 4: Memory Stability (5 minute test)"
START_MEM=$(curl -s http://localhost:5005/api/performance/metrics | jq .processMetrics.workingSetBytes)
for i in {1..300}; do
  curl -s http://localhost:5005/api/settings > /dev/null
  sleep 1
done
END_MEM=$(curl -s http://localhost:5005/api/performance/metrics | jq .processMetrics.workingSetBytes)
GROWTH=$(echo "scale=2; ($END_MEM - $START_MEM) / $START_MEM * 100" | bc)
if (( $(echo "$GROWTH < 20" | bc -l) )); then
  echo "✓ PASS: Memory growth ${GROWTH}% < 20%"
else
  echo "✗ FAIL: Memory growth ${GROWTH}% >= 20%"
fi

echo ""
echo "=== Test Suite Complete ==="
```

Run regression tests:

```bash
chmod +x test-performance.sh
./test-performance.sh
```

## Continuous Monitoring

### Production Monitoring Setup

```bash
# Add to docker-compose.yml (optional)
prometheus:
  image: prom/prometheus
  volumes:
    - ./prometheus.yml:/etc/prometheus/prometheus.yml
  ports:
    - "9090:9090"

grafana:
  image: grafana/grafana
  ports:
    - "3001:3000"
  depends_on:
    - prometheus
```

### Key Metrics to Monitor

1. **Response Time**: p50, p95, p99
2. **Cache Hit Rate**: Should stay >80%
3. **Memory Usage**: Working set should be stable
4. **CPU Usage**: Should be reasonable (<70% sustained)
5. **Error Rate**: Should be <1%
6. **Database Query Time**: p95 <50ms

## Troubleshooting Performance Issues

### Slow Responses

```bash
# Check cache status
curl http://localhost:5005/api/performance/cache/stats

# Check memory
curl http://localhost:5005/api/performance/metrics

# Check GC
curl http://localhost:5005/api/performance/gc/stats

# Review logs
tail -f logs/performance-*.log
```

### High Memory Usage

```bash
# Check GC stats
curl http://localhost:5005/api/performance/gc/stats

# Force GC if needed
curl -X POST http://localhost:5005/api/performance/gc/collect

# Check for memory leaks
dotnet-dump collect -p $(pgrep dotnet)
```

### Low Cache Hit Rate

```bash
# Clear cache and rebuild
curl -X POST http://localhost:5005/api/performance/cache/clear

# Check Redis connection (if used)
redis-cli ping

# Verify cache configuration
grep -A 10 "Caching" appsettings.json
```

## Reporting Results

### Generate Performance Report

```bash
#!/bin/bash

echo "# Performance Test Report" > performance-report.md
echo "Date: $(date)" >> performance-report.md
echo "" >> performance-report.md

# API Performance
echo "## API Performance" >> performance-report.md
k6 run performance-test.js >> performance-report.md

# Cache Stats
echo "## Cache Statistics" >> performance-report.md
curl -s http://localhost:5005/api/performance/cache/stats >> performance-report.md

# Memory Stats
echo "## Memory Statistics" >> performance-report.md
curl -s http://localhost:5005/api/performance/metrics >> performance-report.md

# Bundle Size
echo "## Bundle Size" >> performance-report.md
du -h Aura.Web/dist/assets/*.js >> performance-report.md

# Lighthouse
echo "## Lighthouse Scores" >> performance-report.md
lighthouse http://localhost:5005 --output json | jq '.categories.performance.score' >> performance-report.md
```

## Conclusion

Following this guide ensures comprehensive performance testing across all optimization areas. All acceptance criteria should be validated before considering PR #10 complete.

For issues or questions, consult the [Performance Optimization Guide](PERFORMANCE_OPTIMIZATION_GUIDE.md).
