using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.Orchestration;
using Aura.Core.Services.Media;
using Aura.Core.Services.Visual;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Services.Orchestration;

public class CompositionValidatorTests
{
    private readonly CompositionValidator _validator;

    public CompositionValidatorTests()
    {
        var logger = NullLogger<CompositionValidator>.Instance;
        _validator = new CompositionValidator(logger);
    }

    [Fact]
    public void ValidateComposition_WithEmptyScenes_ReturnsInvalid()
    {
        // Arrange
        var scenes = Array.Empty<Scene>();
        var sceneAssets = new Dictionary<int, IReadOnlyList<Aura.Core.Models.Asset>>();

        // Act
        var result = _validator.ValidateComposition(scenes, sceneAssets);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalErrors);
        Assert.Contains(result.Errors, e => e.Code == CompositionErrorCode.EMPTY_TIMELINE);
    }

    [Fact]
    public void ValidateComposition_WithValidScenes_ReturnsValid()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Script for scene 1", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Scene 2", "Script for scene 2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)),
            new Scene(2, "Scene 3", "Script for scene 3", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        };
        var sceneAssets = new Dictionary<int, IReadOnlyList<Aura.Core.Models.Asset>>();

        // Act
        var result = _validator.ValidateComposition(scenes, sceneAssets);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.HasCriticalErrors);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void ValidateComposition_WithGap_ReturnsInvalid()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Script for scene 1", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Scene 2", "Script for scene 2", TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(5)) // Gap of 1 second
        };
        var sceneAssets = new Dictionary<int, IReadOnlyList<Aura.Core.Models.Asset>>();

        // Act
        var result = _validator.ValidateComposition(scenes, sceneAssets);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Errors, e => e.Code == CompositionErrorCode.GAP_DETECTED);
    }

    [Fact]
    public void ValidateComposition_WithOverlap_ReturnsInvalid()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Script for scene 1", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Scene 2", "Script for scene 2", TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(5)) // Overlap of 1 second
        };
        var sceneAssets = new Dictionary<int, IReadOnlyList<Aura.Core.Models.Asset>>();

        // Act
        var result = _validator.ValidateComposition(scenes, sceneAssets);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Errors, e => e.Code == CompositionErrorCode.OVERLAP);
    }

    [Fact]
    public void ValidateComposition_WithInvalidDuration_ReturnsError()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Script for scene 1", TimeSpan.Zero, TimeSpan.Zero) // Invalid duration
        };
        var sceneAssets = new Dictionary<int, IReadOnlyList<Aura.Core.Models.Asset>>();

        // Act
        var result = _validator.ValidateComposition(scenes, sceneAssets);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Errors, e => e.Code == CompositionErrorCode.INVALID_DURATION);
    }

    [Fact]
    public void ValidateComposition_WithMissingNarration_ReturnsWarning()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Script for scene 1", TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };
        var sceneAssets = new Dictionary<int, IReadOnlyList<Aura.Core.Models.Asset>>();

        // Act
        var result = _validator.ValidateComposition(scenes, sceneAssets, narrationPath: null);

        // Assert
        Assert.True(result.HasWarnings);
        Assert.Contains(result.Errors, e => e.Code == CompositionErrorCode.MISSING_MEDIA && e.Message.Contains("narration"));
    }

    [Fact]
    public void ValidateComposition_WithVeryShortScene_ReturnsWarning()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Script for scene 1", TimeSpan.Zero, TimeSpan.FromSeconds(0.3)) // Very short
        };
        var sceneAssets = new Dictionary<int, IReadOnlyList<Aura.Core.Models.Asset>>();

        // Act
        var result = _validator.ValidateComposition(scenes, sceneAssets);

        // Assert
        Assert.True(result.HasWarnings);
        Assert.Contains(result.Errors, e => 
            e.Code == CompositionErrorCode.INVALID_DURATION && 
            e.Severity == ErrorSeverity.Warning);
    }
}

public class TimingResolverTests
{
    [Fact]
    public async Task ResolveSceneTimingsAsync_WithNoAudioFiles_UsesEstimation()
    {
        // Arrange
        var logger = NullLogger<TimingResolver>.Instance;
        var mediaService = new MockMediaMetadataService();
        var resolver = new TimingResolver(logger, mediaService);

        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "This is a test script with ten words in it.", TimeSpan.Zero, TimeSpan.Zero),
            new Scene(1, "Scene 2", "Another test.", TimeSpan.Zero, TimeSpan.Zero)
        };

        // Act
        var result = await resolver.ResolveSceneTimingsAsync(
            scenes,
            new Dictionary<int, string>(),
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.ResolvedScenes.Count);
        Assert.Equal(2, result.EstimatedDurationsUsed);
        Assert.Equal(0, result.AudioDurationsUsed);
        Assert.Equal(0.0, result.AccuracyPercentage);
    }

    [Fact]
    public async Task ResolveSceneTimingsAsync_WithAudioFiles_UsesActualDurations()
    {
        // Arrange
        var logger = NullLogger<TimingResolver>.Instance;
        var mediaService = new MockMediaMetadataService(5.0); // 5 second audio
        var resolver = new TimingResolver(logger, mediaService);

        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test script", TimeSpan.Zero, TimeSpan.Zero)
        };

        var audioFiles = new Dictionary<int, string>
        {
            [0] = "/fake/audio.wav"
        };

        // Act
        var result = await resolver.ResolveSceneTimingsAsync(
            scenes,
            audioFiles,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.ResolvedScenes);
        Assert.Equal(1, result.AudioDurationsUsed);
        Assert.Equal(0, result.EstimatedDurationsUsed);
        Assert.Equal(100.0, result.AccuracyPercentage);
        Assert.Equal(TimeSpan.FromSeconds(5), result.ResolvedScenes[0].Duration);
    }

    [Fact]
    public async Task ResolveFromConcatenatedAudioAsync_WithValidAudio_ResolvesTiming()
    {
        // Arrange
        var logger = NullLogger<TimingResolver>.Instance;
        var mediaService = new MockMediaMetadataService(15.0); // 15 second total audio
        var resolver = new TimingResolver(logger, mediaService);

        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Short script", TimeSpan.Zero, TimeSpan.Zero),
            new Scene(1, "Scene 2", "Longer script with more words", TimeSpan.Zero, TimeSpan.Zero),
            new Scene(2, "Scene 3", "Medium script here", TimeSpan.Zero, TimeSpan.Zero)
        };

        // Act
        var result = await resolver.ResolveFromConcatenatedAudioAsync(
            scenes,
            "/fake/concatenated.wav",
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.ResolvedScenes.Count);
        Assert.Equal(TimeSpan.FromSeconds(15), result.TotalDuration);
        Assert.Equal(100.0, result.AccuracyPercentage);
        
        // Verify scenes are proportionally distributed
        var totalDuration = result.ResolvedScenes.Sum(s => s.Duration.TotalSeconds);
        Assert.Equal(15.0, totalDuration, 0.1);
    }
}

public class VisualSelectorServiceTests
{
    [Fact]
    public void ExtractKeywords_WithValidText_ReturnsKeywords()
    {
        // Arrange
        var logger = NullLogger<VisualSelectorService>.Instance;
        var service = new VisualSelectorService(logger);

        var heading = "Beautiful Mountain Landscape";
        var script = "The mountains rise majestically against the sky. Nature's beauty is displayed in every peak and valley.";

        // Act
        var keywords = service.ExtractKeywords(heading, script);

        // Assert
        Assert.NotEmpty(keywords);
        Assert.Contains(keywords, k => k.Equals("beautiful", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(keywords, k => k.Equals("mountain", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(keywords, k => k.Equals("landscape", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExtractKeywords_FiltersStopWords()
    {
        // Arrange
        var logger = NullLogger<VisualSelectorService>.Instance;
        var service = new VisualSelectorService(logger);

        var heading = "The";
        var script = "and or but the";

        // Act
        var keywords = service.ExtractKeywords(heading, script);

        // Assert
        Assert.Empty(keywords); // All stop words should be filtered
    }

    [Fact]
    public void ExtractKeywords_LimitsToEightKeywords()
    {
        // Arrange
        var logger = NullLogger<VisualSelectorService>.Instance;
        var service = new VisualSelectorService(logger);

        var heading = "One Two Three Four Five";
        var script = "Six Seven Eight Nine Ten Eleven Twelve Thirteen Fourteen Fifteen";

        // Act
        var keywords = service.ExtractKeywords(heading, script);

        // Assert
        Assert.True(keywords.Count <= 8);
    }
}

// Mock implementation for testing
public class MockMediaMetadataService : IMediaMetadataService
{
    private readonly double _duration;

    public MockMediaMetadataService(double duration = 0)
    {
        _duration = duration;
    }

    public Task<Aura.Core.Models.Media.MediaMetadata?> ExtractMetadataAsync(
        string filePath,
        Aura.Core.Models.Media.MediaType mediaType,
        CancellationToken ct = default)
    {
        if (_duration > 0)
        {
            return Task.FromResult<Aura.Core.Models.Media.MediaMetadata?>(
                new Aura.Core.Models.Media.MediaMetadata
                {
                    Duration = _duration
                });
        }

        return Task.FromResult<Aura.Core.Models.Media.MediaMetadata?>(null);
    }

    public Task<Aura.Core.Models.Media.MediaMetadata?> ExtractMetadataFromStreamAsync(
        System.IO.Stream stream,
        Aura.Core.Models.Media.MediaType mediaType,
        string fileName,
        CancellationToken ct = default)
    {
        return Task.FromResult<Aura.Core.Models.Media.MediaMetadata?>(null);
    }
}
