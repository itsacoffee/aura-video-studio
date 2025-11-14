using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentVerification;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentVerification;

/// <summary>
/// Orchestrates the complete content verification process
/// </summary>
public class ContentVerificationOrchestrator
{
    private readonly ILogger<ContentVerificationOrchestrator> _logger;
    private readonly FactCheckingService _factCheckingService;
    private readonly SourceAttributionService _sourceAttributionService;
    private readonly ConfidenceAnalysisService _confidenceAnalysisService;
    private readonly MisinformationDetectionService _misinformationDetectionService;

    public ContentVerificationOrchestrator(
        ILogger<ContentVerificationOrchestrator> logger,
        FactCheckingService factCheckingService,
        SourceAttributionService sourceAttributionService,
        ConfidenceAnalysisService confidenceAnalysisService,
        MisinformationDetectionService misinformationDetectionService)
    {
        _logger = logger;
        _factCheckingService = factCheckingService;
        _sourceAttributionService = sourceAttributionService;
        _confidenceAnalysisService = confidenceAnalysisService;
        _misinformationDetectionService = misinformationDetectionService;
    }

    /// <summary>
    /// Perform complete verification of content
    /// </summary>
    public async Task<VerificationResult> VerifyContentAsync(
        VerificationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting verification for content {ContentId}", request.ContentId);

        try
        {
            // Step 1: Extract claims from content
            var claims = await ExtractClaimsAsync(request.Content, ct).ConfigureAwait(false);
            _logger.LogInformation("Extracted {Count} claims", claims.Count);

            // Apply max claims limit
            if (claims.Count > request.Options.MaxClaimsToCheck)
            {
                _logger.LogInformation("Limiting to {Max} claims", request.Options.MaxClaimsToCheck);
                claims = claims.Take(request.Options.MaxClaimsToCheck).ToList();
            }

            // Step 2: Fact-check claims
            var factChecks = request.Options.CheckFacts
                ? await _factCheckingService.CheckClaimsAsync(claims, ct).ConfigureAwait(false)
                : new List<FactCheckResult>();

            // Step 3: Analyze confidence
            var confidence = request.Options.AnalyzeConfidence
                ? await _confidenceAnalysisService.AnalyzeConfidenceAsync(
                    request.ContentId, claims, factChecks, ct).ConfigureAwait(false)
                : null;

            // Step 4: Detect misinformation
            var misinformation = request.Options.DetectMisinformation
                ? await _misinformationDetectionService.DetectMisinformationAsync(
                    request.ContentId, request.Content, claims, factChecks, ct).ConfigureAwait(false)
                : null;

            // Step 5: Collect and validate sources
            var sources = request.Options.AttributeSources
                ? await CollectSourcesAsync(factChecks, ct).ConfigureAwait(false)
                : new List<SourceAttribution>();

            // Step 6: Determine overall status and confidence
            var overallStatus = DetermineOverallStatus(factChecks, misinformation);
            var overallConfidence = confidence?.OverallConfidence ?? 0.0;

            // Step 7: Generate warnings
            var warnings = GenerateWarnings(
                factChecks,
                confidence,
                misinformation,
                request.Options.MinConfidenceThreshold);

            var result = new VerificationResult(
                ContentId: request.ContentId,
                Claims: claims,
                FactChecks: factChecks,
                Confidence: confidence ?? new ConfidenceAnalysis(
                    ContentId: request.ContentId,
                    OverallConfidence: 0.0,
                    ClaimConfidences: new Dictionary<string, double>(),
                    HighConfidenceClaims: new List<string>(),
                    LowConfidenceClaims: new List<string>(),
                    UncertainClaims: new List<string> { "Confidence analysis not available" },
                    AnalyzedAt: DateTime.UtcNow
                ),
                Misinformation: misinformation,
                Sources: sources,
                OverallStatus: overallStatus,
                OverallConfidence: overallConfidence,
                Warnings: warnings,
                VerifiedAt: DateTime.UtcNow
            );

            _logger.LogInformation(
                "Verification complete for {ContentId}: Status={Status}, Confidence={Confidence:P0}",
                request.ContentId, overallStatus, overallConfidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying content {ContentId}", request.ContentId);
            throw;
        }
    }

    /// <summary>
    /// Quick verification for real-time feedback
    /// </summary>
    public async Task<QuickVerificationResult> QuickVerifyAsync(
        string content,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Performing quick verification");

        var claims = await ExtractClaimsAsync(content, ct).ConfigureAwait(false);
        
        // Only check top 5 claims for quick verification
        var topClaims = claims.Take(5).ToList();
        var factChecks = await _factCheckingService.CheckClaimsAsync(topClaims, ct).ConfigureAwait(false);

        var avgConfidence = factChecks.Count != 0
            ? factChecks.Average(fc => fc.ConfidenceScore)
            : 0.0;

        var hasIssues = factChecks.Any(fc =>
            fc.Status == VerificationStatus.False ||
            fc.Status == VerificationStatus.Disputed ||
            fc.ConfidenceScore < 0.5);

        return new QuickVerificationResult(
            ClaimCount: claims.Count,
            CheckedCount: topClaims.Count,
            AverageConfidence: avgConfidence,
            HasIssues: hasIssues,
            TopIssues: factChecks
                .Where(fc => fc.ConfidenceScore < 0.5)
                .Select(fc => fc.Explanation ?? "Low confidence")
                .Take(3)
                .ToList()
        );
    }

    /// <summary>
    /// Extract verifiable claims from content
    /// </summary>
    private async Task<List<Claim>> ExtractClaimsAsync(
        string content,
        CancellationToken ct)
    {
        await Task.Delay(10, ct).ConfigureAwait(false); // Simulate processing

        var claims = new List<Claim>();
        var sentences = SplitIntoSentences(content);

        foreach (var (sentence, index) in sentences.Select((s, i) => (s, i)))
        {
            var claimType = ClassifyClaimType(sentence);
            
            // Only extract factual claims
            if (claimType != ClaimType.Opinion)
            {
                claims.Add(new Claim(
                    ClaimId: Guid.NewGuid().ToString(),
                    Text: sentence,
                    Context: content,
                    StartPosition: index * 100, // Approximate
                    EndPosition: (index + 1) * 100,
                    Type: claimType,
                    ExtractionConfidence: 0.8
                ));
            }
        }

        return claims;
    }

    private List<string> SplitIntoSentences(string content)
    {
        // Simple sentence splitting
        var sentences = Regex.Split(content, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        return sentences;
    }

    private ClaimType ClassifyClaimType(string sentence)
    {
        // Simple heuristic classification
        if (Regex.IsMatch(sentence, @"\b(I think|I believe|in my opinion|seems|appears)\b", 
            RegexOptions.IgnoreCase))
        {
            return ClaimType.Opinion;
        }

        if (Regex.IsMatch(sentence, @"\b\d+%|\d+\s*(million|billion|thousand)\b", 
            RegexOptions.IgnoreCase))
        {
            return ClaimType.Statistical;
        }

        if (Regex.IsMatch(sentence, @"\b(study|research|scientists|according to)\b", 
            RegexOptions.IgnoreCase))
        {
            return ClaimType.Scientific;
        }

        if (Regex.IsMatch(sentence, @"\b(in \d{4}|historically|was|were)\b", 
            RegexOptions.IgnoreCase))
        {
            return ClaimType.Historical;
        }

        if (Regex.IsMatch(sentence, @"\b(will|predict|expect|forecast)\b", 
            RegexOptions.IgnoreCase))
        {
            return ClaimType.Prediction;
        }

        return ClaimType.Factual;
    }

    private async Task<List<SourceAttribution>> CollectSourcesAsync(
        List<FactCheckResult> factChecks,
        CancellationToken ct)
    {
        await Task.Delay(10, ct).ConfigureAwait(false);

        var sources = factChecks
            .SelectMany(fc => fc.Evidence)
            .Select(e => e.Source)
            .ToList();

        // Deduplicate sources
        return _sourceAttributionService.DeduplicateSources(sources);
    }

    private VerificationStatus DetermineOverallStatus(
        List<FactCheckResult> factChecks,
        MisinformationDetection? misinformation)
    {
        if (factChecks.Count == 0)
        {
            return VerificationStatus.Unknown;
        }

        // If any claims are false, overall is disputed
        if (factChecks.Any(fc => fc.Status == VerificationStatus.False))
        {
            return VerificationStatus.False;
        }

        // If critical misinformation risk
        if (misinformation?.RiskLevel == MisinformationRiskLevel.Critical)
        {
            return VerificationStatus.Disputed;
        }

        // Calculate percentage of verified claims
        var verifiedCount = factChecks.Count(fc => fc.Status == VerificationStatus.Verified);
        var verifiedPct = (double)verifiedCount / factChecks.Count;

        return verifiedPct switch
        {
            >= 0.8 => VerificationStatus.Verified,
            >= 0.5 => VerificationStatus.PartiallyVerified,
            >= 0.3 => VerificationStatus.Unverified,
            _ => VerificationStatus.Disputed
        };
    }

    private List<string> GenerateWarnings(
        List<FactCheckResult> factChecks,
        ConfidenceAnalysis? confidence,
        MisinformationDetection? misinformation,
        double minConfidenceThreshold)
    {
        var warnings = new List<string>();

        // Check for false claims
        var falseClaims = factChecks.Count(fc => fc.Status == VerificationStatus.False);
        if (falseClaims > 0)
        {
            warnings.Add($"{falseClaims} claim(s) verified as false");
        }

        // Check for disputed claims
        var disputedClaims = factChecks.Count(fc => fc.Status == VerificationStatus.Disputed);
        if (disputedClaims > 0)
        {
            warnings.Add($"{disputedClaims} disputed claim(s) found");
        }

        // Check overall confidence
        if (confidence != null && confidence.OverallConfidence < minConfidenceThreshold)
        {
            warnings.Add(
                $"Overall confidence ({confidence.OverallConfidence:P0}) " +
                $"below threshold ({minConfidenceThreshold:P0})");
        }

        // Check misinformation risk
        if (misinformation != null)
        {
            if (misinformation.RiskLevel >= MisinformationRiskLevel.High)
            {
                warnings.Add(
                    $"High misinformation risk detected: {misinformation.Flags.Count} issue(s)");
            }
        }

        // Check for low confidence claims
        if (confidence != null && confidence.LowConfidenceClaims.Count != 0)
        {
            warnings.Add($"{confidence.LowConfidenceClaims.Count} low confidence claim(s)");
        }

        return warnings;
    }
}

/// <summary>
/// Result of quick verification
/// </summary>
public record QuickVerificationResult(
    int ClaimCount,
    int CheckedCount,
    double AverageConfidence,
    bool HasIssues,
    List<string> TopIssues
);
