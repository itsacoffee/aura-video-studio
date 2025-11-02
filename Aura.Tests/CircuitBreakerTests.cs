using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Services.Health;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class CircuitBreakerTests
{
    private readonly ILogger _logger;
    private readonly CircuitBreakerSettings _settings;

    public CircuitBreakerTests()
    {
        _logger = NullLogger.Instance;
        _settings = new CircuitBreakerSettings
        {
            FailureThreshold = 3,
            FailureRateThreshold = 0.5,
            OpenDurationSeconds = 1, // Short duration for tests
            TimeoutSeconds = 10,
            HealthCheckTimeoutSeconds = 2,
            RollingWindowSize = 10,
            RollingWindowMinutes = 5
        };
    }

    [Fact]
    public void CircuitBreaker_InitialState_IsClosed()
    {
        // Arrange & Act
        var breaker = new CircuitBreaker("TestProvider", _settings, _logger);

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, breaker.State);
        Assert.Equal(0, breaker.ConsecutiveFailures);
    }

    [Fact]
    public async Task ExecuteAsync_Success_KeepsCircuitClosed()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _settings, _logger);
        Func<CancellationToken, Task<string>> action = _ => Task.FromResult("success");

        // Act
        var result = await breaker.ExecuteAsync(action);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(CircuitBreakerState.Closed, breaker.State);
        Assert.Equal(0, breaker.ConsecutiveFailures);
    }

    [Fact]
    public async Task ExecuteAsync_ConsecutiveFailures_OpensCircuit()
    {
        // Arrange - Use manual failure recording to avoid CircuitBreakerOpenException
        var settings = new CircuitBreakerSettings
        {
            FailureThreshold = 3,
            FailureRateThreshold = 1.0, // Set high so only consecutive failures trigger
            OpenDurationSeconds = 1,
            RollingWindowSize = 10,
            RollingWindowMinutes = 5
        };
        var breaker = new CircuitBreaker("TestProvider", settings, _logger);

        // Act - Record failures manually to reach threshold
        for (int i = 0; i < settings.FailureThreshold; i++)
        {
            await breaker.RecordFailureAsync(new Exception("Test failure"), CancellationToken.None);
        }

        // Assert
        Assert.Equal(CircuitBreakerState.Open, breaker.State);
        Assert.Equal(settings.FailureThreshold, breaker.ConsecutiveFailures);
    }

    [Fact]
    public async Task ExecuteAsync_CircuitOpen_ThrowsCircuitBreakerOpenException()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _settings, _logger);
        Func<CancellationToken, Task<string>> failingAction = _ => throw new Exception("Test failure");

        // Open the circuit
        for (int i = 0; i < _settings.FailureThreshold; i++)
        {
            try
            {
                await breaker.ExecuteAsync(failingAction);
            }
            catch (Exception)
            {
                // Expected
            }
        }

        // Act & Assert
        Func<CancellationToken, Task<string>> action = _ => Task.FromResult("success");
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => breaker.ExecuteAsync(action));
    }

    [Fact]
    public async Task CircuitBreaker_OpenToHalfOpen_AfterCooldown()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _settings, _logger);
        Func<CancellationToken, Task<string>> failingAction = _ => throw new Exception("Test failure");

        // Open the circuit
        for (int i = 0; i < _settings.FailureThreshold; i++)
        {
            try
            {
                await breaker.ExecuteAsync(failingAction);
            }
            catch (Exception)
            {
                // Expected
            }
        }

        Assert.Equal(CircuitBreakerState.Open, breaker.State);

        // Wait for cooldown
        await Task.Delay(TimeSpan.FromSeconds(_settings.OpenDurationSeconds + 0.5));

        // Act - Try to execute (should transition to half-open)
        Func<CancellationToken, Task<string>> successAction = _ => Task.FromResult("success");
        var result = await breaker.ExecuteAsync(successAction);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(CircuitBreakerState.Closed, breaker.State);
        Assert.Equal(0, breaker.ConsecutiveFailures);
    }

    [Fact]
    public async Task CircuitBreaker_HalfOpenToOpen_OnFailure()
    {
        // Arrange
        var settings = new CircuitBreakerSettings
        {
            FailureThreshold = 2,
            FailureRateThreshold = 0.5,
            OpenDurationSeconds = 1,
            RollingWindowSize = 10,
            RollingWindowMinutes = 5
        };
        var breaker = new CircuitBreaker("TestProvider", settings, _logger);
        Func<CancellationToken, Task<string>> failingAction = _ => throw new Exception("Test failure");

        // Open the circuit with exactly threshold failures
        for (int i = 0; i < settings.FailureThreshold; i++)
        {
            try
            {
                await breaker.ExecuteAsync(failingAction);
            }
            catch (Exception)
            {
                // Expected
            }
        }

        Assert.Equal(CircuitBreakerState.Open, breaker.State);

        // Wait for cooldown to half-open
        await Task.Delay(TimeSpan.FromSeconds(settings.OpenDurationSeconds + 0.5));

        // Act - Fail in half-open state
        try
        {
            await breaker.ExecuteAsync(failingAction);
        }
        catch (Exception)
        {
            // Expected
        }

        // Assert - Should go back to Open
        Assert.Equal(CircuitBreakerState.Open, breaker.State);
    }

    [Fact]
    public async Task GetFailureRate_CalculatesCorrectly()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _settings, _logger);
        Func<CancellationToken, Task<string>> successAction = _ => Task.FromResult("success");
        Func<CancellationToken, Task<string>> failingAction = _ => throw new Exception("Test failure");

        // Act - 3 successes, 2 failures
        await breaker.ExecuteAsync(successAction);
        await breaker.ExecuteAsync(successAction);
        await breaker.ExecuteAsync(successAction);
        try { await breaker.ExecuteAsync(failingAction); } catch { }
        try { await breaker.ExecuteAsync(failingAction); } catch { }

        var failureRate = breaker.GetFailureRate();

        // Assert
        Assert.Equal(0.4, failureRate, 2); // 2 failures out of 5 = 40%
    }

    [Fact]
    public async Task CircuitBreaker_OpensOnFailureRate()
    {
        // Arrange
        var settings = new CircuitBreakerSettings
        {
            FailureThreshold = 10, // High threshold so consecutive failures don't trigger
            FailureRateThreshold = 0.5, // 50% failure rate
            OpenDurationSeconds = 1,
            RollingWindowSize = 10,
            RollingWindowMinutes = 5
        };
        var breaker = new CircuitBreaker("TestProvider", settings, _logger);
        Func<CancellationToken, Task<string>> successAction = _ => Task.FromResult("success");
        Func<CancellationToken, Task<string>> failingAction = _ => throw new Exception("Test failure");

        // Act - Create 60% failure rate (3 failures, 2 successes)
        await breaker.ExecuteAsync(successAction);
        await breaker.ExecuteAsync(successAction);
        try { await breaker.ExecuteAsync(failingAction); } catch { }
        try { await breaker.ExecuteAsync(failingAction); } catch { }
        try { await breaker.ExecuteAsync(failingAction); } catch { }

        // Assert
        Assert.Equal(CircuitBreakerState.Open, breaker.State);
    }

    [Fact]
    public async Task ResetAsync_ClosesCircuitAndClearsFailures()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _settings, _logger);
        Func<CancellationToken, Task<string>> failingAction = _ => throw new Exception("Test failure");

        // Open the circuit
        for (int i = 0; i < _settings.FailureThreshold; i++)
        {
            try
            {
                await breaker.ExecuteAsync(failingAction);
            }
            catch (Exception)
            {
                // Expected
            }
        }

        Assert.Equal(CircuitBreakerState.Open, breaker.State);

        // Act
        await breaker.ResetAsync();

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, breaker.State);
        Assert.Equal(0, breaker.ConsecutiveFailures);
        Assert.Equal(0.0, breaker.GetFailureRate());
    }

    [Fact]
    public async Task RecordSuccessAsync_ResetsConsecutiveFailures()
    {
        // Arrange - Use very high thresholds to prevent circuit from opening
        var settings = new CircuitBreakerSettings
        {
            FailureThreshold = 100,  // Very high
            FailureRateThreshold = 0.99,  // 99% failure rate needed
            OpenDurationSeconds = 1,
            RollingWindowSize = 100,
            RollingWindowMinutes = 5
        };
        var breaker = new CircuitBreaker("TestProvider", settings, _logger);
        
        // Record some failures (way below threshold)
        await breaker.RecordFailureAsync(new Exception("Test"), CancellationToken.None);
        await breaker.RecordFailureAsync(new Exception("Test"), CancellationToken.None);
        
        Assert.Equal(2, breaker.ConsecutiveFailures);

        // Act
        await breaker.RecordSuccessAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, breaker.ConsecutiveFailures);
    }

    [Fact]
    public async Task CircuitBreaker_RollingWindow_DiscardsOldFailures()
    {
        // Arrange
        var settings = new CircuitBreakerSettings
        {
            FailureThreshold = 10,
            FailureRateThreshold = 0.5,
            OpenDurationSeconds = 1,
            RollingWindowSize = 3, // Small window for testing
            RollingWindowMinutes = 5
        };
        var breaker = new CircuitBreaker("TestProvider", settings, _logger);
        Func<CancellationToken, Task<string>> successAction = _ => Task.FromResult("success");
        Func<CancellationToken, Task<string>> failingAction = _ => throw new Exception("Test failure");

        // Act - Record failures manually first
        await breaker.RecordFailureAsync(new Exception("Test"), CancellationToken.None);
        await breaker.RecordFailureAsync(new Exception("Test"), CancellationToken.None);
        await breaker.RecordFailureAsync(new Exception("Test"), CancellationToken.None);
        
        // Now add successes (should push out old failures from rolling window)
        await breaker.RecordSuccessAsync(CancellationToken.None);
        await breaker.RecordSuccessAsync(CancellationToken.None);
        await breaker.RecordSuccessAsync(CancellationToken.None);

        var failureRate = breaker.GetFailureRate();

        // Assert - All failures should be out of window
        Assert.Equal(0.0, failureRate);
    }
}
