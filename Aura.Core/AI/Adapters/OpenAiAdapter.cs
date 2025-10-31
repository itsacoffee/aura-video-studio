using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Adapters;

/// <summary>
/// OpenAI-specific adapter with optimizations for GPT models
/// </summary>
public class OpenAiAdapter : LlmProviderAdapter
{
    private readonly string _model;
    
    public OpenAiAdapter(ILogger<OpenAiAdapter> logger, string model = "gpt-4o-mini") 
        : base(logger)
    {
        _model = model;
    }
    
    public override string ProviderName => "OpenAI";
    
    public override ProviderCapabilities Capabilities => new()
    {
        MaxTokenLimit = GetMaxTokensForModel(_model),
        DefaultMaxOutputTokens = 2048,
        SupportsJsonMode = true,
        SupportsStreaming = true,
        SupportsFunctionCalling = true,
        TypicalLatency = new LatencyCharacteristics
        {
            MinMs = 500,
            AverageMs = 2000,
            MaxMs = 10000
        },
        ContextWindowSize = GetContextWindowForModel(_model),
        SpecialFeatures = new[] { "function_calling", "json_mode", "system_message_priority" }
    };
    
    public override string OptimizeSystemPrompt(string systemPrompt)
    {
        var startTime = DateTime.UtcNow;
        
        var optimized = systemPrompt;
        
        if (!optimized.TrimStart().StartsWith("You are", StringComparison.OrdinalIgnoreCase))
        {
            optimized = $"You are an expert assistant. {optimized}";
        }
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return optimized;
    }
    
    public override string OptimizeUserPrompt(string userPrompt, LlmOperationType operationType)
    {
        var startTime = DateTime.UtcNow;
        
        var optimized = userPrompt;
        
        if (operationType == LlmOperationType.Extraction)
        {
            optimized = $"{optimized}\n\nProvide your response in a structured format.";
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
                Temperature = 0.7,
                MaxTokens = maxOutputTokens,
                TopP = 0.9,
                FrequencyPenalty = 0.3,
                PresencePenalty = 0.2
            },
            LlmOperationType.Analytical => new AdaptedRequestParameters
            {
                Temperature = 0.3,
                MaxTokens = maxOutputTokens,
                TopP = 0.8,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0
            },
            LlmOperationType.Extraction => new AdaptedRequestParameters
            {
                Temperature = 0.1,
                MaxTokens = maxOutputTokens,
                TopP = 0.7,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0
            },
            LlmOperationType.ShortForm => new AdaptedRequestParameters
            {
                Temperature = 0.5,
                MaxTokens = Math.Min(maxOutputTokens, 512),
                TopP = 0.85,
                FrequencyPenalty = 0.1,
                PresencePenalty = 0.1
            },
            LlmOperationType.LongForm => new AdaptedRequestParameters
            {
                Temperature = 0.7,
                MaxTokens = maxOutputTokens,
                TopP = 0.9,
                FrequencyPenalty = 0.5,
                PresencePenalty = 0.3
            },
            _ => new AdaptedRequestParameters
            {
                Temperature = 0.7,
                MaxTokens = maxOutputTokens,
                TopP = 0.9
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
            "Prompt exceeds max tokens ({Estimated} > {Max}). Truncating for OpenAI.",
            estimatedTokens, maxTokens);
        
        var targetLength = (int)(prompt.Length * ((double)maxTokens / estimatedTokens));
        var truncated = prompt[..Math.Min(targetLength, prompt.Length)];
        
        truncated += "\n\n[Note: Content truncated due to length]";
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return (truncated, true);
    }
    
    public override bool ValidateResponse(string response, LlmOperationType operationType)
    {
        var startTime = DateTime.UtcNow;
        
        if (string.IsNullOrWhiteSpace(response))
        {
            Logger.LogWarning("OpenAI returned empty response");
            ValidatePerformance(DateTime.UtcNow - startTime);
            return false;
        }
        
        if (response.Length < 10)
        {
            Logger.LogWarning("OpenAI response suspiciously short: {Length} characters", response.Length);
        }
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return true;
    }
    
    public override ErrorRecoveryStrategy HandleError(Exception error, int attemptNumber)
    {
        var startTime = DateTime.UtcNow;
        
        var strategy = error switch
        {
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.TooManyRequests =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 3,
                    RetryDelay = TimeSpan.FromSeconds(Math.Pow(2, attemptNumber)),
                    ShouldFallback = attemptNumber >= 3,
                    UserMessage = "OpenAI rate limit exceeded. Retrying with exponential backoff."
                },
            
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.Unauthorized =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = false,
                    ShouldFallback = true,
                    IsPermanentFailure = true,
                    UserMessage = "OpenAI API key is invalid or has been revoked. Please check your API key in Settings."
                },
            
            HttpRequestException httpEx when ((int?)httpEx.StatusCode) >= 500 =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 2,
                    RetryDelay = TimeSpan.FromSeconds(2 * attemptNumber),
                    ShouldFallback = attemptNumber >= 2,
                    UserMessage = "OpenAI service is experiencing issues. Retrying..."
                },
            
            TaskCanceledException or OperationCanceledException =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 1,
                    RetryDelay = TimeSpan.FromSeconds(1),
                    ShouldFallback = attemptNumber >= 1,
                    UserMessage = "OpenAI request timed out. Retrying..."
                },
            
            _ => new ErrorRecoveryStrategy
            {
                ShouldRetry = attemptNumber < 2,
                RetryDelay = TimeSpan.FromSeconds(attemptNumber),
                ShouldFallback = attemptNumber >= 2,
                UserMessage = $"OpenAI error: {error.Message}"
            }
        };
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return strategy;
    }
    
    private int GetMaxTokensForModel(string model)
    {
        return model.ToLowerInvariant() switch
        {
            var m when m.Contains("gpt-4o") => 128000,
            var m when m.Contains("gpt-4-turbo") => 128000,
            var m when m.Contains("gpt-4") => 8192,
            var m when m.Contains("gpt-3.5-turbo-16k") => 16384,
            var m when m.Contains("gpt-3.5-turbo") => 4096,
            _ => 4096
        };
    }
    
    private int GetContextWindowForModel(string model)
    {
        return GetMaxTokensForModel(model);
    }
    
    private int CalculateMaxOutputTokens(LlmOperationType operationType, int estimatedInputTokens)
    {
        var contextWindow = Capabilities.ContextWindowSize;
        var safetyMargin = 100;
        var availableTokens = contextWindow - estimatedInputTokens - safetyMargin;
        
        var desiredTokens = operationType switch
        {
            LlmOperationType.ShortForm => 512,
            LlmOperationType.Analytical => 1024,
            LlmOperationType.Creative => 2048,
            LlmOperationType.LongForm => 4096,
            LlmOperationType.Extraction => 1024,
            _ => 2048
        };
        
        return Math.Min(Math.Max(availableTokens, 256), desiredTokens);
    }
}
