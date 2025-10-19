using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Aura.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aura.Tests;

public class HealthEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_Should_Return200()
    {
        // Act
        var response = await _client.GetAsync("/api/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthLive_Should_ReturnHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health/live");
        var result = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Single(result.Checks);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task HealthReady_Should_ReturnJsonResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");
        var result = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Status);
        Assert.NotEmpty(result.Checks);
    }

    [Fact]
    public async Task HealthReady_Should_ReturnAllChecks()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");
        var result = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        Assert.NotNull(result);
        
        // Should have all 4 checks
        Assert.Equal(4, result.Checks.Count);
        
        // Verify check names
        Assert.Contains(result.Checks, c => c.Name == "FFmpeg");
        Assert.Contains(result.Checks, c => c.Name == "TempDirectory");
        Assert.Contains(result.Checks, c => c.Name == "ProviderRegistry");
        Assert.Contains(result.Checks, c => c.Name == "PortAvailability");
    }

    [Fact]
    public async Task HealthReady_Should_Return503_WhenUnhealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");
        var result = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        Assert.NotNull(result);
        
        // If status is unhealthy, should return 503
        if (result.Status == HealthStatus.Unhealthy)
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task HealthReady_Should_IncludeErrorsWhenUnhealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");
        var result = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        Assert.NotNull(result);
        
        // If status is unhealthy, should have errors
        if (result.Status == HealthStatus.Unhealthy)
        {
            Assert.NotEmpty(result.Errors);
        }
    }

    [Fact]
    public async Task HealthReady_Should_IncludeDetailsInChecks()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");
        var result = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        // Assert
        Assert.NotNull(result);
        
        // Each check should have a message
        foreach (var check in result.Checks)
        {
            Assert.NotNull(check.Name);
            Assert.NotNull(check.Status);
            // Message may be null for some checks
        }
    }
}
