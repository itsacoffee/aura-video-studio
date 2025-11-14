using Xunit;
using Aura.Core.Models;
using Aura.Core.Planner;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Tests;

public class PlannerServiceTests
{
    [Fact]
    public async Task PlannerService_Should_UseRuleBased_WhenNoLlmProvidersAvailable()
    {
        // Arrange
        var providers = new Dictionary<string, ILlmPlannerProvider>
        {
            ["RuleBased"] = new HeuristicRecommendationService(
                NullLogger<HeuristicRecommendationService>.Instance)
        };
        var service = new PlannerService(
            NullLogger<PlannerService>.Instance,
            providers,
            "ProIfAvailable");

        var brief = new Brief(
            Topic: "Machine Learning Basics",
            Audience: "Students",
            Goal: "Educational",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational"
        );
        var request = new RecommendationRequest(brief, planSpec, null, null);

        // Act
        var recommendations = await service.GenerateRecommendationsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations.Outline);
        Assert.InRange(recommendations.SceneCount, 3, 20);
        Assert.Equal("RuleBased", recommendations.ProviderUsed);
        Assert.InRange(recommendations.QualityScore, 0.6, 0.8);
        Assert.NotNull(recommendations.ExplainabilityNotes);
    }

    [Fact]
    public async Task PlannerService_Should_ReturnOutline_WithMinimumSceneCount()
    {
        // Arrange
        var providers = new Dictionary<string, ILlmPlannerProvider>
        {
            ["RuleBased"] = new HeuristicRecommendationService(
                NullLogger<HeuristicRecommendationService>.Instance)
        };
        var service = new PlannerService(
            NullLogger<PlannerService>.Instance,
            providers,
            "Free");

        var brief = new Brief(
            Topic: "Quick Tutorial",
            Audience: "General",
            Goal: "Inform",
            Tone: "Casual",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Fast,
            Density: Density.Sparse,
            Style: "Standard"
        );
        var request = new RecommendationRequest(brief, planSpec, null, null);

        // Act
        var recommendations = await service.GenerateRecommendationsAsync(request, CancellationToken.None);

        // Assert - At least 3 scenes even for short videos
        Assert.InRange(recommendations.SceneCount, 3, 10);
        
        // Verify outline structure
        Assert.Contains("Introduction", recommendations.Outline);
        Assert.Contains("Conclusion", recommendations.Outline);
    }

    [Fact]
    public async Task PlannerService_Should_FallbackToRuleBased_WhenPrimaryProviderFails()
    {
        // Arrange
        var failingProvider = new FailingPlannerProvider();
        var providers = new Dictionary<string, ILlmPlannerProvider>
        {
            ["OpenAI"] = failingProvider,
            ["RuleBased"] = new HeuristicRecommendationService(
                NullLogger<HeuristicRecommendationService>.Instance)
        };
        var service = new PlannerService(
            NullLogger<PlannerService>.Instance,
            providers,
            "ProIfAvailable");

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Standard"
        );
        var request = new RecommendationRequest(brief, planSpec, null, null);

        // Act
        var recommendations = await service.GenerateRecommendationsAsync(request, CancellationToken.None);

        // Assert - Should fall back to RuleBased
        Assert.NotNull(recommendations);
        Assert.Equal("RuleBased", recommendations.ProviderUsed);
        Assert.NotEmpty(recommendations.Outline);
    }

    [Fact]
    public async Task PlannerService_Should_IncludeQualityMetrics()
    {
        // Arrange
        var providers = new Dictionary<string, ILlmPlannerProvider>
        {
            ["RuleBased"] = new HeuristicRecommendationService(
                NullLogger<HeuristicRecommendationService>.Instance)
        };
        var service = new PlannerService(
            NullLogger<PlannerService>.Instance,
            providers,
            "Free");

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Standard"
        );
        var request = new RecommendationRequest(brief, planSpec, null, null);

        // Act
        var recommendations = await service.GenerateRecommendationsAsync(request, CancellationToken.None);

        // Assert
        Assert.InRange(recommendations.QualityScore, 0.0, 1.0);
        Assert.NotNull(recommendations.ProviderUsed);
        Assert.NotEmpty(recommendations.ProviderUsed);
        Assert.NotNull(recommendations.ExplainabilityNotes);
        Assert.NotEmpty(recommendations.ExplainabilityNotes);
    }

    [Fact]
    public void PlannerService_Should_ThrowException_WhenNoProvidersAvailable()
    {
        // Arrange
        var providers = new Dictionary<string, ILlmPlannerProvider>();
        var service = new PlannerService(
            NullLogger<PlannerService>.Instance,
            providers,
            "Free");

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Standard"
        );
        var request = new RecommendationRequest(brief, planSpec, null, null);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.GenerateRecommendationsAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task PlannerService_Should_GenerateAllRequiredFields()
    {
        // Arrange
        var providers = new Dictionary<string, ILlmPlannerProvider>
        {
            ["RuleBased"] = new HeuristicRecommendationService(
                NullLogger<HeuristicRecommendationService>.Instance)
        };
        var service = new PlannerService(
            NullLogger<PlannerService>.Instance,
            providers,
            "Free");

        var brief = new Brief(
            Topic: "Comprehensive Test",
            Audience: "Professional",
            Goal: "Educate",
            Tone: "Professional",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(10),
            Pacing: Pacing.Conversational,
            Density: Density.Dense,
            Style: "Professional"
        );
        var request = new RecommendationRequest(brief, planSpec, null, null);

        // Act
        var recommendations = await service.GenerateRecommendationsAsync(request, CancellationToken.None);

        // Assert - All fields are populated
        Assert.NotNull(recommendations.Outline);
        Assert.NotEmpty(recommendations.Outline);
        Assert.True(recommendations.SceneCount > 0);
        Assert.True(recommendations.ShotsPerScene > 0);
        Assert.InRange(recommendations.BRollPercentage, 0, 100);
        Assert.True(recommendations.OverlayDensity >= 0);
        Assert.True(recommendations.ReadingLevel > 0);
        Assert.NotNull(recommendations.Voice);
        Assert.NotNull(recommendations.Music);
        Assert.NotNull(recommendations.Captions);
        Assert.NotNull(recommendations.ThumbnailPrompt);
        Assert.NotEmpty(recommendations.ThumbnailPrompt);
        Assert.NotNull(recommendations.Seo);
        Assert.NotNull(recommendations.Seo.Title);
        Assert.NotEmpty(recommendations.Seo.Title);
        Assert.NotNull(recommendations.Seo.Description);
        Assert.NotEmpty(recommendations.Seo.Description);
        Assert.NotNull(recommendations.Seo.Tags);
        Assert.NotEmpty(recommendations.Seo.Tags);
    }

    // Helper class for testing fallback
    private sealed class FailingPlannerProvider : ILlmPlannerProvider
    {
        public Task<PlannerRecommendations> GenerateRecommendationsAsync(
            RecommendationRequest request,
            CancellationToken ct = default)
        {
            throw new InvalidOperationException("Simulated provider failure");
        }
    }
}
