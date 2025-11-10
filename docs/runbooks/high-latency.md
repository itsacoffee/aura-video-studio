# Runbook: High API Latency

## Alert Details

- **Alert Name**: API Latency P95 Exceeded
- **Severity**: Warning
- **SLO Target**: P95 < 2000ms
- **Evaluation Window**: 5 minutes
- **Notification**: Slack

## Symptoms

- API responses are slow
- P95 latency exceeds 2 seconds
- User complaints about performance
- Timeouts may be occurring

## Quick Diagnostic Commands

```bash
# Check current latency
curl -s http://localhost:5005/api/monitoring/metrics/histogram/api.request_duration_ms | jq .

# Top slowest endpoints
curl -s http://localhost:5005/api/metrics | jq -r '.[] | "\(.averageDuration)ms \(.endpoint)"' | sort -rn | head -10

# Check system resources
docker stats --no-stream

# Check database query times
grep "Query took" logs/aura-api-$(date +%Y-%m-%d).log | tail -20
```

## Common Causes

### 1. Database Slow Queries

**Symptoms**:
- Logs show slow database queries
- Database CPU high
- Many endpoints affected

**Resolution**:
```bash
# Check slow queries
grep "Query took" logs/aura-api-*.log | awk '{print $NF}' | sort -rn | head -10

# Check database connections
curl -s http://localhost:5005/api/diagnostics/resources | jq '.databaseConnections'

# If connection pool exhausted
docker-compose restart api

# If database overloaded
# - Add indexes to slow queries
# - Optimize queries
# - Scale database
```

### 2. Provider API Slow

**Symptoms**:
- Specific provider endpoints slow
- Provider latency metrics high

**Resolution**:
```bash
# Check provider latencies
curl -s http://localhost:5005/api/monitoring/metrics | jq '.histograms[] | select(.name == "llm.latency_ms")'

# Switch to faster provider
curl -X POST http://localhost:5005/api/providers/disable \
  -H "Content-Type: application/json" \
  -d '{"provider": "<SlowProvider>", "reason": "High latency"}'
```

### 3. High CPU Usage

**Symptoms**:
- CPU usage > 80%
- All endpoints affected
- Response times degrading

**Resolution**:
```bash
# Scale up
docker-compose up -d --scale api=5

# Check for CPU-intensive operations
top -bn1 | head -20
```

### 4. Cache Miss Rate High

**Symptoms**:
- Cache hit rate dropped
- Increased database/provider calls

**Resolution**:
```bash
# Check cache hit rate
curl -s http://localhost:5005/api/monitoring/metrics | jq '.histograms[] | select(.name == "cache.access")'

# Clear and warm cache
curl -X POST http://localhost:5005/api/cache/clear
curl -X POST http://localhost:5005/api/cache/warm
```

## Escalation

- **After 1 hour**: If latency continues to degrade
- **If user impact**: Escalate immediately

## Post-Resolution

1. Identify root cause
2. Add performance monitoring if needed
3. Optimize slow operations

---

**Last Updated**: 2025-11-10
**Maintainer**: Platform Team
