using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Aura.Core.AI.Cache;

/// <summary>
/// Generates deterministic cache keys for LLM requests
/// </summary>
public static class LlmCacheKeyGenerator
{
    private const double MaxCacheableTemperature = 0.3;
    
    private static readonly HashSet<string> CacheableOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "PlanScaffold",
        "OutlineTransform",
        "SceneAnalysis",
        "ContentComplexity",
        "SceneCoherence",
        "NarrativeArc",
        "VisualPrompt",
        "TransitionText"
    };
    
    /// <summary>
    /// Checks if an operation type is cacheable
    /// </summary>
    /// <param name="operationType">Operation type to check</param>
    /// <returns>True if operation is cacheable</returns>
    public static bool IsCacheable(string? operationType)
    {
        if (string.IsNullOrWhiteSpace(operationType))
        {
            return false;
        }
        
        return CacheableOperations.Contains(operationType);
    }
    
    /// <summary>
    /// Checks if temperature is suitable for caching
    /// </summary>
    /// <param name="temperature">Temperature value</param>
    /// <returns>True if temperature is suitable for caching</returns>
    public static bool IsTemperatureSuitable(double temperature)
    {
        return temperature <= MaxCacheableTemperature;
    }
    
    /// <summary>
    /// Generates a cache key from LLM request parameters
    /// </summary>
    /// <param name="providerName">Provider name (e.g., "OpenAI")</param>
    /// <param name="modelName">Model name (e.g., "gpt-4")</param>
    /// <param name="operationType">Operation type (e.g., "PlanScaffold")</param>
    /// <param name="systemPrompt">System prompt (optional)</param>
    /// <param name="userPrompt">User prompt</param>
    /// <param name="temperature">Temperature parameter</param>
    /// <param name="maxTokens">Max tokens parameter</param>
    /// <param name="additionalParams">Additional parameters (optional)</param>
    /// <returns>SHA256 hash as cache key</returns>
    public static string GenerateKey(
        string providerName,
        string modelName,
        string operationType,
        string? systemPrompt,
        string userPrompt,
        double temperature,
        int maxTokens,
        Dictionary<string, object>? additionalParams = null)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentNullException(nameof(providerName));
        }
        
        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentNullException(nameof(modelName));
        }
        
        if (string.IsNullOrWhiteSpace(operationType))
        {
            throw new ArgumentNullException(nameof(operationType));
        }
        
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            throw new ArgumentNullException(nameof(userPrompt));
        }
        
        var sb = new StringBuilder();
        
        sb.Append(providerName);
        sb.Append('|');
        sb.Append(modelName);
        sb.Append('|');
        sb.Append(operationType);
        sb.Append('|');
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            sb.Append(NormalizePrompt(systemPrompt));
            sb.Append('|');
        }
        
        sb.Append(NormalizePrompt(userPrompt));
        sb.Append('|');
        sb.Append(temperature.ToString("F2"));
        sb.Append('|');
        sb.Append(maxTokens);
        
        if (additionalParams != null && additionalParams.Count > 0)
        {
            var sortedParams = additionalParams
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}={kvp.Value}");
            
            sb.Append('|');
            sb.Append(string.Join(",", sortedParams));
        }
        
        var input = sb.ToString();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    /// <summary>
    /// Normalizes a prompt for consistent cache key generation
    /// </summary>
    private static string NormalizePrompt(string prompt)
    {
        return prompt.Trim().ToLowerInvariant();
    }
}
