using System;
using System.Collections.Generic;

namespace Aura.Core.Models.ContentVerification;

/// <summary>
/// Represents a claim extracted from content for verification
/// </summary>
public record Claim(
    string ClaimId,
    string Text,
    string Context,
    int StartPosition,
    int EndPosition,
    ClaimType Type,
    double ExtractionConfidence
);

/// <summary>
/// Type of claim
/// </summary>
public enum ClaimType
{
    Factual,
    Statistical,
    Historical,
    Scientific,
    Opinion,
    Prediction
}

/// <summary>
/// Result of fact-checking a claim
/// </summary>
public record FactCheckResult(
    string ClaimId,
    string Claim,
    VerificationStatus Status,
    double ConfidenceScore,
    List<Evidence> Evidence,
    string? Explanation,
    DateTime VerifiedAt
);

/// <summary>
/// Status of verification
/// </summary>
public enum VerificationStatus
{
    Verified,
    PartiallyVerified,
    Unverified,
    Disputed,
    False,
    Unknown
}

/// <summary>
/// Evidence supporting or disputing a claim
/// </summary>
public record Evidence(
    string EvidenceId,
    string Text,
    SourceAttribution Source,
    double Relevance,
    double Credibility,
    DateTime RetrievedAt
);

/// <summary>
/// Source attribution information
/// </summary>
public record SourceAttribution(
    string SourceId,
    string Name,
    string Url,
    SourceType Type,
    double CredibilityScore,
    DateTime? PublishedDate,
    string? Author
);

/// <summary>
/// Type of source
/// </summary>
public enum SourceType
{
    AcademicJournal,
    NewsOrganization,
    Government,
    Wikipedia,
    Expert,
    Organization,
    Other
}

/// <summary>
/// Confidence analysis for content
/// </summary>
public record ConfidenceAnalysis(
    string ContentId,
    double OverallConfidence,
    Dictionary<string, double> ClaimConfidences,
    List<string> HighConfidenceClaims,
    List<string> LowConfidenceClaims,
    List<string> UncertainClaims,
    DateTime AnalyzedAt
);

/// <summary>
/// Misinformation detection result
/// </summary>
public record MisinformationDetection(
    string ContentId,
    List<MisinformationFlag> Flags,
    double RiskScore,
    MisinformationRiskLevel RiskLevel,
    List<string> Recommendations,
    DateTime DetectedAt
);

/// <summary>
/// Individual misinformation flag
/// </summary>
public record MisinformationFlag(
    string FlagId,
    string ClaimId,
    string Pattern,
    MisinformationCategory Category,
    double Severity,
    string Description,
    List<string> SuggestedCorrections
);

/// <summary>
/// Category of misinformation
/// </summary>
public enum MisinformationCategory
{
    FalseInformation,
    MisleadingContext,
    ManipulatedContent,
    FabricatedContent,
    OutdatedInformation,
    UnsubstantiatedClaim,
    LogicalFallacy
}

/// <summary>
/// Risk level for misinformation
/// </summary>
public enum MisinformationRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Complete verification result
/// </summary>
public record VerificationResult(
    string ContentId,
    List<Claim> Claims,
    List<FactCheckResult> FactChecks,
    ConfidenceAnalysis Confidence,
    MisinformationDetection? Misinformation,
    List<SourceAttribution> Sources,
    VerificationStatus OverallStatus,
    double OverallConfidence,
    List<string> Warnings,
    DateTime VerifiedAt
);

/// <summary>
/// Request for content verification
/// </summary>
public record VerificationRequest(
    string ContentId,
    string Content,
    VerificationOptions Options
);

/// <summary>
/// Options for verification process
/// </summary>
public record VerificationOptions(
    bool CheckFacts = true,
    bool DetectMisinformation = true,
    bool AnalyzeConfidence = true,
    bool AttributeSources = true,
    int MaxClaimsToCheck = 50,
    double MinConfidenceThreshold = 0.5,
    List<string>? ExcludedSourceTypes = null
);
