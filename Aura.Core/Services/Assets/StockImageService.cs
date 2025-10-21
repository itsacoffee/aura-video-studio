using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Assets;

/// <summary>
/// Service for searching and downloading stock images from Pexels and Pixabay
/// </summary>
public class StockImageService
{
    private readonly ILogger<StockImageService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _pexelsApiKey;
    private readonly string? _pixabayApiKey;
    private readonly Dictionary<string, List<StockImage>> _searchCache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    public StockImageService(
        ILogger<StockImageService> logger,
        HttpClient httpClient,
        string? pexelsApiKey = null,
        string? pixabayApiKey = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _pexelsApiKey = pexelsApiKey;
        _pixabayApiKey = pixabayApiKey;
    }

    /// <summary>
    /// Search stock images from multiple providers
    /// </summary>
    public async Task<List<StockImage>> SearchStockImagesAsync(string query, int count = 20)
    {
        _logger.LogInformation("Searching stock images for: {Query}, Count: {Count}", query, count);

        // Check cache
        var cacheKey = $"{query}_{count}";
        if (_searchCache.TryGetValue(cacheKey, out var cachedResults))
        {
            _logger.LogInformation("Returning cached results for {Query}", query);
            return cachedResults;
        }

        var results = new List<StockImage>();

        // Search Pexels
        if (!string.IsNullOrWhiteSpace(_pexelsApiKey))
        {
            try
            {
                var pexelsResults = await SearchPexelsAsync(query, count / 2);
                results.AddRange(pexelsResults);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search Pexels");
            }
        }

        // Search Pixabay
        if (!string.IsNullOrWhiteSpace(_pixabayApiKey))
        {
            try
            {
                var pixabayResults = await SearchPixabayAsync(query, count / 2);
                results.AddRange(pixabayResults);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search Pixabay");
            }
        }

        // If no API keys configured, return empty with message
        if (string.IsNullOrWhiteSpace(_pexelsApiKey) && string.IsNullOrWhiteSpace(_pixabayApiKey))
        {
            _logger.LogWarning("No stock image API keys configured");
        }

        // Remove duplicates and rank by quality
        results = results
            .GroupBy(r => r.FullSizeUrl)
            .Select(g => g.First())
            .OrderByDescending(r => r.Width * r.Height)
            .Take(count)
            .ToList();

        // Cache results
        _searchCache[cacheKey] = results;

        _logger.LogInformation("Found {Count} stock images for {Query}", results.Count, query);
        return results;
    }

    private async Task<List<StockImage>> SearchPexelsAsync(string query, int count)
    {
        var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page={count}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", _pexelsApiKey);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PexelsResponse>(json);

        return result?.Photos?.Select(p => new StockImage
        {
            ThumbnailUrl = p.Src?.Small ?? string.Empty,
            FullSizeUrl = p.Src?.Original ?? string.Empty,
            PreviewUrl = p.Src?.Medium ?? string.Empty,
            Photographer = p.Photographer,
            PhotographerUrl = p.PhotographerUrl,
            Source = "Pexels",
            Width = p.Width,
            Height = p.Height
        }).ToList() ?? new List<StockImage>();
    }

    private async Task<List<StockImage>> SearchPixabayAsync(string query, int count)
    {
        var url = $"https://pixabay.com/api/?key={_pixabayApiKey}&q={Uri.EscapeDataString(query)}&per_page={count}&image_type=photo";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PixabayResponse>(json);

        return result?.Hits?.Select(h => new StockImage
        {
            ThumbnailUrl = h.PreviewUrl ?? string.Empty,
            FullSizeUrl = h.LargeImageUrl ?? h.WebformatUrl ?? string.Empty,
            PreviewUrl = h.WebformatUrl ?? string.Empty,
            Photographer = h.User,
            PhotographerUrl = $"https://pixabay.com/users/{h.User}-{h.UserId}/",
            Source = "Pixabay",
            Width = h.ImageWidth,
            Height = h.ImageHeight
        }).ToList() ?? new List<StockImage>();
    }

    /// <summary>
    /// Download a stock image and return the local file path
    /// </summary>
    public async Task<string> DownloadStockImageAsync(string imageUrl, string destinationPath)
    {
        _logger.LogInformation("Downloading stock image from {Url}", imageUrl);

        var response = await _httpClient.GetAsync(imageUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsByteArrayAsync();
        await System.IO.File.WriteAllBytesAsync(destinationPath, content);

        _logger.LogInformation("Downloaded stock image to {Path}", destinationPath);
        return destinationPath;
    }
}

// DTOs for Pexels API
internal class PexelsResponse
{
    public List<PexelsPhoto>? Photos { get; set; }
}

internal class PexelsPhoto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Photographer { get; set; }
    public string? PhotographerUrl { get; set; }
    public PexelsSrc? Src { get; set; }
}

internal class PexelsSrc
{
    public string? Original { get; set; }
    public string? Large { get; set; }
    public string? Medium { get; set; }
    public string? Small { get; set; }
}

// DTOs for Pixabay API
internal class PixabayResponse
{
    public List<PixabayHit>? Hits { get; set; }
}

internal class PixabayHit
{
    public string? PreviewUrl { get; set; }
    public string? WebformatUrl { get; set; }
    public string? LargeImageUrl { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public string? User { get; set; }
    public int UserId { get; set; }
}
