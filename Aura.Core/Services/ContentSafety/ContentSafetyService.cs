using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentSafety;

/// <summary>
/// Core content safety analysis service
/// </summary>
public class ContentSafetyService
{
    private readonly ILogger<ContentSafetyService> _logger;
    private readonly KeywordListManager _keywordManager;
    private readonly TopicFilterManager _topicManager;

    public ContentSafetyService(
        ILogger<ContentSafetyService> logger,
        KeywordListManager keywordManager,
        TopicFilterManager topicManager)
    {
        _logger = logger;
        _keywordManager = keywordManager;
        _topicManager = topicManager;
    }

    /// <summary>
    /// Analyze content against safety policy
    /// </summary>
    public async Task<SafetyAnalysisResult> AnalyzeContentAsync(
        string contentId,
        string content,
        SafetyPolicy policy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing content {ContentId} with policy {PolicyName}", 
            contentId, policy.Name);

        if (!policy.IsEnabled)
        {
            _logger.LogInformation("Policy disabled, skipping analysis");
            return new SafetyAnalysisResult { ContentId = contentId, IsSafe = true };
        }

        var result = new SafetyAnalysisResult { ContentId = contentId };
        
        try
        {
            await AnalyzeKeywordsAsync(content, policy, result, ct).ConfigureAwait(false);
            
            await AnalyzeTopicsAsync(content, policy, result, ct).ConfigureAwait(false);
            
            AnalyzeCategories(content, policy, result);
            
            CalculateOverallScore(result);
            
            DetermineRecommendedActions(result, policy);
            
            _logger.LogInformation("Analysis complete. Safe: {IsSafe}, Score: {Score}, Violations: {Count}",
                result.IsSafe, result.OverallSafetyScore, result.Violations.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content {ContentId}", contentId);
            throw;
        }
    }

    /// <summary>
    /// Analyze content against keyword rules
    /// </summary>
    private async Task AnalyzeKeywordsAsync(
        string content,
        SafetyPolicy policy,
        SafetyAnalysisResult result,
        CancellationToken ct)
    {
        foreach (var rule in policy.KeywordRules)
        {
            var matches = await _keywordManager.FindMatchesAsync(content, rule, ct).ConfigureAwait(false);
            
            foreach (var match in matches)
            {
                var violation = new SafetyViolation
                {
                    Category = SafetyCategoryType.Profanity,
                    SeverityScore = rule.Action == SafetyAction.Block ? 10 : 5,
                    Reason = $"Keyword '{rule.Keyword}' detected",
                    MatchedContent = match.MatchedText,
                    Position = match.Position,
                    RecommendedAction = rule.Action,
                    SuggestedFix = rule.Replacement,
                    CanOverride = policy.AllowUserOverride
                };
                
                result.Violations.Add(violation);
            }
        }
    }

    /// <summary>
    /// Analyze content against topic filters
    /// </summary>
    private async Task AnalyzeTopicsAsync(
        string content,
        SafetyPolicy policy,
        SafetyAnalysisResult result,
        CancellationToken ct)
    {
        var detectedTopics = await _topicManager.DetectTopicsAsync(content, ct).ConfigureAwait(false);
        
        foreach (var topic in detectedTopics)
        {
            var filter = policy.TopicFilters.FirstOrDefault(f => 
                f.Topic.Equals(topic.Topic, StringComparison.OrdinalIgnoreCase));
            
            if (filter != null && filter.IsBlocked && topic.Confidence >= filter.ConfidenceThreshold)
            {
                var violation = new SafetyViolation
                {
                    Category = SafetyCategoryType.ControversialTopics,
                    SeverityScore = 7,
                    Reason = $"Blocked topic '{topic.Topic}' detected (confidence: {topic.Confidence:P0})",
                    RecommendedAction = filter.Action,
                    CanOverride = policy.AllowUserOverride
                };
                
                result.Violations.Add(violation);
            }
        }
    }

    /// <summary>
    /// Analyze content against category thresholds
    /// </summary>
    private void AnalyzeCategories(
        string content,
        SafetyPolicy policy,
        SafetyAnalysisResult result)
    {
        foreach (var (categoryType, category) in policy.Categories)
        {
            if (!category.IsEnabled)
                continue;
                
            var score = CalculateCategoryScore(content, categoryType);
            result.CategoryScores[categoryType] = score;
            
            if (score > category.Threshold)
            {
                var action = category.SeverityActions.GetValueOrDefault(score, category.DefaultAction);
                
                var violation = new SafetyViolation
                {
                    Category = categoryType,
                    SeverityScore = score,
                    Reason = $"Content exceeds {categoryType} threshold ({score} > {category.Threshold})",
                    RecommendedAction = action,
                    CanOverride = policy.AllowUserOverride
                };
                
                result.Violations.Add(violation);
            }
        }
    }

    /// <summary>
    /// Calculate score for specific category
    /// </summary>
    private int CalculateCategoryScore(string content, SafetyCategoryType category)
    {
        return category switch
        {
            SafetyCategoryType.Profanity => CalculateProfanityScore(content),
            SafetyCategoryType.Violence => CalculateViolenceScore(content),
            SafetyCategoryType.SexualContent => CalculateSexualContentScore(content),
            SafetyCategoryType.HateSpeech => CalculateHateSpeechScore(content),
            SafetyCategoryType.DrugAlcohol => CalculateDrugAlcoholScore(content),
            _ => 0
        };
    }

    private int CalculateProfanityScore(string content)
    {
        var commonProfanity = new[] { "damn", "hell", "crap" };
        var strongProfanity = new[] { "explicit", "vulgar" };
        
        var lowerContent = content.ToLowerInvariant();
        var score = 0;
        
        foreach (var word in commonProfanity)
        {
            if (lowerContent.Contains(word, StringComparison.OrdinalIgnoreCase))
                score += 2;
        }
        
        foreach (var word in strongProfanity)
        {
            if (lowerContent.Contains(word, StringComparison.OrdinalIgnoreCase))
                score += 5;
        }
        
        return Math.Min(score, 10);
    }

    private int CalculateViolenceScore(string content)
    {
        var violenceKeywords = new[] { "violence", "attack", "fight", "kill", "weapon", "blood" };
        var lowerContent = content.ToLowerInvariant();
        
        var count = violenceKeywords.Count(keyword => 
            lowerContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        
        return Math.Min(count * 2, 10);
    }

    private int CalculateSexualContentScore(string content)
    {
        var sexualKeywords = new[] { "sexual", "explicit", "adult", "nsfw" };
        var lowerContent = content.ToLowerInvariant();
        
        var count = sexualKeywords.Count(keyword => 
            lowerContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        
        return Math.Min(count * 3, 10);
    }

    private int CalculateHateSpeechScore(string content)
    {
        var hateSpeechIndicators = new[] { "hate", "racist", "discriminate", "slur" };
        var lowerContent = content.ToLowerInvariant();
        
        var count = hateSpeechIndicators.Count(keyword => 
            lowerContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        
        return Math.Min(count * 4, 10);
    }

    private int CalculateDrugAlcoholScore(string content)
    {
        var substanceKeywords = new[] { "drug", "alcohol", "smoking", "marijuana", "cocaine" };
        var lowerContent = content.ToLowerInvariant();
        
        var count = substanceKeywords.Count(keyword => 
            lowerContent.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        
        return Math.Min(count * 2, 10);
    }

    /// <summary>
    /// Calculate overall safety score
    /// </summary>
    private void CalculateOverallScore(SafetyAnalysisResult result)
    {
        if (result.Violations.Count == 0)
        {
            result.OverallSafetyScore = 100;
            result.IsSafe = true;
            return;
        }
        
        var totalSeverity = result.Violations.Sum(v => v.SeverityScore);
        var avgSeverity = totalSeverity / (double)result.Violations.Count;
        
        result.OverallSafetyScore = Math.Max(0, 100 - (int)(avgSeverity * 10));
        result.IsSafe = result.Violations.All(v => v.RecommendedAction != SafetyAction.Block);
    }

    /// <summary>
    /// Determine recommended actions based on violations
    /// </summary>
    private void DetermineRecommendedActions(SafetyAnalysisResult result, SafetyPolicy policy)
    {
        var blockingViolations = result.Violations.Where(v => v.RecommendedAction == SafetyAction.Block).ToList();
        var warningViolations = result.Violations.Where(v => v.RecommendedAction == SafetyAction.Warn).ToList();
        var reviewViolations = result.Violations.Where(v => v.RecommendedAction == SafetyAction.RequireReview).ToList();
        
        if (blockingViolations.Count != 0 && !policy.AllowUserOverride)
        {
            result.IsSafe = false;
        }
        
        if (reviewViolations.Count != 0)
        {
            result.RequiresReview = true;
        }
        
        if (warningViolations.Count != 0 && blockingViolations.Count == 0)
        {
            result.AllowWithDisclaimer = true;
            result.RecommendedDisclaimer = "This content may contain material that some viewers find objectionable.";
        }
        
        foreach (var violation in result.Violations.Where(v => v.SuggestedFix != null))
        {
            result.SuggestedFixes.Add($"{violation.Reason}: {violation.SuggestedFix}");
        }
    }
}
