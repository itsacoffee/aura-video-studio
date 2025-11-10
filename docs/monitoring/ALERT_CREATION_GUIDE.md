# Alert Creation Guide

## Overview

This guide provides step-by-step instructions for creating effective alerts in Aura. Following these guidelines ensures alerts are actionable, minimize false positives, and enable rapid incident response.

## Table of Contents

- [Alert Design Principles](#alert-design-principles)
- [Alert Lifecycle](#alert-lifecycle)
- [Creating Alerts](#creating-alerts)
- [Alert Types](#alert-types)
- [Threshold Tuning](#threshold-tuning)
- [Testing Alerts](#testing-alerts)
- [Alert Best Practices](#alert-best-practices)

## Alert Design Principles

### 1. Every Alert Must Be Actionable

**Question to ask**: "If this alert fires at 3am, what specific action should the on-call engineer take?"

**Good Example**:
```json
{
  "name": "API Availability Below SLO",
  "description": "API success rate dropped below 99.9%",
  "action": "Check error logs, identify failing endpoints, escalate if needed",
  "runbook": "https://docs.aura.studio/runbooks/api-availability"
}
```

**Bad Example**:
```json
{
  "name": "High Request Count",
  "description": "API is receiving many requests",
  "action": "???" // Not actionable - high traffic might be normal
}
```

### 2. Alert on Symptoms, Not Causes

Alert on user-visible problems, not the underlying causes.

**Symptoms** (alert on these):
- ✅ API returning 500 errors
- ✅ Job success rate below SLO
- ✅ P95 latency exceeding target

**Causes** (don't directly alert, but monitor):
- ❌ CPU usage at 70%
- ❌ Memory at 1.5GB
- ❌ Database connection pool 80% full

**Why?** Causes don't always lead to user impact. Alert on symptoms, then investigate causes.

### 3. Set Appropriate Severity

**Critical** (Page immediately):
- Service outage
- Data loss
- Security breach
- SLO breach with user impact

**Warning** (Notify Slack):
- Degraded performance
- Approaching SLO threshold
- Non-critical failures

**Info** (Dashboard only):
- Usage trends
- Capacity planning metrics
- Informational events

## Alert Lifecycle

```
1. Define → 2. Implement → 3. Test → 4. Deploy → 5. Monitor → 6. Tune → 7. Review
     ↑                                                                      ↓
     ←──────────────────────────────────────────────────────────────────────
```

### 1. Define
- Identify the problem to detect
- Define the metric and threshold
- Determine severity and notification channels
- Write the runbook

### 2. Implement
- Add metric collection code
- Create alert rule configuration
- Set up notification channels

### 3. Test
- Inject failures to verify alert fires
- Confirm notifications are sent
- Validate runbook accuracy

### 4. Deploy
- Roll out to staging first
- Monitor for false positives
- Adjust thresholds if needed

### 5. Monitor
- Track alert firing frequency
- Measure false positive rate
- Gather feedback from on-call

### 6. Tune
- Adjust thresholds based on data
- Modify evaluation windows
- Update runbooks based on incidents

### 7. Review
- Monthly review of all alerts
- Retire alerts that never fire or are not actionable
- Update based on system changes

## Creating Alerts

### Step 1: Identify the Metric

**Business Metrics**:
- `jobs.completed` (with `status` tag)
- `video.generated`
- `llm.requests`
- `cost.usd`

**System Metrics**:
- `api.requests` (with `status_code` tag)
- `api.request_duration_ms`
- `queue.depth`
- `provider.healthy`

**Performance Metrics**:
- CPU usage
- Memory usage
- Database query duration

### Step 2: Define the SLO

```csharp
var slo = new ServiceLevelObjective
{
    Name = "job_success_rate_target",
    Description = "Jobs should succeed 95% of the time",
    SliName = "job_success_rate", // References an SLI
    Operator = SloOperator.GreaterThanOrEqual,
    TargetValue = 95.0,
    EvaluationWindow = TimeSpan.FromMinutes(15),
    Severity = "warning",
    NotificationChannels = new List<string> { "slack" }
};
```

### Step 3: Add Metric Collection

```csharp
// In your service code
_businessMetrics.RecordJobCompleted(
    jobType: "video_generation",
    success: true,
    duration: TimeSpan.FromMinutes(5),
    cost: 0.25m
);
```

### Step 4: Write the Runbook

Create a runbook at `docs/runbooks/<alert-name>.md`:

```markdown
# Job Failure Rate High

## Alert Details
- **Severity**: Warning
- **SLO**: 95% job success rate
- **Evaluation**: 15-minute window

## Symptoms
- Job failure rate exceeds 5%
- Users may experience failed video generations

## Investigation Steps
1. Check recent error logs: `/api/logs?level=error`
2. Review failing jobs: `/api/jobs?status=failed`
3. Check provider health: `/api/health/providers`
4. Review recent deployments

## Common Causes
- Provider API outages
- Invalid user inputs
- Insufficient resources
- Code bugs in new deployment

## Resolution Steps
1. If provider outage: Switch to backup provider
2. If resource issue: Scale up resources
3. If recent deployment: Rollback to previous version
4. If bug: Create hotfix and deploy

## Escalation
- After 30 minutes: Escalate to senior engineer
- After 1 hour: Escalate to engineering manager
```

### Step 5: Configure Notification Channels

In `appsettings.json`:

```json
{
  "Monitoring": {
    "NotificationChannels": {
      "Slack": {
        "Enabled": true,
        "WebhookUrl": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
        "Channel": "#aura-alerts"
      },
      "PagerDuty": {
        "Enabled": true,
        "IntegrationKey": "YOUR_INTEGRATION_KEY"
      }
    }
  }
}
```

### Step 6: Test the Alert

```bash
# Inject failures to test alert
curl -X POST http://localhost:5005/api/test/inject-failures \
  -H "Content-Type: application/json" \
  -d '{"failureType": "job_failure", "count": 10}'

# Check if alert fired
curl http://localhost:5005/api/monitoring/alerts/firing
```

## Alert Types

### 1. Availability Alerts

**Purpose**: Detect when service is unavailable

**Example**:
```json
{
  "name": "API Availability Below SLO",
  "query": "requests | summarize AvailabilityRate = (count() - countif(resultCode >= 500)) * 100.0 / count()",
  "threshold": 99.9,
  "operator": "LessThan",
  "severity": "Critical"
}
```

### 2. Latency Alerts

**Purpose**: Detect slow responses

**Example**:
```json
{
  "name": "API Latency P95 Exceeded",
  "query": "requests | summarize P95Latency = percentile(duration, 95)",
  "threshold": 2000,
  "operator": "GreaterThan",
  "severity": "Warning"
}
```

### 3. Error Rate Alerts

**Purpose**: Detect increased errors

**Example**:
```json
{
  "name": "High Error Rate",
  "query": "requests | summarize ErrorRate = countif(resultCode >= 500) * 100.0 / count()",
  "threshold": 1.0,
  "operator": "GreaterThan",
  "severity": "Critical"
}
```

### 4. Saturation Alerts

**Purpose**: Detect resource exhaustion

**Example**:
```json
{
  "name": "Queue Depth Critical",
  "query": "customMetrics | where name == 'queue.depth' | summarize AvgDepth = avg(value)",
  "threshold": 100,
  "operator": "GreaterThan",
  "severity": "Warning"
}
```

### 5. Business Metric Alerts

**Purpose**: Detect business KPI deviations

**Example**:
```json
{
  "name": "Daily Revenue Below Target",
  "query": "customMetrics | where name == 'revenue.usd' | summarize DailyRevenue = sum(value)",
  "threshold": 1000,
  "operator": "LessThan",
  "severity": "Warning"
}
```

## Threshold Tuning

### Step 1: Collect Baseline Data

Run the system for at least 1 week to establish normal behavior:

```bash
# Get histogram stats for a metric
curl http://localhost:5005/api/monitoring/metrics/histogram/api.request_duration_ms
```

### Step 2: Calculate Percentiles

```json
{
  "count": 10000,
  "min": 10,
  "max": 5000,
  "mean": 150,
  "p50": 100,
  "p90": 300,
  "p95": 500,
  "p99": 1200
}
```

### Step 3: Set Threshold Above Normal

**Rule of Thumb**: Set threshold at P95 + 50% margin

```
P95 = 500ms
Threshold = 500ms * 1.5 = 750ms
```

### Step 4: Monitor False Positives

Track alert firing frequency:
- **Too many alerts** (> 5/day): Threshold too sensitive
- **Never alerts**: Threshold too lenient
- **Sweet spot**: 1-2 alerts/week that are genuine issues

### Step 5: Iterate

Review monthly and adjust thresholds based on:
- System performance changes
- Business growth
- User expectations

## Testing Alerts

### Unit Tests

```csharp
[Fact]
public async Task AlertingEngine_ShouldFireAlert_WhenSloViolated()
{
    // Arrange
    var metrics = new MetricsCollector(_logger);
    var sloConfig = new SliSloConfiguration
    {
        Indicators = new List<ServiceLevelIndicator>
        {
            new ServiceLevelIndicator
            {
                Name = "test_metric",
                MetricName = "test_metric",
                Aggregation = SliAggregation.Average
            }
        },
        Objectives = new List<ServiceLevelObjective>
        {
            new ServiceLevelObjective
            {
                Name = "test_slo",
                SliName = "test_metric",
                Operator = SloOperator.GreaterThan,
                TargetValue = 100
            }
        }
    };
    var alerting = new AlertingEngine(metrics, sloConfig, _logger);

    // Act: Record value that violates SLO
    metrics.RecordGauge("test_metric", 50);
    var alerts = await alerting.EvaluateAsync();

    // Assert
    Assert.NotEmpty(alerts);
    Assert.Equal("test_slo", alerts[0].Name);
}
```

### Integration Tests

```bash
#!/bin/bash
# test-alert-integration.sh

echo "Testing alert pipeline..."

# 1. Inject metric that violates SLO
curl -X POST http://localhost:5005/api/test/inject-metric \
  -d '{"name": "api.errors", "value": 100}'

# 2. Wait for alert evaluation (60 seconds)
sleep 65

# 3. Check if alert fired
FIRING_ALERTS=$(curl -s http://localhost:5005/api/monitoring/alerts/firing | jq '. | length')

if [ "$FIRING_ALERTS" -gt 0 ]; then
  echo "✅ Alert fired successfully"
else
  echo "❌ Alert did not fire"
  exit 1
fi

# 4. Check if notification sent (check Slack, email, etc.)
echo "Verify notification was sent to configured channels"
```

### Synthetic Monitoring

Set up external health checks:

```yaml
# synthetic-checks.yml
checks:
  - name: API Health Check
    url: https://api.aura.studio/api/monitoring/health/synthetic
    interval: 5m
    timeout: 10s
    expect:
      status: 200
      body_contains: '"status":"healthy"'
```

## Alert Best Practices

### ✅ DO

1. **Include Context in Alerts**
   ```
   ALERT: API Availability Below SLO
   Current: 98.5% | Target: 99.9%
   Impact: Users experiencing errors
   Runbook: https://docs.aura.studio/runbooks/api-availability
   Dashboard: https://portal.azure.com/...
   ```

2. **Use Flapping Protection**
   - Require 3 consecutive violations before firing
   - Prevents alerts from firing/clearing repeatedly

3. **Set Evaluation Windows**
   - Use 5-minute windows for latency/errors
   - Use 15-minute windows for business metrics
   - Balance between quick detection and false positives

4. **Group Related Alerts**
   - One incident shouldn't cause 10 alerts
   - Use dependencies: "Don't alert on database if API is already alerting"

5. **Review and Retire**
   - Monthly review of all alerts
   - Retire alerts that never fire
   - Update runbooks based on incident learnings

### ❌ DON'T

1. **Don't Alert on Everything**
   - Not every metric needs an alert
   - Focus on user-impacting issues

2. **Don't Use Fixed Thresholds for Dynamic Systems**
   - Traffic patterns change over time
   - Use percentiles and rolling windows

3. **Don't Forget to Test**
   - Untested alerts won't work when you need them
   - Regularly inject failures to verify alerting

4. **Don't Set and Forget**
   - Thresholds drift over time
   - Review and tune regularly

5. **Don't Page for Non-Urgent Issues**
   - Save PagerDuty for true emergencies
   - Use Slack/email for warnings

## Alert Template

Use this template for consistency:

```json
{
  "name": "<Short descriptive name>",
  "description": "<What problem does this detect>",
  "severity": "Critical|Warning|Info",
  "evaluationFrequency": "PT5M",
  "windowSize": "PT5M",
  "query": "<KQL or metric query>",
  "threshold": 0,
  "operator": "GreaterThan|LessThan|Equal",
  "triggerType": "Total|Consecutive",
  "actionGroups": ["slack", "pagerduty", "email"],
  "runbook": "https://docs.aura.studio/runbooks/<alert-name>",
  "tags": {
    "component": "api|job|provider",
    "impact": "high|medium|low",
    "auto_resolve": "true|false"
  }
}
```

## Troubleshooting Alerts

### Alert Not Firing

1. **Check metric is being collected**:
   ```bash
   curl http://localhost:5005/api/monitoring/metrics
   ```

2. **Verify SLO configuration**:
   ```bash
   curl http://localhost:5005/api/monitoring/alerts
   ```

3. **Check alert evaluation logs**:
   ```bash
   tail -f logs/aura-api-*.log | grep "Alert"
   ```

### Too Many False Positives

1. **Increase threshold**
2. **Extend evaluation window**
3. **Add flapping protection**
4. **Review baseline data**

### Alerts Too Slow

1. **Reduce evaluation frequency** (e.g., 1min instead of 5min)
2. **Use smaller time windows**
3. **Alert on rate of change, not absolute values**

## Further Reading

- [Monitoring Philosophy](./MONITORING_PHILOSOPHY.md)
- [Incident Response Procedures](./INCIDENT_RESPONSE.md)
- [Runbook Template](./RUNBOOK_TEMPLATE.md)
- [Dashboard Creation Guide](./DASHBOARD_CREATION_GUIDE.md)

## Summary

Creating effective alerts requires:
1. **Actionability**: Clear action required
2. **Context**: Enough information to diagnose
3. **Appropriate Severity**: Right notification channel
4. **Testing**: Verify alerts work
5. **Continuous Improvement**: Review and tune regularly

Remember: **The best alert is one that fires rarely, but when it does, it's always correct and actionable.**
