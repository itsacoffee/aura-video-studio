# Health Checks Implementation Guide

## Overview

The Aura system implements comprehensive health checks to ensure operational visibility and automatic recovery. This guide covers the health check system architecture, endpoints, and operational procedures.

## Architecture

### Health Check Components

1. **Database Health Check** (`DatabaseHealthCheck`)
   - Validates database connectivity and responsiveness
   - Measures query response times
   - Reports connection status and performance metrics

2. **Memory Health Check** (`MemoryHealthCheck`)
   - Monitors application memory usage
   - Tracks GC statistics
   - Detects memory pressure and potential leaks

3. **Disk Space Health Check** (`DiskSpaceHealthCheck`)
   - Monitors available disk space
   - Alerts on low disk space conditions
   - Prevents disk exhaustion failures

4. **Dependency Health Check** (`DependencyHealthCheck`)
   - Validates FFmpeg availability and version
   - Checks GPU availability and capabilities
   - Verifies system tier configuration

5. **Provider Health Check** (`ProviderHealthCheck`)
   - Validates LLM provider configuration
   - Checks TTS provider availability
   - Verifies API key configuration

6. **Startup Health Check** (`StartupHealthCheck`)
   - Ensures application initialization is complete
   - Used for Kubernetes readiness probes
   - Prevents premature traffic routing

## Health Check Endpoints

### Liveness Probe
**Endpoint:** `GET /health/live`

Returns a simple 200 OK if the application process is running.

```json
{
  "status": "healthy",
  "timestamp": "2025-11-10T12:00:00Z"
}
```

**Use Case:** Kubernetes liveness probe, basic uptime monitoring

### Readiness Probe
**Endpoint:** `GET /health/ready`

Returns detailed status of all readiness checks.

```json
{
  "status": "healthy",
  "timestamp": "2025-11-10T12:00:00Z",
  "duration": 123.45,
  "checks": [
    {
      "name": "Database",
      "status": "healthy",
      "description": "Database healthy (response: 23ms)",
      "duration": 25.3,
      "data": {
        "connection_available": true,
        "response_time_ms": 23,
        "project_count": 42
      },
      "tags": ["ready", "db"]
    }
  ]
}
```

**Use Case:** Kubernetes readiness probe, load balancer health checks

### Full Health Check
**Endpoint:** `GET /health`

Returns comprehensive health information including environment and version.

```json
{
  "status": "healthy",
  "timestamp": "2025-11-10T12:00:00Z",
  "duration": 145.67,
  "environment": "Production",
  "version": "1.0.0",
  "checks": [...]
}
```

**Use Case:** Monitoring dashboards, detailed diagnostics

### Tag-Based Health Checks
**Endpoint:** `GET /health/{tag}`

Returns health checks filtered by tag (e.g., `db`, `infrastructure`, `providers`).

```bash
GET /health/db
GET /health/infrastructure
GET /health/providers
```

**Use Case:** Component-specific monitoring, targeted diagnostics

## Health Status Levels

### Healthy
All health checks pass. System is operating normally.

### Degraded
Some non-critical issues detected. System is operational but may have reduced functionality or performance.

**Examples:**
- FFmpeg not available (video rendering disabled)
- Memory usage above warning threshold
- Slow database response times

### Unhealthy
Critical issues detected. System may not function properly.

**Examples:**
- Database connection failed
- Memory exhausted
- Disk space critically low
- No LLM providers configured

## Configuration

Health check thresholds are configured in `appsettings.json`:

```json
{
  "HealthChecks": {
    "DiskSpaceThresholdGB": 1.0,
    "DiskSpaceCriticalGB": 0.5,
    "MemoryWarningThresholdMB": 1024.0,
    "MemoryCriticalThresholdMB": 2048.0,
    "DatabaseWarningThresholdMs": 500,
    "DatabaseCriticalThresholdMs": 2000,
    "Timeout": "00:00:10",
    "EnableDetailedLogging": false,
    "EnableAutoRecovery": true
  }
}
```

### Configuration Parameters

- **DiskSpaceThresholdGB**: Warning threshold for free disk space (GB)
- **DiskSpaceCriticalGB**: Critical threshold for free disk space (GB)
- **MemoryWarningThresholdMB**: Warning threshold for memory usage (MB)
- **MemoryCriticalThresholdMB**: Critical threshold for memory usage (MB)
- **DatabaseWarningThresholdMs**: Warning threshold for database response time (ms)
- **DatabaseCriticalThresholdMs**: Critical threshold for database response time (ms)
- **Timeout**: Maximum time for health checks to complete
- **EnableDetailedLogging**: Enable verbose health check logging
- **EnableAutoRecovery**: Enable automatic recovery attempts

## Frontend Integration

### Health Dashboard

The system includes a comprehensive health dashboard at `/health-dashboard`:

```tsx
import { HealthDashboard } from '@/components/Health/HealthDashboard';

// The dashboard automatically polls health status every 30 seconds
<HealthDashboard />
```

### Custom Health Monitoring Hook

```tsx
import { useHealthMonitoring } from '@/hooks/useHealthMonitoring';

function MyComponent() {
  const {
    health,
    loading,
    error,
    isMonitoring,
    startMonitoring,
    stopMonitoring,
    refresh,
  } = useHealthMonitoring({
    pollingInterval: 30000,
    enableAutoRetry: true,
    autoStart: true,
  });

  return (
    <div>
      {health && <div>Status: {health.status}</div>}
      <button onClick={refresh}>Refresh</button>
    </div>
  );
}
```

## Monitoring and Alerting

### Prometheus Integration

Health check metrics are exposed for Prometheus scraping:

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'aura-health'
    scrape_interval: 30s
    metrics_path: '/health'
    static_configs:
      - targets: ['localhost:5005']
```

### Alert Rules

Example Prometheus alert rules:

```yaml
groups:
  - name: aura_health
    rules:
      - alert: AuraUnhealthy
        expr: aura_health_status != 1
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "Aura health check failing"
          description: "{{ $labels.check }} health check has been failing for 5 minutes"

      - alert: AuraDegraded
        expr: aura_health_status == 2
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "Aura running in degraded mode"
          description: "{{ $labels.check }} health check is degraded"
```

## Kubernetes Integration

### Liveness and Readiness Probes

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: aura-api
spec:
  template:
    spec:
      containers:
        - name: aura-api
          image: aura/api:latest
          livenessProbe:
            httpGet:
              path: /health/live
              port: 5005
            initialDelaySeconds: 30
            periodSeconds: 10
            timeoutSeconds: 5
            failureThreshold: 3
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 5005
            initialDelaySeconds: 10
            periodSeconds: 5
            timeoutSeconds: 5
            failureThreshold: 3
```

### Startup Probe

For slow-starting applications:

```yaml
startupProbe:
  httpGet:
    path: /health/ready
    port: 5005
  initialDelaySeconds: 0
  periodSeconds: 5
  timeoutSeconds: 5
  failureThreshold: 30  # 30 * 5s = 150s max startup time
```

## Load Balancer Configuration

### Azure Application Gateway

```json
{
  "healthProbe": {
    "protocol": "Http",
    "path": "/health/ready",
    "interval": 30,
    "timeout": 30,
    "unhealthyThreshold": 3,
    "pickHostNameFromBackendHttpSettings": true,
    "minServers": 0,
    "match": {
      "statusCodes": ["200"]
    }
  }
}
```

### AWS ALB

```json
{
  "HealthCheckEnabled": true,
  "HealthCheckPath": "/health/ready",
  "HealthCheckIntervalSeconds": 30,
  "HealthCheckTimeoutSeconds": 5,
  "HealthyThresholdCount": 2,
  "UnhealthyThresholdCount": 3,
  "Matcher": {
    "HttpCode": "200"
  }
}
```

## Troubleshooting

See [Health Check Runbook](./HEALTH_CHECKS_RUNBOOK.md) for detailed troubleshooting procedures.

## Testing Health Checks

### Manual Testing

```bash
# Check liveness
curl http://localhost:5005/health/live

# Check readiness
curl http://localhost:5005/health/ready

# Get full health details
curl http://localhost:5005/health

# Check specific components
curl http://localhost:5005/health/db
curl http://localhost:5005/health/infrastructure
```

### Integration Tests

Health checks are tested in the integration test suite:

```csharp
[Fact]
public async Task HealthCheck_ReturnsHealthy()
{
    var response = await _client.GetAsync("/health/ready");
    response.EnsureSuccessStatusCode();
    
    var health = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();
    Assert.Equal("healthy", health.Status);
}
```

## Best Practices

1. **Set Appropriate Timeouts**: Health checks should complete quickly (< 10s)
2. **Avoid False Positives**: Configure thresholds to prevent unnecessary alerts
3. **Monitor Check Duration**: Slow health checks indicate system issues
4. **Use Tags Effectively**: Tag checks for targeted monitoring
5. **Enable Auto-Recovery**: Let the system attempt automatic recovery
6. **Review Regularly**: Periodically review health check logs and metrics
7. **Test Failure Scenarios**: Regularly test health check behavior during failures

## Related Documentation

- [Health Check Runbook](./HEALTH_CHECKS_RUNBOOK.md)
- [Monitoring Guide](./MONITORING_GUIDE.md)
- [Alerting Configuration](./ALERTING_CONFIG.md)
- [Operational Readiness](./OPERATIONAL_READINESS.md)
