using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Adapters;

/// <summary>
/// Anthropic Claude-specific adapter with optimizations for Claude models
/// </summary>
public class AnthropicAdapter : LlmProviderAdapter
{
    private readonly string _model;
    
    public AnthropicAdapter(ILogger<AnthropicAdapter> logger, string model = "claude-3-5-sonnet-20241022") 
        : base(logger)
    {
        _model = model;
    }
    
    public override string ProviderName => "Anthropic";
    
    public override ProviderCapabilities Capabilities => new()
    {
        MaxTokenLimit = GetMaxTokensForModel(_model),
        DefaultMaxOutputTokens = 4096,
        SupportsJsonMode = false,
        SupportsStreaming = true,
        SupportsFunctionCalling = false,
        TypicalLatency = new LatencyCharacteristics
        {
            MinMs = 800,
            AverageMs = 3000,
            MaxMs = 15000
        },
        ContextWindowSize = GetContextWindowForModel(_model),
        SpecialFeatures = new[] { "constitutional_ai", "system_prompt_separate", "extended_context" }
    };
    
    public override string OptimizeSystemPrompt(string systemPrompt)
    {
        var startTime = DateTime.UtcNow;
        
        var optimized = systemPrompt;
        
        optimized = $"{optimized}\n\nConstitutional AI Principles:\n" +
                   "- Provide helpful, harmless, and honest responses\n" +
                   "- Be thoughtful and nuanced in analysis\n" +
                   "- Acknowledge limitations and uncertainties";
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return optimized;
    }
    
    public override string OptimizeUserPrompt(string userPrompt, LlmOperationType operationType)
    {
        var startTime = DateTime.UtcNow;
        
        var optimized = userPrompt;
        
        if (operationType == LlmOperationType.Analytical)
        {
            optimized = $"{optimized}\n\nPlease think through this step-by-step and provide detailed analysis.";
        }
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return optimized;
    }
    
    public override AdaptedRequestParameters CalculateParameters(
        LlmOperationType operationType, 
        int estimatedInputTokens)
    {
        var startTime = DateTime.UtcNow;
        
        var maxOutputTokens = CalculateMaxOutputTokens(operationType, estimatedInputTokens);
        
        var parameters = operationType switch
        {
            LlmOperationType.Creative => new AdaptedRequestParameters
            {
                Temperature = 0.8,
                MaxTokens = maxOutputTokens,
                TopP = 0.95,
                StopSequences = new[] { "\n\nHuman:", "\n\nAssistant:" }
            },
            LlmOperationType.Analytical => new AdaptedRequestParameters
            {
                Temperature = 0.5,
                MaxTokens = maxOutputTokens,
                TopP = 0.9,
                StopSequences = new[] { "\n\nHuman:", "\n\nAssistant:" }
            },
            LlmOperationType.Extraction => new AdaptedRequestParameters
            {
                Temperature = 0.2,
                MaxTokens = maxOutputTokens,
                TopP = 0.8,
                StopSequences = new[] { "\n\nHuman:", "\n\nAssistant:" }
            },
            LlmOperationType.ShortForm => new AdaptedRequestParameters
            {
                Temperature = 0.6,
                MaxTokens = Math.Min(maxOutputTokens, 1024),
                TopP = 0.9,
                StopSequences = new[] { "\n\nHuman:", "\n\nAssistant:" }
            },
            LlmOperationType.LongForm => new AdaptedRequestParameters
            {
                Temperature = 0.7,
                MaxTokens = maxOutputTokens,
                TopP = 0.95,
                StopSequences = new[] { "\n\nHuman:", "\n\nAssistant:" }
            },
            _ => new AdaptedRequestParameters
            {
                Temperature = 0.7,
                MaxTokens = maxOutputTokens,
                TopP = 0.9,
                StopSequences = new[] { "\n\nHuman:", "\n\nAssistant:" }
            }
        };
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return parameters;
    }
    
    public override (string TruncatedPrompt, bool WasTruncated) TruncatePrompt(string prompt, int maxTokens)
    {
        var startTime = DateTime.UtcNow;
        
        var estimatedTokens = EstimateTokenCount(prompt);
        
        if (estimatedTokens <= maxTokens)
        {
            ValidatePerformance(DateTime.UtcNow - startTime);
            return (prompt, false);
        }
        
        Logger.LogWarning(
            "Prompt exceeds max tokens ({Estimated} > {Max}). Truncating for Anthropic.",
            estimatedTokens, maxTokens);
        
        var targetLength = (int)(prompt.Length * ((double)maxTokens / estimatedTokens));
        var truncated = prompt[..Math.Min(targetLength, prompt.Length)];
        
        truncated += "\n\n[Note: Content has been truncated due to length constraints]";
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return (truncated, true);
    }
    
    public override bool ValidateResponse(string response, LlmOperationType operationType)
    {
        var startTime = DateTime.UtcNow;
        
        if (string.IsNullOrWhiteSpace(response))
        {
            Logger.LogWarning("Anthropic returned empty response");
            ValidatePerformance(DateTime.UtcNow - startTime);
            return false;
        }
        
        if (response.Contains("\n\nHuman:") || response.Contains("\n\nAssistant:"))
        {
            Logger.LogWarning("Anthropic response contains conversation markers - may be incomplete");
        }
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return true;
    }
    
    public override ErrorRecoveryStrategy HandleError(Exception error, int attemptNumber)
    {
        var startTime = DateTime.UtcNow;
        
        var strategy = error switch
        {
            HttpRequestException httpEx when httpEx.StatusCode == (HttpStatusCode)529 =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 3,
                    RetryDelay = TimeSpan.FromSeconds(Math.Pow(2, attemptNumber + 1)),
                    ShouldFallback = attemptNumber >= 3,
                    UserMessage = "Anthropic service is overloaded. Retrying with backoff..."
                },
            
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.TooManyRequests =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 4,
                    RetryDelay = TimeSpan.FromSeconds(Math.Pow(2, attemptNumber)),
                    ShouldFallback = attemptNumber >= 4,
                    UserMessage = "Anthropic rate limit exceeded. Retrying..."
                },
            
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.Unauthorized =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = false,
                    ShouldFallback = true,
                    IsPermanentFailure = true,
                    UserMessage = "Anthropic API key is invalid. Please check your credentials."
                },
            
            HttpRequestException httpEx when ((int?)httpEx.StatusCode) >= 500 =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 2,
                    RetryDelay = TimeSpan.FromSeconds(3 * attemptNumber),
                    ShouldFallback = attemptNumber >= 2,
                    UserMessage = "Anthropic service error. Retrying..."
                },
            
            TaskCanceledException or OperationCanceledException =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 2,
                    RetryDelay = TimeSpan.FromSeconds(2),
                    ShouldFallback = attemptNumber >= 2,
                    UserMessage = "Anthropic request timed out. Retrying..."
                },
            
            _ => new ErrorRecoveryStrategy
            {
                ShouldRetry = attemptNumber < 2,
                RetryDelay = TimeSpan.FromSeconds(attemptNumber + 1),
                ShouldFallback = attemptNumber >= 2,
                UserMessage = $"Anthropic error: {error.Message}"
            }
        };
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return strategy;
    }
    
    private int GetMaxTokensForModel(string model)
    {
        return model.ToLowerInvariant() switch
        {
            var m when m.Contains("claude-3-5") => 200000,
            var m when m.Contains("claude-3") => 200000,
            var m when m.Contains("claude-2.1") => 200000,
            var m when m.Contains("claude-2") => 100000,
            var m when m.Contains("claude-instant") => 100000,
            _ => 100000
        };
    }
    
    private int GetContextWindowForModel(string model)
    {
        return GetMaxTokensForModel(model);
    }
    
    private int CalculateMaxOutputTokens(LlmOperationType operationType, int estimatedInputTokens)
    {
        var contextWindow = Capabilities.ContextWindowSize;
        var safetyMargin = 200;
        var availableTokens = contextWindow - estimatedInputTokens - safetyMargin;
        
        var desiredTokens = operationType switch
        {
            LlmOperationType.ShortForm => 1024,
            LlmOperationType.Analytical => 2048,
            LlmOperationType.Creative => 4096,
            LlmOperationType.LongForm => 8192,
            LlmOperationType.Extraction => 2048,
            _ => 4096
        };
        
        return Math.Min(Math.Max(availableTokens, 512), desiredTokens);
    }
}
