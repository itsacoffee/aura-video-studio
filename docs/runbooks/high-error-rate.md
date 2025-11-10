# Runbook: High Error Rate

## Alert Details

- **Alert Name**: High Error Rate
- **Severity**: Critical
- **SLO Target**: Error rate < 1%
- **Evaluation Window**: 5 minutes
- **Notification**: PagerDuty + Slack

## Symptoms

- 5xx error rate exceeds 1%
- Multiple API endpoints returning errors
- Increased exception logs
- User complaints about failures

## Quick Diagnostic Commands

```bash
# Check current error rate
curl -s http://localhost:5005/api/monitoring/metrics/histogram/api.errors.5xx | jq .

# View recent errors
tail -n 100 logs/errors-$(date +%Y-%m-%d).log

# Top failing endpoints
grep "500\|502\|503\|504" logs/aura-api-$(date +%Y-%m-%d).log | \
  awk '{print $8}' | sort | uniq -c | sort -rn | head -10

# Check exception types
grep "Exception" logs/errors-$(date +%Y-%m-%d).log | \
  awk -F: '{print $2}' | sort | uniq -c | sort -rn | head -10
```

## Common Causes

### 1. NullReferenceException / Unhandled Exceptions

**Resolution**:
```bash
# If caused by recent deployment, rollback
git checkout HEAD~1 && docker-compose up -d

# Otherwise, restart to clear transient state
docker-compose restart api
```

### 2. Database Connection Errors

**Resolution**:
```bash
# Check database health
curl http://localhost:5005/api/health

# Restart database connection pool
docker-compose restart api

# If database is down
docker-compose restart db
```

### 3. Provider API Failures

**Resolution**:
```bash
# Identify failing provider
curl -s http://localhost:5005/api/health/providers | jq '.[] | select(.healthy == false)'

# Disable provider to trigger failover
curl -X POST http://localhost:5005/api/providers/disable \
  -H "Content-Type: application/json" \
  -d '{"provider": "<ProviderName>", "reason": "High error rate"}'
```

### 4. Memory Leaks / OOM

**Resolution**:
```bash
# Check memory usage
docker stats --no-stream

# Restart to clear memory
docker-compose restart api

# If persistent, scale up
docker-compose up -d --scale api=3
```

## Escalation

- **After 15 minutes**: Page secondary on-call
- **After 1 hour**: Escalate to engineering manager

## Post-Resolution

1. Verify error rate returned to normal
2. Complete post-mortem within 48 hours
3. Add action items to prevent recurrence

---

**Last Updated**: 2025-11-10
**Maintainer**: Platform Team
