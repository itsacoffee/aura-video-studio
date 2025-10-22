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
/// Detects and categorizes patterns in user behavior
/// </summary>
public class PatternRecognitionSystem
{
    private readonly ILogger<PatternRecognitionSystem> _logger;
    private const int MinimumPatternOccurrences = 3;
    private const double MinimumPatternConfidence = 0.4;

    public PatternRecognitionSystem(ILogger<PatternRecognitionSystem> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detect patterns in user decisions
    /// </summary>
    public async Task<List<DecisionPattern>> DetectPatternsAsync(
        List<DecisionRecord> decisions,
        string profileId,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        var patterns = new List<DecisionPattern>();
        
        // Detect acceptance patterns
        patterns.AddRange(DetectAcceptancePatterns(decisions, profileId));
        
        // Detect rejection patterns
        patterns.AddRange(DetectRejectionPatterns(decisions, profileId));
        
        // Detect modification patterns
        patterns.AddRange(DetectModificationPatterns(decisions, profileId));
        
        // Detect temporal patterns
        patterns.AddRange(DetectTemporalPatterns(decisions, profileId));
        
        _logger.LogInformation("Detected {Count} patterns for profile {ProfileId}",
            patterns.Count, profileId);
        
        return patterns.Where(p => p.Strength >= MinimumPatternConfidence).ToList();
    }

    /// <summary>
    /// Calculate pattern strength score
    /// </summary>
    public PatternStrength CalculatePatternStrength(
        List<DecisionRecord> patternDecisions,
        List<DecisionRecord> allDecisions)
    {
        if (patternDecisions.Count == 0)
        {
            return new PatternStrength(0, 0, 0, 0);
        }
        
        // Statistical significance: based on sample size relative to total
        var totalDecisions = allDecisions.Count;
        var patternCount = patternDecisions.Count;
        var significance = totalDecisions > 0
            ? Math.Min(1.0, patternCount / Math.Max(10.0, totalDecisions * 0.3))
            : 0;
        
        // Consistency: how evenly distributed are the pattern occurrences
        var timestamps = patternDecisions.Select(d => d.Timestamp).OrderBy(t => t).ToList();
        var timeSpan = (timestamps.Last() - timestamps.First()).TotalDays;
        var consistency = timeSpan > 0
            ? Math.Min(1.0, patternCount / (timeSpan / 3.0)) // Expect 1 every 3 days for consistency
            : 1.0;
        
        // Recency: exponential decay for old patterns
        var daysSinceLastOccurrence = (DateTime.UtcNow - timestamps.Last()).TotalDays;
        var recency = Math.Exp(-daysSinceLastOccurrence / 90.0); // 90-day decay
        
        // Overall score
        var overallScore = (significance * 0.35) + (consistency * 0.35) + (recency * 0.3);
        
        return new PatternStrength(
            StatisticalSignificance: significance,
            Consistency: consistency,
            Recency: recency,
            OverallScore: overallScore
        );
    }

    /// <summary>
    /// Categorize patterns by type
    /// </summary>
    public Dictionary<string, List<DecisionPattern>> CategorizePatterns(
        List<DecisionPattern> patterns)
    {
        var categorized = new Dictionary<string, List<DecisionPattern>>
        {
            { "ideation", new List<DecisionPattern>() },
            { "script", new List<DecisionPattern>() },
            { "visual", new List<DecisionPattern>() },
            { "audio", new List<DecisionPattern>() },
            { "editing", new List<DecisionPattern>() }
        };
        
        foreach (var pattern in patterns)
        {
            var category = pattern.SuggestionType.ToLowerInvariant();
            if (categorized.ContainsKey(category))
            {
                categorized[category].Add(pattern);
            }
            else
            {
                // Default to ideation if unknown
                categorized["ideation"].Add(pattern);
            }
        }
        
        return categorized;
    }

    /// <summary>
    /// Track temporal patterns (patterns emerging over time)
    /// </summary>
    public async Task<List<DecisionPattern>> TrackTemporalPatternsAsync(
        List<DecisionRecord> decisions,
        string profileId,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;
        
        return DetectTemporalPatterns(decisions, profileId);
    }

    /// <summary>
    /// Resolve conflicting patterns
    /// </summary>
    public List<DecisionPattern> ResolveConflictingPatterns(
        List<DecisionPattern> patterns)
    {
        var resolved = new List<DecisionPattern>();
        
        // Group by suggestion type
        var grouped = patterns.GroupBy(p => p.SuggestionType);
        
        foreach (var group in grouped)
        {
            var groupPatterns = group.ToList();
            
            // Check for conflicts (acceptance vs rejection for same type)
            var acceptancePatterns = groupPatterns
                .Where(p => p.PatternType == "acceptance")
                .OrderByDescending(p => p.Strength)
                .ToList();
            
            var rejectionPatterns = groupPatterns
                .Where(p => p.PatternType == "rejection")
                .OrderByDescending(p => p.Strength)
                .ToList();
            
            // If both acceptance and rejection patterns exist, keep the stronger one
            if (acceptancePatterns.Count > 0 && rejectionPatterns.Count > 0)
            {
                var strongestAcceptance = acceptancePatterns.First();
                var strongestRejection = rejectionPatterns.First();
                
                if (strongestAcceptance.Strength > strongestRejection.Strength * 1.2)
                {
                    // Acceptance pattern is significantly stronger
                    resolved.Add(strongestAcceptance);
                    // Keep modification patterns
                    resolved.AddRange(groupPatterns.Where(p => p.PatternType == "modification"));
                }
                else if (strongestRejection.Strength > strongestAcceptance.Strength * 1.2)
                {
                    // Rejection pattern is significantly stronger
                    resolved.Add(strongestRejection);
                }
                else
                {
                    // Patterns are balanced, keep both
                    resolved.AddRange(groupPatterns);
                }
            }
            else
            {
                // No conflict, keep all patterns
                resolved.AddRange(groupPatterns);
            }
        }
        
        return resolved;
    }

    /// <summary>
    /// Apply pattern decay for old patterns
    /// </summary>
    public List<DecisionPattern> ApplyPatternDecay(
        List<DecisionPattern> patterns,
        int decayDays = 90)
    {
        var now = DateTime.UtcNow;
        var decayed = new List<DecisionPattern>();
        
        foreach (var pattern in patterns)
        {
            var daysSinceLastObserved = (now - pattern.LastObserved).TotalDays;
            
            if (daysSinceLastObserved > decayDays * 2)
            {
                // Pattern is too old, skip it
                continue;
            }
            
            // Apply exponential decay
            var decayFactor = Math.Exp(-daysSinceLastObserved / decayDays);
            var decayedStrength = pattern.Strength * decayFactor;
            
            if (decayedStrength >= MinimumPatternConfidence)
            {
                decayed.Add(pattern with { Strength = decayedStrength });
            }
        }
        
        return decayed;
    }

    #region Private Helper Methods

    private List<DecisionPattern> DetectAcceptancePatterns(
        List<DecisionRecord> decisions,
        string profileId)
    {
        var patterns = new List<DecisionPattern>();
        
        var acceptedDecisions = decisions
            .Where(d => d.Decision.Equals("accepted", StringComparison.OrdinalIgnoreCase))
            .GroupBy(d => d.SuggestionType);
        
        foreach (var group in acceptedDecisions)
        {
            var groupList = group.ToList();
            if (groupList.Count < MinimumPatternOccurrences)
                continue;
            
            var strength = CalculatePatternStrength(groupList, decisions);
            
            patterns.Add(new DecisionPattern(
                PatternId: Guid.NewGuid().ToString("N"),
                ProfileId: profileId,
                SuggestionType: group.Key,
                PatternType: "acceptance",
                Strength: strength.OverallScore,
                Occurrences: groupList.Count,
                FirstObserved: groupList.Min(d => d.Timestamp),
                LastObserved: groupList.Max(d => d.Timestamp),
                PatternData: new Dictionary<string, object>
                {
                    { "consistency", strength.Consistency },
                    { "recency", strength.Recency }
                }
            ));
        }
        
        return patterns;
    }

    private List<DecisionPattern> DetectRejectionPatterns(
        List<DecisionRecord> decisions,
        string profileId)
    {
        var patterns = new List<DecisionPattern>();
        
        var rejectedDecisions = decisions
            .Where(d => d.Decision.Equals("rejected", StringComparison.OrdinalIgnoreCase))
            .GroupBy(d => d.SuggestionType);
        
        foreach (var group in rejectedDecisions)
        {
            var groupList = group.ToList();
            if (groupList.Count < MinimumPatternOccurrences)
                continue;
            
            var strength = CalculatePatternStrength(groupList, decisions);
            
            patterns.Add(new DecisionPattern(
                PatternId: Guid.NewGuid().ToString("N"),
                ProfileId: profileId,
                SuggestionType: group.Key,
                PatternType: "rejection",
                Strength: strength.OverallScore,
                Occurrences: groupList.Count,
                FirstObserved: groupList.Min(d => d.Timestamp),
                LastObserved: groupList.Max(d => d.Timestamp),
                PatternData: new Dictionary<string, object>
                {
                    { "consistency", strength.Consistency },
                    { "recency", strength.Recency }
                }
            ));
        }
        
        return patterns;
    }

    private List<DecisionPattern> DetectModificationPatterns(
        List<DecisionRecord> decisions,
        string profileId)
    {
        var patterns = new List<DecisionPattern>();
        
        var modifiedDecisions = decisions
            .Where(d => d.Decision.Equals("modified", StringComparison.OrdinalIgnoreCase))
            .GroupBy(d => d.SuggestionType);
        
        foreach (var group in modifiedDecisions)
        {
            var groupList = group.ToList();
            if (groupList.Count < MinimumPatternOccurrences)
                continue;
            
            var strength = CalculatePatternStrength(groupList, decisions);
            
            patterns.Add(new DecisionPattern(
                PatternId: Guid.NewGuid().ToString("N"),
                ProfileId: profileId,
                SuggestionType: group.Key,
                PatternType: "modification",
                Strength: strength.OverallScore,
                Occurrences: groupList.Count,
                FirstObserved: groupList.Min(d => d.Timestamp),
                LastObserved: groupList.Max(d => d.Timestamp),
                PatternData: new Dictionary<string, object>
                {
                    { "consistency", strength.Consistency },
                    { "recency", strength.Recency }
                }
            ));
        }
        
        return patterns;
    }

    private List<DecisionPattern> DetectTemporalPatterns(
        List<DecisionRecord> decisions,
        string profileId)
    {
        var patterns = new List<DecisionPattern>();
        var now = DateTime.UtcNow;
        
        // Look for patterns in recent decisions (last 30 days)
        var recentDecisions = decisions
            .Where(d => (now - d.Timestamp).TotalDays <= 30)
            .GroupBy(d => new { d.SuggestionType, d.Decision })
            .Where(g => g.Count() >= MinimumPatternOccurrences)
            .ToList();
        
        foreach (var group in recentDecisions)
        {
            var groupList = group.ToList();
            var strength = CalculatePatternStrength(groupList, decisions);
            
            // Only include if this is emerging (stronger recently than overall)
            var allSameType = decisions.Where(d =>
                d.SuggestionType == group.Key.SuggestionType &&
                d.Decision == group.Key.Decision).ToList();
            
            var overallStrength = CalculatePatternStrength(allSameType, decisions);
            
            if (strength.OverallScore > overallStrength.OverallScore * 1.1)
            {
                patterns.Add(new DecisionPattern(
                    PatternId: Guid.NewGuid().ToString("N"),
                    ProfileId: profileId,
                    SuggestionType: group.Key.SuggestionType,
                    PatternType: $"temporal_{group.Key.Decision}",
                    Strength: strength.OverallScore,
                    Occurrences: groupList.Count,
                    FirstObserved: groupList.Min(d => d.Timestamp),
                    LastObserved: groupList.Max(d => d.Timestamp),
                    PatternData: new Dictionary<string, object>
                    {
                        { "isEmerging", true },
                        { "recentCount", groupList.Count }
                    }
                ));
            }
        }
        
        return patterns;
    }

    #endregion
}
