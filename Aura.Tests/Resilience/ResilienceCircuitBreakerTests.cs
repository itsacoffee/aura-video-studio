using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Resilience;

public class ResilienceCircuitBreakerTests
{
    private readonly ILogger<CircuitBreaker> _logger;

    public ResilienceCircuitBreakerTests()
    {
        _logger = NullLogger<CircuitBreaker>.Instance;
    }

    [Fact]
    public void CircuitBreaker_InitialState_IsClosed()
    {
        // Arrange & Act
        var breaker = new CircuitBreaker(_logger, "TestService");

        // Assert
        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.Equal(0, breaker.FailureCount);
        Assert.Null(breaker.OpenedAt);
        Assert.True(breaker.IsAllowed());
    }

    [Fact]
    public void RecordFailure_BelowThreshold_KeepsClosed()
    {
        // Arrange
        var breaker = new CircuitBreaker(_logger, "TestService", failureThreshold: 5);

        // Act
        breaker.RecordFailure();
        breaker.RecordFailure();
        breaker.RecordFailure();

        // Assert
        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.Equal(3, breaker.FailureCount);
        Assert.True(breaker.IsAllowed());
    }

    [Fact]
    public void RecordFailure_AtThreshold_OpensCircuit()
    {
        // Arrange
        var breaker = new CircuitBreaker(_logger, "TestService", failureThreshold: 3);

        // Act
        breaker.RecordFailure();
        breaker.RecordFailure();
        breaker.RecordFailure();

        // Assert
        Assert.Equal(CircuitState.Open, breaker.State);
        Assert.Equal(3, breaker.FailureCount);
        Assert.False(breaker.IsAllowed());
        Assert.NotNull(breaker.OpenedAt);
    }

    [Fact]
    public void RecordSuccess_ResetsFaiureCount()
    {
        // Arrange
        var breaker = new CircuitBreaker(_logger, "TestService", failureThreshold: 5);
        breaker.RecordFailure();
        breaker.RecordFailure();

        // Act
        breaker.RecordSuccess();

        // Assert
        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.Equal(0, breaker.FailureCount);
    }

    [Fact]
    public void Reset_ClosesOpenCircuit()
    {
        // Arrange
        var breaker = new CircuitBreaker(_logger, "TestService", failureThreshold: 2);
        breaker.RecordFailure();
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Open, breaker.State);

        // Act
        breaker.Reset();

        // Assert
        Assert.Equal(CircuitState.Closed, breaker.State);
        Assert.Equal(0, breaker.FailureCount);
        Assert.True(breaker.IsAllowed());
    }

    [Fact]
    public async Task OpenCircuit_TransitionsToHalfOpen_AfterDuration()
    {
        // Arrange
        var breaker = new CircuitBreaker(
            _logger, 
            "TestService", 
            failureThreshold: 2,
            openDuration: TimeSpan.FromMilliseconds(100));

        breaker.RecordFailure();
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Open, breaker.State);

        // Act
        await Task.Delay(150);

        // Assert
        Assert.Equal(CircuitState.HalfOpen, breaker.State);
        Assert.True(breaker.IsAllowed());
    }

    [Fact]
    public void HalfOpen_SuccessfulRequest_ClosesCircuit()
    {
        // Arrange
        var breaker = new CircuitBreaker(
            _logger, 
            "TestService", 
            failureThreshold: 2,
            openDuration: TimeSpan.FromMilliseconds(1));

        breaker.RecordFailure();
        breaker.RecordFailure();
        
        // Wait for half-open
        Thread.Sleep(10);
        _ = breaker.State; // Trigger state transition check

        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        // Act
        breaker.RecordSuccess();

        // Assert
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public void HalfOpen_FailedRequest_ReopensCircuit()
    {
        // Arrange
        var breaker = new CircuitBreaker(
            _logger, 
            "TestService", 
            failureThreshold: 2,
            openDuration: TimeSpan.FromMilliseconds(1));

        breaker.RecordFailure();
        breaker.RecordFailure();
        
        // Wait for half-open
        Thread.Sleep(10);
        _ = breaker.State; // Trigger state transition check

        Assert.Equal(CircuitState.HalfOpen, breaker.State);

        // Act
        breaker.RecordFailure();

        // Assert
        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_Success_RecordsSuccess()
    {
        // Arrange
        var breaker = new CircuitBreaker(_logger, "TestService");
        breaker.RecordFailure(); // Add a failure first

        // Act
        var result = await breaker.ExecuteAsync(
            async ct => { await Task.Delay(1, ct); return "success"; });

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(0, breaker.FailureCount);
    }

    [Fact]
    public async Task ExecuteAsync_HttpRequestException_RecordsFailure()
    {
        // Arrange
        var breaker = new CircuitBreaker(_logger, "TestService");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            breaker.ExecuteAsync<string>(ct => throw new HttpRequestException("Test error")));

        Assert.Equal(1, breaker.FailureCount);
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutException_RecordsFailure()
    {
        // Arrange
        var breaker = new CircuitBreaker(_logger, "TestService");

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            breaker.ExecuteAsync<string>(ct => throw new TimeoutException("Test timeout")));

        Assert.Equal(1, breaker.FailureCount);
    }

    [Fact]
    public async Task ExecuteAsync_OperationCanceledException_DoesNotRecordFailure()
    {
        // Arrange
        var breaker = new CircuitBreaker(_logger, "TestService");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            breaker.ExecuteAsync<string>(ct => throw new OperationCanceledException(), cts.Token));

        Assert.Equal(0, breaker.FailureCount);
    }

    [Fact]
    public async Task ExecuteAsync_CircuitOpen_ThrowsCircuitBreakerOpenException()
    {
        // Arrange
        var breaker = new CircuitBreaker(_logger, "TestService", failureThreshold: 2);
        breaker.RecordFailure();
        breaker.RecordFailure();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CircuitBreakerOpenException>(() =>
            breaker.ExecuteAsync(ct => Task.FromResult("success")));

        Assert.Contains("TestService", exception.Message);
    }

    [Fact]
    public void ServiceName_IsSetCorrectly()
    {
        // Arrange & Act
        var breaker = new CircuitBreaker(_logger, "MyProvider");

        // Assert
        Assert.Equal("MyProvider", breaker.ServiceName);
    }
}
