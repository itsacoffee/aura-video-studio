using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class ProviderPingServiceTests
{
    private readonly Mock<ILogger<ProviderPingService>> _logger = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<IKeyStore> _keyStore = new();

    [Fact]
    public async Task PingAsync_ReturnsMissingKey_WhenKeyNotConfigured()
    {
        // Arrange
        _keyStore.Setup(k => k.GetKey("OpenAI")).Returns((string?)null);
        var service = CreateService();

        // Act
        var result = await service.PingAsync("openai", null, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.False(result.Attempted);
        Assert.False(result.Success);
        Assert.Equal(ProviderPingErrorCodes.MissingApiKey, result.ErrorCode);
        Assert.Contains("OpenAI", result.Message);
    }

    [Fact]
    public async Task PingAsync_OpenAiSuccess_ReturnsSuccess()
    {
        // Arrange
        _keyStore.Setup(k => k.GetKey("OpenAI")).Returns("sk-test");
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        _httpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        var service = CreateService();

        // Act
        var result = await service.PingAsync("openai", null, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.True(result.Attempted);
        Assert.True(result.Success);
        Assert.Equal(ProviderPingErrorCodes.Success, result.ErrorCode);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task PingAsync_OpenAiInvalidKey_ReturnsInvalidKey()
    {
        // Arrange
        _keyStore.Setup(k => k.GetKey("OpenAI")).Returns("sk-test");
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        _httpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler.Object));

        var service = CreateService();

        // Act
        var result = await service.PingAsync("openai", null, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.True(result.Attempted);
        Assert.False(result.Success);
        Assert.Equal(ProviderPingErrorCodes.InvalidApiKey, result.ErrorCode);
        Assert.Equal(401, result.StatusCode);
    }

    private ProviderPingService CreateService()
    {
        return new ProviderPingService(_logger.Object, _httpClientFactory.Object, _keyStore.Object);
    }
}

