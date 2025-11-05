using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Visual;
using Aura.Core.Services.Visual;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for CandidateCacheService
/// </summary>
public class CandidateCacheServiceTests
{
    private readonly CandidateCacheService _service;

    public CandidateCacheServiceTests()
    {
        var logger = NullLogger<CandidateCacheService>.Instance;
        _service = new CandidateCacheService(logger);
    }

    [Fact]
    public void GenerateRequestId_SamePrompt_ProducesSameId()
    {
        var prompt = CreateTestPrompt();
        var config = CreateTestConfig();

        var id1 = _service.GenerateRequestId(prompt, config);
        var id2 = _service.GenerateRequestId(prompt, config);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void GenerateRequestId_DifferentPrompt_ProducesDifferentId()
    {
        var prompt1 = CreateTestPrompt();
        var prompt2 = CreateTestPrompt() with { Subject = "different subject" };
        var config = CreateTestConfig();

        var id1 = _service.GenerateRequestId(prompt1, config);
        var id2 = _service.GenerateRequestId(prompt2, config);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public async Task CacheCandidatesAsync_StoresAndRetrievesCorrectly()
    {
        var requestId = "test-request-123";
        var result = CreateTestResult();

        await _service.CacheCandidatesAsync(requestId, result, TimeSpan.FromMinutes(30));

        var cached = await _service.GetCachedCandidatesAsync(requestId);

        Assert.NotNull(cached);
        Assert.Equal(requestId, cached.RequestId);
        Assert.Equal(result.SceneIndex, cached.Result.SceneIndex);
        Assert.Equal(result.Candidates.Count, cached.Result.Candidates.Count);
    }

    [Fact]
    public async Task GetCachedCandidatesAsync_ExpiredEntry_ReturnsNull()
    {
        var requestId = "test-expired-123";
        var result = CreateTestResult();

        await _service.CacheCandidatesAsync(requestId, result, TimeSpan.FromMilliseconds(1));
        await Task.Delay(100);

        var cached = await _service.GetCachedCandidatesAsync(requestId);

        Assert.Null(cached);
    }

    [Fact]
    public async Task InvalidateCacheAsync_RemovesEntry()
    {
        var requestId = "test-invalidate-123";
        var result = CreateTestResult();

        await _service.CacheCandidatesAsync(requestId, result);
        await _service.InvalidateCacheAsync(requestId);

        var cached = await _service.GetCachedCandidatesAsync(requestId);

        Assert.Null(cached);
    }

    [Fact]
    public async Task ClearExpiredEntriesAsync_RemovesOnlyExpired()
    {
        var validId = "test-valid-123";
        var expiredId = "test-expired-123";
        var result = CreateTestResult();

        await _service.CacheCandidatesAsync(validId, result, TimeSpan.FromHours(1));
        await _service.CacheCandidatesAsync(expiredId, result, TimeSpan.FromMilliseconds(1));
        await Task.Delay(100);

        await _service.ClearExpiredEntriesAsync();

        var validCached = await _service.GetCachedCandidatesAsync(validId);
        var expiredCached = await _service.GetCachedCandidatesAsync(expiredId);

        Assert.NotNull(validCached);
        Assert.Null(expiredCached);
    }

    [Fact]
    public void GetStatistics_ReturnsAccurateStats()
    {
        var stats = _service.GetStatistics();

        Assert.NotNull(stats);
        Assert.True(stats.TotalEntries >= 0);
        Assert.True(stats.ExpiredEntries >= 0);
        Assert.True(stats.TotalCandidatesCached >= 0);
    }

    private static VisualPrompt CreateTestPrompt()
    {
        return new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "A beautiful sunset over the ocean",
            Subject = "sunset",
            Framing = "wide shot",
            NarrativeKeywords = new[] { "peaceful", "serene", "colorful" },
            Style = VisualStyle.Cinematic,
            QualityTier = VisualQualityTier.Premium
        };
    }

    private static ImageSelectionConfig CreateTestConfig()
    {
        return new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 70.0,
            CandidatesPerScene = 5,
            PreferGeneratedImages = true
        };
    }

    private static ImageSelectionResult CreateTestResult()
    {
        var candidates = new List<ImageCandidate>
        {
            new ImageCandidate
            {
                ImageUrl = "https://example.com/image1.jpg",
                Source = "Test",
                OverallScore = 85.0,
                AestheticScore = 90.0,
                KeywordCoverageScore = 80.0,
                QualityScore = 85.0,
                Reasoning = "High quality test image",
                Width = 1920,
                Height = 1080
            }
        };

        return new ImageSelectionResult
        {
            SceneIndex = 0,
            SelectedImage = candidates.First(),
            Candidates = candidates,
            MinimumAestheticThreshold = 70.0,
            NarrativeKeywords = new[] { "peaceful", "serene" },
            SelectionTimeMs = 100.0,
            MeetsCriteria = true,
            Warnings = Array.Empty<string>()
        };
    }
}
