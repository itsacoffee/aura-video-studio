using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PerformanceAnalytics;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PerformanceAnalytics;

/// <summary>
/// Manages persistence of performance analytics data
/// </summary>
public class AnalyticsPersistence
{
    private readonly ILogger<AnalyticsPersistence> _logger;
    private readonly string _analyticsDirectory;
    private readonly string _videosDirectory;
    private readonly string _linksDirectory;
    private readonly string _correlationsDirectory;
    private readonly string _patternsDirectory;
    private readonly string _abTestsDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public AnalyticsPersistence(ILogger<AnalyticsPersistence> logger, string baseDirectory)
    {
        _logger = logger;
        _analyticsDirectory = Path.Combine(baseDirectory, "Analytics");
        _videosDirectory = Path.Combine(_analyticsDirectory, "Videos");
        _linksDirectory = Path.Combine(_analyticsDirectory, "Links");
        _correlationsDirectory = Path.Combine(_analyticsDirectory, "Correlations");
        _patternsDirectory = Path.Combine(_analyticsDirectory, "Patterns");
        _abTestsDirectory = Path.Combine(_analyticsDirectory, "ABTests");

        // Ensure directories exist
        Directory.CreateDirectory(_analyticsDirectory);
        Directory.CreateDirectory(_videosDirectory);
        Directory.CreateDirectory(_linksDirectory);
        Directory.CreateDirectory(_correlationsDirectory);
        Directory.CreateDirectory(_patternsDirectory);
        Directory.CreateDirectory(_abTestsDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #region Video Performance Operations

    /// <summary>
    /// Save video performance data
    /// </summary>
    public async Task SaveVideoPerformanceAsync(VideoPerformanceData video, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            var profileDir = Path.Combine(_videosDirectory, video.ProfileId);
            Directory.CreateDirectory(profileDir);

            var filePath = Path.Combine(profileDir, $"{video.VideoId}.json");
            var json = JsonSerializer.Serialize(video, _jsonOptions);

            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, filePath, overwrite: true);

            _logger.LogDebug("Saved video performance data for {VideoId}", video.VideoId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load video performance data
    /// </summary>
    public async Task<VideoPerformanceData?> LoadVideoPerformanceAsync(string profileId, string videoId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_videosDirectory, profileId, $"{videoId}.json");

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct);
            return JsonSerializer.Deserialize<VideoPerformanceData>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load video performance for {VideoId}", videoId);
            return null;
        }
    }

    /// <summary>
    /// Load all videos for a profile
    /// </summary>
    public async Task<List<VideoPerformanceData>> LoadAllVideosAsync(string profileId, CancellationToken ct = default)
    {
        var profileDir = Path.Combine(_videosDirectory, profileId);
        
        if (!Directory.Exists(profileDir))
        {
            return new List<VideoPerformanceData>();
        }

        var videos = new List<VideoPerformanceData>();
        var files = Directory.GetFiles(profileDir, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var video = JsonSerializer.Deserialize<VideoPerformanceData>(json, _jsonOptions);
                if (video != null)
                {
                    videos.Add(video);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load video from {FilePath}", file);
            }
        }

        return videos;
    }

    #endregion

    #region Video-Project Link Operations

    /// <summary>
    /// Save video-project link
    /// </summary>
    public async Task SaveVideoLinkAsync(VideoProjectLink link, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            var profileDir = Path.Combine(_linksDirectory, link.ProfileId);
            Directory.CreateDirectory(profileDir);

            var filePath = Path.Combine(profileDir, $"{link.LinkId}.json");
            var json = JsonSerializer.Serialize(link, _jsonOptions);

            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, filePath, overwrite: true);

            _logger.LogDebug("Saved video-project link {LinkId}", link.LinkId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load all links for a profile
    /// </summary>
    public async Task<List<VideoProjectLink>> LoadLinksAsync(string profileId, CancellationToken ct = default)
    {
        var profileDir = Path.Combine(_linksDirectory, profileId);
        
        if (!Directory.Exists(profileDir))
        {
            return new List<VideoProjectLink>();
        }

        var links = new List<VideoProjectLink>();
        var files = Directory.GetFiles(profileDir, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var link = JsonSerializer.Deserialize<VideoProjectLink>(json, _jsonOptions);
                if (link != null)
                {
                    links.Add(link);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load link from {FilePath}", file);
            }
        }

        return links;
    }

    /// <summary>
    /// Find link by video ID
    /// </summary>
    public async Task<VideoProjectLink?> FindLinkByVideoAsync(string profileId, string videoId, CancellationToken ct = default)
    {
        var links = await LoadLinksAsync(profileId, ct);
        return links.FirstOrDefault(l => l.VideoId == videoId);
    }

    /// <summary>
    /// Find link by project ID
    /// </summary>
    public async Task<VideoProjectLink?> FindLinkByProjectAsync(string profileId, string projectId, CancellationToken ct = default)
    {
        var links = await LoadLinksAsync(profileId, ct);
        return links.FirstOrDefault(l => l.ProjectId == projectId);
    }

    #endregion

    #region Correlation Operations

    /// <summary>
    /// Save correlations for a project
    /// </summary>
    public async Task SaveCorrelationsAsync(string projectId, List<DecisionPerformanceCorrelation> correlations, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            var filePath = Path.Combine(_correlationsDirectory, $"{projectId}.json");
            var json = JsonSerializer.Serialize(correlations, _jsonOptions);

            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, filePath, overwrite: true);

            _logger.LogDebug("Saved {Count} correlations for project {ProjectId}", correlations.Count, projectId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load correlations for a project
    /// </summary>
    public async Task<List<DecisionPerformanceCorrelation>> LoadCorrelationsAsync(string projectId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_correlationsDirectory, $"{projectId}.json");

        if (!File.Exists(filePath))
        {
            return new List<DecisionPerformanceCorrelation>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct);
            return JsonSerializer.Deserialize<List<DecisionPerformanceCorrelation>>(json, _jsonOptions) 
                ?? new List<DecisionPerformanceCorrelation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load correlations for project {ProjectId}", projectId);
            return new List<DecisionPerformanceCorrelation>();
        }
    }

    #endregion

    #region Pattern Operations

    /// <summary>
    /// Save success patterns for a profile
    /// </summary>
    public async Task SaveSuccessPatternsAsync(string profileId, List<SuccessPattern> patterns, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            var filePath = Path.Combine(_patternsDirectory, $"{profileId}_success.json");
            var json = JsonSerializer.Serialize(patterns, _jsonOptions);

            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, filePath, overwrite: true);

            _logger.LogDebug("Saved {Count} success patterns for profile {ProfileId}", patterns.Count, profileId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load success patterns for a profile
    /// </summary>
    public async Task<List<SuccessPattern>> LoadSuccessPatternsAsync(string profileId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_patternsDirectory, $"{profileId}_success.json");

        if (!File.Exists(filePath))
        {
            return new List<SuccessPattern>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct);
            return JsonSerializer.Deserialize<List<SuccessPattern>>(json, _jsonOptions) 
                ?? new List<SuccessPattern>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load success patterns for profile {ProfileId}", profileId);
            return new List<SuccessPattern>();
        }
    }

    /// <summary>
    /// Save failure patterns for a profile
    /// </summary>
    public async Task SaveFailurePatternsAsync(string profileId, List<FailurePattern> patterns, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            var filePath = Path.Combine(_patternsDirectory, $"{profileId}_failure.json");
            var json = JsonSerializer.Serialize(patterns, _jsonOptions);

            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, filePath, overwrite: true);

            _logger.LogDebug("Saved {Count} failure patterns for profile {ProfileId}", patterns.Count, profileId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load failure patterns for a profile
    /// </summary>
    public async Task<List<FailurePattern>> LoadFailurePatternsAsync(string profileId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_patternsDirectory, $"{profileId}_failure.json");

        if (!File.Exists(filePath))
        {
            return new List<FailurePattern>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct);
            return JsonSerializer.Deserialize<List<FailurePattern>>(json, _jsonOptions) 
                ?? new List<FailurePattern>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load failure patterns for profile {ProfileId}", profileId);
            return new List<FailurePattern>();
        }
    }

    #endregion

    #region A/B Test Operations

    /// <summary>
    /// Save A/B test
    /// </summary>
    public async Task SaveABTestAsync(ABTest test, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            var profileDir = Path.Combine(_abTestsDirectory, test.ProfileId);
            Directory.CreateDirectory(profileDir);

            var filePath = Path.Combine(profileDir, $"{test.TestId}.json");
            var json = JsonSerializer.Serialize(test, _jsonOptions);

            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, filePath, overwrite: true);

            _logger.LogDebug("Saved A/B test {TestId}", test.TestId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load A/B test
    /// </summary>
    public async Task<ABTest?> LoadABTestAsync(string profileId, string testId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_abTestsDirectory, profileId, $"{testId}.json");

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct);
            return JsonSerializer.Deserialize<ABTest>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load A/B test {TestId}", testId);
            return null;
        }
    }

    /// <summary>
    /// Load all A/B tests for a profile
    /// </summary>
    public async Task<List<ABTest>> LoadAllABTestsAsync(string profileId, CancellationToken ct = default)
    {
        var profileDir = Path.Combine(_abTestsDirectory, profileId);
        
        if (!Directory.Exists(profileDir))
        {
            return new List<ABTest>();
        }

        var tests = new List<ABTest>();
        var files = Directory.GetFiles(profileDir, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var test = JsonSerializer.Deserialize<ABTest>(json, _jsonOptions);
                if (test != null)
                {
                    tests.Add(test);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load A/B test from {FilePath}", file);
            }
        }

        return tests;
    }

    #endregion
}
