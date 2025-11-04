using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Visual;
using Aura.Core.Services.Visual;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for AestheticScoringService
/// </summary>
public class AestheticScoringServiceTests
{
    private readonly AestheticScoringService _service;

    public AestheticScoringServiceTests()
    {
        var logger = NullLogger<AestheticScoringService>.Instance;
        _service = new AestheticScoringService(logger);
    }

    [Fact]
    public async Task ScoreImageAsync_HighQualityImage_Should_ScoreAboveThreshold()
    {
        var candidate = new ImageCandidate
        {
            ImageUrl = "https://example.com/image.jpg",
            Source = "StableDiffusion",
            Width = 1920,
            Height = 1080,
            GenerationLatencyMs = 3000
        };

        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "A beautiful landscape with mountains",
            Subject = "landscape",
            NarrativeKeywords = new[] { "landscape", "mountains", "nature" },
            Style = VisualStyle.Cinematic,
            QualityTier = VisualQualityTier.Premium
        };

        var score = await _service.ScoreImageAsync(candidate, prompt, CancellationToken.None);

        Assert.True(score >= 55.0, $"Expected score >= 55, got {score}");
    }

    [Fact]
    public async Task ScoreImageAsync_LowQualityImage_Should_ScoreBelowThreshold()
    {
        var candidate = new ImageCandidate
        {
            ImageUrl = "https://example.com/lowres.jpg",
            Source = "Stock",
            Width = 640,
            Height = 480,
            GenerationLatencyMs = 50000
        };

        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "A beautiful landscape",
            NarrativeKeywords = new[] { "cityscape", "urban" },
            Style = VisualStyle.Realistic,
            QualityTier = VisualQualityTier.Basic
        };

        var score = await _service.ScoreImageAsync(candidate, prompt, CancellationToken.None);

        Assert.True(score < 70.0, $"Expected score < 70, got {score}");
    }

    [Fact]
    public async Task ScoreAndRankCandidatesAsync_Should_RankByScore()
    {
        var candidates = new List<ImageCandidate>
        {
            new ImageCandidate
            {
                ImageUrl = "https://example.com/low.jpg",
                Source = "Stock",
                Width = 800,
                Height = 600,
                GenerationLatencyMs = 1000
            },
            new ImageCandidate
            {
                ImageUrl = "https://example.com/high.jpg",
                Source = "StableDiffusion",
                Width = 1920,
                Height = 1080,
                GenerationLatencyMs = 3000
            },
            new ImageCandidate
            {
                ImageUrl = "https://example.com/medium.jpg",
                Source = "Pexels",
                Width = 1280,
                Height = 720,
                GenerationLatencyMs = 2000
            }
        };

        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "Professional office setting",
            Subject = "office",
            NarrativeKeywords = new[] { "office", "professional", "business" },
            Style = VisualStyle.Realistic,
            QualityTier = VisualQualityTier.Standard
        };

        var ranked = await _service.ScoreAndRankCandidatesAsync(
            candidates, prompt, 50.0, CancellationToken.None);

        Assert.Equal(3, ranked.Count);
        Assert.True(ranked[0].OverallScore >= ranked[1].OverallScore);
        Assert.True(ranked[1].OverallScore >= ranked[2].OverallScore);
        Assert.Equal("https://example.com/high.jpg", ranked[0].ImageUrl);
    }

    [Fact]
    public async Task ScoreAndRankCandidatesAsync_Should_AddRejectionReasons()
    {
        var candidates = new List<ImageCandidate>
        {
            new ImageCandidate
            {
                ImageUrl = "https://example.com/poor.jpg",
                Source = "Stock",
                Width = 400,
                Height = 300,
                GenerationLatencyMs = 1000
            }
        };

        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "Complex technical diagram",
            NarrativeKeywords = new[] { "diagram", "technical", "complex" },
            Style = VisualStyle.Realistic,
            QualityTier = VisualQualityTier.Standard
        };

        var ranked = await _service.ScoreAndRankCandidatesAsync(
            candidates, prompt, 60.0, CancellationToken.None);

        Assert.Single(ranked);
        Assert.True(ranked[0].RejectionReasons.Count > 0);
    }

    [Fact]
    public void MeetsCriteria_HighQualityCandidate_Should_ReturnTrue()
    {
        var candidate = new ImageCandidate
        {
            AestheticScore = 75.0,
            KeywordCoverageScore = 80.0,
            QualityScore = 85.0,
            OverallScore = 78.0,
            RejectionReasons = Array.Empty<string>()
        };

        var meetsCriteria = _service.MeetsCriteria(candidate, 60.0);

        Assert.True(meetsCriteria);
    }

    [Fact]
    public void MeetsCriteria_LowQualityCandidate_Should_ReturnFalse()
    {
        var candidate = new ImageCandidate
        {
            AestheticScore = 30.0,
            KeywordCoverageScore = 25.0,
            QualityScore = 40.0,
            OverallScore = 32.0,
            RejectionReasons = new[] { "Low aesthetic score" }
        };

        var meetsCriteria = _service.MeetsCriteria(candidate, 60.0);

        Assert.False(meetsCriteria);
    }

    [Fact]
    public async Task ScoreImageAsync_WithKeywordMatch_Should_ScoreHigher()
    {
        var candidate = new ImageCandidate
        {
            ImageUrl = "https://example.com/landscape-mountains.jpg",
            Source = "Pexels",
            Width = 1920,
            Height = 1080,
            Reasoning = "Beautiful landscape with mountains and nature"
        };

        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "A scenic mountain landscape",
            Subject = "landscape",
            NarrativeKeywords = new[] { "landscape", "mountains", "nature" },
            Style = VisualStyle.Cinematic
        };

        var score = await _service.ScoreImageAsync(candidate, prompt, CancellationToken.None);

        Assert.True(score >= 65.0, $"Expected keyword match to boost score, got {score}");
    }

    [Fact]
    public async Task ScoreImageAsync_4KResolution_Should_ScoreHigher()
    {
        var candidate4K = new ImageCandidate
        {
            ImageUrl = "https://example.com/4k.jpg",
            Source = "StableDiffusion",
            Width = 3840,
            Height = 2160,
            GenerationLatencyMs = 3000
        };

        var candidateHD = new ImageCandidate
        {
            ImageUrl = "https://example.com/hd.jpg",
            Source = "StableDiffusion",
            Width = 1920,
            Height = 1080,
            GenerationLatencyMs = 3000
        };

        var prompt = new VisualPrompt
        {
            SceneIndex = 0,
            DetailedDescription = "Test image",
            Style = VisualStyle.Cinematic
        };

        var score4K = await _service.ScoreImageAsync(candidate4K, prompt, CancellationToken.None);
        var scoreHD = await _service.ScoreImageAsync(candidateHD, prompt, CancellationToken.None);

        Assert.True(score4K > scoreHD, $"4K image should score higher: 4K={score4K}, HD={scoreHD}");
    }
}
