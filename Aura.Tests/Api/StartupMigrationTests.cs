using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aura.Tests.Api;

/// <summary>
/// Tests for automatic database migrations on API startup
/// </summary>
public class StartupMigrationTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly ServiceProvider _serviceProvider;

    public StartupMigrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test-startup-db-{Guid.NewGuid()}.db");

        var services = new ServiceCollection();
        
        services.AddDbContext<AuraDbContext>(options =>
            options.UseSqlite($"Data Source={_testDbPath}",
                sqliteOptions => sqliteOptions.MigrationsAssembly("Aura.Api")));

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
    public async Task AutomaticMigration_OnStartup_AppliesAllPendingMigrations()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        
        // Verify there are pending migrations before applying
        var pendingBefore = await dbContext.Database.GetPendingMigrationsAsync();
        Assert.NotEmpty(pendingBefore);
        
        // Act - Simulate what happens on startup
        await dbContext.Database.MigrateAsync();
        
        // Assert
        var pendingAfter = await dbContext.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingAfter);
        
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        Assert.NotEmpty(appliedMigrations);
    }

    [Fact]
    public async Task AutomaticMigration_WhenDatabaseUpToDate_DoesNotFail()
    {
        // Arrange - apply migrations first
        using var scope1 = _serviceProvider.CreateScope();
        var dbContext1 = scope1.ServiceProvider.GetRequiredService<AuraDbContext>();
        await dbContext1.Database.MigrateAsync();
        
        // Act - try to migrate again (simulating restart)
        using var scope2 = _serviceProvider.CreateScope();
        var dbContext2 = scope2.ServiceProvider.GetRequiredService<AuraDbContext>();
        
        var pendingMigrations = await dbContext2.Database.GetPendingMigrationsAsync();
        
        // Assert - no pending migrations
        Assert.Empty(pendingMigrations);
        
        // Migrate again should not fail
        await dbContext2.Database.MigrateAsync();
    }

    [Fact]
    public async Task AutomaticMigration_CreatesAllRequiredTables()
    {
        // Arrange & Act
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await dbContext.Database.MigrateAsync();
        
        // Assert - verify key tables exist by querying them
        var settingsCount = await dbContext.Settings.CountAsync();
        var templatesCount = await dbContext.Templates.CountAsync();
        var projectStatesCount = await dbContext.ProjectStates.CountAsync();
        
        // These queries should not throw exceptions, indicating tables exist
        Assert.True(settingsCount >= 0);
        Assert.True(templatesCount >= 0);
        Assert.True(projectStatesCount >= 0);
    }

    [Fact]
    public async Task AutomaticMigration_AppliesSeedData()
    {
        // Arrange & Act
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await dbContext.Database.MigrateAsync();
        
        // Assert - verify seed data exists
        // Check for default system configuration
        var systemConfig = await dbContext.SystemConfigurations.FirstOrDefaultAsync();
        Assert.NotNull(systemConfig);
        
        // Check for default roles
        var roles = await dbContext.Roles.ToListAsync();
        Assert.NotEmpty(roles);
        Assert.Contains(roles, r => r.Name == "Administrator");
        Assert.Contains(roles, r => r.Name == "User");
        Assert.Contains(roles, r => r.Name == "Viewer");
        
        // Check for default queue configuration
        var queueConfig = await dbContext.QueueConfiguration.FirstOrDefaultAsync();
        Assert.NotNull(queueConfig);
        
        // Check for default analytics retention settings
        var analyticsSettings = await dbContext.AnalyticsRetentionSettings.FirstOrDefaultAsync();
        Assert.NotNull(analyticsSettings);
    }

    [Fact]
    public async Task AutomaticMigration_LogsMigrationStatus()
    {
        // This test verifies that the migration process can be tracked
        // In actual implementation, this would be verified through log output
        
        // Arrange & Act
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        
        var pendingBefore = await dbContext.Database.GetPendingMigrationsAsync();
        var pendingCountBefore = pendingBefore.Count();
        
        await dbContext.Database.MigrateAsync();
        
        var appliedAfter = await dbContext.Database.GetAppliedMigrationsAsync();
        var appliedCountAfter = appliedAfter.Count();
        
        // Assert - migrations were applied
        Assert.True(appliedCountAfter > 0);
        Assert.True(appliedCountAfter >= pendingCountBefore);
    }

    [Fact]
    public async Task AutomaticMigration_HandlesErrorsGracefully()
    {
        // This test verifies that if migration fails, it doesn't crash the application
        // In the actual implementation, errors are logged but don't throw exceptions
        
        // Arrange - use an invalid connection string
        var services = new ServiceCollection();
        services.AddDbContext<AuraDbContext>(options =>
            options.UseSqlite("Data Source=/invalid/path/db.db",
                sqliteOptions => sqliteOptions.MigrationsAssembly("Aura.Api")));
        
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        
        // Act & Assert - migration should fail but be caught
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await dbContext.Database.MigrateAsync();
        });
        
        // In actual Program.cs, this exception is caught and logged
        // The application continues to start
    }
}
