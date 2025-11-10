using System.Threading.Tasks;
using Aura.Api.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.HealthChecks;

public class DiskSpaceHealthCheckTests
{
    private readonly ILogger<DiskSpaceHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public DiskSpaceHealthCheckTests()
    {
        var loggerMock = new Mock<ILogger<DiskSpaceHealthCheck>>();
        _logger = loggerMock.Object;

        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c.GetValue<double>("HealthChecks:DiskSpaceThresholdGB", 1.0))
            .Returns(1.0);
        configurationMock.Setup(c => c.GetValue<double>("HealthChecks:DiskSpaceCriticalGB", 0.5))
            .Returns(0.5);
        _configuration = configurationMock.Object;
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDiskSpaceMetrics()
    {
        // Arrange
        var healthCheck = new DiskSpaceHealthCheck(_logger, _configuration);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Contains("free_gb", result.Data.Keys);
        Assert.Contains("total_gb", result.Data.Keys);
        Assert.Contains("threshold_gb", result.Data.Keys);
        Assert.Contains("critical_gb", result.Data.Keys);
        Assert.Contains("drive", result.Data.Keys);
    }

    [Fact]
    public async Task CheckHealthAsync_WithSufficientSpace_ReturnsHealthy()
    {
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c.GetValue<double>("HealthChecks:DiskSpaceThresholdGB", 1.0))
            .Returns(0.001); // Set very low threshold
        configurationMock.Setup(c => c.GetValue<double>("HealthChecks:DiskSpaceCriticalGB", 0.5))
            .Returns(0.0005); // Set very low threshold

        var healthCheck = new DiskSpaceHealthCheck(_logger, configurationMock.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsPositiveSpaceValues()
    {
        // Arrange
        var healthCheck = new DiskSpaceHealthCheck(_logger, _configuration);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        var freeGB = (double)result.Data["free_gb"];
        var totalGB = (double)result.Data["total_gb"];
        Assert.True(freeGB >= 0);
        Assert.True(totalGB > 0);
        Assert.True(freeGB <= totalGB);
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesThresholdConfiguration()
    {
        // Arrange
        var healthCheck = new DiskSpaceHealthCheck(_logger, _configuration);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(1.0, (double)result.Data["threshold_gb"]);
        Assert.Equal(0.5, (double)result.Data["critical_gb"]);
    }
}
