using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Content;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class PacingOptimizerTests
{
    private readonly PacingOptimizer _optimizer;

    public PacingOptimizerTests()
    {
        var logger = NullLogger<PacingOptimizer>.Instance;
        _optimizer = new PacingOptimizer(logger);
    }

    [Fact]
    public async Task OptimizeTimingAsync_Should_DetectFastPacing()
    {
        // Arrange - Scene with too many words for duration (fast pacing)
        var scenes = new List<Scene>
        {
            new Scene(
                Index: 0,
                Heading: "Introduction",
                Script: "This is a very long script with many words that will be spoken too quickly for comfortable viewing and comprehension by the audience watching the video.",
                Start: TimeSpan.Zero,
                Duration: TimeSpan.FromSeconds(3) // Too short for word count
            )
        };

        var timeline = new Aura.Core.Providers.Timeline(
            Scenes: scenes,
            SceneAssets: new Dictionary<int, IReadOnlyList<Asset>>(),
            NarrationPath: "",
            MusicPath: "",
            SubtitlesPath: null
        );

        // Act
        var optimization = await _optimizer.OptimizeTimingAsync(timeline);

        // Assert
        Assert.NotNull(optimization);
        Assert.NotEmpty(optimization.Suggestions);
        Assert.Equal(PacingPriority.Critical, optimization.Suggestions[0].Priority);
        Assert.Contains("too fast", optimization.Suggestions[0].Reasoning);
    }

    [Fact]
    public async Task OptimizeTimingAsync_Should_DetectSlowPacing()
    {
        // Arrange - Scene with too few words for duration (slow pacing)
        var scenes = new List<Scene>
        {
            new Scene(
                Index: 0,
                Heading: "Introduction",
                Script: "Hello there.",
                Start: TimeSpan.Zero,
                Duration: TimeSpan.FromSeconds(10) // Too long for word count
            )
        };

        var timeline = new Aura.Core.Providers.Timeline(
            Scenes: scenes,
            SceneAssets: new Dictionary<int, IReadOnlyList<Asset>>(),
            NarrationPath: "",
            MusicPath: "",
            SubtitlesPath: null
        );

        // Act
        var optimization = await _optimizer.OptimizeTimingAsync(timeline);

        // Assert
        Assert.NotNull(optimization);
        Assert.NotEmpty(optimization.Suggestions);
        Assert.Contains("too slow", optimization.Suggestions[0].Reasoning);
    }

    [Fact]
    public async Task OptimizeTimingAsync_Should_AcceptGoodPacing()
    {
        // Arrange - Scene with appropriate pacing (2.5 words per second)
        var script = string.Join(" ", Enumerable.Repeat("word", 25)); // 25 words
        var scenes = new List<Scene>
        {
            new Scene(
                Index: 0,
                Heading: "Introduction",
                Script: script,
                Start: TimeSpan.Zero,
                Duration: TimeSpan.FromSeconds(10) // 2.5 words/sec - ideal
            )
        };

        var timeline = new Aura.Core.Providers.Timeline(
            Scenes: scenes,
            SceneAssets: new Dictionary<int, IReadOnlyList<Asset>>(),
            NarrationPath: "",
            MusicPath: "",
            SubtitlesPath: null
        );

        // Act
        var optimization = await _optimizer.OptimizeTimingAsync(timeline);

        // Assert
        Assert.NotNull(optimization);
        // Should have no suggestions for good pacing (within tolerance)
        Assert.Empty(optimization.Suggestions);
        Assert.Contains("well-balanced", optimization.OverallAssessment);
    }

    [Fact]
    public async Task OptimizeTimingAsync_Should_ConsiderOpeningScenePacing()
    {
        // Arrange - Opening scene should have slightly faster pacing
        var script = string.Join(" ", Enumerable.Repeat("word", 20));
        var scenes = new List<Scene>
        {
            new Scene(
                Index: 0,
                Heading: "Opening",
                Script: script,
                Start: TimeSpan.Zero,
                Duration: TimeSpan.FromSeconds(10) // 2 words/sec - too slow for opening
            )
        };

        var timeline = new Aura.Core.Providers.Timeline(
            Scenes: scenes,
            SceneAssets: new Dictionary<int, IReadOnlyList<Asset>>(),
            NarrationPath: "",
            MusicPath: "",
            SubtitlesPath: null
        );

        // Act
        var optimization = await _optimizer.OptimizeTimingAsync(timeline);

        // Assert
        Assert.NotNull(optimization);
        if (optimization.Suggestions.Any())
        {
            Assert.Contains("Opening", optimization.Suggestions[0].Reasoning);
        }
    }

    [Fact]
    public async Task OptimizeTimingAsync_Should_HandleMultipleScenes()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Hello world this is a test.", TimeSpan.Zero, TimeSpan.FromSeconds(1)), // Too fast
            new Scene(1, "Scene 2", "Another test scene here.", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)), // Too slow
            new Scene(2, "Scene 3", string.Join(" ", Enumerable.Repeat("word", 25)), TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(10)) // Good
        };

        var timeline = new Aura.Core.Providers.Timeline(
            Scenes: scenes,
            SceneAssets: new Dictionary<int, IReadOnlyList<Asset>>(),
            NarrationPath: "",
            MusicPath: "",
            SubtitlesPath: null
        );

        // Act
        var optimization = await _optimizer.OptimizeTimingAsync(timeline);

        // Assert
        Assert.NotNull(optimization);
        Assert.NotEmpty(optimization.Suggestions);
        Assert.Contains("3 scenes", optimization.OverallAssessment);
    }
}
