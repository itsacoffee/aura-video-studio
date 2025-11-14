using System;
using System.Collections.Generic;

namespace Aura.Core.Models.PromptManagement;

/// <summary>
/// Categories for organizing prompt templates
/// </summary>
public enum PromptCategory
{
    ScriptGeneration,
    SceneDescription,
    ContentAnalysis,
    Translation,
    Optimization,
    ReviewFeedback,
    Custom
}

/// <summary>
/// Source/ownership of a prompt template
/// </summary>
public enum TemplateSource
{
    System,      // Built-in, read-only
    User,        // User-created, full edit access
    Community,   // Shared by other users
    Cloned       // Cloned from system or community
}

/// <summary>
/// Pipeline stage where prompt is used
/// </summary>
public enum PipelineStage
{
    BriefToOutline,
    OutlineToScript,
    HookGeneration,
    CallToActionGeneration,
    ScriptOptimization,
    VisualDescription,
    ImagePromptFormatting,
    SceneMood,
    CharacterDescription,
    ContentAppropriateness,
    QualityScoring,
    SEOOptimization,
    Translation,
    CulturalAdaptation,
    TitleOptimization,
    DescriptionOptimization,
    HashtagGeneration,
    VideoReview,
    PacingAnalysis,
    Custom
}

/// <summary>
/// Target LLM provider for the prompt
/// </summary>
public enum TargetLlmProvider
{
    Any,        // Works with any provider
    OpenAI,
    Anthropic,
    Gemini,
    Ollama,
    AzureOpenAI
}

/// <summary>
/// Variable type for type-safe substitution
/// </summary>
public enum VariableType
{
    String,
    Numeric,
    Array,
    Object,
    Boolean,
    Conditional
}

/// <summary>
/// Status of a prompt template
/// </summary>
public enum TemplateStatus
{
    Active,
    Inactive,
    Archived,
    Testing
}

/// <summary>
/// Comprehensive prompt template model with versioning and metadata
/// </summary>
public class PromptTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    
    public PromptCategory Category { get; set; }
    public PipelineStage Stage { get; set; }
    public TemplateSource Source { get; set; }
    public TargetLlmProvider TargetProvider { get; set; } = TargetLlmProvider.Any;
    public TemplateStatus Status { get; set; } = TemplateStatus.Active;
    
    public List<PromptVariable> Variables { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    
    public int Version { get; set; } = 1;
    public string? ParentTemplateId { get; set; }
    public bool IsDefault { get; set; }
    
    public PromptPerformanceMetrics Metrics { get; set; } = new();
    
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Variable definition with type and validation rules
/// </summary>
public class PromptVariable
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public VariableType Type { get; set; } = VariableType.String;
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
    public string? ExampleValue { get; set; }
    
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? FormatPattern { get; set; }
    
    public List<string>? AllowedValues { get; set; }
    public List<VariableTransformation> Transformations { get; set; } = new();
}

/// <summary>
/// Transformation functions for variables
/// </summary>
public enum VariableTransformation
{
    Uppercase,
    Lowercase,
    Capitalize,
    Truncate,
    Join,
    Format,
    Escape,
    StripHtml
}

/// <summary>
/// Performance metrics for a prompt template
/// </summary>
public class PromptPerformanceMetrics
{
    public int UsageCount { get; set; }
    public double AverageQualityScore { get; set; }
    public double AverageGenerationTimeMs { get; set; }
    public int AverageTokenUsage { get; set; }
    public double SuccessRate { get; set; } = 1.0;
    public int ThumbsUpCount { get; set; }
    public int ThumbsDownCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Version history entry for template changes
/// </summary>
public class PromptTemplateVersion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TemplateId { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public string PromptText { get; set; } = string.Empty;
    public string ChangeNotes { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    
    public List<PromptVariable> Variables { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// A/B test configuration for comparing prompt variations
/// </summary>
public class PromptABTest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public List<string> TemplateIds { get; set; } = new();
    public ABTestStatus Status { get; set; } = ABTestStatus.Draft;
    
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<ABTestResult> Results { get; set; } = new();
    public string? WinningTemplateId { get; set; }
}

/// <summary>
/// Status of an A/B test
/// </summary>
public enum ABTestStatus
{
    Draft,
    Running,
    Completed,
    Cancelled
}

/// <summary>
/// Result from a single A/B test execution
/// </summary>
public class ABTestResult
{
    public string TemplateId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public double QualityScore { get; set; }
    public double GenerationTimeMs { get; set; }
    public int TokenUsage { get; set; }
    public bool Success { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, double> CustomScores { get; set; } = new();
}

/// <summary>
/// Request to test a prompt template with sample data
/// </summary>
public class PromptTestRequest
{
    public string TemplateId { get; set; } = string.Empty;
    public Dictionary<string, object> TestVariables { get; set; } = new();
    public bool UseLowTokenLimit { get; set; } = true;
    public TargetLlmProvider? PreferredProvider { get; set; }
}

/// <summary>
/// Result from testing a prompt
/// </summary>
public class PromptTestResult
{
    public string TemplateId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? GeneratedContent { get; set; }
    public string? ErrorMessage { get; set; }
    public double GenerationTimeMs { get; set; }
    public int TokensUsed { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string ResolvedPrompt { get; set; } = string.Empty;
}

/// <summary>
/// Analytics query parameters
/// </summary>
public class PromptAnalyticsQuery
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PromptCategory? Category { get; set; }
    public PipelineStage? Stage { get; set; }
    public TemplateSource? Source { get; set; }
    public string? CreatedBy { get; set; }
    public int Top { get; set; } = 10;
}

/// <summary>
/// Aggregated analytics for prompts
/// </summary>
public class PromptAnalytics
{
    public int TotalTemplates { get; set; }
    public int ActiveTemplates { get; set; }
    public int TotalUsages { get; set; }
    public double AverageQualityScore { get; set; }
    public double AverageSuccessRate { get; set; }
    
    public List<TemplateUsageStats> TopPerformingTemplates { get; set; } = new();
    public List<TemplateUsageStats> MostUsedTemplates { get; set; } = new();
    public Dictionary<PromptCategory, int> TemplatesByCategory { get; set; } = new();
    public Dictionary<PipelineStage, double> AverageScoresByStage { get; set; } = new();
}

/// <summary>
/// Usage statistics for a template
/// </summary>
public class TemplateUsageStats
{
    public string TemplateId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public double QualityScore { get; set; }
    public double SuccessRate { get; set; }
    public int TokenUsage { get; set; }
}

/// <summary>
/// Variable resolver configuration
/// </summary>
public class VariableResolverOptions
{
    public bool ThrowOnMissingRequired { get; set; } = true;
    public bool ThrowOnInvalidType { get; set; } = true;
    public bool SanitizeValues { get; set; } = true;
    public int MaxStringLength { get; set; } = 10000;
    public bool AllowHtml { get; set; }
}
