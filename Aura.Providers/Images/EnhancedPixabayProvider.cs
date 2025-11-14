using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Aura.Core.Models.StockMedia;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Enhanced Pixabay provider with video support and comprehensive licensing
/// API Documentation: https://pixabay.com/api/docs/
/// Rate Limit: No explicit limit, but reasonable use expected
/// </summary>
public class EnhancedPixabayProvider : IEnhancedStockProvider
{
    private const string VIMEO_THUMBNAIL_URL_TEMPLATE = "https://i.vimeocdn.com/video/{0}_295x166.jpg";

    private readonly ILogger<EnhancedPixabayProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public EnhancedPixabayProvider(
        ILogger<EnhancedPixabayProvider> logger,
        HttpClient httpClient,
        string? apiKey = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public StockMediaProvider ProviderName => StockMediaProvider.Pixabay;

    public bool SupportsVideo => true;

    public async Task<List<StockMediaResult>> SearchAsync(
        StockMediaSearchRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Pixabay API key not configured");
            return new List<StockMediaResult>();
        }

        var mediaType = request.Type ?? StockMediaType.Image;
        _logger.LogInformation(
            "Searching Pixabay for {MediaType}: {Query} (count: {Count})",
            mediaType, request.Query, request.Count);

        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var results = mediaType == StockMediaType.Video
                    ? await SearchVideosInternalAsync(request, ct).ConfigureAwait(false)
                    : await SearchImagesInternalAsync(request, ct).ConfigureAwait(false);

                return results;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "HTTP error searching Pixabay (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(retryDelay * attempt, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout searching Pixabay for {Query}", request.Query);
                return new List<StockMediaResult>();
            }
        }

        _logger.LogWarning("Failed to search Pixabay after {MaxRetries} attempts", maxRetries);
        return new List<StockMediaResult>();
    }

    private async Task<List<StockMediaResult>> SearchImagesInternalAsync(
        StockMediaSearchRequest request,
        CancellationToken ct)
    {
        var url = BuildImageSearchUrl(request);
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("Pixabay rate limit exceeded");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException("Invalid Pixabay API request. Check your API key and query.");
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(json);

        var results = new List<StockMediaResult>();

        if (doc.RootElement.TryGetProperty("hits", out var hits))
        {
            foreach (var hit in hits.EnumerateArray())
            {
                if (results.Count >= request.Count)
                    break;

                var result = ParseImageResult(hit);
                if (result != null)
                {
                    results.Add(result);
                }
            }
        }

        _logger.LogInformation("Found {Count} images on Pixabay", results.Count);
        return results;
    }

    private async Task<List<StockMediaResult>> SearchVideosInternalAsync(
        StockMediaSearchRequest request,
        CancellationToken ct)
    {
        var url = BuildVideoSearchUrl(request);
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("Pixabay rate limit exceeded");
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(json);

        var results = new List<StockMediaResult>();

        if (doc.RootElement.TryGetProperty("hits", out var hits))
        {
            foreach (var hit in hits.EnumerateArray())
            {
                if (results.Count >= request.Count)
                    break;

                var result = ParseVideoResult(hit);
                if (result != null)
                {
                    results.Add(result);
                }
            }
        }

        _logger.LogInformation("Found {Count} videos on Pixabay", results.Count);
        return results;
    }

    private StockMediaResult? ParseImageResult(JsonElement image)
    {
        if (!image.TryGetProperty("id", out var idElement))
            return null;

        var id = idElement.GetInt32().ToString();
        string? thumbnailUrl = null;
        string? previewUrl = null;
        string? fullSizeUrl = null;

        if (image.TryGetProperty("previewURL", out var preview))
            thumbnailUrl = preview.GetString();
        if (image.TryGetProperty("webformatURL", out var webformat))
            previewUrl = webformat.GetString();
        if (image.TryGetProperty("largeImageURL", out var large))
            fullSizeUrl = large.GetString();

        var width = image.TryGetProperty("imageWidth", out var widthEl) ? widthEl.GetInt32() : 0;
        var height = image.TryGetProperty("imageHeight", out var heightEl) ? heightEl.GetInt32() : 0;

        string? photographer = null;
        if (image.TryGetProperty("user", out var userProp))
            photographer = userProp.GetString();

        var licensing = new AssetLicensingInfo
        {
            LicenseType = "Pixabay License",
            Attribution = photographer != null ? $"Image by {photographer} from Pixabay" : "Image from Pixabay",
            LicenseUrl = "https://pixabay.com/service/license/",
            CommercialUseAllowed = true,
            AttributionRequired = false,
            CreatorName = photographer,
            SourcePlatform = "Pixabay"
        };

        return new StockMediaResult
        {
            Id = id,
            Type = StockMediaType.Image,
            Provider = StockMediaProvider.Pixabay,
            ThumbnailUrl = thumbnailUrl ?? string.Empty,
            PreviewUrl = previewUrl ?? string.Empty,
            FullSizeUrl = fullSizeUrl ?? string.Empty,
            Width = width,
            Height = height,
            Licensing = licensing,
            Metadata = new Dictionary<string, string>
            {
                ["pixabay_id"] = id,
                ["user"] = photographer ?? string.Empty
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

        if (video.TryGetProperty("picture_id", out var pictureId))
        {
            var picId = pictureId.GetString();
            thumbnailUrl = string.Format(VIMEO_THUMBNAIL_URL_TEMPLATE, picId);
        }

        var width = 0;
        var height = 0;
        var duration = video.TryGetProperty("duration", out var durationEl) ? durationEl.GetInt32() : 0;

        if (video.TryGetProperty("videos", out var videos))
        {
            if (videos.TryGetProperty("large", out var large))
            {
                if (large.TryGetProperty("url", out var largeUrl))
                    fullSizeUrl = largeUrl.GetString();
                if (large.TryGetProperty("width", out var w))
                    width = w.GetInt32();
                if (large.TryGetProperty("height", out var h))
                    height = h.GetInt32();
            }
            else if (videos.TryGetProperty("medium", out var medium))
            {
                if (medium.TryGetProperty("url", out var mediumUrl))
                    fullSizeUrl = mediumUrl.GetString();
                if (medium.TryGetProperty("width", out var w))
                    width = w.GetInt32();
                if (medium.TryGetProperty("height", out var h))
                    height = h.GetInt32();
            }
        }

        string? creator = null;
        if (video.TryGetProperty("user", out var userProp))
            creator = userProp.GetString();

        var licensing = new AssetLicensingInfo
        {
            LicenseType = "Pixabay License",
            Attribution = creator != null ? $"Video by {creator} from Pixabay" : "Video from Pixabay",
            LicenseUrl = "https://pixabay.com/service/license/",
            CommercialUseAllowed = true,
            AttributionRequired = false,
            CreatorName = creator,
            SourcePlatform = "Pixabay"
        };

        return new StockMediaResult
        {
            Id = id,
            Type = StockMediaType.Video,
            Provider = StockMediaProvider.Pixabay,
            ThumbnailUrl = thumbnailUrl ?? string.Empty,
            PreviewUrl = previewUrl ?? string.Empty,
            FullSizeUrl = fullSizeUrl ?? string.Empty,
            Width = width,
            Height = height,
            Duration = TimeSpan.FromSeconds(duration),
            Licensing = licensing,
            Metadata = new Dictionary<string, string>
            {
                ["pixabay_id"] = id,
                ["user"] = creator ?? string.Empty,
                ["duration_seconds"] = duration.ToString()
            }
        };
    }

    private string BuildImageSearchUrl(StockMediaSearchRequest request)
    {
        var baseUrl = "https://pixabay.com/api/";
        var query = Uri.EscapeDataString(request.Query);
        var perPage = Math.Min(request.Count, 200);
        var page = request.Page;

        var url = $"{baseUrl}?key={_apiKey}&q={query}&per_page={perPage}&page={page}&image_type=photo";

        if (request.Orientation != null)
            url += $"&orientation={request.Orientation}";

        if (request.Color != null)
            url += $"&colors={request.Color}";

        if (request.SafeSearchEnabled)
            url += "&safesearch=true";

        if (request.MinWidth.HasValue)
            url += $"&min_width={request.MinWidth.Value}";

        if (request.MinHeight.HasValue)
            url += $"&min_height={request.MinHeight.Value}";

        return url;
    }

    private string BuildVideoSearchUrl(StockMediaSearchRequest request)
    {
        var baseUrl = "https://pixabay.com/api/videos/";
        var query = Uri.EscapeDataString(request.Query);
        var perPage = Math.Min(request.Count, 200);
        var page = request.Page;

        var url = $"{baseUrl}?key={_apiKey}&q={query}&per_page={perPage}&page={page}";

        if (request.SafeSearchEnabled)
            url += "&safesearch=true";

        if (request.MinWidth.HasValue)
            url += $"&min_width={request.MinWidth.Value}";

        if (request.MinHeight.HasValue)
            url += $"&min_height={request.MinHeight.Value}";

        return url;
    }

    public RateLimitStatus GetRateLimitStatus()
    {
        return new RateLimitStatus
        {
            Provider = StockMediaProvider.Pixabay,
            RequestsRemaining = 1000,
            RequestsLimit = 1000,
            IsLimited = false
        };
    }

    public async Task<bool> ValidateAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        try
        {
            var url = $"https://pixabay.com/api/?key={_apiKey}&q=test&per_page=1";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Pixabay API key");
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
                var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Failed to download media (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(retryDelay * attempt, ct).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException($"Failed to download media after {maxRetries} attempts");
    }

    public Task TrackDownloadAsync(string mediaId, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
