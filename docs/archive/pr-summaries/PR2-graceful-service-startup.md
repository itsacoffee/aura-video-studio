# PR 2: Add Graceful Service Startup with Missing Table Handling

## Priority: HIGH (Start after PR 1 merges)
## Can run in parallel with: PR 3

## Problem
Services crash immediately on startup when tables don't exist, causing cascading failures and poor user experience.

## Solution

### Step 1: Create Service Health Check Interface

File: `Aura.Core/Services/IHealthCheckableService.cs`

Create a new interface for services that can perform health checks:

```csharp
namespace Aura.Core.Services;

/// <summary>
/// Interface for services that can perform health checks
/// </summary>
public interface IHealthCheckableService
{
    /// <summary>
    /// Checks if the service is healthy and can operate
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public record HealthCheckResult(
    bool IsHealthy,
    string? Message = null,
    Exception? Exception = null);
```

### Step 2: Update BackgroundJobProcessorService

File: `Aura.Api/HostedServices/BackgroundJobProcessorService.cs`

Modify the ExecuteAsync method to:
1. Wait 5 seconds for application startup to complete
2. Check database schema before starting processing
3. Exit gracefully if schema is invalid instead of crashing
4. Log clear error messages indicating what's wrong and how to fix

Add a new method `CheckDatabaseSchemaAsync` that:
- Uses IServiceScopeFactory to get AuraDbContext
- Checks if QueueConfiguration table exists and has data
- If no data exists, attempts to create default configuration with these values:
  - Id = "default"
  - IsEnabled = true
  - MaxConcurrentJobs = 2
  - PollingIntervalSeconds = 5
  - RetryBaseDelaySeconds = 60
  - RetryMaxDelaySeconds = 3600
  - EnableNotifications = true
  - PauseOnBattery = false
  - CpuThrottleThreshold = 90.0
  - MemoryThrottleThreshold = 90.0
  - JobHistoryRetentionDays = 30
  - FailedJobRetentionDays = 90
  - UpdatedAt = DateTime.UtcNow
- Returns HealthCheckResult with success or failure details
- Catches SqliteException when table doesn't exist and returns helpful error message

### Step 3: Update AnalyticsMaintenanceService

File: `Aura.Api/HostedServices/AnalyticsMaintenanceService.cs`

Modify the ExecuteAsync method to:
1. Wait 10 seconds for application startup
2. Verify schema before starting maintenance
3. Exit gracefully with warning if schema is invalid

Add a new method `CheckDatabaseSchemaAsync` that:
- Checks if AnalyticsRetentionSettings table exists
- If no data exists, creates default settings with these values:
  - Id = "default"
  - IsEnabled = true
  - AutoCleanupEnabled = true
  - UsageStatisticsRetentionDays = 90
  - PerformanceMetricsRetentionDays = 30
  - CostTrackingRetentionDays = 365
  - AggregateOldData = true
  - AggregationThresholdDays = 30
  - MaxDatabaseSizeMB = 500
  - CleanupHourUtc = 2
  - TrackSuccessOnly = false
  - CollectHardwareMetrics = true
  - CreatedAt = DateTime.UtcNow
  - UpdatedAt = DateTime.UtcNow
- Returns HealthCheckResult with success or failure details
- Catches SqliteException when table doesn't exist

### Step 4: Update SettingsService with Graceful Fallback

File: `Aura.Core/Services/Settings/SettingsService.cs`

Update the `GetSettingsAsync` method to:
1. Attempt to load settings from database
2. If Settings table doesn't exist, log warning and return in-memory defaults
3. If no settings found, create and save default settings

Add a new method `CreateDefaultSettingsInMemory` that returns UserSettings with reasonable defaults without attempting database write.

Add a new method `CreateDefaultSettingsAsync` that:
- Creates in-memory defaults
- Attempts to save to database
- If save fails, logs warning and returns in-memory defaults anyway
- Never throws exceptions

### Step 5: Add Startup Health Check Report

File: `Aura.Api/Program.cs`

Add code after `var app = builder.Build();` and before `app.Run();` to:
1. Register an ApplicationStarted callback using app.Lifetime.ApplicationStarted
2. Create a service scope
3. Get ILogger<Program> and AuraDbContext from the scope
4. Check each critical table:
   - Settings
   - QueueConfiguration
   - AnalyticsRetentionSettings
5. Log success (✓) or failure (✗) for each table check
6. If any health issues found, log warning with count and suggestion to run migrations
7. If all checks pass, log success message

The health check should catch exceptions and log them without crashing the application.

### Step 6: Create Unit Tests

File: `Aura.Tests/Api/HostedServices/ServiceHealthCheckTests.cs`

Create tests that verify:
1. BackgroundJobProcessorService exits gracefully when table is missing
2. AnalyticsMaintenanceService exits gracefully when table is missing
3. SettingsService returns in-memory defaults when table is missing
4. No exceptions are thrown during graceful degradation

## Acceptance Criteria

- [ ] Services exit gracefully when tables are missing instead of crashing
- [ ] Clear log messages indicate what's wrong and how to fix it
- [ ] Default data is created automatically when possible
- [ ] In-memory fallbacks work when database is unavailable
- [ ] Startup health check report shows all table statuses
- [ ] Application remains stable even with missing tables
- [ ] Unit tests verify graceful degradation
- [ ] No unhandled exceptions during startup
- [ ] Services log helpful error messages with fix instructions

## Build Enforcement
- All error handling must be explicit and complete
- No placeholder exception handlers
- All logging messages must be clear and actionable
- Unit tests must cover all failure scenarios