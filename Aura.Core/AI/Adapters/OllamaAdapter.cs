using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Adapters;

/// <summary>
/// Ollama-specific adapter with optimizations for local model execution
/// </summary>
public class OllamaAdapter : LlmProviderAdapter
{
    private readonly string _model;
    
    public OllamaAdapter(ILogger<OllamaAdapter> logger, string? model = null) 
        : base(logger)
    {
        _model = model ?? ModelRegistry.GetDefaultModel("Ollama");
    }
    
    public override string ProviderName => "Ollama";
    
    public override ProviderCapabilities Capabilities => new()
    {
        MaxTokenLimit = GetMaxTokensForModel(_model),
        DefaultMaxOutputTokens = 1024,
        SupportsJsonMode = true,
        SupportsStreaming = true,
        SupportsFunctionCalling = false,
        TypicalLatency = new LatencyCharacteristics
        {
            MinMs = 2000,
            AverageMs = 8000,
            MaxMs = 30000
        },
        ContextWindowSize = GetContextWindowForModel(_model),
        SpecialFeatures = new[] { "local_execution", "privacy_focused", "no_rate_limits" }
    };
    
    public override string OptimizeSystemPrompt(string systemPrompt)
    {
        var startTime = DateTime.UtcNow;
        
        var optimized = systemPrompt;
        
        if (_model.Contains("llama", StringComparison.OrdinalIgnoreCase))
        {
            optimized = $"[INST] <<SYS>>\n{systemPrompt}\n<</SYS>>\n\n";
        }
        else if (_model.Contains("mistral", StringComparison.OrdinalIgnoreCase))
        {
            optimized = $"<s>[INST] {systemPrompt}";
        }
        else
        {
            optimized = $"System: {systemPrompt}\n\n";
        }
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return optimized;
    }
    
    public override string OptimizeUserPrompt(string userPrompt, LlmOperationType operationType)
    {
        var startTime = DateTime.UtcNow;
        
        var optimized = userPrompt;
        
        if (_model.Contains("llama", StringComparison.OrdinalIgnoreCase))
        {
            optimized = $"{userPrompt} [/INST]";
        }
        else if (_model.Contains("mistral", StringComparison.OrdinalIgnoreCase))
        {
            optimized = $"{userPrompt} [/INST]";
        }
        else
        {
            optimized = $"User: {userPrompt}\nAssistant:";
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
        
        var baseTemp = GetModelBaseTemperature(_model);
        
        var parameters = operationType switch
        {
            LlmOperationType.Creative => new AdaptedRequestParameters
            {
                Temperature = baseTemp + 0.1,
                MaxTokens = maxOutputTokens,
                TopP = 0.9,
                ProviderSpecificParams = new { num_predict = maxOutputTokens, keep_alive = "5m" }
            },
            LlmOperationType.Analytical => new AdaptedRequestParameters
            {
                Temperature = Math.Max(baseTemp - 0.2, 0.1),
                MaxTokens = maxOutputTokens,
                TopP = 0.8,
                ProviderSpecificParams = new { num_predict = maxOutputTokens, keep_alive = "5m" }
            },
            LlmOperationType.Extraction => new AdaptedRequestParameters
            {
                Temperature = 0.1,
                MaxTokens = maxOutputTokens,
                TopP = 0.7,
                ProviderSpecificParams = new { num_predict = maxOutputTokens, keep_alive = "5m" }
            },
            LlmOperationType.ShortForm => new AdaptedRequestParameters
            {
                Temperature = baseTemp,
                MaxTokens = Math.Min(maxOutputTokens, 512),
                TopP = 0.85,
                ProviderSpecificParams = new { num_predict = Math.Min(maxOutputTokens, 512), keep_alive = "3m" }
            },
            LlmOperationType.LongForm => new AdaptedRequestParameters
            {
                Temperature = baseTemp + 0.05,
                MaxTokens = maxOutputTokens,
                TopP = 0.9,
                ProviderSpecificParams = new { num_predict = maxOutputTokens, keep_alive = "10m" }
            },
            _ => new AdaptedRequestParameters
            {
                Temperature = baseTemp,
                MaxTokens = maxOutputTokens,
                TopP = 0.9,
                ProviderSpecificParams = new { num_predict = maxOutputTokens, keep_alive = "5m" }
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
            "Prompt exceeds max tokens ({Estimated} > {Max}). Truncating for Ollama (local models have smaller context windows).",
            estimatedTokens, maxTokens);
        
        var targetLength = (int)(prompt.Length * ((double)maxTokens / estimatedTokens));
        var truncated = prompt[..Math.Min(targetLength, prompt.Length)];
        
        truncated += "\n\n[Content truncated for local model]";
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return (truncated, true);
    }
    
    public override bool ValidateResponse(string response, LlmOperationType operationType)
    {
        var startTime = DateTime.UtcNow;
        
        if (string.IsNullOrWhiteSpace(response))
        {
            Logger.LogWarning("Ollama returned empty response");
            ValidatePerformance(DateTime.UtcNow - startTime);
            return false;
        }
        
        if (response.Contains("[INST]") || response.Contains("[/INST]"))
        {
            Logger.LogWarning("Ollama response contains instruction markers - may indicate incomplete generation");
        }
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return true;
    }
    
    public override ErrorRecoveryStrategy HandleError(Exception error, int attemptNumber)
    {
        var startTime = DateTime.UtcNow;
        
        if (error is HttpRequestException httpEx && 
            (httpEx.InnerException is SocketException || 
             httpEx.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)))
        {
            ValidatePerformance(DateTime.UtcNow - startTime);
            return new ErrorRecoveryStrategy
            {
                ShouldRetry = false,
                ShouldFallback = true,
                IsPermanentFailure = false,
                UserMessage = "Cannot connect to Ollama service. Please ensure Ollama is running (ollama serve) and accessible."
            };
        }
        
        var strategy = error switch
        {
            HttpRequestException httpException when httpException.StatusCode == HttpStatusCode.NotFound =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = false,
                    ShouldFallback = true,
                    IsPermanentFailure = false,
                    UserMessage = $"Model '{_model}' not found in Ollama. Please pull it first: ollama pull {_model}"
                },
            
            HttpRequestException httpException when ((int?)httpException.StatusCode) >= 500 =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 2,
                    RetryDelay = TimeSpan.FromSeconds(3),
                    ShouldFallback = attemptNumber >= 2,
                    UserMessage = "Ollama service error. It may be overloaded or restarting. Retrying..."
                },
            
            TaskCanceledException or OperationCanceledException =>
                new ErrorRecoveryStrategy
                {
                    ShouldRetry = attemptNumber < 1,
                    RetryDelay = TimeSpan.FromSeconds(5),
                    ShouldFallback = attemptNumber >= 1,
                    UserMessage = "Ollama request timed out. Local model may be loading or system is under heavy load."
                },
            
            _ => new ErrorRecoveryStrategy
            {
                ShouldRetry = attemptNumber < 1,
                RetryDelay = TimeSpan.FromSeconds(2),
                ShouldFallback = attemptNumber >= 1,
                UserMessage = $"Ollama error: {error.Message}"
            }
        };
        
        ValidatePerformance(DateTime.UtcNow - startTime);
        return strategy;
    }
    
    private double GetModelBaseTemperature(string model)
    {
        var lowerModel = model.ToLowerInvariant();
        
        if (lowerModel.Contains("llama3.1") || lowerModel.Contains("llama3"))
            return 0.6;
        
        if (lowerModel.Contains("mistral"))
            return 0.7;
        
        if (lowerModel.Contains("phi"))
            return 0.5;
        
        if (lowerModel.Contains("codellama"))
            return 0.4;
        
        return 0.6;
    }
    
    private int GetMaxTokensForModel(string model)
    {
        var modelInfo = ModelRegistry.FindModel("Ollama", model);
        if (modelInfo != null)
        {
            return modelInfo.MaxTokens;
        }
        
        Logger.LogWarning("Model {Model} not found in registry, using pattern-based detection", model);
        var (maxTokens, _) = ModelRegistry.EstimateCapabilities(model);
        return maxTokens;
    }
    
    private int GetContextWindowForModel(string model)
    {
        var modelInfo = ModelRegistry.FindModel("Ollama", model);
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
        
        var safetyMargin = 200;
        var availableTokens = contextWindow - estimatedInputTokens - safetyMargin;
        
        var desiredTokens = operationType switch
        {
            LlmOperationType.ShortForm => 512,
            LlmOperationType.Analytical => 1024,
            LlmOperationType.Creative => 1536,
            LlmOperationType.LongForm => 2048,
            LlmOperationType.Extraction => 768,
            _ => 1024
        };
        
        return Math.Min(Math.Max(availableTokens, 256), desiredTokens);
    }
    
    public override async Task<ProviderHealthResult> HealthCheckAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
        var startTime = DateTime.UtcNow;
        
        try
        {
            Logger.LogDebug("Performing health check for Ollama provider with model {Model}", _model);
            
            var modelExists = ModelRegistry.FindModel("Ollama", _model) != null;
            
            var elapsed = DateTime.UtcNow - startTime;
            
            if (!modelExists)
            {
                Logger.LogWarning("Model {Model} not found in registry during health check", _model);
                return new ProviderHealthResult
                {
                    IsHealthy = false,
                    ResponseTimeMs = elapsed.TotalMilliseconds,
                    ErrorMessage = $"Model {_model} not found in registry",
                    Details = "Model may not be available or configured correctly. Ensure Ollama is running and model is pulled."
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
            Logger.LogError(ex, "Health check failed for Ollama provider");
            
            return new ProviderHealthResult
            {
                IsHealthy = false,
                ResponseTimeMs = elapsed.TotalMilliseconds,
                ErrorMessage = ex.Message,
                Details = "Health check exception occurred. Ensure Ollama service is running."
            };
        }
    }
}
