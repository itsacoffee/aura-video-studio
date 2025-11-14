using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Providers.Validation;
using Xunit;

namespace Aura.E2E;

/// <summary>
/// Integration tests for the provider validation API endpoint
/// </summary>
public class ProviderValidationApiTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:5005";

    public ProviderValidationApiTests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    [Fact(Skip = "Requires API server to be running")]
    public async Task ValidateProviders_WithEmptyArray_ValidatesAllProviders()
    {
        // Arrange
        var request = new { providers = Array.Empty<string>() };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient.PostAsync("/api/providers/validate", content).ConfigureAwait(false);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");

        var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<ValidationResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        Assert.True(result.Results.Length > 0, "Should validate at least one provider");

        // Verify each result has required fields
        foreach (var providerResult in result.Results)
        {
            Assert.NotNull(providerResult.Name);
            Assert.NotNull(providerResult.Details);
            Assert.True(providerResult.ElapsedMs >= 0, "ElapsedMs should be non-negative");
        }
    }

    [Fact(Skip = "Requires API server to be running")]
    public async Task ValidateProviders_WithSpecificProviders_ValidatesOnlyThose()
    {
        // Arrange
        var request = new { providers = new[] { "Ollama", "StableDiffusion" } };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient.PostAsync("/api/providers/validate", content).ConfigureAwait(false);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");

        var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<ValidationResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        Assert.Equal(2, result.Results.Length);

        var providerNames = new HashSet<string>();
        foreach (var providerResult in result.Results)
        {
            providerNames.Add(providerResult.Name);
        }

        Assert.Contains("Ollama", providerNames);
        Assert.Contains("StableDiffusion", providerNames);
    }

    [Fact(Skip = "Requires API server to be running")]
    public async Task ValidateProviders_WithUnknownProvider_ReturnsFailureForThatProvider()
    {
        // Arrange
        var request = new { providers = new[] { "UnknownProvider" } };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _httpClient.PostAsync("/api/providers/validate", content).ConfigureAwait(false);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");

        var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<ValidationResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        Assert.Single(result.Results);

        var providerResult = result.Results[0];
        Assert.Equal("UnknownProvider", providerResult.Name);
        Assert.False(providerResult.Ok);
        Assert.Contains("Unknown provider", providerResult.Details);
    }

    [Fact(Skip = "Requires API server to be running and offline mode enabled")]
    public async Task ValidateProviders_WithOfflineMode_BlocksCloudProviders()
    {
        // Arrange - Enable offline mode first
        var settingsRequest = new { offlineOnly = true };
        var settingsContent = new StringContent(
            JsonSerializer.Serialize(settingsRequest),
            Encoding.UTF8,
            "application/json");

        await _httpClient.PostAsync("/api/settings/save", settingsContent).ConfigureAwait(false);

        // Act - Validate cloud providers
        var request = new { providers = new[] { "OpenAI", "ElevenLabs" } };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/api/providers/validate", content).ConfigureAwait(false);

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");

        var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<ValidationResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.NotNull(result.Results);

        // Both providers should fail with offline mode message
        foreach (var providerResult in result.Results)
        {
            Assert.False(providerResult.Ok);
            Assert.Contains("Offline mode", providerResult.Details);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
