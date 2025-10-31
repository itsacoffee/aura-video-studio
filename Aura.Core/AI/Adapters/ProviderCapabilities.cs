using System;

namespace Aura.Core.AI.Adapters;

/// <summary>
/// Defines the capabilities of an LLM provider
/// </summary>
public record ProviderCapabilities
{
    /// <summary>
    /// Maximum number of tokens supported by the model
    /// </summary>
    public int MaxTokenLimit { get; init; }
    
    /// <summary>
    /// Maximum tokens typically used for output/completion
    /// </summary>
    public int DefaultMaxOutputTokens { get; init; }
    
    /// <summary>
    /// Whether the provider supports JSON mode for structured outputs
    /// </summary>
    public bool SupportsJsonMode { get; init; }
    
    /// <summary>
    /// Whether the provider supports streaming responses
    /// </summary>
    public bool SupportsStreaming { get; init; }
    
    /// <summary>
    /// Whether the provider supports function calling
    /// </summary>
    public bool SupportsFunctionCalling { get; init; }
    
    /// <summary>
    /// Typical latency characteristics in milliseconds
    /// </summary>
    public LatencyCharacteristics TypicalLatency { get; init; }
    
    /// <summary>
    /// Context window size (total tokens including input and output)
    /// </summary>
    public int ContextWindowSize { get; init; }
    
    /// <summary>
    /// Provider-specific features
    /// </summary>
    public string[] SpecialFeatures { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Latency characteristics for a provider
/// </summary>
public record LatencyCharacteristics
{
    /// <summary>
    /// Minimum expected latency in milliseconds
    /// </summary>
    public int MinMs { get; init; }
    
    /// <summary>
    /// Average expected latency in milliseconds
    /// </summary>
    public int AverageMs { get; init; }
    
    /// <summary>
    /// Maximum acceptable latency in milliseconds
    /// </summary>
    public int MaxMs { get; init; }
}

/// <summary>
/// Operation type for LLM requests
/// </summary>
public enum LlmOperationType
{
    /// <summary>
    /// Creative content generation (scripts, stories)
    /// </summary>
    Creative,
    
    /// <summary>
    /// Analytical tasks (scene analysis, complexity assessment)
    /// </summary>
    Analytical,
    
    /// <summary>
    /// Structured data extraction
    /// </summary>
    Extraction,
    
    /// <summary>
    /// Short form generation (transitions, prompts)
    /// </summary>
    ShortForm,
    
    /// <summary>
    /// Long form content generation
    /// </summary>
    LongForm
}

/// <summary>
/// Request parameters adapted for a specific provider
/// </summary>
public record AdaptedRequestParameters
{
    /// <summary>
    /// Temperature setting (0.0 to 1.0+)
    /// </summary>
    public double Temperature { get; init; }
    
    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int MaxTokens { get; init; }
    
    /// <summary>
    /// Top-p nucleus sampling parameter
    /// </summary>
    public double? TopP { get; init; }
    
    /// <summary>
    /// Top-k sampling parameter (for providers that support it)
    /// </summary>
    public int? TopK { get; init; }
    
    /// <summary>
    /// Frequency penalty to reduce repetition
    /// </summary>
    public double? FrequencyPenalty { get; init; }
    
    /// <summary>
    /// Presence penalty for variety
    /// </summary>
    public double? PresencePenalty { get; init; }
    
    /// <summary>
    /// Stop sequences
    /// </summary>
    public string[]? StopSequences { get; init; }
    
    /// <summary>
    /// Additional provider-specific parameters
    /// </summary>
    public object? ProviderSpecificParams { get; init; }
}
