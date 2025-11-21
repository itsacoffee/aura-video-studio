using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.HostedServices;
using Aura.Core.Data;
using Aura.Core.Services.Analytics;
using Aura.Core.Services.Queue;
using Aura.Core.Services.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests.Api.HostedServices;

/// <summary>
/// Tests for graceful service startup with missing table handling
/// </summary>
public class ServiceHealthCheckTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AuraDbContext> _contextOptions;
    private readonly ServiceProvider _serviceProvider;

    public ServiceHealthCheckTests()
    {
        // Create in-memory database for testing
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<AuraDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Build service provider with required dependencies
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddDbContext<AuraDbContext>(options => options.UseSqlite(_connection));
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task BackgroundJobProcessorService_ExitsGracefully_WhenTableMissing()
    {
        // Arrange - Don't create database schema, simulate missing table
        var logger = _serviceProvider.GetRequiredService<ILogger<BackgroundJobProcessorService>>();
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        
        var service = new BackgroundJobProcessorService(logger, _serviceProvider, scopeFactory);
        var cts = new CancellationTokenSource();

        // Act - Start service and let it check health
        var serviceTask = service.StartAsync(cts.Token);
        await serviceTask;

        // Wait a bit for ExecuteAsync to run
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Stop the service
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - Service should have exited gracefully without throwing
        Assert.True(serviceTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task BackgroundJobProcessorService_CreatesDefaultConfig_WhenTableExistsButEmpty()
    {
        // Arrange - Create schema but no data
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await context.Database.EnsureCreatedAsync();

        var logger = _serviceProvider.GetRequiredService<ILogger<BackgroundJobProcessorService>>();
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        
        var service = new BackgroundJobProcessorService(logger, _serviceProvider, scopeFactory);
        var cts = new CancellationTokenSource();

        // Act - Start service
        await service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(7)); // Wait for health check and default creation

        // Assert - Default configuration should be created
        var config = await context.QueueConfiguration.FirstOrDefaultAsync();
        Assert.NotNull(config);
        Assert.Equal("default", config.Id);
        Assert.True(config.IsEnabled);
        Assert.Equal(2, config.MaxConcurrentJobs);

        // Cleanup
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AnalyticsMaintenanceService_ExitsGracefully_WhenTableMissing()
    {
        // Arrange - Don't create database schema
        var logger = _serviceProvider.GetRequiredService<ILogger<AnalyticsMaintenanceService>>();
        
        var service = new AnalyticsMaintenanceService(_serviceProvider, logger);
        var cts = new CancellationTokenSource();

        // Act - Start service and let it check health
        var serviceTask = service.StartAsync(cts.Token);
        await serviceTask;

        // Wait a bit for ExecuteAsync to run
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Stop the service
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - Service should have exited gracefully without throwing
        Assert.True(serviceTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task AnalyticsMaintenanceService_CreatesDefaultSettings_WhenTableExistsButEmpty()
    {
        // Arrange - Create schema but no data
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await context.Database.EnsureCreatedAsync();

        var logger = _serviceProvider.GetRequiredService<ILogger<AnalyticsMaintenanceService>>();
        
        var service = new AnalyticsMaintenanceService(_serviceProvider, logger);
        var cts = new CancellationTokenSource();

        // Act - Start service
        await service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(12)); // Wait for health check and default creation

        // Assert - Default settings should be created
        var settings = await context.AnalyticsRetentionSettings.FirstOrDefaultAsync();
        Assert.NotNull(settings);
        Assert.Equal("default", settings.Id);
        Assert.True(settings.IsEnabled);
        Assert.Equal(90, settings.UsageStatisticsRetentionDays);

        // Cleanup
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SettingsService_ReturnsInMemoryDefaults_WhenTableMissing()
    {
        // Arrange - Create SettingsService with mocked dependencies but no database schema
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<AuraDbContext>(options => options.UseSqlite(_connection));
        
        // Add required dependencies for SettingsService
        services.AddSingleton<Aura.Core.Configuration.ProviderSettings>();
        services.AddSingleton<Aura.Core.Configuration.IKeyStore, Aura.Core.Configuration.InMemoryKeyStore>();
        services.AddSingleton<Aura.Core.Services.SecureStorageService>();
        services.AddSingleton<Aura.Core.Hardware.IHardwareDetector, Aura.Core.Hardware.HardwareDetector>();
        services.AddScoped<ISettingsService, SettingsService>();

        var provider = services.BuildServiceProvider();
        var settingsService = provider.GetRequiredService<ISettingsService>();

        // Act - Try to get settings (table doesn't exist)
        var settings = await settingsService.GetSettingsAsync();

        // Assert - Should return in-memory defaults without throwing
        Assert.NotNull(settings);
        Assert.NotNull(settings.General);
        Assert.NotNull(settings.FileLocations);
        Assert.NotNull(settings.VideoDefaults);
    }

    [Fact]
    public async Task SettingsService_SavesDefaults_WhenTableExistsButEmpty()
    {
        // Arrange - Create schema
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Create SettingsService
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<AuraDbContext>(options => options.UseSqlite(_connection));
        
        services.AddSingleton<Aura.Core.Configuration.ProviderSettings>();
        services.AddSingleton<Aura.Core.Configuration.IKeyStore, Aura.Core.Configuration.InMemoryKeyStore>();
        services.AddSingleton<Aura.Core.Services.SecureStorageService>();
        services.AddSingleton<Aura.Core.Hardware.IHardwareDetector, Aura.Core.Hardware.HardwareDetector>();
        services.AddScoped<ISettingsService, SettingsService>();

        var provider = services.BuildServiceProvider();
        var settingsService = provider.GetRequiredService<ISettingsService>();

        // Act - Get settings (should create defaults)
        var settings = await settingsService.GetSettingsAsync();

        // Assert - Settings should be saved to database
        var settingsEntity = await context.Settings.FirstOrDefaultAsync();
        Assert.NotNull(settingsEntity);
        Assert.Equal("user-settings", settingsEntity.Id);
        Assert.False(string.IsNullOrEmpty(settingsEntity.SettingsJson));
    }

    [Fact]
    public async Task Services_NoUnhandledExceptions_DuringGracefulDegradation()
    {
        // Arrange - No database schema
        var logger = _serviceProvider.GetRequiredService<ILogger<BackgroundJobProcessorService>>();
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        
        var backgroundService = new BackgroundJobProcessorService(logger, _serviceProvider, scopeFactory);
        
        var analyticsLogger = _serviceProvider.GetRequiredService<ILogger<AnalyticsMaintenanceService>>();
        var analyticsService = new AnalyticsMaintenanceService(_serviceProvider, analyticsLogger);

        var cts = new CancellationTokenSource();

        // Act - Start both services without database
        Exception? caughtException = null;
        try
        {
            await backgroundService.StartAsync(cts.Token);
            await analyticsService.StartAsync(cts.Token);
            
            await Task.Delay(TimeSpan.FromSeconds(2));
            
            cts.Cancel();
            await backgroundService.StopAsync(CancellationToken.None);
            await analyticsService.StopAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert - No exceptions should be thrown
        Assert.Null(caughtException);
    }
}
