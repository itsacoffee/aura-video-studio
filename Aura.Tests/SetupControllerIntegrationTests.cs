using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aura.Tests;

public class SetupControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SetupControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetKeyStatus_ReturnsProviderStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/key-status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("openai", content);
        Assert.Contains("anthropic", content);
        Assert.Contains("gemini", content);
    }

    [Fact]
    public async Task GetProviderInfo_OpenAI_ReturnsProviderDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/provider-info/openai");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var providerInfo = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        Assert.NotNull(providerInfo);
        Assert.True(providerInfo.ContainsKey("name"));
        Assert.True(providerInfo.ContainsKey("signupUrl"));
        Assert.True(providerInfo.ContainsKey("pricingUrl"));
        Assert.True(providerInfo.ContainsKey("docsUrl"));
    }

    [Fact]
    public async Task GetProviderInfo_Anthropic_ReturnsProviderDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/provider-info/anthropic");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Anthropic", content);
        Assert.Contains("Claude", content);
    }

    [Fact]
    public async Task GetProviderInfo_Gemini_ReturnsProviderDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/provider-info/gemini");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Gemini", content);
    }

    [Fact]
    public async Task GetProviderInfo_ElevenLabs_ReturnsProviderDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/provider-info/elevenlabs");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ElevenLabs", content);
    }

    [Fact]
    public async Task GetProviderInfo_PlayHT_ReturnsProviderDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/provider-info/playht");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("PlayHT", content);
    }

    [Fact]
    public async Task GetProviderInfo_Replicate_ReturnsProviderDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/provider-info/replicate");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Replicate", content);
    }

    [Fact]
    public async Task GetProviderInfo_UnknownProvider_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/provider-info/unknown");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SaveApiKeys_ValidKeys_ReturnsSuccess()
    {
        // Arrange
        var keys = new Dictionary<string, string>
        {
            { "openai", "test-openai-key-" + Guid.NewGuid() },
            { "gemini", "test-gemini-key-" + Guid.NewGuid() }
        };

        var request = new { Keys = keys };

        // Act
        var response = await _client.PostAsJsonAsync("/api/setup/save-api-keys", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"success\":true", content);
    }

    [Fact]
    public async Task SaveApiKeys_EmptyKeys_ReturnsBadRequest()
    {
        // Arrange
        var request = new { Keys = new Dictionary<string, string>() };

        // Act
        var response = await _client.PostAsJsonAsync("/api/setup/save-api-keys", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteApiKey_ValidProvider_ReturnsSuccess()
    {
        // Arrange - First save a key
        var providerName = "test-provider-" + Guid.NewGuid();
        var keys = new Dictionary<string, string>
        {
            { providerName, "test-key" }
        };
        await _client.PostAsJsonAsync("/api/setup/save-api-keys", new { Keys = keys });

        // Act
        var response = await _client.DeleteAsync($"/api/setup/delete-key/{providerName}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"success\":true", content);
    }

    [Fact]
    public async Task ValidateKey_OpenAI_WithEmptyKey_ReturnsFailure()
    {
        // Arrange
        var request = new
        {
            Provider = "openai",
            ApiKey = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/setup/validate-key", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ValidationResult>();
        
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal("MISSING_KEY", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateKey_UnknownProvider_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Provider = "unknown-provider",
            ApiKey = "test-key"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/setup/validate-key", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDiskSpace_ReturnsAvailableSpace()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/disk-space");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("availableGB", content);
    }

    [Fact]
    public async Task GetStatus_ReturnsSetupStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("setupCompleted", content);
    }
}
