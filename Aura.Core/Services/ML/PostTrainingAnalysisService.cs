using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Orchestration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ML;

/// <summary>
/// LLM-assisted service to analyze training results and provide recommendations
/// for accepting or reverting the newly trained model
/// </summary>
public class PostTrainingAnalysisService
{
    private readonly ILogger<PostTrainingAnalysisService> _logger;
    private readonly UnifiedLlmOrchestrator? _llmOrchestrator;

    public PostTrainingAnalysisService(
        ILogger<PostTrainingAnalysisService> logger,
        UnifiedLlmOrchestrator? llmOrchestrator = null)
    {
        _logger = logger;
        _llmOrchestrator = llmOrchestrator;
    }

    /// <summary>
    /// Analyze training results and provide recommendations
    /// </summary>
    public async Task<PostTrainingAnalysis> AnalyzeTrainingResultsAsync(
        TrainingMetrics metrics,
        PreflightCheckResult preflightResult,
        int annotationCount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing training results: Loss={Loss}, Duration={Duration}s, Samples={Samples}",
            metrics.Loss, metrics.Duration.TotalSeconds, metrics.Samples);

        var analysis = new PostTrainingAnalysis
        {
            TrainingLoss = metrics.Loss,
            TrainingSamples = metrics.Samples,
            TrainingDurationSeconds = (int)metrics.Duration.TotalSeconds,
            AnnotationCount = annotationCount,
            HadGpu = preflightResult.HasGpu,
            ActualTimeMinutes = (int)Math.Ceiling(metrics.Duration.TotalMinutes),
            EstimatedTimeMinutes = preflightResult.EstimatedTrainingTimeMinutes
        };

        try
        {
            // Analyze metrics
            AnalyzeMetrics(analysis);

            // Apply rule-based recommendation
            ApplyRuleBasedRecommendation(analysis);

            _logger.LogInformation("Post-training analysis complete: Recommendation={Recommendation}",
                analysis.Recommendation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete post-training analysis");
            analysis.Warnings.Add("Could not generate detailed analysis due to error");
            analysis.Recommendation = TrainingRecommendation.AcceptWithCaution;
            analysis.Summary = "Training completed but detailed analysis failed. Review metrics manually before deploying.";
        }

        return analysis;
    }

    private void AnalyzeMetrics(PostTrainingAnalysis analysis)
    {
        // Loss analysis
        if (analysis.TrainingLoss < 0.1)
        {
            analysis.Observations.Add("Excellent training loss - model learned patterns effectively");
            analysis.QualityScore += 30;
        }
        else if (analysis.TrainingLoss < 0.3)
        {
            analysis.Observations.Add("Good training loss - model shows solid learning");
            analysis.QualityScore += 20;
        }
        else if (analysis.TrainingLoss < 0.5)
        {
            analysis.Observations.Add("Moderate training loss - model learning is acceptable");
            analysis.QualityScore += 10;
        }
        else if (analysis.TrainingLoss < 0.7)
        {
            analysis.Observations.Add("High training loss - model may need more data or training time");
            analysis.Warnings.Add("Training loss is higher than ideal - consider annotating more frames");
            analysis.QualityScore -= 10;
        }
        else
        {
            analysis.Observations.Add("Very high training loss - model did not learn effectively");
            analysis.Warnings.Add("Poor training results - model may not improve frame selection");
            analysis.Concerns.Add("Training loss exceeds acceptable threshold");
            analysis.QualityScore -= 30;
        }

        // Sample count analysis
        if (analysis.TrainingSamples < 20)
        {
            analysis.Warnings.Add("Very few training samples - model may not generalize well");
            analysis.Concerns.Add("Insufficient training data for reliable results");
            analysis.QualityScore -= 20;
        }
        else if (analysis.TrainingSamples < 50)
        {
            analysis.Warnings.Add("Limited training samples - consider adding more annotations");
            analysis.QualityScore -= 10;
        }
        else if (analysis.TrainingSamples >= 100)
        {
            analysis.Observations.Add("Excellent sample size - model has sufficient data");
            analysis.QualityScore += 20;
        }
        else
        {
            analysis.Observations.Add("Good sample size for initial training");
            analysis.QualityScore += 10;
        }

        // Training duration analysis
        if (analysis.ActualTimeMinutes > 60)
        {
            analysis.Observations.Add("Long training duration - consider GPU acceleration for future training");
        }
        
        if (analysis.EstimatedTimeMinutes > 0)
        {
            var ratio = (double)analysis.ActualTimeMinutes / analysis.EstimatedTimeMinutes;
            if (ratio > 2.0)
            {
                analysis.Warnings.Add("Training took longer than estimated - system may have been under load");
            }
        }

        // GPU usage
        if (!analysis.HadGpu)
        {
            analysis.Observations.Add("Training completed on CPU - GPU would significantly improve speed");
        }

        _logger.LogDebug("Quality score: {Score}", analysis.QualityScore);
    }

    private void ApplyRuleBasedRecommendation(PostTrainingAnalysis analysis)
    {
        // Rule-based recommendation based on quality score and concerns
        if (analysis.Concerns.Count != 0 || analysis.QualityScore < -20)
        {
            analysis.Recommendation = TrainingRecommendation.Revert;
            analysis.Summary = "Training results are below acceptable quality standards. The model may not improve frame selection and could potentially degrade performance.";
            analysis.NextSteps.Add("Review training data for quality issues");
            analysis.NextSteps.Add("Ensure annotations have diverse ratings and represent different frame types");
            analysis.NextSteps.Add("Consider adding more training samples before trying again");
        }
        else if (analysis.Warnings.Count != 0 || analysis.QualityScore < 20)
        {
            analysis.Recommendation = TrainingRecommendation.AcceptWithCaution;
            analysis.Summary = "Training completed with acceptable results, but there are areas for improvement. Test the model carefully before full deployment.";
            analysis.NextSteps.Add("Test the model on a small batch of videos first");
            analysis.NextSteps.Add("Compare results with the default model");
            analysis.NextSteps.Add("Annotate more frames to improve future training");
        }
        else
        {
            analysis.Recommendation = TrainingRecommendation.Accept;
            analysis.Summary = "Training completed successfully with good results. The model should improve frame importance scoring.";
            analysis.NextSteps.Add("Deploy the model for general use");
            analysis.NextSteps.Add("Monitor performance on real videos");
            analysis.NextSteps.Add("Continue collecting annotations to further improve the model");
        }
    }
}

/// <summary>
/// Post-training analysis result
/// </summary>
public class PostTrainingAnalysis
{
    public double TrainingLoss { get; set; }
    public int TrainingSamples { get; set; }
    public int TrainingDurationSeconds { get; set; }
    public int AnnotationCount { get; set; }
    public bool HadGpu { get; set; }
    public int ActualTimeMinutes { get; set; }
    public int EstimatedTimeMinutes { get; set; }

    public int QualityScore { get; set; }
    public List<string> Observations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Concerns { get; set; } = new();
    
    public string Summary { get; set; } = string.Empty;
    public TrainingRecommendation Recommendation { get; set; } = TrainingRecommendation.AcceptWithCaution;
    public List<string> NextSteps { get; set; } = new();
}

/// <summary>
/// Recommendation for what to do with trained model
/// </summary>
public enum TrainingRecommendation
{
    Accept,
    AcceptWithCaution,
    Revert
}
