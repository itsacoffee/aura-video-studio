using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Orchestration;

/// <summary>
/// Optimization request for LLM parameters
/// </summary>
public record OptimizationRequest
{
    /// <summary>
    /// Type of operation to optimize for
    /// </summary>
    public required LlmOperationType OperationType { get; init; }
    
    /// <summary>
    /// Constraints to optimize within
    /// </summary>
    public OptimizationConstraints? Constraints { get; init; }
    
    /// <summary>
    /// Brief description of the use case
    /// </summary>
    public string? UseCase { get; init; }
}

/// <summary>
/// Constraints for parameter optimization
/// </summary>
public record OptimizationConstraints
{
    /// <summary>
    /// Maximum tokens allowed
    /// </summary>
    public int? MaxTokens { get; init; }
    
    /// <summary>
    /// Maximum cost allowed (USD)
    /// </summary>
    public decimal? MaxCost { get; init; }
    
    /// <summary>
    /// Maximum latency allowed (seconds)
    /// </summary>
    public int? MaxLatencySeconds { get; init; }
    
    /// <summary>
    /// Prioritize quality over speed
    /// </summary>
    public bool PrioritizeQuality { get; init; } = true;
}

/// <summary>
/// Optimized parameter suggestion
/// </summary>
public record OptimizationSuggestion
{
    /// <summary>
    /// Suggested temperature
    /// </summary>
    public double Temperature { get; init; }
    
    /// <summary>
    /// Suggested top-p value
    /// </summary>
    public double TopP { get; init; }
    
    /// <summary>
    /// Suggested max tokens
    /// </summary>
    public int MaxTokens { get; init; }
    
    /// <summary>
    /// Suggested timeout (seconds)
    /// </summary>
    public int TimeoutSeconds { get; init; }
    
    /// <summary>
    /// Suggested max retries
    /// </summary>
    public int MaxRetries { get; init; }
    
    /// <summary>
    /// Rationale for the suggestions
    /// </summary>
    public required string Rationale { get; init; }
    
    /// <summary>
    /// Confidence level (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; init; }
}

/// <summary>
/// Optimizes LLM parameters based on operation type and constraints
/// </summary>
public class LlmParameterOptimizer
{
    private readonly ILogger<LlmParameterOptimizer> _logger;
    
    public LlmParameterOptimizer(ILogger<LlmParameterOptimizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Suggests optimal parameters for an operation using LLM analysis
    /// </summary>
    public async Task<OptimizationSuggestion> OptimizeAsync(
        OptimizationRequest request,
        ILlmProvider? llmProvider = null,
        CancellationToken ct = default)
    {
        if (llmProvider == null)
        {
            return GetRuleBasedOptimization(request);
        }
        
        try
        {
            var prompt = BuildOptimizationPrompt(request);
            var response = await llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            
            var suggestion = ParseOptimizationResponse(response, request);
            
            _logger.LogInformation(
                "LLM-based optimization for {OperationType}: temp={Temperature}, tokens={MaxTokens}",
                request.OperationType, suggestion.Temperature, suggestion.MaxTokens);
            
            return suggestion;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM-based optimization failed, falling back to rules");
            return GetRuleBasedOptimization(request);
        }
    }
    
    /// <summary>
    /// Explains adjustments made to a preset based on constraints
    /// </summary>
    public async Task<string> ExplainAdjustmentsAsync(
        LlmOperationPreset basePreset,
        LlmOperationPreset adjustedPreset,
        string reason,
        ILlmProvider? llmProvider = null,
        CancellationToken ct = default)
    {
        if (llmProvider == null)
        {
            return GenerateRuleBasedExplanation(basePreset, adjustedPreset, reason);
        }
        
        try
        {
            var prompt = BuildExplanationPrompt(basePreset, adjustedPreset, reason);
            var response = await llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            
            return ExtractExplanation(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM explanation failed, using rule-based");
            return GenerateRuleBasedExplanation(basePreset, adjustedPreset, reason);
        }
    }
    
    private OptimizationSuggestion GetRuleBasedOptimization(OptimizationRequest request)
    {
        var basePreset = LlmOperationPresets.GetPreset(request.OperationType);
        var constraints = request.Constraints ?? new OptimizationConstraints();
        
        var temperature = basePreset.Temperature;
        var topP = basePreset.TopP;
        var maxTokens = basePreset.MaxTokens;
        var timeoutSeconds = basePreset.TimeoutSeconds;
        var maxRetries = basePreset.MaxRetries;
        
        var rationale = $"Using default preset for {request.OperationType}";
        
        if (constraints.MaxTokens.HasValue && maxTokens > constraints.MaxTokens.Value)
        {
            maxTokens = constraints.MaxTokens.Value;
            rationale += $". Token limit reduced to {maxTokens} per constraint";
        }
        
        if (constraints.MaxLatencySeconds.HasValue && timeoutSeconds > constraints.MaxLatencySeconds.Value)
        {
            timeoutSeconds = constraints.MaxLatencySeconds.Value;
            maxRetries = Math.Max(1, maxRetries - 1);
            rationale += $". Timeout reduced to {timeoutSeconds}s, retries to {maxRetries}";
        }
        
        if (constraints.PrioritizeQuality && temperature > 0.5)
        {
            temperature = Math.Max(0.3, temperature - 0.2);
            rationale += $". Temperature reduced to {temperature:F1} for quality";
        }
        else if (!constraints.PrioritizeQuality && temperature < 0.7)
        {
            temperature = Math.Min(0.9, temperature + 0.2);
            rationale += $". Temperature increased to {temperature:F1} for creativity";
        }
        
        return new OptimizationSuggestion
        {
            Temperature = temperature,
            TopP = topP,
            MaxTokens = maxTokens,
            TimeoutSeconds = timeoutSeconds,
            MaxRetries = maxRetries,
            Rationale = rationale,
            Confidence = 0.85
        };
    }
    
    private static string BuildOptimizationPrompt(OptimizationRequest request)
    {
        var constraints = request.Constraints ?? new OptimizationConstraints();
        
        return $@"You are an expert in LLM parameter optimization. Suggest optimal parameters for the following:

Operation Type: {request.OperationType}
Use Case: {request.UseCase ?? "General purpose"}

Constraints:
- Max Tokens: {constraints.MaxTokens?.ToString() ?? "No limit"}
- Max Cost: ${constraints.MaxCost?.ToString() ?? "No limit"}
- Max Latency: {constraints.MaxLatencySeconds?.ToString() ?? "No limit"}s
- Priority: {(constraints.PrioritizeQuality ? "Quality" : "Speed/Creativity")}

Respond with a JSON object containing:
- temperature (0.0-1.0)
- topP (0.0-1.0)
- maxTokens (integer)
- timeoutSeconds (integer)
- maxRetries (1-5)
- rationale (brief explanation)
- confidence (0.0-1.0)

Example:
{{
  ""temperature"": 0.7,
  ""topP"": 0.9,
  ""maxTokens"": 2000,
  ""timeoutSeconds"": 60,
  ""maxRetries"": 3,
  ""rationale"": ""Balanced parameters for creative content generation"",
  ""confidence"": 0.9
}}";
    }
    
    private OptimizationSuggestion ParseOptimizationResponse(string response, OptimizationRequest request)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;
                
                return new OptimizationSuggestion
                {
                    Temperature = root.GetProperty("temperature").GetDouble(),
                    TopP = root.GetProperty("topP").GetDouble(),
                    MaxTokens = root.GetProperty("maxTokens").GetInt32(),
                    TimeoutSeconds = root.GetProperty("timeoutSeconds").GetInt32(),
                    MaxRetries = root.GetProperty("maxRetries").GetInt32(),
                    Rationale = root.GetProperty("rationale").GetString() ?? "LLM-optimized parameters",
                    Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.8
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM optimization response");
        }
        
        return GetRuleBasedOptimization(request);
    }
    
    private static string BuildExplanationPrompt(
        LlmOperationPreset basePreset,
        LlmOperationPreset adjustedPreset,
        string reason)
    {
        return $@"Explain the following parameter adjustments for LLM operations in 1-2 sentences:

Base Parameters:
- Temperature: {basePreset.Temperature}
- Max Tokens: {basePreset.MaxTokens}
- Timeout: {basePreset.TimeoutSeconds}s

Adjusted Parameters:
- Temperature: {adjustedPreset.Temperature}
- Max Tokens: {adjustedPreset.MaxTokens}
- Timeout: {adjustedPreset.TimeoutSeconds}s

Reason for adjustment: {reason}

Provide a concise, user-friendly explanation of why these changes improve the operation.";
    }
    
    private static string ExtractExplanation(string response)
    {
        var lines = response.Split('\n');
        var explanation = string.Empty;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 20 && !trimmed.StartsWith("Base") && !trimmed.StartsWith("Adjusted"))
            {
                explanation += trimmed + " ";
            }
        }
        
        return explanation.Trim();
    }
    
    private static string GenerateRuleBasedExplanation(
        LlmOperationPreset basePreset,
        LlmOperationPreset adjustedPreset,
        string reason)
    {
        var changes = new System.Collections.Generic.List<string>();
        
        if (Math.Abs(basePreset.Temperature - adjustedPreset.Temperature) > 0.05)
        {
            if (adjustedPreset.Temperature < basePreset.Temperature)
            {
                changes.Add("reduced temperature for more consistent outputs");
            }
            else
            {
                changes.Add("increased temperature for more creative variation");
            }
        }
        
        if (basePreset.MaxTokens != adjustedPreset.MaxTokens)
        {
            if (adjustedPreset.MaxTokens < basePreset.MaxTokens)
            {
                changes.Add("reduced token limit to control costs");
            }
            else
            {
                changes.Add("increased token limit for more detailed responses");
            }
        }
        
        if (basePreset.TimeoutSeconds != adjustedPreset.TimeoutSeconds)
        {
            if (adjustedPreset.TimeoutSeconds < basePreset.TimeoutSeconds)
            {
                changes.Add("shortened timeout for faster failure detection");
            }
            else
            {
                changes.Add("extended timeout for complex operations");
            }
        }
        
        if (changes.Count == 0)
        {
            return $"Parameters optimized for {reason}";
        }
        
        return $"Adjusted parameters to {string.Join(", ", changes)} based on {reason}.";
    }
}
