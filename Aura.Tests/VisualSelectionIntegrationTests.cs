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
/// Integration tests for visual selection workflow: brief→candidates→accept→render
/// </summary>
public class VisualSelectionIntegrationTests
{
    private readonly VisualSelectionService _selectionService;
    private readonly CandidateCacheService _cacheService;
    private readonly ImageSelectionService _imageSelectionService;
    private readonly AestheticScoringService _scoringService;

    public VisualSelectionIntegrationTests()
    {
        var selectionLogger = NullLogger<VisualSelectionService>.Instance;
        var cacheLogger = NullLogger<CandidateCacheService>.Instance;
        var imageLogger = NullLogger<ImageSelectionService>.Instance;
        var scoringLogger = NullLogger<AestheticScoringService>.Instance;
        var stockLogger = NullLogger<Aura.Core.Services.Assets.StockImageService>.Instance;

        _scoringService = new AestheticScoringService(scoringLogger);
        
        var httpClient = new System.Net.Http.HttpClient();
        var stockImageService = new Aura.Core.Services.Assets.StockImageService(
            stockLogger,
            httpClient,
            pexelsApiKey: null,
            pixabayApiKey: null);

        _imageSelectionService = new ImageSelectionService(
            imageLogger,
            stockImageService,
            _scoringService);

        _selectionService = new VisualSelectionService(selectionLogger, _imageSelectionService);
        _cacheService = new CandidateCacheService(cacheLogger);
    }

    [Fact]
    public async Task FullWorkflow_BriefToCandidatesToAccept_CompletesSuccessfully()
    {
        var jobId = "test-job-" + Guid.NewGuid().ToString();
        var sceneIndex = 0;

        var prompt = new VisualPrompt
        {
            SceneIndex = sceneIndex,
            DetailedDescription = "A professional office meeting with diverse team members",
            Subject = "business meeting",
            Framing = "medium shot",
            NarrativeKeywords = new[] { "professional", "collaborative", "diverse" },
            Style = VisualStyle.Documentary,
            QualityTier = VisualQualityTier.Standard
        };

        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 60.0,
            CandidatesPerScene = 5,
            PreferGeneratedImages = false
        };

        var requestId = _cacheService.GenerateRequestId(prompt, config);
        Assert.NotEmpty(requestId);

        var result = await _imageSelectionService.SelectImageForSceneAsync(
            prompt,
            config,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(sceneIndex, result.SceneIndex);
        Assert.NotEmpty(result.Candidates);

        await _cacheService.CacheCandidatesAsync(requestId, result);

        var cached = await _cacheService.GetCachedCandidatesAsync(requestId);
        Assert.NotNull(cached);
        Assert.Equal(result.Candidates.Count, cached.Result.Candidates.Count);

        if (result.SelectedImage != null)
        {
            var selection = await _selectionService.AcceptCandidateAsync(
                jobId,
                sceneIndex,
                result.SelectedImage,
                "test-user");

            Assert.NotNull(selection);
            Assert.Equal(jobId, selection.JobId);
            Assert.Equal(sceneIndex, selection.SceneIndex);
            Assert.Equal(SelectionState.Accepted, selection.State);
            Assert.Equal(result.SelectedImage.ImageUrl, selection.SelectedCandidate?.ImageUrl);
        }

        var retrievedSelection = await _selectionService.GetSelectionAsync(jobId, sceneIndex);
        if (result.SelectedImage != null)
        {
            Assert.NotNull(retrievedSelection);
            Assert.Equal(SelectionState.Accepted, retrievedSelection.State);
        }
    }

    [Fact]
    public async Task ThresholdTuning_FiltersCandidatesCorrectly()
    {
        var jobId = "test-threshold-" + Guid.NewGuid().ToString();
        var sceneIndex = 0;

        var prompt = new VisualPrompt
        {
            SceneIndex = sceneIndex,
            DetailedDescription = "A scenic mountain landscape at dawn",
            Subject = "mountain landscape",
            NarrativeKeywords = new[] { "majestic", "peaceful", "natural" },
            Style = VisualStyle.Realistic,
            QualityTier = VisualQualityTier.Enhanced
        };

        var lowThresholdConfig = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 40.0,
            CandidatesPerScene = 5
        };

        var highThresholdConfig = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 80.0,
            CandidatesPerScene = 5
        };

        var lowResult = await _imageSelectionService.SelectImageForSceneAsync(
            prompt,
            lowThresholdConfig,
            CancellationToken.None);

        var highResult = await _imageSelectionService.SelectImageForSceneAsync(
            prompt,
            highThresholdConfig,
            CancellationToken.None);

        Assert.NotNull(lowResult);
        Assert.NotNull(highResult);

        var lowPassing = lowResult.Candidates.Count(c => c.OverallScore >= lowThresholdConfig.MinimumAestheticThreshold);
        var highPassing = highResult.Candidates.Count(c => c.OverallScore >= highThresholdConfig.MinimumAestheticThreshold);

        Assert.True(lowPassing >= highPassing, "Lower threshold should have more or equal passing candidates");
    }

    [Fact]
    public async Task RegenerateCandidates_UpdatesSelectionCorrectly()
    {
        var jobId = "test-regenerate-" + Guid.NewGuid().ToString();
        var sceneIndex = 0;

        var initialPrompt = new VisualPrompt
        {
            SceneIndex = sceneIndex,
            DetailedDescription = "A city street at night",
            Subject = "urban scene",
            NarrativeKeywords = new[] { "urban", "night", "lights" },
            Style = VisualStyle.Cinematic,
            QualityTier = VisualQualityTier.Standard
        };

        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 60.0,
            CandidatesPerScene = 5
        };

        var initialResult = await _imageSelectionService.SelectImageForSceneAsync(
            initialPrompt,
            config,
            CancellationToken.None);

        if (initialResult.SelectedImage != null)
        {
            await _selectionService.AcceptCandidateAsync(
                jobId,
                sceneIndex,
                initialResult.SelectedImage);
        }

        var refinedPrompt = initialPrompt with
        {
            DetailedDescription = "A vibrant city street at night with neon lights"
        };

        var selection = await _selectionService.RegenerateCandidatesAsync(
            jobId,
            sceneIndex,
            refinedPrompt,
            config);

        Assert.NotNull(selection);
        Assert.Equal(SelectionState.Pending, selection.State);
        Assert.NotEmpty(selection.Candidates);
        Assert.True(selection.Metadata.RegenerationCount >= 1);
        Assert.True(selection.Metadata.LlmAssistedRefinement);
    }

    [Fact]
    public async Task AutoSelectionEvaluation_WorksCorrectly()
    {
        var candidates = new List<ImageCandidate>
        {
            new ImageCandidate
            {
                ImageUrl = "https://example.com/high.jpg",
                Source = "Test",
                OverallScore = 90.0,
                AestheticScore = 92.0,
                KeywordCoverageScore = 88.0,
                QualityScore = 90.0,
                Reasoning = "Excellent quality",
                Width = 1920,
                Height = 1080
            },
            new ImageCandidate
            {
                ImageUrl = "https://example.com/medium.jpg",
                Source = "Test",
                OverallScore = 70.0,
                AestheticScore = 70.0,
                KeywordCoverageScore = 70.0,
                QualityScore = 70.0,
                Reasoning = "Good quality",
                Width = 1920,
                Height = 1080
            }
        };

        var decision = _selectionService.EvaluateAutoSelection(candidates, 85.0);

        Assert.NotNull(decision);
        Assert.True(decision.ShouldAutoSelect);
        Assert.NotNull(decision.SelectedCandidate);
        Assert.Equal(candidates[0].ImageUrl, decision.SelectedCandidate.ImageUrl);
        Assert.True(decision.Confidence >= 85.0);
    }
}
