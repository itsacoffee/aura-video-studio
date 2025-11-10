using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.HealthChecks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests.HealthChecks;

public class DatabaseHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheckTests()
    {
        var services = new ServiceCollection();
        
        // Add in-memory database for testing
        services.AddDbContext<AuraDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid()));
        
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<DatabaseHealthCheck>>();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsHealthy_ReturnsHealthy()
    {
        // Arrange
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var healthCheck = new DatabaseHealthCheck(_logger, scopeFactory);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.True((bool)result.Data["connection_available"]);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsResponseTimeMetrics()
    {
        // Arrange
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var healthCheck = new DatabaseHealthCheck(_logger, scopeFactory);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Contains("response_time_ms", result.Data.Keys);
        var responseTime = (long)result.Data["response_time_ms"];
        Assert.True(responseTime >= 0);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsProjectCount()
    {
        // Arrange
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var healthCheck = new DatabaseHealthCheck(_logger, scopeFactory);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Contains("project_count", result.Data.Keys);
        var projectCount = (int)result.Data["project_count"];
        Assert.True(projectCount >= 0);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var healthCheck = new DatabaseHealthCheck(_logger, scopeFactory);
        var context = new HealthCheckContext();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await healthCheck.CheckHealthAsync(context, cts.Token));
    }
}
