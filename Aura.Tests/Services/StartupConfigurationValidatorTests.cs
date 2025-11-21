using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services;
using Aura.Core.Configuration;
using Aura.Core.Data;
using Aura.Core.Services.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Services;

/// <summary>
/// Tests for StartupConfigurationValidator
/// </summary>
public class StartupConfigurationValidatorTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ServiceCollection _services;

    public StartupConfigurationValidatorTests()
    {
        // Setup in-memory configuration
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string?>("OutputDirectory", "/tmp/test-output"),
            new System.Collections.Generic.KeyValuePair<string, string?>("LogsDirectory", "/tmp/test-logs"),
            new System.Collections.Generic.KeyValuePair<string, string?>("Database:Provider", "SQLite")
        });
        _configuration = configBuilder.Build();

        // Setup service collection
        _services = new ServiceCollection();
        _services.AddSingleton(_configuration);
        _services.AddLogging(builder => builder.AddConsole());
        _services.AddMemoryCache();
        
        // Add DbContext with in-memory database
        _services.AddDbContext<AuraDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase"));
        
        // Add required services
        _services.AddSingleton<FFmpegConfigurationStore>();
        _services.AddSingleton<IFFmpegDetectionService, FFmpegDetectionService>();
        
        _serviceProvider = _services.BuildServiceProvider();
    }

    [Fact]
    public async Task StartAsync_ShouldComplete_WithoutErrors()
    {
        // Arrange
        var logger = NullLogger<StartupConfigurationValidator>.Instance;
        var ffmpegDetection = _serviceProvider.GetRequiredService<IFFmpegDetectionService>();
        var configStore = _serviceProvider.GetRequiredService<FFmpegConfigurationStore>();
        
        var validator = new StartupConfigurationValidator(
            logger,
            _configuration,
            ffmpegDetection,
            configStore,
            _serviceProvider);

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - should not throw
    }

    [Fact]
    public async Task StartAsync_ShouldValidateDatabase()
    {
        // Arrange
        var logger = NullLogger<StartupConfigurationValidator>.Instance;
        var ffmpegDetection = _serviceProvider.GetRequiredService<IFFmpegDetectionService>();
        var configStore = _serviceProvider.GetRequiredService<FFmpegConfigurationStore>();
        
        var validator = new StartupConfigurationValidator(
            logger,
            _configuration,
            ffmpegDetection,
            configStore,
            _serviceProvider);

        // Act
        await validator.StartAsync(CancellationToken.None);

        // Assert - database context should be accessible
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [Fact]
    public async Task StartAsync_ShouldHandleEnvironmentFFmpegPath()
    {
        // Arrange
        var testPath = "/usr/bin/ffmpeg";
        Environment.SetEnvironmentVariable("FFMPEG_PATH", testPath);
        
        try
        {
            var logger = NullLogger<StartupConfigurationValidator>.Instance;
            var ffmpegDetection = _serviceProvider.GetRequiredService<IFFmpegDetectionService>();
            var configStore = _serviceProvider.GetRequiredService<FFmpegConfigurationStore>();
            
            var validator = new StartupConfigurationValidator(
                logger,
                _configuration,
                ffmpegDetection,
                configStore,
                _serviceProvider);

            // Act
            await validator.StartAsync(CancellationToken.None);

            // Assert - should not throw even if path doesn't exist
            // The validator logs warnings but continues
        }
        finally
        {
            Environment.SetEnvironmentVariable("FFMPEG_PATH", null);
        }
    }

    [Fact]
    public void StopAsync_ShouldComplete()
    {
        // Arrange
        var logger = NullLogger<StartupConfigurationValidator>.Instance;
        var ffmpegDetection = _serviceProvider.GetRequiredService<IFFmpegDetectionService>();
        var configStore = _serviceProvider.GetRequiredService<FFmpegConfigurationStore>();
        
        var validator = new StartupConfigurationValidator(
            logger,
            _configuration,
            ffmpegDetection,
            configStore,
            _serviceProvider);

        // Act & Assert - should not throw
        var task = validator.StopAsync(CancellationToken.None);
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        // Arrange
        var ffmpegDetection = _serviceProvider.GetRequiredService<IFFmpegDetectionService>();
        var configStore = _serviceProvider.GetRequiredService<FFmpegConfigurationStore>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new StartupConfigurationValidator(
                null!,
                _configuration,
                ffmpegDetection,
                configStore,
                _serviceProvider));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenConfigurationIsNull()
    {
        // Arrange
        var logger = NullLogger<StartupConfigurationValidator>.Instance;
        var ffmpegDetection = _serviceProvider.GetRequiredService<IFFmpegDetectionService>();
        var configStore = _serviceProvider.GetRequiredService<FFmpegConfigurationStore>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new StartupConfigurationValidator(
                logger,
                null!,
                ffmpegDetection,
                configStore,
                _serviceProvider));
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
