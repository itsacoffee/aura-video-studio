using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Learning;
using Aura.Core.Services.Learning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for AI learning and pattern recognition
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LearningController : ControllerBase
{
    private readonly ILogger<LearningController> _logger;
    private readonly LearningService _learningService;

    public LearningController(
        ILogger<LearningController> logger,
        LearningService learningService)
    {
        _logger = logger;
        _learningService = learningService;
    }

    /// <summary>
    /// Get identified patterns for a profile
    /// </summary>
    [HttpGet("patterns/{profileId}")]
    public async Task<IActionResult> GetPatterns(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var patterns = await _learningService.GetPatternsAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId,
                patterns = patterns.Select(p => new
                {
                    p.PatternId,
                    p.SuggestionType,
                    p.PatternType,
                    p.Strength,
                    p.Occurrences,
                    p.FirstObserved,
                    p.LastObserved,
                    p.PatternData
                }).ToList(),
                count = patterns.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patterns for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve patterns" });
        }
    }

    /// <summary>
    /// Get learning insights for a profile
    /// </summary>
    [HttpGet("insights/{profileId}")]
    public async Task<IActionResult> GetInsights(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var insights = await _learningService.GetInsightsAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId,
                insights = insights.Select(i => new
                {
                    i.InsightId,
                    i.InsightType,
                    i.Category,
                    i.Description,
                    i.Confidence,
                    i.DiscoveredAt,
                    i.IsActionable,
                    i.SuggestedAction
                }).ToList(),
                count = insights.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting insights for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve insights" });
        }
    }

    /// <summary>
    /// Trigger pattern analysis for a profile
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzePatterns(
        [FromBody] AnalyzeRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProfileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            // Start pattern analysis
            var patterns = await _learningService.AnalyzePatternsAsync(request.ProfileId, ct).ConfigureAwait(false);
            
            // Generate insights
            var insights = await _learningService.GenerateInsightsAsync(request.ProfileId, ct).ConfigureAwait(false);
            
            // Infer preferences
            var preferences = await _learningService.InferPreferencesAsync(request.ProfileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId = request.ProfileId,
                analysis = new
                {
                    patternsIdentified = patterns.Count,
                    insightsGenerated = insights.Count,
                    preferencesInferred = preferences.Count,
                    analyzedAt = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing patterns for profile {ProfileId}", request.ProfileId);
            return StatusCode(500, new { error = "Failed to analyze patterns" });
        }
    }

    /// <summary>
    /// Get prediction statistics for a profile
    /// </summary>
    [HttpGet("predictions/{profileId}")]
    public async Task<IActionResult> GetPredictionStats(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var stats = await _learningService.GetPredictionStatsAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId,
                statistics = stats.Select(kv => new
                {
                    suggestionType = kv.Key,
                    kv.Value.TotalDecisions,
                    kv.Value.Accepted,
                    kv.Value.Rejected,
                    kv.Value.Modified,
                    kv.Value.AcceptanceRate,
                    kv.Value.RejectionRate,
                    kv.Value.ModificationRate,
                    kv.Value.AverageDecisionTimeSeconds,
                    kv.Value.LastDecisionAt
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prediction stats for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve prediction statistics" });
        }
    }

    /// <summary>
    /// Rank suggestions by predicted acceptance
    /// </summary>
    [HttpPost("rank-suggestions")]
    public async Task<IActionResult> RankSuggestions(
        [FromBody] RankSuggestionsRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProfileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.SuggestionType))
            {
                return BadRequest(new { error = "SuggestionType is required" });
            }

            if (request.Suggestions == null || request.Suggestions.Count == 0)
            {
                return BadRequest(new { error = "Suggestions list is required" });
            }

            var rankedSuggestions = await _learningService.RankSuggestionsAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId = request.ProfileId,
                suggestionType = request.SuggestionType,
                rankedSuggestions = rankedSuggestions.Select(r => new
                {
                    r.Rank,
                    r.Suggestion,
                    prediction = new
                    {
                        r.Prediction.AcceptanceProbability,
                        r.Prediction.RejectionProbability,
                        r.Prediction.ModificationProbability,
                        r.Prediction.Confidence,
                        r.Prediction.ReasoningFactors
                    }
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ranking suggestions for profile {ProfileId}", request.ProfileId);
            return StatusCode(500, new { error = "Failed to rank suggestions" });
        }
    }

    /// <summary>
    /// Get confidence score for a suggestion type
    /// </summary>
    [HttpGet("confidence/{profileId}/{suggestionType}")]
    public async Task<IActionResult> GetConfidenceScore(
        string profileId,
        string suggestionType,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            if (string.IsNullOrWhiteSpace(suggestionType))
            {
                return BadRequest(new { error = "SuggestionType is required" });
            }

            var confidence = await _learningService.GetConfidenceScoreAsync(profileId, suggestionType, ct).ConfigureAwait(false);

            var confidenceLevel = confidence switch
            {
                >= 0.7 => "high",
                >= 0.4 => "medium",
                _ => "low"
            };

            return Ok(new
            {
                success = true,
                profileId,
                suggestionType,
                confidence,
                confidenceLevel
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting confidence score for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve confidence score" });
        }
    }

    /// <summary>
    /// Reset learning data for a profile
    /// </summary>
    [HttpDelete("reset/{profileId}")]
    public async Task<IActionResult> ResetLearning(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            await _learningService.ResetLearningAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId,
                message = "Learning data reset successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting learning for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to reset learning data" });
        }
    }

    /// <summary>
    /// Get learning maturity level for a profile
    /// </summary>
    [HttpGet("maturity/{profileId}")]
    public async Task<IActionResult> GetMaturityLevel(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var maturity = await _learningService.GetMaturityLevelAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId,
                maturity = new
                {
                    maturity.TotalDecisions,
                    maturity.MaturityLevel,
                    maturity.OverallConfidence,
                    maturity.DecisionsByCategory,
                    maturity.StrengthCategories,
                    maturity.WeakCategories,
                    maturity.LastAnalyzedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maturity level for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve maturity level" });
        }
    }

    /// <summary>
    /// Confirm an inferred preference
    /// </summary>
    [HttpPost("confirm-preference")]
    public async Task<IActionResult> ConfirmPreference(
        [FromBody] ConfirmPreferenceDto request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProfileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.PreferenceId))
            {
                return BadRequest(new { error = "PreferenceId is required" });
            }

            var confirmRequest = new ConfirmPreferenceRequest(
                PreferenceId: request.PreferenceId,
                IsCorrect: request.IsCorrect,
                CorrectedValue: request.CorrectedValue
            );

            await _learningService.ConfirmPreferenceAsync(request.ProfileId, confirmRequest, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                message = "Preference confirmed successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Preference {PreferenceId} not found", request.PreferenceId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming preference {PreferenceId}", request.PreferenceId);
            return StatusCode(500, new { error = "Failed to confirm preference" });
        }
    }

    /// <summary>
    /// Get inferred preferences for a profile
    /// </summary>
    [HttpGet("preferences/{profileId}")]
    public async Task<IActionResult> GetInferredPreferences(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var preferences = await _learningService.GetInferredPreferencesAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId,
                preferences = preferences.Select(p => new
                {
                    p.PreferenceId,
                    p.Category,
                    p.PreferenceName,
                    p.PreferenceValue,
                    p.Confidence,
                    confidenceLevel = p.Confidence switch
                    {
                        >= 0.7 => "high",
                        >= 0.4 => "medium",
                        _ => "low"
                    },
                    p.BasedOnDecisions,
                    p.InferredAt,
                    p.IsConfirmed,
                    p.ConflictsWith
                }).ToList(),
                count = preferences.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inferred preferences for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve preferences" });
        }
    }

    /// <summary>
    /// Get comprehensive learning analytics
    /// </summary>
    [HttpGet("analytics/{profileId}")]
    public async Task<IActionResult> GetAnalytics(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var analytics = await _learningService.GetAnalyticsAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId,
                analytics = new
                {
                    maturity = new
                    {
                        analytics.Maturity.TotalDecisions,
                        analytics.Maturity.MaturityLevel,
                        analytics.Maturity.OverallConfidence,
                        analytics.Maturity.StrengthCategories,
                        analytics.Maturity.WeakCategories
                    },
                    statisticsByCategory = analytics.StatisticsByCategory.Select(s => new
                    {
                        s.SuggestionType,
                        s.TotalDecisions,
                        s.AcceptanceRate,
                        s.RejectionRate,
                        s.ModificationRate
                    }).ToList(),
                    topPatterns = analytics.TopPatterns.Select(p => new
                    {
                        p.SuggestionType,
                        p.PatternType,
                        p.Strength,
                        p.Occurrences
                    }).ToList(),
                    highConfidencePreferences = analytics.HighConfidencePreferences.Select(p => new
                    {
                        p.Category,
                        p.PreferenceName,
                        p.PreferenceValue,
                        p.Confidence,
                        p.IsConfirmed
                    }).ToList(),
                    analytics.TotalInsights,
                    analytics.GeneratedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve analytics" });
        }
    }
}

/// <summary>
/// Request to trigger analysis
/// </summary>
public record AnalyzeRequest(string ProfileId);

/// <summary>
/// DTO for confirming preference
/// </summary>
public record ConfirmPreferenceDto(
    string ProfileId,
    string PreferenceId,
    bool IsCorrect,
    object? CorrectedValue
);
