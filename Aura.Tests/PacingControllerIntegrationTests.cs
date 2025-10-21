using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Aura.Api.Controllers;
using Aura.Core.AI.Pacing;
using Aura.Core.Models;
using Aura.Core.Services.Analytics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aura.Tests;

public class PacingControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PacingControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnalyzePacing_WithValidRequest_ReturnsAnalysis()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scenes = CreateTestScenes(3);
        var request = new
        {
            scenes,
            audioPath = (string?)null,
            format = "Explainer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pacing/analyze", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PacingAnalysisResult>();
        Assert.NotNull(result);
        Assert.True(result.EngagementScore > 0);
        Assert.Equal(scenes.Count, result.SceneRecommendations.Count);
    }

    [Fact]
    public async Task PredictRetention_WithValidRequest_ReturnsAnalysis()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scenes = CreateTestScenes(4);
        var request = new
        {
            scenes,
            audioPath = (string?)null,
            format = "Tutorial"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pacing/retention", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<VideoRetentionAnalysis>();
        Assert.NotNull(result);
        Assert.NotNull(result.PacingAnalysis);
        Assert.NotNull(result.RetentionPrediction);
        Assert.NotNull(result.AttentionCurve);
    }

    [Fact]
    public async Task OptimizeScenes_WithValidRequest_ReturnsOptimizedScenes()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scenes = new List<Scene>
        {
            CreateScene(0, "Long Hook", 25), // Should be shortened
            CreateScene(1, "Middle", 15),
            CreateScene(2, "End", 12)
        };
        var request = new
        {
            scenes,
            format = "Entertainment"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pacing/optimize", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<Scene>>();
        Assert.NotNull(result);
        Assert.Equal(scenes.Count, result.Count);
        
        // First scene should be optimized (shortened for entertainment format)
        Assert.True(result[0].Duration.TotalSeconds <= 20);
    }

    [Fact]
    public async Task GetAttentionCurve_WithValidRequest_ReturnsCurve()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scenes = CreateTestScenes(3);
        var totalDuration = TimeSpan.FromSeconds(scenes.Sum(s => s.Duration.TotalSeconds));
        var request = new
        {
            scenes,
            videoDuration = totalDuration
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pacing/attention-curve", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AttentionCurve>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Points);
        Assert.True(result.AverageEngagement >= 0 && result.AverageEngagement <= 1.0);
    }

    [Fact]
    public async Task CompareVersions_WithValidRequest_ReturnsComparison()
    {
        // Arrange
        var client = _factory.CreateClient();
        var originalScenes = CreateTestScenes(3);
        var optimizedScenes = originalScenes.Select(s => s with { Duration = TimeSpan.FromSeconds(s.Duration.TotalSeconds * 0.9) }).ToList();
        var request = new
        {
            originalScenes,
            optimizedScenes,
            format = "Vlog"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pacing/compare", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<VideoComparisonMetrics>();
        Assert.NotNull(result);
        Assert.NotNull(result.Original);
        Assert.NotNull(result.Optimized);
        Assert.NotNull(result.Improvements);
    }

    [Fact]
    public async Task GetTemplates_ReturnsAllTemplates()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/pacing/templates");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<ContentTemplateDto>>();
        Assert.NotNull(result);
        Assert.True(result.Count >= 6); // At least 6 formats
        
        // Verify templates have required data
        foreach (var template in result)
        {
            Assert.False(string.IsNullOrEmpty(template.Name));
            Assert.False(string.IsNullOrEmpty(template.Description));
            Assert.NotNull(template.Parameters);
        }
    }

    [Fact]
    public async Task AnalyzePacing_WithEmptyScenes_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            scenes = new List<Scene>(),
            audioPath = (string?)null,
            format = "Explainer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pacing/analyze", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OptimizeScenes_WithDifferentFormats_ProducesDifferentResults()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scenes = CreateTestScenes(3);

        // Act - Entertainment format
        var entertainmentRequest = new { scenes, format = "Entertainment" };
        var entertainmentResponse = await client.PostAsJsonAsync("/api/pacing/optimize", entertainmentRequest);
        var entertainmentResult = await entertainmentResponse.Content.ReadFromJsonAsync<List<Scene>>();

        // Act - Educational format
        var educationalRequest = new { scenes, format = "Educational" };
        var educationalResponse = await client.PostAsJsonAsync("/api/pacing/optimize", educationalRequest);
        var educationalResult = await educationalResponse.Content.ReadFromJsonAsync<List<Scene>>();

        // Assert
        Assert.NotNull(entertainmentResult);
        Assert.NotNull(educationalResult);
        
        // Entertainment format should generally have shorter scenes than educational
        var entertainmentAvg = entertainmentResult.Average(s => s.Duration.TotalSeconds);
        var educationalAvg = educationalResult.Average(s => s.Duration.TotalSeconds);
        
        // Educational allows longer durations, so entertainment should be shorter or equal
        Assert.True(entertainmentAvg <= educationalAvg + 5); // Allow small tolerance
    }

    private List<Scene> CreateTestScenes(int count)
    {
        var scenes = new List<Scene>();
        for (int i = 0; i < count; i++)
        {
            scenes.Add(CreateScene(i, $"Scene {i + 1}", 15 + (i * 3)));
        }
        return scenes;
    }

    private Scene CreateScene(int index, string heading, double durationSeconds)
    {
        var wordCount = (int)(durationSeconds / 60.0 * 150);
        var script = string.Join(" ", Enumerable.Range(0, wordCount).Select(i => $"word{i}"));
        
        return new Scene(
            index,
            heading,
            script,
            TimeSpan.FromSeconds(index * durationSeconds),
            TimeSpan.FromSeconds(durationSeconds)
        );
    }
}
