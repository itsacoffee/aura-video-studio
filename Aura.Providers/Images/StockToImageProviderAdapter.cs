using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Adapts an IStockProvider to implement IImageProvider interface.
/// This allows stock image providers (Pexels, Unsplash, Pixabay) to be used
/// wherever IImageProvider is required, such as in the VisualsStage.
/// </summary>
public class StockToImageProviderAdapter : IImageProvider
{
    private readonly ILogger<StockToImageProviderAdapter> _logger;
    private readonly IStockProvider _stockProvider;
    private readonly HttpClient _httpClient;
    private readonly string _tempDirectory;

    public StockToImageProviderAdapter(
        ILogger<StockToImageProviderAdapter> logger,
        IStockProvider stockProvider,
        HttpClient httpClient,
        string? tempDirectory = null)
    {
        _logger = logger;
        _stockProvider = stockProvider;
        _httpClient = httpClient;
        _tempDirectory = tempDirectory ?? Path.Combine(Path.GetTempPath(), "Aura", "stock-images");

        // Ensure temp directory exists
        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    public async Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct)
    {
        var query = ExtractSearchQuery(scene, spec);
        
        _logger.LogInformation(
            "StockToImageProviderAdapter: Fetching stock image for scene {SceneIndex} with query: {Query}",
            scene.Index, query);

        try
        {
            // Search for images using the stock provider
            var searchResults = await _stockProvider.SearchAsync(query, 3, ct).ConfigureAwait(false);

            if (searchResults == null || searchResults.Count == 0)
            {
                _logger.LogWarning(
                    "No stock images found for scene {SceneIndex} with query: {Query}",
                    scene.Index, query);
                return Array.Empty<Asset>();
            }

            // Get the best matching result
            var bestResult = searchResults[0];

            // If the result is a URL, download it to a local temp file
            if (bestResult.PathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                bestResult.PathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var downloadedPath = await DownloadImageAsync(bestResult.PathOrUrl, scene.Index, ct).ConfigureAwait(false);
                
                if (string.IsNullOrEmpty(downloadedPath))
                {
                    _logger.LogWarning(
                        "Failed to download stock image for scene {SceneIndex} from: {Url}",
                        scene.Index, bestResult.PathOrUrl);
                    return Array.Empty<Asset>();
                }

                _logger.LogInformation(
                    "Downloaded stock image for scene {SceneIndex} to: {Path}",
                    scene.Index, downloadedPath);

                return new[]
                {
                    new Asset(
                        Kind: "image",
                        PathOrUrl: downloadedPath,
                        License: bestResult.License,
                        Attribution: bestResult.Attribution
                    )
                };
            }

            // Return as-is if it's already a local path
            return new[] { bestResult };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching stock image for scene {SceneIndex} with query: {Query}",
                scene.Index, query);
            return Array.Empty<Asset>();
        }
    }

    /// <summary>
    /// Extracts a search query from the scene content and visual spec.
    /// </summary>
    private static string ExtractSearchQuery(Scene scene, VisualSpec spec)
    {
        // Combine heading and keywords from visual spec
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(scene.Heading))
        {
            queryParts.Add(scene.Heading);
        }

        if (spec.Keywords != null && spec.Keywords.Length > 0)
        {
            queryParts.AddRange(spec.Keywords);
        }

        var query = string.Join(" ", queryParts);

        // Clean up the query - remove style prefixes that don't help with stock searches
        var stylePatterns = new[] { "style:", "cinematic", "modern", "minimal", "playful", "professional" };
        foreach (var pattern in stylePatterns)
        {
            query = query.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
        }

        // Limit query length for better search results
        if (query.Length > 100)
        {
            query = query[..100];
        }

        return query.Trim();
    }

    // Supported image file extensions for validation
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    /// <summary>
    /// Downloads an image from a URL to a local temp file.
    /// </summary>
    private async Task<string?> DownloadImageAsync(string url, int sceneIndex, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var extension = GetFileExtension(url, response.Content.Headers.ContentType?.MediaType);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var shortGuid = Guid.NewGuid().ToString("N")[..8];
            var fileName = $"stock_scene{sceneIndex:D3}_{timestamp}_{shortGuid}{extension}";
            var filePath = Path.Combine(_tempDirectory, fileName);

            var imageBytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            await File.WriteAllBytesAsync(filePath, imageBytes, ct).ConfigureAwait(false);

            _logger.LogDebug("Downloaded {Bytes} bytes to {Path}", imageBytes.Length, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download image from {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Determines file extension from URL or content type.
    /// </summary>
    private static string GetFileExtension(string url, string? contentType)
    {
        // Try to get extension from URL
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var ext = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext) && SupportedImageExtensions.Contains(ext))
            {
                return ext;
            }
        }
        catch
        {
            // Ignore URI parsing errors
        }

        // Fall back to content type
        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };
    }
}
