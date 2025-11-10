using System.Threading.Tasks;
using Aura.Api.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.HealthChecks;

public class MemoryHealthCheckTests
{
    private readonly ILogger<MemoryHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public MemoryHealthCheckTests()
    {
        var loggerMock = new Mock<ILogger<MemoryHealthCheck>>();
        _logger = loggerMock.Object;

        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c.GetValue<double>("HealthChecks:MemoryWarningThresholdMB", 1024.0))
            .Returns(1024.0);
        configurationMock.Setup(c => c.GetValue<double>("HealthChecks:MemoryCriticalThresholdMB", 2048.0))
            .Returns(2048.0);
        _configuration = configurationMock.Object;
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsMemoryMetrics()
    {
        // Arrange
        var healthCheck = new MemoryHealthCheck(_logger, _configuration);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Contains("working_set_mb", result.Data.Keys);
        Assert.Contains("private_memory_mb", result.Data.Keys);
        Assert.Contains("gc_total_memory_mb", result.Data.Keys);
        Assert.Contains("gc_gen0_collections", result.Data.Keys);
        Assert.Contains("gc_gen1_collections", result.Data.Keys);
        Assert.Contains("gc_gen2_collections", result.Data.Keys);
    }

    [Fact]
    public async Task CheckHealthAsync_WithLowMemory_ReturnsHealthy()
    {
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c.GetValue<double>("HealthChecks:MemoryWarningThresholdMB", 1024.0))
            .Returns(100000.0); // Set very high threshold
        configurationMock.Setup(c => c.GetValue<double>("HealthChecks:MemoryCriticalThresholdMB", 2048.0))
            .Returns(200000.0); // Set very high threshold

        var healthCheck = new MemoryHealthCheck(_logger, configurationMock.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesThresholds()
    {
        // Arrange
        var healthCheck = new MemoryHealthCheck(_logger, _configuration);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Contains("warning_threshold_mb", result.Data.Keys);
        Assert.Contains("critical_threshold_mb", result.Data.Keys);
        Assert.Equal(1024.0, (double)result.Data["warning_threshold_mb"]);
        Assert.Equal(2048.0, (double)result.Data["critical_threshold_mb"]);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsGCStatistics()
    {
        // Arrange
        var healthCheck = new MemoryHealthCheck(_logger, _configuration);
        var context = new HealthCheckContext();

        // Force some GC collections
        GC.Collect(0);
        GC.Collect(1);

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        var gen0Collections = (int)result.Data["gc_gen0_collections"];
        var gen1Collections = (int)result.Data["gc_gen1_collections"];
        Assert.True(gen0Collections >= 0);
        Assert.True(gen1Collections >= 0);
    }
}
