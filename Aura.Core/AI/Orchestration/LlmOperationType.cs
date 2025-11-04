using System;
using System.Collections.Generic;

namespace Aura.Core.AI.Orchestration;

/// <summary>
/// Defines the type of LLM operation being performed
/// </summary>
public enum LlmOperationType
{
    /// <summary>
    /// Generate a video plan from a brief
    /// </summary>
    Planning,
    
    /// <summary>
    /// Generate script content from a plan
    /// </summary>
    Scripting,
    
    /// <summary>
    /// Generate SSML markup for speech synthesis
    /// </summary>
    SsmlPlanning,
    
    /// <summary>
    /// Generate visual prompts for image generation
    /// </summary>
    VisualPrompts,
    
    /// <summary>
    /// RAG-based content retrieval and augmentation
    /// </summary>
    RagRetrieval,
    
    /// <summary>
    /// Scene analysis for importance and pacing
    /// </summary>
    SceneAnalysis,
    
    /// <summary>
    /// Content complexity analysis
    /// </summary>
    ComplexityAnalysis,
    
    /// <summary>
    /// Scene coherence validation
    /// </summary>
    CoherenceValidation,
    
    /// <summary>
    /// Narrative arc validation
    /// </summary>
    NarrativeValidation,
    
    /// <summary>
    /// Transition text generation
    /// </summary>
    TransitionGeneration,
    
    /// <summary>
    /// Script refinement and editing
    /// </summary>
    ScriptRefinement,
    
    /// <summary>
    /// Creative generation (high temperature)
    /// </summary>
    Creative,
    
    /// <summary>
    /// General completion (non-structured)
    /// </summary>
    Completion
}

/// <summary>
/// Preset parameters for each operation type
/// </summary>
public class LlmOperationPreset
{
    public LlmOperationType OperationType { get; init; }
    public double Temperature { get; init; }
    public double TopP { get; init; }
    public int MaxTokens { get; init; }
    public int TimeoutSeconds { get; init; }
    public int MaxRetries { get; init; }
    public bool RequiresJsonMode { get; init; }
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Provides default presets for each operation type
/// </summary>
public static class LlmOperationPresets
{
    private static readonly Dictionary<LlmOperationType, LlmOperationPreset> _presets = new()
    {
        [LlmOperationType.Planning] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.Planning,
            Temperature = 0.7,
            TopP = 0.9,
            MaxTokens = 2000,
            TimeoutSeconds = 60,
            MaxRetries = 3,
            RequiresJsonMode = true,
            Description = "Generate structured video plan from brief"
        },
        
        [LlmOperationType.Scripting] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.Scripting,
            Temperature = 0.8,
            TopP = 0.95,
            MaxTokens = 4000,
            TimeoutSeconds = 90,
            MaxRetries = 3,
            RequiresJsonMode = false,
            Description = "Generate script content from plan"
        },
        
        [LlmOperationType.SsmlPlanning] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.SsmlPlanning,
            Temperature = 0.3,
            TopP = 0.8,
            MaxTokens = 3000,
            TimeoutSeconds = 45,
            MaxRetries = 2,
            RequiresJsonMode = true,
            Description = "Generate SSML markup for speech synthesis"
        },
        
        [LlmOperationType.VisualPrompts] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.VisualPrompts,
            Temperature = 0.9,
            TopP = 0.95,
            MaxTokens = 1500,
            TimeoutSeconds = 45,
            MaxRetries = 3,
            RequiresJsonMode = true,
            Description = "Generate visual prompts for image generation"
        },
        
        [LlmOperationType.RagRetrieval] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.RagRetrieval,
            Temperature = 0.2,
            TopP = 0.8,
            MaxTokens = 2000,
            TimeoutSeconds = 30,
            MaxRetries = 2,
            RequiresJsonMode = false,
            Description = "RAG-based content retrieval and augmentation"
        },
        
        [LlmOperationType.SceneAnalysis] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.SceneAnalysis,
            Temperature = 0.3,
            TopP = 0.8,
            MaxTokens = 800,
            TimeoutSeconds = 30,
            MaxRetries = 2,
            RequiresJsonMode = true,
            Description = "Analyze scene importance and pacing"
        },
        
        [LlmOperationType.ComplexityAnalysis] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.ComplexityAnalysis,
            Temperature = 0.2,
            TopP = 0.8,
            MaxTokens = 1000,
            TimeoutSeconds = 30,
            MaxRetries = 2,
            RequiresJsonMode = true,
            Description = "Analyze content complexity"
        },
        
        [LlmOperationType.CoherenceValidation] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.CoherenceValidation,
            Temperature = 0.2,
            TopP = 0.8,
            MaxTokens = 800,
            TimeoutSeconds = 30,
            MaxRetries = 2,
            RequiresJsonMode = true,
            Description = "Validate scene-to-scene coherence"
        },
        
        [LlmOperationType.NarrativeValidation] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.NarrativeValidation,
            Temperature = 0.3,
            TopP = 0.8,
            MaxTokens = 1500,
            TimeoutSeconds = 45,
            MaxRetries = 2,
            RequiresJsonMode = true,
            Description = "Validate overall narrative arc"
        },
        
        [LlmOperationType.TransitionGeneration] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.TransitionGeneration,
            Temperature = 0.7,
            TopP = 0.9,
            MaxTokens = 500,
            TimeoutSeconds = 30,
            MaxRetries = 2,
            RequiresJsonMode = false,
            Description = "Generate transition text between scenes"
        },
        
        [LlmOperationType.ScriptRefinement] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.ScriptRefinement,
            Temperature = 0.6,
            TopP = 0.9,
            MaxTokens = 3000,
            TimeoutSeconds = 60,
            MaxRetries = 3,
            RequiresJsonMode = false,
            Description = "Refine and edit script content"
        },
        
        [LlmOperationType.Creative] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.Creative,
            Temperature = 0.9,
            TopP = 0.95,
            MaxTokens = 3000,
            TimeoutSeconds = 90,
            MaxRetries = 3,
            RequiresJsonMode = false,
            Description = "Creative generation with high temperature"
        },
        
        [LlmOperationType.Completion] = new LlmOperationPreset
        {
            OperationType = LlmOperationType.Completion,
            Temperature = 0.7,
            TopP = 0.9,
            MaxTokens = 2000,
            TimeoutSeconds = 60,
            MaxRetries = 3,
            RequiresJsonMode = false,
            Description = "General completion (non-structured)"
        }
    };
    
    /// <summary>
    /// Gets the preset for a specific operation type
    /// </summary>
    public static LlmOperationPreset GetPreset(LlmOperationType operationType)
    {
        return _presets.TryGetValue(operationType, out var preset) 
            ? preset 
            : _presets[LlmOperationType.Completion];
    }
    
    /// <summary>
    /// Gets all available presets
    /// </summary>
    public static IReadOnlyDictionary<LlmOperationType, LlmOperationPreset> GetAllPresets()
    {
        return _presets;
    }
    
    /// <summary>
    /// Creates a custom preset based on a default preset
    /// </summary>
    public static LlmOperationPreset CreateCustomPreset(
        LlmOperationType operationType,
        double? temperature = null,
        double? topP = null,
        int? maxTokens = null,
        int? timeoutSeconds = null,
        int? maxRetries = null)
    {
        var basePreset = GetPreset(operationType);
        
        return new LlmOperationPreset
        {
            OperationType = operationType,
            Temperature = temperature ?? basePreset.Temperature,
            TopP = topP ?? basePreset.TopP,
            MaxTokens = maxTokens ?? basePreset.MaxTokens,
            TimeoutSeconds = timeoutSeconds ?? basePreset.TimeoutSeconds,
            MaxRetries = maxRetries ?? basePreset.MaxRetries,
            RequiresJsonMode = basePreset.RequiresJsonMode,
            Description = basePreset.Description
        };
    }
}
