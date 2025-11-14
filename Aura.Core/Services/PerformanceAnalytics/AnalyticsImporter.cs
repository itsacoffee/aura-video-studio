using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PerformanceAnalytics;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PerformanceAnalytics;

/// <summary>
/// Imports analytics data from CSV/JSON files and normalizes to common format
/// </summary>
public class AnalyticsImporter
{
    private readonly ILogger<AnalyticsImporter> _logger;
    private readonly AnalyticsPersistence _persistence;

    public AnalyticsImporter(
        ILogger<AnalyticsImporter> logger,
        AnalyticsPersistence persistence)
    {
        _logger = logger;
        _persistence = persistence;
    }

    /// <summary>
    /// Import analytics from CSV file
    /// </summary>
    public async Task<AnalyticsImport> ImportFromCsvAsync(
        string profileId,
        string platform,
        string filePath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Importing analytics from CSV: {FilePath}", filePath);

        var videos = new List<VideoPerformanceData>();

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null
        }))
        {
            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                try
                {
                    var video = ParseCsvRecord(profileId, platform, record);
                    if (video != null)
                    {
                        videos.Add(video);
                        await _persistence.SaveVideoPerformanceAsync(video, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse CSV record");
                }
            }
        }

        var import = new AnalyticsImport(
            ImportId: Guid.NewGuid().ToString(),
            ProfileId: profileId,
            Platform: platform,
            ImportType: "manual_csv",
            ImportedAt: DateTime.UtcNow,
            VideosImported: videos.Count,
            ImportedBy: null,
            FilePath: filePath,
            Metadata: new Dictionary<string, object>
            {
                { "total_records", videos.Count }
            }
        );

        _logger.LogInformation("Imported {Count} videos from CSV", videos.Count);
        return import;
    }

    /// <summary>
    /// Import analytics from JSON file
    /// </summary>
    public async Task<AnalyticsImport> ImportFromJsonAsync(
        string profileId,
        string platform,
        string filePath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Importing analytics from JSON: {FilePath}", filePath);

        var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        var videos = new List<VideoPerformanceData>();

        // Handle different JSON structures based on platform
        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                try
                {
                    var video = ParseJsonRecord(profileId, platform, item);
                    if (video != null)
                    {
                        videos.Add(video);
                        await _persistence.SaveVideoPerformanceAsync(video, ct).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse JSON record");
                }
            }
        }
        else if (data.ValueKind == JsonValueKind.Object)
        {
            // Check if there's a videos array in the object
            if (data.TryGetProperty("videos", out var videosArray))
            {
                foreach (var item in videosArray.EnumerateArray())
                {
                    try
                    {
                        var video = ParseJsonRecord(profileId, platform, item);
                        if (video != null)
                        {
                            videos.Add(video);
                            await _persistence.SaveVideoPerformanceAsync(video, ct).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse JSON record");
                    }
                }
            }
        }

        var import = new AnalyticsImport(
            ImportId: Guid.NewGuid().ToString(),
            ProfileId: profileId,
            Platform: platform,
            ImportType: "manual_json",
            ImportedAt: DateTime.UtcNow,
            VideosImported: videos.Count,
            ImportedBy: null,
            FilePath: filePath,
            Metadata: new Dictionary<string, object>
            {
                { "total_records", videos.Count }
            }
        );

        _logger.LogInformation("Imported {Count} videos from JSON", videos.Count);
        return import;
    }

    /// <summary>
    /// Parse CSV record to video performance data
    /// </summary>
    private VideoPerformanceData? ParseCsvRecord(string profileId, string platform, dynamic record)
    {
        var recordDict = (IDictionary<string, object>)record;

        // Try to extract common fields with various possible column names
        var videoId = GetFieldValue(recordDict, "video_id", "id", "video id");
        var title = GetFieldValue(recordDict, "title", "video_title", "name");
        var views = GetNumericValue(recordDict, "views", "view_count");
        var likes = GetNumericValue(recordDict, "likes", "like_count");
        var comments = GetNumericValue(recordDict, "comments", "comment_count");
        var shares = GetNumericValue(recordDict, "shares", "share_count");
        var publishedAt = GetDateValue(recordDict, "published_at", "publish_date", "date");

        if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(title))
        {
            return null;
        }

        var engagementRate = views > 0 ? (double)(likes + comments + shares) / views : 0;

        return new VideoPerformanceData(
            VideoId: videoId,
            ProfileId: profileId,
            ProjectId: null,
            Platform: platform,
            VideoTitle: title,
            VideoUrl: GetFieldValue(recordDict, "url", "video_url"),
            PublishedAt: publishedAt ?? DateTime.UtcNow,
            DataCollectedAt: DateTime.UtcNow,
            Metrics: new PerformanceMetrics(
                Views: views,
                WatchTimeMinutes: GetNumericValue(recordDict, "watch_time_minutes", "watch_time"),
                AverageViewDuration: GetDoubleValue(recordDict, "avg_view_duration", "average_view_duration"),
                AverageViewPercentage: GetDoubleValue(recordDict, "avg_view_percentage", "average_percentage_viewed"),
                Engagement: new EngagementMetrics(
                    Likes: likes,
                    Dislikes: GetNumericValue(recordDict, "dislikes", "dislike_count"),
                    Comments: comments,
                    Shares: shares,
                    EngagementRate: engagementRate
                ),
                ClickThroughRate: GetDoubleValue(recordDict, "ctr", "click_through_rate"),
                Traffic: null,
                RetentionCurve: null,
                Devices: null
            ),
            Audience: null,
            RawData: new Dictionary<string, object>(recordDict)
        );
    }

    /// <summary>
    /// Parse JSON record to video performance data
    /// </summary>
    private VideoPerformanceData? ParseJsonRecord(string profileId, string platform, JsonElement record)
    {
        var videoId = GetJsonStringValue(record, "video_id", "id", "videoId");
        var title = GetJsonStringValue(record, "title", "video_title", "name");
        var views = GetJsonLongValue(record, "views", "view_count", "viewCount");
        var likes = GetJsonLongValue(record, "likes", "like_count", "likeCount");
        var comments = GetJsonLongValue(record, "comments", "comment_count", "commentCount");
        var shares = GetJsonLongValue(record, "shares", "share_count", "shareCount");

        if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(title))
        {
            return null;
        }

        var engagementRate = views > 0 ? (double)(likes + comments + shares) / views : 0;

        return new VideoPerformanceData(
            VideoId: videoId,
            ProfileId: profileId,
            ProjectId: null,
            Platform: platform,
            VideoTitle: title,
            VideoUrl: GetJsonStringValue(record, "url", "video_url", "videoUrl"),
            PublishedAt: GetJsonDateValue(record, "published_at", "publishedAt", "publish_date") ?? DateTime.UtcNow,
            DataCollectedAt: DateTime.UtcNow,
            Metrics: new PerformanceMetrics(
                Views: views,
                WatchTimeMinutes: GetJsonLongValue(record, "watch_time_minutes", "watchTimeMinutes"),
                AverageViewDuration: GetJsonDoubleValue(record, "avg_view_duration", "averageViewDuration"),
                AverageViewPercentage: GetJsonDoubleValue(record, "avg_view_percentage", "averageViewPercentage"),
                Engagement: new EngagementMetrics(
                    Likes: likes,
                    Dislikes: GetJsonLongValue(record, "dislikes", "dislike_count"),
                    Comments: comments,
                    Shares: shares,
                    EngagementRate: engagementRate
                ),
                ClickThroughRate: GetJsonDoubleValue(record, "ctr", "click_through_rate"),
                Traffic: null,
                RetentionCurve: null,
                Devices: null
            ),
            Audience: null,
            RawData: null
        );
    }

    #region Helper Methods

    private string? GetFieldValue(IDictionary<string, object> dict, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                return value.ToString();
            }
        }
        return null;
    }

    private long GetNumericValue(IDictionary<string, object> dict, params string[] possibleKeys)
    {
        var value = GetFieldValue(dict, possibleKeys);
        if (long.TryParse(value, out var result))
        {
            return result;
        }
        return 0;
    }

    private double? GetDoubleValue(IDictionary<string, object> dict, params string[] possibleKeys)
    {
        var value = GetFieldValue(dict, possibleKeys);
        if (double.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }

    private DateTime? GetDateValue(IDictionary<string, object> dict, params string[] possibleKeys)
    {
        var value = GetFieldValue(dict, possibleKeys);
        if (DateTime.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }

    private string? GetJsonStringValue(JsonElement element, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }
        return null;
    }

    private long GetJsonLongValue(JsonElement element, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (element.TryGetProperty(key, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var result))
                {
                    return result;
                }
                if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out var strResult))
                {
                    return strResult;
                }
            }
        }
        return 0;
    }

    private double? GetJsonDoubleValue(JsonElement element, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (element.TryGetProperty(key, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var result))
                {
                    return result;
                }
                if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var strResult))
                {
                    return strResult;
                }
            }
        }
        return null;
    }

    private DateTime? GetJsonDateValue(JsonElement element, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(value.GetString(), out var result))
                {
                    return result;
                }
            }
        }
        return null;
    }

    #endregion
}
