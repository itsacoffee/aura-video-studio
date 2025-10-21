using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.EditingIntelligence;
using Aura.Core.Models.Timeline;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.EditingIntelligence;

/// <summary>
/// Service for automated quality control checks
/// </summary>
public class QualityControlService
{
    private readonly ILogger<QualityControlService> _logger;

    public QualityControlService(ILogger<QualityControlService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Run comprehensive quality control checks on timeline
    /// </summary>
    public async Task<IReadOnlyList<QualityIssue>> RunQualityChecksAsync(EditableTimeline timeline)
    {
        _logger.LogInformation("Running quality control checks");
        var issues = new List<QualityIssue>();

        // Check for missing assets
        issues.AddRange(await CheckMissingAssetsAsync(timeline));

        // Check for technical quality issues
        issues.AddRange(await CheckTechnicalQualityAsync(timeline));

        // Check for continuity errors
        issues.AddRange(CheckContinuityErrors(timeline));

        // Check for black frames or gaps
        issues.AddRange(CheckTimelineGaps(timeline));

        _logger.LogInformation("Found {Count} quality issues", issues.Count);
        return issues;
    }

    private async Task<List<QualityIssue>> CheckMissingAssetsAsync(EditableTimeline timeline)
    {
        var issues = new List<QualityIssue>();

        foreach (var scene in timeline.Scenes)
        {
            // Check narration audio
            if (!string.IsNullOrEmpty(scene.NarrationAudioPath) && !File.Exists(scene.NarrationAudioPath))
            {
                issues.Add(new QualityIssue(
                    Type: QualityIssueType.MissingAsset,
                    Severity: QualityIssueSeverity.Critical,
                    Location: scene.Start,
                    Description: $"Missing narration audio for scene {scene.Index}: {scene.NarrationAudioPath}",
                    FixSuggestion: "Regenerate narration audio or remove reference"
                ));
            }

            // Check visual assets
            if (scene.VisualAssets != null)
            {
                foreach (var asset in scene.VisualAssets)
                {
                    if (!File.Exists(asset.FilePath))
                    {
                        issues.Add(new QualityIssue(
                            Type: QualityIssueType.MissingAsset,
                            Severity: QualityIssueSeverity.Error,
                            Location: scene.Start + asset.Start,
                            Description: $"Missing visual asset: {asset.FilePath}",
                            FixSuggestion: "Replace with available asset or remove from timeline"
                        ));
                    }
                }
            }
        }

        // Check background music
        if (!string.IsNullOrEmpty(timeline.BackgroundMusicPath) && !File.Exists(timeline.BackgroundMusicPath))
        {
            issues.Add(new QualityIssue(
                Type: QualityIssueType.MissingAsset,
                Severity: QualityIssueSeverity.Warning,
                Location: null,
                Description: $"Missing background music: {timeline.BackgroundMusicPath}",
                FixSuggestion: "Select different music track or remove background music"
            ));
        }

        await Task.CompletedTask;
        return issues;
    }

    private async Task<List<QualityIssue>> CheckTechnicalQualityAsync(EditableTimeline timeline)
    {
        var issues = new List<QualityIssue>();

        foreach (var scene in timeline.Scenes)
        {
            if (scene.VisualAssets == null)
                continue;

            foreach (var asset in scene.VisualAssets)
            {
                // Check for very small or very large assets (potential resolution issues)
                if (asset.Position.Width < 10 || asset.Position.Height < 10)
                {
                    issues.Add(new QualityIssue(
                        Type: QualityIssueType.LowResolution,
                        Severity: QualityIssueSeverity.Warning,
                        Location: scene.Start + asset.Start,
                        Description: "Asset appears very small, may appear pixelated",
                        FixSuggestion: "Use higher resolution source or increase size"
                    ));
                }

                // Check for extreme opacity
                if (asset.Opacity < 0.3)
                {
                    issues.Add(new QualityIssue(
                        Type: QualityIssueType.ColorInconsistency,
                        Severity: QualityIssueSeverity.Info,
                        Location: scene.Start + asset.Start,
                        Description: "Asset has very low opacity and may be barely visible",
                        FixSuggestion: "Increase opacity or remove asset"
                    ));
                }

                // Check for excessive effects
                if (asset.Effects != null)
                {
                    if (asset.Effects.Brightness < 0.3 || asset.Effects.Brightness > 2.0)
                    {
                        issues.Add(new QualityIssue(
                            Type: QualityIssueType.ColorInconsistency,
                            Severity: QualityIssueSeverity.Warning,
                            Location: scene.Start + asset.Start,
                            Description: $"Extreme brightness setting ({asset.Effects.Brightness:F1})",
                            FixSuggestion: "Adjust brightness to reasonable range (0.5-1.5)"
                        ));
                    }

                    if (asset.Effects.Contrast < 0.5 || asset.Effects.Contrast > 2.0)
                    {
                        issues.Add(new QualityIssue(
                            Type: QualityIssueType.ColorInconsistency,
                            Severity: QualityIssueSeverity.Warning,
                            Location: scene.Start + asset.Start,
                            Description: $"Extreme contrast setting ({asset.Effects.Contrast:F1})",
                            FixSuggestion: "Adjust contrast to reasonable range (0.7-1.3)"
                        ));
                    }
                }
            }
        }

        await Task.CompletedTask;
        return issues;
    }

    private List<QualityIssue> CheckContinuityErrors(EditableTimeline timeline)
    {
        var issues = new List<QualityIssue>();

        // Check for sudden visual style changes
        for (int i = 0; i < timeline.Scenes.Count - 1; i++)
        {
            var currentScene = timeline.Scenes[i];
            var nextScene = timeline.Scenes[i + 1];

            // Check for color/effect inconsistency between scenes
            if (currentScene.VisualAssets?.Any() == true && nextScene.VisualAssets?.Any() == true)
            {
                var currentAvgBrightness = currentScene.VisualAssets
                    .Average(a => a.Effects?.Brightness ?? 1.0);
                var nextAvgBrightness = nextScene.VisualAssets
                    .Average(a => a.Effects?.Brightness ?? 1.0);

                if (Math.Abs(currentAvgBrightness - nextAvgBrightness) > 0.5)
                {
                    issues.Add(new QualityIssue(
                        Type: QualityIssueType.ContinuityError,
                        Severity: QualityIssueSeverity.Warning,
                        Location: nextScene.Start,
                        Description: "Sudden brightness change between scenes may be jarring",
                        FixSuggestion: "Match color grading between adjacent scenes or add transition"
                    ));
                }
            }
        }

        // Check for audio level consistency (would need actual audio analysis)
        // Placeholder for future implementation
        if (timeline.Scenes.Any(s => !string.IsNullOrEmpty(s.NarrationAudioPath)))
        {
            issues.Add(new QualityIssue(
                Type: QualityIssueType.AudioClipping,
                Severity: QualityIssueSeverity.Info,
                Location: null,
                Description: "Recommend audio level normalization across all scenes",
                FixSuggestion: "Apply audio normalization in final render"
            ));
        }

        return issues;
    }

    private List<QualityIssue> CheckTimelineGaps(EditableTimeline timeline)
    {
        var issues = new List<QualityIssue>();

        // Check for gaps between scenes
        for (int i = 0; i < timeline.Scenes.Count - 1; i++)
        {
            var currentScene = timeline.Scenes[i];
            var nextScene = timeline.Scenes[i + 1];

            var currentEnd = currentScene.Start + currentScene.Duration;
            var gap = nextScene.Start - currentEnd;

            if (gap > TimeSpan.FromMilliseconds(100)) // Allow small rounding differences
            {
                issues.Add(new QualityIssue(
                    Type: QualityIssueType.BlackFrame,
                    Severity: QualityIssueSeverity.Warning,
                    Location: currentEnd,
                    Description: $"Gap of {gap.TotalSeconds:F2}s between scenes (potential black frame)",
                    FixSuggestion: "Adjust scene timing to eliminate gap or add intentional transition"
                ));
            }
        }

        // Check for scenes with no content
        foreach (var scene in timeline.Scenes)
        {
            var hasNarration = !string.IsNullOrEmpty(scene.NarrationAudioPath);
            var hasVisuals = scene.VisualAssets?.Any() == true;

            if (!hasNarration && !hasVisuals)
            {
                issues.Add(new QualityIssue(
                    Type: QualityIssueType.MissingAsset,
                    Severity: QualityIssueSeverity.Error,
                    Location: scene.Start,
                    Description: $"Scene {scene.Index} has no narration or visual content",
                    FixSuggestion: "Add content to scene or remove from timeline"
                ));
            }
        }

        return issues;
    }

    /// <summary>
    /// Detect audio/video desynchronization issues
    /// </summary>
    public async Task<IReadOnlyList<QualityIssue>> DetectDesyncIssuesAsync(EditableTimeline timeline)
    {
        var issues = new List<QualityIssue>();

        foreach (var scene in timeline.Scenes)
        {
            // Check if visual duration matches narration (would need actual audio analysis)
            if (!string.IsNullOrEmpty(scene.NarrationAudioPath))
            {
                // Placeholder: In real implementation, would analyze audio file duration
                var hasVisuals = scene.VisualAssets?.Any() == true;
                if (hasVisuals)
                {
                    var visualDuration = scene.VisualAssets!
                        .Max(a => a.Start + a.Duration);

                    if (Math.Abs((visualDuration - scene.Duration).TotalSeconds) > 1.0)
                    {
                        issues.Add(new QualityIssue(
                            Type: QualityIssueType.AudioDesync,
                            Severity: QualityIssueSeverity.Warning,
                            Location: scene.Start,
                            Description: "Visual assets may not match narration duration",
                            FixSuggestion: "Adjust visual asset timing to match narration"
                        ));
                    }
                }
            }
        }

        await Task.CompletedTask;
        return issues;
    }
}
