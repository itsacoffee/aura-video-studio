using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Assets;

/// <summary>
/// Tracks asset usage in timelines
/// </summary>
public class AssetUsageTracker
{
    private readonly ILogger<AssetUsageTracker> _logger;
    private readonly Dictionary<Guid, List<TimelineUsage>> _usageMap = new();

    public AssetUsageTracker(ILogger<AssetUsageTracker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Record asset usage in a timeline
    /// </summary>
    public Task RecordUsageAsync(Guid assetId, string timelineId, TimeSpan position)
    {
        _logger.LogInformation("Recording usage of asset {AssetId} in timeline {TimelineId}", assetId, timelineId);

        if (!_usageMap.TryGetValue(assetId, out var value))
        {
            value = new List<TimelineUsage>();
            _usageMap[assetId] = value;
        }

        var usage = new TimelineUsage(timelineId, position, DateTime.UtcNow);
        value.Add(usage);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Get timeline usage for an asset
    /// </summary>
    public Task<List<TimelineUsage>> GetTimelineUsageAsync(Guid assetId)
    {
        _usageMap.TryGetValue(assetId, out var usages);
        return Task.FromResult(usages ?? new List<TimelineUsage>());
    }

    /// <summary>
    /// Get asset references to check if safe to delete
    /// </summary>
    public Task<List<string>> GetAssetReferencesAsync(Guid assetId)
    {
        var timelines = _usageMap.TryGetValue(assetId, out var usages)
            ? usages.Select(u => u.TimelineId).Distinct().ToList()
            : new List<string>();

        return Task.FromResult(timelines);
    }

    /// <summary>
    /// Remove usage records for a timeline
    /// </summary>
    public Task RemoveTimelineUsageAsync(string timelineId)
    {
        foreach (var assetId in _usageMap.Keys.ToList())
        {
            _usageMap[assetId] = _usageMap[assetId]
                .Where(u => u.TimelineId != timelineId)
                .ToList();

            if (_usageMap[assetId].Count == 0)
            {
                _usageMap.Remove(assetId);
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Timeline usage record
/// </summary>
public record TimelineUsage(
    string TimelineId,
    TimeSpan Position,
    DateTime UsedAt);
