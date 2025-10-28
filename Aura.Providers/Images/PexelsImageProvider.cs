using System;
using System.Collections.Generic;
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
/// Dedicated Pexels API client with enhanced error handling, rate limiting, and retry logic.
/// Free tier: 50 requests/hour.
/// </summary>
public class PexelsImageProvider : IStockProvider
{
    private readonly ILogger<PexelsImageProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private int _requestsRemaining = 50;
    private int _requestsLimit = 50;
    private DateTime _rateLimitReset = DateTime.UtcNow;

    public PexelsImageProvider(
        ILogger<PexelsImageProvider> logger,
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
            error = "Pexels API key not configured. Get a free key at https://www.pexels.com/api/";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Gets the current quota status.
    /// </summary>
    public (int remaining, int limit, DateTime? resetTime) GetQuotaStatus()
    {
        return (_requestsRemaining, _requestsLimit, _rateLimitReset);
    }

    public async Task<IReadOnlyList<Asset>> SearchAsync(string query, int count, CancellationToken ct)
    {
        if (!ValidateApiKey(out var error))
        {
            _logger.LogWarning("Pexels API key validation failed: {Error}", error);
            return Array.Empty<Asset>();
        }

        // Check rate limits
        if (_requestsRemaining <= 0 && DateTime.UtcNow < _rateLimitReset)
        {
            var waitTime = _rateLimitReset - DateTime.UtcNow;
            _logger.LogWarning("Pexels rate limit exceeded. Resets in {WaitTime}", waitTime);
            throw new InvalidOperationException($"Rate limit exceeded. Try again in {waitTime.TotalMinutes:F1} minutes.");
        }

        _logger.LogInformation("Searching Pexels for: {Query} (count: {Count})", query, count);

        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page={Math.Min(count, 80)}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue(_apiKey!);

                var response = await _httpClient.SendAsync(request, ct);

                // Update rate limit info from headers
                UpdateRateLimitInfo(response.Headers);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Pexels rate limit hit (429). Attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelay * attempt, ct);
                        continue;
                    }
                    throw new InvalidOperationException("Pexels rate limit exceeded. Please try again later.");
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var doc = JsonDocument.Parse(json);

                var assets = new List<Asset>();

                if (doc.RootElement.TryGetProperty("photos", out var photos))
                {
                    foreach (var photo in photos.EnumerateArray())
                    {
                        if (assets.Count >= count)
                            break;

                        string? photoUrl = null;
                        if (photo.TryGetProperty("src", out var src) &&
                            src.TryGetProperty("large2x", out var large2x))
                        {
                            photoUrl = large2x.GetString();
                        }

                        string? photographer = null;
                        if (photo.TryGetProperty("photographer", out var photoProp))
                        {
                            photographer = photoProp.GetString();
                        }

                        if (!string.IsNullOrEmpty(photoUrl))
                        {
                            assets.Add(new Asset(
                                Kind: "image",
                                PathOrUrl: photoUrl,
                                License: "Pexels License (Free to use)",
                                Attribution: photographer != null ? $"Photo by {photographer} on Pexels" : "Photo from Pexels"
                            ));
                        }
                    }
                }

                _logger.LogInformation("Found {Count} images on Pexels. Quota: {Remaining}/{Limit}", 
                    assets.Count, _requestsRemaining, _requestsLimit);
                return assets;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "HTTP error searching Pexels (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(retryDelay * attempt, ct);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout searching Pexels for {Query}", query);
                return Array.Empty<Asset>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Pexels for {Query}", query);
                return Array.Empty<Asset>();
            }
        }

        _logger.LogWarning("Failed to search Pexels after {MaxRetries} attempts", maxRetries);
        return Array.Empty<Asset>();
    }

    /// <summary>
    /// Downloads an image with retry logic.
    /// </summary>
    public async Task<byte[]> DownloadImageAsync(string imageUrl, CancellationToken ct)
    {
        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(imageUrl, ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(ct);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Failed to download image (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(retryDelay * attempt, ct);
            }
        }

        throw new InvalidOperationException($"Failed to download image after {maxRetries} attempts");
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

        if (headers.TryGetValues("X-Ratelimit-Reset", out var reset))
        {
            if (long.TryParse(reset.FirstOrDefault(), out var resetTimestamp))
            {
                _rateLimitReset = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).UtcDateTime;
            }
        }
    }
}
