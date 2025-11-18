# Resilience Runbook

## Quick Reference Guide for Operators

### Emergency Procedures

#### Circuit Breaker Opened - Service Unavailable

**Symptoms:**
- API returns 503 Service Unavailable
- Logs show "Circuit breaker OPENED" messages
- `/api/resilience/health` shows degraded services

**Diagnosis:**
```bash
# Check which services have open circuits
curl http://localhost:5000/api/resilience/circuit-breakers

# Get detailed metrics for the service
curl http://localhost:5000/api/resilience/metrics/ServiceName

# Check recent errors
curl http://localhost:5000/api/resilience/errors/recent?count=50
```

**Resolution:**
1. **Identify root cause:**
   - Check external service status (OpenAI, Anthropic, etc.)
   - Verify network connectivity
   - Check API keys are valid
   - Review recent errors for patterns

2. **Quick fixes:**
   ```bash
   # If temporary issue resolved, reset metrics to allow retry
   curl -X POST http://localhost:5000/api/resilience/metrics/ServiceName/reset
   ```

3. **Configuration adjustments:**
   - Edit `appsettings.json` to adjust circuit breaker thresholds
   - Increase `OpenDurationSeconds` if service needs more recovery time
   - Decrease `FailureThreshold` if too sensitive

#### High Error Rate Alert

**Symptoms:**
- Alert: "High error rate detected"
- Dashboard shows >10 errors/minute
- `/api/resilience/health` status is "Degraded"

**Diagnosis:**
```bash
# Get error rate trend
curl "http://localhost:5000/api/resilience/metrics/ServiceName/error-rate?windowMinutes=15"

# Check error categories
curl http://localhost:5000/api/resilience/metrics/ServiceName

# Get active alerts
curl http://localhost:5000/api/resilience/alerts
```

**Resolution:**
1. **Check error categories:**
   - Timeout errors → Increase timeout or optimize operations
   - Rate limit errors → Reduce request rate or upgrade API plan
   - Network errors → Check connectivity, firewall rules
   - Validation errors → Check input data quality

2. **Temporary mitigation:**
   - Enable fallback providers
   - Reduce parallel request limit
   - Increase retry delays

#### Saga Compensation Failed

**Symptoms:**
- Logs show "Failed to compensate saga step"
- Partial state changes visible
- Data inconsistency reported

**Diagnosis:**
```bash
# Check logs for saga execution
grep "SagaOrchestrator" logs/aura-api-*.log

# Look for compensation_failed events
grep "compensation_failed" logs/aura-api-*.log
```

**Resolution:**
1. **Identify failed step:**
   - Review saga events in logs
   - Check CorrelationId for full transaction trace

2. **Manual compensation:**
   - Run manual cleanup scripts
   - Delete/rollback created resources
   - Update database records

3. **Prevention:**
   - Ensure compensation actions are idempotent
   - Add validation before creating resources
   - Consider implementing saga persistence

### Health Monitoring

#### Dashboard URLs

```
Health Overview:          /api/resilience/health
Circuit Breakers:         /api/resilience/circuit-breakers
Error Metrics:            /api/resilience/metrics
Recent Errors:            /api/resilience/errors/recent?count=100
Active Alerts:            /api/resilience/alerts
```

#### Health Status Interpretation

| Status | Meaning | Action Required |
|--------|---------|----------------|
| Healthy | All systems operational | None |
| Degraded | Some issues detected | Monitor, investigate if persists |
| Unhealthy | Critical issues present | Immediate action required |

#### Key Metrics

```json
{
  "errorRate": 0.025,          // <5% is healthy
  "errorsPerMinute": 2.5,      // <10 is healthy
  "circuitState": "Closed",    // Closed is healthy
  "lastErrorTime": "..."       // Recent = investigate
}
```

### Configuration Guide

#### Circuit Breaker Tuning

**Default Settings:**
```json
{
  "CircuitBreaker": {
    "FailureThreshold": 5,           // Consecutive failures before opening
    "FailureRateThreshold": 0.5,     // 50% failure rate threshold
    "OpenDurationSeconds": 30,       // Time to stay open before half-open
    "TimeoutSeconds": 10,            // Request timeout
    "RollingWindowSize": 100,        // Number of requests to track
    "RollingWindowMinutes": 5        // Time window for failure rate
  }
}
```

**Tuning Recommendations:**

| Scenario | Adjust | To |
|----------|--------|-----|
| Too sensitive (opens too often) | ↑ `FailureThreshold` | 7-10 |
| Not sensitive enough | ↓ `FailureThreshold` | 3 |
| Service needs more recovery time | ↑ `OpenDurationSeconds` | 60-120 |
| Fast recovery expected | ↓ `OpenDurationSeconds` | 10-20 |
| Slow API calls timing out | ↑ `TimeoutSeconds` | 30-60 |

#### Retry Policy Tuning

**Provider-Specific Settings:**

| Provider | Max Retries | Base Delay | Backoff |
|----------|-------------|------------|---------|
| OpenAI | 4 | 2s | Exponential + Jitter |
| Anthropic | 4 | 2s | Exponential + Jitter |
| Ollama | 3 | 100ms | Constant |

**Custom Configuration:**
```csharp
var pipeline = _factory.CreateCustomPipeline<Result>(new ResiliencePipelineOptions
{
    Name = "CustomService",
    MaxRetryAttempts = 5,
    RetryDelay = TimeSpan.FromSeconds(3),
    CircuitBreakerBreakDuration = TimeSpan.FromMinutes(2)
});
```

### Monitoring Automation

#### Setting Up Alerts

**Prometheus/Grafana Integration:**
```yaml
# Add to prometheus.yml
scrape_configs:
  - job_name: 'aura_resilience'
    metrics_path: '/api/resilience/metrics'
    scrape_interval: 30s
    static_configs:
      - targets: ['localhost:5000']
```

**Alert Rules:**
```yaml
groups:
  - name: resilience
    interval: 30s
    rules:
      - alert: CircuitBreakerOpen
        expr: circuit_breaker_state == 1
        for: 1m
        annotations:
          summary: "Circuit breaker opened for {{ $labels.service }}"
      
      - alert: HighErrorRate
        expr: error_rate > 0.1
        for: 5m
        annotations:
          summary: "High error rate on {{ $labels.service }}"
```

#### Log Analysis

**Common Log Patterns:**
```bash
# Find all circuit breaker state changes
grep "Circuit breaker" logs/aura-api-*.log | tail -20

# Find error spikes
grep -c "ERROR" logs/aura-api-$(date +%Y%m%d).log

# Find specific error categories
grep "category.*Timeout" logs/aura-api-*.log

# Find saga failures
grep "Saga.*failed" logs/aura-api-*.log
```

### Performance Optimization

#### Reducing Error Rates

1. **Optimize retry logic:**
   - Add intelligent backoff based on error type
   - Skip retries for known non-retryable errors
   - Use circuit breakers to fail fast

2. **Connection pooling:**
   ```csharp
   builder.Services.AddHttpClient<MyService>()
       .SetHandlerLifetime(TimeSpan.FromMinutes(5))
       .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
       {
           PooledConnectionLifetime = TimeSpan.FromMinutes(2),
           MaxConnectionsPerServer = 10
       });
   ```

3. **Request deduplication:**
   ```csharp
   var result = await _idempotencyManager.ExecuteIdempotentAsync(
       idempotencyKey,
       () => CallExpensiveOperation()
   );
   ```

#### Memory Management

**Cleanup Configuration:**
```json
{
  "Resilience": {
    "Idempotency": {
      "DefaultTtlHours": 24,
      "MaxRecords": 10000,
      "CleanupIntervalHours": 1
    },
    "Monitoring": {
      "CleanupIntervalHours": 1,
      "AlertRetentionHours": 24
    }
  }
}
```

**Manual Cleanup:**
```bash
# Clear old idempotency records
curl -X POST http://localhost:5000/api/admin/cleanup/idempotency

# Clear old alerts
curl -X POST http://localhost:5000/api/admin/cleanup/alerts
```

### Testing Procedures

#### Chaos Engineering Tests

**Simulate Circuit Breaker Opening:**
```csharp
// In test environment
for (int i = 0; i < 10; i++)
{
    try {
        await _service.CallExternalApi();
    } catch {
        // Expected failures to trigger circuit breaker
    }
}

// Verify circuit is open
var state = await _client.GetAsync("/api/resilience/circuit-breakers/ExternalApi");
Assert.Equal("Open", state.State);
```

**Simulate High Error Rate:**
```bash
# Load test with intentional failures
ab -n 1000 -c 10 http://localhost:5000/api/test/failure-injection
```

**Test Saga Compensation:**
```csharp
// Test that compensation runs on failure
var saga = new TestSaga();
var result = await _orchestrator.ExecuteAsync(context, saga.Steps);
Assert.False(result.Success);
Assert.True(saga.Step1.WasCompensated);
```

#### Recovery Time Testing

```bash
# 1. Open circuit breaker
curl -X POST http://localhost:5000/api/test/circuit-breaker/open

# 2. Wait for recovery period
sleep 35

# 3. Verify half-open state
curl http://localhost:5000/api/resilience/circuit-breakers/TestService

# 4. Send success request to close
curl http://localhost:5000/api/test/success

# 5. Verify closed state
curl http://localhost:5000/api/resilience/circuit-breakers/TestService
```

### Common Issues and Solutions

#### Issue: Circuit Breaker Won't Close

**Possible Causes:**
- External service still failing
- Timeout too short for service
- Health check failing

**Solution:**
```bash
# 1. Verify external service is actually healthy
curl https://api.openai.com/v1/models -H "Authorization: Bearer $KEY"

# 2. Check timeout configuration
# 3. Reset metrics if service is confirmed healthy
curl -X POST http://localhost:5000/api/resilience/metrics/OpenAI/reset
```

#### Issue: Idempotency Records Growing Too Large

**Symptoms:**
- Memory usage increasing
- Slow idempotency checks

**Solution:**
```json
{
  "Resilience": {
    "Idempotency": {
      "DefaultTtlHours": 6,      // Reduce from 24
      "MaxRecords": 5000,        // Reduce from 10000
      "CleanupIntervalHours": 0.5 // Increase frequency
    }
  }
}
```

#### Issue: Saga Steps Not Compensating

**Possible Causes:**
- CanCompensate = false
- Compensation logic throwing exception
- State not stored in context

**Debugging:**
```csharp
// Add logging in BaseSagaStep
protected override async Task CompensateAsync(SagaContext context, CancellationToken ct)
{
    _logger.LogInformation("Compensating step {StepId}", StepId);
    
    try {
        // Your compensation logic
    } catch (Exception ex) {
        _logger.LogError(ex, "Compensation failed for {StepId}", StepId);
        throw;
    }
}
```

### SLA Targets

| Metric | Target | Threshold |
|--------|--------|-----------|
| Transient Error Recovery | <5 seconds | 10 seconds |
| Circuit Breaker Recovery | <30 seconds | 60 seconds |
| Saga Compensation | <10 seconds | 30 seconds |
| Overall Availability | 99.9% | 99.5% |
| Error Rate | <5% | <10% |

### Contact and Escalation

**Level 1 - Operations:**
- Monitor dashboards
- Basic troubleshooting
- Configuration adjustments
- Metric resets

**Level 2 - Engineering:**
- Circuit breaker tuning
- Retry policy optimization
- Saga compensation debugging
- Performance optimization

**Level 3 - Architecture:**
- Resilience pattern redesign
- External service integration issues
- Distributed transaction problems
- System-wide reliability concerns

## Appendix

### Useful Commands Reference

```bash
# Health check
curl http://localhost:5000/api/resilience/health | jq

# All circuit breakers
curl http://localhost:5000/api/resilience/circuit-breakers | jq

# Service metrics
curl http://localhost:5000/api/resilience/metrics | jq

# Recent errors
curl 'http://localhost:5000/api/resilience/errors/recent?count=50' | jq

# Error rate (5 min window)
curl 'http://localhost:5000/api/resilience/metrics/OpenAI/error-rate?windowMinutes=5' | jq

# Active alerts
curl http://localhost:5000/api/resilience/alerts | jq

# Reset service metrics
curl -X POST http://localhost:5000/api/resilience/metrics/OpenAI/reset

# Tail logs for errors
tail -f logs/errors-$(date +%Y%m%d).log

# Count errors by service
grep ERROR logs/aura-api-*.log | awk '{print $NF}' | sort | uniq -c | sort -rn

# Find correlation ID
grep "correlation-id-here" logs/aura-api-*.log
```

### Configuration Templates

**Production (Conservative):**
```json
{
  "CircuitBreaker": {
    "FailureThreshold": 7,
    "FailureRateThreshold": 0.4,
    "OpenDurationSeconds": 60,
    "TimeoutSeconds": 30
  }
}
```

**Development (Aggressive):**
```json
{
  "CircuitBreaker": {
    "FailureThreshold": 3,
    "FailureRateThreshold": 0.6,
    "OpenDurationSeconds": 10,
    "TimeoutSeconds": 10
  }
}
```

**High-Traffic (Optimized):**
```json
{
  "CircuitBreaker": {
    "FailureThreshold": 10,
    "FailureRateThreshold": 0.3,
    "OpenDurationSeconds": 45,
    "TimeoutSeconds": 20,
    "RollingWindowSize": 200
  }
}
```
