using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.VideoOptimization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.VideoOptimization;

public class ABTestingServiceTests
{
    private readonly Mock<ILogger<ABTestingService>> _mockLogger;
    private readonly Mock<ILogger<AttentionPredictionService>> _mockAttentionLogger;
    private readonly AttentionPredictionService _attentionService;
    private readonly ABTestingService _service;

    public ABTestingServiceTests()
    {
        _mockLogger = new Mock<ILogger<ABTestingService>>();
        _mockAttentionLogger = new Mock<ILogger<AttentionPredictionService>>();
        _attentionService = new AttentionPredictionService(_mockAttentionLogger.Object);
        _service = new ABTestingService(_mockLogger.Object, _attentionService);
    }

    [Fact]
    public async Task CompareStrategiesAsync_WithMultipleStrategies_ReturnsWinner()
    {
        // Arrange
        var baseScenes = CreateTestScenes(5);
        var strategies = new List<PacingStrategy>
        {
            new PacingStrategy("Strategy A", "First strategy", baseScenes, PacingApproach.Balanced),
            new PacingStrategy("Strategy B", "Second strategy", baseScenes, PacingApproach.FastPaced),
        };
        var options = new ABTestOptions();

        // Act
        var result = await _service.CompareStrategiesAsync(strategies, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.WinningStrategy);
        Assert.Equal(strategies.Count, result.StrategyResults.Count);
        Assert.NotEmpty(result.Comparison);
        Assert.NotEmpty(result.Recommendation);
    }

    [Fact]
    public async Task CompareStrategiesAsync_WithLessThanTwoStrategies_ThrowsArgumentException()
    {
        // Arrange
        var strategies = new List<PacingStrategy>
        {
            new PacingStrategy("Only One", "Single strategy", CreateTestScenes(3), PacingApproach.Balanced),
        };
        var options = new ABTestOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CompareStrategiesAsync(strategies, options));
    }

    [Fact]
    public async Task CompareStrategiesAsync_RanksStrategiesByCompositeScore()
    {
        // Arrange
        var baseScenes = CreateTestScenes(5);
        var strategies = new List<PacingStrategy>
        {
            new PacingStrategy("Strategy A", "First", baseScenes, PacingApproach.Balanced),
            new PacingStrategy("Strategy B", "Second", baseScenes, PacingApproach.FastPaced),
            new PacingStrategy("Strategy C", "Third", baseScenes, PacingApproach.SlowPaced),
        };
        var options = new ABTestOptions();

        // Act
        var result = await _service.CompareStrategiesAsync(strategies, options);

        // Assert
        var sortedResults = result.StrategyResults.OrderByDescending(r => r.CompositeScore).ToList();
        Assert.Equal(result.WinningStrategy.CompositeScore, sortedResults[0].CompositeScore);
    }

    [Fact]
    public async Task GenerateVariantsAsync_CreatesMultipleStrategies()
    {
        // Arrange
        var baseScenes = CreateTestScenes(5);
        var options = new VariantGenerationOptions();

        // Act
        var variants = await _service.GenerateVariantsAsync(baseScenes, options);

        // Assert
        Assert.NotEmpty(variants);
        Assert.Contains(variants, v => v.ApproachType == PacingApproach.Balanced);
        Assert.Contains(variants, v => v.ApproachType == PacingApproach.FastPaced);
        Assert.Contains(variants, v => v.ApproachType == PacingApproach.SlowPaced);
        Assert.Contains(variants, v => v.ApproachType == PacingApproach.Dynamic);
    }

    [Fact]
    public async Task GenerateVariantsAsync_WithSelectiveOptions_CreatesOnlyRequestedVariants()
    {
        // Arrange
        var baseScenes = CreateTestScenes(5);
        var options = new VariantGenerationOptions(
            IncludeFastPaced: true,
            IncludeSlowPaced: false,
            IncludeDynamic: false
        );

        // Act
        var variants = await _service.GenerateVariantsAsync(baseScenes, options);

        // Assert
        Assert.Contains(variants, v => v.ApproachType == PacingApproach.FastPaced);
        Assert.DoesNotContain(variants, v => v.ApproachType == PacingApproach.SlowPaced);
        Assert.DoesNotContain(variants, v => v.ApproachType == PacingApproach.Dynamic);
    }

    [Fact]
    public async Task GenerateVariantsAsync_FastPacedVariantHasShorterDuration()
    {
        // Arrange
        var baseScenes = CreateTestScenes(5);
        var options = new VariantGenerationOptions();

        // Act
        var variants = await _service.GenerateVariantsAsync(baseScenes, options);

        // Assert
        var original = variants.First(v => v.ApproachType == PacingApproach.Balanced);
        var fastPaced = variants.First(v => v.ApproachType == PacingApproach.FastPaced);

        var originalDuration = original.Scenes.Sum(s => s.Duration.TotalSeconds);
        var fastDuration = fastPaced.Scenes.Sum(s => s.Duration.TotalSeconds);

        Assert.True(fastDuration < originalDuration);
    }

    [Fact]
    public async Task CompareStrategiesAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var baseScenes = CreateTestScenes(3);
        var strategies = new List<PacingStrategy>
        {
            new PacingStrategy("A", "First", baseScenes, PacingApproach.Balanced),
            new PacingStrategy("B", "Second", baseScenes, PacingApproach.FastPaced),
        };
        var options = new ABTestOptions();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.CompareStrategiesAsync(strategies, options, cts.Token));
    }

    private List<Scene> CreateTestScenes(int count)
    {
        var scenes = new List<Scene>();
        var cumulativeStart = TimeSpan.Zero;

        for (int i = 0; i < count; i++)
        {
            var duration = TimeSpan.FromSeconds(10 + (i * 2));
            var script = GenerateScript(duration);
            
            scenes.Add(new Scene(
                Index: i,
                Heading: $"Scene {i + 1}",
                Script: script,
                Start: cumulativeStart,
                Duration: duration
            ));

            cumulativeStart += duration;
        }

        return scenes;
    }

    private string GenerateScript(TimeSpan duration)
    {
        var wordCount = (int)(duration.TotalSeconds * 2.5);
        var words = Enumerable.Range(0, wordCount).Select(i => $"word{i}");
        return string.Join(" ", words);
    }
}
