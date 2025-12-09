using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Director;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Director;

public class AIDirectorServiceTests
{
    private readonly Mock<ILogger<AIDirectorService>> _loggerMock;
    private readonly Mock<ILogger<EmotionalArcAnalyzer>> _analyzerLoggerMock;
    private readonly Mock<ILlmProvider> _llmProviderMock;

    public AIDirectorServiceTests()
    {
        _loggerMock = new Mock<ILogger<AIDirectorService>>();
        _analyzerLoggerMock = new Mock<ILogger<EmotionalArcAnalyzer>>();
        _llmProviderMock = new Mock<ILlmProvider>();
    }

    private AIDirectorService CreateService(ILlmProvider? llmProvider = null)
    {
        var analyzer = new EmotionalArcAnalyzer(_analyzerLoggerMock.Object, llmProvider);
        return new AIDirectorService(_loggerMock.Object, analyzer);
    }

    private static List<Scene> CreateTestScenes(int count = 3)
    {
        return Enumerable.Range(0, count)
            .Select(i => new Scene(
                i,
                $"Scene {i + 1}",
                $"This is the content for scene {i + 1}. It discusses important concepts.",
                TimeSpan.FromSeconds(i * 10),
                TimeSpan.FromSeconds(10)))
            .ToList();
    }

    private static Brief CreateTestBrief(string tone = "professional")
    {
        return new Brief(
            "Test Topic",
            "General",
            "Inform",
            tone,
            "en",
            Aspect.Widescreen16x9);
    }

    [Fact]
    public async Task AnalyzeAndDirectAsync_Documentary_SelectsStableMotion()
    {
        // Arrange
        var scenes = CreateTestScenes();
        var brief = CreateTestBrief("neutral");
        var service = CreateService();

        // Act
        var decisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.Documentary, CancellationToken.None);

        // Assert
        Assert.NotNull(decisions);
        Assert.Equal(3, decisions.SceneDirections.Count);
        Assert.Equal("Documentary", decisions.OverallStyle);

        // Documentary preset should use stable motions (None, ZoomIn, PanLeft, PanRight)
        Assert.All(decisions.SceneDirections, d =>
        {
            Assert.True(
                d.Motion is KenBurnsMotion.None 
                or KenBurnsMotion.ZoomIn 
                or KenBurnsMotion.PanLeft 
                or KenBurnsMotion.PanRight,
                $"Expected stable motion, got {d.Motion}");
        });
    }

    [Fact]
    public async Task AnalyzeAndDirectAsync_TikTokEnergy_SelectsDynamicMotion()
    {
        // Arrange
        var scenes = CreateTestScenes(5);
        var brief = new Brief(
            "Test",
            "Young Adults",
            "Entertain",
            "Energetic",
            "en",
            Aspect.Vertical9x16);
        var service = CreateService();

        // Act
        var decisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.TikTokEnergy, CancellationToken.None);

        // Assert
        Assert.NotNull(decisions);
        Assert.Equal(5, decisions.SceneDirections.Count);
        Assert.Equal("TikTokEnergy", decisions.OverallStyle);

        // TikTok preset should have at least some dynamic motion
        Assert.True(decisions.SceneDirections.Any(d => d.Motion != KenBurnsMotion.None),
            "TikTok preset should include motion effects");

        // TikTok scenes should be shorter
        Assert.True(
            decisions.SceneDirections.Average(d => d.SuggestedDuration.TotalSeconds) <= 5,
            "TikTok scenes should average 5 seconds or less");
    }

    [Fact]
    public async Task AnalyzeAndDirectAsync_Cinematic_SelectsDramaticTransitions()
    {
        // Arrange
        var scenes = CreateTestScenes(4);
        var brief = CreateTestBrief("dramatic");
        var service = CreateService();

        // Act
        var decisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.Cinematic, CancellationToken.None);

        // Assert
        Assert.NotNull(decisions);
        Assert.Equal("Cinematic", decisions.OverallStyle);

        // Cinematic should have smooth transitions (not cuts)
        var nonCutTransitions = decisions.SceneDirections
            .Count(d => d.InTransition != DirectorTransitionType.Cut);
        Assert.True(nonCutTransitions > 0, 
            "Cinematic preset should include non-cut transitions");

        // First scene should fade in
        Assert.Equal(DirectorTransitionType.Fade, decisions.SceneDirections[0].InTransition);
        
        // Last scene should fade out
        Assert.Equal(DirectorTransitionType.Fade, decisions.SceneDirections[^1].OutTransition);
    }

    [Fact]
    public async Task AnalyzeAndDirectAsync_Corporate_SelectsSubtleMotion()
    {
        // Arrange
        var scenes = CreateTestScenes();
        var brief = CreateTestBrief("professional");
        var service = CreateService();

        // Act
        var decisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.Corporate, CancellationToken.None);

        // Assert
        Assert.NotNull(decisions);
        Assert.Equal("Corporate", decisions.OverallStyle);

        // Corporate preset should have minimal motion
        var noMotionCount = decisions.SceneDirections.Count(d => d.Motion == KenBurnsMotion.None);
        Assert.True(noMotionCount >= decisions.SceneDirections.Count / 2,
            "Corporate preset should have minimal motion in most scenes");
    }

    [Fact]
    public async Task AnalyzeAndDirectAsync_Educational_EmphasizesKeyPoints()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new(0, "Introduction", "This is the key concept to understand.", TimeSpan.Zero, TimeSpan.FromSeconds(10)),
            new(1, "Explanation", "Let me explain this important topic.", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)),
            new(2, "Summary", "Remember the key takeaways.", TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10)),
        };
        var brief = new Brief("Test", "Students", "Educate", "clear", "en", Aspect.Widescreen16x9);
        var service = CreateService();

        // Act
        var decisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.Educational, CancellationToken.None);

        // Assert
        Assert.NotNull(decisions);
        Assert.Equal("Educational", decisions.OverallStyle);

        // Educational should use cross-dissolve transitions for comprehension
        Assert.All(decisions.SceneDirections.Skip(1), d =>
        {
            Assert.True(
                d.InTransition is DirectorTransitionType.CrossDissolve or DirectorTransitionType.Fade,
                "Educational preset should use smooth transitions");
        });
    }

    [Fact]
    public void SmoothTransitions_PreventsJarringSequences()
    {
        // Arrange
        var directions = new List<SceneDirection>
        {
            new(0, KenBurnsMotion.ZoomIn, DirectorTransitionType.Wipe, DirectorTransitionType.Wipe, 0.5, "center", TimeSpan.FromSeconds(5)),
            new(1, KenBurnsMotion.ZoomOut, DirectorTransitionType.Wipe, DirectorTransitionType.Wipe, 0.5, "center", TimeSpan.FromSeconds(5)),
            new(2, KenBurnsMotion.ZoomIn, DirectorTransitionType.Wipe, DirectorTransitionType.Cut, 0.5, "center", TimeSpan.FromSeconds(5))
        };
        var service = CreateService();

        // Act
        var smoothed = service.SmoothTransitions(directions);

        // Assert - no back-to-back wipes
        for (int i = 1; i < smoothed.Count; i++)
        {
            if (smoothed[i - 1].OutTransition == DirectorTransitionType.Wipe)
            {
                Assert.NotEqual(DirectorTransitionType.Wipe, smoothed[i].InTransition);
            }
        }
    }

    [Fact]
    public void SmoothTransitions_PreventsRepeatedMotions()
    {
        // Arrange
        var directions = new List<SceneDirection>
        {
            new(0, KenBurnsMotion.ZoomIn, DirectorTransitionType.Cut, DirectorTransitionType.Cut, 0.5, "center", TimeSpan.FromSeconds(5)),
            new(1, KenBurnsMotion.ZoomIn, DirectorTransitionType.Cut, DirectorTransitionType.Cut, 0.5, "center", TimeSpan.FromSeconds(5)),
            new(2, KenBurnsMotion.ZoomIn, DirectorTransitionType.Cut, DirectorTransitionType.Cut, 0.5, "center", TimeSpan.FromSeconds(5))
        };
        var service = CreateService();

        // Act
        var smoothed = service.SmoothTransitions(directions);

        // Assert - adjacent scenes should have different motions
        for (int i = 1; i < smoothed.Count; i++)
        {
            Assert.NotEqual(smoothed[i - 1].Motion, smoothed[i].Motion);
        }
    }

    [Fact]
    public async Task AnalyzeAndDirectAsync_Custom_ReturnsNoMotion()
    {
        // Arrange
        var scenes = CreateTestScenes();
        var brief = CreateTestBrief();
        var service = CreateService();

        // Act
        var decisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.Custom, CancellationToken.None);

        // Assert
        Assert.NotNull(decisions);
        Assert.Equal("Custom", decisions.OverallStyle);

        // Custom preset should have no automatic motion
        Assert.All(decisions.SceneDirections, d =>
        {
            Assert.Equal(KenBurnsMotion.None, d.Motion);
        });
    }

    [Fact]
    public async Task AnalyzeAndDirectAsync_EmptyScenes_ReturnsEmptyDecisions()
    {
        // Arrange
        var scenes = new List<Scene>();
        var brief = CreateTestBrief();
        var service = CreateService();

        // Act
        var decisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.Documentary, CancellationToken.None);

        // Assert
        Assert.NotNull(decisions);
        Assert.Empty(decisions.SceneDirections);
    }

    [Fact]
    public async Task AnalyzeAndDirectAsync_SetsEmotionalArcFromAnalysis()
    {
        // Arrange
        var scenes = CreateTestScenes();
        var brief = CreateTestBrief("exciting");
        var service = CreateService();

        // Act
        var decisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.Storytelling, CancellationToken.None);

        // Assert
        Assert.NotNull(decisions);
        Assert.NotEmpty(decisions.EmotionalArc);
        Assert.Contains(scenes.Count.ToString(), decisions.EmotionalArc);
    }

    [Fact]
    public async Task AnalyzeAndDirectAsync_FirstAndLastScenesFade()
    {
        // Arrange
        var scenes = CreateTestScenes(5);
        var brief = CreateTestBrief();
        var service = CreateService();

        // Act
        var decisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.Documentary, CancellationToken.None);

        // Assert
        Assert.Equal(DirectorTransitionType.Fade, decisions.SceneDirections[0].InTransition);
        Assert.Equal(DirectorTransitionType.Fade, decisions.SceneDirections[^1].OutTransition);
    }

    [Fact]
    public async Task AnalyzeAndDirectAsync_KenBurnsIntensityMatchesPreset()
    {
        // Arrange
        var scenes = CreateTestScenes();
        var brief = CreateTestBrief();
        var service = CreateService();

        // Act - TikTok should have higher intensity
        var tikTokDecisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.TikTokEnergy, CancellationToken.None);
        
        // Act - Corporate should have lower intensity
        var corpDecisions = await service.AnalyzeAndDirectAsync(
            scenes, brief, DirectorPreset.Corporate, CancellationToken.None);

        // Assert
        var tikTokAvgIntensity = tikTokDecisions.SceneDirections.Average(d => d.KenBurnsIntensity);
        var corpAvgIntensity = corpDecisions.SceneDirections.Average(d => d.KenBurnsIntensity);

        Assert.True(tikTokAvgIntensity > corpAvgIntensity,
            "TikTok preset should have higher Ken Burns intensity than Corporate");
    }
}
