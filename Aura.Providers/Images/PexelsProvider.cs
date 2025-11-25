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
using Aura.Core.Models.Assets;
using Aura.Core.Models.StockMedia;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

// Alias to disambiguate Asset types
using CoreAsset = Aura.Core.Models.Asset;

namespace Aura.Providers.Images;

/// <summary>
/// Unified Pexels provider that consolidates PexelsStockProvider, PexelsImageProvider, 
/// and EnhancedPexelsProvider into a single implementation.
/// 
/// Implements both IStockProvider (basic interface) and IEnhancedStockProvider (advanced interface)
/// for backward compatibility while providing enhanced functionality.
/// 
/// API Documentation: https://www.pexels.com/api/documentation/
/// Rate Limit: 200 requests per hour for free tier
/// </summary>
public class PexelsProvider : IStockProvider, IEnhancedStockProvider
{
    private readonly ILogger<PexelsProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    
    private int _requestsRemaining = 200;
    private int _requestsLimit = 200;
    private DateTime _rateLimitReset = DateTime.UtcNow.AddHours(1);

    private const int MaxRetries = 3;
    private const int MaxPerPage = 80;

    public PexelsProvider(
        ILogger<PexelsProvider> logger,
        HttpClient httpClient,
        string? apiKey = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    #region IEnhancedStockProvider Implementation

    /// <inheritdoc />
    public StockMediaProvider ProviderName => StockMediaProvider.Pexels;

    /// <inheritdoc />
    public bool SupportsVideo => true;

    /// <inheritdoc />
    public async Task<List<StockMediaResult>> SearchAsync(
        StockMediaSearchRequest request,
        CancellationToken ct)
    {
        if (!ValidateApiKey(out var error))
        {
            _logger.LogWarning("Pexels API key validation failed: {Error}", error);
            return new List<StockMediaResult>();
        }

        if (!CheckRateLimit())
        {
            return new List<StockMediaResult>();
        }

        var mediaType = request.Type ?? StockMediaType.Image;
        _logger.LogInformation(
            "Searching Pexels for {MediaType}: {Query} (count: {Count})",
            mediaType, request.Query, request.Count);

        var retryDelay = TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var results = mediaType == StockMediaType.Video
                    ? await SearchVideosInternalAsync(request, ct).ConfigureAwait(false)
                    : await SearchImagesInternalAsync(request, ct).ConfigureAwait(false);

                return results;
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "HTTP error searching Pexels (attempt {Attempt}/{MaxRetries})", attempt, MaxRetries);
                await Task.Delay(retryDelay * attempt, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout searching Pexels for {Query}", request.Query);
                return new List<StockMediaResult>();
            }
        }

        _logger.LogWarning("Failed to search Pexels after {MaxRetries} attempts", MaxRetries);
        return new List<StockMediaResult>();
    }

    /// <inheritdoc />
    public RateLimitStatus GetRateLimitStatus()
    {
        return new RateLimitStatus
        {
            Provider = StockMediaProvider.Pexels,
            RequestsRemaining = _requestsRemaining,
            RequestsLimit = _requestsLimit,
            ResetTime = _rateLimitReset,
            IsLimited = _requestsRemaining <= 0 && DateTime.UtcNow < _rateLimitReset
        };
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.pexels.com/v1/curated?per_page=1");
            request.Headers.Authorization = new AuthenticationHeaderValue(_apiKey);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Pexels API key");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadMediaAsync(string url, CancellationToken ct)
    {
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "Failed to download media (attempt {Attempt}/{MaxRetries})", attempt, MaxRetries);
                await Task.Delay(retryDelay * attempt, ct).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException($"Failed to download media after {MaxRetries} attempts");
    }

    /// <inheritdoc />
    public Task TrackDownloadAsync(string mediaId, CancellationToken ct)
    {
        // Pexels doesn't require download tracking
        return Task.CompletedTask;
    }

    #endregion

    #region IStockProvider Implementation

    /// <inheritdoc />
    async Task<IReadOnlyList<CoreAsset>> IStockProvider.SearchAsync(string query, int count, CancellationToken ct)
    {
        if (!ValidateApiKey(out var error))
        {
            _logger.LogWarning("Pexels API key validation failed: {Error}", error);
            return Array.Empty<CoreAsset>();
        }

        if (!CheckRateLimit())
        {
            return Array.Empty<CoreAsset>();
        }

        _logger.LogInformation("Searching Pexels for: {Query} (count: {Count})", query, count);

        var retryDelay = TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page={Math.Min(count, MaxPerPage)}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue(_apiKey!);

                var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
                UpdateRateLimitInfo(response.Headers);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Pexels rate limit hit (429). Attempt {Attempt}/{MaxRetries}", attempt, MaxRetries);
                    
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(retryDelay * attempt, ct).ConfigureAwait(false);
                        continue;
                    }
                    return Array.Empty<CoreAsset>();
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var doc = JsonDocument.Parse(json);

                var assets = new List<CoreAsset>();

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
                            assets.Add(new CoreAsset(
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
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "HTTP error searching Pexels (attempt {Attempt}/{MaxRetries})", attempt, MaxRetries);
                await Task.Delay(retryDelay * attempt, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout searching Pexels for {Query}", query);
                return Array.Empty<CoreAsset>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Pexels for {Query}", query);
                return Array.Empty<CoreAsset>();
            }
        }

        _logger.LogWarning("Failed to search Pexels after {MaxRetries} attempts", MaxRetries);
        return Array.Empty<CoreAsset>();
    }

    #endregion

    #region Public Helper Methods

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

    #endregion

    #region Private Methods

    private bool CheckRateLimit()
    {
        if (_requestsRemaining <= 0 && DateTime.UtcNow < _rateLimitReset)
        {
            var waitTime = _rateLimitReset - DateTime.UtcNow;
            _logger.LogWarning("Pexels rate limit exceeded. Resets in {WaitTime}", waitTime);
            return false;
        }
        return true;
    }

    private async Task<List<StockMediaResult>> SearchImagesInternalAsync(
        StockMediaSearchRequest request,
        CancellationToken ct)
    {
        var url = BuildImageSearchUrl(request);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(_apiKey!);

        var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        UpdateRateLimitInfo(response.Headers);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("Pexels rate limit exceeded");
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(json);

        var results = new List<StockMediaResult>();

        if (doc.RootElement.TryGetProperty("photos", out var photos))
        {
            foreach (var photo in photos.EnumerateArray())
            {
                if (results.Count >= request.Count)
                    break;

                var result = ParsePhotoResult(photo);
                if (result != null)
                {
                    results.Add(result);
                }
            }
        }

        _logger.LogInformation(
            "Found {Count} images on Pexels. Quota: {Remaining}/{Limit}",
            results.Count, _requestsRemaining, _requestsLimit);

        return results;
    }

    private async Task<List<StockMediaResult>> SearchVideosInternalAsync(
        StockMediaSearchRequest request,
        CancellationToken ct)
    {
        var url = BuildVideoSearchUrl(request);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(_apiKey!);

        var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        UpdateRateLimitInfo(response.Headers);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("Pexels rate limit exceeded");
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(json);

        var results = new List<StockMediaResult>();

        if (doc.RootElement.TryGetProperty("videos", out var videos))
        {
            foreach (var video in videos.EnumerateArray())
            {
                if (results.Count >= request.Count)
                    break;

                var result = ParseVideoResult(video);
                if (result != null)
                {
                    results.Add(result);
                }
            }
        }

        _logger.LogInformation(
            "Found {Count} videos on Pexels. Quota: {Remaining}/{Limit}",
            results.Count, _requestsRemaining, _requestsLimit);

        return results;
    }

    private StockMediaResult? ParsePhotoResult(JsonElement photo)
    {
        if (!photo.TryGetProperty("id", out var idElement))
            return null;

        var id = idElement.GetInt32().ToString();
        string? thumbnailUrl = null;
        string? previewUrl = null;
        string? fullSizeUrl = null;

        if (photo.TryGetProperty("src", out var src))
        {
            if (src.TryGetProperty("tiny", out var tiny))
                thumbnailUrl = tiny.GetString();
            if (src.TryGetProperty("medium", out var medium))
                previewUrl = medium.GetString();
            if (src.TryGetProperty("large2x", out var large2x))
                fullSizeUrl = large2x.GetString();
        }

        var width = photo.TryGetProperty("width", out var widthEl) ? widthEl.GetInt32() : 0;
        var height = photo.TryGetProperty("height", out var heightEl) ? heightEl.GetInt32() : 0;

        string? photographer = null;
        string? photographerUrl = null;

        if (photo.TryGetProperty("photographer", out var photoProp))
            photographer = photoProp.GetString();
        if (photo.TryGetProperty("photographer_url", out var photoUrl))
            photographerUrl = photoUrl.GetString();

        var licensing = new AssetLicensingInfo
        {
            LicenseType = "Pexels License",
            Attribution = photographer != null ? $"Photo by {photographer} on Pexels" : "Photo from Pexels",
            LicenseUrl = "https://www.pexels.com/license/",
            CommercialUseAllowed = true,
            AttributionRequired = false,
            CreatorName = photographer,
            CreatorUrl = photographerUrl,
            SourcePlatform = "Pexels"
        };

        return new StockMediaResult
        {
            Id = id,
            Type = StockMediaType.Image,
            Provider = StockMediaProvider.Pexels,
            ThumbnailUrl = thumbnailUrl ?? string.Empty,
            PreviewUrl = previewUrl ?? string.Empty,
            FullSizeUrl = fullSizeUrl ?? string.Empty,
            Width = width,
            Height = height,
            Licensing = licensing,
            Metadata = new Dictionary<string, string>
            {
                ["pexels_id"] = id,
                ["photographer"] = photographer ?? string.Empty,
                ["photographer_url"] = photographerUrl ?? string.Empty
            }
        };
    }

    private StockMediaResult? ParseVideoResult(JsonElement video)
    {
        if (!video.TryGetProperty("id", out var idElement))
            return null;

        var id = idElement.GetInt32().ToString();
        string? thumbnailUrl = null;
        string? previewUrl = null;
        string? fullSizeUrl = null;

        if (video.TryGetProperty("image", out var imageUrl))
            thumbnailUrl = imageUrl.GetString();

        var width = video.TryGetProperty("width", out var widthEl) ? widthEl.GetInt32() : 0;
        var height = video.TryGetProperty("height", out var heightEl) ? heightEl.GetInt32() : 0;
        var duration = video.TryGetProperty("duration", out var durationEl) ? durationEl.GetInt32() : 0;

        if (video.TryGetProperty("video_files", out var videoFiles))
        {
            foreach (var file in videoFiles.EnumerateArray())
            {
                if (file.TryGetProperty("link", out var link) && 
                    file.TryGetProperty("quality", out var quality))
                {
                    var qualityStr = quality.GetString();
                    if (qualityStr == "hd")
                    {
                        fullSizeUrl = link.GetString();
                        break;
                    }
                    else if (fullSizeUrl == null && qualityStr == "sd")
                    {
                        fullSizeUrl = link.GetString();
                    }
                }
            }
        }

        string? creator = null;
        string? creatorUrl = null;

        if (video.TryGetProperty("user", out var user))
        {
            if (user.TryGetProperty("name", out var nameProp))
                creator = nameProp.GetString();
            if (user.TryGetProperty("url", out var urlProp))
                creatorUrl = urlProp.GetString();
        }

        var licensing = new AssetLicensingInfo
        {
            LicenseType = "Pexels License",
            Attribution = creator != null ? $"Video by {creator} on Pexels" : "Video from Pexels",
            LicenseUrl = "https://www.pexels.com/license/",
            CommercialUseAllowed = true,
            AttributionRequired = false,
            CreatorName = creator,
            CreatorUrl = creatorUrl,
            SourcePlatform = "Pexels"
        };

        return new StockMediaResult
        {
            Id = id,
            Type = StockMediaType.Video,
            Provider = StockMediaProvider.Pexels,
            ThumbnailUrl = thumbnailUrl ?? string.Empty,
            PreviewUrl = previewUrl ?? string.Empty,
            FullSizeUrl = fullSizeUrl ?? string.Empty,
            Width = width,
            Height = height,
            Duration = TimeSpan.FromSeconds(duration),
            Licensing = licensing,
            Metadata = new Dictionary<string, string>
            {
                ["pexels_id"] = id,
                ["creator"] = creator ?? string.Empty,
                ["creator_url"] = creatorUrl ?? string.Empty,
                ["duration_seconds"] = duration.ToString()
            }
        };
    }

    private string BuildImageSearchUrl(StockMediaSearchRequest request)
    {
        var baseUrl = "https://api.pexels.com/v1/search";
        var query = Uri.EscapeDataString(request.Query);
        var perPage = Math.Min(request.Count, MaxPerPage);
        var page = request.Page;

        var url = $"{baseUrl}?query={query}&per_page={perPage}&page={page}";

        if (request.Orientation != null)
            url += $"&orientation={request.Orientation}";

        if (request.Color != null)
            url += $"&color={request.Color}";

        return url;
    }

    private string BuildVideoSearchUrl(StockMediaSearchRequest request)
    {
        var baseUrl = "https://api.pexels.com/videos/search";
        var query = Uri.EscapeDataString(request.Query);
        var perPage = Math.Min(request.Count, MaxPerPage);
        var page = request.Page;

        var url = $"{baseUrl}?query={query}&per_page={perPage}&page={page}";

        if (request.Orientation != null)
            url += $"&orientation={request.Orientation}";

        if (request.MinWidth.HasValue)
            url += $"&min_width={request.MinWidth.Value}";

        if (request.MinHeight.HasValue)
            url += $"&min_height={request.MinHeight.Value}";

        if (request.MinDuration.HasValue)
            url += $"&min_duration={request.MinDuration.Value.TotalSeconds}";

        if (request.MaxDuration.HasValue)
            url += $"&max_duration={request.MaxDuration.Value.TotalSeconds}";

        return url;
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

    #endregion
}
