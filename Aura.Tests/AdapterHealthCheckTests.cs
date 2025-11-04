using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Adapters;
using Aura.Core.Configuration;
using Aura.Core.Services.Health;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class AdapterHealthCheckTests
{
    [Fact]
    public async Task OpenAiAdapter_HealthCheck_ReturnsHealthyForValidModel()
    {
        // Arrange
        var adapter = new OpenAiAdapter(NullLogger<OpenAiAdapter>.Instance, "gpt-4o-mini");

        // Act
        var result = await adapter.HealthCheckAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsHealthy);
        Assert.True(result.ResponseTimeMs >= 0);
        Assert.Null(result.ErrorMessage);
        Assert.Contains("available", result.Details ?? string.Empty);
    }

    [Fact]
    public async Task OpenAiAdapter_HealthCheck_ReturnsUnhealthyForInvalidModel()
    {
        // Arrange
        var adapter = new OpenAiAdapter(NullLogger<OpenAiAdapter>.Instance, "nonexistent-model");

        // Act
        var result = await adapter.HealthCheckAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Contains("not found", result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task AnthropicAdapter_HealthCheck_ReturnsHealthyForValidModel()
    {
        // Arrange
        var adapter = new AnthropicAdapter(NullLogger<AnthropicAdapter>.Instance, "claude-3-5-sonnet-20241022");

        // Act
        var result = await adapter.HealthCheckAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsHealthy);
        Assert.True(result.ResponseTimeMs >= 0);
    }

    [Fact]
    public async Task GeminiAdapter_HealthCheck_ReturnsHealthyForValidModel()
    {
        // Arrange
        var adapter = new GeminiAdapter(NullLogger<GeminiAdapter>.Instance, "gemini-1.5-pro");

        // Act
        var result = await adapter.HealthCheckAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsHealthy);
        Assert.True(result.ResponseTimeMs >= 0);
    }

    [Fact]
    public async Task AzureOpenAiAdapter_HealthCheck_ReturnsHealthyForValidModel()
    {
        // Arrange
        var adapter = new AzureOpenAiAdapter(NullLogger<AzureOpenAiAdapter>.Instance, "gpt-4o");

        // Act
        var result = await adapter.HealthCheckAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsHealthy);
        Assert.True(result.ResponseTimeMs >= 0);
    }

    [Fact]
    public async Task OllamaAdapter_HealthCheck_ReturnsHealthyForValidModel()
    {
        // Arrange
        var adapter = new OllamaAdapter(NullLogger<OllamaAdapter>.Instance, "llama3.2");

        // Act
        var result = await adapter.HealthCheckAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsHealthy);
        Assert.True(result.ResponseTimeMs >= 0);
    }

    [Fact]
    public void Adapter_CanAttachCircuitBreaker()
    {
        // Arrange
        var adapter = new OpenAiAdapter(NullLogger<OpenAiAdapter>.Instance, "gpt-4o-mini");
        var settings = new CircuitBreakerSettings
        {
            FailureThreshold = 3,
            FailureRateThreshold = 0.5,
            OpenDurationSeconds = 60,
            TimeoutSeconds = 30,
            HealthCheckTimeoutSeconds = 5,
            RollingWindowSize = 10,
            RollingWindowMinutes = 5
        };
        var circuitBreaker = new CircuitBreaker("OpenAI", settings, NullLogger.Instance);

        // Act
        adapter.CircuitBreaker = circuitBreaker;

        // Assert
        Assert.NotNull(adapter.CircuitBreaker);
        Assert.Equal(CircuitBreakerState.Closed, adapter.CircuitBreaker.State);
    }

    [Fact]
    public void ErrorRecoveryStrategy_SupportsValidationFailureFlag()
    {
        // Arrange & Act
        var strategy = new ErrorRecoveryStrategy
        {
            ShouldRetry = true,
            RetryDelay = TimeSpan.FromSeconds(1),
            IsValidationFailure = true,
            ModifiedPrompt = "Please return valid JSON"
        };

        // Assert
        Assert.True(strategy.ShouldRetry);
        Assert.True(strategy.IsValidationFailure);
        Assert.NotNull(strategy.ModifiedPrompt);
    }

    [Fact]
    public void ProviderHealthResult_CapturesTimestamp()
    {
        // Arrange & Act
        var before = DateTime.UtcNow;
        var result = new ProviderHealthResult
        {
            IsHealthy = true,
            ResponseTimeMs = 123.45,
            Details = "Test details"
        };
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(result.Timestamp >= before);
        Assert.True(result.Timestamp <= after);
    }

    [Fact]
    public async Task HealthCheck_CompletesUnder100ms()
    {
        // Arrange
        var adapter = new OpenAiAdapter(NullLogger<OpenAiAdapter>.Instance, "gpt-4o-mini");

        // Act
        var result = await adapter.HealthCheckAsync(CancellationToken.None);

        // Assert
        Assert.True(result.ResponseTimeMs < 100, 
            $"Health check took {result.ResponseTimeMs}ms, expected under 100ms");
    }
}
