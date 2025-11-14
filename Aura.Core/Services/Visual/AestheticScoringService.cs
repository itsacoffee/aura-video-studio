using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Visual;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Service for scoring image aesthetics using heuristic rules and optional ML models
/// </summary>
public class AestheticScoringService
{
    private readonly ILogger<AestheticScoringService> _logger;

    public AestheticScoringService(ILogger<AestheticScoringService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Score a single image candidate
    /// </summary>
    public async Task<double> ScoreImageAsync(
        ImageCandidate candidate,
        VisualPrompt prompt,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        var aestheticScore = CalculateAestheticScore(candidate, prompt);
        var keywordScore = CalculateKeywordCoverageScore(candidate, prompt);
        var qualityScore = CalculateQualityScore(candidate);

        var overallScore = (aestheticScore * 0.4) + (keywordScore * 0.4) + (qualityScore * 0.2);

        _logger.LogDebug(
            "Image scored: Aesthetic={Aesthetic:F1}, Keywords={Keywords:F1}, Quality={Quality:F1}, Overall={Overall:F1}",
            aestheticScore, keywordScore, qualityScore, overallScore);

        return overallScore;
    }

    /// <summary>
    /// Calculate heuristic aesthetic score based on composition and visual principles
    /// </summary>
    private double CalculateAestheticScore(ImageCandidate candidate, VisualPrompt prompt)
    {
        var score = 50.0;

        var aspectRatio = candidate.Width > 0 ? (double)candidate.Width / candidate.Height : 1.0;
        if (aspectRatio >= 1.5 && aspectRatio <= 1.8)
        {
            score += 10.0;
        }

        if (candidate.Width >= 1920 && candidate.Height >= 1080)
        {
            score += 15.0;
        }
        else if (candidate.Width >= 1280 && candidate.Height >= 720)
        {
            score += 10.0;
        }

        if (prompt.Style == VisualStyle.Cinematic || prompt.Style == VisualStyle.Dramatic)
        {
            score += 5.0;
        }

        if (prompt.QualityTier == VisualQualityTier.Premium)
        {
            score += 10.0;
        }
        else if (prompt.QualityTier == VisualQualityTier.Enhanced)
        {
            score += 5.0;
        }

        if (candidate.Source == "StableDiffusion" || candidate.Source == "Stability")
        {
            score += 10.0;
        }

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// Calculate keyword coverage score based on narrative keywords
    /// </summary>
    private double CalculateKeywordCoverageScore(ImageCandidate candidate, VisualPrompt prompt)
    {
        if (prompt.NarrativeKeywords == null || prompt.NarrativeKeywords.Count == 0)
        {
            return 70.0;
        }

        var imageText = $"{candidate.ImageUrl} {candidate.Source} {candidate.Reasoning}".ToLowerInvariant();
        var matchedKeywords = 0;

        foreach (var keyword in prompt.NarrativeKeywords)
        {
            if (imageText.Contains(keyword.ToLowerInvariant()))
            {
                matchedKeywords++;
            }
        }

        var coverageRatio = (double)matchedKeywords / prompt.NarrativeKeywords.Count;
        var score = coverageRatio * 100.0;

        if (prompt.DetailedDescription.Length > 0)
        {
            var descriptionWords = prompt.DetailedDescription.ToLowerInvariant().Split(' ');
            var commonWords = 0;
            foreach (var word in descriptionWords.Where(w => w.Length > 4))
            {
                if (imageText.Contains(word))
                {
                    commonWords++;
                }
            }

            if (descriptionWords.Length > 0)
            {
                var descriptionMatch = (double)commonWords / descriptionWords.Length;
                score = (score + (descriptionMatch * 100.0)) / 2.0;
            }
        }

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// Calculate technical quality score
    /// </summary>
    private double CalculateQualityScore(ImageCandidate candidate)
    {
        var score = 50.0;

        var resolution = candidate.Width * candidate.Height;
        if (resolution >= 3840 * 2160)
        {
            score += 20.0;
        }
        else if (resolution >= 1920 * 1080)
        {
            score += 15.0;
        }
        else if (resolution >= 1280 * 720)
        {
            score += 10.0;
        }
        else if (resolution < 640 * 480)
        {
            score -= 20.0;
        }

        var aspectRatio = candidate.Width > 0 ? (double)candidate.Width / candidate.Height : 1.0;
        if (aspectRatio >= 1.3 && aspectRatio <= 2.0)
        {
            score += 10.0;
        }

        if (candidate.GenerationLatencyMs < 5000)
        {
            score += 5.0;
        }
        else if (candidate.GenerationLatencyMs > 30000)
        {
            score -= 10.0;
        }

        if (candidate.Source == "StableDiffusion" || candidate.Source == "Stability")
        {
            score += 15.0;
        }

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// Batch score multiple candidates and rank them
    /// </summary>
    public async Task<IReadOnlyList<ImageCandidate>> ScoreAndRankCandidatesAsync(
        IReadOnlyList<ImageCandidate> candidates,
        VisualPrompt prompt,
        double minimumThreshold = 60.0,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;
        var scoredCandidates = new List<ImageCandidate>();

        foreach (var candidate in candidates)
        {
            var aestheticScore = CalculateAestheticScore(candidate, prompt);
            var keywordScore = CalculateKeywordCoverageScore(candidate, prompt);
            var qualityScore = CalculateQualityScore(candidate);

            var overallScore = (aestheticScore * 0.4) + (keywordScore * 0.4) + (qualityScore * 0.2);

            var rejectionReasons = new List<string>();
            if (overallScore < minimumThreshold)
            {
                rejectionReasons.Add($"Overall score {overallScore:F1} below threshold {minimumThreshold:F1}");
            }
            if (aestheticScore < 40.0)
            {
                rejectionReasons.Add($"Low aesthetic score: {aestheticScore:F1}");
            }
            if (keywordScore < 30.0)
            {
                rejectionReasons.Add($"Poor keyword coverage: {keywordScore:F1}");
            }

            var scoredCandidate = candidate with
            {
                AestheticScore = aestheticScore,
                KeywordCoverageScore = keywordScore,
                QualityScore = qualityScore,
                OverallScore = overallScore,
                RejectionReasons = rejectionReasons
            };

            scoredCandidates.Add(scoredCandidate);
        }

        var rankedCandidates = scoredCandidates
            .OrderByDescending(c => c.OverallScore)
            .ToList();

        _logger.LogInformation(
            "Scored {Count} candidates. Top score: {TopScore:F1}, Passing threshold: {Passing}",
            candidates.Count,
            rankedCandidates.FirstOrDefault()?.OverallScore ?? 0,
            rankedCandidates.Count(c => c.OverallScore >= minimumThreshold));

        return rankedCandidates;
    }

    /// <summary>
    /// Validate that a candidate meets minimum criteria
    /// </summary>
    public bool MeetsCriteria(ImageCandidate candidate, double minimumThreshold)
    {
        return candidate.OverallScore >= minimumThreshold &&
               candidate.AestheticScore >= 40.0 &&
               candidate.KeywordCoverageScore >= 30.0 &&
               candidate.RejectionReasons.Count == 0;
    }
}
