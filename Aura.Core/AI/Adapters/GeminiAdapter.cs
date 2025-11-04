using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Adapters;

/// <summary>
/// Google Gemini-specific adapter with optimizations for Gemini models
/// </summary>
public class GeminiAdapter : LlmProviderAdapter
{
    private readonly string _model;
    
    public GeminiAdapter(ILogger<GeminiAdapter> logger, string? model = null) 
        : base(logger)
    {
        _model = model ?? ModelRegistry.GetDefaultModel("Gemini");
    }
    
    public override string ProviderName => "Gemini";
    
    public override ProviderCapabilities Capabilities => new()
    {
        MaxTokenLimit = GetMaxTokensForModel(_model),
        DefaultMaxOutputTokens = 2048,
        SupportsJsonMode = false,
        SupportsStreaming = true,
        SupportsFunctionCalling = true,
        TypicalLatency = new LatencyCharacteristics
        {
            MinMs = 600,
            AverageMs = 2500,
            MaxMs = 12000
        },
        ContextWindowSize = GetContextWindowForModel(_model),
        SpecialFeatures = new[] { "multimodal", "safety_settings", "multiple_candidates" }
    };
    
    public override string OptimizeSystemPrompt(string systemPrompt)
    {
        var startTime = DateTime.UtcNow;
        
        var optimized = systemPrompt;
        
        optimized = $"Context: {optimized}\n\n" +
                   "Important: Ensure all responses are safe, helpful, and appropriate.";
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return optimized;
    }
    
    public override string OptimizeUserPrompt(string userPrompt, LlmOperationType operationType)
    {
        var startTime = DateTime.UtcNow;
        
        var optimized = userPrompt;
        
        if (operationType == LlmOperationType.Creative)
        {
            optimized = $"{optimized}\n\nBe creative and innovative in your response.";
        }
        else if (operationType == LlmOperationType.Extraction)
        {
            optimized = $"{optimized}\n\nExtract the key information in a clear, structured format.";
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
                Temperature = 0.9,
                MaxTokens = maxOutputTokens,
                TopP = 0.95,
                TopK = 40
            },
            LlmOperationType.Analytical => new AdaptedRequestParameters
            {
                Temperature = 0.4,
                MaxTokens = maxOutputTokens,
                TopP = 0.85,
                TopK = 20
            },
            LlmOperationType.Extraction => new AdaptedRequestParameters
            {
                Temperature = 0.2,
                MaxTokens = maxOutputTokens,
                TopP = 0.8,
                TopK = 10
            },
            LlmOperationType.ShortForm => new AdaptedRequestParameters
            {
                Temperature = 0.6,
                MaxTokens = Math.Min(maxOutputTokens, 800),
                TopP = 0.9,
                TopK = 30
            },
            LlmOperationType.LongForm => new AdaptedRequestParameters
            {
                Temperature = 0.8,
                MaxTokens = maxOutputTokens,
                TopP = 0.95,
                TopK = 40
            },
            _ => new AdaptedRequestParameters
            {
                Temperature = 0.7,
                MaxTokens = maxOutputTokens,
                TopP = 0.9,
                TopK = 40
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
            "Prompt exceeds max tokens ({Estimated} > {Max}). Truncating for Gemini.",
            estimatedTokens, maxTokens);
        
        var targetLength = (int)(prompt.Length * ((double)maxTokens / estimatedTokens));
        var truncated = prompt[..Math.Min(targetLength, prompt.Length)];
        
        truncated += "\n\n[Content truncated]";
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return (truncated, true);
    }
    
    public override bool ValidateResponse(string response, LlmOperationType operationType)
    {
        var startTime = DateTime.UtcNow;
        
        if (string.IsNullOrWhiteSpace(response))
        {
            Logger.LogWarning("Gemini returned empty response");
            ValidatePerformance(DateTime.UtcNow - startTime);
            return false;
        }
        
        if (response.Contains("[SAFETY_BLOCKED]") || response.Contains("I cannot"))
        {
            Logger.LogWarning("Gemini response may have been blocked by safety filters");
        }
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return true;
    }
    
    public override ErrorRecoveryStrategy HandleError(Exception error, int attemptNumber)
    {
        var startTime = DateTime.UtcNow;
        
        var errorMessage = error.Message.ToLowerInvariant();
        
        if (errorMessage.Contains("safety") || errorMessage.Contains("blocked"))
        {
            ValidatePerformance(DateTime.UtcNow - startTime);
            return new ErrorRecoveryStrategy
            {
                ShouldRetry = true,
                RetryDelay = TimeSpan.FromMilliseconds(500),
                ModifiedPrompt = "[Prompt modified to comply with safety guidelines]",
                UserMessage = "Content was blocked by safety filters. Retrying with modified prompt..."
            };
        }
        
        var strategy = error switch
        {
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.TooManyRequests =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 3,
                    RetryDelay = TimeSpan.FromSeconds(Math.Pow(2, attemptNumber)),
                    ShouldFallback = attemptNumber >= 3,
                    UserMessage = "Gemini rate limit exceeded. Retrying..."
                },
            
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.Unauthorized =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = false,
                    ShouldFallback = true,
                    IsPermanentFailure = true,
                    UserMessage = "Gemini API key is invalid. Please check your credentials."
                },
            
            HttpRequestException httpEx when ((int?)httpEx.StatusCode) >= 500 =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 2,
                    RetryDelay = TimeSpan.FromSeconds(2 * attemptNumber),
                    ShouldFallback = attemptNumber >= 2,
                    UserMessage = "Gemini service error. Retrying..."
                },
            
            TaskCanceledException or OperationCanceledException =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 2,
                    RetryDelay = TimeSpan.FromSeconds(2),
                    ShouldFallback = attemptNumber >= 2,
                    UserMessage = "Gemini request timed out. Retrying..."
                },
            
            _ => new ErrorRecoveryStrategy
            {
                ShouldRetry = attemptNumber < 2,
                RetryDelay = TimeSpan.FromSeconds(attemptNumber),
                ShouldFallback = attemptNumber >= 2,
                UserMessage = $"Gemini error: {error.Message}"
            }
        };
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return strategy;
    }
    
    private int GetMaxTokensForModel(string model)
    {
        var modelInfo = ModelRegistry.FindModel("Gemini", model);
        if (modelInfo != null)
        {
            return modelInfo.MaxTokens;
        }
        
        Logger.LogWarning("Model {Model} not found in registry, estimating capabilities", model);
        var (maxTokens, _) = ModelRegistry.EstimateCapabilities(model);
        return maxTokens;
    }
    
    private int GetContextWindowForModel(string model)
    {
        var modelInfo = ModelRegistry.FindModel("Gemini", model);
        if (modelInfo != null)
        {
            return modelInfo.ContextWindow;
        }
        
        var (_, contextWindow) = ModelRegistry.EstimateCapabilities(model);
        return contextWindow;
    }
    
    private int CalculateMaxOutputTokens(LlmOperationType operationType, int estimatedInputTokens)
    {
        var contextWindow = Capabilities.ContextWindowSize;
        var safetyMargin = 100;
        var availableTokens = contextWindow - estimatedInputTokens - safetyMargin;
        
        var desiredTokens = operationType switch
        {
            LlmOperationType.ShortForm => 800,
            LlmOperationType.Analytical => 1536,
            LlmOperationType.Creative => 2048,
            LlmOperationType.LongForm => 4096,
            LlmOperationType.Extraction => 1024,
            _ => 2048
        };
        
        return Math.Min(Math.Max(availableTokens, 256), desiredTokens);
    }
    
    public override async Task<ProviderHealthResult> HealthCheckAsync(CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            Logger.LogDebug("Performing health check for Gemini provider with model {Model}", _model);
            
            var modelExists = ModelRegistry.FindModel("Gemini", _model) != null;
            
            var elapsed = DateTime.UtcNow - startTime;
            
            if (!modelExists)
            {
                Logger.LogWarning("Model {Model} not found in registry during health check", _model);
                return new ProviderHealthResult
                {
                    IsHealthy = false,
                    ResponseTimeMs = elapsed.TotalMilliseconds,
                    ErrorMessage = $"Model {_model} not found in registry",
                    Details = "Model may not be available or configured correctly"
                };
            }
            
            return new ProviderHealthResult
            {
                IsHealthy = true,
                ResponseTimeMs = elapsed.TotalMilliseconds,
                Details = $"Model {_model} is available. Context window: {GetContextWindowForModel(_model)} tokens"
            };
        }
        catch (Exception ex)
        {
            var elapsed = DateTime.UtcNow - startTime;
            Logger.LogError(ex, "Health check failed for Gemini provider");
            
            return new ProviderHealthResult
            {
                IsHealthy = false,
                ResponseTimeMs = elapsed.TotalMilliseconds,
                ErrorMessage = ex.Message,
                Details = "Health check exception occurred"
            };
        }
    }
}
