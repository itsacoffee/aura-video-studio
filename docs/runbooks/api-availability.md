# Runbook: API Availability Below SLO

## Alert Details

- **Alert Name**: API Availability Below SLO
- **Severity**: Critical
- **SLO Target**: 99.9% availability
- **Evaluation Window**: 5 minutes
- **Notification**: PagerDuty + Slack

## Symptoms

- API success rate dropped below 99.9%
- Users experiencing 500 Internal Server Errors
- Increased error logs
- Health check endpoint may be returning errors

## Quick Response (First 5 Minutes)

```bash
# 1. Acknowledge the alert
# (Done automatically in PagerDuty)

# 2. Check current health status
curl -s http://localhost:5005/api/health | jq .

# 3. Check error rate
curl -s http://localhost:5005/api/monitoring/metrics/histogram/api.errors.5xx | jq .

# 4. Check recent errors
tail -n 50 logs/errors-$(date +%Y-%m-%d).log

# 5. Check if this is a deployment issue
git log --since="1 hour ago" --oneline
```

## Investigation Steps

### 1. Identify Failing Endpoints

```bash
# Check which endpoints are failing
grep -E "Status: 5[0-9]{2}" logs/aura-api-$(date +%Y-%m-%d).log | \
  awk '{print $NF}' | sort | uniq -c | sort -rn | head -10
```

### 2. Check System Resources

```bash
# CPU and Memory
docker stats --no-stream

# Disk space
df -h

# Database connections
curl -s http://localhost:5005/api/diagnostics/resources | jq '.databaseConnections'
```

### 3. Check Provider Health

```bash
# Check all provider health
curl -s http://localhost:5005/api/health/providers | jq .

# Check for provider failures
curl -s http://localhost:5005/api/monitoring/metrics/gauge/provider.healthy | jq .
```

### 4. Review Recent Changes

```bash
# Recent deployments
git log --since="2 hours ago" --oneline

# Recent config changes
git diff HEAD~5 -- Aura.Api/appsettings.json
```

### 5. Check Dependencies

```bash
# Database health
curl -s http://localhost:5005/api/health | jq '.checks[] | select(.name == "Database")'

# External service health
curl -s http://localhost:5005/api/health/providers | jq '.[] | select(.healthy == false)'
```

## Common Causes and Resolutions

### Cause 1: Recent Deployment Bug

**Symptoms**:
- Errors started immediately after deployment
- Specific endpoint pattern failing
- Stack traces in logs point to new code

**Resolution**:
```bash
# Rollback to previous version
git log -10 --oneline
git checkout <previous-stable-commit>

# Rebuild and redeploy
docker build -t aura-api:rollback .
docker-compose up -d

# Verify service restored
curl http://localhost:5005/api/health

# Monitor for 5 minutes
watch -n 10 'curl -s http://localhost:5005/api/monitoring/alerts/firing'
```

### Cause 2: Provider API Outage

**Symptoms**:
- Logs show provider API timeouts
- Provider health check failing
- Specific provider-related endpoints failing

**Resolution**:
```bash
# Check which provider is down
curl -s http://localhost:5005/api/health/providers | jq '.[] | select(.healthy == false)'

# Disable failing provider (triggers failover)
curl -X POST http://localhost:5005/api/providers/disable \
  -H "Content-Type: application/json" \
  -d '{"provider": "OpenAI", "reason": "API outage"}'

# Verify failover successful
curl -s http://localhost:5005/api/health/providers | jq .

# Test endpoint that was failing
curl -X POST http://localhost:5005/api/script/generate \
  -H "Content-Type: application/json" \
  -d '{"topic": "test"}'
```

### Cause 3: Resource Exhaustion

**Symptoms**:
- High CPU or memory usage
- Slow response times before failures
- OOM errors in logs

**Resolution**:
```bash
# Check resources
docker stats --no-stream

# If CPU/Memory high, scale up
docker-compose up -d --scale api=5

# If disk full, clear old logs/artifacts
docker system prune -f
rm -rf /tmp/*
find logs/ -name "*.log" -mtime +7 -delete

# If database connections exhausted
curl -X POST http://localhost:5005/api/cache/clear
docker-compose restart api
```

### Cause 4: Database Issues

**Symptoms**:
- Timeouts on database queries
- Connection pool errors
- All endpoints affected

**Resolution**:
```bash
# Check database health
curl -s http://localhost:5005/api/health | jq '.checks[] | select(.name == "Database")'

# Check connection pool
curl -s http://localhost:5005/api/diagnostics/resources | jq '.databaseConnections'

# Restart API to reset connections
docker-compose restart api

# If database itself is down, restart it
docker-compose restart db

# Verify restoration
curl http://localhost:5005/api/health
```

### Cause 5: High Traffic / DDoS

**Symptoms**:
- Sudden spike in request rate
- Many requests from same IPs
- Slow responses across all endpoints

**Resolution**:
```bash
# Check request rate
curl -s http://localhost:5005/api/metrics | jq '.["api.requests"]'

# Check top requesting IPs
grep "$(date +%Y-%m-%d)" logs/aura-api-*.log | \
  awk '{print $1}' | sort | uniq -c | sort -rn | head -20

# If DDoS, enable rate limiting
# (Already enabled by default, may need to tighten)

# Scale up to handle legitimate traffic
docker-compose up -d --scale api=10

# If specific IPs are attacking, block them
# (This would be done at firewall/load balancer level)
```

## Escalation

### Escalate After 30 Minutes If:
- Cannot identify root cause
- Mitigation attempts unsuccessful
- Service not restored

### Escalation Path:
1. **Secondary On-Call**: Page via PagerDuty
2. **Engineering Manager**: Call directly
3. **VP Engineering**: For prolonged SEV-1 incidents

### Escalation Contact:
```
Secondary On-Call: See PagerDuty schedule
Engineering Manager: manager@aura.studio
VP Engineering: vpe@aura.studio
```

## Communication

### Internal (Slack #incident-active)

```
🚨 INCIDENT: API Availability Below SLO

Severity: SEV-1
Started: 14:30 UTC
Current Availability: 98.5% (Target: 99.9%)
Impact: Users experiencing 500 errors

Status: Investigating
Lead: @oncall-engineer

Updates every 15 minutes.
```

### External (Status Page)

```
⚠️ Service Disruption

We are currently experiencing issues with our API.
Some users may encounter errors when using the service.

Our team is actively working on a resolution.

Started: 14:30 UTC
Updated: 14:45 UTC
Next update: 15:00 UTC
```

## Verification

After mitigation, verify service is fully restored:

```bash
# 1. Check health endpoint
curl http://localhost:5005/api/health
# Expected: All checks "Healthy"

# 2. Check error rate returned to normal
curl -s http://localhost:5005/api/monitoring/metrics/histogram/api.errors.5xx | jq '.count'
# Expected: Low or zero errors in last 5 min

# 3. Check availability metric
curl -s http://localhost:5005/api/monitoring/metrics | jq '.gauges[] | select(.name == "api_availability")'
# Expected: > 99.9%

# 4. Run smoke tests
./scripts/smoke-tests.sh

# 5. Monitor for 15 minutes to ensure stability
watch -n 30 'curl -s http://localhost:5005/api/monitoring/alerts/firing | jq .'
```

## Post-Incident

### Immediate Actions

1. **Close Alert**: Should auto-resolve when metrics return to normal
2. **Update Status Page**: Mark as resolved
3. **Post in Slack**: Incident resolved message
4. **Create Post-Mortem Issue**: In GitHub

### Post-Mortem (Within 48 Hours)

Create post-mortem covering:
- Timeline of events
- Root cause analysis
- What went well / what went wrong
- Action items to prevent recurrence

See: [Incident Response Procedures](../monitoring/INCIDENT_RESPONSE.md)

## Related Runbooks

- [High Error Rate](./high-error-rate.md)
- [High Latency](./high-latency.md)
- Provider Health
- Resource Exhaustion

## Monitoring

- **Dashboard**: Operational Dashboard
- **Metrics**: `api.requests`, `api.errors.5xx`, `api_availability`
- **Logs**: `logs/errors-YYYY-MM-DD.log`

## References

- SLO Configuration
- Alert Rules
- Monitoring API

---

**Last Updated**: 2025-11-10
**Maintainer**: Platform Team
**Tested**: 2025-11-10
