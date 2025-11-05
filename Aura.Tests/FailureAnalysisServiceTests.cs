using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Diagnostics;
using Aura.Core.Services.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class FailureAnalysisServiceTests
{
    private readonly Mock<ILogger<FailureAnalysisService>> _loggerMock;
    private readonly FailureAnalysisService _service;

    public FailureAnalysisServiceTests()
    {
        _loggerMock = new Mock<ILogger<FailureAnalysisService>>();
        _service = new FailureAnalysisService(_loggerMock.Object);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_RateLimitError_IdentifiesRateLimitRootCause()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-1",
            Status = JobStatus.Failed,
            Stage = "TTS",
            ErrorMessage = "Rate limit exceeded. Too many requests to the API.",
            CorrelationId = "corr-123"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal("test-job-1", analysis.JobId);
        Assert.Equal(RootCauseType.RateLimit, analysis.PrimaryRootCause.Type);
        Assert.True(analysis.PrimaryRootCause.Confidence >= 85);
        Assert.Contains("Rate limit", analysis.PrimaryRootCause.Description);
        Assert.NotEmpty(analysis.RecommendedActions);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_InvalidApiKey_IdentifiesInvalidApiKeyRootCause()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-2",
            Status = JobStatus.Failed,
            Stage = "Script",
            ErrorMessage = "Invalid API key provided. Please check your credentials.",
            CorrelationId = "corr-456"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(RootCauseType.InvalidApiKey, analysis.PrimaryRootCause.Type);
        Assert.True(analysis.PrimaryRootCause.Confidence >= 90);
        Assert.NotEmpty(analysis.RecommendedActions);
        
        // Should recommend updating API key
        Assert.Contains(analysis.RecommendedActions, 
            action => action.Type == ActionType.ApiKey && action.Title.Contains("Update"));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_MissingApiKey_IdentifiesMissingApiKeyRootCause()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-3",
            Status = JobStatus.Failed,
            Stage = "TTS",
            ErrorMessage = "Missing API key for ElevenLabs. No API key configured.",
            CorrelationId = "corr-789"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(RootCauseType.MissingApiKey, analysis.PrimaryRootCause.Type);
        Assert.True(analysis.PrimaryRootCause.Confidence >= 95);
        Assert.Equal("ElevenLabs", analysis.PrimaryRootCause.Provider);
        Assert.NotEmpty(analysis.RecommendedActions);
        
        // Should recommend adding API key
        Assert.Contains(analysis.RecommendedActions, 
            action => action.Type == ActionType.ApiKey && action.Title.Contains("Add"));
    }

    [Fact]
    public async Task AnalyzeFailureAsync_FFmpegNotFound_IdentifiesFFmpegNotFoundRootCause()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-4",
            Status = JobStatus.Failed,
            Stage = "Render",
            ErrorMessage = "FFmpeg not found. Please install FFmpeg.",
            CorrelationId = "corr-101"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(RootCauseType.FFmpegNotFound, analysis.PrimaryRootCause.Type);
        Assert.True(analysis.PrimaryRootCause.Confidence >= 90);
        Assert.NotEmpty(analysis.RecommendedActions);
        
        // Should recommend installation
        Assert.Contains(analysis.RecommendedActions, 
            action => action.Type == ActionType.Installation);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_NetworkError_IdentifiesNetworkRootCause()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-5",
            Status = JobStatus.Failed,
            Stage = "Script",
            ErrorMessage = "Network connection failed. Unable to reach the API server.",
            CorrelationId = "corr-202"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(RootCauseType.NetworkError, analysis.PrimaryRootCause.Type);
        Assert.True(analysis.PrimaryRootCause.Confidence >= 80);
        Assert.NotEmpty(analysis.RecommendedActions);
        
        // Should recommend network checks
        Assert.Contains(analysis.RecommendedActions, 
            action => action.Type == ActionType.Network);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_UnknownError_ReturnsUnknownRootCause()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-6",
            Status = JobStatus.Failed,
            Stage = "Unknown",
            ErrorMessage = "Some random error that doesn't match any pattern",
            CorrelationId = "corr-303"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(RootCauseType.Unknown, analysis.PrimaryRootCause.Type);
        Assert.True(analysis.PrimaryRootCause.Confidence <= 50);
        Assert.NotEmpty(analysis.RecommendedActions);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_IncludesDocumentationLinks()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-7",
            Status = JobStatus.Failed,
            Stage = "TTS",
            ErrorMessage = "Rate limit exceeded",
            CorrelationId = "corr-404"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotEmpty(analysis.DocumentationLinks);
        Assert.All(analysis.DocumentationLinks, link =>
        {
            Assert.NotEmpty(link.Title);
            Assert.NotEmpty(link.Url);
        });
    }

    [Fact]
    public async Task AnalyzeFailureAsync_PrioritizesRecommendations()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-8",
            Status = JobStatus.Failed,
            Stage = "TTS",
            ErrorMessage = "Rate limit exceeded",
            CorrelationId = "corr-505"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotEmpty(analysis.RecommendedActions);
        
        // Verify recommendations are sorted by priority
        for (int i = 0; i < analysis.RecommendedActions.Count - 1; i++)
        {
            Assert.True(analysis.RecommendedActions[i].Priority <= 
                       analysis.RecommendedActions[i + 1].Priority);
        }
    }

    [Fact]
    public async Task AnalyzeFailureAsync_IncludesEstimatedTime()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-9",
            Status = JobStatus.Failed,
            Stage = "Script",
            ErrorMessage = "Invalid API key",
            CorrelationId = "corr-606"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotEmpty(analysis.RecommendedActions);
        Assert.Contains(analysis.RecommendedActions, 
            action => action.EstimatedMinutes.HasValue && action.EstimatedMinutes.Value > 0);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_GeneratesHelpfulSummary()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-10",
            Status = JobStatus.Failed,
            Stage = "Render",
            ErrorMessage = "Codec not found",
            CorrelationId = "corr-707"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotEmpty(analysis.Summary);
        Assert.Contains(job.Id, analysis.Summary);
        Assert.Contains(job.Stage, analysis.Summary);
    }

    [Fact]
    public async Task AnalyzeFailureAsync_MultiplePatterns_IdentifiesMultipleRootCauses()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-11",
            Status = JobStatus.Failed,
            Stage = "TTS",
            ErrorMessage = "Network connection timeout occurred while accessing rate-limited API",
            CorrelationId = "corr-808"
        };

        // Act
        var analysis = await _service.AnalyzeFailureAsync(job, null, CancellationToken.None);

        // Assert
        Assert.NotNull(analysis);
        
        // Should identify either rate limit or network (or both)
        var allCauses = new[] { analysis.PrimaryRootCause }
            .Concat(analysis.SecondaryRootCauses);
        Assert.Contains(allCauses, cause => 
            cause.Type == RootCauseType.RateLimit || cause.Type == RootCauseType.NetworkError);
    }
}
