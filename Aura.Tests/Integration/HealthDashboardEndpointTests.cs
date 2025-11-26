using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests for the Health Dashboard endpoint (PR-006)
/// </summary>
public class HealthDashboardEndpointTests : ApiIntegrationTestBase
{
    public HealthDashboardEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task HealthDashboard_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/health-dashboard");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthDashboard_ReturnsExpectedStructure()
    {
        // Act
        var response = await Client.GetAsync("/api/health-dashboard");
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Assert - verify required properties exist
        Assert.True(root.TryGetProperty("providers", out var providers), "Response should have 'providers' array");
        Assert.Equal(JsonValueKind.Array, providers.ValueKind);

        Assert.True(root.TryGetProperty("summary", out var summary), "Response should have 'summary' object");
        Assert.Equal(JsonValueKind.Object, summary.ValueKind);

        Assert.True(root.TryGetProperty("timestamp", out _), "Response should have 'timestamp'");
    }

    [Fact]
    public async Task HealthDashboard_SummaryHasRequiredFields()
    {
        // Act
        var response = await Client.GetAsync("/api/health-dashboard");
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        var summary = root.GetProperty("summary");

        // Assert - verify summary has required fields
        Assert.True(summary.TryGetProperty("totalProviders", out _), "Summary should have 'totalProviders'");
        Assert.True(summary.TryGetProperty("healthyProviders", out _), "Summary should have 'healthyProviders'");
        Assert.True(summary.TryGetProperty("degradedProviders", out _), "Summary should have 'degradedProviders'");
        Assert.True(summary.TryGetProperty("offlineProviders", out _), "Summary should have 'offlineProviders'");
        Assert.True(summary.TryGetProperty("notConfiguredProviders", out _), "Summary should have 'notConfiguredProviders'");
        Assert.True(summary.TryGetProperty("byCategory", out _), "Summary should have 'byCategory'");
    }

    [Fact]
    public async Task HealthDashboard_ProviderHasRequiredFields()
    {
        // Act
        var response = await Client.GetAsync("/api/health-dashboard");
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        var providers = root.GetProperty("providers");

        // Assert - at least one provider should exist
        Assert.True(providers.GetArrayLength() > 0, "Should have at least one provider");

        // Verify first provider has required fields
        var firstProvider = providers[0];
        Assert.True(firstProvider.TryGetProperty("name", out _), "Provider should have 'name'");
        Assert.True(firstProvider.TryGetProperty("category", out _), "Provider should have 'category'");
        Assert.True(firstProvider.TryGetProperty("tier", out _), "Provider should have 'tier'");
        Assert.True(firstProvider.TryGetProperty("healthStatus", out _), "Provider should have 'healthStatus'");
        Assert.True(firstProvider.TryGetProperty("isConfigured", out _), "Provider should have 'isConfigured'");
        Assert.True(firstProvider.TryGetProperty("requiresApiKey", out _), "Provider should have 'requiresApiKey'");
        Assert.True(firstProvider.TryGetProperty("successRate", out _), "Provider should have 'successRate'");
        Assert.True(firstProvider.TryGetProperty("averageLatencyMs", out _), "Provider should have 'averageLatencyMs'");
        Assert.True(firstProvider.TryGetProperty("circuitState", out _), "Provider should have 'circuitState'");
    }

    [Fact]
    public async Task HealthDashboard_ContainsLlmProviders()
    {
        // Act
        var response = await Client.GetAsync("/api/health-dashboard");
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        var providers = root.GetProperty("providers");

        // Assert - check that LLM providers are included
        var llmProviders = providers.EnumerateArray()
            .Where(p => p.GetProperty("category").GetString() == "LLM")
            .ToList();
        
        Assert.True(llmProviders.Count > 0, "Should have at least one LLM provider");
    }

    [Fact]
    public async Task HealthDashboard_ContainsTtsProviders()
    {
        // Act
        var response = await Client.GetAsync("/api/health-dashboard");
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        var providers = root.GetProperty("providers");

        // Assert - check that TTS providers are included
        var ttsProviders = providers.EnumerateArray()
            .Where(p => p.GetProperty("category").GetString() == "TTS")
            .ToList();
        
        Assert.True(ttsProviders.Count > 0, "Should have at least one TTS provider");
    }

    [Fact]
    public async Task HealthDashboard_ContainsImageProviders()
    {
        // Act
        var response = await Client.GetAsync("/api/health-dashboard");
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        var providers = root.GetProperty("providers");

        // Assert - check that Image providers are included
        var imageProviders = providers.EnumerateArray()
            .Where(p => p.GetProperty("category").GetString() == "Image")
            .ToList();
        
        Assert.True(imageProviders.Count > 0, "Should have at least one Image provider");
    }

    [Fact]
    public async Task HealthDashboard_HasValidHealthStatuses()
    {
        // Act
        var response = await Client.GetAsync("/api/health-dashboard");
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        var providers = root.GetProperty("providers");

        // Assert - check that all providers have valid health statuses
        var validStatuses = new HashSet<string> { "healthy", "degraded", "offline", "not_configured", "unknown" };
        foreach (var provider in providers.EnumerateArray())
        {
            var status = provider.GetProperty("healthStatus").GetString();
            Assert.True(validStatuses.Contains(status ?? ""), $"Invalid health status: {status}");
        }
    }

    [Fact]
    public async Task HealthDashboard_SummaryCountsAreConsistent()
    {
        // Act
        var response = await Client.GetAsync("/api/health-dashboard");
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        var providers = root.GetProperty("providers");
        var summary = root.GetProperty("summary");

        // Assert - verify totals match
        var totalProviders = summary.GetProperty("totalProviders").GetInt32();
        var actualCount = providers.GetArrayLength();
        Assert.Equal(totalProviders, actualCount);
    }

    [Fact]
    public async Task HealthDashboard_ContentTypeIsJson()
    {
        // Act
        var response = await Client.GetAsync("/api/health-dashboard");

        // Assert
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Contains("application/json", response.Content.Headers.ContentType.ToString());
    }
}
