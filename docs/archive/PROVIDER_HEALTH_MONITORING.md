# Provider Health Monitoring System

## Overview

The Provider Health Monitoring system automatically detects and avoids failing providers, ensuring reliable video generation through intelligent provider selection and automatic failover.

## Features

### 1. Automatic Health Monitoring
- Background health checks every 5 minutes
- 5-second timeout per health check
- Tracks consecutive failures and success rates
- Rolling statistics (last 100 checks) for accurate metrics

### 2. Smart Provider Selection
- Composite scoring algorithm:
  - Success rate: 50% weight
  - Response time: 30% weight (faster is better)
  - Tier preference: 20% weight
- Automatic exclusion of providers with 3+ consecutive failures
- Fallback to guaranteed providers when all others fail

### 3. Provider Health Dashboard
- Real-time status display with color-coded badges
- Success rates and response times
- Filter by provider type (LLM/TTS/Image)
- Manual health check triggers
- Auto-refresh every 10 seconds
- Expandable error details

### 4. Health Notifications
- Polls health status every 30 seconds
- Alerts on provider status changes:
  - Provider goes offline (red)
  - Provider recovers (green)
  - Provider degrades (yellow)
- 5-minute cooldown per provider to avoid spam
- Persistent state across sessions
- Mute/unmute per provider

## Architecture

### Core Services

#### ProviderHealthMonitor
Located: `Aura.Core/Services/Health/ProviderHealthMonitor.cs`

Responsibilities:
- Store health metrics per provider
- Execute health checks with timeout
- Track consecutive failures
- Calculate rolling statistics

Key Methods:
- `CheckProviderHealthAsync(name, healthCheckFunc)` - Run health check
- `GetProviderHealth(name)` - Get cached metrics
- `GetAllProviderHealth()` - Get all provider metrics
- `RunPeriodicHealthChecksAsync()` - Background monitoring loop

#### SmartProviderSelector
Located: `Aura.Core/Services/Providers/SmartProviderSelector.cs`

Responsibilities:
- Select best provider based on health metrics
- Filter by tier preference (Free/Balanced/Pro)
- Calculate composite scores
- Provide fallback providers

Key Methods:
- `SelectBestLlmProviderAsync(tier)` - Select LLM provider
- `SelectBestTtsProviderAsync(tier)` - Select TTS provider
- `SelectBestImageProviderAsync(tier)` - Select image provider
- `RecordProviderUsage(name, success, error)` - Track usage

#### ProviderHealthChecks
Located: `Aura.Core/Services/Health/ProviderHealthChecks.cs`

Static health check functions for each provider type:
- `CreateLlmHealthCheck()` - Generic LLM test
- `CreateTtsHealthCheck()` - TTS synthesis test
- `CreateOllamaHealthCheck()` - Ollama /api/tags endpoint
- `CreateOpenAiHealthCheck()` - OpenAI /v1/models endpoint
- `CreateAzureOpenAiHealthCheck()` - Azure deployments endpoint
- `CreateGeminiHealthCheck()` - Gemini models endpoint
- `CreateStableDiffusionHealthCheck()` - SD /sdapi/v1/sd-models endpoint

### API Endpoints

#### HealthController
Located: `Aura.Api/Controllers/HealthController.cs`

Endpoints:
- `GET /api/health/providers` - Get all provider health metrics
- `GET /api/health/providers/{name}` - Get specific provider metrics
- `POST /api/health/providers/{name}/check` - Trigger immediate check
- `POST /api/health/providers/check-all` - Check all providers
- `GET /api/health/providers/summary` - Get overall health summary

### UI Components

#### ProviderHealthDashboard
Located: `Aura.Web/src/pages/Health/ProviderHealthDashboard.tsx`

Features:
- Grid layout of provider cards
- Real-time status badges
- Success rate with color coding
- Response time metrics
- Filter by provider type
- Manual refresh button
- Auto-refresh toggle
- Expandable error details
- Test connection per provider

#### HealthNotificationService
Located: `Aura.Web/src/services/HealthNotificationService.ts`

Features:
- Background polling (30 seconds)
- State change detection
- Toast notifications
- 5-minute cooldown
- localStorage persistence
- Mute/unmute providers

## Usage

### Accessing the Dashboard

1. Start the Aura application
2. Navigate to "Provider Health" in the sidebar
3. View real-time status of all providers
4. Enable auto-refresh for continuous monitoring
5. Click "Test Connection" to manually verify a provider
6. Click "Refresh All" to trigger immediate health checks

### Understanding Provider Status

**Healthy (Green)**
- All recent checks passing
- Less than 3 consecutive failures
- Provider is available for selection

**Degraded (Yellow)**
- Some recent failures
- 1-2 consecutive failures
- Still operational but not optimal
- May be excluded from selection

**Offline (Red)**
- 3 or more consecutive failures
- Provider is excluded from automatic selection
- Requires manual intervention or recovery

### Health Metrics Explained

**Success Rate**
- Percentage of successful health checks
- Based on last 100 checks
- Green: >95%, Yellow: 80-95%, Red: <80%

**Response Time**
- Time taken to complete health check
- Displayed in milliseconds
- Lower is better

**Consecutive Failures**
- Number of failures in a row
- Resets to 0 on first success
- Provider excluded at 3+

**Average Response Time**
- Rolling average of last 100 checks
- Used for provider scoring
- Helps identify slow providers

## Configuration

### Background Monitoring

Health checks run automatically every 5 minutes. This is configured in:
- `Aura.Api/Program.cs` - Background task setup
- `ProviderHealthMonitor.RunPeriodicHealthChecksAsync()` - 5-minute interval

To modify the interval, change the `TimeSpan.FromMinutes(5)` value.

### Health Check Timeout

All health checks timeout after 5 seconds. This is configured in:
- `ProviderHealthMonitor.CheckProviderHealthAsync()` - Creates 5-second timeout

To modify the timeout, change the `TimeSpan.FromSeconds(5)` value.

### Provider Selection Scoring

The composite score weights can be adjusted in:
- `SmartProviderSelector.CalculateProviderScore()`

Current weights:
- Success rate: 50%
- Response time: 30%
- Tier preference: 20%

### Notification Polling

Notifications poll every 30 seconds with a 5-minute cooldown. This is configured in:
- `HealthNotificationService.ts`
  - `POLL_INTERVAL_MS = 30 * 1000` (30 seconds)
  - `NOTIFICATION_COOLDOWN_MS = 5 * 60 * 1000` (5 minutes)

## Testing

### Unit Tests

Run health monitoring tests:
```bash
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~ProviderHealth"
dotnet test Aura.Tests/Aura.Tests.csproj --filter "FullyQualifiedName~SmartProviderSelector"
```

Test files:
- `Aura.Tests/ProviderHealthMonitorTests.cs` - 11 tests
- `Aura.Tests/SmartProviderSelectorTests.cs` - 8 tests

### Manual Testing

1. **Test Provider Failure Detection**
   - Stop Ollama service (if installed)
   - Wait up to 5 minutes
   - Verify Ollama shows red "Offline" status
   - Check error message displays correctly

2. **Test Automatic Failover**
   - With Ollama offline, start a video generation
   - Verify system selects alternative provider
   - Check logs show provider selection reason

3. **Test Provider Recovery**
   - Restart Ollama service
   - Click "Test Connection" on Ollama card
   - Verify status changes to green
   - Generate another video
   - Verify Ollama is used if it has better metrics

4. **Test API Key Expiration**
   - Use invalid OpenAI API key
   - Wait for health check
   - Verify authentication error shown
   - Verify error message is clear

5. **Test Notifications**
   - Keep dashboard open
   - Stop a provider service
   - Verify toast notification appears
   - Restart provider
   - Verify recovery notification appears

## Troubleshooting

### Health Checks Not Running

Check that background monitoring started:
1. Look for log message: "Starting provider health monitoring..."
2. Verify `ProviderHealthMonitor` is registered as singleton
3. Check application lifetime events are firing

### Provider Always Shows Offline

1. Verify provider service is actually running
2. Check health check function is registered
3. Review error message in dashboard for details
4. Test provider manually using its API

### Notifications Not Appearing

1. Check browser console for errors
2. Verify API endpoint `/api/health/providers` is accessible
3. Check notification service is started
4. Verify cooldown period hasn't suppressed notification
5. Check if provider is muted in localStorage

### Incorrect Provider Selected

1. Review health metrics in dashboard
2. Check provider tier filtering (Free/Balanced/Pro)
3. Verify scoring weights are appropriate
4. Check consecutive failure counts
5. Review logs for selection reasoning

## Best Practices

1. **Monitor the Dashboard Regularly**
   - Check for degraded providers
   - Review error messages
   - Identify patterns in failures

2. **Configure API Keys Properly**
   - Ensure API keys are valid
   - Monitor expiration dates
   - Test after configuration changes

3. **Keep Services Running**
   - Ensure local services (Ollama, Stable Diffusion) are started
   - Configure auto-start if possible
   - Monitor service health outside of Aura

4. **Review Logs**
   - Check API logs for health check results
   - Look for patterns in failures
   - Use logs to debug provider issues

5. **Test After Changes**
   - Test health checks after provider configuration changes
   - Verify fallback behavior works
   - Ensure notifications appear correctly

## Future Enhancements

Potential improvements to consider:

1. **Configurable Health Check Intervals**
   - Per-provider intervals
   - Adaptive intervals based on failure rate
   - Backoff on repeated failures

2. **Historical Data Visualization**
   - Charts of success rate over time
   - Response time trends
   - Failure pattern analysis

3. **Advanced Alerting**
   - Email notifications
   - Webhook integrations
   - Slack/Discord alerts

4. **Predictive Failure Detection**
   - ML-based failure prediction
   - Anomaly detection in metrics
   - Proactive provider switching

5. **Load Balancing**
   - Round-robin among healthy providers
   - Weighted distribution based on performance
   - Request rate limiting per provider

6. **Health Check Customization**
   - User-defined health checks
   - Configurable timeout per provider
   - Custom success criteria

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review logs in `/api/logs`
3. Open an issue on GitHub with:
   - Health metrics from dashboard
   - Error messages
   - Steps to reproduce
   - Log excerpts
