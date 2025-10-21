using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Aura.Core.Models.Timeline;
using Aura.Core.Services.Render;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class SmartRendererTests
{
    private readonly Mock<ILogger<SmartRenderer>> _mockLogger;
    private readonly string _tempCacheDir;

    public SmartRendererTests()
    {
        _mockLogger = new Mock<ILogger<SmartRenderer>>();
        _tempCacheDir = Path.Combine(Path.GetTempPath(), "aura_test_cache_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempCacheDir);
    }

    [Fact]
    public void CalculateSceneChecksum_ReturnsSameChecksumForIdenticalScenes()
    {
        // Arrange
        var renderer = new SmartRenderer(_mockLogger.Object, _tempCacheDir);
        var scene = new TimelineScene(
            Index: 0,
            Heading: "Test Scene",
            Script: "This is a test script",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5)
        );

        // Act
        var checksum1 = renderer.CalculateSceneChecksum(scene);
        var checksum2 = renderer.CalculateSceneChecksum(scene);

        // Assert
        Assert.Equal(checksum1, checksum2);
        Assert.NotEmpty(checksum1);
    }

    [Fact]
    public void CalculateSceneChecksum_ReturnsDifferentChecksumForDifferentScenes()
    {
        // Arrange
        var renderer = new SmartRenderer(_mockLogger.Object, _tempCacheDir);
        var scene1 = new TimelineScene(
            Index: 0,
            Heading: "Test Scene 1",
            Script: "Script 1",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5)
        );
        var scene2 = new TimelineScene(
            Index: 0,
            Heading: "Test Scene 2",
            Script: "Script 2",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5)
        );

        // Act
        var checksum1 = renderer.CalculateSceneChecksum(scene1);
        var checksum2 = renderer.CalculateSceneChecksum(scene2);

        // Assert
        Assert.NotEqual(checksum1, checksum2);
    }

    [Fact]
    public void CalculateSceneChecksum_IncludesDuration()
    {
        // Arrange
        var renderer = new SmartRenderer(_mockLogger.Object, _tempCacheDir);
        var scene1 = new TimelineScene(
            Index: 0,
            Heading: "Test Scene",
            Script: "Script",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5)
        );
        var scene2 = scene1 with { Duration = TimeSpan.FromSeconds(10) };

        // Act
        var checksum1 = renderer.CalculateSceneChecksum(scene1);
        var checksum2 = renderer.CalculateSceneChecksum(scene2);

        // Assert
        Assert.NotEqual(checksum1, checksum2);
    }

    [Fact]
    public async Task GenerateRenderPlanAsync_AllNewScenes_MarksAllAsNew()
    {
        // Arrange
        var renderer = new SmartRenderer(_mockLogger.Object, _tempCacheDir);
        var timeline = new EditableTimeline();
        timeline.AddScene(new TimelineScene(
            Index: 0,
            Heading: "Scene 1",
            Script: "Script 1",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5)
        ));
        timeline.AddScene(new TimelineScene(
            Index: 1,
            Heading: "Scene 2",
            Script: "Script 2",
            Start: TimeSpan.FromSeconds(5),
            Duration: TimeSpan.FromSeconds(5)
        ));

        // Act
        var plan = await renderer.GenerateRenderPlanAsync(timeline, "test_job_1");

        // Assert
        Assert.Equal(2, plan.TotalScenes);
        Assert.Equal(0, plan.UnmodifiedScenes);
        Assert.Equal(0, plan.ModifiedScenes);
        Assert.Equal(2, plan.NewScenes);
        Assert.All(plan.Scenes, scene => Assert.Equal(SceneRenderStatus.New, scene.Status));
    }

    [Fact]
    public async Task GenerateRenderPlanAsync_NoCache_ReturnsAllNew()
    {
        // Arrange
        var renderer = new SmartRenderer(_mockLogger.Object, _tempCacheDir);
        var timeline = new EditableTimeline();
        timeline.AddScene(new TimelineScene(
            Index: 0,
            Heading: "Scene 1",
            Script: "Script",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5)
        ));

        // Act
        var plan = await renderer.GenerateRenderPlanAsync(timeline, "job123");

        // Assert
        Assert.Equal(1, plan.TotalScenes);
        Assert.Equal(1, plan.NewScenes);
        Assert.Equal(0, plan.UnmodifiedScenes);
        Assert.Equal(0, plan.ModifiedScenes);
    }

    [Fact]
    public void CalculateSceneChecksum_ChangesWithAssets()
    {
        // Arrange
        var renderer = new SmartRenderer(_mockLogger.Object, _tempCacheDir);
        var scene1 = new TimelineScene(
            Index: 0,
            Heading: "Test Scene",
            Script: "Script",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5),
            VisualAssets: new List<TimelineAsset>()
        );
        
        var asset = new TimelineAsset(
            Id: "asset1",
            Type: AssetType.Image,
            FilePath: "/path/to/image.jpg",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5),
            Position: new Position(0, 0, 100, 100)
        );
        
        var scene2 = scene1 with 
        { 
            VisualAssets = new List<TimelineAsset> { asset }
        };

        // Act
        var checksum1 = renderer.CalculateSceneChecksum(scene1);
        var checksum2 = renderer.CalculateSceneChecksum(scene2);

        // Assert
        Assert.NotEqual(checksum1, checksum2);
    }

    [Fact]
    public void CalculateSceneChecksum_ChangesWithTransition()
    {
        // Arrange
        var renderer = new SmartRenderer(_mockLogger.Object, _tempCacheDir);
        var scene1 = new TimelineScene(
            Index: 0,
            Heading: "Test Scene",
            Script: "Script",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5),
            TransitionType: "None"
        );
        var scene2 = scene1 with { TransitionType = "Fade" };

        // Act
        var checksum1 = renderer.CalculateSceneChecksum(scene1);
        var checksum2 = renderer.CalculateSceneChecksum(scene2);

        // Assert
        Assert.NotEqual(checksum1, checksum2);
    }

    [Fact]
    public void CalculateSceneChecksum_ReturnsConsistentFormat()
    {
        // Arrange
        var renderer = new SmartRenderer(_mockLogger.Object, _tempCacheDir);
        var scene = new TimelineScene(
            Index: 0,
            Heading: "Test",
            Script: "Test",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5)
        );

        // Act
        var checksum = renderer.CalculateSceneChecksum(scene);

        // Assert
        // MD5 checksum should be 32 hex characters
        Assert.Equal(32, checksum.Length);
        Assert.Matches("^[a-f0-9]{32}$", checksum);
    }

    public void Dispose()
    {
        // Cleanup temp directory
        if (Directory.Exists(_tempCacheDir))
        {
            try
            {
                Directory.Delete(_tempCacheDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
