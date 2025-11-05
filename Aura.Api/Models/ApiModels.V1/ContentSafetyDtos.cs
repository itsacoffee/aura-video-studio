using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Content Safety API DTOs
/// </summary>

/// <summary>
/// Request to analyze content for safety
/// </summary>
public record SafetyAnalysisRequest(
    string Content,
    string? PolicyId = null);

/// <summary>
/// Response containing safety analysis results
/// </summary>
public record SafetyAnalysisResponse(
    bool IsSafe,
    int OverallSafetyScore,
    List<SafetyViolationDto> Violations,
    List<SafetyWarningDto> Warnings,
    Dictionary<string, int> CategoryScores,
    bool RequiresReview,
    bool AllowWithDisclaimer,
    string? RecommendedDisclaimer,
    List<string> SuggestedFixes);

/// <summary>
/// Safety violation details
/// </summary>
public record SafetyViolationDto(
    string Id,
    string Category,
    int SeverityScore,
    string Reason,
    string? MatchedContent,
    int? Position,
    string RecommendedAction,
    string? SuggestedFix,
    bool CanOverride);

/// <summary>
/// Safety warning details
/// </summary>
public record SafetyWarningDto(
    string Id,
    string Category,
    string Message,
    string? Context,
    List<string> Suggestions);

/// <summary>
/// Request to create or update safety policy
/// </summary>
public record SafetyPolicyRequest(
    string Name,
    string? Description,
    bool IsEnabled,
    bool AllowUserOverride,
    string Preset,
    Dictionary<string, SafetyCategoryDto>? Categories,
    List<KeywordRuleDto>? KeywordRules,
    List<TopicFilterDto>? TopicFilters,
    BrandSafetySettingsDto? BrandSafety,
    AgeAppropriatenessSettingsDto? AgeSettings,
    CulturalSensitivitySettingsDto? CulturalSettings,
    ComplianceSettingsDto? ComplianceSettings);

/// <summary>
/// Safety policy response
/// </summary>
public record SafetyPolicyResponse(
    string Id,
    string Name,
    string? Description,
    bool IsEnabled,
    bool AllowUserOverride,
    string Preset,
    Dictionary<string, SafetyCategoryDto> Categories,
    List<KeywordRuleDto> KeywordRules,
    List<TopicFilterDto> TopicFilters,
    BrandSafetySettingsDto? BrandSafety,
    AgeAppropriatenessSettingsDto? AgeSettings,
    CulturalSensitivitySettingsDto? CulturalSettings,
    ComplianceSettingsDto? ComplianceSettings,
    bool IsDefault,
    int UsageCount,
    DateTime? LastUsedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Safety category settings
/// </summary>
public record SafetyCategoryDto(
    string Type,
    int Threshold,
    bool IsEnabled,
    string DefaultAction,
    Dictionary<int, string>? SeverityActions,
    string? CustomGuidelines);

/// <summary>
/// Keyword filtering rule
/// </summary>
public record KeywordRuleDto(
    string? Id,
    string Keyword,
    string MatchType,
    bool IsCaseSensitive,
    string Action,
    string? Replacement,
    List<string>? ContextExceptions,
    bool IsRegex);

/// <summary>
/// Topic filter settings
/// </summary>
public record TopicFilterDto(
    string? Id,
    string Topic,
    bool IsBlocked,
    double ConfidenceThreshold,
    string Action,
    List<string>? Subtopics,
    List<string>? AllowedContexts);

/// <summary>
/// Brand safety settings
/// </summary>
public record BrandSafetySettingsDto(
    List<string>? RequiredKeywords,
    List<string>? BannedCompetitors,
    List<string>? BrandTerminology,
    string? BrandVoiceGuidelines,
    List<string>? RequiredDisclaimers,
    int MinBrandVoiceScore);

/// <summary>
/// Age appropriateness settings
/// </summary>
public record AgeAppropriatenessSettingsDto(
    int MinimumAge,
    int MaximumAge,
    string TargetRating,
    bool RequireParentalGuidance,
    List<string>? AgeSpecificRestrictions);

/// <summary>
/// Cultural sensitivity settings
/// </summary>
public record CulturalSensitivitySettingsDto(
    List<string>? TargetRegions,
    Dictionary<string, List<string>>? CulturalTaboos,
    bool AvoidStereotypes,
    bool RequireInclusiveLanguage,
    List<string>? ReligiousSensitivities);

/// <summary>
/// Compliance settings
/// </summary>
public record ComplianceSettingsDto(
    List<string>? RequiredDisclosures,
    bool CoppaCompliant,
    bool GdprCompliant,
    bool FtcCompliant,
    List<string>? IndustryRegulations,
    Dictionary<string, string>? AutoDisclosures);

/// <summary>
/// Request to import keyword list
/// </summary>
public record ImportKeywordsRequest(
    string Text,
    string? DefaultAction);

/// <summary>
/// Safety audit log entry
/// </summary>
public record SafetyAuditLogDto(
    string Id,
    DateTime Timestamp,
    string ContentId,
    string PolicyId,
    string UserId,
    SafetyAnalysisResponse AnalysisResult,
    string Decision,
    string? DecisionReason,
    List<string> OverriddenViolations,
    string? ProjectId,
    string ContentType);

/// <summary>
/// Request to record safety decision
/// </summary>
public record SafetyDecisionRequest(
    string ContentId,
    string PolicyId,
    string Decision,
    string? DecisionReason,
    List<string>? OverriddenViolations);

/// <summary>
/// Request to validate LLM prompt
/// </summary>
public record ValidateLlmPromptRequest(
    string Prompt,
    string? PolicyId = null);

/// <summary>
/// Response from LLM prompt validation
/// </summary>
public record ValidateLlmPromptResponse(
    string OriginalPrompt,
    bool IsValid,
    bool CanProceed,
    SafetyAnalysisResponse? AnalysisResult,
    string? ModifiedPrompt,
    string? Explanation,
    List<string> Alternatives);

/// <summary>
/// Request to suggest safe alternatives
/// </summary>
public record SuggestAlternativesRequest(
    string Content,
    string? PolicyId = null,
    int Count = 3);

/// <summary>
/// Response with suggested alternatives
/// </summary>
public record SuggestAlternativesResponse(
    List<string> Alternatives,
    string Reasoning);

/// <summary>
/// Request to validate stock media query
/// </summary>
public record ValidateStockQueryRequest(
    string Query,
    string? PolicyId = null);

/// <summary>
/// Response from stock media query validation
/// </summary>
public record ValidateStockQueryResponse(
    string OriginalQuery,
    bool IsValid,
    string ValidationMessage,
    string SanitizedQuery,
    SafetyAnalysisResponse? AnalysisResult,
    List<string> Alternatives);

/// <summary>
/// Request for remediation report
/// </summary>
public record RemediationReportRequest(
    string ContentId,
    string Content,
    string? PolicyId = null);

/// <summary>
/// Remediation report response
/// </summary>
public record RemediationReportResponse(
    string ContentId,
    SafetyAnalysisResponse? AnalysisResult,
    string Summary,
    string DetailedExplanation,
    List<RemediationStrategyDto> RemediationStrategies,
    List<string> Alternatives,
    List<UserOptionDto> UserOptions,
    string RecommendedAction);

/// <summary>
/// Remediation strategy DTO
/// </summary>
public record RemediationStrategyDto(
    string Name,
    string Description,
    string Difficulty,
    int SuccessLikelihood,
    List<string> Steps);

/// <summary>
/// User option DTO
/// </summary>
public record UserOptionDto(
    string Id,
    string Label,
    string Description,
    bool IsRecommended,
    bool RequiresAdvancedMode);
