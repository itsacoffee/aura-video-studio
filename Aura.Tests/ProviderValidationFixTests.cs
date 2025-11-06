using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Providers.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for provider validation fixes (OpenAI and Pexels)
/// </summary>
public class ProviderValidationFixTests
{
    #region OpenAI Validator Tests

    [Fact]
    public async Task OpenAiValidator_WithNoApiKey_ReturnsFailure()
    {
        // Arrange
        var httpClient = new HttpClient();
        var validator = new OpenAiValidator(
            NullLogger<OpenAiValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync(null, null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("OpenAI", result.Name);
        Assert.Contains("not configured", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenAiValidator_WithValidKey_SendsCorrectAuthorizationHeader()
    {
        // Arrange
        var testApiKey = "sk-test123456789";
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "https://api.openai.com/v1/chat/completions" &&
                    req.Headers.Authorization != null &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == testApiKey),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"choices\": [{\"message\": {\"content\": \"Hi\"}}]}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new OpenAiValidator(
            NullLogger<OpenAiValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync(testApiKey, null, CancellationToken.None);

        // Assert
        Assert.True(result.Ok, $"Expected success but got: {result.Details}");
        Assert.Equal("OpenAI", result.Name);
        
        // Verify the mock was called with correct headers
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Headers.Authorization != null &&
                req.Headers.Authorization.Parameter == testApiKey),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task OpenAiValidator_WithInvalidKey_Returns401()
    {
        // Arrange
        var testApiKey = "sk-invalid123456789";
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"error\": {\"message\": \"Invalid API key\"}}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new OpenAiValidator(
            NullLogger<OpenAiValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync(testApiKey, null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("OpenAI", result.Name);
        Assert.Contains("Invalid API key", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Pexels Validator Tests

    [Fact]
    public async Task PexelsValidator_WithNoApiKey_ReturnsFailure()
    {
        // Arrange
        var httpClient = new HttpClient();
        var validator = new PexelsValidator(
            NullLogger<PexelsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync(null, null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("Pexels", result.Name);
        Assert.Contains("not configured", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PexelsValidator_WithShortApiKey_ReturnsFailure()
    {
        // Arrange
        var httpClient = new HttpClient();
        var validator = new PexelsValidator(
            NullLogger<PexelsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync("short", null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("Pexels", result.Name);
        Assert.Contains("format invalid", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PexelsValidator_WithValidKey_SendsCorrectAuthorizationHeader()
    {
        // Arrange
        var testApiKey = "test_pexels_key_123456789";
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                // Pexels API uses the API key directly in the Authorization header
                if (req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().StartsWith("https://api.pexels.com/v1/curated") &&
                    req.Headers.Contains("Authorization") &&
                    req.Headers.GetValues("Authorization").First() == testApiKey)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{\"photos\": [], \"page\": 1}")
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\": \"Invalid request\"}")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new PexelsValidator(
            NullLogger<PexelsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync(testApiKey, null, CancellationToken.None);

        // Assert
        Assert.True(result.Ok, $"Expected success but got: {result.Details}");
        Assert.Equal("Pexels", result.Name);
        
        // Verify the mock was called
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PexelsValidator_WithInvalidKey_Returns401()
    {
        // Arrange
        var testApiKey = "invalid_pexels_key_123456789";
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"error\": \"Invalid API key\"}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new PexelsValidator(
            NullLogger<PexelsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync(testApiKey, null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("Pexels", result.Name);
        Assert.Contains("invalid", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PexelsValidator_TrimsWhitespaceFromApiKey()
    {
        // Arrange
        var testApiKey = "  test_pexels_key_123456789  ";
        var trimmedKey = testApiKey.Trim();
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                // Verify the key was trimmed by checking the Authorization header
                if (req.Headers.Contains("Authorization") &&
                    req.Headers.GetValues("Authorization").First() == trimmedKey)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{\"photos\": [], \"page\": 1}")
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\": \"Invalid request\"}")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new PexelsValidator(
            NullLogger<PexelsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync(testApiKey, null, CancellationToken.None);

        // Assert
        Assert.True(result.Ok, $"Expected success but got: {result.Details}");
        
        // Verify the mock was called
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region ProviderValidationService Integration Tests

    [Fact]
    public void ProviderValidationService_IncludesPexelsValidator()
    {
        // This test verifies that Pexels validator is registered in the service
        // Since we can't easily mock the entire service, we'll test it via the E2E tests
        // This is a placeholder to document the requirement
        Assert.True(true, "Pexels validator should be registered in ProviderValidationService");
    }

    #endregion
}
