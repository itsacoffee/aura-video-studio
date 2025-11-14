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
/// Service for detecting potential misinformation patterns
/// </summary>
public class MisinformationDetectionService
{
    private readonly ILogger<MisinformationDetectionService> _logger;

    public MisinformationDetectionService(ILogger<MisinformationDetectionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detect misinformation patterns in content
    /// </summary>
    public async Task<MisinformationDetection> DetectMisinformationAsync(
        string contentId,
        string content,
        List<Claim> claims,
        List<FactCheckResult> factChecks,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Detecting misinformation in content {ContentId}", contentId);

        await Task.Delay(10, ct); // Simulate processing

        var flags = new List<MisinformationFlag>();

        // Check for false or disputed claims
        flags.AddRange(await CheckFactCheckResultsAsync(claims, factChecks, ct));

        // Check for common misinformation patterns
        flags.AddRange(await CheckMisinformationPatternsAsync(content, claims, ct));

        // Check for logical fallacies
        flags.AddRange(await CheckLogicalFallaciesAsync(content, claims, ct));

        // Calculate overall risk
        var riskScore = CalculateRiskScore(flags);
        var riskLevel = DetermineRiskLevel(riskScore);

        // Generate recommendations
        var recommendations = GenerateRecommendations(flags, riskLevel);

        return new MisinformationDetection(
            ContentId: contentId,
            Flags: flags,
            RiskScore: riskScore,
            RiskLevel: riskLevel,
            Recommendations: recommendations,
            DetectedAt: DateTime.UtcNow
        );
    }

    private async Task<List<MisinformationFlag>> CheckFactCheckResultsAsync(
        List<Claim> claims,
        List<FactCheckResult> factChecks,
        CancellationToken ct)
    {
        await Task.Delay(10, ct);

        var flags = new List<MisinformationFlag>();

        foreach (var factCheck in factChecks)
        {
            if (factCheck.Status == VerificationStatus.False)
            {
                var claim = claims.FirstOrDefault(c => c.ClaimId == factCheck.ClaimId);
                flags.Add(new MisinformationFlag(
                    FlagId: Guid.NewGuid().ToString(),
                    ClaimId: factCheck.ClaimId,
                    Pattern: "False Information",
                    Category: MisinformationCategory.FalseInformation,
                    Severity: 0.9,
                    Description: $"Claim has been verified as false: {factCheck.Explanation}",
                    SuggestedCorrections: new List<string>
                    {
                        "Remove this claim",
                        "Replace with verified information",
                        "Add correction notice"
                    }
                ));
            }
            else if (factCheck.Status == VerificationStatus.Disputed)
            {
                flags.Add(new MisinformationFlag(
                    FlagId: Guid.NewGuid().ToString(),
                    ClaimId: factCheck.ClaimId,
                    Pattern: "Disputed Claim",
                    Category: MisinformationCategory.UnsubstantiatedClaim,
                    Severity: 0.6,
                    Description: "This claim is disputed or controversial",
                    SuggestedCorrections: new List<string>
                    {
                        "Add disclaimer about disputed nature",
                        "Present multiple viewpoints",
                        "Cite conflicting sources"
                    }
                ));
            }
            else if (factCheck.ConfidenceScore < 0.5)
            {
                flags.Add(new MisinformationFlag(
                    FlagId: Guid.NewGuid().ToString(),
                    ClaimId: factCheck.ClaimId,
                    Pattern: "Low Confidence Claim",
                    Category: MisinformationCategory.UnsubstantiatedClaim,
                    Severity: 0.5,
                    Description: "Insufficient evidence to support this claim",
                    SuggestedCorrections: new List<string>
                    {
                        "Add sources and citations",
                        "Use qualifying language",
                        "Mark as opinion if factual verification not possible"
                    }
                ));
            }
        }

        return flags;
    }

    private async Task<List<MisinformationFlag>> CheckMisinformationPatternsAsync(
        string content,
        List<Claim> claims,
        CancellationToken ct)
    {
        await Task.Delay(10, ct);

        var flags = new List<MisinformationFlag>();

        // Check for absolute language patterns
        var absolutePatterns = new[]
        {
            @"\balways\b",
            @"\bnever\b",
            @"\beveryone\b",
            @"\bno one\b",
            @"\ball\b.*\bpeople\b",
            @"\b100%\b"
        };

        foreach (var pattern in absolutePatterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                flags.Add(new MisinformationFlag(
                    FlagId: Guid.NewGuid().ToString(),
                    ClaimId: null,
                    Pattern: "Absolute Language",
                    Category: MisinformationCategory.MisleadingContext,
                    Severity: 0.4,
                    Description: "Content contains absolute statements that may oversimplify or mislead",
                    SuggestedCorrections: new List<string>
                    {
                        "Use more nuanced language",
                        "Add qualifiers like 'often', 'typically', 'many'",
                        "Acknowledge exceptions"
                    }
                ));
                break; // Only flag once
            }
        }

        // Check for sensationalist language
        var sensationalPatterns = new[]
        {
            @"\bshocking\b",
            @"\bunbelievable\b",
            @"\bamazing\b.*\bsecret\b",
            @"\bthey don't want you to know\b"
        };

        foreach (var pattern in sensationalPatterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                flags.Add(new MisinformationFlag(
                    FlagId: Guid.NewGuid().ToString(),
                    ClaimId: null,
                    Pattern: "Sensationalist Language",
                    Category: MisinformationCategory.MisleadingContext,
                    Severity: 0.5,
                    Description: "Content uses sensationalist language that may mislead",
                    SuggestedCorrections: new List<string>
                    {
                        "Use neutral, factual language",
                        "Focus on verified information",
                        "Remove emotional manipulation"
                    }
                ));
                break;
            }
        }

        // Check for lack of sources
        var citationPatterns = new[]
        {
            @"according to",
            @"study shows",
            @"research indicates",
            @"\bsource:",
            @"\[.*\]"
        };

        var hasCitations = citationPatterns.Any(pattern =>
            Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase));

        if (!hasCitations && claims.Count > 3)
        {
            flags.Add(new MisinformationFlag(
                FlagId: Guid.NewGuid().ToString(),
                ClaimId: null,
                Pattern: "Missing Source Attribution",
                Category: MisinformationCategory.UnsubstantiatedClaim,
                Severity: 0.6,
                Description: "Content makes multiple claims without source attribution",
                SuggestedCorrections: new List<string>
                {
                    "Add source citations",
                    "Reference credible authorities",
                    "Link to supporting evidence"
                }
            ));
        }

        return flags;
    }

    private async Task<List<MisinformationFlag>> CheckLogicalFallaciesAsync(
        string content,
        List<Claim> claims,
        CancellationToken ct)
    {
        await Task.Delay(10, ct);

        var flags = new List<MisinformationFlag>();

        // Check for correlation/causation confusion
        if (Regex.IsMatch(content, @"\bcauses?\b.*\bbecause\b", RegexOptions.IgnoreCase))
        {
            flags.Add(new MisinformationFlag(
                FlagId: Guid.NewGuid().ToString(),
                ClaimId: null,
                Pattern: "Potential Causal Fallacy",
                Category: MisinformationCategory.LogicalFallacy,
                Severity: 0.5,
                Description: "Content may confuse correlation with causation",
                SuggestedCorrections: new List<string>
                {
                    "Clarify whether relationship is causal or correlative",
                    "Provide evidence for causal claims",
                    "Use careful language (e.g., 'associated with' vs 'causes')"
                }
            ));
        }

        // Check for appeals to emotion
        var emotionalPatterns = new[]
        {
            @"\bterrible\b",
            @"\bhorrible\b",
            @"\bdevastating\b"
        };

        foreach (var pattern in emotionalPatterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                flags.Add(new MisinformationFlag(
                    FlagId: Guid.NewGuid().ToString(),
                    ClaimId: null,
                    Pattern: "Appeal to Emotion",
                    Category: MisinformationCategory.LogicalFallacy,
                    Severity: 0.3,
                    Description: "Content uses emotional language that may cloud judgment",
                    SuggestedCorrections: new List<string>
                    {
                        "Use more objective language",
                        "Focus on facts rather than emotions",
                        "Let viewers form their own emotional responses"
                    }
                ));
                break;
            }
        }

        return flags;
    }

    private double CalculateRiskScore(List<MisinformationFlag> flags)
    {
        if (flags.Count == 0)
        {
            return 0.0;
        }

        // Weight by severity and count
        var severitySum = flags.Sum(f => f.Severity);
        var averageSeverity = severitySum / flags.Count;
        
        // More flags increase risk, but with diminishing returns
        var countFactor = 1 - Math.Exp(-flags.Count / 3.0);

        return Math.Min(1.0, averageSeverity * (0.7 + 0.3 * countFactor));
    }

    private MisinformationRiskLevel DetermineRiskLevel(double riskScore)
    {
        return riskScore switch
        {
            >= 0.8 => MisinformationRiskLevel.Critical,
            >= 0.6 => MisinformationRiskLevel.High,
            >= 0.4 => MisinformationRiskLevel.Medium,
            _ => MisinformationRiskLevel.Low
        };
    }

    private List<string> GenerateRecommendations(
        List<MisinformationFlag> flags,
        MisinformationRiskLevel riskLevel)
    {
        var recommendations = new List<string>();

        if (riskLevel >= MisinformationRiskLevel.High)
        {
            recommendations.Add("Content requires significant revision before publication");
            recommendations.Add("All flagged claims should be reviewed and corrected");
        }
        else if (riskLevel == MisinformationRiskLevel.Medium)
        {
            recommendations.Add("Review flagged items and make corrections where appropriate");
            recommendations.Add("Consider adding source citations");
        }
        else
        {
            recommendations.Add("Content appears generally reliable");
            recommendations.Add("Minor improvements suggested in flagged areas");
        }

        // Add category-specific recommendations
        var categories = flags.Select(f => f.Category).Distinct();
        
        if (categories.Contains(MisinformationCategory.FalseInformation))
        {
            recommendations.Add("Remove or correct false information immediately");
        }
        
        if (categories.Contains(MisinformationCategory.UnsubstantiatedClaim))
        {
            recommendations.Add("Add sources and evidence for unsubstantiated claims");
        }
        
        if (categories.Contains(MisinformationCategory.LogicalFallacy))
        {
            recommendations.Add("Review logical reasoning and argumentation");
        }

        return recommendations;
    }
}
