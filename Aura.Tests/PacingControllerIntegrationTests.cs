using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Aura.Api.Models.Responses;
using Aura.Core.Models;
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
        var brief = new Brief("Test Topic", "General Audience", "Educate", "Professional", "English", Aspect.Widescreen16x9);
        
        var request = new
        {
            Script = "This is a test script with enough content to analyze pacing effectively.",
            Scenes = scenes,
            TargetPlatform = "YouTube",
            TargetDuration = 60.0,
            Audience = "General",
            Brief = brief
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pacing/analyze", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PacingAnalysisResponse>();
        Assert.NotNull(result);
        Assert.True(result.OverallScore >= 0);
        Assert.NotEmpty(result.AnalysisId);
        Assert.NotEmpty(result.CorrelationId);
        Assert.Equal(scenes.Count, result.Suggestions.Count);
    }

    [Fact]
    public async Task GetPlatformPresets_ReturnsAllPlatforms()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/pacing/platforms");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PlatformPresetsResponse>();
        Assert.NotNull(result);
        Assert.Equal(5, result.Platforms.Count); // YouTube, TikTok, Instagram Reels, YouTube Shorts, Facebook
        
        // Verify expected platforms
        Assert.Contains(result.Platforms, p => p.Name == "YouTube");
        Assert.Contains(result.Platforms, p => p.Name == "TikTok");
        Assert.Contains(result.Platforms, p => p.Name == "Instagram Reels");
        Assert.Contains(result.Platforms, p => p.Name == "YouTube Shorts");
        Assert.Contains(result.Platforms, p => p.Name == "Facebook");

        // Verify YouTube preset
        var youtube = result.Platforms.First(p => p.Name == "YouTube");
        Assert.Equal(1.0, youtube.PacingMultiplier);
        Assert.Equal("Conversational", youtube.RecommendedPacing);
    }

    [Fact]
    public async Task GetAnalysis_WithValidId_ReturnsAnalysis()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scenes = CreateTestScenes(3);
        var brief = new Brief("Test Topic", "General Audience", "Educate", "Professional", "English", Aspect.Widescreen16x9);
        
        // First, create an analysis
        var createRequest = new
        {
            Script = "This is a test script with enough content.",
            Scenes = scenes,
            TargetPlatform = "YouTube",
            TargetDuration = 60.0,
            Audience = "General",
            Brief = brief
        };
        var createResponse = await client.PostAsJsonAsync("/api/pacing/analyze", createRequest);
        var analysis = await createResponse.Content.ReadFromJsonAsync<PacingAnalysisResponse>();
        Assert.NotNull(analysis);
        var analysisId = analysis.AnalysisId;

        // Act
        var response = await client.GetAsync($"/api/pacing/analysis/{analysisId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PacingAnalysisResponse>();
        Assert.NotNull(result);
        Assert.Equal(analysisId, result.AnalysisId);
    }

    [Fact]
    public async Task ReanalyzePacing_WithValidId_ReturnsNewAnalysis()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scenes = CreateTestScenes(3);
        var brief = new Brief("Test Topic", "General Audience", "Educate", "Professional", "English", Aspect.Widescreen16x9);
        
        // First, create an analysis
        var createRequest = new
        {
            Script = "This is a test script with enough content.",
            Scenes = scenes,
            TargetPlatform = "YouTube",
            TargetDuration = 60.0,
            Audience = "General",
            Brief = brief
        };
        var createResponse = await client.PostAsJsonAsync("/api/pacing/analyze", createRequest);
        var analysis = await createResponse.Content.ReadFromJsonAsync<PacingAnalysisResponse>();
        Assert.NotNull(analysis);
        var analysisId = analysis.AnalysisId;

        // Act - reanalyze with different parameters
        var reanalyzeRequest = new
        {
            OptimizationLevel = "High",
            TargetPlatform = "TikTok"
        };
        var response = await client.PostAsJsonAsync($"/api/pacing/reanalyze/{analysisId}", reanalyzeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PacingAnalysisResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(analysisId, result.AnalysisId); // Should have a new ID
        Assert.Contains("Reanalyzed from", result.Warnings[0]);
    }

    [Fact]
    public async Task DeleteAnalysis_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scenes = CreateTestScenes(3);
        var brief = new Brief("Test Topic", "General Audience", "Educate", "Professional", "English", Aspect.Widescreen16x9);
        
        // First, create an analysis
        var createRequest = new
        {
            Script = "This is a test script with enough content.",
            Scenes = scenes,
            TargetPlatform = "YouTube",
            TargetDuration = 60.0,
            Audience = "General",
            Brief = brief
        };
        var createResponse = await client.PostAsJsonAsync("/api/pacing/analyze", createRequest);
        var analysis = await createResponse.Content.ReadFromJsonAsync<PacingAnalysisResponse>();
        Assert.NotNull(analysis);
        var analysisId = analysis.AnalysisId;

        // Act
        var response = await client.DeleteAsync($"/api/pacing/analysis/{analysisId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DeleteAnalysisResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("successfully deleted", result.Message);

        // Verify it's actually deleted
        var getResponse = await client.GetAsync($"/api/pacing/analysis/{analysisId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAnalysis_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidId = "non-existent-id";

        // Act
        var response = await client.GetAsync($"/api/pacing/analysis/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzePacing_WithEmptyScenes_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var brief = new Brief("Test Topic", "General Audience", "Educate", "Professional", "English", Aspect.Widescreen16x9);
        
        var request = new
        {
            Script = "Test script",
            Scenes = new List<Scene>(),
            TargetPlatform = "YouTube",
            TargetDuration = 60.0,
            Audience = "General",
            Brief = brief
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pacing/analyze", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzePacing_WithInvalidPlatform_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scenes = CreateTestScenes(3);
        var brief = new Brief("Test Topic", "General Audience", "Educate", "Professional", "English", Aspect.Widescreen16x9);
        
        var request = new
        {
            Script = "Test script with content",
            Scenes = scenes,
            TargetPlatform = "InvalidPlatform",
            TargetDuration = 60.0,
            Audience = "General",
            Brief = brief
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pacing/analyze", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzePacing_WithEmptyScript_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var scenes = CreateTestScenes(3);
        var brief = new Brief("Test Topic", "General Audience", "Educate", "Professional", "English", Aspect.Widescreen16x9);
        
        var request = new
        {
            Script = "",
            Scenes = scenes,
            TargetPlatform = "YouTube",
            TargetDuration = 60.0,
            Audience = "General",
            Brief = brief
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pacing/analyze", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
