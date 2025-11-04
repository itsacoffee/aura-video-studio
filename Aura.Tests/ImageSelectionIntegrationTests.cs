using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Visual;
using Aura.Core.Services.Assets;
using Aura.Core.Services.Visual;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for complete scene flow from visual prompt to selected asset
/// </summary>
public class ImageSelectionIntegrationTests
{
    private readonly VisualPromptGenerationService _promptService;
    private readonly ImageSelectionService _selectionService;
    private readonly AestheticScoringService _scoringService;

    public ImageSelectionIntegrationTests()
    {
        var promptLogger = NullLogger<VisualPromptGenerationService>.Instance;
        var selectionLogger = NullLogger<ImageSelectionService>.Instance;
        var scoringLogger = NullLogger<AestheticScoringService>.Instance;
        var stockLogger = NullLogger<StockImageService>.Instance;
        var continuityLogger = NullLogger<VisualContinuityEngine>.Instance;
        var optimizerLogger = NullLogger<PromptOptimizer>.Instance;

        var cinematography = new CinematographyKnowledgeBase();
        var continuityEngine = new VisualContinuityEngine(continuityLogger);
        var promptOptimizer = new PromptOptimizer(optimizerLogger);

        _promptService = new VisualPromptGenerationService(
            promptLogger,
            cinematography,
            continuityEngine,
            promptOptimizer);

        var httpClient = new HttpClient();
        var stockService = new StockImageService(stockLogger, httpClient);

        _scoringService = new AestheticScoringService(scoringLogger);
        _selectionService = new ImageSelectionService(selectionLogger, stockService, _scoringService);
    }

    [Fact]
    public async Task CompleteFlow_FromSceneToSelectedAsset_Should_Work()
    {
        var scene = new Scene(
            0,
            "Opening Scene",
            "A professional office setting with modern furniture and large windows",
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10));

        var brief = new Brief(
            "Corporate training video",
            null,
            null,
            "professional",
            "en",
            Aspect.Widescreen16x9);

        var prompt = await _promptService.GenerateVisualPromptForSceneAsync(
            scene,
            null,
            brief.Tone,
            VisualStyle.Realistic,
            70.0,
            50.0,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.NotNull(prompt);
        Assert.Equal(0, prompt.SceneIndex);
        Assert.NotEmpty(prompt.DetailedDescription);

        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 50.0,
            CandidatesPerScene = 3
        };

        var selectionResult = await _selectionService.SelectImageForSceneAsync(
            prompt,
            config,
            CancellationToken.None);

        Assert.NotNull(selectionResult);
        Assert.Equal(0, selectionResult.SceneIndex);
        Assert.NotEmpty(selectionResult.Candidates);
        Assert.True(selectionResult.SelectionTimeMs > 0);
    }

    [Fact]
    public async Task CompleteFlow_MultipleScenes_Should_SelectImagesForAll()
    {
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "An office workspace with computers", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Scene 2", "Team meeting in a conference room", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)),
            new Scene(2, "Scene 3", "Product demonstration with technology", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        };

        var brief = new Brief("Product demo", null, null, "professional", "en", Aspect.Widescreen16x9);

        var prompts = await _promptService.GenerateVisualPromptsAsync(
            scenes,
            brief,
            null,
            null,
            CancellationToken.None);

        Assert.Equal(3, prompts.Count);

        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 45.0,
            CandidatesPerScene = 2
        };

        var selectionResults = await _selectionService.SelectImagesForScenesAsync(
            prompts,
            config,
            CancellationToken.None);

        Assert.Equal(3, selectionResults.Count);
        Assert.All(selectionResults, result =>
        {
            Assert.NotEmpty(result.Candidates);
            Assert.True(result.SelectionTimeMs >= 0);
        });
    }

    [Fact]
    public async Task CompleteFlow_WithKeywords_Should_MatchInCandidates()
    {
        var scene = new Scene(
            0,
            "Nature Scene",
            "Beautiful sunset over mountains with vibrant colors",
            TimeSpan.Zero,
            TimeSpan.FromSeconds(8));

        var brief = new Brief("Nature documentary", null, null, "dramatic", "en", Aspect.Widescreen16x9);

        var prompt = await _promptService.GenerateVisualPromptForSceneAsync(
            scene,
            null,
            brief.Tone,
            VisualStyle.Cinematic,
            85.0,
            75.0,
            null,
            null,
            null,
            CancellationToken.None);

        prompt = prompt with
        {
            NarrativeKeywords = new[] { "sunset", "mountains", "nature", "landscape" }
        };

        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 55.0,
            CandidatesPerScene = 4
        };

        var selectionResult = await _selectionService.SelectImageForSceneAsync(
            prompt,
            config,
            CancellationToken.None);

        Assert.NotNull(selectionResult);
        Assert.Equal(prompt.NarrativeKeywords, selectionResult.NarrativeKeywords);
    }

    [Fact]
    public async Task CompleteFlow_Should_IncludeLicensingInformation()
    {
        var scene = new Scene(
            0,
            "Business Scene",
            "Modern office environment with professionals",
            TimeSpan.Zero,
            TimeSpan.FromSeconds(6));

        var brief = new Brief("Business presentation", null, null, "professional", "en", Aspect.Widescreen16x9);

        var prompt = await _promptService.GenerateVisualPromptForSceneAsync(
            scene,
            null,
            brief.Tone,
            VisualStyle.Realistic,
            60.0,
            50.0,
            null,
            null,
            null,
            CancellationToken.None);

        var selectionResult = await _selectionService.SelectImageForSceneAsync(
            prompt,
            null,
            CancellationToken.None);

        Assert.NotNull(selectionResult);
        Assert.All(selectionResult.Candidates, candidate =>
        {
            Assert.NotNull(candidate.Licensing);
            Assert.NotEmpty(candidate.Licensing.LicenseType);
            Assert.NotEmpty(candidate.Licensing.SourcePlatform);
            Assert.True(candidate.Licensing.CommercialUseAllowed);
        });
    }

    [Fact]
    public async Task CompleteFlow_Should_RankCandidatesByScore()
    {
        var scene = new Scene(
            0,
            "Tech Scene",
            "Modern technology and innovation showcase",
            TimeSpan.Zero,
            TimeSpan.FromSeconds(7));

        var brief = new Brief("Tech showcase", null, null, "modern", "en", Aspect.Widescreen16x9);

        var prompt = await _promptService.GenerateVisualPromptForSceneAsync(
            scene,
            null,
            brief.Tone,
            VisualStyle.Modern,
            75.0,
            60.0,
            null,
            null,
            null,
            CancellationToken.None);

        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = 40.0,
            CandidatesPerScene = 5
        };

        var selectionResult = await _selectionService.SelectImageForSceneAsync(
            prompt,
            config,
            CancellationToken.None);

        Assert.NotNull(selectionResult);

        if (selectionResult.Candidates.Count > 1)
        {
            for (int i = 1; i < selectionResult.Candidates.Count; i++)
            {
                Assert.True(
                    selectionResult.Candidates[i - 1].OverallScore >= selectionResult.Candidates[i].OverallScore,
                    $"Candidate {i - 1} score ({selectionResult.Candidates[i - 1].OverallScore}) should be >= candidate {i} score ({selectionResult.Candidates[i].OverallScore})");
            }
        }
    }

    [Fact]
    public async Task CompleteFlow_Should_MeetMinimumThreshold()
    {
        var scene = new Scene(
            0,
            "High Quality Scene",
            "Premium quality visual content",
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5));

        var brief = new Brief("Premium video", null, null, "cinematic", "en", Aspect.Widescreen16x9);

        var prompt = await _promptService.GenerateVisualPromptForSceneAsync(
            scene,
            null,
            brief.Tone,
            VisualStyle.Cinematic,
            90.0,
            80.0,
            null,
            null,
            null,
            CancellationToken.None);

        var threshold = 65.0;
        var config = new ImageSelectionConfig
        {
            MinimumAestheticThreshold = threshold,
            CandidatesPerScene = 3
        };

        var selectionResult = await _selectionService.SelectImageForSceneAsync(
            prompt,
            config,
            CancellationToken.None);

        Assert.NotNull(selectionResult);
        Assert.Equal(threshold, selectionResult.MinimumAestheticThreshold);

        if (selectionResult.SelectedImage != null && selectionResult.MeetsCriteria)
        {
            Assert.True(
                selectionResult.SelectedImage.OverallScore >= threshold,
                $"Selected image score ({selectionResult.SelectedImage.OverallScore}) should meet threshold ({threshold})");
        }
    }
}
