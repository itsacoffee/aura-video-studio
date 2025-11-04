using System.Collections.Generic;

namespace Aura.Core.AI.Cache;

/// <summary>
/// Configuration options for LLM cache
/// </summary>
public class LlmCacheOptions
{
    /// <summary>
    /// Enable or disable caching globally
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Maximum number of entries in memory cache
    /// </summary>
    public int MaxEntries { get; set; } = 1000;
    
    /// <summary>
    /// Default time-to-live for cache entries in seconds
    /// </summary>
    public int DefaultTtlSeconds { get; set; } = 3600;
    
    /// <summary>
    /// Enable disk-based storage for overflow
    /// </summary>
    public bool UseDiskStorage { get; set; } = false;
    
    /// <summary>
    /// Directory path for disk cache
    /// </summary>
    public string DiskStoragePath { get; set; } = "./cache/llm";
    
    /// <summary>
    /// Maximum disk cache size in MB
    /// </summary>
    public int MaxDiskSizeMB { get; set; } = 100;
    
    /// <summary>
    /// Memory threshold percentage (0-100) to trigger circuit breaker
    /// </summary>
    public int MemoryThresholdPercent { get; set; } = 85;
}

/// <summary>
/// Configuration options for LLM prewarming
/// </summary>
public class LlmPrewarmOptions
{
    /// <summary>
    /// Enable or disable prewarming
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Maximum concurrent prewarm operations
    /// </summary>
    public int MaxConcurrentPrewarms { get; set; } = 3;
    
    /// <summary>
    /// List of prompts to prewarm on startup
    /// </summary>
    public List<PrewarmPrompt> PrewarmPrompts { get; set; } = new();
}

/// <summary>
/// Configuration for a single prewarm prompt
/// </summary>
public class PrewarmPrompt
{
    public required string ProviderName { get; init; }
    public required string ModelName { get; init; }
    public required string OperationType { get; init; }
    public string? SystemPrompt { get; init; }
    public required string UserPrompt { get; init; }
    public double Temperature { get; init; } = 0.2;
    public int MaxTokens { get; init; } = 1000;
    public int TtlSeconds { get; init; } = 7200;
}
