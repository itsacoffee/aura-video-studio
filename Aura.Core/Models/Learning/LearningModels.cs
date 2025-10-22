using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Learning;

/// <summary>
/// Represents an identified pattern in user decisions
/// </summary>
public record DecisionPattern(
    string PatternId,
    string ProfileId,
    string SuggestionType,          // ideation, script, visual, audio, editing
    string PatternType,             // acceptance, rejection, modification
    double Strength,                // 0-1 confidence in pattern
    int Occurrences,                // Number of times pattern observed
    DateTime FirstObserved,
    DateTime LastObserved,
    Dictionary<string, object>? PatternData  // Additional pattern-specific data
);

/// <summary>
/// Learning insight about user behavior
/// </summary>
public record LearningInsight(
    string InsightId,
    string ProfileId,
    string InsightType,             // preference, tendency, anti-pattern
    string Category,                // ideation, script, visual, audio, editing
    string Description,             // Human-readable insight
    double Confidence,              // 0-1 confidence level
    DateTime DiscoveredAt,
    bool IsActionable,              // Whether this suggests a user action
    string? SuggestedAction         // Optional action user could take
);

/// <summary>
/// Inferred preference from user behavior
/// </summary>
public record InferredPreference(
    string PreferenceId,
    string ProfileId,
    string Category,                // tone, visual, audio, editing
    string PreferenceName,          // Specific preference attribute
    object PreferenceValue,         // Inferred value
    double Confidence,              // low (0-0.4), medium (0.4-0.7), high (0.7-1.0)
    int BasedOnDecisions,           // Number of decisions this is based on
    DateTime InferredAt,
    bool IsConfirmed,               // Whether user has confirmed this preference
    string? ConflictsWith           // If conflicts with explicit preference
);

/// <summary>
/// Prediction about how user will respond to a suggestion
/// </summary>
public record SuggestionPrediction(
    string SuggestionType,
    Dictionary<string, object> SuggestionData,
    double AcceptanceProbability,   // 0-1 probability of acceptance
    double RejectionProbability,    // 0-1 probability of rejection
    double ModificationProbability, // 0-1 probability of modification
    double Confidence,              // Overall confidence in prediction
    List<string> ReasoningFactors,  // Why this prediction was made
    List<string>? SimilarPastDecisions  // IDs of similar past decisions
);

/// <summary>
/// Statistics about decision patterns for a category
/// </summary>
public record DecisionStatistics(
    string ProfileId,
    string SuggestionType,
    int TotalDecisions,
    int Accepted,
    int Rejected,
    int Modified,
    double AcceptanceRate,
    double RejectionRate,
    double ModificationRate,
    double AverageDecisionTimeSeconds,  // How quickly decisions are made
    DateTime? LastDecisionAt
);

/// <summary>
/// Learning maturity level for a profile
/// </summary>
public record LearningMaturity(
    string ProfileId,
    int TotalDecisions,
    Dictionary<string, int> DecisionsByCategory,  // Decisions per suggestion type
    string MaturityLevel,           // nascent (<20), developing (20-50), mature (50-100), expert (100+)
    double OverallConfidence,       // 0-1 overall confidence in learning
    List<string> StrengthCategories,    // Categories with good learning
    List<string> WeakCategories,        // Categories needing more data
    DateTime LastAnalyzedAt
);

/// <summary>
/// Pattern strength score for a detected pattern
/// </summary>
public record PatternStrength(
    double StatisticalSignificance, // 0-1 based on sample size and consistency
    double Consistency,             // 0-1 how consistent the pattern is
    double Recency,                 // 0-1 weight for recent vs old observations
    double OverallScore             // 0-1 combined strength score
);

/// <summary>
/// Context for decision analysis
/// </summary>
public record DecisionContext(
    string? TimeOfDay,              // morning, afternoon, evening, night
    string? DayOfWeek,              // Monday-Sunday
    int? SessionDecisionNumber,     // Nth decision in this session
    double? DecisionTimeSeconds,    // How long to make decision
    Dictionary<string, object>? AdditionalContext
);

/// <summary>
/// Request to rank suggestions by predicted acceptance
/// </summary>
public record RankSuggestionsRequest(
    string ProfileId,
    string SuggestionType,
    List<Dictionary<string, object>> Suggestions  // List of suggestions to rank
);

/// <summary>
/// Ranked suggestion with prediction
/// </summary>
public record RankedSuggestion(
    int Rank,                       // 1-based ranking
    Dictionary<string, object> Suggestion,
    SuggestionPrediction Prediction
);

/// <summary>
/// Request to confirm an inferred preference
/// </summary>
public record ConfirmPreferenceRequest(
    string PreferenceId,
    bool IsCorrect,
    object? CorrectedValue          // If IsCorrect=false, the corrected value
);

/// <summary>
/// Cross-profile pattern (common across multiple profiles)
/// </summary>
public record CrossProfilePattern(
    string PatternId,
    List<string> ProfileIds,
    string SuggestionType,
    string PatternDescription,
    double AverageStrength,
    int TotalOccurrences
);

/// <summary>
/// Learning analytics summary
/// </summary>
public record LearningAnalytics(
    string ProfileId,
    LearningMaturity Maturity,
    List<DecisionStatistics> StatisticsByCategory,
    List<DecisionPattern> TopPatterns,
    List<InferredPreference> HighConfidencePreferences,
    int TotalInsights,
    DateTime GeneratedAt
);
