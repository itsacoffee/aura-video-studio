using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Chain-of-thought generation stages for iterative script creation
/// </summary>
public enum ChainOfThoughtStage
{
    TopicAnalysis,
    Outline,
    FullScript
}

/// <summary>
/// Result from a chain-of-thought stage with content and metadata
/// </summary>
public record ChainOfThoughtResult(
    ChainOfThoughtStage Stage,
    string Content,
    bool RequiresUserReview,
    string? SuggestedEdits = null);

/// <summary>
/// Few-shot example for a specific video type
/// </summary>
public record FewShotExample(
    string VideoType,
    string ExampleName,
    string Description,
    string SampleBrief,
    string SampleOutput,
    string[] KeyTechniques);

/// <summary>
/// Prompt template version with metadata
/// </summary>
public record PromptVersion(
    string Version,
    string Name,
    string Description,
    string SystemPrompt,
    string UserPromptTemplate,
    Dictionary<string, string> Variables,
    bool IsDefault = false);

/// <summary>
/// Preview of a prompt with variable substitutions
/// </summary>
public record PromptPreview(
    string SystemPrompt,
    string UserPrompt,
    string FinalPrompt,
    Dictionary<string, string> SubstitutedVariables,
    string PromptVersion,
    int EstimatedTokens);

/// <summary>
/// User's saved prompt preset
/// </summary>
public record PromptPreset(
    string Name,
    string Description,
    string? AdditionalInstructions,
    string? ExampleStyle,
    bool EnableChainOfThought,
    string? PromptVersion,
    DateTime CreatedAt,
    DateTime? LastUsedAt);
