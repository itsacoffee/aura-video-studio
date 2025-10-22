using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Learning;
using Aura.Core.Models.Profiles;
using Aura.Core.Services.Profiles;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Learning;

/// <summary>
/// Main learning service that coordinates pattern recognition, preference inference, and predictive ranking
/// </summary>
public class LearningService
{
    private readonly ILogger<LearningService> _logger;
    private readonly ProfileService _profileService;
    private readonly DecisionAnalysisEngine _decisionAnalysis;
    private readonly PatternRecognitionSystem _patternRecognition;
    private readonly PreferenceInferenceEngine _preferenceInference;
    private readonly PredictiveSuggestionRanker _suggestionRanker;
    private readonly LearningPersistence _persistence;
    private readonly SemaphoreSlim _analysisLock = new(1, 1);

    public LearningService(
        ILogger<LearningService> logger,
        ProfileService profileService,
        DecisionAnalysisEngine decisionAnalysis,
        PatternRecognitionSystem patternRecognition,
        PreferenceInferenceEngine preferenceInference,
        PredictiveSuggestionRanker suggestionRanker,
        LearningPersistence persistence)
    {
        _logger = logger;
        _profileService = profileService;
        _decisionAnalysis = decisionAnalysis;
        _patternRecognition = patternRecognition;
        _preferenceInference = preferenceInference;
        _suggestionRanker = suggestionRanker;
        _persistence = persistence;
    }

    #region Pattern Analysis

    /// <summary>
    /// Get identified patterns for a profile
    /// </summary>
    public async Task<List<DecisionPattern>> GetPatternsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var patterns = await _persistence.LoadPatternsAsync(profileId, ct);
        
        // Apply pattern decay
        var decayedPatterns = _patternRecognition.ApplyPatternDecay(patterns);
        
        return decayedPatterns;
    }

    /// <summary>
    /// Trigger pattern analysis for a profile
    /// </summary>
    public async Task<List<DecisionPattern>> AnalyzePatternsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        await _analysisLock.WaitAsync(ct);
        try
        {
            _logger.LogInformation("Starting pattern analysis for profile {ProfileId}", profileId);
            
            // Get decision history
            var decisions = await _profileService.GetDecisionHistoryAsync(profileId, ct);
            
            if (decisions.Count < 5)
            {
                _logger.LogDebug("Insufficient decisions ({Count}) for pattern analysis", decisions.Count);
                return new List<DecisionPattern>();
            }
            
            // Detect patterns
            var patterns = await _patternRecognition.DetectPatternsAsync(decisions, profileId, ct);
            
            // Resolve conflicting patterns
            patterns = _patternRecognition.ResolveConflictingPatterns(patterns);
            
            // Save patterns
            await _persistence.SavePatternsAsync(profileId, patterns, ct);
            
            _logger.LogInformation("Pattern analysis complete: {Count} patterns identified", patterns.Count);
            return patterns;
        }
        finally
        {
            _analysisLock.Release();
        }
    }

    #endregion

    #region Insights

    /// <summary>
    /// Get learning insights for a profile
    /// </summary>
    public async Task<List<LearningInsight>> GetInsightsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var insights = await _persistence.LoadInsightsAsync(profileId, ct);
        
        // Filter out old insights (older than 90 days)
        var cutoffDate = DateTime.UtcNow.AddDays(-90);
        return insights.Where(i => i.DiscoveredAt > cutoffDate).ToList();
    }

    /// <summary>
    /// Generate new insights for a profile
    /// </summary>
    public async Task<List<LearningInsight>> GenerateInsightsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        await _analysisLock.WaitAsync(ct);
        try
        {
            _logger.LogInformation("Generating insights for profile {ProfileId}", profileId);
            
            var decisions = await _profileService.GetDecisionHistoryAsync(profileId, ct);
            var patterns = await GetPatternsAsync(profileId, ct);
            var preferences = await _persistence.LoadInferredPreferencesAsync(profileId, ct);
            
            var insights = new List<LearningInsight>();
            
            // Context-based insights
            var contextInsights = await _decisionAnalysis.AnalyzeDecisionContextAsync(decisions, profileId, ct);
            insights.AddRange(contextInsights);
            
            // Preference confirmation suggestions
            var preferenceInsights = await _preferenceInference.SuggestPreferenceConfirmationsAsync(preferences, ct);
            insights.AddRange(preferenceInsights);
            
            // Proactive suggestions
            var proactiveInsights = await _suggestionRanker.GenerateProactiveSuggestionsAsync(
                profileId, patterns, preferences, ct);
            insights.AddRange(proactiveInsights);
            
            // Save insights
            await _persistence.SaveInsightsAsync(profileId, insights, ct);
            
            _logger.LogInformation("Generated {Count} insights for profile {ProfileId}",
                insights.Count, profileId);
            
            return insights;
        }
        finally
        {
            _analysisLock.Release();
        }
    }

    #endregion

    #region Predictions

    /// <summary>
    /// Get prediction statistics for a profile
    /// </summary>
    public async Task<Dictionary<string, DecisionStatistics>> GetPredictionStatsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var decisions = await _profileService.GetDecisionHistoryAsync(profileId, ct);
        return await _decisionAnalysis.CalculateAcceptanceRatesAsync(decisions, profileId, ct);
    }

    /// <summary>
    /// Rank suggestions by predicted acceptance
    /// </summary>
    public async Task<List<RankedSuggestion>> RankSuggestionsAsync(
        RankSuggestionsRequest request,
        CancellationToken ct = default)
    {
        var decisions = await _profileService.GetDecisionHistoryAsync(request.ProfileId, ct);
        var patterns = await GetPatternsAsync(request.ProfileId, ct);
        
        return await _suggestionRanker.RankSuggestionsAsync(request, decisions, patterns, ct);
    }

    /// <summary>
    /// Get confidence score for a suggestion type
    /// </summary>
    public async Task<double> GetConfidenceScoreAsync(
        string profileId,
        string suggestionType,
        CancellationToken ct = default)
    {
        var decisions = await _profileService.GetDecisionHistoryAsync(profileId, ct);
        var typeDecisions = decisions
            .Where(d => d.SuggestionType.Equals(suggestionType, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (typeDecisions.Count == 0)
            return 0.0;
        
        // Confidence based on data availability and recency
        var dataScore = Math.Min(1.0, typeDecisions.Count / 30.0);
        var recentDecisions = typeDecisions.Count(d => (DateTime.UtcNow - d.Timestamp).TotalDays <= 30);
        var recencyScore = Math.Min(1.0, recentDecisions / 10.0);
        
        return (dataScore * 0.6) + (recencyScore * 0.4);
    }

    #endregion

    #region Maturity

    /// <summary>
    /// Get learning maturity level for a profile
    /// </summary>
    public async Task<LearningMaturity> GetMaturityLevelAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var decisions = await _profileService.GetDecisionHistoryAsync(profileId, ct);
        var patterns = await GetPatternsAsync(profileId, ct);
        
        // Count decisions by category
        var decisionsByCategory = decisions
            .GroupBy(d => d.SuggestionType)
            .ToDictionary(g => g.Key, g => g.Count());
        
        var totalDecisions = decisions.Count;
        
        // Determine maturity level
        var maturityLevel = totalDecisions switch
        {
            < 20 => "nascent",
            < 50 => "developing",
            < 100 => "mature",
            _ => "expert"
        };
        
        // Calculate overall confidence based on patterns and data
        var overallConfidence = patterns.Count > 0
            ? Math.Min(1.0, (patterns.Average(p => p.Strength) + (totalDecisions / 100.0)) / 2.0)
            : Math.Min(1.0, totalDecisions / 50.0);
        
        // Identify strength and weak categories
        var strengthCategories = decisionsByCategory
            .Where(kv => kv.Value >= 20)
            .Select(kv => kv.Key)
            .ToList();
        
        var weakCategories = new List<string> { "ideation", "script", "visual", "audio", "editing" }
            .Where(cat => !decisionsByCategory.ContainsKey(cat) || decisionsByCategory[cat] < 10)
            .ToList();
        
        return new LearningMaturity(
            ProfileId: profileId,
            TotalDecisions: totalDecisions,
            DecisionsByCategory: decisionsByCategory,
            MaturityLevel: maturityLevel,
            OverallConfidence: overallConfidence,
            StrengthCategories: strengthCategories,
            WeakCategories: weakCategories,
            LastAnalyzedAt: DateTime.UtcNow
        );
    }

    #endregion

    #region Preference Management

    /// <summary>
    /// Confirm an inferred preference
    /// </summary>
    public async Task ConfirmPreferenceAsync(
        string profileId,
        ConfirmPreferenceRequest request,
        CancellationToken ct = default)
    {
        var preferences = await _persistence.LoadInferredPreferencesAsync(profileId, ct);
        var preference = preferences.FirstOrDefault(p => p.PreferenceId == request.PreferenceId);
        
        if (preference == null)
        {
            throw new InvalidOperationException($"Preference {request.PreferenceId} not found");
        }
        
        InferredPreference updated;
        if (request.IsCorrect)
        {
            // Mark as confirmed
            updated = preference with { IsConfirmed = true };
        }
        else
        {
            // Update with corrected value
            updated = preference with
            {
                PreferenceValue = request.CorrectedValue ?? preference.PreferenceValue,
                IsConfirmed = true,
                Confidence = Math.Max(0.5, preference.Confidence) // Boost confidence since user confirmed
            };
        }
        
        await _persistence.UpdateInferredPreferenceAsync(profileId, updated, ct);
        
        _logger.LogInformation("Confirmed preference {PreferenceId} for profile {ProfileId}",
            request.PreferenceId, profileId);
    }

    /// <summary>
    /// Get inferred preferences for a profile
    /// </summary>
    public async Task<List<InferredPreference>> GetInferredPreferencesAsync(
        string profileId,
        CancellationToken ct = default)
    {
        return await _persistence.LoadInferredPreferencesAsync(profileId, ct);
    }

    /// <summary>
    /// Analyze and infer preferences
    /// </summary>
    public async Task<List<InferredPreference>> InferPreferencesAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var decisions = await _profileService.GetDecisionHistoryAsync(profileId, ct);
        var explicitPreferences = await _profileService.GetPreferencesAsync(profileId, ct);
        
        var inferred = await _preferenceInference.DetectImplicitPreferencesAsync(
            decisions, profileId, explicitPreferences, ct);
        
        // Check for conflicts
        var conflicts = await _preferenceInference.DetectPreferenceConflictsAsync(
            inferred, explicitPreferences, ct);
        
        // Save inferred preferences
        await _persistence.SaveInferredPreferencesAsync(profileId, inferred, ct);
        
        _logger.LogInformation("Inferred {Count} preferences for profile {ProfileId} ({Conflicts} conflicts)",
            inferred.Count, profileId, conflicts.Count);
        
        return inferred;
    }

    #endregion

    #region Cross-Profile Analysis

    /// <summary>
    /// Compare learning across multiple profiles
    /// </summary>
    public async Task<Dictionary<string, LearningMaturity>> CompareProfilesAsync(
        List<string> profileIds,
        CancellationToken ct = default)
    {
        var comparison = new Dictionary<string, LearningMaturity>();
        
        foreach (var profileId in profileIds)
        {
            var maturity = await GetMaturityLevelAsync(profileId, ct);
            comparison[profileId] = maturity;
        }
        
        return comparison;
    }

    #endregion

    #region Reset

    /// <summary>
    /// Reset learning data for a profile
    /// </summary>
    public async Task ResetLearningAsync(string profileId, CancellationToken ct = default)
    {
        await _persistence.ResetLearningAsync(profileId, ct);
        _logger.LogInformation("Reset learning data for profile {ProfileId}", profileId);
    }

    #endregion

    #region Analytics

    /// <summary>
    /// Get comprehensive learning analytics
    /// </summary>
    public async Task<LearningAnalytics> GetAnalyticsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var maturity = await GetMaturityLevelAsync(profileId, ct);
        var stats = await GetPredictionStatsAsync(profileId, ct);
        var patterns = await GetPatternsAsync(profileId, ct);
        var preferences = await GetInferredPreferencesAsync(profileId, ct);
        var insights = await GetInsightsAsync(profileId, ct);
        
        var topPatterns = patterns
            .OrderByDescending(p => p.Strength)
            .Take(10)
            .ToList();
        
        var highConfidencePreferences = preferences
            .Where(p => p.Confidence >= 0.7)
            .ToList();
        
        return new LearningAnalytics(
            ProfileId: profileId,
            Maturity: maturity,
            StatisticsByCategory: stats.Values.ToList(),
            TopPatterns: topPatterns,
            HighConfidencePreferences: highConfidencePreferences,
            TotalInsights: insights.Count,
            GeneratedAt: DateTime.UtcNow
        );
    }

    #endregion
}
