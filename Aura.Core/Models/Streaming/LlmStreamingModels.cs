using System;

namespace Aura.Core.Models.Streaming;

/// <summary>
/// Represents a unified streaming chunk from any LLM provider
/// </summary>
public record LlmStreamChunk
{
    /// <summary>
    /// Provider name (OpenAI, Anthropic, Gemini, Azure, Ollama)
    /// </summary>
    public required string ProviderName { get; init; }

    /// <summary>
    /// New content in this chunk
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Full accumulated content so far (optional, for convenience)
    /// </summary>
    public string? AccumulatedContent { get; init; }

    /// <summary>
    /// Token index in the stream
    /// </summary>
    public int TokenIndex { get; init; }

    /// <summary>
    /// Whether this is the final chunk
    /// </summary>
    public bool IsFinal { get; init; }

    /// <summary>
    /// Optional metadata about the generation
    /// </summary>
    public LlmStreamMetadata? Metadata { get; init; }

    /// <summary>
    /// Error information if streaming failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Metadata about the LLM streaming generation
/// </summary>
public record LlmStreamMetadata
{
    /// <summary>
    /// Total tokens generated (available in final chunk)
    /// </summary>
    public int? TotalTokens { get; init; }

    /// <summary>
    /// Estimated cost in USD (null for free providers)
    /// </summary>
    public decimal? EstimatedCost { get; init; }

    /// <summary>
    /// Tokens generated per second
    /// </summary>
    public double? TokensPerSecond { get; init; }

    /// <summary>
    /// Whether this is from a local model (e.g., Ollama)
    /// </summary>
    public bool IsLocalModel { get; init; }

    /// <summary>
    /// Model name used for generation
    /// </summary>
    public string? ModelName { get; init; }

    /// <summary>
    /// Time to first token in milliseconds
    /// </summary>
    public double? TimeToFirstTokenMs { get; init; }

    /// <summary>
    /// Total generation time in milliseconds
    /// </summary>
    public double? TotalDurationMs { get; init; }

    /// <summary>
    /// Finish reason (stop, length, error, etc.)
    /// </summary>
    public string? FinishReason { get; init; }
}

/// <summary>
/// Provider-specific characteristics for adaptive UI
/// </summary>
public record LlmProviderCharacteristics
{
    /// <summary>
    /// Whether this is a local provider (e.g., Ollama)
    /// </summary>
    public bool IsLocal { get; init; }

    /// <summary>
    /// Expected time to first token in milliseconds
    /// </summary>
    public int ExpectedFirstTokenMs { get; init; }

    /// <summary>
    /// Expected tokens generated per second
    /// </summary>
    public int ExpectedTokensPerSec { get; init; }

    /// <summary>
    /// Whether this provider supports streaming
    /// </summary>
    public bool SupportsStreaming { get; init; } = true;

    /// <summary>
    /// Provider tier (Free, Pro, Enterprise)
    /// </summary>
    public string ProviderTier { get; init; } = "Unknown";

    /// <summary>
    /// Estimated cost per 1K tokens (null for free providers)
    /// </summary>
    public decimal? CostPer1KTokens { get; init; }
}
