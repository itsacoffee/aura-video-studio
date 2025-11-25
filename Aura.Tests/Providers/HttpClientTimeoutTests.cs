using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests.Providers;

/// <summary>
/// Tests for HttpClient timeout configuration to ensure Ollama providers
/// have proper timeout settings to accommodate slow local model generation.
/// 
/// The root cause of "fails after a few minutes" issue was HttpClient's
/// default 100-second timeout killing connections before the provider's
/// 15-minute timeout was reached.
/// </summary>
public class HttpClientTimeoutTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public HttpClientTimeoutTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
    }

    [Fact]
    public void OllamaScriptProvider_HttpClient_Timeout_ExceedsProviderTimeout()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(100) // Default timeout
        };

        var logger = new Mock<ILogger<OllamaScriptProvider>>();

        // Act
        var provider = new OllamaScriptProvider(
            logger.Object,
            httpClient,
            timeoutSeconds: 900 // 15 minutes - provider timeout
        );

        // Assert
        Assert.True(
            httpClient.Timeout >= TimeSpan.FromSeconds(900),
            $"HttpClient timeout ({httpClient.Timeout.TotalSeconds}s) should be >= provider timeout (900s)"
        );
    }

    [Fact]
    public void OllamaScriptProvider_HttpClient_Timeout_LogsWarning_WhenIncreased()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(100) // Default short timeout
        };

        var logger = new Mock<ILogger<OllamaScriptProvider>>();

        // Act
        var provider = new OllamaScriptProvider(logger.Object, httpClient);

        // Assert - verify warning was logged about timeout increase
        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HttpClient timeout")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log a warning when HttpClient timeout is increased");
    }

    [Fact]
    public void OllamaScriptProvider_HttpClient_Timeout_NoChange_WhenAlreadyLonger()
    {
        // Arrange - HttpClient already has longer timeout than provider
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(20) // Already longer than provider timeout
        };

        var logger = new Mock<ILogger<OllamaScriptProvider>>();

        // Act
        var provider = new OllamaScriptProvider(
            logger.Object,
            httpClient,
            timeoutSeconds: 900 // 15 minutes
        );

        // Assert - timeout should remain unchanged
        Assert.Equal(TimeSpan.FromMinutes(20), httpClient.Timeout);
    }

    [Fact]
    public void OllamaLlmProvider_HttpClient_Timeout_ExceedsProviderTimeout()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(100) // Default timeout
        };

        var logger = new Mock<ILogger<OllamaLlmProvider>>();

        // Act
        var provider = new OllamaLlmProvider(
            logger.Object,
            httpClient,
            timeoutSeconds: 900 // 15 minutes - provider timeout
        );

        // Assert
        Assert.True(
            httpClient.Timeout >= TimeSpan.FromSeconds(900),
            $"HttpClient timeout ({httpClient.Timeout.TotalSeconds}s) should be >= provider timeout (900s)"
        );
    }

    [Fact]
    public void OllamaLlmProvider_HttpClient_Timeout_LogsWarning_WhenIncreased()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(100) // Default short timeout
        };

        var logger = new Mock<ILogger<OllamaLlmProvider>>();

        // Act
        var provider = new OllamaLlmProvider(logger.Object, httpClient);

        // Assert - verify warning was logged about timeout increase
        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HttpClient timeout")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log a warning when HttpClient timeout is increased");
    }

    [Fact]
    public void OllamaLlmProvider_HttpClient_Timeout_NoChange_WhenAlreadyLonger()
    {
        // Arrange - HttpClient already has longer timeout than provider
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(25) // Already longer than provider timeout + buffer
        };

        var logger = new Mock<ILogger<OllamaLlmProvider>>();

        // Act
        var provider = new OllamaLlmProvider(
            logger.Object,
            httpClient,
            timeoutSeconds: 900 // 15 minutes
        );

        // Assert - timeout should remain unchanged
        Assert.Equal(TimeSpan.FromMinutes(25), httpClient.Timeout);
    }

    [Fact]
    public void OllamaLlmProvider_Default_Timeout_Is15Minutes()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(25)
        };

        var logger = new Mock<ILogger<OllamaLlmProvider>>();

        // Act
        var provider = new OllamaLlmProvider(logger.Object, httpClient);

        // Assert - verify default provider timeout is 15 minutes (900 seconds)
        // The provider should have a 15-minute internal timeout by default
        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("providerTimeout=900s")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log provider timeout as 900s (15 minutes)");
    }

    [Fact]
    public void OllamaScriptProvider_Default_Timeout_Is15Minutes()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(25)
        };

        var logger = new Mock<ILogger<OllamaScriptProvider>>();

        // Act
        var provider = new OllamaScriptProvider(logger.Object, httpClient);

        // Assert - verify default provider timeout is 15 minutes (900 seconds)
        // The provider should have a 15-minute internal timeout by default
        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("timeout=900s")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log provider timeout as 900s (15 minutes)");
    }

    [Fact]
    public void HttpClient_Timeout_Buffer_IsFiveMinutes()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(100) // Short timeout
        };

        var providerTimeout = 900; // 15 minutes
        var expectedMinimumTimeout = providerTimeout + 300; // 20 minutes (15 + 5 buffer)

        var logger = new Mock<ILogger<OllamaLlmProvider>>();

        // Act
        var provider = new OllamaLlmProvider(
            logger.Object,
            httpClient,
            timeoutSeconds: providerTimeout
        );

        // Assert - HttpClient timeout should be at least provider timeout + 5 minute buffer
        Assert.True(
            httpClient.Timeout.TotalSeconds >= expectedMinimumTimeout,
            $"HttpClient timeout ({httpClient.Timeout.TotalSeconds}s) should be >= {expectedMinimumTimeout}s (provider timeout + 5 min buffer)"
        );
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
