# Oncall Runbook - Aura Video Studio

This runbook provides procedures for diagnosing and resolving production incidents.

## Table of Contents

- [Getting Started](#getting-started)
- [Incident Response Process](#incident-response-process)
- [Common Issues](#common-issues)
- [Diagnostic Tools](#diagnostic-tools)
- [Using Correlation IDs](#using-correlation-ids)
- [Escalation Procedures](#escalation-procedures)
- [Post-Incident](#post-incident)

## Getting Started

### Prerequisites for Oncall

Before your oncall shift:

1. **Access verification**:
   - [ ] Production environment access configured
   - [ ] Log aggregation system accessible
   - [ ] Monitoring dashboards accessible
   - [ ] Incident management system access
   - [ ] PagerDuty/alerting system configured

2. **Tools installed**:
   - [ ] `kubectl` or deployment CLI
   - [ ] `jq` for JSON processing
   - [ ] Database client
   - [ ] SSH keys configured

3. **Knowledge checklist**:
   - [ ] Read this runbook completely
   - [ ] Review recent deployments
   - [ ] Check known issues board
   - [ ] Review architecture documentation

### Key System URLs

- **Production API**: https://api.aura.studio
- **Staging API**: https://staging-api.aura.studio
- **Monitoring**: https://metrics.aura.studio
- **Logs**: https://logs.aura.studio
- **Status Page**: https://status.aura.studio

### Quick Health Check

```bash
# System health
curl https://api.aura.studio/api/health/system | jq

# Provider health
curl https://api.aura.studio/api/health/providers | jq

# Liveness (is app running)
curl https://api.aura.studio/health/live

# Readiness (ready to serve traffic)
curl https://api.aura.studio/health/ready
```

## Incident Response Process

### Step 1: Acknowledge and Assess

1. **Acknowledge the alert** in PagerDuty/incident system
2. **Check system status**:
   ```bash
   curl https://api.aura.studio/api/health/system | jq
   ```
3. **Assess severity**:
   - **SEV1** (Critical): Complete system outage, data loss, security breach
   - **SEV2** (High): Major feature broken, significant performance degradation
   - **SEV3** (Medium): Minor feature broken, some users affected
   - **SEV4** (Low): Cosmetic issue, workaround available

4. **Update status page** if user-facing issue

### Step 2: Gather Information

1. **Recent changes**:
   ```bash
   # Check recent deployments
   kubectl rollout history deployment/aura-api
   ```

2. **Current metrics**:
   - Error rate
   - Response times (P50, P95, P99)
   - Request volume
   - Resource utilization (CPU, memory, disk)

3. **Error logs** (last 15 minutes):
   ```bash
   # Get recent errors
   kubectl logs -l app=aura-api --since=15m | grep ERROR
   ```

4. **User reports**: Check support channels for user-reported issues

### Step 3: Diagnose

Use diagnostic tools (see [Diagnostic Tools](#diagnostic-tools) section) to identify root cause.

**Common diagnostic paths**:

- High error rate → Check provider health, database connections
- Slow response times → Check resource utilization, database queries
- Complete outage → Check pod status, infrastructure health
- Intermittent issues → Check circuit breakers, rate limits

### Step 4: Mitigate

**Temporary mitigations**:

1. **Scale up** if resource constrained:
   ```bash
   kubectl scale deployment/aura-api --replicas=20
   ```

2. **Restart pods** if memory leak or stale state:
   ```bash
   kubectl rollout restart deployment/aura-api
   ```

3. **Enable offline mode** if provider issues:
   ```bash
   # Update configuration to use only local providers
   kubectl set env deployment/aura-api OFFLINE_MODE=true
   ```

4. **Rollback** if caused by recent deployment:
   ```bash
   kubectl rollout undo deployment/aura-api
   ```

### Step 5: Resolve

Implement permanent fix:

1. For code issues: Deploy hotfix
2. For configuration issues: Update configuration
3. For infrastructure issues: Scale or reconfigure infrastructure
4. For provider issues: Switch providers or wait for recovery

### Step 6: Communicate

1. **Internal**: Update incident channel with status
2. **External**: Update status page if customer-facing
3. **Stakeholders**: Notify if high-severity or prolonged
4. **Documentation**: Record actions taken and outcome

## Common Issues

### Issue: High Error Rate

**Symptoms**:
- Error rate dashboard shows spike
- Logs filled with exceptions
- Users reporting errors

**Diagnosis**:

```bash
# Check error types
kubectl logs -l app=aura-api --since=5m | grep ERROR | cut -d' ' -f5- | sort | uniq -c | sort -rn | head -20

# Check which endpoints failing
kubectl logs -l app=aura-api --since=5m | grep "HTTP 500" | awk '{print $10}' | sort | uniq -c | sort -rn
```

**Common causes and fixes**:

1. **Provider failures**:
   ```bash
   curl https://api.aura.studio/api/health/providers | jq
   # Fix: Enable fallback providers or switch to offline mode
   ```

2. **Database connection issues**:
   ```bash
   # Check database connectivity
   kubectl exec -it $(kubectl get pod -l app=aura-api -o jsonpath='{.items[0].metadata.name}') -- curl localhost:5005/health/ready
   # Fix: Restart database connection pool or scale database
   ```

3. **Rate limiting**:
   ```bash
   # Check for rate limit errors
   kubectl logs -l app=aura-api --since=5m | grep "Rate limit exceeded"
   # Fix: Increase rate limits or scale API instances
   ```

### Issue: Slow Performance

**Symptoms**:
- P95 latency increased
- Users reporting slowness
- Request queue building up

**Diagnosis**:

```bash
# Check resource utilization
kubectl top pods -l app=aura-api

# Check for slow queries
# (View APM/tracing for slow transactions)

# Check provider latency
curl https://api.aura.studio/api/diagnostics/metrics | jq '.providerMetrics'
```

**Common causes and fixes**:

1. **High CPU usage**:
   ```bash
   # Scale horizontally
   kubectl scale deployment/aura-api --replicas=20
   ```

2. **Memory pressure**:
   ```bash
   # Check for memory leaks
   kubectl logs -l app=aura-api | grep "OutOfMemory"
   # Fix: Restart pods, investigate memory leak
   ```

3. **Slow database queries**:
   ```bash
   # Identify slow queries in database logs
   # Fix: Add indexes, optimize queries, or cache results
   ```

4. **Provider timeouts**:
   ```bash
   # Check provider response times
   curl https://api.aura.studio/api/health/providers | jq '.[] | select(.averageLatency > 1000)'
   # Fix: Increase timeout, switch providers, or enable caching
   ```

### Issue: Complete System Outage

**Symptoms**:
- All health checks failing
- 503 or 500 responses for all requests
- No pods running

**Diagnosis**:

```bash
# Check pod status
kubectl get pods -l app=aura-api

# Check deployment status
kubectl describe deployment aura-api

# Check for infrastructure issues
kubectl get nodes
```

**Common causes and fixes**:

1. **All pods crashed**:
   ```bash
   # Check pod logs
   kubectl logs -l app=aura-api --previous
   # Fix: Identify crash cause, fix, redeploy
   ```

2. **Kubernetes/infrastructure issue**:
   ```bash
   # Check cluster events
   kubectl get events --sort-by='.lastTimestamp' | tail -20
   # Fix: Contact infrastructure team or cloud provider
   ```

3. **Database unavailable**:
   ```bash
   # Check database status
   # Fix: Restore database, restore from backup, or failover to replica
   ```

4. **Recent bad deployment**:
   ```bash
   # Rollback immediately
   kubectl rollout undo deployment/aura-api
   kubectl rollout status deployment/aura-api
   ```

### Issue: Memory Leak

**Symptoms**:
- Memory usage grows continuously
- Pods get OOM killed and restart
- Performance degrades over time

**Diagnosis**:

```bash
# Monitor memory over time
watch -n 10 'kubectl top pods -l app=aura-api'

# Check for OOM kills
kubectl get events --field-selector reason=OOMKilled

# Check pod restart count
kubectl get pods -l app=aura-api -o jsonpath='{range .items[*]}{.metadata.name}{"\t"}{.status.containerStatuses[0].restartCount}{"\n"}{end}'
```

**Immediate mitigation**:

```bash
# Restart pods on rolling basis
kubectl rollout restart deployment/aura-api

# Or force immediate restart
kubectl delete pods -l app=aura-api --grace-period=0 --force
```

**Investigation**:

1. Capture heap dump before restarting:
   ```bash
   kubectl exec -it $(kubectl get pod -l app=aura-api -o jsonpath='{.items[0].metadata.name}') -- dotnet-dump collect -p 1
   ```

2. Review recent code changes for:
   - Unclosed database connections
   - Event listener leaks
   - Cached data not expiring
   - Large object retention

### Issue: Circuit Breakers Tripped

**Symptoms**:
- Errors mentioning "circuit breaker open"
- Fallback providers being used
- Provider health showing "Unhealthy"

**Diagnosis**:

```bash
# Check provider health
curl https://api.aura.studio/api/health/providers | jq

# Check circuit breaker status
curl https://api.aura.studio/api/diagnostics/circuit-breakers | jq
```

**Resolution**:

1. **Identify failing provider**:
   ```bash
   curl https://api.aura.studio/api/health/providers | jq '.[] | select(.status == "Unhealthy")'
   ```

2. **Check provider status** (OpenAI, ElevenLabs, etc.)

3. **Options**:
   - Wait for provider recovery (circuit will auto-reset)
   - Switch to different provider
   - Enable offline mode temporarily

4. **Manual circuit reset** (if provider recovered):
   ```bash
   # This endpoint may not exist - check API documentation
   curl -X POST https://api.aura.studio/api/circuit-breakers/{provider}/reset
   ```

## Diagnostic Tools

### 1. Health Endpoints

```bash
# Overall system health
curl https://api.aura.studio/api/health/system | jq

# Example response:
# {
#   "status": "Healthy",
#   "timestamp": "2025-11-02T15:00:00Z",
#   "components": {
#     "llm": "Healthy",
#     "tts": "Degraded",
#     "storage": "Healthy"
#   }
# }
```

### 2. Diagnostics Report

```bash
# Generate full diagnostics report
curl https://api.aura.studio/api/diagnostics/report | jq

# This returns:
# - System information
# - Provider status
# - Recent errors
# - Performance metrics
# - Resource usage
```

### 3. Logs with Grep Patterns

```bash
# All errors in last hour
kubectl logs -l app=aura-api --since=1h | grep ERROR

# Specific error type
kubectl logs -l app=aura-api --since=30m | grep "NullReferenceException"

# HTTP 500 errors
kubectl logs -l app=aura-api --since=15m | grep "HTTP 500"

# Slow requests (>1000ms)
kubectl logs -l app=aura-api --since=10m | grep "Duration.*[1-9][0-9][0-9][0-9]ms"
```

### 4. Metrics Queries

Using your monitoring system (Prometheus/Grafana):

```promql
# Error rate
rate(http_requests_total{status=~"5.."}[5m])

# Request latency P95
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Memory usage
container_memory_usage_bytes{pod=~"aura-api.*"}

# CPU usage
rate(container_cpu_usage_seconds_total{pod=~"aura-api.*"}[5m])
```

## Using Correlation IDs

Every API request/response includes an `X-Correlation-ID` header for tracking requests across the system.

### Finding Correlation IDs

**From user report**:
- Ask user for approximate time and action taken
- Search logs for matching pattern

**From error notification**:
- Correlation ID usually included in alert details
- Check alert payload for `correlationId` field

**From API response**:
```bash
# Make request and capture header
curl -I https://api.aura.studio/api/health/system | grep -i correlation
# X-Correlation-ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

### Tracing with Correlation IDs

```bash
# Find all logs for a specific request
CORRELATION_ID="a1b2c3d4-e5f6-7890-abcd-ef1234567890"

# In Kubernetes
kubectl logs -l app=aura-api --since=1h | grep "$CORRELATION_ID"

# In centralized logging (adjust for your system)
# ElasticSearch
curl -X GET "https://logs.aura.studio/_search?q=correlationId:$CORRELATION_ID"

# CloudWatch Logs
aws logs filter-log-events --log-group-name /aws/aura-api --filter-pattern "$CORRELATION_ID"

# Splunk
# Search: index=aura correlationId=$CORRELATION_ID
```

### Example: Full Request Trace

```bash
CORRELATION_ID="abc-123"

# Get all logs with this correlation ID
kubectl logs -l app=aura-api --since=2h | grep "$CORRELATION_ID" | less

# You should see:
# 1. Initial request received
# 2. Provider calls made
# 3. Database queries executed
# 4. Response sent
# 5. Any errors encountered
```

This allows you to trace the complete flow of a request and identify where failures occurred.

## Escalation Procedures

### When to Escalate

Escalate if:

- Issue severity is SEV1 and not resolved within 15 minutes
- Issue requires specialized knowledge (database, infrastructure)
- Issue requires access you don't have
- Issue is getting worse despite mitigation attempts
- You've been working on issue for >2 hours with no progress

### Escalation Contacts

1. **Backend Team Lead**: [Contact info from team roster]
2. **DevOps Engineer**: [Contact info from team roster]
3. **Database Administrator**: [Contact info from team roster]
4. **Engineering Manager**: [Contact info from team roster]

### Escalation Message Template

```
Subject: [SEV{level}] {Brief issue description}

Issue: {What's broken}
Impact: {How many users affected, what functionality impacted}
Started: {When issue began}
Actions Taken: 
  - {Action 1}
  - {Action 2}
Current Status: {Current state}
Correlation IDs: {Relevant correlation IDs}

Request: {What help you need}
```

### External Escalations

If issue is with third-party service:

1. **Check status page** first (OpenAI, ElevenLabs, AWS, etc.)
2. **File support ticket** with:
   - Account ID
   - Affected endpoints
   - Error messages
   - Correlation IDs
   - Time range
3. **Implement workaround** while waiting (fallback providers, caching, etc.)

## Post-Incident

### Immediate Post-Resolution

1. **Verify resolution**:
   - All health checks green
   - Error rate back to normal
   - Performance metrics normal
   - Users reporting issue resolved

2. **Update communications**:
   - Update status page to "Resolved"
   - Notify stakeholders of resolution
   - Update incident ticket with resolution

3. **Monitor** for 30-60 minutes after resolution

### Post-Incident Review (within 24-48 hours)

Create post-incident review document covering:

1. **Timeline**: When issue started, detected, mitigated, resolved
2. **Root cause**: What caused the issue
3. **Impact**: How many users affected, duration, business impact
4. **Response effectiveness**: What went well, what didn't
5. **Action items**: Preventive measures, monitoring improvements, documentation updates

### Action Items

Common action items from incidents:

- Add monitoring/alerting for new failure mode
- Improve documentation based on what was unclear
- Add automated recovery for known failure patterns
- Update runbook with lessons learned
- Schedule team discussion of incident

## Useful Commands Reference

```bash
# Pod management
kubectl get pods -l app=aura-api
kubectl describe pod {pod-name}
kubectl logs {pod-name} --tail=100 --follow
kubectl exec -it {pod-name} -- /bin/bash

# Deployment management
kubectl get deployments
kubectl describe deployment aura-api
kubectl rollout history deployment/aura-api
kubectl rollout undo deployment/aura-api
kubectl scale deployment/aura-api --replicas=10

# Configuration
kubectl get configmap
kubectl describe configmap aura-config
kubectl get secret

# Events
kubectl get events --sort-by='.lastTimestamp' | tail -20

# Resource usage
kubectl top nodes
kubectl top pods -l app=aura-api

# Database (adjust for your database type)
kubectl exec -it {db-pod} -- psql -U aura -d aura_production
```

## Helpful Links

- **Architecture Docs**: /docs/architecture/
- **API Documentation**: https://api.aura.studio/swagger
- **Monitoring Dashboard**: https://metrics.aura.studio
- **Log Viewer**: https://logs.aura.studio
- **Recent Deployments**: https://github.com/Saiyan9001/aura-video-studio/deployments
- **Known Issues**: https://github.com/Saiyan9001/aura-video-studio/issues?q=is:issue+is:open+label:production

## Document Maintenance

- **Last Updated**: 2025-11-02
- **Review Frequency**: Monthly or after each incident
- **Owner**: DevOps Team

Update this runbook when:
- New common issues identified
- Procedures change
- New tools added
- Feedback from oncall engineers
