using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// DTOs for prompt management API endpoints
/// </summary>

/// <summary>
/// Request to create a new prompt template
/// </summary>
public record CreatePromptTemplateRequest(
    string Name,
    string Description,
    string PromptText,
    string Category,
    string Stage,
    string? TargetProvider,
    List<PromptVariableDto> Variables,
    List<string> Tags);

/// <summary>
/// Request to update an existing prompt template
/// </summary>
public record UpdatePromptTemplateRequest(
    string Name,
    string Description,
    string PromptText,
    List<PromptVariableDto> Variables,
    List<string> Tags,
    string Status,
    string ChangeNotes);

/// <summary>
/// Prompt template response
/// </summary>
public record PromptTemplateDto(
    string Id,
    string Name,
    string Description,
    string PromptText,
    string Category,
    string Stage,
    string Source,
    string TargetProvider,
    string Status,
    List<PromptVariableDto> Variables,
    List<string> Tags,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    string? ModifiedBy,
    int Version,
    string? ParentTemplateId,
    bool IsDefault,
    PromptPerformanceMetricsDto Metrics);

/// <summary>
/// Variable definition in a prompt template
/// </summary>
public record PromptVariableDto(
    string Name,
    string Description,
    string Type,
    bool Required,
    string? DefaultValue,
    string? ExampleValue,
    int? MinLength,
    int? MaxLength,
    string? FormatPattern,
    List<string>? AllowedValues);

/// <summary>
/// Performance metrics for a prompt template
/// </summary>
public record PromptPerformanceMetricsDto(
    int UsageCount,
    double AverageQualityScore,
    double AverageGenerationTimeMs,
    int AverageTokenUsage,
    double SuccessRate,
    int ThumbsUpCount,
    int ThumbsDownCount,
    DateTime? LastUsedAt);

/// <summary>
/// Version history entry
/// </summary>
public record PromptTemplateVersionDto(
    string Id,
    string TemplateId,
    int VersionNumber,
    string PromptText,
    string ChangeNotes,
    string ChangedBy,
    DateTime ChangedAt);

/// <summary>
/// Request to test a prompt template
/// </summary>
public record TestPromptRequest(
    string TemplateId,
    Dictionary<string, object> TestVariables,
    bool UseLowTokenLimit,
    string? PreferredProvider);

/// <summary>
/// Result from testing a prompt
/// </summary>
public record TestPromptResultDto(
    string TemplateId,
    bool Success,
    string? GeneratedContent,
    string? ErrorMessage,
    double GenerationTimeMs,
    int TokensUsed,
    DateTime ExecutedAt,
    string ResolvedPrompt);

/// <summary>
/// Request to create a prompt A/B test
/// </summary>
public record CreatePromptABTestRequest(
    string Name,
    string Description,
    List<string> TemplateIds);

/// <summary>
/// Request to run an A/B test
/// </summary>
public record RunABTestRequest(
    Dictionary<string, object> TestVariables,
    int Iterations);

/// <summary>
/// A/B test response
/// </summary>
public record ABTestDto(
    string Id,
    string Name,
    string Description,
    List<string> TemplateIds,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string CreatedBy,
    DateTime CreatedAt,
    List<ABTestResultDto> Results,
    string? WinningTemplateId);

/// <summary>
/// A/B test result
/// </summary>
public record ABTestResultDto(
    string TemplateId,
    string TemplateName,
    double QualityScore,
    double GenerationTimeMs,
    int TokenUsage,
    bool Success,
    DateTime ExecutedAt);

/// <summary>
/// A/B test summary statistics
/// </summary>
public record ABTestSummaryDto(
    string TemplateId,
    string TemplateName,
    double AverageQualityScore,
    double AverageGenerationTimeMs,
    int AverageTokenUsage,
    double SuccessRate,
    int TotalRuns);

/// <summary>
/// Request to record feedback on a template
/// </summary>
public record RecordFeedbackRequest(
    bool ThumbsUp,
    double? QualityScore,
    double? GenerationTimeMs,
    int? TokenUsage);

/// <summary>
/// Analytics query parameters
/// </summary>
public record PromptAnalyticsQueryDto(
    DateTime? StartDate,
    DateTime? EndDate,
    string? Category,
    string? Stage,
    string? Source,
    string? CreatedBy,
    int Top);

/// <summary>
/// Aggregated analytics response
/// </summary>
public record PromptAnalyticsDto(
    int TotalTemplates,
    int ActiveTemplates,
    int TotalUsages,
    double AverageQualityScore,
    double AverageSuccessRate,
    List<TemplateUsageStatsDto> TopPerformingTemplates,
    List<TemplateUsageStatsDto> MostUsedTemplates,
    Dictionary<string, int> TemplatesByCategory,
    Dictionary<string, double> AverageScoresByStage);

/// <summary>
/// Usage statistics for a template
/// </summary>
public record TemplateUsageStatsDto(
    string TemplateId,
    string TemplateName,
    int UsageCount,
    double QualityScore,
    double SuccessRate,
    int TokenUsage);

/// <summary>
/// Request to clone a template
/// </summary>
public record CloneTemplateRequest(string? NewName);

/// <summary>
/// Request to resolve template variables
/// </summary>
public record ResolveTemplateRequest(
    Dictionary<string, object> Variables,
    bool SanitizeValues,
    bool ThrowOnMissing);

/// <summary>
/// Response from template resolution
/// </summary>
public record ResolveTemplateResponse(
    string ResolvedPrompt,
    int EstimatedTokens);

/// <summary>
/// Request to validate template resolution
/// </summary>
public record ValidateTemplateResolutionRequest(
    string TemplateId,
    Dictionary<string, object> TestVariables);

/// <summary>
/// List templates query parameters
/// </summary>
public record ListTemplatesQuery(
    string? Category,
    string? Stage,
    string? Source,
    string? Status,
    string? CreatedBy,
    string? SearchTerm,
    int Skip,
    int Take);
