using System;
using System.Threading.Tasks;
using Aura.Core.Services.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Resilience;

public class CircuitBreakerRegistryTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly CircuitBreakerRegistry _registry;

    public CircuitBreakerRegistryTests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());
        
        _registry = new CircuitBreakerRegistry(_loggerFactoryMock.Object);
    }

    [Fact]
    public void GetOrCreate_NewService_CreatesNewBreaker()
    {
        // Act
        var breaker = _registry.GetOrCreate("OpenAI");

        // Assert
        Assert.NotNull(breaker);
        Assert.Equal("OpenAI", breaker.ServiceName);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public void GetOrCreate_SameService_ReturnsSameBreaker()
    {
        // Act
        var breaker1 = _registry.GetOrCreate("OpenAI");
        var breaker2 = _registry.GetOrCreate("OpenAI");

        // Assert
        Assert.Same(breaker1, breaker2);
    }

    [Fact]
    public void GetOrCreate_DifferentServices_ReturnsDifferentBreakers()
    {
        // Act
        var openAiBreaker = _registry.GetOrCreate("OpenAI");
        var elevenLabsBreaker = _registry.GetOrCreate("ElevenLabs");

        // Assert
        Assert.NotSame(openAiBreaker, elevenLabsBreaker);
        Assert.Equal("OpenAI", openAiBreaker.ServiceName);
        Assert.Equal("ElevenLabs", elevenLabsBreaker.ServiceName);
    }

    [Fact]
    public void GetOrCreate_CustomSettings_AppliesSettings()
    {
        // Act
        var breaker = _registry.GetOrCreate(
            "OpenAI",
            failureThreshold: 10,
            openDuration: TimeSpan.FromMinutes(5));

        // Assert
        Assert.NotNull(breaker);
        
        // Record 9 failures - should still be closed
        for (int i = 0; i < 9; i++)
        {
            breaker.RecordFailure();
        }
        Assert.Equal(CircuitState.Closed, breaker.State);

        // Record 10th failure - should open
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Open, breaker.State);
    }

    [Fact]
    public void GetState_ExistingService_ReturnsState()
    {
        // Arrange
        var breaker = _registry.GetOrCreate("OpenAI", failureThreshold: 2);
        breaker.RecordFailure();
        breaker.RecordFailure();

        // Act
        var state = _registry.GetState("OpenAI");

        // Assert
        Assert.Equal(CircuitState.Open, state);
    }

    [Fact]
    public void GetState_NonExistingService_ReturnsClosed()
    {
        // Act
        var state = _registry.GetState("NonExistent");

        // Assert
        Assert.Equal(CircuitState.Closed, state);
    }

    [Fact]
    public void Reset_ExistingService_ResetsBreaker()
    {
        // Arrange
        var breaker = _registry.GetOrCreate("OpenAI", failureThreshold: 2);
        breaker.RecordFailure();
        breaker.RecordFailure();
        Assert.Equal(CircuitState.Open, breaker.State);

        // Act
        var result = _registry.Reset("OpenAI");

        // Assert
        Assert.True(result);
        Assert.Equal(CircuitState.Closed, breaker.State);
    }

    [Fact]
    public void Reset_NonExistingService_ReturnsFalse()
    {
        // Act
        var result = _registry.Reset("NonExistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ResetAll_ResetsAllBreakers()
    {
        // Arrange
        var openAiBreaker = _registry.GetOrCreate("OpenAI", failureThreshold: 2);
        var elevenLabsBreaker = _registry.GetOrCreate("ElevenLabs", failureThreshold: 2);

        openAiBreaker.RecordFailure();
        openAiBreaker.RecordFailure();
        elevenLabsBreaker.RecordFailure();
        elevenLabsBreaker.RecordFailure();

        Assert.Equal(CircuitState.Open, openAiBreaker.State);
        Assert.Equal(CircuitState.Open, elevenLabsBreaker.State);

        // Act
        _registry.ResetAll();

        // Assert
        Assert.Equal(CircuitState.Closed, openAiBreaker.State);
        Assert.Equal(CircuitState.Closed, elevenLabsBreaker.State);
    }

    [Fact]
    public void GetAllStates_ReturnsAllServiceStates()
    {
        // Arrange
        var openAiBreaker = _registry.GetOrCreate("OpenAI", failureThreshold: 2);
        var elevenLabsBreaker = _registry.GetOrCreate("ElevenLabs", failureThreshold: 2);
        
        openAiBreaker.RecordFailure();
        openAiBreaker.RecordFailure();

        // Act
        var states = _registry.GetAllStates();

        // Assert
        Assert.Equal(2, states.Count);
        Assert.Equal(CircuitState.Open, states["OpenAI"]);
        Assert.Equal(CircuitState.Closed, states["ElevenLabs"]);
    }

    [Fact]
    public void GetAllInfo_ReturnsDetailedInfo()
    {
        // Arrange
        var breaker = _registry.GetOrCreate("OpenAI", failureThreshold: 2);
        breaker.RecordFailure();

        // Act
        var info = _registry.GetAllInfo();

        // Assert
        Assert.Single(info);
        Assert.Equal("OpenAI", info["OpenAI"].ServiceName);
        Assert.Equal(CircuitState.Closed, info["OpenAI"].State);
        Assert.Equal(1, info["OpenAI"].FailureCount);
    }

    [Fact]
    public void IsAllowed_OpenCircuit_ReturnsFalse()
    {
        // Arrange
        var breaker = _registry.GetOrCreate("OpenAI", failureThreshold: 2);
        breaker.RecordFailure();
        breaker.RecordFailure();

        // Act
        var isAllowed = _registry.IsAllowed("OpenAI");

        // Assert
        Assert.False(isAllowed);
    }

    [Fact]
    public void IsAllowed_ClosedCircuit_ReturnsTrue()
    {
        // Arrange
        _registry.GetOrCreate("OpenAI");

        // Act
        var isAllowed = _registry.IsAllowed("OpenAI");

        // Assert
        Assert.True(isAllowed);
    }

    [Fact]
    public void IsAllowed_NonExistingService_ReturnsTrue()
    {
        // Act
        var isAllowed = _registry.IsAllowed("NonExistent");

        // Assert
        Assert.True(isAllowed);
    }

    [Fact]
    public void GetRegisteredServices_ReturnsAllServiceNames()
    {
        // Arrange
        _registry.GetOrCreate("OpenAI");
        _registry.GetOrCreate("ElevenLabs");
        _registry.GetOrCreate("Ollama");

        // Act
        var services = _registry.GetRegisteredServices();

        // Assert
        Assert.Contains("OpenAI", services);
        Assert.Contains("ElevenLabs", services);
        Assert.Contains("Ollama", services);
    }
}
