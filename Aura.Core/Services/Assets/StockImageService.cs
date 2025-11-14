using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Assets;

/// <summary>
/// Service for searching and downloading stock images from Pexels, Pixabay, and Unsplash
/// with enhanced error handling, rate limiting, and fallback mechanisms.
/// </summary>
public class StockImageService
{
    private readonly ILogger<StockImageService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _pexelsApiKey;
    private readonly string? _pixabayApiKey;
    private readonly string? _unsplashApiKey;
    private readonly Dictionary<string, List<StockImage>> _searchCache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    public StockImageService(
        ILogger<StockImageService> logger,
        HttpClient httpClient,
        string? pexelsApiKey = null,
        string? pixabayApiKey = null,
        string? unsplashApiKey = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _pexelsApiKey = pexelsApiKey;
        _pixabayApiKey = pixabayApiKey;
        _unsplashApiKey = unsplashApiKey;
    }

    /// <summary>
    /// Validates configured API keys.
    /// </summary>
    public Dictionary<string, (bool isValid, string? error)> ValidateApiKeys()
    {
        var results = new Dictionary<string, (bool isValid, string? error)>();

        if (!string.IsNullOrWhiteSpace(_pexelsApiKey))
        {
            results["Pexels"] = (true, null);
        }
        else
        {
            results["Pexels"] = (false, "API key not configured. Get a free key at https://www.pexels.com/api/");
        }

        if (!string.IsNullOrWhiteSpace(_pixabayApiKey))
        {
            results["Pixabay"] = (true, null);
        }
        else
        {
            results["Pixabay"] = (false, "API key not configured. Get a free key at https://pixabay.com/api/docs/");
        }

        if (!string.IsNullOrWhiteSpace(_unsplashApiKey))
        {
            results["Unsplash"] = (true, null);
        }
        else
        {
            results["Unsplash"] = (false, "API key not configured. Get a free key at https://unsplash.com/developers");
        }

        return results;
    }

    /// <summary>
    /// Search stock images from multiple providers with fallback.
    /// </summary>
    public async Task<List<StockImage>> SearchStockImagesAsync(string query, int count = 20, CancellationToken ct = default)
    {
        return await SearchWithFallbackAsync(query, count, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Search with automatic fallback when primary provider fails.
    /// Attempts parallel search across all providers with timeout.
    /// </summary>
    public async Task<List<StockImage>> SearchWithFallbackAsync(string query, int count = 20, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching stock images for: {Query}, Count: {Count}", query, count);

        // Check cache
        var cacheKey = $"{query}_{count}";
        if (_searchCache.TryGetValue(cacheKey, out var cachedResults))
        {
            _logger.LogInformation("Returning cached results for {Query}", query);
            return cachedResults;
        }

        // Validate API keys
        var validations = ValidateApiKeys();
        var availableProviders = validations.Where(v => v.Value.isValid).Select(v => v.Key).ToList();

        if (availableProviders.Count == 0)
        {
            _logger.LogWarning("No stock image API keys configured");
            throw new InvalidOperationException(
                "No stock image providers configured. Please configure at least one API key:\n" +
                string.Join("\n", validations.Where(v => !v.Value.isValid)
                    .Select(v => $"- {v.Key}: {v.Value.error}")));
        }

        _logger.LogInformation("Available providers: {Providers}", string.Join(", ", availableProviders));

        // Create search tasks for all available providers
        var searchTasks = new List<Task<List<StockImage>>>();
        var perProviderCount = count / Math.Max(availableProviders.Count, 1);

        if (availableProviders.Contains("Pexels"))
        {
            searchTasks.Add(SearchPexelsWithRetryAsync(query, perProviderCount, ct));
        }

        if (availableProviders.Contains("Pixabay"))
        {
            searchTasks.Add(SearchPixabayWithRetryAsync(query, perProviderCount, ct));
        }

        if (availableProviders.Contains("Unsplash"))
        {
            searchTasks.Add(SearchUnsplashWithRetryAsync(query, perProviderCount, ct));
        }

        // Execute searches in parallel with timeout
        var timeout = TimeSpan.FromSeconds(30);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        var results = new List<StockImage>();
        try
        {
            var completedResults = await Task.WhenAll(searchTasks).ConfigureAwait(false);
            foreach (var providerResults in completedResults)
            {
                results.AddRange(providerResults);
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            _logger.LogWarning("Stock image search timed out after {Timeout}", timeout);
            // Use partial results from completed tasks
            foreach (var task in searchTasks.Where(t => t.IsCompletedSuccessfully))
            {
                results.AddRange(task.Result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during parallel stock image search");
        }

        // If no results from any provider, throw detailed error
        if (results.Count == 0)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve stock images from all providers. Check your API keys and internet connection.");
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

        _logger.LogInformation("Found {Count} stock images for {Query} from {ProviderCount} providers", 
            results.Count, query, availableProviders.Count);
        return results;
    }

    private async Task<List<StockImage>> SearchPexelsWithRetryAsync(string query, int count, CancellationToken ct)
    {
        try
        {
            return await SearchPexelsAsync(query, count, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("rate limit"))
        {
            _logger.LogWarning("Pexels rate limit exceeded: {Message}", ex.Message);
            return new List<StockImage>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search Pexels, will try other providers");
            return new List<StockImage>();
        }
    }

    private async Task<List<StockImage>> SearchPixabayWithRetryAsync(string query, int count, CancellationToken ct)
    {
        try
        {
            return await SearchPixabayAsync(query, count, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("rate limit"))
        {
            _logger.LogWarning("Pixabay rate limit exceeded: {Message}", ex.Message);
            return new List<StockImage>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search Pixabay, will try other providers");
            return new List<StockImage>();
        }
    }

    private async Task<List<StockImage>> SearchUnsplashWithRetryAsync(string query, int count, CancellationToken ct)
    {
        try
        {
            return await SearchUnsplashAsync(query, count, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("rate limit"))
        {
            _logger.LogWarning("Unsplash rate limit exceeded: {Message}", ex.Message);
            return new List<StockImage>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search Unsplash, will try other providers");
            return new List<StockImage>();
        }
    }

    private async Task<List<StockImage>> SearchPexelsAsync(string query, int count, CancellationToken ct = default)
    {
        var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page={count}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", _pexelsApiKey);

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        
        // Handle rate limiting
        if ((int)response.StatusCode == 429)
        {
            throw new InvalidOperationException("Pexels rate limit exceeded (50 requests/hour). Please try again later.");
        }
        
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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

    private async Task<List<StockImage>> SearchPixabayAsync(string query, int count, CancellationToken ct = default)
    {
        var url = $"https://pixabay.com/api/?key={_pixabayApiKey}&q={Uri.EscapeDataString(query)}&per_page={count}&image_type=photo";
        
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        
        // Handle rate limiting
        if ((int)response.StatusCode == 429)
        {
            throw new InvalidOperationException("Pixabay rate limit exceeded. Please try again later.");
        }
        
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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

    private async Task<List<StockImage>> SearchUnsplashAsync(string query, int count, CancellationToken ct = default)
    {
        var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(query)}&per_page={Math.Min(count, 30)}";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Client-ID {_unsplashApiKey}");

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        
        // Handle rate limiting
        if ((int)response.StatusCode == 429)
        {
            throw new InvalidOperationException("Unsplash rate limit exceeded (50 requests/hour). Please try again later.");
        }
        
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);

        var results = new List<StockImage>();
        if (doc.RootElement.TryGetProperty("results", out var resultsArray))
        {
            foreach (var result in resultsArray.EnumerateArray())
            {
                string? imageUrl = null;
                if (result.TryGetProperty("urls", out var urls) &&
                    urls.TryGetProperty("regular", out var regular))
                {
                    imageUrl = regular.GetString();
                }

                string? photographer = null;
                if (result.TryGetProperty("user", out var user) &&
                    user.TryGetProperty("name", out var name))
                {
                    photographer = name.GetString();
                }

                int width = 0;
                if (result.TryGetProperty("width", out var widthProp))
                {
                    width = widthProp.GetInt32();
                }

                int height = 0;
                if (result.TryGetProperty("height", out var heightProp))
                {
                    height = heightProp.GetInt32();
                }

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    results.Add(new StockImage
                    {
                        ThumbnailUrl = imageUrl,
                        FullSizeUrl = imageUrl,
                        PreviewUrl = imageUrl,
                        Photographer = photographer,
                        PhotographerUrl = photographer != null ? $"https://unsplash.com/@{photographer.Replace(" ", "").ToLower()}" : null,
                        Source = "Unsplash",
                        Width = width,
                        Height = height
                    });
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Download a stock image and return the local file path with retry logic
    /// </summary>
    public async Task<string> DownloadStockImageAsync(string imageUrl, string destinationPath, CancellationToken ct = default)
    {
        _logger.LogInformation("Downloading stock image from {Url}", imageUrl);

        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(imageUrl, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                await File.WriteAllBytesAsync(destinationPath, content, ct).ConfigureAwait(false);

                _logger.LogInformation("Downloaded stock image to {Path}", destinationPath);
                return destinationPath;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Failed to download image (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(retryDelay * attempt, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading stock image from {Url}", imageUrl);
                throw;
            }
        }

        throw new InvalidOperationException($"Failed to download image after {maxRetries} attempts");
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
