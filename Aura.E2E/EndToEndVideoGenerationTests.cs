using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Aura.E2E;

/// <summary>
/// End-to-end tests for complete video generation workflow
/// Tests the API from HTTP request to final video output
/// </summary>
public class EndToEndVideoGenerationTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _httpClient;

    public EndToEndVideoGenerationTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _httpClient = fixture.HttpClient;
    }

    [Fact]
    public async Task CompleteVideoGeneration_FromBriefToVideo_Success()
    {
        // Arrange
        var request = new
        {
            brief = new
            {
                topic = "Test Video",
                audience = "Developers",
                goal = "Demonstrate E2E testing"
            },
            planSpec = new
            {
                targetDurationSeconds = 30,
                style = "educational"
            },
            voiceSpec = new
            {
                voiceName = "default",
                speed = 1.0
            },
            renderSpec = new
            {
                width = 1280,
                height = 720,
                fps = 30,
                codec = "h264"
            },
            isQuickDemo = true
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act - Start generation
        var response = await _httpClient.PostAsync("/api/generation/start", content);

        // Assert - Generation started
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GenerationStartResponse>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(result);
        Assert.NotNull(result.JobId);
        Assert.NotNull(result.CorrelationId);

        // Wait for completion
        var jobId = result.JobId;
        var maxWaitTime = TimeSpan.FromMinutes(5);
        var startTime = DateTime.UtcNow;
        string? outputPath = null;

        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            var statusResponse = await _httpClient.GetAsync($"/api/generation/status/{jobId}");
            statusResponse.EnsureSuccessStatusCode();

            var statusBody = await statusResponse.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<GenerationStatusResponse>(
                statusBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            Assert.NotNull(status);

            if (status.Status == "Completed")
            {
                outputPath = status.OutputPath;
                break;
            }
            else if (status.Status == "Failed" || status.Status == "Cancelled")
            {
                Assert.Fail($"Generation failed or was cancelled: {status.ErrorMessage}");
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Assert - Video was generated
        Assert.NotNull(outputPath);
        Assert.True(File.Exists(outputPath), $"Output video should exist at {outputPath}");

        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0, "Output video should not be empty");
    }

    [Fact]
    public async Task VideoGeneration_WithCancellation_StopsSuccessfully()
    {
        // Arrange
        var request = new
        {
            brief = new
            {
                topic = "Cancellation Test",
                audience = "Testers",
                goal = "Test cancellation"
            },
            planSpec = new
            {
                targetDurationSeconds = 120, // Long video
                style = "test"
            },
            voiceSpec = new
            {
                voiceName = "default",
                speed = 1.0
            },
            renderSpec = new
            {
                width = 1280,
                height = 720,
                fps = 30,
                codec = "h264"
            },
            isQuickDemo = false
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act - Start generation
        var startResponse = await _httpClient.PostAsync("/api/generation/start", content);
        startResponse.EnsureSuccessStatusCode();

        var responseBody = await startResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GenerationStartResponse>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(result);
        var jobId = result.JobId;

        // Wait a bit for generation to start
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Cancel the generation
        var cancelResponse = await _httpClient.PostAsync($"/api/generation/cancel/{jobId}", null);
        cancelResponse.EnsureSuccessStatusCode();

        // Wait for cancellation to complete
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Check status
        var statusResponse = await _httpClient.GetAsync($"/api/generation/status/{jobId}");
        statusResponse.EnsureSuccessStatusCode();

        var statusBody = await statusResponse.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<GenerationStatusResponse>(
            statusBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        // Assert - Generation was cancelled
        Assert.NotNull(status);
        Assert.Equal("Cancelled", status.Status);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    private class GenerationStartResponse
    {
        public string JobId { get; set; } = "";
        public string CorrelationId { get; set; } = "";
        public string Message { get; set; } = "";
    }

    private class GenerationStatusResponse
    {
        public string JobId { get; set; } = "";
        public string Status { get; set; } = "";
        public double? OverallProgress { get; set; }
        public string? CurrentStage { get; set; }
        public string? OutputPath { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

/// <summary>
/// Test fixture for E2E tests that spins up the API
/// </summary>
public class ApiTestFixture : IAsyncDisposable
{
    public HttpClient HttpClient { get; }
    
    public ApiTestFixture()
    {
        // In a real implementation, this would start the API server
        // For now, assumes API is running on localhost:5005
        var baseAddress = Environment.GetEnvironmentVariable("AURA_API_URL") 
            ?? "http://localhost:5005";
        
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(baseAddress),
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient?.Dispose();
        await Task.CompletedTask;
    }
}
