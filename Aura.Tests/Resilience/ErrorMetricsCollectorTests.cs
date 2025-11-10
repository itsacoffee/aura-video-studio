using Aura.Core.Resilience.ErrorTracking;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Resilience;

public class ErrorMetricsCollectorTests
{
    private readonly Mock<ILogger<ErrorMetricsCollector>> _loggerMock;
    private readonly ErrorMetricsCollector _collector;

    public ErrorMetricsCollectorTests()
    {
        _loggerMock = new Mock<ILogger<ErrorMetricsCollector>>();
        _collector = new ErrorMetricsCollector(_loggerMock.Object);
    }

    [Fact]
    public void RecordError_ShouldIncrementErrorCount()
    {
        // Arrange
        var serviceName = "TestService";
        var exception = new InvalidOperationException("Test error");

        // Act
        _collector.RecordError(serviceName, exception);

        // Assert
        var metrics = _collector.GetMetrics(serviceName);
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.TotalErrors);
        Assert.Equal(exception, metrics.LastError);
    }

    [Fact]
    public void RecordError_ShouldCategorizeErrors()
    {
        // Arrange
        var serviceName = "TestService";

        // Act
        _collector.RecordError(serviceName, new TimeoutException());
        _collector.RecordError(serviceName, new HttpRequestException());
        _collector.RecordError(serviceName, new ArgumentException());

        // Assert
        var metrics = _collector.GetMetrics(serviceName);
        Assert.Equal(1, metrics.GetCategoryCount(ErrorCategory.Timeout));
        Assert.Equal(1, metrics.GetCategoryCount(ErrorCategory.Network));
        Assert.Equal(1, metrics.GetCategoryCount(ErrorCategory.Validation));
    }

    [Fact]
    public void RecordSuccess_ShouldIncrementSuccessCount()
    {
        // Arrange
        var serviceName = "TestService";

        // Act
        _collector.RecordSuccess(serviceName);
        _collector.RecordSuccess(serviceName);

        // Assert
        var metrics = _collector.GetMetrics(serviceName);
        Assert.NotNull(metrics);
        Assert.Equal(2, metrics.TotalSuccesses);
    }

    [Fact]
    public void GetErrorRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var serviceName = "TestService";
        
        // Act
        _collector.RecordError(serviceName, new Exception());
        _collector.RecordError(serviceName, new Exception());
        _collector.RecordSuccess(serviceName);
        _collector.RecordSuccess(serviceName);

        // Assert
        var metrics = _collector.GetMetrics(serviceName);
        Assert.Equal(0.5, metrics.ErrorRate); // 2 errors / 4 total = 0.5
    }

    [Fact]
    public void GetRecentErrors_ShouldReturnRecentErrors()
    {
        // Arrange
        var serviceName = "TestService";
        
        for (int i = 0; i < 10; i++)
        {
            _collector.RecordError(serviceName, new Exception($"Error {i}"));
        }

        // Act
        var recentErrors = _collector.GetRecentErrors(5).ToList();

        // Assert
        Assert.Equal(5, recentErrors.Count);
    }

    [Fact]
    public void GetAllMetrics_ShouldReturnAllServices()
    {
        // Arrange
        _collector.RecordError("Service1", new Exception());
        _collector.RecordError("Service2", new Exception());
        _collector.RecordError("Service3", new Exception());

        // Act
        var allMetrics = _collector.GetAllMetrics();

        // Assert
        Assert.Equal(3, allMetrics.Count);
        Assert.Contains("Service1", allMetrics.Keys);
        Assert.Contains("Service2", allMetrics.Keys);
        Assert.Contains("Service3", allMetrics.Keys);
    }

    [Fact]
    public void ResetMetrics_ShouldClearServiceMetrics()
    {
        // Arrange
        var serviceName = "TestService";
        _collector.RecordError(serviceName, new Exception());
        _collector.RecordSuccess(serviceName);

        // Act
        _collector.ResetMetrics(serviceName);

        // Assert
        var metrics = _collector.GetMetrics(serviceName);
        Assert.Null(metrics);
    }

    [Fact]
    public void GetErrorRate_ForTimeWindow_ShouldCalculateCorrectly()
    {
        // Arrange
        var serviceName = "TestService";
        
        // Record 10 errors
        for (int i = 0; i < 10; i++)
        {
            _collector.RecordError(serviceName, new Exception());
        }

        // Act
        var errorRate = _collector.GetErrorRate(serviceName, TimeSpan.FromMinutes(5));

        // Assert - should be 2 errors per minute (10 errors / 5 minutes)
        Assert.Equal(2.0, errorRate);
    }

    [Fact]
    public void GetMetrics_ShouldReturnNullForUnknownService()
    {
        // Act
        var metrics = _collector.GetMetrics("UnknownService");

        // Assert
        Assert.Null(metrics);
    }
}
