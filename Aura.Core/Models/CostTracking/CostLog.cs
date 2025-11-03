using System;
using Aura.Core.Models.Providers;

namespace Aura.Core.Models.CostTracking;

/// <summary>
/// Individual cost log entry for tracking API usage
/// </summary>
public record CostLog
{
    /// <summary>
    /// Unique identifier for this log entry
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Timestamp when this cost was incurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// User identifier
    /// </summary>
    public string UserId { get; init; } = "default";
    
    /// <summary>
    /// Provider name (OpenAI, ElevenLabs, etc.)
    /// </summary>
    public required string ProviderName { get; init; }
    
    /// <summary>
    /// Feature that incurred the cost
    /// </summary>
    public required CostFeatureType Feature { get; init; }
    
    /// <summary>
    /// Cost in USD (or configured currency)
    /// </summary>
    public required decimal Cost { get; init; }
    
    /// <summary>
    /// Project identifier (optional)
    /// </summary>
    public string? ProjectId { get; init; }
    
    /// <summary>
    /// Project name for display
    /// </summary>
    public string? ProjectName { get; init; }
    
    /// <summary>
    /// Additional details about the request (JSON serialized)
    /// </summary>
    public string? RequestDetails { get; init; }
    
    /// <summary>
    /// Number of tokens used (for LLM operations)
    /// </summary>
    public int? TokensUsed { get; init; }
    
    /// <summary>
    /// Number of characters used (for TTS operations)
    /// </summary>
    public int? CharactersUsed { get; init; }
    
    /// <summary>
    /// Compute time in seconds (for video/image generation)
    /// </summary>
    public double? ComputeTimeSeconds { get; init; }
    
    /// <summary>
    /// Operation type (for LLM operations)
    /// </summary>
    public LlmOperationType? OperationType { get; init; }
}

/// <summary>
/// Type of feature that incurs costs
/// </summary>
public enum CostFeatureType
{
    /// <summary>
    /// Script generation using LLM
    /// </summary>
    ScriptGeneration,
    
    /// <summary>
    /// Script refinement using LLM
    /// </summary>
    ScriptRefinement,
    
    /// <summary>
    /// Text-to-speech synthesis
    /// </summary>
    TextToSpeech,
    
    /// <summary>
    /// Image generation
    /// </summary>
    ImageGeneration,
    
    /// <summary>
    /// Visual prompt generation
    /// </summary>
    VisualPrompts,
    
    /// <summary>
    /// Video rendering/processing
    /// </summary>
    VideoRendering,
    
    /// <summary>
    /// Narration optimization
    /// </summary>
    NarrationOptimization,
    
    /// <summary>
    /// Quick operations (simple LLM tasks)
    /// </summary>
    QuickOperations,
    
    /// <summary>
    /// Scene analysis
    /// </summary>
    SceneAnalysis,
    
    /// <summary>
    /// Other/miscellaneous costs
    /// </summary>
    Other
}
