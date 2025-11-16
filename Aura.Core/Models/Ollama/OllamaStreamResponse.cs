namespace Aura.Core.Models.Ollama;

/// <summary>
/// Represents a streaming chunk response from Ollama API
/// </summary>
public record OllamaStreamResponse
{
    /// <summary>
    /// Model name used for generation
    /// </summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the response was created
    /// </summary>
    public string CreatedAt { get; init; } = string.Empty;

    /// <summary>
    /// The response text chunk for this iteration
    /// </summary>
    public string Response { get; init; } = string.Empty;

    /// <summary>
    /// Whether this is the final chunk
    /// </summary>
    public bool Done { get; init; }

    /// <summary>
    /// Total duration of generation in nanoseconds (only in final chunk)
    /// </summary>
    public long? TotalDuration { get; init; }

    /// <summary>
    /// Model load duration in nanoseconds (only in final chunk)
    /// </summary>
    public long? LoadDuration { get; init; }

    /// <summary>
    /// Number of prompt tokens evaluated (only in final chunk)
    /// </summary>
    public int? PromptEvalCount { get; init; }

    /// <summary>
    /// Duration of prompt evaluation in nanoseconds (only in final chunk)
    /// </summary>
    public long? PromptEvalDuration { get; init; }

    /// <summary>
    /// Number of tokens generated (only in final chunk)
    /// </summary>
    public int? EvalCount { get; init; }

    /// <summary>
    /// Duration of token generation in nanoseconds (only in final chunk)
    /// </summary>
    public long? EvalDuration { get; init; }

    /// <summary>
    /// Calculate tokens per second from metrics
    /// </summary>
    public double? GetTokensPerSecond()
    {
        if (EvalCount.HasValue && EvalDuration.HasValue && EvalDuration.Value > 0)
        {
            return (double)EvalCount.Value / (EvalDuration.Value / 1_000_000_000.0);
        }
        return null;
    }

    /// <summary>
    /// Calculate progress percentage based on token count
    /// </summary>
    public double GetProgressPercentage(int expectedTokens = 2048)
    {
        if (EvalCount.HasValue && expectedTokens > 0)
        {
            return Math.Min(100.0, (double)EvalCount.Value / expectedTokens * 100.0);
        }
        return 0.0;
    }
}
