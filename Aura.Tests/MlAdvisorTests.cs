using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Services.ML;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Unit tests for ML advisor services (LabelingFocusAdvisor and PostTrainingAnalysisService)
/// </summary>
public class MlAdvisorTests
{
    private readonly ILogger<LabelingFocusAdvisor> _advisorLogger;
    private readonly ILogger<PostTrainingAnalysisService> _analysisLogger;
    private readonly ILogger<PreflightCheckService> _preflightLogger;
    private readonly HardwareDetector _hardwareDetector;

    public MlAdvisorTests()
    {
        var loggerFactory = new LoggerFactory();
        _advisorLogger = loggerFactory.CreateLogger<LabelingFocusAdvisor>();
        _analysisLogger = loggerFactory.CreateLogger<PostTrainingAnalysisService>();
        _preflightLogger = loggerFactory.CreateLogger<PreflightCheckService>();
        _hardwareDetector = new HardwareDetector(loggerFactory.CreateLogger<HardwareDetector>());
    }

    [Fact]
    public async Task LabelingAdvisor_WithNoAnnotations_ProvidesStarterAdvice()
    {
        var advisor = new LabelingFocusAdvisor(_advisorLogger);
        var annotations = new List<AnnotationRecord>();

        var result = await advisor.GetLabelingAdviceAsync(annotations, CancellationToken.None);

        Assert.Equal(0, result.TotalAnnotations);
        Assert.NotEmpty(result.Recommendations);
        Assert.NotEmpty(result.FocusAreas);
        Assert.Contains(result.Recommendations, r => r.ToLower().Contains("diverse"));
    }

    [Fact]
    public async Task LabelingAdvisor_WithBalancedDistribution_NoWarnings()
    {
        var advisor = new LabelingFocusAdvisor(_advisorLogger);
        var annotations = new List<AnnotationRecord>
        {
            new("frame1.jpg", 0.2, DateTime.UtcNow),
            new("frame2.jpg", 0.3, DateTime.UtcNow),
            new("frame3.jpg", 0.5, DateTime.UtcNow),
            new("frame4.jpg", 0.7, DateTime.UtcNow),
            new("frame5.jpg", 0.8, DateTime.UtcNow)
        };

        var result = await advisor.GetLabelingAdviceAsync(annotations, CancellationToken.None);

        Assert.Equal(5, result.TotalAnnotations);
        Assert.True(result.AverageRating >= 0.4 && result.AverageRating <= 0.6);
        Assert.Equal(0.2, result.MinRating);
        Assert.Equal(0.8, result.MaxRating);
        Assert.NotEmpty(result.Recommendations);
    }

    [Fact]
    public async Task LabelingAdvisor_WithSkewedDistribution_WarnsAboutImbalance()
    {
        var advisor = new LabelingFocusAdvisor(_advisorLogger);
        var annotations = new List<AnnotationRecord>
        {
            new("frame1.jpg", 0.9, DateTime.UtcNow),
            new("frame2.jpg", 0.95, DateTime.UtcNow),
            new("frame3.jpg", 0.85, DateTime.UtcNow),
            new("frame4.jpg", 0.92, DateTime.UtcNow),
            new("frame5.jpg", 0.88, DateTime.UtcNow)
        };

        var result = await advisor.GetLabelingAdviceAsync(annotations, CancellationToken.None);

        Assert.Equal(5, result.TotalAnnotations);
        Assert.True(result.Warnings.Any(w => w.ToLower().Contains("underrepresented") || w.ToLower().Contains("skewed")));
    }

    [Fact]
    public async Task PostTrainingAnalysis_ExcellentResults_RecommendsAccept()
    {
        var analysisService = new PostTrainingAnalysisService(_analysisLogger);
        var preflightService = new PreflightCheckService(_preflightLogger, _hardwareDetector);
        
        var preflightResult = await preflightService.CheckSystemCapabilitiesAsync(100, CancellationToken.None);
        var metrics = new TrainingMetrics(
            Loss: 0.08,
            Samples: 100,
            Duration: TimeSpan.FromMinutes(5),
            AdditionalMetrics: null
        );

        var analysis = await analysisService.AnalyzeTrainingResultsAsync(
            metrics, 
            preflightResult, 
            100, 
            CancellationToken.None);

        Assert.Equal(0.08, analysis.TrainingLoss);
        Assert.Equal(100, analysis.TrainingSamples);
        Assert.Equal(TrainingRecommendation.Accept, analysis.Recommendation);
        Assert.NotEmpty(analysis.Summary);
        Assert.NotEmpty(analysis.NextSteps);
        Assert.True(analysis.QualityScore > 20);
    }

    [Fact]
    public async Task PostTrainingAnalysis_PoorResults_RecommendsRevert()
    {
        var analysisService = new PostTrainingAnalysisService(_analysisLogger);
        var preflightService = new PreflightCheckService(_preflightLogger, _hardwareDetector);
        
        var preflightResult = await preflightService.CheckSystemCapabilitiesAsync(15, CancellationToken.None);
        var metrics = new TrainingMetrics(
            Loss: 0.85,
            Samples: 15,
            Duration: TimeSpan.FromMinutes(2),
            AdditionalMetrics: null
        );

        var analysis = await analysisService.AnalyzeTrainingResultsAsync(
            metrics, 
            preflightResult, 
            15, 
            CancellationToken.None);

        Assert.Equal(0.85, analysis.TrainingLoss);
        Assert.Equal(15, analysis.TrainingSamples);
        Assert.Equal(TrainingRecommendation.Revert, analysis.Recommendation);
        Assert.NotEmpty(analysis.Concerns);
        Assert.NotEmpty(analysis.Warnings);
        Assert.True(analysis.QualityScore < 0);
    }

    [Fact]
    public async Task PostTrainingAnalysis_ModerateResults_RecommendsAcceptWithCaution()
    {
        var analysisService = new PostTrainingAnalysisService(_analysisLogger);
        var preflightService = new PreflightCheckService(_preflightLogger, _hardwareDetector);
        
        var preflightResult = await preflightService.CheckSystemCapabilitiesAsync(50, CancellationToken.None);
        var metrics = new TrainingMetrics(
            Loss: 0.35,
            Samples: 50,
            Duration: TimeSpan.FromMinutes(8),
            AdditionalMetrics: null
        );

        var analysis = await analysisService.AnalyzeTrainingResultsAsync(
            metrics, 
            preflightResult, 
            50, 
            CancellationToken.None);

        Assert.Equal(0.35, analysis.TrainingLoss);
        Assert.Equal(50, analysis.TrainingSamples);
        Assert.Contains(analysis.Recommendation, new[] 
        { 
            TrainingRecommendation.Accept, 
            TrainingRecommendation.AcceptWithCaution 
        });
        Assert.NotEmpty(analysis.Summary);
    }

    [Fact]
    public async Task PreflightCheck_InsufficientAnnotations_FailsRequirements()
    {
        var preflightService = new PreflightCheckService(_preflightLogger, _hardwareDetector);
        
        var result = await preflightService.CheckSystemCapabilitiesAsync(10, CancellationToken.None);

        Assert.Equal(10, result.AnnotationCount);
        Assert.False(result.MeetsMinimumRequirements);
        Assert.Contains(result.Warnings, w => w.ToLower().Contains("insufficient") && w.ToLower().Contains("annotation"));
    }

    [Fact]
    public async Task PreflightCheck_SufficientAnnotations_PassesRequirements()
    {
        var preflightService = new PreflightCheckService(_preflightLogger, _hardwareDetector);
        
        var result = await preflightService.CheckSystemCapabilitiesAsync(100, CancellationToken.None);

        Assert.Equal(100, result.AnnotationCount);
        Assert.True(result.EstimatedTrainingTimeMinutes > 0);
        Assert.NotNull(result.Warnings);
        Assert.NotNull(result.Recommendations);
    }
}
