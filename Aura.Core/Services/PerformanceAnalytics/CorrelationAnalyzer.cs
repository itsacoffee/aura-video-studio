using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PerformanceAnalytics;
using Aura.Core.Models.Profiles;
using Aura.Core.Services.Profiles;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PerformanceAnalytics;

/// <summary>
/// Analyzes correlations between AI decisions and video performance
/// </summary>
public class CorrelationAnalyzer
{
    private readonly ILogger<CorrelationAnalyzer> _logger;
    private readonly AnalyticsPersistence _persistence;
    private readonly ProfileService _profileService;

    public CorrelationAnalyzer(
        ILogger<CorrelationAnalyzer> logger,
        AnalyticsPersistence persistence,
        ProfileService profileService)
    {
        _logger = logger;
        _persistence = persistence;
        _profileService = profileService;
    }

    /// <summary>
    /// Analyze correlations for a project
    /// </summary>
    public async Task<List<DecisionPerformanceCorrelation>> AnalyzeProjectCorrelationsAsync(
        string projectId,
        string profileId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing correlations for project {ProjectId}", projectId);

        // Find the video linked to this project
        var link = await _persistence.FindLinkByProjectAsync(profileId, projectId, ct);
        if (link == null)
        {
            _logger.LogWarning("No video linked to project {ProjectId}", projectId);
            return new List<DecisionPerformanceCorrelation>();
        }

        // Load video performance data
        var video = await _persistence.LoadVideoPerformanceAsync(profileId, link.VideoId, ct);
        if (video == null)
        {
            _logger.LogWarning("No performance data for video {VideoId}", link.VideoId);
            return new List<DecisionPerformanceCorrelation>();
        }

        // Load profile preferences to identify decisions made
        var preferences = await _profileService.GetPreferencesAsync(profileId, ct);
        if (preferences == null)
        {
            return new List<DecisionPerformanceCorrelation>();
        }

        // Calculate performance outcome
        var outcome = CalculatePerformanceOutcome(video, profileId);

        // Analyze correlations for different decision types
        var correlations = new List<DecisionPerformanceCorrelation>();

        // Tone decisions
        if (preferences.Tone != null)
        {
            correlations.AddRange(AnalyzeToneCorrelations(projectId, link.VideoId, preferences.Tone, outcome));
        }

        // Visual decisions
        if (preferences.Visual != null)
        {
            correlations.AddRange(AnalyzeVisualCorrelations(projectId, link.VideoId, preferences.Visual, outcome));
        }

        // Audio decisions
        if (preferences.Audio != null)
        {
            correlations.AddRange(AnalyzeAudioCorrelations(projectId, link.VideoId, preferences.Audio, outcome));
        }

        // Editing decisions
        if (preferences.Editing != null)
        {
            correlations.AddRange(AnalyzeEditingCorrelations(projectId, link.VideoId, preferences.Editing, outcome));
        }

        // Platform decisions
        if (preferences.Platform != null)
        {
            correlations.AddRange(AnalyzePlatformCorrelations(projectId, link.VideoId, preferences.Platform, outcome));
        }

        // Save correlations
        await _persistence.SaveCorrelationsAsync(projectId, correlations, ct);

        _logger.LogInformation("Analyzed {Count} correlations for project {ProjectId}", correlations.Count, projectId);
        return correlations;
    }

    /// <summary>
    /// Calculate performance outcome for a video
    /// </summary>
    private PerformanceOutcome CalculatePerformanceOutcome(VideoPerformanceData video, string profileId)
    {
        // Calculate composite performance score (0-100)
        var scores = new Dictionary<string, double>();

        // Normalize views (arbitrary scale, adjust based on typical performance)
        var viewScore = Math.Min(100, (video.Metrics.Views / 1000.0) * 10);
        scores["views"] = viewScore;

        // Engagement rate score
        var engagementScore = Math.Min(100, video.Metrics.Engagement.EngagementRate * 10000);
        scores["engagement"] = engagementScore;

        // Retention score (if available)
        if (video.Metrics.AverageViewPercentage.HasValue)
        {
            scores["retention"] = video.Metrics.AverageViewPercentage.Value * 100;
        }

        // Click-through rate score (if available)
        if (video.Metrics.ClickThroughRate.HasValue)
        {
            scores["ctr"] = video.Metrics.ClickThroughRate.Value * 1000;
        }

        // Calculate composite score (weighted average)
        var compositeScore = scores.Values.Average();

        // Categorize outcome
        var outcomeType = compositeScore switch
        {
            >= 80 => "high_success",
            >= 60 => "success",
            >= 40 => "average",
            >= 20 => "below_average",
            _ => "failure"
        };

        return new PerformanceOutcome(
            OutcomeType: outcomeType,
            PerformanceScore: compositeScore,
            ComparedTo: "absolute_scale",
            MetricScores: scores
        );
    }

    /// <summary>
    /// Analyze tone preference correlations
    /// </summary>
    private List<DecisionPerformanceCorrelation> AnalyzeToneCorrelations(
        string projectId,
        string videoId,
        TonePreferences tone,
        PerformanceOutcome outcome)
    {
        var correlations = new List<DecisionPerformanceCorrelation>();

        // Formality correlation
        correlations.Add(new DecisionPerformanceCorrelation(
            CorrelationId: Guid.NewGuid().ToString(),
            ProjectId: projectId,
            VideoId: videoId,
            DecisionType: "tone_formality",
            DecisionValue: tone.Formality.ToString(),
            Outcome: outcome,
            CorrelationStrength: CalculateCorrelationStrength(outcome),
            StatisticalSignificance: 0.05, // Placeholder - would need more data for real calculation
            AnalyzedAt: DateTime.UtcNow,
            DecisionContext: new Dictionary<string, object>
            {
                { "formality_level", tone.Formality }
            }
        ));

        // Energy correlation
        correlations.Add(new DecisionPerformanceCorrelation(
            CorrelationId: Guid.NewGuid().ToString(),
            ProjectId: projectId,
            VideoId: videoId,
            DecisionType: "tone_energy",
            DecisionValue: tone.Energy.ToString(),
            Outcome: outcome,
            CorrelationStrength: CalculateCorrelationStrength(outcome),
            StatisticalSignificance: 0.05,
            AnalyzedAt: DateTime.UtcNow,
            DecisionContext: new Dictionary<string, object>
            {
                { "energy_level", tone.Energy }
            }
        ));

        return correlations;
    }

    /// <summary>
    /// Analyze visual preference correlations
    /// </summary>
    private List<DecisionPerformanceCorrelation> AnalyzeVisualCorrelations(
        string projectId,
        string videoId,
        VisualPreferences visual,
        PerformanceOutcome outcome)
    {
        var correlations = new List<DecisionPerformanceCorrelation>();

        if (!string.IsNullOrEmpty(visual.Aesthetic))
        {
            correlations.Add(new DecisionPerformanceCorrelation(
                CorrelationId: Guid.NewGuid().ToString(),
                ProjectId: projectId,
                VideoId: videoId,
                DecisionType: "visual_aesthetic",
                DecisionValue: visual.Aesthetic,
                Outcome: outcome,
                CorrelationStrength: CalculateCorrelationStrength(outcome),
                StatisticalSignificance: 0.05,
                AnalyzedAt: DateTime.UtcNow,
                DecisionContext: new Dictionary<string, object>
                {
                    { "aesthetic_style", visual.Aesthetic }
                }
            ));
        }

        if (!string.IsNullOrEmpty(visual.PacingPreference))
        {
            correlations.Add(new DecisionPerformanceCorrelation(
                CorrelationId: Guid.NewGuid().ToString(),
                ProjectId: projectId,
                VideoId: videoId,
                DecisionType: "visual_pacing",
                DecisionValue: visual.PacingPreference,
                Outcome: outcome,
                CorrelationStrength: CalculateCorrelationStrength(outcome),
                StatisticalSignificance: 0.05,
                AnalyzedAt: DateTime.UtcNow,
                DecisionContext: new Dictionary<string, object>
                {
                    { "pacing_style", visual.PacingPreference }
                }
            ));
        }

        return correlations;
    }

    /// <summary>
    /// Analyze audio preference correlations
    /// </summary>
    private List<DecisionPerformanceCorrelation> AnalyzeAudioCorrelations(
        string projectId,
        string videoId,
        AudioPreferences audio,
        PerformanceOutcome outcome)
    {
        var correlations = new List<DecisionPerformanceCorrelation>();

        correlations.Add(new DecisionPerformanceCorrelation(
            CorrelationId: Guid.NewGuid().ToString(),
            ProjectId: projectId,
            VideoId: videoId,
            DecisionType: "audio_energy",
            DecisionValue: audio.MusicEnergy.ToString(),
            Outcome: outcome,
            CorrelationStrength: CalculateCorrelationStrength(outcome),
            StatisticalSignificance: 0.05,
            AnalyzedAt: DateTime.UtcNow,
            DecisionContext: new Dictionary<string, object>
            {
                { "music_energy", audio.MusicEnergy }
            }
        ));

        return correlations;
    }

    /// <summary>
    /// Analyze editing preference correlations
    /// </summary>
    private List<DecisionPerformanceCorrelation> AnalyzeEditingCorrelations(
        string projectId,
        string videoId,
        EditingPreferences editing,
        PerformanceOutcome outcome)
    {
        var correlations = new List<DecisionPerformanceCorrelation>();

        correlations.Add(new DecisionPerformanceCorrelation(
            CorrelationId: Guid.NewGuid().ToString(),
            ProjectId: projectId,
            VideoId: videoId,
            DecisionType: "editing_pacing",
            DecisionValue: editing.Pacing.ToString(),
            Outcome: outcome,
            CorrelationStrength: CalculateCorrelationStrength(outcome),
            StatisticalSignificance: 0.05,
            AnalyzedAt: DateTime.UtcNow,
            DecisionContext: new Dictionary<string, object>
            {
                { "pacing_level", editing.Pacing }
            }
        ));

        correlations.Add(new DecisionPerformanceCorrelation(
            CorrelationId: Guid.NewGuid().ToString(),
            ProjectId: projectId,
            VideoId: videoId,
            DecisionType: "editing_cut_frequency",
            DecisionValue: editing.CutFrequency.ToString(),
            Outcome: outcome,
            CorrelationStrength: CalculateCorrelationStrength(outcome),
            StatisticalSignificance: 0.05,
            AnalyzedAt: DateTime.UtcNow,
            DecisionContext: new Dictionary<string, object>
            {
                { "cut_frequency", editing.CutFrequency }
            }
        ));

        return correlations;
    }

    /// <summary>
    /// Analyze platform preference correlations
    /// </summary>
    private List<DecisionPerformanceCorrelation> AnalyzePlatformCorrelations(
        string projectId,
        string videoId,
        PlatformPreferences platform,
        PerformanceOutcome outcome)
    {
        var correlations = new List<DecisionPerformanceCorrelation>();

        if (!string.IsNullOrEmpty(platform.AspectRatio))
        {
            correlations.Add(new DecisionPerformanceCorrelation(
                CorrelationId: Guid.NewGuid().ToString(),
                ProjectId: projectId,
                VideoId: videoId,
                DecisionType: "platform_aspect_ratio",
                DecisionValue: platform.AspectRatio,
                Outcome: outcome,
                CorrelationStrength: CalculateCorrelationStrength(outcome),
                StatisticalSignificance: 0.05,
                AnalyzedAt: DateTime.UtcNow,
                DecisionContext: new Dictionary<string, object>
                {
                    { "aspect_ratio", platform.AspectRatio }
                }
            ));
        }

        return correlations;
    }

    /// <summary>
    /// Calculate correlation strength based on outcome
    /// Simple heuristic - would need historical data for real calculation
    /// </summary>
    private double CalculateCorrelationStrength(PerformanceOutcome outcome)
    {
        return outcome.OutcomeType switch
        {
            "high_success" => 0.8,
            "success" => 0.5,
            "average" => 0.0,
            "below_average" => -0.5,
            "failure" => -0.8,
            _ => 0.0
        };
    }
}
