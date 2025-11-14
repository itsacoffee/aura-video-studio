using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Sfx;

/// <summary>
/// Freesound.org API provider for sound effects
/// Requires API key from https://freesound.org/apiv2/apply/
/// </summary>
public class FreesoundSfxProvider : ISfxProvider
{
    private readonly ILogger<FreesoundSfxProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private const string BaseUrl = "https://freesound.org/apiv2";

    public string Name => "Freesound";

    public FreesoundSfxProvider(
        ILogger<FreesoundSfxProvider> logger,
        HttpClient httpClient,
        string? apiKey = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_apiKey}");
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Freesound API key not configured");
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/me/", ct).ConfigureAwait(false);
            var isAvailable = response.IsSuccessStatusCode;
            _logger.LogInformation("Freesound API {Status}", isAvailable ? "available" : "unavailable");
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Freesound availability");
            return false;
        }
    }

    public async Task<SearchResult<SfxAsset>> SearchAsync(
        SfxSearchCriteria criteria,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("Freesound API key not configured");

        try
        {
            var queryParams = BuildQueryParams(criteria);
            var url = $"{BaseUrl}/search/text/?{queryParams}";

            _logger.LogInformation("Searching Freesound: {Url}", url);

            var response = await _httpClient.GetFromJsonAsync<FreesoundSearchResponse>(url, ct).ConfigureAwait(false);

            if (response == null)
                return new SearchResult<SfxAsset>(new List<SfxAsset>(), 0, criteria.Page, criteria.PageSize, 0);

            var assets = response.Results.Select(MapToSfxAsset).ToList();
            var totalPages = (int)Math.Ceiling(response.Count / (double)criteria.PageSize);

            return new SearchResult<SfxAsset>(
                Results: assets,
                TotalCount: response.Count,
                Page: criteria.Page,
                PageSize: criteria.PageSize,
                TotalPages: totalPages
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Freesound");
            throw;
        }
    }

    public async Task<SfxAsset?> GetByIdAsync(string assetId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("Freesound API key not configured");

        try
        {
            var url = $"{BaseUrl}/sounds/{assetId}/";
            var sound = await _httpClient.GetFromJsonAsync<FreesoundSound>(url, ct).ConfigureAwait(false);

            return sound != null ? MapToSfxAsset(sound) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Freesound asset {AssetId}", assetId);
            return null;
        }
    }

    public async Task<string> DownloadAsync(string assetId, string destinationPath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("Freesound API key not configured");

        try
        {
            var sound = await _httpClient.GetFromJsonAsync<FreesoundSound>($"{BaseUrl}/sounds/{assetId}/", ct).ConfigureAwait(false);
            if (sound?.Previews?.PreviewHqMp3 == null)
                throw new InvalidOperationException($"No download URL for asset {assetId}");

            var downloadUrl = sound.Previews.PreviewHqMp3;
            var response = await _httpClient.GetAsync(downloadUrl, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            await File.WriteAllBytesAsync(destinationPath, bytes, ct).ConfigureAwait(false);

            _logger.LogInformation("Downloaded Freesound asset {AssetId} to {Path}", assetId, destinationPath);
            return destinationPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading Freesound asset {AssetId}", assetId);
            throw;
        }
    }

    public async Task<string?> GetPreviewUrlAsync(string assetId, CancellationToken ct = default)
    {
        var asset = await GetByIdAsync(assetId, ct).ConfigureAwait(false);
        return asset?.PreviewUrl;
    }

    public async Task<SearchResult<SfxAsset>> FindByTagsAsync(
        List<string> tags,
        int maxResults = 20,
        CancellationToken ct = default)
    {
        var criteria = new SfxSearchCriteria(
            Tags: tags,
            PageSize: maxResults
        );

        return await SearchAsync(criteria, ct).ConfigureAwait(false);
    }

    private string BuildQueryParams(SfxSearchCriteria criteria)
    {
        var parameters = new List<string>
        {
            $"page={criteria.Page}",
            $"page_size={criteria.PageSize}",
            "fields=id,name,description,tags,duration,license,username,previews"
        };

        var filters = new List<string>();

        if (criteria.MaxDuration.HasValue)
            filters.Add($"duration:[0 TO {criteria.MaxDuration.Value.TotalSeconds}]");

        if (criteria.Type.HasValue)
        {
            var typeTag = criteria.Type.Value.ToString().ToLowerInvariant();
            filters.Add($"tag:{typeTag}");
        }

        if (criteria.Tags != null && criteria.Tags.Count > 0)
        {
            foreach (var tag in criteria.Tags)
            {
                filters.Add($"tag:{tag}");
            }
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchQuery))
        {
            parameters.Add($"query={Uri.EscapeDataString(criteria.SearchQuery)}");
        }

        if (filters.Count > 0)
        {
            parameters.Add($"filter={Uri.EscapeDataString(string.Join(" ", filters))}");
        }

        return string.Join("&", parameters);
    }

    private SfxAsset MapToSfxAsset(FreesoundSound sound)
    {
        var licenseType = MapLicense(sound.License);
        var (commercialUse, attribution) = GetLicenseRequirements(licenseType);

        var tags = sound.Tags ?? new List<string>();
        var sfxType = InferSfxType(tags);

        return new SfxAsset(
            AssetId: sound.Id.ToString(),
            Title: sound.Name,
            Artist: sound.Username,
            FilePath: string.Empty,
            PreviewUrl: sound.Previews?.PreviewHqMp3,
            Duration: TimeSpan.FromSeconds(sound.Duration),
            LicenseType: licenseType,
            LicenseUrl: $"https://creativecommons.org/licenses/{sound.License?.Replace("http://creativecommons.org/licenses/", "").TrimEnd('/')}",
            CommercialUseAllowed: commercialUse,
            AttributionRequired: attribution,
            AttributionText: $"{sound.Name} by {sound.Username} (Freesound)",
            SourcePlatform: "Freesound",
            CreatorProfileUrl: $"https://freesound.org/people/{sound.Username}/",
            Type: sfxType,
            Tags: tags,
            Description: sound.Description ?? string.Empty,
            Metadata: new Dictionary<string, object>
            {
                ["FreesoundId"] = sound.Id,
                ["NumDownloads"] = sound.NumDownloads ?? 0,
                ["AvgRating"] = sound.AvgRating ?? 0
            }
        );
    }

    private LicenseType MapLicense(string? license)
    {
        if (string.IsNullOrEmpty(license))
            return LicenseType.Custom;

        return license.ToLowerInvariant() switch
        {
            var l when l.Contains("cc0") => LicenseType.CreativeCommonsZero,
            var l when l.Contains("by-nc-nd") => LicenseType.CreativeCommonsBYNCND,
            var l when l.Contains("by-nc-sa") => LicenseType.CreativeCommonsBYNCSA,
            var l when l.Contains("by-nc") => LicenseType.CreativeCommonsBYNC,
            var l when l.Contains("by-sa") => LicenseType.CreativeCommonsBYSA,
            var l when l.Contains("by") => LicenseType.CreativeCommonsBY,
            _ => LicenseType.Custom
        };
    }

    private (bool CommercialUse, bool Attribution) GetLicenseRequirements(LicenseType licenseType)
    {
        return licenseType switch
        {
            LicenseType.PublicDomain => (true, false),
            LicenseType.CreativeCommonsZero => (true, false),
            LicenseType.CreativeCommonsBY => (true, true),
            LicenseType.CreativeCommonsBYSA => (true, true),
            LicenseType.CreativeCommonsBYNC => (false, true),
            LicenseType.CreativeCommonsBYNCND => (false, true),
            LicenseType.CreativeCommonsBYNCSA => (false, true),
            _ => (false, true)
        };
    }

    private SoundEffectType InferSfxType(List<string> tags)
    {
        var tagSet = new HashSet<string>(tags.Select(t => t.ToLowerInvariant()));

        if (tagSet.Overlaps(new[] { "whoosh", "swish", "swoosh" }))
            return SoundEffectType.Whoosh;
        if (tagSet.Overlaps(new[] { "impact", "hit", "punch", "slam" }))
            return SoundEffectType.Impact;
        if (tagSet.Overlaps(new[] { "click", "button", "switch" }))
            return SoundEffectType.Click;
        if (tagSet.Overlaps(new[] { "ui", "interface", "menu" }))
            return SoundEffectType.UI;
        if (tagSet.Overlaps(new[] { "transition", "fade", "change" }))
            return SoundEffectType.Transition;
        if (tagSet.Overlaps(new[] { "ambient", "atmosphere", "background" }))
            return SoundEffectType.Ambient;
        if (tagSet.Overlaps(new[] { "nature", "wind", "water", "rain" }))
            return SoundEffectType.Nature;
        if (tagSet.Overlaps(new[] { "technology", "computer", "digital", "tech" }))
            return SoundEffectType.Technology;
        if (tagSet.Overlaps(new[] { "action", "fight", "combat" }))
            return SoundEffectType.Action;
        if (tagSet.Overlaps(new[] { "notification", "alert", "beep", "chime" }))
            return SoundEffectType.Notification;

        return SoundEffectType.Ambient;
    }

    private sealed record FreesoundSearchResponse(
        int Count,
        string? Next,
        string? Previous,
        List<FreesoundSound> Results
    );

    private sealed record FreesoundSound(
        int Id,
        string Name,
        string? Description,
        List<string>? Tags,
        double Duration,
        string? License,
        string Username,
        FreesoundPreviews? Previews,
        int? NumDownloads,
        double? AvgRating
    );

    private sealed record FreesoundPreviews(
        string? PreviewHqMp3,
        string? PreviewHqOgg,
        string? PreviewLqMp3,
        string? PreviewLqOgg
    );
}
