using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.HostedServices;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class OllamaHealthCheckServiceTests
{
    [Fact]
    public async Task CheckNowAsync_WhenOllamaAvailable_ReturnsTrue()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/api/tags")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"models\":[]}")
            });

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(mockHttpMessageHandler.Object));

        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            mockHttpClientFactory.Object
        );

        // Act
        var result = await service.CheckNowAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(service.IsOllamaAvailable);
        Assert.True(service.LastCheckTime > DateTime.MinValue);
    }

    [Fact]
    public async Task CheckNowAsync_WhenOllamaNotAvailable_ReturnsFalse()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(mockHttpMessageHandler.Object));

        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            mockHttpClientFactory.Object
        );

        // Act
        var result = await service.CheckNowAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.False(service.IsOllamaAvailable);
        Assert.True(service.LastCheckTime > DateTime.MinValue);
    }

    [Fact]
    public async Task IsOllamaAvailable_InitiallyFalse()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new Mock<HttpMessageHandler>().Object));

        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            mockHttpClientFactory.Object
        );

        // Assert
        Assert.False(service.IsOllamaAvailable);
        Assert.Equal(DateTime.MinValue, service.LastCheckTime);
    }
}
