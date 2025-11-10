# Aura Monitoring and Alerting

## Overview

This directory contains comprehensive monitoring and alerting documentation for Aura production operations. The implementation provides full observability, proactive alerting, operational dashboards, and incident response capabilities.

## Quick Start

### For Operators

1. **Configure Notification Channels** in `appsettings.Production.json`:
   ```json
   {
     "Monitoring": {
       "NotificationChannels": {
         "Slack": {
           "Enabled": true,
           "WebhookUrl": "https://hooks.slack.com/...",
           "Channel": "#aura-alerts"
         }
       }
     }
   }
   ```

2. **Start Monitoring Services**:
   ```bash
   cd Aura.Api
   dotnet run
   ```

3. **Access Monitoring Endpoints**:
   - Metrics: `http://localhost:5005/api/monitoring/metrics`
   - Alerts: `http://localhost:5005/api/monitoring/alerts`
   - Health: `http://localhost:5005/api/monitoring/health/synthetic`

### For Developers

1. **Record Business Metrics**:
   ```csharp
   _businessMetrics.RecordJobCompleted("video_generation", true, duration, cost);
   ```

2. **Measure Operations**:
   ```csharp
   using var timer = _metrics.MeasureDuration("operation_name");
   // Your code here
   ```

3. **Track Errors**:
   ```csharp
   _metrics.IncrementCounter("errors", 1, new Dictionary<string, string>
   {
       ["error_type"] = "provider_timeout"
   });
   ```

### For On-Call Engineers

1. **Check Firing Alerts**:
   ```bash
   curl http://localhost:5005/api/monitoring/alerts/firing
   ```

2. **Follow Runbooks** in `/docs/runbooks/`:
   - [API Availability](../runbooks/api-availability.md)
   - [High Error Rate](../runbooks/high-error-rate.md)
   - [High Latency](../runbooks/high-latency.md)

3. **Follow Incident Response** procedures in [INCIDENT_RESPONSE.md](./INCIDENT_RESPONSE.md)

## Documentation Structure

```
docs/monitoring/
├── README.md                          # This file
├── MONITORING_PHILOSOPHY.md           # Monitoring principles and best practices
├── ALERT_CREATION_GUIDE.md            # Guide for creating effective alerts
├── INCIDENT_RESPONSE.md               # Incident response procedures
└── DASHBOARD_CREATION_GUIDE.md        # Dashboard design guide (TBD)

docs/runbooks/
├── api-availability.md                # API availability incident response
├── high-error-rate.md                 # High error rate troubleshooting
├── high-latency.md                    # Performance degradation response
└── ...                                # Additional runbooks

Aura.Api/Monitoring/
├── DashboardDefinitions.json          # Dashboard configurations
├── AlertRules.json                    # Alert rule definitions
└── LogAnalyticsQueries.kql            # KQL queries for Azure Log Analytics

Aura.Core/Monitoring/
├── MetricsCollector.cs                # Core metrics collection
├── BusinessMetricsCollector.cs        # Business KPI tracking
├── SliSloConfiguration.cs             # SLI/SLO definitions
└── AlertingEngine.cs                  # Alert evaluation engine
```

## Key Features

### ✅ Comprehensive Metrics Collection
- Business KPIs (jobs, costs, usage)
- System metrics (latency, errors, throughput)
- Resource utilization (CPU, memory, disk)

### ✅ Service Level Objectives (SLOs)
- API Availability: 99.9%
- API Latency P95: < 2 seconds
- Job Success Rate: 95%
- Error Rate: < 1%

### ✅ Proactive Alerting
- Critical alerts to PagerDuty
- Warning alerts to Slack
- Auto-resolution when metrics return to normal
- Flapping protection (3 consecutive violations)

### ✅ Rich Dashboards
- Operational Dashboard (real-time system health)
- Business Metrics Dashboard (KPIs and trends)
- Performance Dashboard (resource utilization)
- Cost Tracking Dashboard (budget monitoring)

### ✅ Incident Response
- Detailed runbooks for common issues
- Escalation procedures
- Post-mortem templates
- On-call rotation support

## Configuration

### Monitoring Options

All monitoring features can be configured in `appsettings.json`:

```json
{
  "Monitoring": {
    "EnableApplicationInsights": true,
    "EnableCustomMetrics": true,
    "EnableBusinessMetrics": true,
    "EnableSliSloMonitoring": true,
    "EnableAlerting": true,
    "SloEvaluationIntervalSeconds": 60,
    "AlertEvaluationIntervalSeconds": 60,
    "MetricRetentionDays": 90
  }
}
```

### Notification Channels

Four notification channels are supported:

1. **Slack**: Team notifications (warnings)
2. **PagerDuty**: On-call alerts (critical)
3. **Email**: Digest reports (non-urgent)
4. **Webhook**: Custom integrations

See [Configuration Examples](#configuration-examples) below for setup details.

## Metrics Reference

### Business Metrics

| Metric Name | Type | Description |
|------------|------|-------------|
| `jobs.completed` | Counter | Job completions (tagged by status) |
| `jobs.duration_seconds` | Histogram | Job processing time |
| `jobs.cost_usd` | Histogram | Job cost |
| `video.generated` | Counter | Videos created |
| `video.scenes_per_video` | Histogram | Scene count per video |
| `llm.requests` | Counter | LLM API calls |
| `llm.latency_ms` | Histogram | LLM response time |
| `llm.cost_usd` | Histogram | LLM cost |
| `tts.requests` | Counter | TTS generations |
| `tts.cost_usd` | Histogram | TTS cost |
| `image.generated` | Counter | Images created |
| `cache.access` | Counter | Cache hits/misses |
| `cost.usd` | Histogram | Total costs |

### System Metrics

| Metric Name | Type | Description |
|------------|------|-------------|
| `api.requests` | Counter | API request count |
| `api.request_duration_ms` | Histogram | API latency |
| `api.errors.4xx` | Counter | Client errors |
| `api.errors.5xx` | Counter | Server errors |
| `queue.depth` | Gauge | Job queue backlog |
| `provider.healthy` | Gauge | Provider health status |

## API Endpoints

### Metrics

- `GET /api/monitoring/metrics` - Full metrics snapshot
- `GET /api/monitoring/metrics/gauge/{name}` - Specific gauge value
- `GET /api/monitoring/metrics/histogram/{name}` - Histogram statistics

### Alerts

- `GET /api/monitoring/alerts` - All alert states
- `GET /api/monitoring/alerts/firing` - Only firing alerts

### Health

- `GET /api/monitoring/health/synthetic` - Synthetic monitoring endpoint

## Dashboards

Four pre-configured dashboards are available:

### 1. Operational Dashboard
**Purpose**: Real-time system health monitoring  
**Refresh**: 1 minute  
**Widgets**: Availability, latency, error rate, active alerts, queue depth

### 2. Business Metrics Dashboard
**Purpose**: Business KPI tracking  
**Refresh**: 5 minutes  
**Widgets**: Job success rate, video generation, LLM usage, cache hit rate, costs

### 3. Performance Dashboard
**Purpose**: System performance and resources  
**Refresh**: 1 minute  
**Widgets**: CPU/memory usage, latency distribution, database performance, provider health

### 4. Cost Tracking Dashboard
**Purpose**: Cost analysis and budgeting  
**Refresh**: 15 minutes  
**Widgets**: Total cost, cost by category, LLM/TTS/image costs, budget tracking

## Alert Rules

12 pre-configured alert rules covering:

**Critical Alerts** (PagerDuty):
- API availability below 99.9%
- Error rate exceeds 1%
- High exception rate

**Warning Alerts** (Slack):
- API latency P95 > 2s
- Job failure rate > 5%
- Queue depth > 100
- Provider health degraded
- High CPU/memory usage
- Database slow queries
- Daily cost exceeded
- Disk space low

## Runbooks

Detailed runbooks are available for common incidents:

- [API Availability](../runbooks/api-availability.md) - Complete service outage or degradation
- [High Error Rate](../runbooks/high-error-rate.md) - Increased 5xx errors
- [High Latency](../runbooks/high-latency.md) - Slow API responses

Each runbook includes:
- Alert details and symptoms
- Quick diagnostic commands
- Common causes and resolutions
- Escalation procedures
- Post-incident actions

## Configuration Examples

### Application Insights Setup

```json
{
  "Monitoring": {
    "EnableApplicationInsights": true,
    "ApplicationInsightsConnectionString": "InstrumentationKey=...;IngestionEndpoint=https://...;LiveEndpoint=https://..."
  }
}
```

### Slack Integration

```json
{
  "Monitoring": {
    "NotificationChannels": {
      "Slack": {
        "Enabled": true,
        "WebhookUrl": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
        "Channel": "#aura-alerts",
        "MentionUsers": ["@oncall", "@team-lead"]
      }
    }
  }
}
```

### PagerDuty Integration

```json
{
  "Monitoring": {
    "NotificationChannels": {
      "PagerDuty": {
        "Enabled": true,
        "IntegrationKey": "YOUR_INTEGRATION_KEY",
        "ServiceKey": "YOUR_SERVICE_KEY"
      }
    }
  }
}
```

### Email Notifications

```json
{
  "Monitoring": {
    "NotificationChannels": {
      "Email": {
        "Enabled": true,
        "SmtpServer": "smtp.sendgrid.net",
        "SmtpPort": 587,
        "FromAddress": "alerts@aura.studio",
        "ToAddresses": ["oncall@aura.studio", "team@aura.studio"],
        "Username": "apikey",
        "Password": "YOUR_SENDGRID_API_KEY"
      }
    }
  }
}
```

## Testing

### Unit Tests

```bash
cd Aura.Tests
dotnet test --filter "FullyQualifiedName~Monitoring"
```

### Integration Tests

```bash
# Test metrics collection
curl http://localhost:5005/api/monitoring/metrics

# Test alert firing
# (Inject test failures and verify alerts)

# Test notifications
# (Verify Slack/PagerDuty receives alerts)
```

### Synthetic Monitoring

Set up external monitoring (e.g., Pingdom, UptimeRobot) to check:

```
GET https://api.aura.studio/api/monitoring/health/synthetic
Expect: 200 OK with {"status": "healthy"}
```

## Troubleshooting

### Metrics Not Collecting

1. Check monitoring is enabled: `"EnableCustomMetrics": true`
2. Verify services are running: Check logs for `MetricsExporterService` and `AlertEvaluationService`
3. Check for errors: `tail -f logs/errors-*.log`

### Alerts Not Firing

1. Verify alerting enabled: `"EnableAlerting": true`
2. Check SLO evaluation: `curl /api/monitoring/alerts`
3. Verify thresholds: Metrics may not be violating SLOs
4. Check evaluation interval: Default is 60 seconds

### Notifications Not Sending

1. Verify channel configuration (webhook URLs, API keys)
2. Check network connectivity to external services
3. Review logs for notification errors
4. Test webhook URLs manually with curl

## Best Practices

### ✅ DO

- Monitor user-impacting metrics (availability, latency)
- Create actionable alerts with clear runbooks
- Tune alert thresholds to minimize false positives
- Review and update runbooks after incidents
- Conduct regular fire drills

### ❌ DON'T

- Alert on everything (causes alert fatigue)
- Set and forget thresholds (they drift over time)
- Skip post-mortems (miss learning opportunities)
- Page for non-urgent issues (save PagerDuty for real emergencies)

## Further Reading

- [Monitoring Philosophy](./MONITORING_PHILOSOPHY.md) - Core principles and best practices
- [Alert Creation Guide](./ALERT_CREATION_GUIDE.md) - How to create effective alerts
- [Incident Response](./INCIDENT_RESPONSE.md) - Detailed incident procedures
- [Implementation Summary](../../MONITORING_ALERTING_IMPLEMENTATION.md) - Technical details

## Support

For questions or issues:

1. Check runbooks in `/docs/runbooks/`
2. Review documentation in `/docs/monitoring/`
3. Contact platform team: platform@aura.studio
4. File an issue: GitHub Issues

## Changelog

### 2025-11-10 - Initial Release (PR #14)

- ✅ Core metrics collection infrastructure
- ✅ Business KPI tracking
- ✅ SLI/SLO monitoring
- ✅ Alert evaluation engine
- ✅ Four operational dashboards
- ✅ 12 pre-configured alert rules
- ✅ Notification channels (Slack, PagerDuty, Email, Webhook)
- ✅ Application Insights integration
- ✅ Comprehensive documentation
- ✅ Runbooks for common incidents
- ✅ Unit and integration tests

---

**Maintained by**: Platform Engineering Team  
**Last Updated**: 2025-11-10  
**Version**: 1.0.0
