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
/// Analyzes user decision history to identify patterns and calculate statistics
/// </summary>
public class DecisionAnalysisEngine
{
    private readonly ILogger<DecisionAnalysisEngine> _logger;
    private const int MinimumDecisionsForPattern = 5;
    private const double PatternDecayDays = 90.0; // Patterns older than 90 days decay

    public DecisionAnalysisEngine(ILogger<DecisionAnalysisEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate acceptance rate per suggestion type
    /// </summary>
    public async Task<Dictionary<string, DecisionStatistics>> CalculateAcceptanceRatesAsync(
        List<DecisionRecord> decisions,
        string profileId,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false); // For async signature
        
        var statsByType = new Dictionary<string, DecisionStatistics>();
        
        var groupedByType = decisions.GroupBy(d => d.SuggestionType);
        
        foreach (var group in groupedByType)
        {
            var suggestionType = group.Key;
            var typeDecisions = group.ToList();
            
            var accepted = typeDecisions.Count(d => d.Decision.Equals("accepted", StringComparison.OrdinalIgnoreCase));
            var rejected = typeDecisions.Count(d => d.Decision.Equals("rejected", StringComparison.OrdinalIgnoreCase));
            var modified = typeDecisions.Count(d => d.Decision.Equals("modified", StringComparison.OrdinalIgnoreCase));
            var total = typeDecisions.Count;
            
            // Calculate average decision time if available
            var decisionsWithTime = typeDecisions
                .Where(d => d.Context?.ContainsKey("decisionTimeSeconds") == true)
                .ToList();
            
            var avgDecisionTime = decisionsWithTime.Count > 0
                ? decisionsWithTime.Average(d => Convert.ToDouble(d.Context!["decisionTimeSeconds"]))
                : 0.0;
            
            statsByType[suggestionType] = new DecisionStatistics(
                ProfileId: profileId,
                SuggestionType: suggestionType,
                TotalDecisions: total,
                Accepted: accepted,
                Rejected: rejected,
                Modified: modified,
                AcceptanceRate: total > 0 ? (double)accepted / total : 0.0,
                RejectionRate: total > 0 ? (double)rejected / total : 0.0,
                ModificationRate: total > 0 ? (double)modified / total : 0.0,
                AverageDecisionTimeSeconds: avgDecisionTime,
                LastDecisionAt: typeDecisions.Max(d => d.Timestamp)
            );
        }
        
        _logger.LogDebug("Calculated acceptance rates for {Count} suggestion types", statsByType.Count);
        return statsByType;
    }

    /// <summary>
    /// Identify rejection patterns (suggestions user consistently rejects)
    /// </summary>
    public async Task<List<DecisionPattern>> IdentifyRejectionPatternsAsync(
        List<DecisionRecord> decisions,
        string profileId,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var patterns = new List<DecisionPattern>();
        var now = DateTime.UtcNow;
        
        // Group by suggestion type and look for consistent rejections
        var groupedByType = decisions
            .Where(d => d.Decision.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            .GroupBy(d => d.SuggestionType);
        
        foreach (var group in groupedByType)
        {
            var suggestionType = group.Key;
            var rejections = group.OrderBy(d => d.Timestamp).ToList();
            
            if (rejections.Count < MinimumDecisionsForPattern)
                continue;
            
            // Calculate pattern strength with time decay
            var strength = CalculatePatternStrengthWithDecay(rejections, now);
            
            if (strength.OverallScore > 0.5) // Only include significant patterns
            {
                patterns.Add(new DecisionPattern(
                    PatternId: Guid.NewGuid().ToString("N"),
                    ProfileId: profileId,
                    SuggestionType: suggestionType,
                    PatternType: "rejection",
                    Strength: strength.OverallScore,
                    Occurrences: rejections.Count,
                    FirstObserved: rejections.First().Timestamp,
                    LastObserved: rejections.Last().Timestamp,
                    PatternData: new Dictionary<string, object>
                    {
                        { "consistency", strength.Consistency },
                        { "recency", strength.Recency },
                        { "statisticalSignificance", strength.StatisticalSignificance }
                    }
                ));
            }
        }
        
        _logger.LogInformation("Identified {Count} rejection patterns for profile {ProfileId}",
            patterns.Count, profileId);
        
        return patterns;
    }

    /// <summary>
    /// Analyze modification patterns (how users change AI suggestions)
    /// </summary>
    public async Task<List<DecisionPattern>> AnalyzeModificationPatternsAsync(
        List<DecisionRecord> decisions,
        string profileId,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var patterns = new List<DecisionPattern>();
        var now = DateTime.UtcNow;
        
        var modifications = decisions
            .Where(d => d.Decision.Equals("modified", StringComparison.OrdinalIgnoreCase))
            .GroupBy(d => d.SuggestionType);
        
        foreach (var group in modifications)
        {
            var suggestionType = group.Key;
            var mods = group.OrderBy(d => d.Timestamp).ToList();
            
            if (mods.Count < MinimumDecisionsForPattern)
                continue;
            
            // Look for common modification types in context
            var modificationTypes = mods
                .Where(m => m.Context?.ContainsKey("modificationType") == true)
                .GroupBy(m => m.Context!["modificationType"].ToString())
                .Where(g => g.Count() >= 3) // At least 3 occurrences
                .ToList();
            
            foreach (var modTypeGroup in modificationTypes)
            {
                var modType = modTypeGroup.Key ?? "unknown";
                var modList = modTypeGroup.OrderBy(m => m.Timestamp).ToList();
                
                var strength = CalculatePatternStrengthWithDecay(modList, now);
                
                if (strength.OverallScore > 0.4)
                {
                    patterns.Add(new DecisionPattern(
                        PatternId: Guid.NewGuid().ToString("N"),
                        ProfileId: profileId,
                        SuggestionType: suggestionType,
                        PatternType: "modification",
                        Strength: strength.OverallScore,
                        Occurrences: modList.Count,
                        FirstObserved: modList.First().Timestamp,
                        LastObserved: modList.Last().Timestamp,
                        PatternData: new Dictionary<string, object>
                        {
                            { "modificationType", modType },
                            { "consistency", strength.Consistency }
                        }
                    ));
                }
            }
        }
        
        _logger.LogInformation("Identified {Count} modification patterns for profile {ProfileId}",
            patterns.Count, profileId);
        
        return patterns;
    }

    /// <summary>
    /// Track decision velocity (how quickly user accepts or rejects)
    /// </summary>
    public async Task<Dictionary<string, double>> CalculateDecisionVelocityAsync(
        List<DecisionRecord> decisions,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var velocityByType = new Dictionary<string, double>();
        
        var decisionsWithTime = decisions
            .Where(d => d.Context?.ContainsKey("decisionTimeSeconds") == true)
            .GroupBy(d => d.SuggestionType);
        
        foreach (var group in decisionsWithTime)
        {
            var times = group
                .Select(d => Convert.ToDouble(d.Context!["decisionTimeSeconds"]))
                .ToList();
            
            if (times.Count > 0)
            {
                velocityByType[group.Key] = times.Average();
            }
        }
        
        return velocityByType;
    }

    /// <summary>
    /// Analyze decision context (circumstances of acceptance/rejection)
    /// </summary>
    public async Task<List<LearningInsight>> AnalyzeDecisionContextAsync(
        List<DecisionRecord> decisions,
        string profileId,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        var insights = new List<LearningInsight>();
        
        // Analyze time-of-day patterns
        var timePatterns = AnalyzeTimeOfDayPatterns(decisions, profileId);
        insights.AddRange(timePatterns);
        
        // Analyze quick vs slow decisions
        var velocityInsights = AnalyzeDecisionSpeedPatterns(decisions, profileId);
        insights.AddRange(velocityInsights);
        
        _logger.LogDebug("Generated {Count} context insights for profile {ProfileId}",
            insights.Count, profileId);
        
        return insights;
    }

    #region Helper Methods

    private PatternStrength CalculatePatternStrengthWithDecay(
        List<DecisionRecord> decisions,
        DateTime now)
    {
        if (decisions.Count == 0)
        {
            return new PatternStrength(0, 0, 0, 0);
        }
        
        // Statistical significance based on sample size
        // Using simple sigmoid: more decisions = higher significance
        var sampleSize = decisions.Count;
        var significance = Math.Min(1.0, sampleSize / 30.0); // Plateaus at 30 decisions
        
        // Consistency: are decisions evenly distributed or clustered?
        var timestamps = decisions.Select(d => d.Timestamp).OrderBy(t => t).ToList();
        var totalSpan = (timestamps.Last() - timestamps.First()).TotalDays;
        var consistency = totalSpan > 0 ? Math.Min(1.0, sampleSize / (totalSpan / 7.0)) : 1.0;
        
        // Recency: exponential decay for older patterns
        var daysSinceLastObservation = (now - timestamps.Last()).TotalDays;
        var recency = Math.Exp(-daysSinceLastObservation / PatternDecayDays);
        
        // Overall score: weighted combination
        var overallScore = (significance * 0.4) + (consistency * 0.3) + (recency * 0.3);
        
        return new PatternStrength(
            StatisticalSignificance: significance,
            Consistency: consistency,
            Recency: recency,
            OverallScore: overallScore
        );
    }

    private List<LearningInsight> AnalyzeTimeOfDayPatterns(
        List<DecisionRecord> decisions,
        string profileId)
    {
        var insights = new List<LearningInsight>();
        
        var decisionsWithTime = decisions
            .Where(d => d.Context?.ContainsKey("timeOfDay") == true)
            .ToList();
        
        if (decisionsWithTime.Count < 10)
            return insights;
        
        var byTimeOfDay = decisionsWithTime
            .GroupBy(d => d.Context!["timeOfDay"].ToString())
            .Select(g => new
            {
                TimeOfDay = g.Key,
                Count = g.Count(),
                AcceptanceRate = g.Count(d => d.Decision.Equals("accepted", StringComparison.OrdinalIgnoreCase)) / (double)g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();
        
        var mostActiveTime = byTimeOfDay.FirstOrDefault();
        if (mostActiveTime != null && mostActiveTime.Count >= 5)
        {
            insights.Add(new LearningInsight(
                InsightId: Guid.NewGuid().ToString("N"),
                ProfileId: profileId,
                InsightType: "tendency",
                Category: "general",
                Description: $"Most active during {mostActiveTime.TimeOfDay} with {mostActiveTime.AcceptanceRate:P0} acceptance rate",
                Confidence: Math.Min(1.0, mostActiveTime.Count / 20.0),
                DiscoveredAt: DateTime.UtcNow,
                IsActionable: false,
                SuggestedAction: null
            ));
        }
        
        return insights;
    }

    private List<LearningInsight> AnalyzeDecisionSpeedPatterns(
        List<DecisionRecord> decisions,
        string profileId)
    {
        var insights = new List<LearningInsight>();
        
        var decisionsWithTime = decisions
            .Where(d => d.Context?.ContainsKey("decisionTimeSeconds") == true)
            .Select(d => new
            {
                Decision = d,
                Time = Convert.ToDouble(d.Context!["decisionTimeSeconds"])
            })
            .ToList();
        
        if (decisionsWithTime.Count < 10)
            return insights;
        
        var avgTime = decisionsWithTime.Average(x => x.Time);
        var quickDecisions = decisionsWithTime.Where(x => x.Time < avgTime / 2).ToList();
        
        if (quickDecisions.Count >= 5)
        {
            var quickAcceptanceRate = quickDecisions.Count(x =>
                x.Decision.Decision.Equals("accepted", StringComparison.OrdinalIgnoreCase)) / (double)quickDecisions.Count;
            
            if (quickAcceptanceRate > 0.8)
            {
                insights.Add(new LearningInsight(
                    InsightId: Guid.NewGuid().ToString("N"),
                    ProfileId: profileId,
                    InsightType: "tendency",
                    Category: "general",
                    Description: $"Quick decisions (< {avgTime / 2:F1}s) have high acceptance rate ({quickAcceptanceRate:P0})",
                    Confidence: Math.Min(1.0, quickDecisions.Count / 15.0),
                    DiscoveredAt: DateTime.UtcNow,
                    IsActionable: false,
                    SuggestedAction: null
                ));
            }
        }
        
        return insights;
    }

    #endregion
}
