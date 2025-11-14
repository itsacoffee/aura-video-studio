using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PerformanceAnalytics;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PerformanceAnalytics;

/// <summary>
/// Links published videos to Aura projects
/// </summary>
public class VideoProjectLinker
{
    private readonly ILogger<VideoProjectLinker> _logger;
    private readonly AnalyticsPersistence _persistence;
    private const double AUTO_LINK_THRESHOLD = 0.7;

    public VideoProjectLinker(
        ILogger<VideoProjectLinker> logger,
        AnalyticsPersistence persistence)
    {
        _logger = logger;
        _persistence = persistence;
    }

    /// <summary>
    /// Create a manual link between video and project
    /// </summary>
    public async Task<VideoProjectLink> CreateManualLinkAsync(
        string videoId,
        string projectId,
        string profileId,
        string linkedBy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating manual link: Video {VideoId} -> Project {ProjectId}", videoId, projectId);

        var link = new VideoProjectLink(
            LinkId: Guid.NewGuid().ToString(),
            VideoId: videoId,
            ProjectId: projectId,
            ProfileId: profileId,
            LinkType: "manual",
            ConfidenceScore: 1.0,
            LinkedAt: DateTime.UtcNow,
            LinkedBy: linkedBy,
            IsConfirmed: true,
            MatchingFactors: new Dictionary<string, object>
            {
                { "method", "manual" }
            }
        );

        await _persistence.SaveVideoLinkAsync(link, ct).ConfigureAwait(false);
        return link;
    }

    /// <summary>
    /// Attempt to automatically link a video to a project by title matching
    /// </summary>
    public async Task<VideoProjectLink?> AutoLinkByTitleAsync(
        VideoPerformanceData video,
        List<(string ProjectId, string Title)> availableProjects,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Attempting auto-link for video: {VideoTitle}", video.VideoTitle);

        var bestMatch = FindBestTitleMatch(video.VideoTitle, availableProjects);
        if (bestMatch == null || bestMatch.Value.Score < AUTO_LINK_THRESHOLD)
        {
            _logger.LogDebug("No confident match found (best score: {Score})", bestMatch?.Score ?? 0);
            return null;
        }

        _logger.LogInformation(
            "Auto-linking video '{VideoTitle}' to project '{ProjectTitle}' (confidence: {Score:P0})",
            video.VideoTitle, bestMatch.Value.Title, bestMatch.Value.Score);

        var link = new VideoProjectLink(
            LinkId: Guid.NewGuid().ToString(),
            VideoId: video.VideoId,
            ProjectId: bestMatch.Value.ProjectId,
            ProfileId: video.ProfileId,
            LinkType: "auto_title_match",
            ConfidenceScore: bestMatch.Value.Score,
            LinkedAt: DateTime.UtcNow,
            LinkedBy: null,
            IsConfirmed: false,
            MatchingFactors: new Dictionary<string, object>
            {
                { "method", "title_similarity" },
                { "video_title", video.VideoTitle },
                { "project_title", bestMatch.Value.Title },
                { "similarity_score", bestMatch.Value.Score }
            }
        );

        await _persistence.SaveVideoLinkAsync(link, ct).ConfigureAwait(false);
        return link;
    }

    /// <summary>
    /// Confirm an auto-linked match
    /// </summary>
    public async Task<VideoProjectLink?> ConfirmLinkAsync(
        string linkId,
        string profileId,
        bool isCorrect,
        CancellationToken ct = default)
    {
        var links = await _persistence.LoadLinksAsync(profileId, ct).ConfigureAwait(false);
        var link = links.FirstOrDefault(l => l.LinkId == linkId);

        if (link == null)
        {
            return null;
        }

        if (!isCorrect)
        {
            // If user says it's wrong, we could delete the link
            _logger.LogInformation("User rejected auto-link {LinkId}", linkId);
            return null;
        }

        // Update link to confirmed
        var confirmedLink = link with { IsConfirmed = true };
        await _persistence.SaveVideoLinkAsync(confirmedLink, ct).ConfigureAwait(false);

        return confirmedLink;
    }

    /// <summary>
    /// Find best title match using simple string similarity
    /// </summary>
    private (string ProjectId, string Title, double Score)? FindBestTitleMatch(
        string videoTitle,
        List<(string ProjectId, string Title)> projects)
    {
        var videoTitleNorm = NormalizeTitle(videoTitle);
        
        var matches = projects
            .Select(p => new
            {
                p.ProjectId,
                p.Title,
                Score = CalculateTitleSimilarity(videoTitleNorm, NormalizeTitle(p.Title))
            })
            .Where(m => m.Score > 0)
            .OrderByDescending(m => m.Score)
            .FirstOrDefault();

        if (matches == null)
        {
            return null;
        }

        return (matches.ProjectId, matches.Title, matches.Score);
    }

    /// <summary>
    /// Normalize title for comparison
    /// </summary>
    private string NormalizeTitle(string title)
    {
        return title
            .ToLowerInvariant()
            .Replace("-", " ")
            .Replace("_", " ")
            .Trim();
    }

    /// <summary>
    /// Calculate similarity between two titles
    /// Uses simple word overlap - could be enhanced with fuzzy matching
    /// </summary>
    private double CalculateTitleSimilarity(string title1, string title2)
    {
        var words1 = title1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = title2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        if (words1.Count == 0 || words2.Count == 0)
        {
            return 0;
        }

        // Calculate Jaccard similarity
        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        var jaccardSimilarity = union > 0 ? (double)intersection / union : 0;

        // Bonus for exact match
        if (title1 == title2)
        {
            return 1.0;
        }

        // Bonus for one contains the other
        if (title1.Contains(title2) || title2.Contains(title1))
        {
            return Math.Max(jaccardSimilarity, 0.8);
        }

        return jaccardSimilarity;
    }

    /// <summary>
    /// Get all unlinked videos for a profile
    /// </summary>
    public async Task<List<VideoPerformanceData>> GetUnlinkedVideosAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var allVideos = await _persistence.LoadAllVideosAsync(profileId, ct).ConfigureAwait(false);
        var links = await _persistence.LoadLinksAsync(profileId, ct).ConfigureAwait(false);
        var linkedVideoIds = links.Select(l => l.VideoId).ToHashSet();

        return allVideos.Where(v => !linkedVideoIds.Contains(v.VideoId)).ToList();
    }

    /// <summary>
    /// Get all linked videos for a profile
    /// </summary>
    public async Task<List<(VideoPerformanceData Video, VideoProjectLink Link)>> GetLinkedVideosAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var allVideos = await _persistence.LoadAllVideosAsync(profileId, ct).ConfigureAwait(false);
        var links = await _persistence.LoadLinksAsync(profileId, ct).ConfigureAwait(false);

        var result = new List<(VideoPerformanceData, VideoProjectLink)>();

        foreach (var link in links)
        {
            var video = allVideos.FirstOrDefault(v => v.VideoId == link.VideoId);
            if (video != null)
            {
                result.Add((video, link));
            }
        }

        return result;
    }
}
