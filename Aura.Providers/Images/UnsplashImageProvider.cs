using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Dedicated Unsplash API client with enhanced error handling and download tracking.
/// Free tier: 50 requests/hour.
/// Important: Unsplash requires download tracking per their API guidelines.
/// </summary>
public class UnsplashImageProvider : IStockProvider
{
    private readonly ILogger<UnsplashImageProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private int _requestsRemaining = 50;
    private int _requestsLimit = 50;

    public UnsplashImageProvider(
        ILogger<UnsplashImageProvider> logger,
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
            error = "Unsplash API key not configured. Get a free key at https://unsplash.com/developers";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Gets the current quota status.
    /// </summary>
    public (int remaining, int limit) GetQuotaStatus()
    {
        return (_requestsRemaining, _requestsLimit);
    }

    public async Task<IReadOnlyList<Asset>> SearchAsync(string query, int count, CancellationToken ct)
    {
        if (!ValidateApiKey(out var error))
        {
            _logger.LogWarning("Unsplash API key validation failed: {Error}", error);
            return Array.Empty<Asset>();
        }

        // Check rate limits
        if (_requestsRemaining <= 0)
        {
            _logger.LogWarning("Unsplash rate limit exceeded");
            throw new InvalidOperationException("Rate limit exceeded. Please try again in an hour.");
        }

        _logger.LogInformation("Searching Unsplash for: {Query} (count: {Count})", query, count);

        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(query)}&per_page={Math.Min(count, 30)}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", _apiKey);

                var response = await _httpClient.SendAsync(request, ct);

                // Update rate limit info from headers
                UpdateRateLimitInfo(response.Headers);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Unsplash rate limit hit (429). Attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelay * attempt, ct);
                        continue;
                    }
                    throw new InvalidOperationException("Unsplash rate limit exceeded. Please try again later.");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException("Invalid Unsplash API key. Please check your configuration.");
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var doc = JsonDocument.Parse(json);

                var assets = new List<Asset>();

                if (doc.RootElement.TryGetProperty("results", out var results))
                {
                    foreach (var result in results.EnumerateArray())
                    {
                        if (assets.Count >= count)
                            break;

                        string? imageUrl = null;
                        string? downloadLocation = null;

                        if (result.TryGetProperty("urls", out var urls) &&
                            urls.TryGetProperty("regular", out var regular))
                        {
                            imageUrl = regular.GetString();
                        }

                        // Store download location for tracking (per Unsplash guidelines)
                        if (result.TryGetProperty("links", out var links) &&
                            links.TryGetProperty("download_location", out var downloadLoc))
                        {
                            downloadLocation = downloadLoc.GetString();
                        }

                        string? photographer = null;
                        if (result.TryGetProperty("user", out var user) &&
                            user.TryGetProperty("name", out var name))
                        {
                            photographer = name.GetString();
                        }

                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            var attribution = photographer != null 
                                ? $"Photo by {photographer} on Unsplash" 
                                : "Photo from Unsplash";

                            // Include download location in metadata if available
                            if (!string.IsNullOrEmpty(downloadLocation))
                            {
                                attribution += $" (tracking: {downloadLocation})";
                            }

                            assets.Add(new Asset(
                                Kind: "image",
                                PathOrUrl: imageUrl,
                                License: "Unsplash License (Free to use)",
                                Attribution: attribution
                            ));
                        }
                    }
                }

                _logger.LogInformation("Found {Count} images on Unsplash. Quota: {Remaining}/{Limit}", 
                    assets.Count, _requestsRemaining, _requestsLimit);
                return assets;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "HTTP error searching Unsplash (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(retryDelay * attempt, ct);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout searching Unsplash for {Query}", query);
                return Array.Empty<Asset>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Unsplash for {Query}", query);
                return Array.Empty<Asset>();
            }
        }

        _logger.LogWarning("Failed to search Unsplash after {MaxRetries} attempts", maxRetries);
        return Array.Empty<Asset>();
    }

    /// <summary>
    /// Tracks a download per Unsplash API guidelines.
    /// This should be called when a user downloads an image from Unsplash.
    /// </summary>
    public async Task TrackDownloadAsync(string downloadLocation, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(downloadLocation))
        {
            return;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, downloadLocation);
            request.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", _apiKey);

            var response = await _httpClient.SendAsync(request, ct);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully tracked Unsplash download");
            }
            else
            {
                _logger.LogWarning("Failed to track Unsplash download: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error tracking Unsplash download");
        }
    }

    private void UpdateRateLimitInfo(HttpResponseHeaders headers)
    {
        if (headers.TryGetValues("X-Ratelimit-Remaining", out var remaining))
        {
            if (int.TryParse(remaining.FirstOrDefault(), out var remainingValue))
            {
                _requestsRemaining = remainingValue;
            }
        }

        if (headers.TryGetValues("X-Ratelimit-Limit", out var limit))
        {
            if (int.TryParse(limit.FirstOrDefault(), out var limitValue))
            {
                _requestsLimit = limitValue;
            }
        }
    }
}
