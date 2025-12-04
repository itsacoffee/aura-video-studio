using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Audio;

/// <summary>
/// Pixabay Music API provider for royalty-free background music.
/// Requires API key from https://pixabay.com/api/docs/
/// </summary>
public class PixabayMusicProvider : IMusicProvider
{
    private readonly ILogger<PixabayMusicProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private const string BaseUrl = "https://pixabay.com/api/videos/";

    public string Name => "Pixabay";

    public PixabayMusicProvider(
        ILogger<PixabayMusicProvider> logger,
        HttpClient httpClient,
        string? apiKey = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey ?? Environment.GetEnvironmentVariable("PIXABAY_API_KEY");
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Pixabay API key not configured");
            return false;
        }

        try
        {
            // Test API with a simple search
            var response = await _httpClient.GetAsync(
                $"{BaseUrl}?key={_apiKey}&per_page=1",
                ct
            ).ConfigureAwait(false);

            var isAvailable = response.IsSuccessStatusCode;
            _logger.LogInformation("Pixabay API {Status}", isAvailable ? "available" : "unavailable");
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Pixabay availability");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<SearchResult<MusicAsset>> SearchAsync(
        MusicSearchCriteria criteria,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Pixabay API key not configured, returning mock results");
            return GetMockResults(criteria);
        }

        try
        {
            var queryParams = BuildQueryParams(criteria);
            var url = $"{BaseUrl}?key={_apiKey}&{queryParams}";

            _logger.LogInformation("Searching Pixabay music: {Query}", criteria.SearchQuery ?? "all");

            var response = await _httpClient.GetFromJsonAsync<PixabayResponse>(url, ct).ConfigureAwait(false);

            if (response == null || response.Hits == null)
            {
                return new SearchResult<MusicAsset>(
                    Results: new List<MusicAsset>(),
                    TotalCount: 0,
                    Page: criteria.Page,
                    PageSize: criteria.PageSize,
                    TotalPages: 0
                );
            }

            var assets = response.Hits.Select(MapToMusicAsset).ToList();
            var totalPages = (int)Math.Ceiling(response.TotalHits / (double)criteria.PageSize);

            return new SearchResult<MusicAsset>(
                Results: assets,
                TotalCount: response.TotalHits,
                Page: criteria.Page,
                PageSize: criteria.PageSize,
                TotalPages: totalPages
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Pixabay");
            return GetMockResults(criteria);
        }
    }

    /// <inheritdoc />
    public async Task<MusicAsset?> GetByIdAsync(string assetId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Pixabay API key not configured");
            return GetMockAsset(assetId);
        }

        try
        {
            var url = $"{BaseUrl}?key={_apiKey}&id={assetId}";
            var response = await _httpClient.GetFromJsonAsync<PixabayResponse>(url, ct).ConfigureAwait(false);

            if (response?.Hits == null || response.Hits.Count == 0)
                return null;

            return MapToMusicAsset(response.Hits[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Pixabay asset {AssetId}", assetId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string> DownloadAsync(
        string assetId,
        string destinationPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Downloading Pixabay asset {AssetId} to {Path}", assetId, destinationPath);

        var asset = await GetByIdAsync(assetId, ct).ConfigureAwait(false);
        if (asset == null)
            throw new InvalidOperationException($"Asset {assetId} not found");

        var downloadUrl = asset.PreviewUrl ?? asset.FilePath;
        if (string.IsNullOrEmpty(downloadUrl) || downloadUrl.StartsWith("/mock"))
        {
            // Create a placeholder file for mock assets
            await CreatePlaceholderAudioAsync(destinationPath, ct).ConfigureAwait(false);
            return destinationPath;
        }

        var response = await _httpClient.GetAsync(downloadUrl, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        await File.WriteAllBytesAsync(destinationPath, bytes, ct).ConfigureAwait(false);

        _logger.LogInformation("Downloaded Pixabay asset to {Path}", destinationPath);
        return destinationPath;
    }

    /// <inheritdoc />
    public async Task<string?> GetPreviewUrlAsync(string assetId, CancellationToken ct = default)
    {
        var asset = await GetByIdAsync(assetId, ct).ConfigureAwait(false);
        return asset?.PreviewUrl;
    }

    private string BuildQueryParams(MusicSearchCriteria criteria)
    {
        var parameters = new List<string>
        {
            $"page={criteria.Page}",
            $"per_page={criteria.PageSize}",
            "video_type=all"  // For audio content from video API
        };

        if (!string.IsNullOrWhiteSpace(criteria.SearchQuery))
        {
            parameters.Add($"q={Uri.EscapeDataString(criteria.SearchQuery)}");
        }

        // Map mood/genre to category if possible
        if (criteria.Genre.HasValue)
        {
            var category = MapGenreToCategory(criteria.Genre.Value);
            if (!string.IsNullOrEmpty(category))
            {
                parameters.Add($"category={category}");
            }
        }

        return string.Join("&", parameters);
    }

    private string? MapGenreToCategory(MusicGenre genre)
    {
        return genre switch
        {
            MusicGenre.Classical => "music",
            MusicGenre.Jazz => "music",
            MusicGenre.Rock => "music",
            MusicGenre.Pop => "music",
            MusicGenre.Electronic => "music",
            MusicGenre.Ambient => "music",
            _ => "music"
        };
    }

    private MusicAsset MapToMusicAsset(PixabayHit hit)
    {
        var duration = TimeSpan.FromSeconds(hit.Duration);
        var (genre, mood, energy, bpm) = InferMetadataFromTags(hit.Tags ?? "");

        return new MusicAsset(
            AssetId: hit.Id.ToString(),
            Title: hit.Tags?.Split(',').FirstOrDefault()?.Trim() ?? $"Track {hit.Id}",
            Artist: hit.User ?? "Pixabay Artist",
            Album: null,
            FilePath: hit.Videos?.Medium?.Url ?? "",
            PreviewUrl: hit.Videos?.Small?.Url ?? hit.Videos?.Medium?.Url,
            Duration: duration,
            LicenseType: LicenseType.PublicDomain,
            LicenseUrl: "https://pixabay.com/service/license/",
            CommercialUseAllowed: true,
            AttributionRequired: false,
            AttributionText: null,
            SourcePlatform: "Pixabay",
            CreatorProfileUrl: $"https://pixabay.com/users/{hit.User}/",
            Genre: genre,
            Mood: mood,
            Energy: energy,
            BPM: bpm,
            Tags: (hit.Tags ?? "").Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList(),
            Metadata: new Dictionary<string, object>
            {
                ["PixabayId"] = hit.Id,
                ["Downloads"] = hit.Downloads,
                ["Views"] = hit.Views,
                ["Likes"] = hit.Likes
            }
        );
    }

    private (MusicGenre Genre, MusicMood Mood, EnergyLevel Energy, int BPM) InferMetadataFromTags(string tags)
    {
        var tagSet = new HashSet<string>(
            tags.Split(',').Select(t => t.Trim().ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase
        );

        var genre = MusicGenre.Ambient;
        var mood = MusicMood.Neutral;
        var energy = EnergyLevel.Medium;
        var bpm = 100;

        // Genre inference
        if (tagSet.Overlaps(new[] { "electronic", "synth", "techno", "edm" }))
        {
            genre = MusicGenre.Electronic;
            bpm = 128;
        }
        else if (tagSet.Overlaps(new[] { "classical", "orchestra", "piano" }))
        {
            genre = MusicGenre.Classical;
            bpm = 90;
        }
        else if (tagSet.Overlaps(new[] { "rock", "guitar", "drums" }))
        {
            genre = MusicGenre.Rock;
            bpm = 120;
        }
        else if (tagSet.Overlaps(new[] { "jazz", "saxophone", "blues" }))
        {
            genre = MusicGenre.Jazz;
            bpm = 110;
        }
        else if (tagSet.Overlaps(new[] { "corporate", "business", "motivational" }))
        {
            genre = MusicGenre.Corporate;
            bpm = 115;
        }
        else if (tagSet.Overlaps(new[] { "cinematic", "epic", "film" }))
        {
            genre = MusicGenre.Cinematic;
            bpm = 100;
        }

        // Mood inference
        if (tagSet.Overlaps(new[] { "happy", "joyful", "cheerful" }))
        {
            mood = MusicMood.Happy;
            energy = EnergyLevel.High;
        }
        else if (tagSet.Overlaps(new[] { "sad", "melancholic", "emotional" }))
        {
            mood = MusicMood.Melancholic;
            energy = EnergyLevel.Low;
        }
        else if (tagSet.Overlaps(new[] { "calm", "peaceful", "relaxing" }))
        {
            mood = MusicMood.Calm;
            energy = EnergyLevel.VeryLow;
        }
        else if (tagSet.Overlaps(new[] { "epic", "dramatic", "intense" }))
        {
            mood = MusicMood.Epic;
            energy = EnergyLevel.VeryHigh;
        }
        else if (tagSet.Overlaps(new[] { "uplifting", "inspiring", "positive" }))
        {
            mood = MusicMood.Uplifting;
            energy = EnergyLevel.High;
        }

        return (genre, mood, energy, bpm);
    }

    private SearchResult<MusicAsset> GetMockResults(MusicSearchCriteria criteria)
    {
        var mockTracks = new List<MusicAsset>
        {
            CreateMockAsset("pixabay_001", "Upbeat Corporate", MusicGenre.Corporate, MusicMood.Uplifting, EnergyLevel.High, 125),
            CreateMockAsset("pixabay_002", "Calm Ambient", MusicGenre.Ambient, MusicMood.Calm, EnergyLevel.Low, 80),
            CreateMockAsset("pixabay_003", "Epic Cinematic", MusicGenre.Cinematic, MusicMood.Epic, EnergyLevel.VeryHigh, 110),
            CreateMockAsset("pixabay_004", "Happy Pop", MusicGenre.Pop, MusicMood.Happy, EnergyLevel.High, 120),
            CreateMockAsset("pixabay_005", "Relaxing Piano", MusicGenre.Classical, MusicMood.Calm, EnergyLevel.VeryLow, 70)
        };

        // Apply filters
        var filtered = mockTracks.AsEnumerable();

        if (criteria.Mood.HasValue)
            filtered = filtered.Where(t => t.Mood == criteria.Mood.Value);

        if (criteria.Genre.HasValue)
            filtered = filtered.Where(t => t.Genre == criteria.Genre.Value);

        if (criteria.Energy.HasValue)
            filtered = filtered.Where(t => t.Energy == criteria.Energy.Value);

        var results = filtered.Take(criteria.PageSize).ToList();

        return new SearchResult<MusicAsset>(
            Results: results,
            TotalCount: results.Count,
            Page: 1,
            PageSize: criteria.PageSize,
            TotalPages: 1
        );
    }

    private MusicAsset? GetMockAsset(string assetId)
    {
        var mockResults = GetMockResults(new MusicSearchCriteria());
        return mockResults.Results.FirstOrDefault(a => a.AssetId == assetId);
    }

    private MusicAsset CreateMockAsset(
        string id,
        string title,
        MusicGenre genre,
        MusicMood mood,
        EnergyLevel energy,
        int bpm)
    {
        return new MusicAsset(
            AssetId: id,
            Title: title,
            Artist: "Pixabay Artist",
            Album: null,
            FilePath: $"/mock/{id}.mp3",
            PreviewUrl: $"/mock/{id}_preview.mp3",
            Duration: TimeSpan.FromMinutes(3),
            LicenseType: LicenseType.PublicDomain,
            LicenseUrl: "https://pixabay.com/service/license/",
            CommercialUseAllowed: true,
            AttributionRequired: false,
            AttributionText: null,
            SourcePlatform: "Pixabay",
            CreatorProfileUrl: null,
            Genre: genre,
            Mood: mood,
            Energy: energy,
            BPM: bpm,
            Tags: new List<string> { genre.ToString(), mood.ToString() },
            Metadata: null
        );
    }

    private async Task CreatePlaceholderAudioAsync(string path, CancellationToken ct)
    {
        // Create a minimal WAV file as placeholder
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        // WAV header for 1 second of silence at 44100 Hz, 16-bit, mono
        var sampleRate = 44100;
        var bitsPerSample = 16;
        var channels = 1;
        var dataSize = sampleRate * (bitsPerSample / 8) * channels * 1; // 1 second

        // RIFF header
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + dataSize);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        // fmt chunk
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * (bitsPerSample / 8));
        writer.Write((short)(channels * (bitsPerSample / 8)));
        writer.Write((short)bitsPerSample);

        // data chunk
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);

        // Write silence
        var silence = new byte[dataSize];
        await stream.WriteAsync(silence, ct).ConfigureAwait(false);
    }

    // Pixabay API response models
    private sealed record PixabayResponse(
        int Total,
        int TotalHits,
        List<PixabayHit>? Hits
    );

    private sealed record PixabayHit(
        int Id,
        string? Tags,
        string? User,
        int Duration,
        int Views,
        int Downloads,
        int Likes,
        PixabayVideos? Videos
    );

    private sealed record PixabayVideos(
        PixabayVideo? Large,
        PixabayVideo? Medium,
        PixabayVideo? Small,
        PixabayVideo? Tiny
    );

    private sealed record PixabayVideo(
        string? Url,
        int Width,
        int Height,
        int Size
    );
}
