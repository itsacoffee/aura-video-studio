using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.HealthChecks;

/// <summary>
/// Health check for IntelligentContentAdvisor functionality
/// </summary>
public class ContentQualityHealthCheck : IHealthCheck
{
    private readonly ILogger<ContentQualityHealthCheck> _logger;
    private readonly IntelligentContentAdvisor? _contentAdvisor;

    public string Name => "Content Quality System";

    public ContentQualityHealthCheck(
        ILogger<ContentQualityHealthCheck> logger,
        IntelligentContentAdvisor? contentAdvisor = null)
    {
        _logger = logger;
        _contentAdvisor = contentAdvisor;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new HealthCheckResult
        {
            Name = Name,
            Status = HealthStatus.Healthy
        };

        if (_contentAdvisor == null)
        {
            result.Status = HealthStatus.Degraded;
            result.Message = "IntelligentContentAdvisor not available";
            result.Duration = sw.Elapsed;
            return result;
        }

        try
        {
            // Test script with known quality issues
            var testScript = @"In today's video, we're going to delve into the topic of AI. 
                It's important to note that AI is a game changer. 
                Firstly, AI can do many things. 
                Secondly, it is revolutionary. 
                Thirdly, it's cutting-edge technology.
                Don't forget to like and subscribe!";

            var testBrief = new Brief(
                Topic: "AI Technology",
                Audience: "General",
                Goal: "Educational",
                Tone: "informative",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var testSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(30),
                Pacing: Models.Pacing.Conversational,
                Density: Models.Density.Balanced,
                Style: "test"
            );

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            var analysis = await _contentAdvisor.AnalyzeContentQualityAsync(
                testScript,
                testBrief,
                testSpec,
                cts.Token).ConfigureAwait(false);

            // Verify the analysis detected issues
            var hasIssues = analysis.Issues.Count > 0;
            var hasSuggestions = analysis.Suggestions.Count > 0;
            var hasValidScores = analysis.OverallScore >= 0 && analysis.OverallScore <= 100;

            result.Data["OverallScore"] = analysis.OverallScore;
            result.Data["IssuesDetected"] = analysis.Issues.Count;
            result.Data["SuggestionsProvided"] = analysis.Suggestions.Count;
            result.Data["AnalysisTime"] = sw.ElapsedMilliseconds;

            if (!hasValidScores)
            {
                result.Status = HealthStatus.Degraded;
                result.Message = "Quality analysis returned invalid scores";
            }
            else if (!hasIssues)
            {
                result.Status = HealthStatus.Degraded;
                result.Message = "Quality analysis failed to detect known issues in test script";
            }
            else
            {
                result.Status = HealthStatus.Healthy;
                result.Message = $"Content quality system functional (detected {analysis.Issues.Count} issues)";
            }
        }
        catch (OperationCanceledException)
        {
            result.Status = HealthStatus.Degraded;
            result.Message = "Content quality check timed out";
            _logger.LogWarning("Content quality health check timed out");
        }
        catch (Exception ex)
        {
            result.Status = HealthStatus.Unhealthy;
            result.Message = $"Content quality check failed: {ex.Message}";
            result.Exception = ex;
            _logger.LogError(ex, "Content quality health check failed");
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }
}
