using System;
using System.Collections.Generic;
using System.Net.Http;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Providers;

/// <summary>
/// Tests for Ollama provider HTTP client timeout configuration.
/// Verifies that timeout is always properly synchronized between provider and HttpClient.
/// </summary>
public class OllamaTimeoutConfigurationTests : IDisposable
{
    private readonly List<HttpClient> _httpClients = new();

    private HttpClient CreateHttpClient(TimeSpan? timeout = null)
    {
        var httpClient = new HttpClient();
        if (timeout.HasValue)
        {
            httpClient.Timeout = timeout.Value;
        }
        _httpClients.Add(httpClient);
        return httpClient;
    }

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
        {
            httpClient.Dispose();
        }
        _httpClients.Clear();
    }

    [Fact]
    public void OllamaLlmProvider_WithShortHttpClientTimeout_IncreasesTimeout()
    {
        // Arrange
        var httpClient = CreateHttpClient(TimeSpan.FromSeconds(100));
        var logger = NullLogger<OllamaLlmProvider>.Instance;

        // Act
        var provider = new OllamaLlmProvider(logger, httpClient, timeoutSeconds: 900);

        // Assert - timeout should be at least provider timeout + 300 seconds buffer
        Assert.True(
            httpClient.Timeout >= TimeSpan.FromSeconds(900 + 300),
            $"HttpClient timeout should be increased to match provider timeout + buffer. Actual: {httpClient.Timeout.TotalSeconds}s"
        );
    }

    [Fact]
    public void OllamaLlmProvider_WithInfiniteTimeout_DoesNotModify()
    {
        // Arrange
        var httpClient = CreateHttpClient(Timeout.InfiniteTimeSpan);
        var logger = NullLogger<OllamaLlmProvider>.Instance;

        // Act
        var provider = new OllamaLlmProvider(logger, httpClient, timeoutSeconds: 900);

        // Assert - infinite timeout should not be modified
        Assert.Equal(Timeout.InfiniteTimeSpan, httpClient.Timeout);
    }

    [Fact]
    public void OllamaLlmProvider_WithSufficientTimeout_DoesNotModify()
    {
        // Arrange
        var sufficientTimeout = TimeSpan.FromSeconds(900 + 300 + 100); // More than required
        var httpClient = CreateHttpClient(sufficientTimeout);
        var logger = NullLogger<OllamaLlmProvider>.Instance;

        // Act
        var provider = new OllamaLlmProvider(logger, httpClient, timeoutSeconds: 900);

        // Assert - timeout should remain unchanged
        Assert.Equal(sufficientTimeout, httpClient.Timeout);
    }

    [Fact]
    public void OllamaScriptProvider_WithShortHttpClientTimeout_IncreasesTimeout()
    {
        // Arrange
        var httpClient = CreateHttpClient(TimeSpan.FromSeconds(100));
        var logger = NullLogger<OllamaScriptProvider>.Instance;

        // Act
        var provider = new OllamaScriptProvider(logger, httpClient, timeoutSeconds: 900);

        // Assert - timeout should be at least provider timeout + 300 seconds buffer
        Assert.True(
            httpClient.Timeout >= TimeSpan.FromSeconds(900 + 300),
            $"HttpClient timeout should be increased to match provider timeout + buffer. Actual: {httpClient.Timeout.TotalSeconds}s"
        );
    }

    [Fact]
    public void OllamaScriptProvider_WithInfiniteTimeout_DoesNotModify()
    {
        // Arrange
        var httpClient = CreateHttpClient(Timeout.InfiniteTimeSpan);
        var logger = NullLogger<OllamaScriptProvider>.Instance;

        // Act
        var provider = new OllamaScriptProvider(logger, httpClient, timeoutSeconds: 900);

        // Assert - infinite timeout should not be modified
        Assert.Equal(Timeout.InfiniteTimeSpan, httpClient.Timeout);
    }

    [Fact]
    public void OllamaScriptProvider_WithSufficientTimeout_DoesNotModify()
    {
        // Arrange
        var sufficientTimeout = TimeSpan.FromSeconds(900 + 300 + 100); // More than required
        var httpClient = CreateHttpClient(sufficientTimeout);
        var logger = NullLogger<OllamaScriptProvider>.Instance;

        // Act
        var provider = new OllamaScriptProvider(logger, httpClient, timeoutSeconds: 900);

        // Assert - timeout should remain unchanged
        Assert.Equal(sufficientTimeout, httpClient.Timeout);
    }

    [Fact]
    public void OllamaLlmProvider_TimeoutBuffer_IsConsistentWith300Seconds()
    {
        // Arrange
        var httpClient = CreateHttpClient(TimeSpan.FromSeconds(50));
        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var providerTimeout = 600; // 10 minutes

        // Act
        var provider = new OllamaLlmProvider(logger, httpClient, timeoutSeconds: providerTimeout);

        // Assert - verify the 300 second (5 minute) buffer is applied
        Assert.Equal(TimeSpan.FromSeconds(providerTimeout + 300), httpClient.Timeout);
    }

    [Fact]
    public void OllamaScriptProvider_TimeoutBuffer_IsConsistentWith300Seconds()
    {
        // Arrange
        var httpClient = CreateHttpClient(TimeSpan.FromSeconds(50));
        var logger = NullLogger<OllamaScriptProvider>.Instance;
        var providerTimeout = 600; // 10 minutes

        // Act
        var provider = new OllamaScriptProvider(logger, httpClient, timeoutSeconds: providerTimeout);

        // Assert - verify the 300 second (5 minute) buffer is applied
        Assert.Equal(TimeSpan.FromSeconds(providerTimeout + 300), httpClient.Timeout);
    }

    [Fact]
    public void OllamaLlmProvider_WithDefaultTimeout_IncreasesToProviderTimeout()
    {
        // Arrange - default HttpClient timeout is 100 seconds
        var httpClient = CreateHttpClient();
        var logger = NullLogger<OllamaLlmProvider>.Instance;

        // Act
        var provider = new OllamaLlmProvider(logger, httpClient, timeoutSeconds: 900);

        // Assert - should increase default 100s to 1200s (900 + 300)
        Assert.True(
            httpClient.Timeout >= TimeSpan.FromSeconds(1200),
            $"Default HttpClient timeout should be increased. Actual: {httpClient.Timeout.TotalSeconds}s"
        );
    }

    [Fact]
    public void OllamaScriptProvider_WithDefaultTimeout_IncreasesToProviderTimeout()
    {
        // Arrange - default HttpClient timeout is 100 seconds
        var httpClient = CreateHttpClient();
        var logger = NullLogger<OllamaScriptProvider>.Instance;

        // Act
        var provider = new OllamaScriptProvider(logger, httpClient, timeoutSeconds: 900);

        // Assert - should increase default 100s to 1200s (900 + 300)
        Assert.True(
            httpClient.Timeout >= TimeSpan.FromSeconds(1200),
            $"Default HttpClient timeout should be increased. Actual: {httpClient.Timeout.TotalSeconds}s"
        );
    }
}
