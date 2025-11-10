using Aura.Core.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Resilience;

public class CircuitBreakerStateManagerTests
{
    private readonly Mock<ILogger<CircuitBreakerStateManager>> _loggerMock;
    private readonly CircuitBreakerStateManager _manager;

    public CircuitBreakerStateManagerTests()
    {
        _loggerMock = new Mock<ILogger<CircuitBreakerStateManager>>();
        _manager = new CircuitBreakerStateManager(_loggerMock.Object);
    }

    [Fact]
    public void RecordStateChange_ShouldStoreState()
    {
        // Arrange
        var serviceName = "TestService";

        // Act
        _manager.RecordStateChange(serviceName, CircuitState.Open, "Too many failures");

        // Assert
        var state = _manager.GetState(serviceName);
        Assert.NotNull(state);
        Assert.Equal(serviceName, state.ServiceName);
        Assert.Equal(CircuitState.Open, state.State);
        Assert.Equal("Too many failures", state.Reason);
    }

    [Fact]
    public void RecordStateChange_ShouldUpdateExistingState()
    {
        // Arrange
        var serviceName = "TestService";
        _manager.RecordStateChange(serviceName, CircuitState.Open);

        // Act
        _manager.RecordStateChange(serviceName, CircuitState.HalfOpen);

        // Assert
        var state = _manager.GetState(serviceName);
        Assert.Equal(CircuitState.HalfOpen, state.State);
    }

    [Fact]
    public void GetAllStates_ShouldReturnAllRecordedStates()
    {
        // Arrange
        _manager.RecordStateChange("Service1", CircuitState.Open);
        _manager.RecordStateChange("Service2", CircuitState.Closed);
        _manager.RecordStateChange("Service3", CircuitState.HalfOpen);

        // Act
        var allStates = _manager.GetAllStates();

        // Assert
        Assert.Equal(3, allStates.Count);
        Assert.Contains("Service1", allStates.Keys);
        Assert.Contains("Service2", allStates.Keys);
        Assert.Contains("Service3", allStates.Keys);
    }

    [Fact]
    public void GetDegradedServices_ShouldReturnOpenAndHalfOpenCircuits()
    {
        // Arrange
        _manager.RecordStateChange("Service1", CircuitState.Open);
        _manager.RecordStateChange("Service2", CircuitState.Closed);
        _manager.RecordStateChange("Service3", CircuitState.HalfOpen);

        // Act
        var degraded = _manager.GetDegradedServices().ToList();

        // Assert
        Assert.Equal(2, degraded.Count);
        Assert.Contains(degraded, s => s.ServiceName == "Service1");
        Assert.Contains(degraded, s => s.ServiceName == "Service3");
        Assert.DoesNotContain(degraded, s => s.ServiceName == "Service2");
    }

    [Fact]
    public void GetHealthyServices_ShouldReturnClosedCircuits()
    {
        // Arrange
        _manager.RecordStateChange("Service1", CircuitState.Open);
        _manager.RecordStateChange("Service2", CircuitState.Closed);
        _manager.RecordStateChange("Service3", CircuitState.HalfOpen);

        // Act
        var healthy = _manager.GetHealthyServices().ToList();

        // Assert
        Assert.Single(healthy);
        Assert.Equal("Service2", healthy[0].ServiceName);
    }

    [Fact]
    public void IsServiceAvailable_ShouldReturnFalseForOpenCircuits()
    {
        // Arrange
        _manager.RecordStateChange("TestService", CircuitState.Open);

        // Act
        var isAvailable = _manager.IsServiceAvailable("TestService");

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public void IsServiceAvailable_ShouldReturnTrueForClosedCircuits()
    {
        // Arrange
        _manager.RecordStateChange("TestService", CircuitState.Closed);

        // Act
        var isAvailable = _manager.IsServiceAvailable("TestService");

        // Assert
        Assert.True(isAvailable);
    }

    [Fact]
    public void IsServiceAvailable_ShouldReturnTrueForUnknownServices()
    {
        // Act
        var isAvailable = _manager.IsServiceAvailable("UnknownService");

        // Assert
        Assert.True(isAvailable, "Unknown services should be assumed available");
    }

    [Fact]
    public void GetState_ShouldReturnNullForUnknownService()
    {
        // Act
        var state = _manager.GetState("UnknownService");

        // Assert
        Assert.Null(state);
    }
}
