using System.Threading.Tasks;
using Aura.Api.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.HealthChecks;

public class StartupHealthCheckTests
{
    private readonly ILogger<StartupHealthCheck> _logger;

    public StartupHealthCheckTests()
    {
        var loggerMock = new Mock<ILogger<StartupHealthCheck>>();
        _logger = loggerMock.Object;
    }

    [Fact]
    public async Task CheckHealthAsync_BeforeMarkAsReady_ReturnsUnhealthy()
    {
        // Arrange
        var healthCheck = new StartupHealthCheck(_logger);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("still starting up", result.Description);
        Assert.NotNull(result.Data);
        Assert.False((bool)result.Data["ready"]);
    }

    [Fact]
    public async Task CheckHealthAsync_AfterMarkAsReady_ReturnsHealthy()
    {
        // Arrange
        var healthCheck = new StartupHealthCheck(_logger);
        var context = new HealthCheckContext();

        // Act
        healthCheck.MarkAsReady();
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("ready", result.Description);
        Assert.NotNull(result.Data);
        Assert.True((bool)result.Data["ready"]);
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesTimestamp()
    {
        // Arrange
        var healthCheck = new StartupHealthCheck(_logger);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Contains("timestamp", result.Data.Keys);
        Assert.IsType<DateTime>(result.Data["timestamp"]);
    }

    [Fact]
    public void MarkAsReady_CanBeCalledMultipleTimes()
    {
        // Arrange
        var healthCheck = new StartupHealthCheck(_logger);

        // Act & Assert - should not throw
        healthCheck.MarkAsReady();
        healthCheck.MarkAsReady();
        healthCheck.MarkAsReady();
    }

    [Fact]
    public async Task CheckHealthAsync_StatePersiststAcrossMultipleCalls()
    {
        // Arrange
        var healthCheck = new StartupHealthCheck(_logger);
        var context = new HealthCheckContext();

        // Act
        healthCheck.MarkAsReady();
        var result1 = await healthCheck.CheckHealthAsync(context);
        var result2 = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result1.Status);
        Assert.Equal(HealthStatus.Healthy, result2.Status);
    }
}
