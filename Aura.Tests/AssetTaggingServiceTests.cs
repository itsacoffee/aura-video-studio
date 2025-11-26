using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Aura.Core.Providers;
using Aura.Core.Services.Assets;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class AssetTaggingServiceTests
{
    private readonly Mock<ILogger<AssetTaggingService>> _loggerMock;
    private readonly Mock<ILogger<AssetTagger>> _taggerLoggerMock;
    private readonly AssetTagger _fallbackTagger;

    public AssetTaggingServiceTests()
    {
        _loggerMock = new Mock<ILogger<AssetTaggingService>>();
        _taggerLoggerMock = new Mock<ILogger<AssetTagger>>();
        _fallbackTagger = new AssetTagger(_taggerLoggerMock.Object);
    }

    [Fact]
    public async Task TagAssetAsync_WithFallbackTagger_ShouldGenerateTags()
    {
        // Arrange
        var service = new AssetTaggingService(_loggerMock.Object, null, _fallbackTagger);
        var asset = CreateTestAsset("sunset_beach.jpg", AssetType.Image);

        // Act
        var result = await service.TagAssetAsync(asset);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Metadata);
        Assert.NotEmpty(result.Metadata.Tags);
        Assert.Equal("Fallback", result.Metadata.TaggingProvider);
    }

    [Fact]
    public async Task TagAssetAsync_WithLlmProvider_ShouldUseLlmProvider()
    {
        // Arrange
        var providerMock = new Mock<IAssetTagProvider>();
        providerMock.Setup(p => p.Name).Returns("TestProvider");
        providerMock.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        providerMock.Setup(p => p.GenerateTagsAsync(
            It.IsAny<string>(),
            It.IsAny<AssetType>(),
            It.IsAny<AssetMetadata?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SemanticAssetMetadata
            {
                AssetId = Guid.NewGuid(),
                Tags = new List<AssetTag>
                {
                    new AssetTag("test", 0.9f, TagCategory.Subject),
                    new AssetTag("sample", 0.8f, TagCategory.Style)
                },
                Description = "Test description",
                Mood = "happy",
                Subject = "test subject",
                TaggedAt = DateTime.UtcNow,
                TaggingProvider = "TestProvider",
                ConfidenceScore = 0.85f
            });

        var service = new AssetTaggingService(_loggerMock.Object, providerMock.Object, _fallbackTagger);
        var asset = CreateTestAsset("test.jpg", AssetType.Image);

        // Act
        var result = await service.TagAssetAsync(asset);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Metadata);
        Assert.Equal("TestProvider", result.Metadata.TaggingProvider);
        Assert.Equal(2, result.Metadata.Tags.Count);
        Assert.Equal("happy", result.Metadata.Mood);
    }

    [Fact]
    public async Task TagAssetAsync_WhenProviderUnavailable_ShouldFallback()
    {
        // Arrange
        var providerMock = new Mock<IAssetTagProvider>();
        providerMock.Setup(p => p.Name).Returns("UnavailableProvider");
        providerMock.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new AssetTaggingService(_loggerMock.Object, providerMock.Object, _fallbackTagger);
        var asset = CreateTestAsset("beach_sunset.jpg", AssetType.Image);

        // Act
        var result = await service.TagAssetAsync(asset);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Metadata);
        Assert.Equal("Fallback", result.Metadata.TaggingProvider);
    }

    [Fact]
    public async Task TagAssetsAsync_ShouldTagMultipleAssets()
    {
        // Arrange
        var service = new AssetTaggingService(_loggerMock.Object, null, _fallbackTagger);
        var assets = new List<Asset>
        {
            CreateTestAsset("image1.jpg", AssetType.Image),
            CreateTestAsset("video1.mp4", AssetType.Video),
            CreateTestAsset("audio1.mp3", AssetType.Audio)
        };

        // Act
        var results = await service.TagAssetsAsync(assets);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
    }

    [Fact]
    public void UpdateTags_ShouldStoreNewTags()
    {
        // Arrange
        var service = new AssetTaggingService(_loggerMock.Object, null, _fallbackTagger);
        var assetId = Guid.NewGuid();
        var tags = new List<AssetTag>
        {
            new AssetTag("custom1", 0.95f, TagCategory.Custom),
            new AssetTag("custom2", 0.85f, TagCategory.Custom)
        };

        // Act
        service.UpdateTags(assetId, tags);
        var metadata = service.GetMetadata(assetId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(2, metadata.Tags.Count);
        Assert.Equal("Manual", metadata.TaggingProvider);
        Assert.Contains(metadata.Tags, t => t.Name == "custom1");
    }

    [Fact]
    public void SearchByTags_MatchAny_ShouldReturnMatchingAssets()
    {
        // Arrange
        var service = new AssetTaggingService(_loggerMock.Object, null, _fallbackTagger);
        
        var asset1Id = Guid.NewGuid();
        var asset2Id = Guid.NewGuid();
        
        service.UpdateTags(asset1Id, new List<AssetTag>
        {
            new AssetTag("nature", 0.9f, TagCategory.Subject),
            new AssetTag("outdoor", 0.8f, TagCategory.Setting)
        });
        
        service.UpdateTags(asset2Id, new List<AssetTag>
        {
            new AssetTag("city", 0.9f, TagCategory.Setting),
            new AssetTag("urban", 0.8f, TagCategory.Style)
        });

        // Act
        var results = service.SearchByTags(new List<string> { "nature" }, matchAll: false);

        // Assert
        Assert.Single(results);
        Assert.Contains(asset1Id, results);
    }

    [Fact]
    public void SearchByTags_MatchAll_ShouldReturnOnlyFullMatches()
    {
        // Arrange
        var service = new AssetTaggingService(_loggerMock.Object, null, _fallbackTagger);
        
        var asset1Id = Guid.NewGuid();
        var asset2Id = Guid.NewGuid();
        
        service.UpdateTags(asset1Id, new List<AssetTag>
        {
            new AssetTag("nature", 0.9f, TagCategory.Subject),
            new AssetTag("outdoor", 0.8f, TagCategory.Setting),
            new AssetTag("sunny", 0.7f, TagCategory.Mood)
        });
        
        service.UpdateTags(asset2Id, new List<AssetTag>
        {
            new AssetTag("nature", 0.9f, TagCategory.Subject),
            new AssetTag("indoor", 0.8f, TagCategory.Setting)
        });

        // Act
        var results = service.SearchByTags(new List<string> { "nature", "outdoor" }, matchAll: true);

        // Assert
        Assert.Single(results);
        Assert.Contains(asset1Id, results);
    }

    [Fact]
    public async Task SearchBySimilarityAsync_WithFallback_ShouldReturnKeywordMatches()
    {
        // Arrange
        var service = new AssetTaggingService(_loggerMock.Object, null, _fallbackTagger);
        
        var assetId = Guid.NewGuid();
        service.UpdateTags(assetId, new List<AssetTag>
        {
            new AssetTag("sunset", 0.9f, TagCategory.Subject),
            new AssetTag("beach", 0.8f, TagCategory.Setting),
            new AssetTag("calm", 0.7f, TagCategory.Mood)
        });

        // Act
        var results = await service.SearchBySimilarityAsync("beautiful sunset on the beach");

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.AssetId == assetId);
    }

    [Fact]
    public async Task MatchAssetsToSceneAsync_ShouldReturnRankedAssets()
    {
        // Arrange
        var service = new AssetTaggingService(_loggerMock.Object, null, _fallbackTagger);
        
        var beachAssetId = Guid.NewGuid();
        var cityAssetId = Guid.NewGuid();
        
        service.UpdateTags(beachAssetId, new List<AssetTag>
        {
            new AssetTag("beach", 0.9f, TagCategory.Setting),
            new AssetTag("ocean", 0.8f, TagCategory.Subject),
            new AssetTag("relaxing", 0.7f, TagCategory.Mood)
        });
        
        service.UpdateTags(cityAssetId, new List<AssetTag>
        {
            new AssetTag("city", 0.9f, TagCategory.Setting),
            new AssetTag("urban", 0.8f, TagCategory.Style),
            new AssetTag("busy", 0.7f, TagCategory.Mood)
        });

        // Act
        var results = await service.MatchAssetsToSceneAsync(
            "Beach vacation",
            "A relaxing day at the beach with ocean waves",
            AssetType.Image,
            5);

        // Assert
        Assert.NotEmpty(results);
        var topMatch = results.First();
        Assert.Equal(beachAssetId, topMatch.AssetId);
    }

    private static Asset CreateTestAsset(string filename, AssetType type)
    {
        return new Asset
        {
            Id = Guid.NewGuid(),
            Type = type,
            FilePath = $"/path/to/{filename}",
            Title = filename,
            Source = AssetSource.Uploaded,
            Metadata = new AssetMetadata
            {
                Width = 1920,
                Height = 1080,
                Duration = type == AssetType.Video ? TimeSpan.FromSeconds(30) : null
            },
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };
    }
}
