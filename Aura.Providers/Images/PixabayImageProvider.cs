using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Dedicated Pixabay API client with enhanced error handling and video search capability.
/// Free tier: No explicit rate limit, but API key required.
/// </summary>
public class PixabayImageProvider : IStockProvider
{
    private readonly ILogger<PixabayImageProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public PixabayImageProvider(
        ILogger<PixabayImageProvider> logger,
        HttpClient httpClient,
        string? apiKey = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    /// <summary>
    /// Validates if the API key is configured and valid.
    /// </summary>
    public bool ValidateApiKey(out string? error)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            error = "Pixabay API key not configured. Get a free key at https://pixabay.com/api/docs/";
            return false;
        }

        error = null;
        return true;
    }

    public async Task<IReadOnlyList<Asset>> SearchAsync(string query, int count, CancellationToken ct)
    {
        if (!ValidateApiKey(out var error))
        {
            _logger.LogWarning("Pixabay API key validation failed: {Error}", error);
            return Array.Empty<Asset>();
        }

        _logger.LogInformation("Searching Pixabay for: {Query} (count: {Count})", query, count);

        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = $"https://pixabay.com/api/?key={_apiKey}&q={Uri.EscapeDataString(query)}&per_page={Math.Min(count, 200)}&image_type=photo";
                var response = await _httpClient.GetAsync(url, ct);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Pixabay rate limit hit (429). Attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelay * attempt, ct);
                        continue;
                    }
                    throw new InvalidOperationException("Pixabay rate limit exceeded. Please try again later.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Pixabay API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    
                    // Handle specific error codes
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new InvalidOperationException("Invalid Pixabay API request. Check your API key and query.");
                    }
                    
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                var doc = JsonDocument.Parse(json);

                var assets = new List<Asset>();

                if (doc.RootElement.TryGetProperty("hits", out var hits))
                {
                    foreach (var hit in hits.EnumerateArray())
                    {
                        if (assets.Count >= count)
                            break;

                        string? imageUrl = null;
                        if (hit.TryGetProperty("largeImageURL", out var largeImage))
                        {
                            imageUrl = largeImage.GetString();
                        }

                        string? photographer = null;
                        if (hit.TryGetProperty("user", out var userProp))
                        {
                            photographer = userProp.GetString();
                        }

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            assets.Add(new Asset(
                                Kind: "image",
                                PathOrUrl: imageUrl,
                                License: "Pixabay License (Free to use)",
                                Attribution: photographer != null ? $"Image by {photographer} from Pixabay" : "Image from Pixabay"
                            ));
                        }
                    }
                }

                _logger.LogInformation("Found {Count} images on Pixabay", assets.Count);
                return assets;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "HTTP error searching Pixabay (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(retryDelay * attempt, ct);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout searching Pixabay for {Query}", query);
                return Array.Empty<Asset>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Pixabay for {Query}", query);
                return Array.Empty<Asset>();
            }
        }

        _logger.LogWarning("Failed to search Pixabay after {MaxRetries} attempts", maxRetries);
        return Array.Empty<Asset>();
    }

    /// <summary>
    /// Searches for videos on Pixabay.
    /// </summary>
    public async Task<IReadOnlyList<Asset>> SearchVideosAsync(string query, int count, CancellationToken ct)
    {
        if (!ValidateApiKey(out var error))
        {
            _logger.LogWarning("Pixabay API key validation failed: {Error}", error);
            return Array.Empty<Asset>();
        }

        _logger.LogInformation("Searching Pixabay videos for: {Query} (count: {Count})", query, count);

        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = $"https://pixabay.com/api/videos/?key={_apiKey}&q={Uri.EscapeDataString(query)}&per_page={Math.Min(count, 200)}";
                var response = await _httpClient.GetAsync(url, ct);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Pixabay rate limit hit (429). Attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelay * attempt, ct);
                        continue;
                    }
                    throw new InvalidOperationException("Pixabay rate limit exceeded. Please try again later.");
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var doc = JsonDocument.Parse(json);

                var assets = new List<Asset>();

                if (doc.RootElement.TryGetProperty("hits", out var hits))
                {
                    foreach (var hit in hits.EnumerateArray())
                    {
                        if (assets.Count >= count)
                            break;

                        string? videoUrl = null;
                        if (hit.TryGetProperty("videos", out var videos))
                        {
                            // Get the highest quality video
                            if (videos.TryGetProperty("large", out var large) && 
                                large.TryGetProperty("url", out var largeUrl))
                            {
                                videoUrl = largeUrl.GetString();
                            }
                            else if (videos.TryGetProperty("medium", out var medium) && 
                                     medium.TryGetProperty("url", out var mediumUrl))
                            {
                                videoUrl = mediumUrl.GetString();
                            }
                        }

                        string? creator = null;
                        if (hit.TryGetProperty("user", out var userProp))
                        {
                            creator = userProp.GetString();
                        }

                        if (!string.IsNullOrEmpty(videoUrl))
                        {
                            assets.Add(new Asset(
                                Kind: "video",
                                PathOrUrl: videoUrl,
                                License: "Pixabay License (Free to use)",
                                Attribution: creator != null ? $"Video by {creator} from Pixabay" : "Video from Pixabay"
                            ));
                        }
                    }
                }

                _logger.LogInformation("Found {Count} videos on Pixabay", assets.Count);
                return assets;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "HTTP error searching Pixabay videos (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(retryDelay * attempt, ct);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout searching Pixabay videos for {Query}", query);
                return Array.Empty<Asset>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Pixabay videos for {Query}", query);
                return Array.Empty<Asset>();
            }
        }

        _logger.LogWarning("Failed to search Pixabay videos after {MaxRetries} attempts", maxRetries);
        return Array.Empty<Asset>();
    }
}
