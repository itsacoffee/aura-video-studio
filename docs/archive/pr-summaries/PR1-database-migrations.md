# PR 1: Add Missing Database Migrations for Settings and Queue Configuration

## Priority: CRITICAL (Must complete first)
## Can run in parallel with: None (blocks PR 2 and PR 3)

## Problem
The application is attempting to query database tables that don't exist:
- `Settings` table (SettingsEntity)
- `QueueConfiguration` table (QueueConfigurationEntity)
- `AnalyticsRetentionSettings` table (AnalyticsRetentionSettingsEntity)

This causes SQLite errors on startup preventing the application from running.

## Root Cause
Entity classes exist in the codebase but corresponding EF Core migrations were never created or are incomplete.

## Solution

### Step 1: Create Migration for Settings Table

Create a new migration file in `Aura.Core/Migrations/` directory.

The migration should:
- Create a table named "Settings"
- Include columns: Id (TEXT, primary key), SettingsJson (TEXT), IsEncrypted (INTEGER/bool), Version (INTEGER), CreatedAt (TEXT), UpdatedAt (TEXT), CreatedBy (TEXT, nullable), ModifiedBy (TEXT, nullable)
- Create an index on UpdatedAt column

### Step 2: Create Migration for Queue Configuration

Create a new migration file in `Aura.Core/Migrations/` directory.

The migration should:
- Create a table named "QueueConfiguration"
- Include columns with the following defaults:
  - Id (TEXT, primary key)
  - IsEnabled (INTEGER/bool, default: true)
  - MaxConcurrentJobs (INTEGER, default: 2)
  - PollingIntervalSeconds (INTEGER, default: 5)
  - RetryBaseDelaySeconds (INTEGER, default: 60)
  - RetryMaxDelaySeconds (INTEGER, default: 3600)
  - EnableNotifications (INTEGER/bool, default: true)
  - PauseOnBattery (INTEGER/bool, default: false)
  - CpuThrottleThreshold (REAL, default: 90.0)
  - MemoryThrottleThreshold (REAL, default: 90.0)
  - JobHistoryRetentionDays (INTEGER, default: 30)
  - FailedJobRetentionDays (INTEGER, default: 90)
  - UpdatedAt (TEXT)
- Insert a default configuration row with Id="default"
- Create an index on UpdatedAt column

### Step 3: Create Migration for Analytics Retention Settings

Create a new migration file in `Aura.Core/Migrations/` directory.

The migration should:
- Create a table named "AnalyticsRetentionSettings"
- Include columns with the following defaults:
  - Id (TEXT, primary key)
  - IsEnabled (INTEGER/bool, default: true)
  - AutoCleanupEnabled (INTEGER/bool, default: true)
  - UsageStatisticsRetentionDays (INTEGER, default: 90)
  - PerformanceMetricsRetentionDays (INTEGER, default: 30)
  - CostTrackingRetentionDays (INTEGER, default: 365)
  - AggregateOldData (INTEGER/bool, default: true)
  - AggregationThresholdDays (INTEGER, default: 30)
  - MaxDatabaseSizeMB (INTEGER, default: 500)
  - CleanupHourUtc (INTEGER, default: 2)
  - TrackSuccessOnly (INTEGER,bool, default: false)
  - CollectHardwareMetrics (INTEGER,bool, default: true)
  - CreatedAt (TEXT)
  - UpdatedAt (TEXT)
  - CreatedBy (TEXT, nullable)
  - ModifiedBy (TEXT, nullable)
- Insert a default settings row with Id="default"
- Create indexes on IsEnabled and UpdatedAt columns

### Step 4: Update AuraDbContext

File: `Aura.Core/Data/AuraDbContext.cs`

Add these DbSet properties if they don't already exist:

```csharp
public DbSet<SettingsEntity> Settings { get; set; } = null!;
public DbSet<QueueConfigurationEntity> QueueConfiguration { get; set; } = null!;
public DbSet<AnalyticsRetentionSettingsEntity> AnalyticsRetentionSettings { get; set; } = null!;
```

### Step 5: Create Entity Configuration for Settings

File: `Aura.Core/Data/EntityConfigurations/SettingsEntityConfiguration.cs`

Create a new file with IEntityTypeConfiguration implementation that:
- Maps to "Settings" table
- Sets Id as primary key and required
- Sets SettingsJson, IsEncrypted, Version, CreatedAt, UpdatedAt as required
- Creates index on UpdatedAt

### Step 6: Create Entity Configuration for QueueConfiguration

File: `Aura.Core/Data/EntityConfigurations/QueueConfigurationEntityConfiguration.cs`

Create a new file with IEntityTypeConfiguration implementation that:
- Maps to "QueueConfiguration" table
- Sets all properties as required with appropriate default values
- Creates index on UpdatedAt

### Step 7: Create Entity Configuration for AnalyticsRetentionSettings

File: `Aura.Core/Data/EntityConfigurations/AnalyticsRetentionSettingsEntityConfiguration.cs`

Create a new file with IEntityTypeConfiguration implementation that:
- Maps to "AnalyticsRetentionSettings" table
- Sets all properties as required with appropriate default values
- Creates indexes on IsEnabled and UpdatedAt

### Step 8: Register Entity Configurations

File: `Aura.Core/Data/AuraDbContext.cs`

In the `OnModelCreating` method, add:

```csharp
modelBuilder.ApplyConfiguration(new SettingsEntityConfiguration());
modelBuilder.ApplyConfiguration(new QueueConfigurationEntityConfiguration());
modelBuilder.ApplyConfiguration(new AnalyticsRetentionSettingsEntityConfiguration());
```

### Step 9: Generate and Apply Migrations

Run these commands in the project root:

```bash
dotnet ef migrations add AddSettingsTable --project Aura.Core --startup-project Aura.Api
dotnet ef migrations add AddQueueConfiguration --project Aura.Core --startup-project Aura.Api
dotnet ef migrations add AddAnalyticsRetentionSettings --project Aura.Core --startup-project Aura.Api
```

Note: The AI agent should create these migrations programmatically rather than running commands.

## Testing

Create unit tests in `Aura.Tests/Core/Data/MigrationTests.cs` to verify:
1. Settings table exists after migration
2. QueueConfiguration table has default entry with correct values
3. AnalyticsRetentionSettings table has default entry with correct values
4. All indexes are created correctly

## Acceptance Criteria

- [ ] All three migrations compile without errors
- [ ] Migrations apply successfully to a fresh database
- [ ] Migrations apply successfully to existing database
- [ ] Default data is inserted for QueueConfiguration (Id="default")
- [ ] Default data is inserted for AnalyticsRetentionSettings (Id="default")
- [ ] All indexes are created (UpdatedAt, IsEnabled where applicable)
- [ ] Unit tests pass
- [ ] Application starts without SQLite errors about missing tables
- [ ] BackgroundJobProcessorService starts without errors
- [ ] AnalyticsMaintenanceService starts without errors
- [ ] SettingsService can load user settings without errors

## Build Enforcement
- Zero placeholders - all migration code must be complete
- All entity configurations must be production-ready
- Pre-commit hooks must pass
- No compilation errors
