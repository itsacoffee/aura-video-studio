using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Learning;
using Aura.Core.Models.Profiles;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Learning;

/// <summary>
/// Infers user preferences from decision patterns and behavior
/// </summary>
public class PreferenceInferenceEngine
{
    private readonly ILogger<PreferenceInferenceEngine> _logger;
    private const int MinimumDecisionsForInference = 8;

    public PreferenceInferenceEngine(ILogger<PreferenceInferenceEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detect implicit preferences from user actions
    /// </summary>
    public async Task<List<InferredPreference>> DetectImplicitPreferencesAsync(
        List<DecisionRecord> decisions,
        string profileId,
        ProfilePreferences? explicitPreferences = null,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var preferences = new List<InferredPreference>();
        
        // Infer tone preferences
        preferences.AddRange(InferTonePreferences(decisions, profileId, explicitPreferences?.Tone));
        
        // Infer visual preferences
        preferences.AddRange(InferVisualPreferences(decisions, profileId, explicitPreferences?.Visual));
        
        // Infer audio preferences
        preferences.AddRange(InferAudioPreferences(decisions, profileId, explicitPreferences?.Audio));
        
        // Infer editing preferences
        preferences.AddRange(InferEditingPreferences(decisions, profileId, explicitPreferences?.Editing));
        
        _logger.LogInformation("Inferred {Count} preferences for profile {ProfileId}",
            preferences.Count, profileId);
        
        return preferences;
    }

    /// <summary>
    /// Calculate confidence score for inferred preference
    /// </summary>
    public double CalculateConfidence(int basedOnDecisions, double consistencyRate)
    {
        // Confidence increases with more decisions and higher consistency
        var sampleSizeScore = Math.Min(1.0, basedOnDecisions / 30.0); // Plateaus at 30
        var consistencyScore = consistencyRate;
        
        var confidence = (sampleSizeScore * 0.4) + (consistencyScore * 0.6);
        
        return confidence;
    }

    /// <summary>
    /// Validate inferences against future decisions
    /// </summary>
    public async Task<Dictionary<string, double>> ValidatePreferencesAsync(
        List<InferredPreference> preferences,
        List<DecisionRecord> newDecisions,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var validationScores = new Dictionary<string, double>();
        
        foreach (var preference in preferences)
        {
            var relevantDecisions = newDecisions
                .Where(d => d.Timestamp > preference.InferredAt)
                .Where(d => IsRelevantToPreference(d, preference))
                .ToList();
            
            if (relevantDecisions.Count > 0)
            {
                var confirmingDecisions = relevantDecisions
                    .Count(d => ConfirmsPreference(d, preference));
                
                var accuracy = (double)confirmingDecisions / relevantDecisions.Count;
                validationScores[preference.PreferenceId] = accuracy;
            }
        }
        
        return validationScores;
    }

    /// <summary>
    /// Suggest preferences for explicit confirmation
    /// </summary>
    public async Task<List<LearningInsight>> SuggestPreferenceConfirmationsAsync(
        List<InferredPreference> preferences,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var suggestions = new List<LearningInsight>();
        
        // Suggest confirming high-confidence preferences
        var highConfidence = preferences
            .Where(p => p.Confidence >= 0.7 && !p.IsConfirmed)
            .OrderByDescending(p => p.Confidence)
            .Take(5)
            .ToList();
        
        foreach (var pref in highConfidence)
        {
            suggestions.Add(new LearningInsight(
                InsightId: Guid.NewGuid().ToString("N"),
                ProfileId: pref.ProfileId,
                InsightType: "preference",
                Category: pref.Category,
                Description: $"We've noticed you prefer {pref.PreferenceName}: {FormatPreferenceValue(pref.PreferenceValue)}",
                Confidence: pref.Confidence,
                DiscoveredAt: DateTime.UtcNow,
                IsActionable: true,
                SuggestedAction: $"Confirm preference: {pref.PreferenceName}"
            ));
        }
        
        return suggestions;
    }

    /// <summary>
    /// Detect conflicts between inferred and explicit preferences
    /// </summary>
    public async Task<List<InferredPreference>> DetectPreferenceConflictsAsync(
        List<InferredPreference> inferred,
        ProfilePreferences explicitPreferences,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var conflicts = new List<InferredPreference>();
        
        foreach (var inferredPref in inferred)
        {
            var conflict = CheckForConflict(inferredPref, explicitPreferences);
            if (conflict != null)
            {
                conflicts.Add(inferredPref with { ConflictsWith = conflict });
            }
        }
        
        _logger.LogInformation("Found {Count} preference conflicts", conflicts.Count);
        return conflicts;
    }

    /// <summary>
    /// Track how preferences evolve over time
    /// </summary>
    public async Task<List<LearningInsight>> TrackPreferenceEvolutionAsync(
        List<InferredPreference> currentPreferences,
        List<InferredPreference> historicalPreferences,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var evolutions = new List<LearningInsight>();
        
        // Group by preference name and category
        var grouped = currentPreferences
            .GroupBy(p => new { p.Category, p.PreferenceName })
            .ToList();
        
        foreach (var group in grouped)
        {
            var historical = historicalPreferences
                .Where(h => h.Category == group.Key.Category && h.PreferenceName == group.Key.PreferenceName)
                .OrderBy(h => h.InferredAt)
                .ToList();
            
            var current = group.First();
            
            if (historical.Count > 0)
            {
                var previous = historical.Last();
                
                // Check if preference has changed significantly
                if (!PreferenceValuesMatch(current.PreferenceValue, previous.PreferenceValue))
                {
                    evolutions.Add(new LearningInsight(
                        InsightId: Guid.NewGuid().ToString("N"),
                        ProfileId: current.ProfileId,
                        InsightType: "preference",
                        Category: current.Category,
                        Description: $"Your {current.PreferenceName} preference has changed from {FormatPreferenceValue(previous.PreferenceValue)} to {FormatPreferenceValue(current.PreferenceValue)}",
                        Confidence: Math.Min(current.Confidence, previous.Confidence),
                        DiscoveredAt: DateTime.UtcNow,
                        IsActionable: true,
                        SuggestedAction: "Update your profile preferences to reflect this change"
                    ));
                }
            }
        }
        
        return evolutions;
    }

    #region Private Helper Methods

    private List<InferredPreference> InferTonePreferences(
        List<DecisionRecord> decisions,
        string profileId,
        TonePreferences? explicitTone)
    {
        var preferences = new List<InferredPreference>();
        
        var toneDecisions = decisions
            .Where(d => d.Context?.ContainsKey("tone") == true)
            .ToList();
        
        if (toneDecisions.Count < MinimumDecisionsForInference)
            return preferences;
        
        // Infer formality preference
        var formalityChoices = toneDecisions
            .Where(d => d.Context!.ContainsKey("formality") && d.Decision == "accepted")
            .Select(d => Convert.ToInt32(d.Context!["formality"]))
            .ToList();
        
        if (formalityChoices.Count >= 5)
        {
            var avgFormality = (int)formalityChoices.Average();
            var consistency = CalculateConsistency(formalityChoices.Select(f => (double)f).ToList());
            var confidence = CalculateConfidence(formalityChoices.Count, consistency);
            
            preferences.Add(new InferredPreference(
                PreferenceId: Guid.NewGuid().ToString("N"),
                ProfileId: profileId,
                Category: "tone",
                PreferenceName: "formality",
                PreferenceValue: avgFormality,
                Confidence: confidence,
                BasedOnDecisions: formalityChoices.Count,
                InferredAt: DateTime.UtcNow,
                IsConfirmed: false,
                ConflictsWith: null
            ));
        }
        
        // Infer energy preference
        var energyChoices = toneDecisions
            .Where(d => d.Context!.ContainsKey("energy") && d.Decision == "accepted")
            .Select(d => Convert.ToInt32(d.Context!["energy"]))
            .ToList();
        
        if (energyChoices.Count >= 5)
        {
            var avgEnergy = (int)energyChoices.Average();
            var consistency = CalculateConsistency(energyChoices.Select(e => (double)e).ToList());
            var confidence = CalculateConfidence(energyChoices.Count, consistency);
            
            preferences.Add(new InferredPreference(
                PreferenceId: Guid.NewGuid().ToString("N"),
                ProfileId: profileId,
                Category: "tone",
                PreferenceName: "energy",
                PreferenceValue: avgEnergy,
                Confidence: confidence,
                BasedOnDecisions: energyChoices.Count,
                InferredAt: DateTime.UtcNow,
                IsConfirmed: false,
                ConflictsWith: null
            ));
        }
        
        return preferences;
    }

    private List<InferredPreference> InferVisualPreferences(
        List<DecisionRecord> decisions,
        string profileId,
        VisualPreferences? explicitVisual)
    {
        var preferences = new List<InferredPreference>();
        
        var visualDecisions = decisions
            .Where(d => d.SuggestionType.Equals("visual", StringComparison.OrdinalIgnoreCase))
            .Where(d => d.Decision == "accepted")
            .ToList();
        
        if (visualDecisions.Count < MinimumDecisionsForInference)
            return preferences;
        
        // Infer aesthetic preference
        var aestheticChoices = visualDecisions
            .Where(d => d.Context?.ContainsKey("aesthetic") == true)
            .Select(d => d.Context!["aesthetic"].ToString())
            .GroupBy(a => a)
            .OrderByDescending(g => g.Count())
            .ToList();
        
        if (aestheticChoices.Count > 0 && aestheticChoices.First().Count() >= 3)
        {
            var preferredAesthetic = aestheticChoices.First().Key;
            var consistency = (double)aestheticChoices.First().Count() / visualDecisions.Count;
            var confidence = CalculateConfidence(aestheticChoices.First().Count(), consistency);
            
            preferences.Add(new InferredPreference(
                PreferenceId: Guid.NewGuid().ToString("N"),
                ProfileId: profileId,
                Category: "visual",
                PreferenceName: "aesthetic",
                PreferenceValue: preferredAesthetic ?? "balanced",
                Confidence: confidence,
                BasedOnDecisions: aestheticChoices.First().Count(),
                InferredAt: DateTime.UtcNow,
                IsConfirmed: false,
                ConflictsWith: null
            ));
        }
        
        return preferences;
    }

    private List<InferredPreference> InferAudioPreferences(
        List<DecisionRecord> decisions,
        string profileId,
        AudioPreferences? explicitAudio)
    {
        var preferences = new List<InferredPreference>();
        
        var audioDecisions = decisions
            .Where(d => d.SuggestionType.Equals("audio", StringComparison.OrdinalIgnoreCase))
            .Where(d => d.Decision == "accepted")
            .ToList();
        
        if (audioDecisions.Count < MinimumDecisionsForInference)
            return preferences;
        
        // Infer music genre preferences
        var genreChoices = audioDecisions
            .Where(d => d.Context?.ContainsKey("musicGenre") == true)
            .Select(d => d.Context!["musicGenre"].ToString())
            .GroupBy(g => g)
            .Where(g => g.Count() >= 3)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .ToList();
        
        if (genreChoices.Count > 0)
        {
            var genres = genreChoices.Select(g => g.Key).ToList();
            var confidence = CalculateConfidence(
                genreChoices.Sum(g => g.Count()),
                (double)genreChoices.Sum(g => g.Count()) / audioDecisions.Count
            );
            
            preferences.Add(new InferredPreference(
                PreferenceId: Guid.NewGuid().ToString("N"),
                ProfileId: profileId,
                Category: "audio",
                PreferenceName: "musicGenres",
                PreferenceValue: genres,
                Confidence: confidence,
                BasedOnDecisions: genreChoices.Sum(g => g.Count()),
                InferredAt: DateTime.UtcNow,
                IsConfirmed: false,
                ConflictsWith: null
            ));
        }
        
        return preferences;
    }

    private List<InferredPreference> InferEditingPreferences(
        List<DecisionRecord> decisions,
        string profileId,
        EditingPreferences? explicitEditing)
    {
        var preferences = new List<InferredPreference>();
        
        var editingDecisions = decisions
            .Where(d => d.SuggestionType.Equals("editing", StringComparison.OrdinalIgnoreCase))
            .Where(d => d.Decision == "accepted")
            .ToList();
        
        if (editingDecisions.Count < MinimumDecisionsForInference)
            return preferences;
        
        // Infer pacing preference
        var pacingChoices = editingDecisions
            .Where(d => d.Context?.ContainsKey("pacing") == true)
            .Select(d => Convert.ToInt32(d.Context!["pacing"]))
            .ToList();
        
        if (pacingChoices.Count >= 5)
        {
            var avgPacing = (int)pacingChoices.Average();
            var consistency = CalculateConsistency(pacingChoices.Select(p => (double)p).ToList());
            var confidence = CalculateConfidence(pacingChoices.Count, consistency);
            
            preferences.Add(new InferredPreference(
                PreferenceId: Guid.NewGuid().ToString("N"),
                ProfileId: profileId,
                Category: "editing",
                PreferenceName: "pacing",
                PreferenceValue: avgPacing,
                Confidence: confidence,
                BasedOnDecisions: pacingChoices.Count,
                InferredAt: DateTime.UtcNow,
                IsConfirmed: false,
                ConflictsWith: null
            ));
        }
        
        return preferences;
    }

    private double CalculateConsistency(List<double> values)
    {
        if (values.Count < 2)
            return 1.0;
        
        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
        var stdDev = Math.Sqrt(variance);
        
        // Normalize standard deviation to 0-1 (assuming values are in 0-100 range)
        var normalizedStdDev = stdDev / 100.0;
        
        // Consistency is inverse of standard deviation
        return Math.Max(0, 1.0 - normalizedStdDev);
    }

    private bool IsRelevantToPreference(DecisionRecord decision, InferredPreference preference)
    {
        // Check if decision context contains the preference attribute
        if (decision.Context == null)
            return false;
        
        var prefNameLower = preference.PreferenceName.ToLowerInvariant();
        return decision.Context.Keys.Any(k => k.ToLowerInvariant().Contains(prefNameLower));
    }

    private bool ConfirmsPreference(DecisionRecord decision, InferredPreference preference)
    {
        if (decision.Decision != "accepted")
            return false;
        
        if (decision.Context == null)
            return false;
        
        // Check if decision's context matches the inferred preference value
        var contextValue = decision.Context
            .FirstOrDefault(kv => kv.Key.Equals(preference.PreferenceName, StringComparison.OrdinalIgnoreCase))
            .Value;
        
        if (contextValue == null)
            return false;
        
        return PreferenceValuesMatch(contextValue, preference.PreferenceValue);
    }

    private bool PreferenceValuesMatch(object value1, object value2)
    {
        if (value1 == null && value2 == null)
            return true;
        
        if (value1 == null || value2 == null)
            return false;
        
        // For numeric values, check if they're close (within 20%)
        if (value1 is int int1 && value2 is int int2)
        {
            return Math.Abs(int1 - int2) <= 20;
        }
        
        // For strings, exact match
        return value1.ToString()?.Equals(value2.ToString(), StringComparison.OrdinalIgnoreCase) == true;
    }

    private string? CheckForConflict(InferredPreference inferred, ProfilePreferences explicitPreferences)
    {
        // Check each category for conflicts
        switch (inferred.Category.ToLowerInvariant())
        {
            case "tone":
                if (explicitPreferences.Tone != null && inferred.PreferenceName == "formality")
                {
                    var explicitValue = explicitPreferences.Tone.Formality;
                    if (!PreferenceValuesMatch(inferred.PreferenceValue, explicitValue))
                    {
                        return $"Explicit formality setting: {explicitValue}";
                    }
                }
                break;
            
            case "visual":
                if (explicitPreferences.Visual != null && inferred.PreferenceName == "aesthetic")
                {
                    var explicitValue = explicitPreferences.Visual.Aesthetic;
                    if (explicitValue != null && !PreferenceValuesMatch(inferred.PreferenceValue, explicitValue))
                    {
                        return $"Explicit aesthetic setting: {explicitValue}";
                    }
                }
                break;
        }
        
        return null;
    }

    private string FormatPreferenceValue(object value)
    {
        if (value is List<string> list)
        {
            return string.Join(", ", list);
        }
        
        return value.ToString() ?? "unknown";
    }

    #endregion
}
