using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentVerification;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentVerification;

/// <summary>
/// Service for analyzing confidence in content assertions
/// </summary>
public class ConfidenceAnalysisService
{
    private readonly ILogger<ConfidenceAnalysisService> _logger;

    public ConfidenceAnalysisService(ILogger<ConfidenceAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyze confidence across all claims in content
    /// </summary>
    public async Task<ConfidenceAnalysis> AnalyzeConfidenceAsync(
        string contentId,
        List<Claim> claims,
        List<FactCheckResult> factChecks,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing confidence for content {ContentId} with {Count} claims",
            contentId, claims.Count);

        await Task.Delay(10, ct).ConfigureAwait(false); // Simulate processing

        var claimConfidences = new Dictionary<string, double>();
        var highConfidence = new List<string>();
        var lowConfidence = new List<string>();
        var uncertain = new List<string>();

        foreach (var factCheck in factChecks)
        {
            var confidence = factCheck.ConfidenceScore;
            claimConfidences[factCheck.ClaimId] = confidence;

            if (confidence >= 0.8)
            {
                highConfidence.Add(factCheck.ClaimId);
            }
            else if (confidence >= 0.5)
            {
                uncertain.Add(factCheck.ClaimId);
            }
            else
            {
                lowConfidence.Add(factCheck.ClaimId);
            }
        }

        var overallConfidence = claimConfidences.Count != 0
            ? claimConfidences.Values.Average()
            : 0.0;

        return new ConfidenceAnalysis(
            ContentId: contentId,
            OverallConfidence: overallConfidence,
            ClaimConfidences: claimConfidences,
            HighConfidenceClaims: highConfidence,
            LowConfidenceClaims: lowConfidence,
            UncertainClaims: uncertain,
            AnalyzedAt: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Calculate confidence score for a single claim
    /// </summary>
    public double CalculateClaimConfidence(
        Claim claim,
        List<Evidence> evidence)
    {
        if (evidence.Count == 0)
        {
            return 0.0;
        }

        // Weight factors
        const double evidenceCountWeight = 0.2;
        const double credibilityWeight = 0.4;
        const double relevanceWeight = 0.3;
        const double extractionWeight = 0.1;

        // Evidence count score (normalized to 0-1, capped at 5 pieces)
        var evidenceCountScore = Math.Min(evidence.Count, 5) / 5.0;

        // Average credibility of sources
        var avgCredibility = evidence.Average(e => e.Credibility);

        // Average relevance of evidence
        var avgRelevance = evidence.Average(e => e.Relevance);

        // Extraction confidence
        var extractionConfidence = claim.ExtractionConfidence;

        // Weighted sum
        var confidence = 
            (evidenceCountScore * evidenceCountWeight) +
            (avgCredibility * credibilityWeight) +
            (avgRelevance * relevanceWeight) +
            (extractionConfidence * extractionWeight);

        return Math.Min(1.0, confidence);
    }

    /// <summary>
    /// Identify claims needing human review
    /// </summary>
    public async Task<List<ReviewRecommendation>> IdentifyReviewNeedsAsync(
        ConfidenceAnalysis analysis,
        double reviewThreshold = 0.6,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Identifying claims needing review (threshold: {Threshold})",
            reviewThreshold);

        await Task.Delay(10, ct).ConfigureAwait(false);

        var recommendations = new List<ReviewRecommendation>();

        foreach (var (claimId, confidence) in analysis.ClaimConfidences)
        {
            if (confidence < reviewThreshold)
            {
                var priority = confidence switch
                {
                    < 0.3 => ReviewPriority.High,
                    < 0.5 => ReviewPriority.Medium,
                    _ => ReviewPriority.Low
                };

                recommendations.Add(new ReviewRecommendation(
                    ClaimId: claimId,
                    Confidence: confidence,
                    Priority: priority,
                    Reason: $"Confidence score {confidence:P0} is below threshold {reviewThreshold:P0}",
                    SuggestedActions: GetSuggestedActions(confidence)
                ));
            }
        }

        return recommendations.OrderByDescending(r => r.Priority).ToList();
    }

    /// <summary>
    /// Generate confidence report
    /// </summary>
    public string GenerateConfidenceReport(ConfidenceAnalysis analysis)
    {
        var totalClaims = analysis.ClaimConfidences.Count;
        var highPct = totalClaims > 0 ? (analysis.HighConfidenceClaims.Count * 100.0) / totalClaims : 0;
        var uncertainPct = totalClaims > 0 ? (analysis.UncertainClaims.Count * 100.0) / totalClaims : 0;
        var lowPct = totalClaims > 0 ? (analysis.LowConfidenceClaims.Count * 100.0) / totalClaims : 0;

        return $@"Confidence Analysis Report
========================
Overall Confidence: {analysis.OverallConfidence:P1}
Total Claims: {totalClaims}

Distribution:
- High Confidence (≥80%): {analysis.HighConfidenceClaims.Count} ({highPct:F0}%)
- Uncertain (50-80%): {analysis.UncertainClaims.Count} ({uncertainPct:F0}%)
- Low Confidence (<50%): {analysis.LowConfidenceClaims.Count} ({lowPct:F0}%)

Recommendation: {GetOverallRecommendation(analysis.OverallConfidence)}";
    }

    private List<string> GetSuggestedActions(double confidence)
    {
        return confidence switch
        {
            < 0.3 => new List<string>
            {
                "Verify claim with multiple independent sources",
                "Consider removing or rephrasing if unverifiable",
                "Add disclaimer if claim is speculative"
            },
            < 0.5 => new List<string>
            {
                "Seek additional sources to support claim",
                "Consider adding qualifying language (e.g., 'reportedly', 'according to')",
                "Review evidence quality"
            },
            _ => new List<string>
            {
                "Monitor for new evidence",
                "Consider adding source attribution"
            }
        };
    }

    private string GetOverallRecommendation(double overallConfidence)
    {
        return overallConfidence switch
        {
            >= 0.8 => "Content has high confidence and can be published with minimal concern.",
            >= 0.6 => "Content is reasonably confident but review uncertain claims before publishing.",
            >= 0.4 => "Content has moderate confidence. Significant review recommended before publishing.",
            _ => "Content has low confidence. Extensive verification needed before publishing."
        };
    }
}

/// <summary>
/// Recommendation for manual review
/// </summary>
public record ReviewRecommendation(
    string ClaimId,
    double Confidence,
    ReviewPriority Priority,
    string Reason,
    List<string> SuggestedActions
);

/// <summary>
/// Priority level for review
/// </summary>
public enum ReviewPriority
{
    Low,
    Medium,
    High,
    Critical
}
