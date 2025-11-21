using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Tests for the simple health endpoint added in PR 001
/// </summary>
public class HealthEndpointTests : ApiIntegrationTestBase
{
    public HealthEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task HealthzSimple_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/healthz/simple");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthzSimple_ReturnsExpectedStructure()
    {
        // Act
        var response = await Client.GetAsync("/healthz/simple");
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("status", out var status));
        Assert.Equal("ok", status.GetString());

        Assert.True(root.TryGetProperty("service", out var service));
        Assert.Equal("Aura.Api", service.GetString());

        Assert.True(root.TryGetProperty("version", out var version));
        Assert.NotNull(version.GetString());

        Assert.True(root.TryGetProperty("timestampUtc", out var timestamp));
        Assert.NotNull(timestamp.GetString());
    }

    [Fact]
    public async Task HealthzSimple_TimestampIsValidIsoFormat()
    {
        // Act
        var response = await Client.GetAsync("/healthz/simple");
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("timestampUtc", out var timestampElement));
        var timestampStr = timestampElement.GetString();
        Assert.NotNull(timestampStr);

        // Verify it can be parsed as DateTime
        var canParse = DateTime.TryParse(timestampStr, out var parsedDate);
        Assert.True(canParse, $"Timestamp '{timestampStr}' should be parseable as DateTime");
        
        // Verify it's recent (within last 5 seconds)
        var now = DateTime.UtcNow;
        var diff = Math.Abs((now - parsedDate).TotalSeconds);
        Assert.True(diff < 5, $"Timestamp should be recent. Difference: {diff} seconds");
    }

    [Fact]
    public async Task HealthzSimple_ContentTypeIsJson()
    {
        // Act
        var response = await Client.GetAsync("/healthz/simple");

        // Assert
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Contains("application/json", response.Content.Headers.ContentType.ToString());
    }
}
