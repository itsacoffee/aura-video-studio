using System;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Aura.Core.Services.Assets;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class AssetTaggerTests
{
    private readonly AssetTagger _tagger;

    public AssetTaggerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<AssetTagger>();
        _tagger = new AssetTagger(logger);
    }

    [Fact]
    public async Task GenerateTagsAsync_ForImage_ShouldGenerateImageTags()
    {
        // Arrange
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Type = Core.Models.Assets.AssetType.Image,
            FilePath = "/path/to/sunset_beach_landscape.jpg",
            Title = "Sunset Beach Landscape",
            Source = AssetSource.Uploaded,
            Metadata = new AssetMetadata
            {
                Width = 1920,
                Height = 1080
            },
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };

        // Act
        var tags = await _tagger.GenerateTagsAsync(asset);

        // Assert
        Assert.NotEmpty(tags);
        Assert.Contains(tags, t => t.Name == "image");
        Assert.Contains(tags, t => t.Name == "hd");
        Assert.Contains(tags, t => t.Name == "landscape");
        // Should extract keywords from filename
        Assert.Contains(tags, t => t.Name == "sunset" || t.Name == "beach");
    }

    [Fact]
    public async Task GenerateTagsAsync_ForVideo_ShouldGenerateVideoTags()
    {
        // Arrange
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Type = Core.Models.Assets.AssetType.Video,
            FilePath = "/path/to/city_timelapse.mp4",
            Title = "City Timelapse",
            Source = AssetSource.Uploaded,
            Metadata = new AssetMetadata
            {
                Width = 3840,
                Height = 2160,
                Duration = TimeSpan.FromSeconds(30)
            },
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };

        // Act
        var tags = await _tagger.GenerateTagsAsync(asset);

        // Assert
        Assert.NotEmpty(tags);
        Assert.Contains(tags, t => t.Name == "video");
        Assert.Contains(tags, t => t.Name == "4k");
        // 30 seconds is considered medium duration
        Assert.Contains(tags, t => t.Name == "medium");
    }

    [Fact]
    public async Task GenerateTagsAsync_ForAudio_ShouldGenerateAudioTags()
    {
        // Arrange
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Type = Core.Models.Assets.AssetType.Audio,
            FilePath = "/path/to/upbeat_corporate_music.mp3",
            Title = "Upbeat Corporate Music",
            Source = AssetSource.Uploaded,
            Metadata = new AssetMetadata
            {
                Duration = TimeSpan.FromMinutes(3)
            },
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };

        // Act
        var tags = await _tagger.GenerateTagsAsync(asset);

        // Assert
        Assert.NotEmpty(tags);
        Assert.Contains(tags, t => t.Name == "audio");
        // 3 minutes is considered long duration (> 180 seconds)
        Assert.Contains(tags, t => t.Name == "long");
        // Should detect mood keywords from filename
        Assert.Contains(tags, t => t.Name == "upbeat" || t.Name == "corporate");
    }

    [Fact]
    public async Task GenerateTagsAsync_ShouldIncludeConfidenceScores()
    {
        // Arrange
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Type = Core.Models.Assets.AssetType.Image,
            FilePath = "/path/to/test.jpg",
            Title = "Test Image",
            Source = AssetSource.Uploaded,
            Metadata = new AssetMetadata(),
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };

        // Act
        var tags = await _tagger.GenerateTagsAsync(asset);

        // Assert
        Assert.All(tags, tag =>
        {
            Assert.True(tag.Confidence >= 0 && tag.Confidence <= 1.0f,
                $"Tag {tag.Name} has invalid confidence score: {tag.Confidence}");
        });
    }
}
