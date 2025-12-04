using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Templates;

/// <summary>
/// Represents a pre-built video structure template that guides script generation.
/// Templates define sections with timing, variables for customization, and metadata.
/// </summary>
public record VideoTemplate(
    string Id,
    string Name,
    string Description,
    string Category,
    TemplateStructureSpec Structure,
    IReadOnlyList<TemplateVariable> Variables,
    TemplateThumbnail? Thumbnail,
    TemplateMetadata Metadata);

/// <summary>
/// Defines the structure of a video template with sections and timing.
/// </summary>
public record TemplateStructureSpec(
    IReadOnlyList<TemplateSection> Sections,
    TimeSpan EstimatedDuration,
    int RecommendedSceneCount);

/// <summary>
/// Represents a section within a video template.
/// </summary>
public record TemplateSection(
    string Name,
    string Purpose,
    SectionType Type,
    TimeSpan SuggestedDuration,
    string PromptTemplate,
    bool IsOptional = false,
    IReadOnlyList<string>? ExampleContent = null,
    bool IsRepeatable = false,
    string? RepeatCountVariable = null);

/// <summary>
/// Types of sections that can appear in a video template.
/// </summary>
public enum SectionType
{
    Hook,
    Introduction,
    MainPoint,
    Transition,
    Example,
    CallToAction,
    Conclusion,
    NumberedItem,
    Comparison,
    Problem,
    Solution,
    Testimonial,
    Setup,
    RisingAction,
    Climax,
    Resolution,
    Lesson,
    Overview,
    Prerequisites,
    Step,
    CommonMistakes,
    Summary,
    Attention,
    Interest,
    Desire,
    Action,
    Recap,
    Verdict,
    OptionA,
    OptionB
}

/// <summary>
/// Represents a variable that users can customize in a template.
/// </summary>
public record TemplateVariable(
    string Name,
    string DisplayName,
    VariableType Type,
    string? DefaultValue,
    string? Placeholder,
    bool IsRequired,
    IReadOnlyList<string>? Options = null,
    int? MinValue = null,
    int? MaxValue = null);

/// <summary>
/// Types of variables supported in templates.
/// </summary>
public enum VariableType
{
    Text,
    Number,
    Selection,
    MultiSelection,
    LongText
}

/// <summary>
/// Metadata about a template for filtering and recommendations.
/// </summary>
public record TemplateMetadata(
    string[] RecommendedAudiences,
    string[] RecommendedTones,
    Aspect[] SupportedAspects,
    TimeSpan MinDuration,
    TimeSpan MaxDuration,
    string[] Tags);

/// <summary>
/// Thumbnail configuration for template display.
/// </summary>
public record TemplateThumbnail(string IconName, string AccentColor);

/// <summary>
/// Result of applying a template with variable values.
/// </summary>
public record TemplatedBrief(
    Brief Brief,
    PlanSpec PlanSpec,
    IReadOnlyList<GeneratedSection> Sections,
    VideoTemplate SourceTemplate);

/// <summary>
/// A generated section with content from template application.
/// </summary>
public record GeneratedSection(
    string Name,
    string Content,
    TimeSpan SuggestedDuration,
    SectionType Type);

/// <summary>
/// Request to apply a template with variable values.
/// </summary>
public record ApplyVideoTemplateRequest(
    string TemplateId,
    IDictionary<string, string> VariableValues,
    string? Language = null,
    Aspect? Aspect = null,
    Pacing? Pacing = null,
    Density? Density = null);

/// <summary>
/// Request to preview a script from a template.
/// </summary>
public record PreviewScriptRequest(
    string TemplateId,
    IDictionary<string, string> VariableValues);

/// <summary>
/// Response containing a preview of the generated script.
/// </summary>
public record ScriptPreviewResponse(
    string Script,
    IReadOnlyList<GeneratedSection> Sections,
    TimeSpan EstimatedDuration,
    int SceneCount);
