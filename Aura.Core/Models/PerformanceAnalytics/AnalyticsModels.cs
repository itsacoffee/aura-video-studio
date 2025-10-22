using System;
using System.Collections.Generic;

namespace Aura.Core.Models.PerformanceAnalytics;

/// <summary>
/// Represents video performance data from any platform
/// </summary>
public record VideoPerformanceData(
    string VideoId,
    string ProfileId,
    string? ProjectId,            // Linked Aura project, if any
    string Platform,              // YouTube, TikTok, Instagram, etc.
    string VideoTitle,
    string? VideoUrl,
    DateTime PublishedAt,
    DateTime? DataCollectedAt,
    PerformanceMetrics Metrics,
    AudienceData? Audience,
    Dictionary<string, object>? RawData  // Original platform data
);

/// <summary>
/// Normalized performance metrics across all platforms
/// </summary>
public record PerformanceMetrics(
    long Views,
    long? WatchTimeMinutes,      // Total watch time
    double? AverageViewDuration,  // In seconds
    double? AverageViewPercentage, // Percentage of video watched
    EngagementMetrics Engagement,
    double? ClickThroughRate,     // For thumbnails (if available)
    TrafficSources? Traffic,
    List<RetentionPoint>? RetentionCurve,
    DeviceBreakdown? Devices
);

/// <summary>
/// Engagement metrics (likes, comments, shares)
/// </summary>
public record EngagementMetrics(
    long Likes,
    long? Dislikes,
    long Comments,
    long Shares,
    double EngagementRate         // (likes + comments + shares) / views
);

/// <summary>
/// Traffic source breakdown
/// </summary>
public record TrafficSources(
    long? Search,
    long? Suggested,
    long? External,
    long? Direct,
    Dictionary<string, long>? Other  // Platform-specific sources
);

/// <summary>
/// Point on audience retention curve
/// </summary>
public record RetentionPoint(
    int TimeSeconds,              // Time from start of video
    double RetentionPercentage    // Percentage of viewers still watching
);

/// <summary>
/// Device type breakdown
/// </summary>
public record DeviceBreakdown(
    long? Mobile,
    long? Desktop,
    long? Tablet,
    long? TV,
    long? Other
);

/// <summary>
/// Audience demographic data
/// </summary>
public record AudienceData(
    AgeGenderBreakdown? Demographics,
    Dictionary<string, long>? Geography,  // Country/region -> view count
    Dictionary<string, long>? Languages   // Language -> view count
);

/// <summary>
/// Age and gender breakdown
/// </summary>
public record AgeGenderBreakdown(
    Dictionary<string, Dictionary<string, double>>? AgeGender  // age_range -> { gender -> percentage }
);

/// <summary>
/// Platform connection/credentials
/// </summary>
public record PlatformConnection(
    string ConnectionId,
    string ProfileId,
    string Platform,              // YouTube, TikTok, Instagram
    string ConnectionType,        // oauth, api_key, manual
    DateTime ConnectedAt,
    DateTime? LastSyncAt,
    bool IsActive,
    string? AccountName,          // Display name for the connected account
    Dictionary<string, object>? Metadata
);

/// <summary>
/// Encrypted platform credentials
/// </summary>
public record PlatformCredentials(
    string ConnectionId,
    string Platform,
    string CredentialType,        // oauth_token, api_key, etc.
    string EncryptedCredentials,  // Encrypted credential data
    DateTime ExpiresAt,
    bool NeedsRefresh
);

/// <summary>
/// Analytics import record
/// </summary>
public record AnalyticsImport(
    string ImportId,
    string ProfileId,
    string Platform,
    string ImportType,            // manual_csv, manual_json, api_sync
    DateTime ImportedAt,
    int VideosImported,
    string? ImportedBy,
    string? FilePath,             // For manual imports
    Dictionary<string, object>? Metadata
);

/// <summary>
/// Link between published video and Aura project
/// </summary>
public record VideoProjectLink(
    string LinkId,
    string VideoId,
    string ProjectId,
    string ProfileId,
    string LinkType,              // manual, auto_title_match, auto_metadata_match
    double ConfidenceScore,       // 0-1 for auto-matches
    DateTime LinkedAt,
    string? LinkedBy,             // User who created link (for manual)
    bool IsConfirmed,             // Whether user confirmed auto-match
    Dictionary<string, object>? MatchingFactors  // What led to the match
);

/// <summary>
/// Correlation between AI decision and video performance
/// </summary>
public record DecisionPerformanceCorrelation(
    string CorrelationId,
    string ProjectId,
    string VideoId,
    string DecisionType,          // tone_choice, visual_style, editing_pace, etc.
    string DecisionValue,         // The specific choice made
    PerformanceOutcome Outcome,
    double CorrelationStrength,   // -1 to +1
    double StatisticalSignificance, // p-value
    DateTime AnalyzedAt,
    Dictionary<string, object>? DecisionContext
);

/// <summary>
/// Performance outcome categorization
/// </summary>
public record PerformanceOutcome(
    string OutcomeType,           // high_success, success, average, below_average, failure
    double PerformanceScore,      // 0-100 composite score
    string? ComparedTo,           // What this is compared to (profile_avg, similar_videos, etc.)
    Dictionary<string, double>? MetricScores  // Individual metric scores
);

/// <summary>
/// Identified success pattern from performance data
/// </summary>
public record SuccessPattern(
    string PatternId,
    string ProfileId,
    string PatternType,           // decision, timing, audience, content_type
    string Description,           // Human-readable pattern description
    double Strength,              // 0-1 confidence in pattern
    int Occurrences,              // Number of times observed
    PerformanceImpact Impact,
    DateTime FirstObserved,
    DateTime LastObserved,
    Dictionary<string, object>? PatternData
);

/// <summary>
/// Identified failure pattern from performance data
/// </summary>
public record FailurePattern(
    string PatternId,
    string ProfileId,
    string PatternType,
    string Description,
    double Strength,
    int Occurrences,
    PerformanceImpact Impact,
    DateTime FirstObserved,
    DateTime LastObserved,
    Dictionary<string, object>? PatternData
);

/// <summary>
/// Impact of a pattern on performance
/// </summary>
public record PerformanceImpact(
    double ViewsImpact,           // Percentage change in views
    double? EngagementImpact,     // Percentage change in engagement
    double? RetentionImpact,      // Percentage change in retention
    double OverallImpact          // Composite impact score
);

/// <summary>
/// Performance feedback for learning system
/// </summary>
public record PerformanceFeedback(
    string FeedbackId,
    string ProjectId,
    string ProfileId,
    DateTime CreatedAt,
    List<DecisionPerformanceCorrelation> Correlations,
    List<string> SuccessfulDecisions,     // Decision types that worked well
    List<string> UnsuccessfulDecisions,   // Decision types that didn't work
    Dictionary<string, double> ConfidenceAdjustments  // Decision type -> adjustment (-1 to +1)
);

/// <summary>
/// A/B test configuration
/// </summary>
public record ABTest(
    string TestId,
    string ProfileId,
    string TestName,
    string? Description,
    string Category,              // thumbnail, title, hook, pacing, etc.
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string Status,                // draft, running, completed, cancelled
    List<TestVariant> Variants,
    ABTestResults? Results
);

/// <summary>
/// Variant in an A/B test
/// </summary>
public record TestVariant(
    string VariantId,
    string VariantName,
    string? Description,
    string? ProjectId,            // Associated Aura project
    string? VideoId,              // Published video for this variant
    Dictionary<string, object> Configuration,  // Variant-specific settings
    DateTime CreatedAt
);

/// <summary>
/// Results of an A/B test
/// </summary>
public record ABTestResults(
    string TestId,
    DateTime AnalyzedAt,
    string? Winner,               // VariantId of winning variant
    double Confidence,            // Statistical confidence in results
    Dictionary<string, VariantPerformance> VariantResults,
    List<string> Insights,
    bool IsStatisticallySignificant
);

/// <summary>
/// Performance data for a test variant
/// </summary>
public record VariantPerformance(
    string VariantId,
    long SampleSize,              // Number of impressions/views
    PerformanceMetrics Metrics,
    double PerformanceScore,      // 0-100 composite score
    Dictionary<string, double>? MetricComparisons  // vs. other variants
);
