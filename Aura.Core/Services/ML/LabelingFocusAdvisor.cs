using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Orchestration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ML;

/// <summary>
/// LLM-assisted service to provide intelligent guidance on which frames to annotate
/// for maximizing training data quality and diversity
/// </summary>
public class LabelingFocusAdvisor
{
    private readonly ILogger<LabelingFocusAdvisor> _logger;
    private readonly UnifiedLlmOrchestrator? _llmOrchestrator;

    public LabelingFocusAdvisor(
        ILogger<LabelingFocusAdvisor> logger,
        UnifiedLlmOrchestrator? llmOrchestrator = null)
    {
        _logger = logger;
        _llmOrchestrator = llmOrchestrator;
    }

    /// <summary>
    /// Analyze existing annotations and provide suggestions for improving dataset
    /// </summary>
    public async Task<LabelingAdviceResult> GetLabelingAdviceAsync(
        List<AnnotationRecord> existingAnnotations,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating labeling advice for {Count} existing annotations", 
            existingAnnotations.Count);

        var result = new LabelingAdviceResult
        {
            TotalAnnotations = existingAnnotations.Count
        };

        try
        {
            // Analyze annotation distribution
            AnalyzeDistribution(existingAnnotations, result);

            // Generate recommendations based on distribution analysis
            if (existingAnnotations.Count > 0)
            {
                GenerateAdviceFromDistribution(result);
            }
            else
            {
                result.Recommendations.Add("Start by annotating a diverse set of frames from different scenes");
                result.Recommendations.Add("Include frames with varying visual complexity (simple, medium, detailed)");
                result.Recommendations.Add("Annotate frames from beginning, middle, and end of videos");
                result.FocusAreas.Add("Scene transitions and key moments");
                result.FocusAreas.Add("Frames with clear subjects or actions");
                result.FocusAreas.Add("Visually distinct frames (avoid near-duplicates)");
            }

            _logger.LogInformation("Labeling advice generated with {RecCount} recommendations", 
                result.Recommendations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate labeling advice");
            result.Recommendations.Add("Continue annotating a diverse set of frames");
        }

        return result;
    }

    private void AnalyzeDistribution(List<AnnotationRecord> annotations, LabelingAdviceResult result)
    {
        if (annotations.Count == 0)
        {
            return;
        }

        var ratings = annotations.Select(a => a.Rating).ToList();
        
        result.AverageRating = ratings.Average();
        result.MinRating = ratings.Min();
        result.MaxRating = ratings.Max();

        // Calculate distribution buckets
        var lowCount = ratings.Count(r => r < 0.3);
        var mediumCount = ratings.Count(r => r >= 0.3 && r < 0.7);
        var highCount = ratings.Count(r => r >= 0.7);

        result.RatingDistribution = new Dictionary<string, int>
        {
            ["Low (0-0.3)"] = lowCount,
            ["Medium (0.3-0.7)"] = mediumCount,
            ["High (0.7-1.0)"] = highCount
        };

        // Identify imbalances
        var total = annotations.Count;
        var lowPercent = (lowCount * 100.0) / total;
        var mediumPercent = (mediumCount * 100.0) / total;
        var highPercent = (highCount * 100.0) / total;

        if (lowPercent < 20)
        {
            result.Warnings.Add($"Low-importance frames underrepresented ({lowPercent:F0}%) - annotate more simple or less critical frames");
        }
        if (highPercent < 20)
        {
            result.Warnings.Add($"High-importance frames underrepresented ({highPercent:F0}%) - annotate more visually striking or key frames");
        }
        if (Math.Abs(lowPercent - highPercent) > 40)
        {
            result.Warnings.Add("Rating distribution is heavily skewed - aim for more balanced representation");
        }

        _logger.LogDebug("Rating distribution: Low={Low}%, Medium={Medium}%, High={High}%",
            lowPercent, mediumPercent, highPercent);
    }

    private void GenerateAdviceFromDistribution(LabelingAdviceResult result)
    {
        // Generate targeted recommendations based on the current distribution
        var lowCount = result.RatingDistribution.GetValueOrDefault("Low (0-0.3)", 0);
        var mediumCount = result.RatingDistribution.GetValueOrDefault("Medium (0.3-0.7)", 0);
        var highCount = result.RatingDistribution.GetValueOrDefault("High (0.7-1.0)", 0);
        var total = result.TotalAnnotations;

        var lowPercent = (lowCount * 100.0) / total;
        var highPercent = (highCount * 100.0) / total;

        if (lowPercent < 25)
        {
            result.Recommendations.Add("Annotate more low-importance frames (simple backgrounds, static scenes, less critical moments) to balance your dataset");
            result.FocusAreas.Add("Simple background frames");
            result.FocusAreas.Add("Static or minimal-motion scenes");
        }

        if (highPercent < 25)
        {
            result.Recommendations.Add("Annotate more high-importance frames (key moments, visual peaks, scene highlights) for better model accuracy");
            result.FocusAreas.Add("Scene transitions and key moments");
            result.FocusAreas.Add("Visually striking or detailed frames");
        }

        result.Recommendations.Add("Ensure annotations cover frames from beginning, middle, and end of videos");
        result.Recommendations.Add("Include frames with varying lighting conditions and visual complexity");
        result.Recommendations.Add("Avoid annotating many consecutive similar frames - spread annotations across different scenes");
        
        result.FocusAreas.Add("Diverse scene types");
        result.FocusAreas.Add("Various visual characteristics");
    }
}

/// <summary>
/// Result of labeling advice analysis
/// </summary>
public class LabelingAdviceResult
{
    public int TotalAnnotations { get; set; }
    public double AverageRating { get; set; }
    public double MinRating { get; set; }
    public double MaxRating { get; set; }
    public Dictionary<string, int> RatingDistribution { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> FocusAreas { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
