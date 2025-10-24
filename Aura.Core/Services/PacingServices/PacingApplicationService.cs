using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Models.Settings;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PacingServices;

/// <summary>
/// Service responsible for validating and applying pacing suggestions to video scenes
/// Ensures timeline constraints are respected and maintains narrative coherence
/// </summary>
public class PacingApplicationService
{
    private readonly ILogger<PacingApplicationService> _logger;

    // Configuration constants
    private const double MinSceneDurationSeconds = 3.0;
    private const double MaxSceneDurationSeconds = 120.0;
    private const double TotalDurationTolerancePercent = 0.15; // 15% tolerance

    public PacingApplicationService(ILogger<PacingApplicationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates pacing suggestions against timeline constraints
    /// </summary>
    public PacingValidationResult ValidateSuggestions(
        PacingAnalysisResult analysisResult,
        IReadOnlyList<Scene> currentScenes,
        TimeSpan? targetDuration = null,
        double minimumConfidenceThreshold = 70.0)
    {
        var issues = new List<string>();
        var warnings = new List<string>();

        // Check confidence threshold
        if (analysisResult.ConfidenceScore < minimumConfidenceThreshold)
        {
            issues.Add($"Analysis confidence score {analysisResult.ConfidenceScore:F1}% is below threshold {minimumConfidenceThreshold:F1}%");
        }

        // Validate each scene suggestion
        foreach (var suggestion in analysisResult.TimingSuggestions)
        {
            if (suggestion.SceneIndex < 0 || suggestion.SceneIndex >= currentScenes.Count)
            {
                issues.Add($"Scene index {suggestion.SceneIndex} is out of range (0-{currentScenes.Count - 1})");
                continue;
            }

            // Check duration bounds
            if (suggestion.OptimalDuration.TotalSeconds < MinSceneDurationSeconds)
            {
                warnings.Add($"Scene {suggestion.SceneIndex} optimal duration {suggestion.OptimalDuration.TotalSeconds:F1}s is below minimum {MinSceneDurationSeconds}s");
            }

            if (suggestion.OptimalDuration.TotalSeconds > MaxSceneDurationSeconds)
            {
                warnings.Add($"Scene {suggestion.SceneIndex} optimal duration {suggestion.OptimalDuration.TotalSeconds:F1}s exceeds maximum {MaxSceneDurationSeconds}s");
            }

            // Check min/max range consistency
            if (suggestion.MinDuration > suggestion.OptimalDuration || suggestion.OptimalDuration > suggestion.MaxDuration)
            {
                issues.Add($"Scene {suggestion.SceneIndex} has inconsistent duration range: min={suggestion.MinDuration}, optimal={suggestion.OptimalDuration}, max={suggestion.MaxDuration}");
            }
        }

        // Check total duration if target is specified
        if (targetDuration.HasValue)
        {
            var totalOptimal = TimeSpan.FromSeconds(
                analysisResult.TimingSuggestions.Sum(s => s.OptimalDuration.TotalSeconds));
            
            var difference = Math.Abs((totalOptimal - targetDuration.Value).TotalSeconds);
            var tolerance = targetDuration.Value.TotalSeconds * TotalDurationTolerancePercent;

            if (difference > tolerance)
            {
                warnings.Add($"Total optimal duration {totalOptimal} differs from target {targetDuration.Value} by {difference:F1}s (tolerance: {tolerance:F1}s)");
            }
        }

        var isValid = issues.Count == 0;
        
        _logger.LogInformation(
            "Pacing validation complete: {IsValid}, {IssueCount} issues, {WarningCount} warnings",
            isValid ? "VALID" : "INVALID", issues.Count, warnings.Count);

        return new PacingValidationResult(isValid, issues, warnings);
    }

    /// <summary>
    /// Applies pacing suggestions to scenes with constraint enforcement
    /// Returns updated scenes with new durations and timing information
    /// </summary>
    public IReadOnlyList<Scene> ApplySuggestions(
        PacingAnalysisResult analysisResult,
        IReadOnlyList<Scene> currentScenes,
        PacingApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(analysisResult);
        ArgumentNullException.ThrowIfNull(currentScenes);
        ArgumentNullException.ThrowIfNull(options);

        _logger.LogInformation(
            "Applying pacing suggestions to {SceneCount} scenes with optimization level: {Level}",
            currentScenes.Count, options.OptimizationLevel);

        var updatedScenes = new List<Scene>();
        var changeLogs = new List<string>();

        // Apply suggestions scene by scene
        for (int i = 0; i < currentScenes.Count; i++)
        {
            var scene = currentScenes[i];
            var suggestion = analysisResult.TimingSuggestions.FirstOrDefault(s => s.SceneIndex == i);

            if (suggestion == null)
            {
                _logger.LogWarning("No pacing suggestion found for scene {SceneIndex}, keeping current duration", i);
                updatedScenes.Add(scene);
                continue;
            }

            // Determine new duration based on optimization level
            var newDuration = CalculateAppliedDuration(
                scene.Duration, 
                suggestion, 
                options.OptimizationLevel,
                options.MinimumConfidenceThreshold);

            // Enforce absolute constraints
            newDuration = EnforceConstraints(newDuration);

            // Log change if significant
            if (Math.Abs((newDuration - scene.Duration).TotalSeconds) > 0.5)
            {
                var change = $"Scene {i}: {scene.Duration.TotalSeconds:F1}s → {newDuration.TotalSeconds:F1}s " +
                            $"(optimal: {suggestion.OptimalDuration.TotalSeconds:F1}s, confidence: {suggestion.Confidence:F1}%)";
                changeLogs.Add(change);
                _logger.LogDebug(change);
            }

            updatedScenes.Add(scene with { Duration = newDuration });
        }

        // Recalculate start times to maintain sequential timeline
        var finalScenes = RecalculateStartTimes(updatedScenes);

        // Adjust proportionally if total duration must match target
        if (options.TargetDuration.HasValue)
        {
            finalScenes = AdjustToTargetDuration(finalScenes, options.TargetDuration.Value);
        }

        _logger.LogInformation(
            "Applied {ChangeCount} duration changes. Total duration: {TotalDuration}",
            changeLogs.Count,
            TimeSpan.FromSeconds(finalScenes.Sum(s => s.Duration.TotalSeconds)));

        foreach (var log in changeLogs)
        {
            _logger.LogInformation(log);
        }

        return finalScenes;
    }

    /// <summary>
    /// Calculates the applied duration based on optimization level and confidence
    /// </summary>
    private TimeSpan CalculateAppliedDuration(
        TimeSpan currentDuration,
        SceneTimingSuggestion suggestion,
        OptimizationLevel level,
        double minimumConfidence)
    {
        // If suggestion confidence is too low, keep current duration
        if (suggestion.Confidence < minimumConfidence)
        {
            return currentDuration;
        }

        return level switch
        {
            OptimizationLevel.Conservative => 
                // Blend 70% current, 30% optimal
                TimeSpan.FromSeconds(currentDuration.TotalSeconds * 0.7 + suggestion.OptimalDuration.TotalSeconds * 0.3),
            
            OptimizationLevel.Balanced => 
                // Blend 40% current, 60% optimal
                TimeSpan.FromSeconds(currentDuration.TotalSeconds * 0.4 + suggestion.OptimalDuration.TotalSeconds * 0.6),
            
            OptimizationLevel.Aggressive => 
                // Use optimal duration directly
                suggestion.OptimalDuration,
            
            _ => currentDuration
        };
    }

    /// <summary>
    /// Enforces hard constraints on scene duration
    /// </summary>
    private TimeSpan EnforceConstraints(TimeSpan duration)
    {
        var seconds = duration.TotalSeconds;
        seconds = Math.Clamp(seconds, MinSceneDurationSeconds, MaxSceneDurationSeconds);
        return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Recalculates scene start times based on durations
    /// </summary>
    private List<Scene> RecalculateStartTimes(List<Scene> scenes)
    {
        var result = new List<Scene>();
        var currentStart = TimeSpan.Zero;

        foreach (var scene in scenes)
        {
            result.Add(scene with { Start = currentStart });
            currentStart += scene.Duration;
        }

        return result;
    }

    /// <summary>
    /// Adjusts scene durations proportionally to match target total duration
    /// </summary>
    private List<Scene> AdjustToTargetDuration(List<Scene> scenes, TimeSpan targetDuration)
    {
        var currentTotal = scenes.Sum(s => s.Duration.TotalSeconds);
        
        if (Math.Abs(currentTotal - targetDuration.TotalSeconds) < 1.0)
        {
            // Already close enough
            return scenes;
        }

        var scaleFactor = targetDuration.TotalSeconds / currentTotal;
        
        _logger.LogInformation(
            "Adjusting total duration from {Current}s to {Target}s (scale: {Scale:F3})",
            currentTotal, targetDuration.TotalSeconds, scaleFactor);

        var adjustedScenes = scenes
            .Select(s => s with { Duration = TimeSpan.FromSeconds(s.Duration.TotalSeconds * scaleFactor) })
            .Select(s => s with { Duration = EnforceConstraints(s.Duration) })
            .ToList();

        return RecalculateStartTimes(adjustedScenes);
    }
}

/// <summary>
/// Options for pacing application
/// </summary>
public record PacingApplicationOptions(
    OptimizationLevel OptimizationLevel = OptimizationLevel.Balanced,
    double MinimumConfidenceThreshold = 70.0,
    TimeSpan? TargetDuration = null,
    bool AutoApply = true);

/// <summary>
/// Result of pacing validation
/// </summary>
public record PacingValidationResult(
    bool IsValid,
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> Warnings);
