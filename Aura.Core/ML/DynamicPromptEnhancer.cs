using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Models.Settings;
using Microsoft.Extensions.Logging;

namespace Aura.Core.ML;

/// <summary>
/// ML-powered prompt enhancement engine that dynamically adjusts LLM prompts
/// based on historical performance and user preferences
/// </summary>
public class DynamicPromptEnhancer
{
    private readonly ILogger<DynamicPromptEnhancer> _logger;
    private readonly Dictionary<string, PromptOptimization> _optimizations = new();

    public DynamicPromptEnhancer(ILogger<DynamicPromptEnhancer> logger)
    {
        _logger = logger;
        InitializeDefaultOptimizations();
    }

    /// <summary>
    /// Enhance a prompt based on optimization settings and historical performance
    /// </summary>
    public async Task<EnhancedPrompt> EnhancePromptAsync(
        string originalPrompt,
        Brief brief,
        PlanSpec spec,
        AIOptimizationSettings settings,
        Dictionary<string, double>? historicalPerformance = null,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false); // For async interface consistency

        if (!settings.Enabled)
        {
            // Optimization disabled, return original
            return new EnhancedPrompt
            {
                Prompt = originalPrompt,
                Applied = false,
                Enhancements = new List<string> { "Optimization disabled by user" }
            };
        }

        var sb = new StringBuilder(originalPrompt);
        var enhancements = new List<string>();

        // Apply level-based enhancements
        switch (settings.Level)
        {
            case OptimizationLevel.Conservative:
                ApplyConservativeEnhancements(sb, brief, spec, enhancements);
                break;
            case OptimizationLevel.Balanced:
                ApplyBalancedEnhancements(sb, brief, spec, enhancements);
                break;
            case OptimizationLevel.Aggressive:
                ApplyAggressiveEnhancements(sb, brief, spec, enhancements, historicalPerformance);
                break;
        }

        // Apply metric-specific enhancements
        ApplyMetricEnhancements(sb, settings.OptimizationMetrics, enhancements);

        var enhancedPrompt = sb.ToString();

        _logger.LogInformation(
            "Enhanced prompt with {Count} modifications (level: {Level})",
            enhancements.Count, settings.Level);

        return new EnhancedPrompt
        {
            Prompt = enhancedPrompt,
            Applied = true,
            Enhancements = enhancements,
            OptimizationLevel = settings.Level.ToString()
        };
    }

    /// <summary>
    /// Apply conservative enhancements (minimal changes)
    /// </summary>
    private void ApplyConservativeEnhancements(
        StringBuilder prompt,
        Brief brief,
        PlanSpec spec,
        List<string> enhancements)
    {
        // Only add clarifications without changing core structure
        if (brief.Tone.Contains("professional", StringComparison.OrdinalIgnoreCase))
        {
            prompt.AppendLine("\nEnsure professional terminology and industry-appropriate language.");
            enhancements.Add("Added professional language guidance");
        }
    }

    /// <summary>
    /// Apply balanced enhancements (moderate optimization)
    /// </summary>
    private void ApplyBalancedEnhancements(
        StringBuilder prompt,
        Brief brief,
        PlanSpec spec,
        List<string> enhancements)
    {
        // Add engagement boosters
        prompt.AppendLine("\nFocus on clear value delivery and audience engagement throughout.");
        enhancements.Add("Added engagement focus");

        // Add pacing guidance based on spec
        if (spec.Pacing == Pacing.Fast)
        {
            prompt.AppendLine("Maintain high energy and momentum. Use shorter sentences and active voice.");
            enhancements.Add("Optimized for fast pacing");
        }
        else if (spec.Pacing == Pacing.Chill)
        {
            prompt.AppendLine("Allow space for reflection. Use a measured, contemplative tone.");
            enhancements.Add("Optimized for relaxed pacing");
        }

        // Add authenticity reminder
        prompt.AppendLine("Ensure content feels natural and human, not AI-generated.");
        enhancements.Add("Added authenticity emphasis");
    }

    /// <summary>
    /// Apply aggressive enhancements (maximum optimization)
    /// </summary>
    private void ApplyAggressiveEnhancements(
        StringBuilder prompt,
        Brief brief,
        PlanSpec spec,
        List<string> enhancements,
        Dictionary<string, double>? historicalPerformance)
    {
        // Include all balanced enhancements
        ApplyBalancedEnhancements(prompt, brief, spec, enhancements);

        // Add historical performance insights
        if (historicalPerformance != null && historicalPerformance.Count > 0)
        {
            var bestTopic = historicalPerformance.OrderByDescending(kv => kv.Value).First();
            prompt.AppendLine($"\nHistorical data shows strong performance with {bestTopic.Key}-style content.");
            enhancements.Add($"Applied historical insights from {bestTopic.Key}");
        }

        // Add advanced quality controls
        prompt.AppendLine(@"
ADVANCED QUALITY CONTROLS:
- Every sentence must earn its place - no filler
- Use specific examples and concrete details
- Create clear narrative momentum
- Include strategic pattern interrupts
- Build to satisfying payoffs");
        enhancements.Add("Applied advanced quality controls");

        // Add metric-specific optimizations
        prompt.AppendLine("Optimize for maximum viewer retention and engagement metrics.");
        enhancements.Add("Added retention optimization");
    }

    /// <summary>
    /// Apply metric-specific enhancements
    /// </summary>
    private void ApplyMetricEnhancements(
        StringBuilder prompt,
        List<OptimizationMetric> metrics,
        List<string> enhancements)
    {
        if (metrics.Contains(OptimizationMetric.Engagement))
        {
            prompt.AppendLine("\nPrioritize viewer engagement: use hooks, curiosity gaps, and clear payoffs.");
            enhancements.Add("Engagement optimization");
        }

        if (metrics.Contains(OptimizationMetric.Quality))
        {
            prompt.AppendLine("Maintain high quality standards: verify facts, use precise language, avoid clichés.");
            enhancements.Add("Quality optimization");
        }

        if (metrics.Contains(OptimizationMetric.Authenticity))
        {
            prompt.AppendLine("Ensure authentic, human voice: avoid AI patterns, use natural language flow.");
            enhancements.Add("Authenticity optimization");
        }

        if (metrics.Contains(OptimizationMetric.Speed))
        {
            prompt.AppendLine("Optimize for efficient generation while maintaining quality thresholds.");
            enhancements.Add("Speed optimization");
        }
    }

    /// <summary>
    /// Record prompt performance for future optimization
    /// </summary>
    public void RecordPromptPerformance(
        string promptType,
        double qualityScore,
        string optimizationLevel)
    {
        var key = $"{promptType}_{optimizationLevel}";
        
        if (!_optimizations.TryGetValue(key, out var value))
        {
            value = new PromptOptimization
            {
                PromptType = promptType,
                Level = optimizationLevel,
                Scores = new List<double>()
            };
            _optimizations[key] = value;
        }

        value.Scores.Add(qualityScore);

        // Keep only recent scores
        if (value.Scores.Count > 50)
        {
            value.Scores.RemoveAt(0);
        }

        _logger.LogDebug(
            "Recorded performance for {PromptType} ({Level}): {Score:F1}",
            promptType, optimizationLevel, qualityScore);
    }

    /// <summary>
    /// Initialize default optimization strategies
    /// </summary>
    private void InitializeDefaultOptimizations()
    {
        // Placeholder for preloaded optimization strategies
        // In a full implementation, this would load from trained models
    }
}

/// <summary>
/// Enhanced prompt with applied optimizations
/// </summary>
public record EnhancedPrompt
{
    public string Prompt { get; init; } = string.Empty;
    public bool Applied { get; init; }
    public List<string> Enhancements { get; init; } = new();
    public string? OptimizationLevel { get; init; }
}

/// <summary>
/// Prompt optimization tracking
/// </summary>
internal record PromptOptimization
{
    public string PromptType { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public List<double> Scores { get; init; } = new();
}
