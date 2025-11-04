using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Music;

/// <summary>
/// Local stock music provider - uses pre-downloaded royalty-free music library
/// </summary>
public class LocalStockMusicProvider : IMusicProvider
{
    private readonly ILogger<LocalStockMusicProvider> _logger;
    private readonly string _musicLibraryPath;
    private List<MusicAsset>? _cachedLibrary;

    public string Name => "LocalStock";

    public LocalStockMusicProvider(
        ILogger<LocalStockMusicProvider> logger,
        string? musicLibraryPath = null)
    {
        _logger = logger;
        _musicLibraryPath = musicLibraryPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Aura", "Music");
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        var isAvailable = Directory.Exists(_musicLibraryPath);
        _logger.LogInformation("Local music library {Status} at {Path}",
            isAvailable ? "found" : "not found", _musicLibraryPath);
        return Task.FromResult(isAvailable);
    }

    public async Task<SearchResult<MusicAsset>> SearchAsync(
        MusicSearchCriteria criteria,
        CancellationToken ct = default)
    {
        await EnsureLibraryLoadedAsync(ct);

        var query = _cachedLibrary ?? new List<MusicAsset>();

        if (criteria.Mood.HasValue)
            query = query.Where(m => m.Mood == criteria.Mood.Value).ToList();

        if (criteria.Genre.HasValue)
            query = query.Where(m => m.Genre == criteria.Genre.Value).ToList();

        if (criteria.Energy.HasValue)
            query = query.Where(m => m.Energy == criteria.Energy.Value).ToList();

        if (criteria.MinBPM.HasValue)
            query = query.Where(m => m.BPM >= criteria.MinBPM.Value).ToList();

        if (criteria.MaxBPM.HasValue)
            query = query.Where(m => m.BPM <= criteria.MaxBPM.Value).ToList();

        if (criteria.MinDuration.HasValue)
            query = query.Where(m => m.Duration >= criteria.MinDuration.Value).ToList();

        if (criteria.MaxDuration.HasValue)
            query = query.Where(m => m.Duration <= criteria.MaxDuration.Value).ToList();

        if (criteria.CommercialUseOnly == true)
            query = query.Where(m => m.CommercialUseAllowed).ToList();

        if (criteria.NoAttributionRequired == true)
            query = query.Where(m => !m.AttributionRequired).ToList();

        if (!string.IsNullOrWhiteSpace(criteria.SearchQuery))
        {
            var searchLower = criteria.SearchQuery.ToLowerInvariant();
            query = query.Where(m =>
                m.Title.ToLowerInvariant().Contains(searchLower) ||
                (m.Artist?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                m.Tags.Any(t => t.ToLowerInvariant().Contains(searchLower))
            ).ToList();
        }

        if (criteria.Tags != null && criteria.Tags.Count > 0)
        {
            query = query.Where(m =>
                criteria.Tags.Any(tag => m.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            ).ToList();
        }

        var totalCount = query.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)criteria.PageSize);

        var results = query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();

        return new SearchResult<MusicAsset>(
            Results: results,
            TotalCount: totalCount,
            Page: criteria.Page,
            PageSize: criteria.PageSize,
            TotalPages: totalPages
        );
    }

    public async Task<MusicAsset?> GetByIdAsync(string assetId, CancellationToken ct = default)
    {
        await EnsureLibraryLoadedAsync(ct);
        return _cachedLibrary?.FirstOrDefault(m => m.AssetId == assetId);
    }

    public Task<string> DownloadAsync(string assetId, string destinationPath, CancellationToken ct = default)
    {
        var asset = _cachedLibrary?.FirstOrDefault(m => m.AssetId == assetId);
        if (asset == null)
            throw new FileNotFoundException($"Music asset not found: {assetId}");

        if (!File.Exists(asset.FilePath))
            throw new FileNotFoundException($"Music file not found: {asset.FilePath}");

        File.Copy(asset.FilePath, destinationPath, overwrite: true);
        _logger.LogInformation("Copied music file {AssetId} to {Destination}", assetId, destinationPath);

        return Task.FromResult(destinationPath);
    }

    public async Task<string?> GetPreviewUrlAsync(string assetId, CancellationToken ct = default)
    {
        var asset = await GetByIdAsync(assetId, ct);
        return asset?.PreviewUrl ?? asset?.FilePath;
    }

    private async Task EnsureLibraryLoadedAsync(CancellationToken ct)
    {
        if (_cachedLibrary != null)
            return;

        await Task.Run(() => LoadLibrary(), ct);
    }

    private void LoadLibrary()
    {
        _cachedLibrary = new List<MusicAsset>();

        if (!Directory.Exists(_musicLibraryPath))
        {
            _logger.LogWarning("Music library path does not exist: {Path}", _musicLibraryPath);
            _cachedLibrary = GetDefaultMockLibrary();
            return;
        }

        try
        {
            var musicFiles = Directory.GetFiles(_musicLibraryPath, "*.mp3", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(_musicLibraryPath, "*.wav", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(_musicLibraryPath, "*.ogg", SearchOption.AllDirectories));

            foreach (var filePath in musicFiles)
            {
                try
                {
                    var asset = CreateAssetFromFile(filePath);
                    _cachedLibrary.Add(asset);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process music file: {Path}", filePath);
                }
            }

            _logger.LogInformation("Loaded {Count} music tracks from local library", _cachedLibrary.Count);

            if (_cachedLibrary.Count == 0)
            {
                _logger.LogInformation("No music files found, using mock library");
                _cachedLibrary = GetDefaultMockLibrary();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading music library");
            _cachedLibrary = GetDefaultMockLibrary();
        }
    }

    private MusicAsset CreateAssetFromFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var fileInfo = new FileInfo(filePath);

        var assetId = $"local_{Path.GetFileName(filePath)}";
        var duration = TimeSpan.FromMinutes(3);

        var (genre, mood, energy, bpm) = InferMetadataFromFilename(fileName);

        return new MusicAsset(
            AssetId: assetId,
            Title: fileName,
            Artist: "Local Library",
            Album: null,
            FilePath: filePath,
            PreviewUrl: null,
            Duration: duration,
            LicenseType: LicenseType.PublicDomain,
            LicenseUrl: "https://creativecommons.org/publicdomain/zero/1.0/",
            CommercialUseAllowed: true,
            AttributionRequired: false,
            AttributionText: null,
            SourcePlatform: "Local",
            CreatorProfileUrl: null,
            Genre: genre,
            Mood: mood,
            Energy: energy,
            BPM: bpm,
            Tags: new List<string> { genre.ToString(), mood.ToString() },
            Metadata: new Dictionary<string, object>
            {
                ["FileSizeBytes"] = fileInfo.Length,
                ["AddedDate"] = fileInfo.CreationTimeUtc
            }
        );
    }

    private (MusicGenre Genre, MusicMood Mood, EnergyLevel Energy, int BPM) InferMetadataFromFilename(string filename)
    {
        var lower = filename.ToLowerInvariant();

        var genre = lower switch
        {
            _ when lower.Contains("corporate") => MusicGenre.Corporate,
            _ when lower.Contains("electronic") => MusicGenre.Electronic,
            _ when lower.Contains("ambient") => MusicGenre.Ambient,
            _ when lower.Contains("cinematic") => MusicGenre.Cinematic,
            _ when lower.Contains("rock") => MusicGenre.Rock,
            _ when lower.Contains("classical") => MusicGenre.Classical,
            _ when lower.Contains("orchestral") => MusicGenre.Orchestral,
            _ => MusicGenre.Corporate
        };

        var mood = lower switch
        {
            _ when lower.Contains("uplifting") || lower.Contains("motivational") => MusicMood.Uplifting,
            _ when lower.Contains("calm") || lower.Contains("peaceful") => MusicMood.Calm,
            _ when lower.Contains("energetic") || lower.Contains("upbeat") => MusicMood.Energetic,
            _ when lower.Contains("dramatic") || lower.Contains("epic") => MusicMood.Dramatic,
            _ when lower.Contains("happy") => MusicMood.Happy,
            _ when lower.Contains("sad") || lower.Contains("melancholic") => MusicMood.Melancholic,
            _ when lower.Contains("tense") => MusicMood.Tense,
            _ => MusicMood.Neutral
        };

        var energy = lower switch
        {
            _ when lower.Contains("high energy") || lower.Contains("intense") => EnergyLevel.VeryHigh,
            _ when lower.Contains("energetic") || lower.Contains("upbeat") => EnergyLevel.High,
            _ when lower.Contains("calm") || lower.Contains("low") => EnergyLevel.Low,
            _ when lower.Contains("very calm") || lower.Contains("ambient") => EnergyLevel.VeryLow,
            _ => EnergyLevel.Medium
        };

        var bpm = energy switch
        {
            EnergyLevel.VeryLow => 70,
            EnergyLevel.Low => 90,
            EnergyLevel.Medium => 110,
            EnergyLevel.High => 130,
            EnergyLevel.VeryHigh => 150,
            _ => 100
        };

        return (genre, mood, energy, bpm);
    }

    private List<MusicAsset> GetDefaultMockLibrary()
    {
        return new List<MusicAsset>
        {
            new("mock_upbeat_corporate", "Upbeat Corporate", "AudioLib", null,
                "/mock/upbeat_corporate.mp3", null, TimeSpan.FromMinutes(3),
                LicenseType.PublicDomain, "https://creativecommons.org/publicdomain/zero/1.0/",
                true, false, null, "Local", null,
                MusicGenre.Corporate, MusicMood.Uplifting, EnergyLevel.High, 128,
                new List<string> { "corporate", "uplifting", "motivational" }, null),

            new("mock_calm_ambient", "Calm Ambient", "AudioLib", null,
                "/mock/calm_ambient.mp3", null, TimeSpan.FromMinutes(4),
                LicenseType.PublicDomain, "https://creativecommons.org/publicdomain/zero/1.0/",
                true, false, null, "Local", null,
                MusicGenre.Ambient, MusicMood.Calm, EnergyLevel.Low, 80,
                new List<string> { "ambient", "calm", "relaxing" }, null),

            new("mock_energetic_electronic", "Energetic Electronic", "AudioLib", null,
                "/mock/energetic_electronic.mp3", null, TimeSpan.FromMinutes(2.5),
                LicenseType.PublicDomain, "https://creativecommons.org/publicdomain/zero/1.0/",
                true, false, null, "Local", null,
                MusicGenre.Electronic, MusicMood.Energetic, EnergyLevel.VeryHigh, 140,
                new List<string> { "electronic", "energetic", "upbeat" }, null),

            new("mock_epic_orchestral", "Epic Orchestral", "AudioLib", null,
                "/mock/epic_orchestral.mp3", null, TimeSpan.FromMinutes(3.5),
                LicenseType.PublicDomain, "https://creativecommons.org/publicdomain/zero/1.0/",
                true, false, null, "Local", null,
                MusicGenre.Orchestral, MusicMood.Epic, EnergyLevel.High, 110,
                new List<string> { "orchestral", "epic", "dramatic" }, null),

            new("mock_serious_cinematic", "Serious Cinematic", "AudioLib", null,
                "/mock/serious_cinematic.mp3", null, TimeSpan.FromMinutes(4.2),
                LicenseType.PublicDomain, "https://creativecommons.org/publicdomain/zero/1.0/",
                true, false, null, "Local", null,
                MusicGenre.Cinematic, MusicMood.Serious, EnergyLevel.Medium, 95,
                new List<string> { "cinematic", "serious", "thoughtful" }, null)
        };
    }
}
