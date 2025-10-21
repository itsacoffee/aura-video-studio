using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Aura.Core.Services.Assets;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class AssetLibraryServiceTests : IDisposable
{
    private readonly string _testLibraryPath;
    private readonly AssetLibraryService _service;
    private readonly ThumbnailGenerator _thumbnailGenerator;

    public AssetLibraryServiceTests()
    {
        _testLibraryPath = Path.Combine(Path.GetTempPath(), $"AssetLibraryTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testLibraryPath);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var thumbnailLogger = loggerFactory.CreateLogger<ThumbnailGenerator>();
        var serviceLogger = loggerFactory.CreateLogger<AssetLibraryService>();

        _thumbnailGenerator = new ThumbnailGenerator(thumbnailLogger);
        _service = new AssetLibraryService(serviceLogger, _testLibraryPath, _thumbnailGenerator);
    }

    [Fact]
    public async Task AddAssetAsync_ShouldAddAssetToLibrary()
    {
        // Arrange
        var testFilePath = Path.Combine(Path.GetTempPath(), "test_image.jpg");
        await File.WriteAllTextAsync(testFilePath, "test image content");

        // Act
        var asset = await _service.AddAssetAsync(testFilePath, Core.Models.Assets.AssetType.Image);

        // Assert
        Assert.NotNull(asset);
        Assert.NotEqual(Guid.Empty, asset.Id);
        Assert.Equal(Core.Models.Assets.AssetType.Image, asset.Type);
        Assert.Equal("test_image", asset.Title);
        Assert.Equal(AssetSource.Uploaded, asset.Source);

        // Cleanup
        File.Delete(testFilePath);
    }

    [Fact]
    public async Task SearchAssetsAsync_WithNoFilters_ShouldReturnAllAssets()
    {
        // Arrange
        var testFile1 = Path.Combine(Path.GetTempPath(), "image1.jpg");
        var testFile2 = Path.Combine(Path.GetTempPath(), "video1.mp4");
        await File.WriteAllTextAsync(testFile1, "image content");
        await File.WriteAllTextAsync(testFile2, "video content");

        await _service.AddAssetAsync(testFile1, Core.Models.Assets.AssetType.Image);
        await _service.AddAssetAsync(testFile2, Core.Models.Assets.AssetType.Video);

        // Act
        var result = await _service.SearchAssetsAsync();

        // Assert
        Assert.Equal(2, result.Assets.Count);
        Assert.Equal(2, result.TotalCount);

        // Cleanup
        File.Delete(testFile1);
        File.Delete(testFile2);
    }

    [Fact]
    public async Task SearchAssetsAsync_WithTypeFilter_ShouldReturnMatchingAssets()
    {
        // Arrange
        var testFile1 = Path.Combine(Path.GetTempPath(), "image1.jpg");
        var testFile2 = Path.Combine(Path.GetTempPath(), "video1.mp4");
        await File.WriteAllTextAsync(testFile1, "image content");
        await File.WriteAllTextAsync(testFile2, "video content");

        await _service.AddAssetAsync(testFile1, Core.Models.Assets.AssetType.Image);
        await _service.AddAssetAsync(testFile2, Core.Models.Assets.AssetType.Video);

        var filters = new AssetSearchFilters { Type = Core.Models.Assets.AssetType.Image };

        // Act
        var result = await _service.SearchAssetsAsync(filters: filters);

        // Assert
        Assert.Single(result.Assets);
        Assert.Equal(Core.Models.Assets.AssetType.Image, result.Assets[0].Type);

        // Cleanup
        File.Delete(testFile1);
        File.Delete(testFile2);
    }

    [Fact]
    public async Task TagAssetAsync_ShouldAddTagsToAsset()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), "test_image.jpg");
        await File.WriteAllTextAsync(testFile, "test content");

        var asset = await _service.AddAssetAsync(testFile, Core.Models.Assets.AssetType.Image);
        var tags = new[] { "landscape", "nature", "outdoor" }.ToList();

        // Act
        await _service.TagAssetAsync(asset.Id, tags);

        // Assert
        var updatedAsset = await _service.GetAssetAsync(asset.Id);
        Assert.NotNull(updatedAsset);
        Assert.True(updatedAsset.Tags.Count >= 3);
        Assert.Contains(updatedAsset.Tags, t => t.Name == "landscape");
        Assert.Contains(updatedAsset.Tags, t => t.Name == "nature");
        Assert.Contains(updatedAsset.Tags, t => t.Name == "outdoor");

        // Cleanup
        File.Delete(testFile);
    }

    [Fact]
    public async Task CreateCollectionAsync_ShouldCreateCollection()
    {
        // Act
        var collection = await _service.CreateCollectionAsync("Test Collection", "Test description");

        // Assert
        Assert.NotNull(collection);
        Assert.NotEqual(Guid.Empty, collection.Id);
        Assert.Equal("Test Collection", collection.Name);
        Assert.Equal("Test description", collection.Description);
    }

    [Fact]
    public async Task AddToCollectionAsync_ShouldAddAssetToCollection()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), "test_image.jpg");
        await File.WriteAllTextAsync(testFile, "test content");

        var asset = await _service.AddAssetAsync(testFile, Core.Models.Assets.AssetType.Image);
        var collection = await _service.CreateCollectionAsync("Test Collection");

        // Act
        await _service.AddToCollectionAsync(asset.Id, collection.Id);

        // Assert
        var collections = await _service.GetCollectionsAsync();
        var updatedCollection = collections.First(c => c.Id == collection.Id);
        Assert.Contains(asset.Id, updatedCollection.AssetIds);

        var updatedAsset = await _service.GetAssetAsync(asset.Id);
        Assert.Contains(collection.Name, updatedAsset!.Collections);

        // Cleanup
        File.Delete(testFile);
    }

    [Fact]
    public async Task DeleteAssetAsync_ShouldRemoveAssetFromLibrary()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), "test_image.jpg");
        await File.WriteAllTextAsync(testFile, "test content");

        var asset = await _service.AddAssetAsync(testFile, Core.Models.Assets.AssetType.Image);

        // Act
        var success = await _service.DeleteAssetAsync(asset.Id, deleteFromDisk: false);

        // Assert
        Assert.True(success);
        var deletedAsset = await _service.GetAssetAsync(asset.Id);
        Assert.Null(deletedAsset);

        // Cleanup
        File.Delete(testFile);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testLibraryPath))
            {
                Directory.Delete(_testLibraryPath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
