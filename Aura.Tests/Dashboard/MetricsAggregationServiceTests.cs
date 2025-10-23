using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services.Dashboard;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Dashboard;

public class MetricsAggregationServiceTests
{
    private readonly Mock<ILogger<MetricsAggregationService>> _mockLogger;
    private readonly MetricsAggregationService _service;

    public MetricsAggregationServiceTests()
    {
        _mockLogger = new Mock<ILogger<MetricsAggregationService>>();
        _service = new MetricsAggregationService(_mockLogger.Object);
    }

    [Fact]
    public async Task GetAggregatedMetricsAsync_ReturnsQualityMetrics()
    {
        // Act
        var result = await _service.GetAggregatedMetricsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalVideosProcessed > 0);
        Assert.True(result.AverageQualityScore >= 0 && result.AverageQualityScore <= 100);
        Assert.True(result.SuccessRate >= 0 && result.SuccessRate <= 100);
        Assert.True(result.ComplianceRate >= 0 && result.ComplianceRate <= 100);
    }

    [Fact]
    public async Task GetMetricsBreakdownAsync_ReturnsBreakdown()
    {
        // Act
        var result = await _service.GetMetricsBreakdownAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ResolutionMetrics);
        Assert.NotNull(result.AudioQualityMetrics);
        Assert.NotNull(result.FrameRateMetrics);
        Assert.NotNull(result.ConsistencyMetrics);

        // Verify resolution metrics
        Assert.True(result.ResolutionMetrics.TotalChecks > 0);
        Assert.True(result.ResolutionMetrics.PassedChecks + result.ResolutionMetrics.FailedChecks == result.ResolutionMetrics.TotalChecks);
    }

    [Fact]
    public async Task GetAggregatedMetricsAsync_RespectsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await _service.GetAggregatedMetricsAsync(cts.Token);
        });
    }
}
