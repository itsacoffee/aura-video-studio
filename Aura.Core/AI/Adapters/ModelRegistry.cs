using System;
using System.Collections.Generic;
using System.Linq;

namespace Aura.Core.AI.Adapters;

/// <summary>
/// Registry of known LLM models with their capabilities.
/// This should be updated periodically as providers release new models.
/// </summary>
public static class ModelRegistry
{
    /// <summary>
    /// Model information entry
    /// </summary>
    public record ModelInfo
    {
        public required string Provider { get; init; }
        public required string ModelId { get; init; }
        public required int MaxTokens { get; init; }
        public required int ContextWindow { get; init; }
        public string[]? Aliases { get; init; }
        public DateTime? DeprecationDate { get; init; }
        public string? ReplacementModel { get; init; }
    }
    
    /// <summary>
    /// All registered models. This list should be updated as providers change their offerings.
    /// </summary>
    private static readonly List<ModelInfo> _models = new()
    {
        new ModelInfo 
        { 
            Provider = "OpenAI", 
            ModelId = "gpt-4o",
            MaxTokens = 128000,
            ContextWindow = 128000,
            Aliases = new[] { "gpt-4o-latest" }
        },
        new ModelInfo 
        { 
            Provider = "OpenAI", 
            ModelId = "gpt-4o-mini",
            MaxTokens = 128000,
            ContextWindow = 128000,
            Aliases = new[] { "gpt-4o-mini-latest" }
        },
        new ModelInfo 
        { 
            Provider = "OpenAI", 
            ModelId = "gpt-4-turbo",
            MaxTokens = 128000,
            ContextWindow = 128000,
            Aliases = new[] { "gpt-4-turbo-preview" }
        },
        new ModelInfo 
        { 
            Provider = "OpenAI", 
            ModelId = "gpt-4",
            MaxTokens = 8192,
            ContextWindow = 8192,
            Aliases = new[] { "gpt-4-0613" }
        },
        new ModelInfo 
        { 
            Provider = "OpenAI", 
            ModelId = "gpt-3.5-turbo",
            MaxTokens = 4096,
            ContextWindow = 16384,
            Aliases = new[] { "gpt-3.5-turbo-0125" }
        },
        
        new ModelInfo 
        { 
            Provider = "Anthropic", 
            ModelId = "claude-3-5-sonnet-20241022",
            MaxTokens = 8192,
            ContextWindow = 200000,
            Aliases = new[] { "claude-3-5-sonnet", "claude-3.5-sonnet" }
        },
        new ModelInfo 
        { 
            Provider = "Anthropic", 
            ModelId = "claude-3-opus-20240229",
            MaxTokens = 4096,
            ContextWindow = 200000,
            Aliases = new[] { "claude-3-opus" }
        },
        new ModelInfo 
        { 
            Provider = "Anthropic", 
            ModelId = "claude-3-sonnet-20240229",
            MaxTokens = 4096,
            ContextWindow = 200000,
            Aliases = new[] { "claude-3-sonnet" }
        },
        new ModelInfo 
        { 
            Provider = "Anthropic", 
            ModelId = "claude-3-haiku-20240307",
            MaxTokens = 4096,
            ContextWindow = 200000,
            Aliases = new[] { "claude-3-haiku" }
        },
        
        new ModelInfo 
        { 
            Provider = "Gemini", 
            ModelId = "gemini-1.5-pro",
            MaxTokens = 8192,
            ContextWindow = 2097152,
            Aliases = new[] { "gemini-1.5-pro-latest" }
        },
        new ModelInfo 
        { 
            Provider = "Gemini", 
            ModelId = "gemini-1.5-flash",
            MaxTokens = 8192,
            ContextWindow = 1048576,
            Aliases = new[] { "gemini-1.5-flash-latest" }
        },
        new ModelInfo 
        { 
            Provider = "Gemini", 
            ModelId = "gemini-pro",
            MaxTokens = 2048,
            ContextWindow = 32768
        },
        
        new ModelInfo 
        { 
            Provider = "Azure", 
            ModelId = "gpt-4o",
            MaxTokens = 128000,
            ContextWindow = 128000
        },
        new ModelInfo 
        { 
            Provider = "Azure", 
            ModelId = "gpt-4-turbo",
            MaxTokens = 128000,
            ContextWindow = 128000
        },
        new ModelInfo 
        { 
            Provider = "Azure", 
            ModelId = "gpt-4",
            MaxTokens = 8192,
            ContextWindow = 8192
        },
        new ModelInfo 
        { 
            Provider = "Azure", 
            ModelId = "gpt-35-turbo",
            MaxTokens = 4096,
            ContextWindow = 16384,
            Aliases = new[] { "gpt-3.5-turbo" }
        }
    };
    
    /// <summary>
    /// Pattern-based model detection for Ollama and other local models
    /// </summary>
    private static readonly Dictionary<string, Func<string, ModelInfo?>> _patternDetectors = new()
    {
        ["llama"] = modelName =>
        {
            if (modelName.Contains("3.1", StringComparison.OrdinalIgnoreCase))
                return new ModelInfo { Provider = "Ollama", ModelId = modelName, MaxTokens = 128000, ContextWindow = 128000 };
            if (modelName.Contains("3", StringComparison.OrdinalIgnoreCase))
                return new ModelInfo { Provider = "Ollama", ModelId = modelName, MaxTokens = 8192, ContextWindow = 8192 };
            if (modelName.Contains("2", StringComparison.OrdinalIgnoreCase))
                return new ModelInfo { Provider = "Ollama", ModelId = modelName, MaxTokens = 4096, ContextWindow = 4096 };
            return new ModelInfo { Provider = "Ollama", ModelId = modelName, MaxTokens = 4096, ContextWindow = 4096 };
        },
        ["mistral"] = modelName => 
            new ModelInfo { Provider = "Ollama", ModelId = modelName, MaxTokens = 8192, ContextWindow = 8192 },
        ["phi"] = modelName => 
            new ModelInfo { Provider = "Ollama", ModelId = modelName, MaxTokens = 4096, ContextWindow = 4096 },
        ["gemma"] = modelName => 
            new ModelInfo { Provider = "Ollama", ModelId = modelName, MaxTokens = 8192, ContextWindow = 8192 },
        ["codellama"] = modelName => 
            new ModelInfo { Provider = "Ollama", ModelId = modelName, MaxTokens = 16384, ContextWindow = 16384 }
    };
    
    /// <summary>
    /// Try to find a model by name or alias
    /// </summary>
    public static ModelInfo? FindModel(string provider, string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return null;
        
        var lowerModelName = modelName.ToLowerInvariant();
        
        var exactMatch = _models.FirstOrDefault(m => 
            m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) &&
            m.ModelId.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
            return exactMatch;
        
        var aliasMatch = _models.FirstOrDefault(m => 
            m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) &&
            m.Aliases != null &&
            m.Aliases.Any(a => a.Equals(modelName, StringComparison.OrdinalIgnoreCase)));
        
        if (aliasMatch != null)
            return aliasMatch;
        
        if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var (pattern, detector) in _patternDetectors)
            {
                if (lowerModelName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return detector(modelName);
                }
            }
            
            return new ModelInfo 
            { 
                Provider = "Ollama", 
                ModelId = modelName, 
                MaxTokens = 4096, 
                ContextWindow = 4096 
            };
        }
        
        return null;
    }
    
    /// <summary>
    /// Get default model for a provider
    /// </summary>
    public static string GetDefaultModel(string provider)
    {
        return provider switch
        {
            "OpenAI" => "gpt-4o-mini",
            "Anthropic" => "claude-3-5-sonnet-20241022",
            "Gemini" => "gemini-1.5-pro",
            "Azure" => "gpt-4o",
            "Ollama" => "llama3.1",
            _ => "unknown"
        };
    }
    
    /// <summary>
    /// Get all models for a provider
    /// </summary>
    public static IEnumerable<ModelInfo> GetModelsForProvider(string provider)
    {
        return _models.Where(m => m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Estimate model capabilities from model name patterns when not in registry
    /// </summary>
    public static (int maxTokens, int contextWindow) EstimateCapabilities(string modelName)
    {
        var lower = modelName.ToLowerInvariant();
        
        if (lower.Contains("128k") || lower.Contains("gpt-4o"))
            return (128000, 128000);
        
        if (lower.Contains("32k"))
            return (32000, 32000);
        
        if (lower.Contains("16k"))
            return (16000, 16000);
        
        if (lower.Contains("turbo"))
            return (4096, 16384);
        
        if (lower.Contains("gpt-4"))
            return (8192, 8192);
        
        if (lower.Contains("gpt-3"))
            return (4096, 4096);
        
        if (lower.Contains("claude-3"))
            return (4096, 200000);
        
        if (lower.Contains("gemini-1.5"))
            return (8192, 1000000);
        
        if (lower.Contains("gemini"))
            return (2048, 32768);
        
        return (4096, 4096);
    }
}
