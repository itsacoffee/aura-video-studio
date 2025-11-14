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
/// Ranks and filters AI suggestions based on predicted user acceptance
/// </summary>
public class PredictiveSuggestionRanker
{
    private readonly ILogger<PredictiveSuggestionRanker> _logger;
    private readonly PatternRecognitionSystem _patternRecognition;

    public PredictiveSuggestionRanker(
        ILogger<PredictiveSuggestionRanker> logger,
        PatternRecognitionSystem patternRecognition)
    {
        _logger = logger;
        _patternRecognition = patternRecognition;
    }

    /// <summary>
    /// Rank suggestions by predicted acceptance probability
    /// </summary>
    public async Task<List<RankedSuggestion>> RankSuggestionsAsync(
        RankSuggestionsRequest request,
        List<DecisionRecord> decisionHistory,
        List<DecisionPattern> patterns,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var rankedSuggestions = new List<RankedSuggestion>();
        
        foreach (var suggestion in request.Suggestions)
        {
            var prediction = await PredictAcceptanceAsync(
                suggestion,
                request.SuggestionType,
                decisionHistory,
                patterns,
                ct).ConfigureAwait(false);
            
            rankedSuggestions.Add(new RankedSuggestion(
                Rank: 0, // Will be set after sorting
                Suggestion: suggestion,
                Prediction: prediction
            ));
        }
        
        // Sort by acceptance probability (descending)
        var sorted = rankedSuggestions
            .OrderByDescending(s => s.Prediction.AcceptanceProbability)
            .Select((s, index) => s with { Rank = index + 1 })
            .ToList();
        
        _logger.LogInformation("Ranked {Count} suggestions for type {Type}",
            sorted.Count, request.SuggestionType);
        
        return sorted;
    }

    /// <summary>
    /// Calculate confidence score for a suggestion
    /// </summary>
    public async Task<double> CalculateSuggestionConfidenceAsync(
        Dictionary<string, object> suggestion,
        string suggestionType,
        List<DecisionRecord> decisionHistory,
        List<DecisionPattern> patterns,
        CancellationToken ct = default)
    {
        var prediction = await PredictAcceptanceAsync(
            suggestion,
            suggestionType,
            decisionHistory,
            patterns,
            ct).ConfigureAwait(false);
        
        return prediction.Confidence;
    }

    /// <summary>
    /// Predict how user will respond to a suggestion
    /// </summary>
    public async Task<SuggestionPrediction> PredictAcceptanceAsync(
        Dictionary<string, object> suggestion,
        string suggestionType,
        List<DecisionRecord> decisionHistory,
        List<DecisionPattern> patterns,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var relevantDecisions = decisionHistory
            .Where(d => d.SuggestionType.Equals(suggestionType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Timestamp)
            .Take(50) // Consider recent 50 decisions
            .ToList();
        
        var relevantPatterns = patterns
            .Where(p => p.SuggestionType.Equals(suggestionType, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        // Calculate probabilities
        var acceptanceProbability = CalculateAcceptanceProbability(
            suggestion,
            relevantDecisions,
            relevantPatterns);
        
        var rejectionProbability = CalculateRejectionProbability(
            suggestion,
            relevantDecisions,
            relevantPatterns);
        
        var modificationProbability = 1.0 - acceptanceProbability - rejectionProbability;
        modificationProbability = Math.Max(0, modificationProbability); // Ensure non-negative
        
        // Find similar past decisions
        var similarDecisions = FindSimilarDecisions(suggestion, relevantDecisions);
        
        // Generate reasoning factors
        var reasoningFactors = GenerateReasoningFactors(
            acceptanceProbability,
            relevantPatterns,
            similarDecisions);
        
        // Calculate overall confidence
        var confidence = CalculateOverallConfidence(
            relevantDecisions.Count,
            relevantPatterns.Count,
            similarDecisions.Count);
        
        return new SuggestionPrediction(
            SuggestionType: suggestionType,
            SuggestionData: suggestion,
            AcceptanceProbability: acceptanceProbability,
            RejectionProbability: rejectionProbability,
            ModificationProbability: modificationProbability,
            Confidence: confidence,
            ReasoningFactors: reasoningFactors,
            SimilarPastDecisions: similarDecisions.Select(d => d.RecordId).ToList()
        );
    }

    /// <summary>
    /// Filter suggestions likely to be rejected
    /// </summary>
    public async Task<List<Dictionary<string, object>>> FilterSuggestionsAsync(
        List<Dictionary<string, object>> suggestions,
        string suggestionType,
        List<DecisionRecord> decisionHistory,
        List<DecisionPattern> patterns,
        double rejectionThreshold = 0.7,
        CancellationToken ct = default)
    {
        var filtered = new List<Dictionary<string, object>>();
        
        foreach (var suggestion in suggestions)
        {
            var prediction = await PredictAcceptanceAsync(
                suggestion,
                suggestionType,
                decisionHistory,
                patterns,
                ct).ConfigureAwait(false);
            
            if (prediction.RejectionProbability < rejectionThreshold)
            {
                filtered.Add(suggestion);
            }
            else
            {
                _logger.LogDebug("Filtered out suggestion with {Probability:P0} rejection probability",
                    prediction.RejectionProbability);
            }
        }
        
        _logger.LogInformation("Filtered {Original} suggestions to {Filtered} (threshold: {Threshold:P0})",
            suggestions.Count, filtered.Count, rejectionThreshold);
        
        return filtered;
    }

    /// <summary>
    /// Generate alternative suggestions for rejected items
    /// </summary>
    public async Task<List<LearningInsight>> GenerateAlternativesAsync(
        Dictionary<string, object> rejectedSuggestion,
        string suggestionType,
        List<DecisionPattern> patterns,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var insights = new List<LearningInsight>();
        
        // Find patterns that might explain rejection
        var rejectionPatterns = patterns
            .Where(p => p.SuggestionType.Equals(suggestionType, StringComparison.OrdinalIgnoreCase))
            .Where(p => p.PatternType == "rejection")
            .OrderByDescending(p => p.Strength)
            .ToList();
        
        foreach (var pattern in rejectionPatterns.Take(3))
        {
            var suggestion = GenerateAlternativeSuggestion(rejectedSuggestion, pattern);
            
            insights.Add(new LearningInsight(
                InsightId: Guid.NewGuid().ToString("N"),
                ProfileId: pattern.ProfileId,
                InsightType: "alternative",
                Category: suggestionType,
                Description: suggestion,
                Confidence: pattern.Strength,
                DiscoveredAt: DateTime.UtcNow,
                IsActionable: true,
                SuggestedAction: "Try alternative approach"
            ));
        }
        
        return insights;
    }

    /// <summary>
    /// Generate proactive suggestions based on patterns
    /// </summary>
    public async Task<List<LearningInsight>> GenerateProactiveSuggestionsAsync(
        string profileId,
        List<DecisionPattern> patterns,
        List<InferredPreference> preferences,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var suggestions = new List<LearningInsight>();
        
        // Suggest based on strong patterns
        var strongPatterns = patterns
            .Where(p => p.Strength > 0.7)
            .OrderByDescending(p => p.Strength)
            .Take(5)
            .ToList();
        
        foreach (var pattern in strongPatterns)
        {
            suggestions.Add(new LearningInsight(
                InsightId: Guid.NewGuid().ToString("N"),
                ProfileId: profileId,
                InsightType: "proactive",
                Category: pattern.SuggestionType,
                Description: $"Based on your pattern, you might want to {GenerateProactiveSuggestion(pattern)}",
                Confidence: pattern.Strength,
                DiscoveredAt: DateTime.UtcNow,
                IsActionable: true,
                SuggestedAction: "Apply suggestion automatically"
            ));
        }
        
        return suggestions;
    }

    #region Private Helper Methods

    private double CalculateAcceptanceProbability(
        Dictionary<string, object> suggestion,
        List<DecisionRecord> decisions,
        List<DecisionPattern> patterns)
    {
        if (decisions.Count == 0)
            return 0.5; // Default to neutral
        
        // Base probability from overall acceptance rate
        var accepted = decisions.Count(d => d.Decision.Equals("accepted", StringComparison.OrdinalIgnoreCase));
        var baseProbability = (double)accepted / decisions.Count;
        
        // Adjust based on patterns
        var acceptancePatterns = patterns.Where(p => p.PatternType == "acceptance").ToList();
        if (acceptancePatterns.Count > 0)
        {
            var patternWeight = acceptancePatterns.Average(p => p.Strength);
            baseProbability = (baseProbability * 0.6) + (patternWeight * 0.4);
        }
        
        // Adjust based on similar past decisions
        var similarDecisions = FindSimilarDecisions(suggestion, decisions);
        if (similarDecisions.Count > 0)
        {
            var similarAccepted = similarDecisions.Count(d =>
                d.Decision.Equals("accepted", StringComparison.OrdinalIgnoreCase));
            var similarProbability = (double)similarAccepted / similarDecisions.Count;
            baseProbability = (baseProbability * 0.5) + (similarProbability * 0.5);
        }
        
        return Math.Max(0, Math.Min(1.0, baseProbability));
    }

    private double CalculateRejectionProbability(
        Dictionary<string, object> suggestion,
        List<DecisionRecord> decisions,
        List<DecisionPattern> patterns)
    {
        if (decisions.Count == 0)
            return 0.3; // Default to low rejection
        
        // Base probability from overall rejection rate
        var rejected = decisions.Count(d => d.Decision.Equals("rejected", StringComparison.OrdinalIgnoreCase));
        var baseProbability = (double)rejected / decisions.Count;
        
        // Adjust based on rejection patterns
        var rejectionPatterns = patterns.Where(p => p.PatternType == "rejection").ToList();
        if (rejectionPatterns.Count > 0)
        {
            var patternWeight = rejectionPatterns.Average(p => p.Strength);
            baseProbability = (baseProbability * 0.5) + (patternWeight * 0.5);
        }
        
        return Math.Max(0, Math.Min(1.0, baseProbability));
    }

    private List<DecisionRecord> FindSimilarDecisions(
        Dictionary<string, object> suggestion,
        List<DecisionRecord> decisions)
    {
        var similar = new List<DecisionRecord>();
        
        foreach (var decision in decisions)
        {
            if (decision.Context == null)
                continue;
            
            var similarity = CalculateSimilarity(suggestion, decision.Context);
            if (similarity > 0.6) // 60% similarity threshold
            {
                similar.Add(decision);
            }
        }
        
        return similar;
    }

    private double CalculateSimilarity(
        Dictionary<string, object> suggestion,
        Dictionary<string, object> context)
    {
        var commonKeys = suggestion.Keys.Intersect(context.Keys).ToList();
        if (commonKeys.Count == 0)
            return 0;
        
        var matches = 0;
        foreach (var key in commonKeys)
        {
            if (suggestion[key]?.ToString() == context[key]?.ToString())
            {
                matches++;
            }
        }
        
        return (double)matches / commonKeys.Count;
    }

    private List<string> GenerateReasoningFactors(
        double acceptanceProbability,
        List<DecisionPattern> patterns,
        List<DecisionRecord> similarDecisions)
    {
        var factors = new List<string>();
        
        if (acceptanceProbability > 0.7)
        {
            factors.Add("High acceptance rate for similar suggestions");
        }
        else if (acceptanceProbability < 0.3)
        {
            factors.Add("Low acceptance rate for similar suggestions");
        }
        
        var strongPatterns = patterns.Where(p => p.Strength > 0.7).ToList();
        if (strongPatterns.Count > 0)
        {
            factors.Add($"{strongPatterns.Count} strong pattern(s) detected");
        }
        
        if (similarDecisions.Count > 0)
        {
            factors.Add($"Based on {similarDecisions.Count} similar past decision(s)");
        }
        else
        {
            factors.Add("No similar past decisions found");
        }
        
        return factors;
    }

    private double CalculateOverallConfidence(
        int decisionCount,
        int patternCount,
        int similarDecisionCount)
    {
        // Confidence increases with more data
        var dataScore = Math.Min(1.0, decisionCount / 30.0);
        var patternScore = Math.Min(1.0, patternCount / 5.0);
        var similarityScore = Math.Min(1.0, similarDecisionCount / 5.0);
        
        return (dataScore * 0.4) + (patternScore * 0.3) + (similarityScore * 0.3);
    }

    private string GenerateAlternativeSuggestion(
        Dictionary<string, object> rejectedSuggestion,
        DecisionPattern pattern)
    {
        // Generate a suggestion based on what user typically rejects
        return $"Try adjusting {pattern.SuggestionType} parameters based on rejection pattern";
    }

    private string GenerateProactiveSuggestion(DecisionPattern pattern)
    {
        return pattern.PatternType switch
        {
            "acceptance" => $"automatically apply {pattern.SuggestionType} suggestions you typically accept",
            "rejection" => $"skip {pattern.SuggestionType} suggestions you typically reject",
            "modification" => $"pre-apply common modifications to {pattern.SuggestionType} suggestions",
            _ => $"optimize {pattern.SuggestionType} workflow"
        };
    }

    #endregion
}
