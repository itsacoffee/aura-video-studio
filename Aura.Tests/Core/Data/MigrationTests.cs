using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aura.Tests.Core.Data;

/// <summary>
/// Tests to verify database migrations for Settings, QueueConfiguration, and AnalyticsRetentionSettings tables
/// </summary>
public class MigrationTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly ServiceProvider _serviceProvider;

    public MigrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test-migrations-{Guid.NewGuid()}.db");

        var services = new ServiceCollection();
        
        services.AddDbContext<AuraDbContext>(options =>
            options.UseSqlite($"Data Source={_testDbPath}"));

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task Settings_Table_Exists_After_Migration()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

        // Act - Apply migrations
        await context.Database.MigrateAsync();

        // Assert - Verify Settings table exists by querying it
        var settings = await context.Settings.ToListAsync();
        Assert.NotNull(settings);
    }

    [Fact]
    public async Task Settings_Table_Has_Correct_Indexes()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await context.Database.MigrateAsync();

        // Act - Query indexes
        var indexQuery = @"
            SELECT name FROM sqlite_master 
            WHERE type='index' 
            AND tbl_name='Settings' 
            AND name LIKE 'IX_Settings%'";
        
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = indexQuery;
        
        var indexes = new System.Collections.Generic.List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0));
        }

        // Assert
        Assert.Contains("IX_Settings_UpdatedAt", indexes);
        Assert.Contains("IX_Settings_Version", indexes);
    }

    [Fact]
    public async Task QueueConfiguration_Table_Has_Default_Entry()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

        // Act - Apply migrations
        await context.Database.MigrateAsync();

        // Assert - Verify default entry exists
        var defaultConfig = await context.QueueConfiguration
            .FirstOrDefaultAsync(q => q.Id == "default");

        Assert.NotNull(defaultConfig);
        Assert.Equal("default", defaultConfig.Id);
        Assert.Equal(2, defaultConfig.MaxConcurrentJobs);
        Assert.False(defaultConfig.PauseOnBattery);
        Assert.Equal(90, defaultConfig.CpuThrottleThreshold);
        Assert.Equal(90, defaultConfig.MemoryThrottleThreshold);
        Assert.True(defaultConfig.IsEnabled);
        Assert.Equal(5, defaultConfig.PollingIntervalSeconds);
        Assert.Equal(30, defaultConfig.JobHistoryRetentionDays);
        Assert.Equal(90, defaultConfig.FailedJobRetentionDays);
        Assert.Equal(60, defaultConfig.RetryBaseDelaySeconds);
        Assert.Equal(3600, defaultConfig.RetryMaxDelaySeconds);
        Assert.True(defaultConfig.EnableNotifications);
    }

    [Fact]
    public async Task QueueConfiguration_Table_Has_UpdatedAt_Index()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await context.Database.MigrateAsync();

        // Act - Query indexes
        var indexQuery = @"
            SELECT name FROM sqlite_master 
            WHERE type='index' 
            AND tbl_name='QueueConfiguration' 
            AND name='IX_QueueConfiguration_UpdatedAt'";
        
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = indexQuery;
        
        var result = await command.ExecuteScalarAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IX_QueueConfiguration_UpdatedAt", result.ToString());
    }

    [Fact]
    public async Task AnalyticsRetentionSettings_Table_Has_Default_Entry()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

        // Act - Apply migrations
        await context.Database.MigrateAsync();

        // Assert - Verify default entry exists
        var defaultSettings = await context.AnalyticsRetentionSettings
            .FirstOrDefaultAsync(a => a.Id == "default");

        Assert.NotNull(defaultSettings);
        Assert.Equal("default", defaultSettings.Id);
        Assert.True(defaultSettings.IsEnabled);
        Assert.Equal(90, defaultSettings.UsageStatisticsRetentionDays);
        Assert.Equal(365, defaultSettings.CostTrackingRetentionDays);
        Assert.Equal(30, defaultSettings.PerformanceMetricsRetentionDays);
        Assert.True(defaultSettings.AutoCleanupEnabled);
        Assert.Equal(2, defaultSettings.CleanupHourUtc);
        Assert.False(defaultSettings.TrackSuccessOnly);
        Assert.True(defaultSettings.CollectHardwareMetrics);
        Assert.True(defaultSettings.AggregateOldData);
        Assert.Equal(30, defaultSettings.AggregationThresholdDays);
        Assert.Equal(500, defaultSettings.MaxDatabaseSizeMB);
    }

    [Fact]
    public async Task AnalyticsRetentionSettings_Table_Has_Correct_Indexes()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await context.Database.MigrateAsync();

        // Act - Query indexes
        var indexQuery = @"
            SELECT name FROM sqlite_master 
            WHERE type='index' 
            AND tbl_name='AnalyticsRetentionSettings' 
            AND name LIKE 'IX_AnalyticsRetentionSettings%'";
        
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = indexQuery;
        
        var indexes = new System.Collections.Generic.List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(0));
        }

        // Assert
        Assert.Contains("IX_AnalyticsRetentionSettings_IsEnabled", indexes);
        Assert.Contains("IX_AnalyticsRetentionSettings_UpdatedAt", indexes);
    }

    [Fact]
    public async Task All_Migrations_Apply_Successfully_To_Fresh_Database()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

        // Act - Apply all migrations
        await context.Database.MigrateAsync();

        // Assert - Verify database is ready
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect);

        // Verify critical tables have expected data
        var queueConfigExists = await context.QueueConfiguration.AnyAsync();
        var analyticsSettingsExists = await context.AnalyticsRetentionSettings.AnyAsync();

        Assert.True(queueConfigExists); // Should have default entry
        Assert.True(analyticsSettingsExists); // Should have default entry
        
        // Verify Settings table is accessible (may be empty)
        var settingsCount = await context.Settings.CountAsync();
        Assert.True(settingsCount >= 0);
    }

    [Fact]
    public async Task Migrations_Apply_Successfully_To_Existing_Database()
    {
        // Arrange - Create database with initial migration
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            await context.Database.MigrateAsync();
        }

        // Act - Apply migrations again (simulating existing database)
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            await context.Database.MigrateAsync();

            // Assert - Verify all tables still exist and work
            var settingsCount = await context.Settings.CountAsync();
            var queueConfigCount = await context.QueueConfiguration.CountAsync();
            var analyticsSettingsCount = await context.AnalyticsRetentionSettings.CountAsync();

            Assert.True(settingsCount >= 0);
            Assert.Equal(1, queueConfigCount); // Should have exactly one default entry
            Assert.Equal(1, analyticsSettingsCount); // Should have exactly one default entry
        }
    }

    [Fact]
    public async Task Settings_Table_Supports_Insert_And_Query()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await context.Database.MigrateAsync();

        // Act - Insert test settings
        var testSettings = new SettingsEntity
        {
            Id = "test-settings",
            SettingsJson = "{\"theme\":\"dark\"}",
            IsEncrypted = false,
            Version = "1.0.0",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Settings.Add(testSettings);
        await context.SaveChangesAsync();

        // Assert - Query back
        var retrieved = await context.Settings.FindAsync("test-settings");
        Assert.NotNull(retrieved);
        Assert.Equal("test-settings", retrieved.Id);
        Assert.Equal("{\"theme\":\"dark\"}", retrieved.SettingsJson);
        Assert.False(retrieved.IsEncrypted);
    }
}
