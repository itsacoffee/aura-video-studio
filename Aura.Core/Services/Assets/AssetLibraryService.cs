using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Assets;

/// <summary>
/// Service for managing asset library with JSON file storage
/// </summary>
public class AssetLibraryService
{
    private readonly ILogger<AssetLibraryService> _logger;
    private readonly string _libraryPath;
    private readonly string _assetsDirectory;
    private readonly string _thumbnailsDirectory;
    private readonly ThumbnailGenerator _thumbnailGenerator;

    private readonly Dictionary<Guid, Asset> _assets = new();
    private readonly Dictionary<Guid, AssetCollection> _collections = new();

    public AssetLibraryService(
        ILogger<AssetLibraryService> logger,
        string libraryPath,
        ThumbnailGenerator thumbnailGenerator)
    {
        _logger = logger;
        _libraryPath = libraryPath;
        _assetsDirectory = Path.Combine(libraryPath, "assets");
        _thumbnailsDirectory = Path.Combine(libraryPath, "thumbnails");
        _thumbnailGenerator = thumbnailGenerator;

        // Ensure directories exist
        Directory.CreateDirectory(_libraryPath);
        Directory.CreateDirectory(_assetsDirectory);
        Directory.CreateDirectory(_thumbnailsDirectory);

        // Load library from disk
        LoadLibraryAsync().Wait();
    }

    /// <summary>
    /// Add an asset to the library
    /// </summary>
    public async Task<Asset> AddAssetAsync(string filePathOrUrl, AssetType type, AssetSource source = AssetSource.Uploaded)
    {
        _logger.LogInformation("Adding asset: {Path}, Type: {Type}", filePathOrUrl, type);

        var assetId = Guid.NewGuid();
        var fileName = Path.GetFileName(filePathOrUrl);
        var managedPath = filePathOrUrl;

        // Copy file to managed directory if local file
        if (!filePathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var extension = Path.GetExtension(filePathOrUrl);
            var managedFileName = $"{assetId}{extension}";
            managedPath = Path.Combine(_assetsDirectory, managedFileName);
            
            if (File.Exists(filePathOrUrl))
            {
                File.Copy(filePathOrUrl, managedPath, overwrite: true);
            }
        }

        // Generate thumbnail
        var thumbnailPath = await _thumbnailGenerator.GenerateThumbnailAsync(
            managedPath, 
            assetId, 
            type, 
            _thumbnailsDirectory).ConfigureAwait(false);

        // Extract metadata
        var metadata = await ExtractMetadataAsync(managedPath, type).ConfigureAwait(false);

        var asset = new Asset
        {
            Id = assetId,
            Type = type,
            FilePath = managedPath,
            ThumbnailPath = thumbnailPath,
            Title = Path.GetFileNameWithoutExtension(fileName),
            Source = source,
            Metadata = metadata,
            DateAdded = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
            UsageCount = 0
        };

        _assets[assetId] = asset;
        await SaveLibraryAsync().ConfigureAwait(false);

        _logger.LogInformation("Asset added successfully: {AssetId}", assetId);
        return asset;
    }

    /// <summary>
    /// Get asset by ID
    /// </summary>
    public Task<Asset?> GetAssetAsync(Guid assetId)
    {
        _assets.TryGetValue(assetId, out var asset);
        return Task.FromResult(asset);
    }

    /// <summary>
    /// Search assets with filters
    /// </summary>
    public Task<AssetSearchResult> SearchAssetsAsync(
        string? query = null,
        AssetSearchFilters? filters = null,
        int page = 1,
        int pageSize = 50,
        string sortBy = "dateAdded",
        bool sortDescending = true)
    {
        _logger.LogInformation("Searching assets: Query={Query}, Page={Page}", query, page);

        var assets = _assets.Values.AsQueryable();

        // Apply filters
        if (filters != null)
        {
            if (filters.Type.HasValue)
                assets = assets.Where(a => a.Type == filters.Type.Value);

            if (filters.Tags.Count != 0)
                assets = assets.Where(a => a.Tags.Any(t => filters.Tags.Contains(t.Name)));

            if (filters.StartDate.HasValue)
                assets = assets.Where(a => a.DateAdded >= filters.StartDate.Value);

            if (filters.EndDate.HasValue)
                assets = assets.Where(a => a.DateAdded <= filters.EndDate.Value);

            if (filters.Source.HasValue)
                assets = assets.Where(a => a.Source == filters.Source.Value);

            if (filters.Collections.Count != 0)
                assets = assets.Where(a => a.Collections.Any(c => filters.Collections.Contains(c)));

            if (filters.UsedInTimeline.HasValue)
                assets = assets.Where(a => filters.UsedInTimeline.Value ? a.UsageCount > 0 : a.UsageCount == 0);

            // Resolution filters
            if (filters.MinWidth.HasValue)
                assets = assets.Where(a => a.Metadata.Width >= filters.MinWidth.Value);
            if (filters.MaxWidth.HasValue)
                assets = assets.Where(a => a.Metadata.Width <= filters.MaxWidth.Value);
            if (filters.MinHeight.HasValue)
                assets = assets.Where(a => a.Metadata.Height >= filters.MinHeight.Value);
            if (filters.MaxHeight.HasValue)
                assets = assets.Where(a => a.Metadata.Height <= filters.MaxHeight.Value);

            // Duration filters
            if (filters.MinDuration.HasValue)
                assets = assets.Where(a => a.Metadata.Duration >= filters.MinDuration.Value);
            if (filters.MaxDuration.HasValue)
                assets = assets.Where(a => a.Metadata.Duration <= filters.MaxDuration.Value);
        }

        // Apply query search
        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLowerInvariant();
            assets = assets.Where(a =>
                a.Title.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                (a.Description != null && a.Description.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase)) ||
                a.Tags.Any(t => t.Name.Contains(lowerQuery)));
        }

        // Apply sorting
        assets = sortBy.ToLowerInvariant() switch
        {
            "title" => sortDescending ? assets.OrderByDescending(a => a.Title) : assets.OrderBy(a => a.Title),
            "datemodified" => sortDescending ? assets.OrderByDescending(a => a.DateModified) : assets.OrderBy(a => a.DateModified),
            "usagecount" => sortDescending ? assets.OrderByDescending(a => a.UsageCount) : assets.OrderBy(a => a.UsageCount),
            _ => sortDescending ? assets.OrderByDescending(a => a.DateAdded) : assets.OrderBy(a => a.DateAdded),
        };

        var totalCount = assets.Count();
        var pagedAssets = assets
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new AssetSearchResult
        {
            Assets = pagedAssets,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Tag an asset
    /// </summary>
    public async Task TagAssetAsync(Guid assetId, List<string> tags)
    {
        if (!_assets.TryGetValue(assetId, out var asset))
        {
            throw new ArgumentException($"Asset {assetId} not found");
        }

        var existingTags = asset.Tags.Select(t => t.Name).ToHashSet();
        var newTags = tags
            .Where(t => !existingTags.Contains(t.ToLowerInvariant()))
            .Select(t => new AssetTag(t, 100))
            .ToList();

        if (newTags.Count != 0)
        {
            var updatedTags = asset.Tags.Concat(newTags).ToList();
            _assets[assetId] = asset with 
            { 
                Tags = updatedTags,
                DateModified = DateTime.UtcNow
            };
            await SaveLibraryAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Create a collection
    /// </summary>
    public async Task<AssetCollection> CreateCollectionAsync(string name, string? description = null, string color = "#0078D4")
    {
        var collection = new AssetCollection
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Color = color,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };

        _collections[collection.Id] = collection;
        await SaveLibraryAsync().ConfigureAwait(false);
        return collection;
    }

    /// <summary>
    /// Add asset to collection
    /// </summary>
    public async Task AddToCollectionAsync(Guid assetId, Guid collectionId)
    {
        if (!_assets.TryGetValue(assetId, out var asset))
            throw new ArgumentException($"Asset {assetId} not found");

        if (!_collections.TryGetValue(collectionId, out var collection))
            throw new ArgumentException($"Collection {collectionId} not found");

        // Update collection
        if (!collection.AssetIds.Contains(assetId))
        {
            _collections[collectionId] = collection with
            {
                AssetIds = collection.AssetIds.Concat(new[] { assetId }).ToList(),
                DateModified = DateTime.UtcNow
            };
        }

        if (!asset.Collections.Contains(collection.Name))
        {
            _assets[assetId] = asset with
            {
                Collections = asset.Collections.Concat(new[] { collection.Name }).ToList(),
                DateModified = DateTime.UtcNow
            };
        }

        await SaveLibraryAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Get all collections
    /// </summary>
    public Task<List<AssetCollection>> GetCollectionsAsync()
    {
        return Task.FromResult(_collections.Values.ToList());
    }

    /// <summary>
    /// Delete an asset
    /// </summary>
    public async Task<bool> DeleteAssetAsync(Guid assetId, bool deleteFromDisk = false)
    {
        if (!_assets.TryGetValue(assetId, out var asset))
            return false;

        // Remove from collections
        foreach (var collection in _collections.Values.Where(c => c.AssetIds.Contains(assetId)))
        {
            _collections[collection.Id] = collection with
            {
                AssetIds = collection.AssetIds.Where(id => id != assetId).ToList(),
                DateModified = DateTime.UtcNow
            };
        }

        // Remove from library
        _assets.Remove(assetId);

        // Delete files if requested
        if (deleteFromDisk)
        {
            try
            {
                if (File.Exists(asset.FilePath))
                    File.Delete(asset.FilePath);
                if (asset.ThumbnailPath != null && File.Exists(asset.ThumbnailPath))
                    File.Delete(asset.ThumbnailPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete asset files for {AssetId}", assetId);
            }
        }

        await SaveLibraryAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Update asset usage count
    /// </summary>
    public async Task IncrementUsageAsync(Guid assetId)
    {
        if (_assets.TryGetValue(assetId, out var asset))
        {
            _assets[assetId] = asset with
            {
                UsageCount = asset.UsageCount + 1,
                DateModified = DateTime.UtcNow
            };
            await SaveLibraryAsync().ConfigureAwait(false);
        }
    }

    private async Task<AssetMetadata> ExtractMetadataAsync(string filePath, AssetType type)
    {
        var metadata = new AssetMetadata();

        if (!File.Exists(filePath))
            return metadata;

        var fileInfo = new FileInfo(filePath);
        metadata = metadata with { FileSizeBytes = fileInfo.Length };

        // For now, return basic metadata
        // In a full implementation, this would use FFmpeg or image libraries to extract detailed metadata
        return await Task.FromResult(metadata).ConfigureAwait(false);
    }

    private async Task LoadLibraryAsync()
    {
        var assetsFile = Path.Combine(_libraryPath, "assets.json");
        var collectionsFile = Path.Combine(_libraryPath, "collections.json");

        if (File.Exists(assetsFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(assetsFile).ConfigureAwait(false);
                var assets = JsonSerializer.Deserialize<List<Asset>>(json) ?? new List<Asset>();
                foreach (var asset in assets)
                {
                    _assets[asset.Id] = asset;
                }
                _logger.LogInformation("Loaded {Count} assets from library", assets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load assets from library");
            }
        }

        if (File.Exists(collectionsFile))
        {
            try
            {
                var json = await File.ReadAllTextAsync(collectionsFile).ConfigureAwait(false);
                var collections = JsonSerializer.Deserialize<List<AssetCollection>>(json) ?? new List<AssetCollection>();
                foreach (var collection in collections)
                {
                    _collections[collection.Id] = collection;
                }
                _logger.LogInformation("Loaded {Count} collections from library", collections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load collections from library");
            }
        }
    }

    private async Task SaveLibraryAsync()
    {
        var assetsFile = Path.Combine(_libraryPath, "assets.json");
        var collectionsFile = Path.Combine(_libraryPath, "collections.json");

        try
        {
            var assetsJson = JsonSerializer.Serialize(_assets.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(assetsFile, assetsJson).ConfigureAwait(false);

            var collectionsJson = JsonSerializer.Serialize(_collections.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(collectionsFile, collectionsJson).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save library");
        }
    }
}
