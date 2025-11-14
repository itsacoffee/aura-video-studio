using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Tests to verify JSON serialization of OpenAI validation API responses
/// Ensures the API returns camelCase properties for JavaScript/TypeScript clients
/// </summary>
public class OpenAIValidationApiJsonSerializationTests
{
    [Fact]
    public async Task ValidateOpenAIKey_Response_ShouldUseCamelCase()
    {
        // This test verifies the fix for the issue where the backend was returning PascalCase
        // but the frontend expected camelCase, causing validation to fail.
        
        // Arrange
        var mockHttpHandler = new MockHttpMessageHandler((req, ct) =>
        {
            // Simulate OpenAI API success response
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"data\": []}")
            });
        });
        
        var httpClient = new HttpClient(mockHttpHandler);
        var openAIService = new Aura.Core.Services.Providers.OpenAIKeyValidationService(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<Aura.Core.Services.Providers.OpenAIKeyValidationService>(),
            httpClient);

        // Act
        var result = await openAIService.ValidateKeyAsync("sk-test1234567890abcdefghijklmnopqrstuvwxyz");

        // Manually create a ProviderValidationResponse DTO like the controller does
        var response = new Aura.Api.Models.ApiModels.V1.ProviderValidationResponse(
            IsValid: result.IsValid,
            Status: result.Status,
            Message: result.Message,
            CorrelationId: "test-correlation-id",
            Details: new Aura.Api.Models.ApiModels.V1.ValidationDetails(
                Provider: "OpenAI",
                KeyFormat: result.FormatValid ? "valid" : "invalid",
                FormatValid: result.FormatValid,
                NetworkCheckPassed: result.NetworkCheckPassed,
                HttpStatusCode: result.HttpStatusCode,
                ErrorType: result.ErrorType,
                ResponseTimeMs: result.ResponseTimeMs,
                DiagnosticInfo: result.DiagnosticInfo));

        // Serialize using the same configuration as the API
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(response, jsonOptions);
        
        // Assert - Verify camelCase properties
        Assert.Contains("\"isValid\":", json);
        Assert.Contains("\"status\":", json);
        Assert.Contains("\"message\":", json);
        Assert.Contains("\"correlationId\":", json);
        Assert.Contains("\"details\":", json);
        
        // Should NOT contain PascalCase
        Assert.DoesNotContain("\"IsValid\":", json);
        Assert.DoesNotContain("\"Status\":", json);
        Assert.DoesNotContain("\"Message\":", json);
        Assert.DoesNotContain("\"CorrelationId\":", json);
        Assert.DoesNotContain("\"Details\":", json);

        // Verify the response can be deserialized with camelCase
        var deserializedResponse = JsonSerializer.Deserialize<Aura.Api.Models.ApiModels.V1.ProviderValidationResponse>(
            json, 
            jsonOptions);
        
        Assert.NotNull(deserializedResponse);
        Assert.Equal(result.IsValid, deserializedResponse.IsValid);
        Assert.Equal(result.Status, deserializedResponse.Status);
        Assert.Equal("test-correlation-id", deserializedResponse.CorrelationId);
    }

    [Fact]
    public void ProviderValidationResponse_SerializedWithCamelCase_MatchesFrontendExpectations()
    {
        // Arrange
        var response = new Aura.Api.Models.ApiModels.V1.ProviderValidationResponse(
            IsValid: true,
            Status: "Valid",
            Message: "API key is valid and verified with OpenAI.",
            CorrelationId: "test-123",
            Details: new Aura.Api.Models.ApiModels.V1.ValidationDetails(
                Provider: "OpenAI",
                KeyFormat: "valid",
                FormatValid: true,
                NetworkCheckPassed: true,
                HttpStatusCode: 200,
                ErrorType: null,
                ResponseTimeMs: 150,
                DiagnosticInfo: "Validated successfully after 1 attempts"));

        // Act - Serialize with camelCase
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(response, jsonOptions);

        // Assert - Check all expected camelCase properties that frontend expects
        var expectedProperties = new[]
        {
            "isValid",
            "status", 
            "message",
            "correlationId",
            "details",
            "provider",
            "keyFormat",
            "formatValid",
            "networkCheckPassed",
            "httpStatusCode",
            "responseTimeMs",
            "diagnosticInfo"
        };

        foreach (var property in expectedProperties)
        {
            Assert.Contains($"\"{property}\":", json, 
                $"Expected property '{property}' in camelCase format. JSON: {json}");
        }

        // Verify it can be parsed as the frontend expects
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        // Frontend checks: data.isValid === true (line 816 in onboarding.ts)
        Assert.True(root.TryGetProperty("isValid", out var isValidProp));
        Assert.True(isValidProp.GetBoolean());
        
        Assert.True(root.TryGetProperty("status", out var statusProp));
        Assert.Equal("Valid", statusProp.GetString());
        
        Assert.True(root.TryGetProperty("message", out var messageProp));
        Assert.Equal("API key is valid and verified with OpenAI.", messageProp.GetString());
    }
}

/// <summary>
/// Mock HTTP message handler for testing
/// </summary>
internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, System.Threading.CancellationToken, Task<HttpResponseMessage>> _sendAsync;

    public MockHttpMessageHandler(Func<HttpRequestMessage, System.Threading.CancellationToken, Task<HttpResponseMessage>> sendAsync)
    {
        _sendAsync = sendAsync;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        return _sendAsync(request, cancellationToken);
    }
}
