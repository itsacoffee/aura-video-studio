using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Aura.Core.Models.StockMedia;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Enhanced Unsplash provider with comprehensive licensing
/// API Documentation: https://unsplash.com/documentation
/// Rate Limit: 50 requests per hour for free tier
/// Important: Unsplash requires download tracking per their API guidelines
/// </summary>
public class EnhancedUnsplashProvider : IEnhancedStockProvider
{
    private readonly ILogger<EnhancedUnsplashProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private int _requestsRemaining = 50;
    private int _requestsLimit = 50;

    public EnhancedUnsplashProvider(
        ILogger<EnhancedUnsplashProvider> logger,
        HttpClient httpClient,
        string? apiKey = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public StockMediaProvider ProviderName => StockMediaProvider.Unsplash;

    public bool SupportsVideo => false;

    public async Task<List<StockMediaResult>> SearchAsync(
        StockMediaSearchRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Unsplash API key not configured");
            return new List<StockMediaResult>();
        }

        if (_requestsRemaining <= 0)
        {
            _logger.LogWarning("Unsplash rate limit exceeded");
            throw new InvalidOperationException("Rate limit exceeded. Please try again in an hour.");
        }

        _logger.LogInformation(
            "Searching Unsplash for: {Query} (count: {Count})",
            request.Query, request.Count);

        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = BuildSearchUrl(request);
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", _apiKey);

                var response = await _httpClient.SendAsync(httpRequest, ct);
                UpdateRateLimitInfo(response.Headers);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Unsplash rate limit hit (429). Attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelay * attempt, ct);
                        continue;
                    }
                    throw new InvalidOperationException("Unsplash rate limit exceeded");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException("Invalid Unsplash API key");
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var doc = JsonDocument.Parse(json);

                var results = new List<StockMediaResult>();

                if (doc.RootElement.TryGetProperty("results", out var resultsArray))
                {
                    foreach (var result in resultsArray.EnumerateArray())
                    {
                        if (results.Count >= request.Count)
                            break;

                        var parsed = ParsePhotoResult(result);
                        if (parsed != null)
                        {
                            results.Add(parsed);
                        }
                    }
                }

                _logger.LogInformation(
                    "Found {Count} images on Unsplash. Quota: {Remaining}/{Limit}",
                    results.Count, _requestsRemaining, _requestsLimit);

                return results;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "HTTP error searching Unsplash (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(retryDelay * attempt, ct);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout searching Unsplash for {Query}", request.Query);
                return new List<StockMediaResult>();
            }
        }

        _logger.LogWarning("Failed to search Unsplash after {MaxRetries} attempts", maxRetries);
        return new List<StockMediaResult>();
    }

    private StockMediaResult? ParsePhotoResult(JsonElement photo)
    {
        if (!photo.TryGetProperty("id", out var idElement))
            return null;

        var id = idElement.GetString() ?? string.Empty;
        string? thumbnailUrl = null;
        string? previewUrl = null;
        string? fullSizeUrl = null;
        string? downloadLocation = null;

        if (photo.TryGetProperty("urls", out var urls))
        {
            if (urls.TryGetProperty("thumb", out var thumb))
                thumbnailUrl = thumb.GetString();
            if (urls.TryGetProperty("small", out var small))
                previewUrl = small.GetString();
            if (urls.TryGetProperty("regular", out var regular))
                fullSizeUrl = regular.GetString();
        }

        if (photo.TryGetProperty("links", out var links) &&
            links.TryGetProperty("download_location", out var downloadLoc))
        {
            downloadLocation = downloadLoc.GetString();
        }

        var width = photo.TryGetProperty("width", out var widthEl) ? widthEl.GetInt32() : 0;
        var height = photo.TryGetProperty("height", out var heightEl) ? heightEl.GetInt32() : 0;

        string? photographer = null;
        string? photographerUrl = null;

        if (photo.TryGetProperty("user", out var user))
        {
            if (user.TryGetProperty("name", out var name))
                photographer = name.GetString();
            if (user.TryGetProperty("links", out var userLinks) &&
                userLinks.TryGetProperty("html", out var html))
                photographerUrl = html.GetString();
        }

        var licensing = new AssetLicensingInfo
        {
            LicenseType = "Unsplash License",
            Attribution = photographer != null ? $"Photo by {photographer} on Unsplash" : "Photo from Unsplash",
            LicenseUrl = "https://unsplash.com/license",
            CommercialUseAllowed = true,
            AttributionRequired = true,
            CreatorName = photographer,
            CreatorUrl = photographerUrl,
            SourcePlatform = "Unsplash"
        };

        return new StockMediaResult
        {
            Id = id,
            Type = StockMediaType.Image,
            Provider = StockMediaProvider.Unsplash,
            ThumbnailUrl = thumbnailUrl ?? string.Empty,
            PreviewUrl = previewUrl ?? string.Empty,
            FullSizeUrl = fullSizeUrl ?? string.Empty,
            DownloadUrl = downloadLocation,
            Width = width,
            Height = height,
            Licensing = licensing,
            Metadata = new Dictionary<string, string>
            {
                ["unsplash_id"] = id,
                ["photographer"] = photographer ?? string.Empty,
                ["photographer_url"] = photographerUrl ?? string.Empty,
                ["download_location"] = downloadLocation ?? string.Empty
            }
        };
    }

    private string BuildSearchUrl(StockMediaSearchRequest request)
    {
        var baseUrl = "https://api.unsplash.com/search/photos";
        var query = Uri.EscapeDataString(request.Query);
        var perPage = Math.Min(request.Count, 30);
        var page = request.Page;

        var url = $"{baseUrl}?query={query}&per_page={perPage}&page={page}";

        if (request.Orientation != null)
            url += $"&orientation={request.Orientation}";

        if (request.Color != null)
            url += $"&color={request.Color}";

        if (request.SafeSearchEnabled)
            url += "&content_filter=high";

        return url;
    }

    public RateLimitStatus GetRateLimitStatus()
    {
        return new RateLimitStatus
        {
            Provider = StockMediaProvider.Unsplash,
            RequestsRemaining = _requestsRemaining,
            RequestsLimit = _requestsLimit,
            IsLimited = _requestsRemaining <= 0
        };
    }

    public async Task<bool> ValidateAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.unsplash.com/photos?per_page=1");
            request.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", _apiKey);

            var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Unsplash API key");
            return false;
        }
    }

    public async Task<byte[]> DownloadMediaAsync(string url, CancellationToken ct)
    {
        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(ct);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Failed to download media (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(retryDelay * attempt, ct);
            }
        }

        throw new InvalidOperationException($"Failed to download media after {maxRetries} attempts");
    }

    public async Task TrackDownloadAsync(string mediaId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(mediaId) || string.IsNullOrEmpty(_apiKey))
            return;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, mediaId);
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
