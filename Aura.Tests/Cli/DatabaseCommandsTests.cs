using System;
using System.IO;
using System.Threading.Tasks;
using Aura.Cli.Commands;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests.Cli;

/// <summary>
/// Tests for database migration CLI commands
/// </summary>
public class DatabaseCommandsTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly ServiceProvider _serviceProvider;
    private readonly MigrateCommand _migrateCommand;
    private readonly StatusCommand _statusCommand;
    private readonly ResetCommand _resetCommand;

    public DatabaseCommandsTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test-cli-db-{Guid.NewGuid()}.db");

        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        services.AddDbContext<AuraDbContext>(options =>
            options.UseSqlite($"Data Source={_testDbPath}",
                sqliteOptions => sqliteOptions.MigrationsAssembly("Aura.Api")));

        _serviceProvider = services.BuildServiceProvider();

        var logger = _serviceProvider.GetRequiredService<ILogger<MigrateCommand>>();
        var dbContext = _serviceProvider.GetRequiredService<AuraDbContext>();
        
        _migrateCommand = new MigrateCommand(
            _serviceProvider.GetRequiredService<ILogger<MigrateCommand>>(),
            dbContext);
        
        _statusCommand = new StatusCommand(
            _serviceProvider.GetRequiredService<ILogger<StatusCommand>>(),
            dbContext);
        
        _resetCommand = new ResetCommand(
            _serviceProvider.GetRequiredService<ILogger<ResetCommand>>(),
            dbContext);
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
    public async Task MigrateCommand_WithPendingMigrations_AppliesSuccessfully()
    {
        // Arrange - database doesn't exist yet, so there will be pending migrations
        
        // Act
        var exitCode = await _migrateCommand.ExecuteAsync(Array.Empty<string>());
        
        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(_testDbPath));
        
        // Verify database is functional
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [Fact]
    public async Task MigrateCommand_WithNoPendingMigrations_ReturnsSuccess()
    {
        // Arrange - apply migrations first
        await _migrateCommand.ExecuteAsync(Array.Empty<string>());
        
        // Act - run migrate again
        var exitCode = await _migrateCommand.ExecuteAsync(Array.Empty<string>());
        
        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task MigrateCommand_WithDryRun_DoesNotApplyMigrations()
    {
        // Act
        var exitCode = await _migrateCommand.ExecuteAsync(new[] { "--dry-run" });
        
        // Assert
        Assert.Equal(0, exitCode);
        // Database should not be created in dry-run mode
        Assert.False(File.Exists(_testDbPath));
    }

    [Fact]
    public async Task MigrateCommand_WithHelp_ShowsHelp()
    {
        // Act
        var exitCode = await _migrateCommand.ExecuteAsync(new[] { "--help" });
        
        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task StatusCommand_WithExistingDatabase_ShowsStatus()
    {
        // Arrange - create database
        await _migrateCommand.ExecuteAsync(Array.Empty<string>());
        
        // Act
        var exitCode = await _statusCommand.ExecuteAsync(Array.Empty<string>());
        
        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task StatusCommand_WithoutDatabase_CannotConnect()
    {
        // Act - status check before database exists
        var exitCode = await _statusCommand.ExecuteAsync(Array.Empty<string>());
        
        // Assert
        // Should fail because database doesn't exist
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task StatusCommand_WithVerboseFlag_ShowsAllMigrations()
    {
        // Arrange
        await _migrateCommand.ExecuteAsync(Array.Empty<string>());
        
        // Act
        var exitCode = await _statusCommand.ExecuteAsync(new[] { "--verbose" });
        
        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task StatusCommand_WithHelp_ShowsHelp()
    {
        // Act
        var exitCode = await _statusCommand.ExecuteAsync(new[] { "--help" });
        
        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ResetCommand_WithForceFlag_DropsAndRecreatesDatabase()
    {
        // Arrange - create database first
        await _migrateCommand.ExecuteAsync(Array.Empty<string>());
        Assert.True(File.Exists(_testDbPath));
        
        // Act
        var exitCode = await _resetCommand.ExecuteAsync(new[] { "--force" });
        
        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(_testDbPath));
        
        // Verify database is functional after reset
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [Fact]
    public async Task ResetCommand_WithDryRun_DoesNotDropDatabase()
    {
        // Arrange - create database first
        await _migrateCommand.ExecuteAsync(Array.Empty<string>());
        var creationTime = File.GetLastWriteTimeUtc(_testDbPath);
        
        // Act
        var exitCode = await _resetCommand.ExecuteAsync(new[] { "--dry-run", "--force" });
        
        // Assert
        Assert.Equal(0, exitCode);
        // Database should still exist and not be modified
        Assert.True(File.Exists(_testDbPath));
        Assert.Equal(creationTime, File.GetLastWriteTimeUtc(_testDbPath));
    }

    [Fact]
    public async Task ResetCommand_WithHelp_ShowsHelp()
    {
        // Act
        var exitCode = await _resetCommand.ExecuteAsync(new[] { "--help" });
        
        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task MigrationWorkflow_EndToEnd_WorksCorrectly()
    {
        // This test verifies the complete workflow:
        // 1. Check status (should fail - no DB)
        // 2. Apply migrations
        // 3. Check status (should succeed - DB exists)
        // 4. Apply migrations again (should be no-op)
        // 5. Reset database
        // 6. Check status (should succeed - DB recreated)
        
        // Step 1: Status should fail
        var statusExitCode1 = await _statusCommand.ExecuteAsync(Array.Empty<string>());
        Assert.Equal(1, statusExitCode1);
        
        // Step 2: Apply migrations
        var migrateExitCode1 = await _migrateCommand.ExecuteAsync(Array.Empty<string>());
        Assert.Equal(0, migrateExitCode1);
        
        // Step 3: Status should succeed
        var statusExitCode2 = await _statusCommand.ExecuteAsync(Array.Empty<string>());
        Assert.Equal(0, statusExitCode2);
        
        // Step 4: Migrate again (no-op)
        var migrateExitCode2 = await _migrateCommand.ExecuteAsync(Array.Empty<string>());
        Assert.Equal(0, migrateExitCode2);
        
        // Step 5: Reset
        var resetExitCode = await _resetCommand.ExecuteAsync(new[] { "--force" });
        Assert.Equal(0, resetExitCode);
        
        // Step 6: Status should succeed
        var statusExitCode3 = await _statusCommand.ExecuteAsync(Array.Empty<string>());
        Assert.Equal(0, statusExitCode3);
    }
}
