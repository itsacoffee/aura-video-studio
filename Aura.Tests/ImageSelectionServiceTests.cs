using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Visual;
using Aura.Core.Services.Assets;
using Aura.Core.Services.Visual;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for ImageSelectionService
/// </summary>
public class ImageSelectionServiceTests
{
    private readonly ImageSelectionService _service;
    private readonly StockImageService _stockImageService;

    public ImageSelectionServiceTests()
    {
        var logger = NullLogger<ImageSelectionService>.Instance;
        var stockLogger = NullLogger<StockImageService>.Instance;
        var scoringLogger = NullLogger<AestheticScoringService>.Instance;

        var httpClient = new HttpClient();
        _stockImageService = new StockImageService(stockLogger, httpClient);

        var scoringService = new AestheticScoringService(scoringLogger);
        _service = new ImageSelectionService(logger, _stockImageService, scoringService);
    }

    [Fact]
    public async Task SelectImageForSceneAsync_Should_ReturnResult()
    {
        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "A professional office setting with modern furniture",
            Subject = "office",
            Framing = "medium shot",
            NarrativeKeywords = new[] { "office", "professional", "business" },
            Style = VisualStyle.Realistic,
            QualityTier = VisualQualityTier.Standard
        };

        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 50.0,
            CandidatesPerScene = 3
        };

        var result = await _service.SelectImageForSceneAsync(prompt, config, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.SceneIndex);
        Assert.NotEmpty(result.Candidates);
        Assert.True(result.SelectionTimeMs > 0);
    }

    [Fact]
    public async Task SelectImageForSceneAsync_WithHighThreshold_Should_HaveWarnings()
    {
        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "Test scene",
            Subject = "test",
            Style = VisualStyle.Realistic
        };

        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 95.0,
            CandidatesPerScene = 2
        };

        var result = await _service.SelectImageForSceneAsync(prompt, config, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task SelectImagesForScenesAsync_Should_ProcessMultipleScenes()
    {
        var prompts = new List<VisualPrompt>
        {
            new VisualPrompt
            {
                SceneIndex = 0,
                DetailedDescription = "Scene 1",
                Subject = "landscape",
                NarrativeKeywords = new[] { "nature", "outdoor" },
                Style = VisualStyle.Cinematic
            },
            new VisualPrompt
            {
                SceneIndex = 1,
                DetailedDescription = "Scene 2",
                Subject = "city",
                NarrativeKeywords = new[] { "urban", "modern" },
                Style = VisualStyle.Realistic
            }
        };

        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 50.0,
            CandidatesPerScene = 2
        };

        var results = await _service.SelectImagesForScenesAsync(prompts, config, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(0, results[0].SceneIndex);
        Assert.Equal(1, results[1].SceneIndex);
    }

    [Fact]
    public async Task SelectImageForSceneAsync_Should_IncludeLicensingInfo()
    {
        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "Nature photography",
            Subject = "nature",
            NarrativeKeywords = new[] { "nature", "landscape" },
            Style = VisualStyle.Documentary
        };

        var result = await _service.SelectImageForSceneAsync(prompt, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Candidates);
        
        foreach (var candidate in result.Candidates)
        {
            Assert.NotNull(candidate.Licensing);
            Assert.NotEmpty(candidate.Licensing.LicenseType);
            Assert.NotEmpty(candidate.Licensing.SourcePlatform);
        }
    }

    [Fact]
    public async Task SelectImageForSceneAsync_Should_RankByScore()
    {
        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "Technology and innovation",
            Subject = "technology",
            NarrativeKeywords = new[] { "technology", "innovation", "digital" },
            Style = VisualStyle.Modern,
            QualityTier = VisualQualityTier.Enhanced
        };

        var config = new ImageSelectionConfig
        {
            CandidatesPerScene = 5
        };

        var result = await _service.SelectImageForSceneAsync(prompt, config, CancellationToken.None);

        Assert.NotNull(result);
        
        if (result.Candidates.Count > 1)
        {
            for (int i = 1; i < result.Candidates.Count; i++)
            {
                Assert.True(
                    result.Candidates[i - 1].OverallScore >= result.Candidates[i].OverallScore,
                    "Candidates should be ranked by overall score");
            }
        }
    }

    [Fact]
    public async Task SelectImageForSceneAsync_Should_TrackNarrativeKeywords()
    {
        var keywords = new[] { "sunset", "beach", "ocean" };
        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "Beautiful sunset at the beach",
            Subject = "beach",
            NarrativeKeywords = keywords,
            Style = VisualStyle.Cinematic
        };

        var result = await _service.SelectImageForSceneAsync(prompt, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(keywords, result.NarrativeKeywords);
    }

    [Fact]
    public async Task SelectImageForSceneAsync_Should_RespectMinimumThreshold()
    {
        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "Test scene",
            Subject = "test",
            Style = VisualStyle.Realistic
        };

        var threshold = 65.0;
        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = threshold
        };

        var result = await _service.SelectImageForSceneAsync(prompt, config, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(threshold, result.MinimumAestheticThreshold);
        
        if (result.SelectedImage != null)
        {
            Assert.True(
                result.SelectedImage.OverallScore >= threshold ||
                !result.MeetsCriteria,
                "Selected image should meet threshold or MeetsCriteria should be false");
        }
    }

    [Fact]
    public async Task SelectImageForSceneAsync_WithValidConfig_Should_CompleteSuccessfully()
    {
        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "Test scene with basic requirements",
            Subject = "test",
            Style = VisualStyle.Realistic
        };

        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 40.0,
            CandidatesPerScene = 2
        };

        var result = await _service.SelectImageForSceneAsync(prompt, config, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.SceneIndex);
        Assert.True(result.SelectionTimeMs >= 0);
    }
}
