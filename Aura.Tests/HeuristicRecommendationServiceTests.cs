using Xunit;
using Aura.Core.Models;
using Aura.Core.Planner;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Tests;

public class HeuristicRecommendationServiceTests
{
    [Fact]
    public async Task GenerateRecommendationsAsync_Should_ReturnValidRecommendations()
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
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
        Assert.InRange(recommendations.ShotsPerScene, 1, 5);
        Assert.InRange(recommendations.BRollPercentage, 0, 100);
        Assert.InRange(recommendations.OverlayDensity, 0, 10);
        Assert.InRange(recommendations.ReadingLevel, 8, 16);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_Should_RespectSceneCountConstraints()
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
        var brief = new Brief(
            Topic: "Quick Tutorial",
            Audience: "General",
            Goal: "Inform",
            Tone: "Casual",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(10),
            Pacing: Pacing.Fast,
            Density: Density.Dense,
            Style: "Standard"
        );
        var constraints = new RecommendationConstraints(
            MaxSceneCount: 8,
            MinSceneCount: 5,
            MaxBRollPercentage: null,
            MaxReadingLevel: null
        );
        var request = new RecommendationRequest(brief, planSpec, null, constraints);

        // Act
        var recommendations = await service.GenerateRecommendationsAsync(request, CancellationToken.None);

        // Assert
        Assert.InRange(recommendations.SceneCount, 5, 8);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_Should_RespectBRollConstraints()
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
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
            Density: Density.Dense, // Would normally give 50%
            Style: "Standard"
        );
        var constraints = new RecommendationConstraints(
            MaxSceneCount: null,
            MinSceneCount: null,
            MaxBRollPercentage: 25.0,
            MaxReadingLevel: null
        );
        var request = new RecommendationRequest(brief, planSpec, null, constraints);

        // Act
        var recommendations = await service.GenerateRecommendationsAsync(request, CancellationToken.None);

        // Assert
        Assert.True(recommendations.BRollPercentage <= 25.0);
    }

    [Theory]
    [InlineData(Pacing.Chill, 2)]
    [InlineData(Pacing.Conversational, 3)]
    [InlineData(Pacing.Fast, 4)]
    public async Task GenerateRecommendationsAsync_Should_AdjustShotsPerSceneByPacing(Pacing pacing, int expectedShots)
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
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
            Pacing: pacing,
            Density: Density.Balanced,
            Style: "Standard"
        );
        var request = new RecommendationRequest(brief, planSpec, null, null);

        // Act
        var recommendations = await service.GenerateRecommendationsAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedShots, recommendations.ShotsPerScene);
    }

    [Theory]
    [InlineData(Density.Sparse, 15.0)]
    [InlineData(Density.Balanced, 30.0)]
    [InlineData(Density.Dense, 50.0)]
    public async Task GenerateRecommendationsAsync_Should_AdjustBRollByDensity(Density density, double expectedBRoll)
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
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
            Density: density,
            Style: "Standard"
        );
        var request = new RecommendationRequest(brief, planSpec, null, null);

        // Act
        var recommendations = await service.GenerateRecommendationsAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedBRoll, recommendations.BRollPercentage);
    }

    [Theory]
    [InlineData("Children", 8)]
    [InlineData("Teens", 10)]
    [InlineData("General", 12)]
    [InlineData("Professional", 14)]
    [InlineData("Academic", 16)]
    public async Task GenerateRecommendationsAsync_Should_AdjustReadingLevelByAudience(string audience, int expectedLevel)
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: audience,
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
        Assert.Equal(expectedLevel, recommendations.ReadingLevel);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_Should_GenerateVoiceRecommendations()
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Energetic",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Fast,
            Density: Density.Balanced,
            Style: "Standard"
        );
        var request = new RecommendationRequest(brief, planSpec, null, null);

        // Act
        var recommendations = await service.GenerateRecommendationsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(recommendations.Voice);
        Assert.InRange(recommendations.Voice.Rate, 0.5, 1.5);
        Assert.InRange(recommendations.Voice.Pitch, 0.5, 1.5);
        Assert.NotEmpty(recommendations.Voice.Style);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_Should_GenerateMusicRecommendations()
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Professional",
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
        Assert.NotNull(recommendations.Music);
        Assert.NotEmpty(recommendations.Music.Tempo);
        Assert.NotEmpty(recommendations.Music.Genre);
        Assert.NotEmpty(recommendations.Music.IntensityCurve);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_Should_GenerateCaptionStyle()
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Vertical9x16
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
        Assert.NotNull(recommendations.Captions);
        Assert.NotEmpty(recommendations.Captions.Position);
        Assert.NotEmpty(recommendations.Captions.FontSize);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_Should_GenerateThumbnailPrompt()
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
        var brief = new Brief(
            Topic: "Machine Learning Basics",
            Audience: "Students",
            Goal: "Educational",
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
        Assert.NotNull(recommendations.ThumbnailPrompt);
        Assert.NotEmpty(recommendations.ThumbnailPrompt);
        Assert.Contains("Machine Learning Basics", recommendations.ThumbnailPrompt);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_Should_GenerateSeoRecommendations()
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
        var brief = new Brief(
            Topic: "Introduction to Python Programming",
            Audience: "Beginners",
            Goal: "Educational",
            Tone: "Friendly",
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
        Assert.NotNull(recommendations.Seo);
        Assert.NotEmpty(recommendations.Seo.Title);
        Assert.NotEmpty(recommendations.Seo.Description);
        Assert.NotNull(recommendations.Seo.Tags);
        Assert.NotEmpty(recommendations.Seo.Tags);
        Assert.True(recommendations.Seo.Tags.Length <= 10);
    }

    [Fact]
    public async Task GenerateRecommendationsAsync_Should_GenerateOutlineWithCorrectSceneCount()
    {
        // Arrange
        var service = new HeuristicRecommendationService(NullLogger<HeuristicRecommendationService>.Instance);
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

        // Assert
        Assert.NotNull(recommendations.Outline);
        Assert.Contains("Introduction", recommendations.Outline);
        Assert.Contains("Conclusion", recommendations.Outline);
        // Count scene markers in outline
        var sceneMarkers = recommendations.Outline.Split('\n')
            .Where(line => line.Trim().Length > 0 && char.IsDigit(line.Trim()[0]))
            .Count();
        Assert.Equal(recommendations.SceneCount, sceneMarkers);
    }
}
