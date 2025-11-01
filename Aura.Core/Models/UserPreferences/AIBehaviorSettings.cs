using System;
using System.Collections.Generic;

namespace Aura.Core.Models.UserPreferences;

/// <summary>
/// AI behavior settings for controlling LLM parameters and prompts
/// Allows per-stage customization of AI generation
/// </summary>
public class AIBehaviorSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Script Generation Parameters
    public LLMStageParameters ScriptGeneration { get; set; } = new();
    
    // Scene Description Parameters
    public LLMStageParameters SceneDescription { get; set; } = new();
    
    // Content Optimization Parameters
    public LLMStageParameters ContentOptimization { get; set; } = new();
    
    // Translation Parameters
    public LLMStageParameters Translation { get; set; } = new();
    
    // Quality Analysis Parameters
    public LLMStageParameters QualityAnalysis { get; set; } = new();
    
    // Global Settings
    public double CreativityVsAdherence { get; set; } = 0.5; // 0=strict adherence, 1=full creativity
    public bool EnableChainOfThought { get; set; } = false;
    public bool ShowPromptsBeforeSending { get; set; } = false; // Review mode
    
    // Metadata
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// LLM parameters for a specific pipeline stage
/// </summary>
public class LLMStageParameters
{
    public string StageName { get; set; } = string.Empty;
    
    // Standard LLM Parameters
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 0.9;
    public double FrequencyPenalty { get; set; } = 0.0;
    public double PresencePenalty { get; set; } = 0.0;
    public int MaxTokens { get; set; } = 2000;
    
    // Custom System Prompt
    public string? CustomSystemPrompt { get; set; }
    
    // Preferred Model (can override global)
    public string? PreferredModel { get; set; }
    
    // Validation Strictness (for quality analysis stage)
    public double StrictnessLevel { get; set; } = 0.5; // 0=lenient, 1=very strict
}

/// <summary>
/// Custom prompt template with variable placeholders
/// </summary>
public class CustomPromptTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty; // ScriptGeneration, SceneDescription, etc.
    public string TemplateText { get; set; } = string.Empty;
    public List<string> Variables { get; set; } = new(); // {{topic}}, {{audience}}, {{duration}}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // A/B Testing Support
    public string? VariantGroup { get; set; }
    public int SuccessCount { get; set; }
    public int TotalUses { get; set; }
    public double SuccessRate => TotalUses > 0 ? (double)SuccessCount / TotalUses : 0.0;
    
    // Metadata
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsFavorite { get; set; }
}
