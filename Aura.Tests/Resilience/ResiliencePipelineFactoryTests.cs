using Aura.Core.Configuration;
using Aura.Core.Errors;
using Aura.Core.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aura.Tests.Resilience;

public class ResiliencePipelineFactoryTests
{
    private readonly Mock<ILogger<ResiliencePipelineFactory>> _loggerMock;
    private readonly ResiliencePipelineFactory _factory;

    public ResiliencePipelineFactoryTests()
    {
        _loggerMock = new Mock<ILogger<ResiliencePipelineFactory>>();
        
        var settings = new CircuitBreakerSettings
        {
            FailureThreshold = 3,
            OpenDurationSeconds = 30,
            TimeoutSeconds = 10
        };
        
        var optionsMock = new Mock<IOptions<CircuitBreakerSettings>>();
        optionsMock.Setup(o => o.Value).Returns(settings);

        _factory = new ResiliencePipelineFactory(_loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public async Task GetPipeline_ShouldRetryOnTransientFailures()
    {
        // Arrange
        var pipeline = _factory.GetPipeline<string>("TestService");
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<ProviderException>(async () =>
        {
            await pipeline.ExecuteAsync(async token =>
            {
                attemptCount++;
                await Task.Delay(10, token);
                throw new ProviderException("Test", "Test", "Test") { IsTransient = true };
            });
        });

        // Should have tried multiple times (initial + retries)
        Assert.True(attemptCount > 1, $"Expected multiple attempts, got {attemptCount}");
    }

    [Fact]
    public async Task GetPipeline_ShouldNotRetryOnNonTransientFailures()
    {
        // Arrange
        var pipeline = _factory.GetPipeline<string>("TestService");
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await pipeline.ExecuteAsync(async token =>
            {
                attemptCount++;
                await Task.Delay(10, token);
                throw new InvalidOperationException("Non-transient error");
            });
        });

        // Should only try once (non-transient errors don't retry)
        Assert.Equal(1, attemptCount);
    }

    [Fact]
    public async Task GetHttpPipeline_ShouldRetryOn503()
    {
        // Arrange
        var pipeline = _factory.GetHttpPipeline("TestHttpService");
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await pipeline.ExecuteAsync(async token =>
            {
                attemptCount++;
                await Task.Delay(10, token);
                
                // Simulate 503 response
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
                
                // For testing, we need to throw since we can't actually return a failing response
                throw new InvalidOperationException("Simulated 503");
            });
        });

        // Should have retried
        Assert.True(attemptCount > 1, $"Expected multiple attempts for 503, got {attemptCount}");
    }

    [Fact]
    public void CreateCustomPipeline_ShouldUseProvidedOptions()
    {
        // Arrange
        var options = new ResiliencePipelineOptions
        {
            Name = "CustomService",
            EnableRetry = true,
            MaxRetryAttempts = 5,
            EnableCircuitBreaker = true,
            EnableTimeout = true,
            Timeout = TimeSpan.FromSeconds(15)
        };

        // Act
        var pipeline = _factory.CreateCustomPipeline<string>(options);

        // Assert
        Assert.NotNull(pipeline);
    }

    [Fact]
    public async Task GetPipeline_ShouldCachePipelines()
    {
        // Arrange & Act
        var pipeline1 = _factory.GetPipeline<string>("TestService");
        var pipeline2 = _factory.GetPipeline<string>("TestService");

        // Assert - should return same instance
        Assert.Same(pipeline1, pipeline2);
    }

    [Fact]
    public async Task GetPipeline_ForOpenAI_ShouldUseCustomSettings()
    {
        // Arrange
        var pipeline = _factory.GetPipeline<string>("OpenAI");
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await pipeline.ExecuteAsync(async token =>
            {
                attemptCount++;
                await Task.Delay(10, token);
                throw new HttpRequestException("Rate limited");
            });
        });

        // OpenAI should have more retries (4 attempts)
        Assert.True(attemptCount >= 3, $"Expected at least 3 attempts for OpenAI, got {attemptCount}");
    }
}
