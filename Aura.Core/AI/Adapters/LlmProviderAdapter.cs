using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Adapters;

/// <summary>
/// Base class for provider-specific LLM adapters that optimize prompts, parameters,
/// and error handling for each provider's characteristics
/// </summary>
public abstract class LlmProviderAdapter
{
    protected readonly ILogger Logger;
    
    protected LlmProviderAdapter(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Gets the name of the provider this adapter supports
    /// </summary>
    public abstract string ProviderName { get; }
    
    /// <summary>
    /// Gets the capabilities of this provider
    /// </summary>
    public abstract ProviderCapabilities Capabilities { get; }
    
    /// <summary>
    /// Optimizes a system prompt for this provider's format and best practices
    /// </summary>
    /// <param name="systemPrompt">Original system prompt</param>
    /// <returns>Optimized system prompt</returns>
    public abstract string OptimizeSystemPrompt(string systemPrompt);
    
    /// <summary>
    /// Optimizes a user prompt for this provider's format and best practices
    /// </summary>
    /// <param name="userPrompt">Original user prompt</param>
    /// <param name="operationType">Type of operation being performed</param>
    /// <returns>Optimized user prompt</returns>
    public abstract string OptimizeUserPrompt(string userPrompt, LlmOperationType operationType);
    
    /// <summary>
    /// Calculates optimal request parameters based on operation type
    /// </summary>
    /// <param name="operationType">Type of operation being performed</param>
    /// <param name="estimatedInputTokens">Estimated number of input tokens</param>
    /// <returns>Adapted request parameters</returns>
    public abstract AdaptedRequestParameters CalculateParameters(
        LlmOperationType operationType, 
        int estimatedInputTokens);
    
    /// <summary>
    /// Truncates a prompt if it exceeds token limits
    /// </summary>
    /// <param name="prompt">Prompt to truncate</param>
    /// <param name="maxTokens">Maximum allowed tokens</param>
    /// <returns>Truncated prompt and whether truncation occurred</returns>
    public abstract (string TruncatedPrompt, bool WasTruncated) TruncatePrompt(string prompt, int maxTokens);
    
    /// <summary>
    /// Validates a response from the provider
    /// </summary>
    /// <param name="response">Response to validate</param>
    /// <param name="operationType">Type of operation that generated the response</param>
    /// <returns>True if response is valid, false otherwise</returns>
    public abstract bool ValidateResponse(string response, LlmOperationType operationType);
    
    /// <summary>
    /// Handles a provider-specific error and determines recovery strategy
    /// </summary>
    /// <param name="error">Error that occurred</param>
    /// <param name="attemptNumber">Current retry attempt number</param>
    /// <returns>Recovery strategy</returns>
    public abstract ErrorRecoveryStrategy HandleError(Exception error, int attemptNumber);
    
    /// <summary>
    /// Estimates the number of tokens in a text string
    /// Uses a simple heuristic: ~4 characters per token for English text
    /// </summary>
    protected virtual int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        
        return (int)Math.Ceiling(text.Length / 4.0);
    }
    
    /// <summary>
    /// Checks if the adapter overhead is acceptable (should be less than 5ms)
    /// </summary>
    protected virtual void ValidatePerformance(TimeSpan elapsed)
    {
        const int MaxOverheadMs = 5;
        if (elapsed.TotalMilliseconds > MaxOverheadMs)
        {
            Logger.LogWarning(
                "Adapter overhead ({ElapsedMs}ms) exceeds target of {MaxMs}ms for {Provider}",
                elapsed.TotalMilliseconds, MaxOverheadMs, ProviderName);
        }
    }
}

/// <summary>
/// Strategy for recovering from an error
/// </summary>
public record ErrorRecoveryStrategy
{
    /// <summary>
    /// Whether to retry the request
    /// </summary>
    public bool ShouldRetry { get; init; }
    
    /// <summary>
    /// Delay before retrying (if applicable)
    /// </summary>
    public TimeSpan? RetryDelay { get; init; }
    
    /// <summary>
    /// Whether to fallback to another provider
    /// </summary>
    public bool ShouldFallback { get; init; }
    
    /// <summary>
    /// Modified prompt to use on retry (if applicable)
    /// </summary>
    public string? ModifiedPrompt { get; init; }
    
    /// <summary>
    /// Error message to present to user (if not retrying)
    /// </summary>
    public string? UserMessage { get; init; }
    
    /// <summary>
    /// Whether this is a permanent failure (don't retry)
    /// </summary>
    public bool IsPermanentFailure { get; init; }
}
