using Xunit;
using Aura.Core.Models;
using Aura.Core.Planner;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Aura.Tests;

public class RecommendationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RecommendationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override the recommendation service with our test implementation
                services.AddSingleton<IRecommendationService>(sp =>
                    new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance));
            });
        });
    }

    [Fact]
    public async Task RecommendationsEndpoint_Should_ReturnOk_WithValidRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            topic = "Machine Learning Basics",
            audience = "Students",
            goal = "Educational",
            tone = "Informative",
            language = "en-US",
            aspect = "Widescreen16x9",
            targetDurationMinutes = 5.0,
            pacing = "Conversational",
            density = "Balanced",
            style = "Educational"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/planner/recommendations", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RecommendationsEndpoint_Should_ReturnBadRequest_WithoutTopic()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            topic = "",
            targetDurationMinutes = 5.0,
            pacing = "Conversational",
            density = "Balanced"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/planner/recommendations", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecommendationsEndpoint_Should_ReturnBadRequest_WithInvalidDuration()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            topic = "Test Topic",
            targetDurationMinutes = 0.0,
            pacing = "Conversational",
            density = "Balanced"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/planner/recommendations", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecommendationsEndpoint_Should_AcceptConstraints()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            topic = "Test Topic",
            audience = "General",
            targetDurationMinutes = 5.0,
            pacing = "Conversational",
            density = "Balanced",
            style = "Standard",
            constraints = new
            {
                maxSceneCount = 8,
                minSceneCount = 4,
                maxBRollPercentage = 30.0,
                maxReadingLevel = 12
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/planner/recommendations", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RecommendationsEndpoint_Should_AcceptAudiencePersona()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            topic = "Advanced Programming",
            audience = "Developers",
            targetDurationMinutes = 10.0,
            pacing = "Fast",
            density = "Dense",
            style = "Technical",
            audiencePersona = "Professional software engineers with 3+ years experience"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/planner/recommendations", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RecommendationsEndpoint_Should_UseDefaults_WhenOptionalFieldsMissing()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            topic = "Simple Topic",
            targetDurationMinutes = 3.0
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/planner/recommendations", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
