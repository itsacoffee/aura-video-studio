using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Orchestration;

/// <summary>
/// Validates video composition for timeline correctness, gaps, overlaps, and missing media
/// </summary>
public class CompositionValidator
{
    private readonly ILogger<CompositionValidator> _logger;
    private const double ToleranceSeconds = 0.05; // 50ms tolerance for floating point precision

    public CompositionValidator(ILogger<CompositionValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates a complete timeline composition
    /// </summary>
    public CompositionValidationResult ValidateComposition(
        IReadOnlyList<Scene> scenes,
        IReadOnlyDictionary<int, IReadOnlyList<Aura.Core.Models.Asset>> sceneAssets,
        string? narrationPath = null,
        string? musicPath = null)
    {
        _logger.LogInformation("Validating composition with {SceneCount} scenes", scenes.Count);

        var result = new CompositionValidationResult
        {
            IsValid = true,
            Errors = new List<CompositionError>()
        };

        if (scenes == null || scenes.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add(new CompositionError
            {
                Code = CompositionErrorCode.EMPTY_TIMELINE,
                Severity = ErrorSeverity.Critical,
                Message = "Timeline contains no scenes"
            });
            return result;
        }

        // Validate scene timing continuity
        ValidateSceneContinuity(scenes, result);

        // Validate no overlaps
        ValidateNoOverlaps(scenes, result);

        // Validate gaps
        ValidateNoGaps(scenes, result);

        // Validate total duration
        ValidateTotalDuration(scenes, result);

        // Validate media files exist
        ValidateMediaFiles(scenes, sceneAssets, narrationPath, musicPath, result);

        // Validate scene durations are reasonable
        ValidateSceneDurations(scenes, result);

        _logger.LogInformation(
            "Composition validation complete: {Status}, {ErrorCount} errors, {WarningCount} warnings",
            result.IsValid ? "VALID" : "INVALID",
            result.Errors.Count(e => e.Severity >= ErrorSeverity.Error),
            result.Errors.Count(e => e.Severity == ErrorSeverity.Warning));

        return result;
    }

    private void ValidateSceneContinuity(IReadOnlyList<Scene> scenes, CompositionValidationResult result)
    {
        var sortedScenes = scenes.OrderBy(s => s.Start).ToList();

        for (int i = 0; i < sortedScenes.Count; i++)
        {
            var scene = sortedScenes[i];

            // Validate scene has positive duration
            if (scene.Duration <= TimeSpan.Zero)
            {
                result.IsValid = false;
                result.Errors.Add(new CompositionError
                {
                    Code = CompositionErrorCode.INVALID_DURATION,
                    Severity = ErrorSeverity.Error,
                    Message = $"Scene {scene.Index} has invalid duration: {scene.Duration}",
                    SceneIndex = scene.Index,
                    Timestamp = scene.Start
                });
            }

            // Validate scene end doesn't exceed expected
            var expectedEnd = scene.Start + scene.Duration;
            if (i < sortedScenes.Count - 1)
            {
                var nextScene = sortedScenes[i + 1];
                if (expectedEnd > nextScene.Start + TimeSpan.FromSeconds(ToleranceSeconds))
                {
                    result.IsValid = false;
                    result.Errors.Add(new CompositionError
                    {
                        Code = CompositionErrorCode.OVERLAP,
                        Severity = ErrorSeverity.Error,
                        Message = $"Scene {scene.Index} overlaps with scene {nextScene.Index} by {(expectedEnd - nextScene.Start).TotalSeconds:F3}s",
                        SceneIndex = scene.Index,
                        Timestamp = scene.Start,
                        Details = new Dictionary<string, object>
                        {
                            ["sceneEnd"] = expectedEnd,
                            ["nextSceneStart"] = nextScene.Start,
                            ["overlapDuration"] = (expectedEnd - nextScene.Start).TotalSeconds
                        }
                    });
                }
            }
        }
    }

    private void ValidateNoOverlaps(IReadOnlyList<Scene> scenes, CompositionValidationResult result)
    {
        var sortedScenes = scenes.OrderBy(s => s.Start).ToList();

        for (int i = 0; i < sortedScenes.Count - 1; i++)
        {
            var currentScene = sortedScenes[i];
            var nextScene = sortedScenes[i + 1];

            var currentEnd = currentScene.Start + currentScene.Duration;
            var overlap = currentEnd - nextScene.Start;

            if (overlap > TimeSpan.FromSeconds(ToleranceSeconds))
            {
                result.IsValid = false;
                result.Errors.Add(new CompositionError
                {
                    Code = CompositionErrorCode.OVERLAP,
                    Severity = ErrorSeverity.Error,
                    Message = $"Overlap detected between scenes {currentScene.Index} and {nextScene.Index}: {overlap.TotalSeconds:F3}s",
                    SceneIndex = currentScene.Index,
                    Timestamp = currentEnd,
                    Details = new Dictionary<string, object>
                    {
                        ["overlapStart"] = currentEnd,
                        ["overlapEnd"] = nextScene.Start,
                        ["overlapDuration"] = overlap.TotalSeconds
                    }
                });
            }
        }
    }

    private void ValidateNoGaps(IReadOnlyList<Scene> scenes, CompositionValidationResult result)
    {
        var sortedScenes = scenes.OrderBy(s => s.Start).ToList();

        // First scene should start at or near zero
        if (sortedScenes[0].Start > TimeSpan.FromSeconds(ToleranceSeconds))
        {
            result.Errors.Add(new CompositionError
            {
                Code = CompositionErrorCode.GAP_DETECTED,
                Severity = ErrorSeverity.Warning,
                Message = $"Timeline starts at {sortedScenes[0].Start.TotalSeconds:F3}s instead of 0s",
                SceneIndex = sortedScenes[0].Index,
                Timestamp = TimeSpan.Zero,
                Details = new Dictionary<string, object>
                {
                    ["gapDuration"] = sortedScenes[0].Start.TotalSeconds
                }
            });
        }

        // Check for gaps between scenes
        for (int i = 0; i < sortedScenes.Count - 1; i++)
        {
            var currentScene = sortedScenes[i];
            var nextScene = sortedScenes[i + 1];

            var currentEnd = currentScene.Start + currentScene.Duration;
            var gap = nextScene.Start - currentEnd;

            if (gap > TimeSpan.FromSeconds(ToleranceSeconds))
            {
                result.IsValid = false;
                result.Errors.Add(new CompositionError
                {
                    Code = CompositionErrorCode.GAP_DETECTED,
                    Severity = ErrorSeverity.Error,
                    Message = $"Gap detected between scenes {currentScene.Index} and {nextScene.Index}: {gap.TotalSeconds:F3}s",
                    SceneIndex = currentScene.Index,
                    Timestamp = currentEnd,
                    Details = new Dictionary<string, object>
                    {
                        ["gapStart"] = currentEnd,
                        ["gapEnd"] = nextScene.Start,
                        ["gapDuration"] = gap.TotalSeconds
                    }
                });
            }
        }
    }

    private void ValidateTotalDuration(IReadOnlyList<Scene> scenes, CompositionValidationResult result)
    {
        var lastScene = scenes.MaxBy(s => s.Start + s.Duration);
        if (lastScene != null)
        {
            var totalDuration = lastScene.Start + lastScene.Duration;

            if (totalDuration <= TimeSpan.Zero)
            {
                result.IsValid = false;
                result.Errors.Add(new CompositionError
                {
                    Code = CompositionErrorCode.INVALID_DURATION,
                    Severity = ErrorSeverity.Critical,
                    Message = "Total composition duration is zero or negative"
                });
            }
            else if (totalDuration < TimeSpan.FromSeconds(1))
            {
                result.Errors.Add(new CompositionError
                {
                    Code = CompositionErrorCode.INVALID_DURATION,
                    Severity = ErrorSeverity.Warning,
                    Message = $"Total composition duration is very short: {totalDuration.TotalSeconds:F2}s"
                });
            }
            else if (totalDuration > TimeSpan.FromHours(2))
            {
                result.Errors.Add(new CompositionError
                {
                    Code = CompositionErrorCode.INVALID_DURATION,
                    Severity = ErrorSeverity.Warning,
                    Message = $"Total composition duration is very long: {totalDuration.TotalHours:F2}h"
                });
            }
        }
    }

    private void ValidateMediaFiles(
        IReadOnlyList<Scene> scenes,
        IReadOnlyDictionary<int, IReadOnlyList<Aura.Core.Models.Asset>>? sceneAssets,
        string? narrationPath,
        string? musicPath,
        CompositionValidationResult result)
    {
        // Validate narration file
        if (!string.IsNullOrEmpty(narrationPath))
        {
            if (!File.Exists(narrationPath))
            {
                result.IsValid = false;
                result.Errors.Add(new CompositionError
                {
                    Code = CompositionErrorCode.MISSING_MEDIA,
                    Severity = ErrorSeverity.Critical,
                    Message = $"Narration file not found: {narrationPath}",
                    Details = new Dictionary<string, object> { ["path"] = narrationPath }
                });
            }
        }
        else
        {
            result.Errors.Add(new CompositionError
            {
                Code = CompositionErrorCode.MISSING_MEDIA,
                Severity = ErrorSeverity.Warning,
                Message = "No narration file specified"
            });
        }

        // Validate music file if specified
        if (!string.IsNullOrEmpty(musicPath) && !File.Exists(musicPath))
        {
            result.Errors.Add(new CompositionError
            {
                Code = CompositionErrorCode.MISSING_MEDIA,
                Severity = ErrorSeverity.Warning,
                Message = $"Music file not found: {musicPath}",
                Details = new Dictionary<string, object> { ["path"] = musicPath }
            });
        }

        // Validate scene assets if provided
        if (sceneAssets != null)
        {
            foreach (var scene in scenes)
            {
                if (sceneAssets.TryGetValue(scene.Index, out var assets))
                {
                    foreach (var asset in assets)
                    {
                        if (!string.IsNullOrEmpty(asset.PathOrUrl) && !File.Exists(asset.PathOrUrl))
                        {
                            result.Errors.Add(new CompositionError
                            {
                                Code = CompositionErrorCode.MISSING_MEDIA,
                                Severity = ErrorSeverity.Warning,
                                Message = $"Asset file not found for scene {scene.Index}: {asset.PathOrUrl}",
                                SceneIndex = scene.Index,
                                Details = new Dictionary<string, object> { ["path"] = asset.PathOrUrl }
                            });
                        }
                    }
                }
            }
        }
    }

    private void ValidateSceneDurations(IReadOnlyList<Scene> scenes, CompositionValidationResult result)
    {
        foreach (var scene in scenes)
        {
            // Warn if scene is too short (< 0.5 seconds)
            if (scene.Duration < TimeSpan.FromSeconds(0.5))
            {
                result.Errors.Add(new CompositionError
                {
                    Code = CompositionErrorCode.INVALID_DURATION,
                    Severity = ErrorSeverity.Warning,
                    Message = $"Scene {scene.Index} has very short duration: {scene.Duration.TotalSeconds:F2}s",
                    SceneIndex = scene.Index,
                    Timestamp = scene.Start
                });
            }

            // Warn if scene is too long (> 2 minutes)
            if (scene.Duration > TimeSpan.FromMinutes(2))
            {
                result.Errors.Add(new CompositionError
                {
                    Code = CompositionErrorCode.INVALID_DURATION,
                    Severity = ErrorSeverity.Warning,
                    Message = $"Scene {scene.Index} has very long duration: {scene.Duration.TotalMinutes:F2}m",
                    SceneIndex = scene.Index,
                    Timestamp = scene.Start
                });
            }
        }
    }
}

/// <summary>
/// Result of composition validation
/// </summary>
public class CompositionValidationResult
{
    public bool IsValid { get; set; }
    public List<CompositionError> Errors { get; set; } = new();

    public bool HasCriticalErrors => Errors.Any(e => e.Severity == ErrorSeverity.Critical);
    public bool HasErrors => Errors.Any(e => e.Severity >= ErrorSeverity.Error);
    public bool HasWarnings => Errors.Any(e => e.Severity == ErrorSeverity.Warning);

    public int CriticalCount => Errors.Count(e => e.Severity == ErrorSeverity.Critical);
    public int ErrorCount => Errors.Count(e => e.Severity == ErrorSeverity.Error);
    public int WarningCount => Errors.Count(e => e.Severity == ErrorSeverity.Warning);
}

/// <summary>
/// A composition validation error or warning
/// </summary>
public class CompositionError
{
    public CompositionErrorCode Code { get; set; }
    public ErrorSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? SceneIndex { get; set; }
    public TimeSpan? Timestamp { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Error codes for composition validation
/// </summary>
public enum CompositionErrorCode
{
    GAP_DETECTED,
    OVERLAP,
    MISSING_MEDIA,
    INVALID_DURATION,
    EMPTY_TIMELINE,
    DISCONTINUOUS_TIMELINE
}

/// <summary>
/// Severity levels for validation errors
/// </summary>
public enum ErrorSeverity
{
    Warning = 1,
    Error = 2,
    Critical = 3
}
