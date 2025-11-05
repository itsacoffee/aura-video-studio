using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Aura.Core.Services.ContentSafety;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Unit tests for policy evaluation and diff generation
/// </summary>
public class PolicyEvaluationTests
{
    private readonly KeywordListManager _keywordManager;
    private readonly TopicFilterManager _topicManager;
    private readonly ContentSafetyService _safetyService;

    public PolicyEvaluationTests()
    {
        _keywordManager = new KeywordListManager(NullLogger<KeywordListManager>.Instance);
        _topicManager = new TopicFilterManager(NullLogger<TopicFilterManager>.Instance);
        _safetyService = new ContentSafetyService(
            NullLogger<ContentSafetyService>.Instance,
            _keywordManager,
            _topicManager);
    }

    [Theory]
    [InlineData("Profanity", "This contains explicit language", true)]
    [InlineData("Violence", "Peaceful meditation video", false)]
    [InlineData("SexualContent", "Educational family content", false)]
    [InlineData("Violence", "Action-packed fighting scenes", true)]
    public async Task CategoryEvaluation_ShouldDetect_CorrectViolations(
        string categoryName,
        string content,
        bool shouldViolate)
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var category = Enum.Parse<SafetyCategoryType>(categoryName);
        
        // Increase threshold for this specific category
        if (policy.Categories.ContainsKey(category))
        {
            policy.Categories[category].Threshold = 3;
        }

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            $"test-category-{categoryName}",
            content,
            policy,
            CancellationToken.None);

        // Assert
        if (shouldViolate)
        {
            Assert.True(result.CategoryScores.ContainsKey(category));
            Assert.True(result.CategoryScores[category] > 0);
        }
        else
        {
            if (result.CategoryScores.ContainsKey(category))
            {
                Assert.True(result.CategoryScores[category] <= policy.Categories[category].Threshold);
            }
        }
    }

    [Fact]
    public async Task PolicyPreset_Unrestricted_ShouldAllowAllContent()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetUnrestrictedPolicy();
        var extremeContent = "Explicit violence, drugs, hate speech, and adult content.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-unrestricted",
            extremeContent,
            policy,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSafe);
    }

    [Fact]
    public async Task PolicyPreset_Strict_ShouldBlockMildContent()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var mildContent = "Video mentions violence in passing.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-strict",
            mildContent,
            policy,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSafe);
        Assert.NotEmpty(result.Violations);
    }

    [Fact]
    public async Task PolicyPreset_Minimal_ShouldAllowMostContent()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetMinimalPolicy();
        var moderateContent = "Video discusses violence in educational context.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-minimal",
            moderateContent,
            policy,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSafe || result.Violations.Count < 2);
    }

    [Theory]
    [InlineData(KeywordMatchType.WholeWord, "banned word here", true)]
    [InlineData(KeywordMatchType.WholeWord, "unbanned wordhere", false)]
    [InlineData(KeywordMatchType.Substring, "contains banned text", true)]
    [InlineData(KeywordMatchType.Substring, "contains banne text", false)]
    public async Task KeywordMatching_DifferentTypes_ShouldMatch_Correctly(
        KeywordMatchType matchType,
        string content,
        bool shouldMatch)
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.KeywordRules.Clear();
        policy.KeywordRules.Add(new KeywordRule
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "banned",
            MatchType = matchType,
            Action = SafetyAction.Block,
            IsCaseSensitive = false
        });

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-keyword-match",
            content,
            policy,
            CancellationToken.None);

        // Assert
        if (shouldMatch)
        {
            Assert.False(result.IsSafe);
            Assert.NotEmpty(result.Violations);
        }
        else
        {
            Assert.Empty(result.Violations.FindAll(v => v.Category == SafetyCategoryType.Profanity));
        }
    }

    [Theory]
    [InlineData(true, "BANNED", true)]
    [InlineData(false, "BANNED", true)]
    [InlineData(true, "banned", true)]
    [InlineData(false, "banned", true)]
    [InlineData(true, "Banned", true)]
    [InlineData(false, "Banned", true)]
    public async Task KeywordMatching_CaseSensitivity_ShouldBehave_Correctly(
        bool caseSensitive,
        string keyword,
        bool shouldMatchLowerCase)
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.KeywordRules.Clear();
        policy.KeywordRules.Add(new KeywordRule
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "banned",
            MatchType = KeywordMatchType.WholeWord,
            Action = SafetyAction.Block,
            IsCaseSensitive = caseSensitive
        });

        var content = $"This contains {keyword}";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-case-sensitivity",
            content,
            policy,
            CancellationToken.None);

        // Assert
        if (caseSensitive && keyword != "banned")
        {
            Assert.Empty(result.Violations.FindAll(v => v.MatchedContent?.Contains(keyword) == true));
        }
        else
        {
            Assert.NotEmpty(result.Violations);
        }
    }

    [Theory]
    [InlineData(SafetyAction.Block, false)]
    [InlineData(SafetyAction.Warn, true)]
    [InlineData(SafetyAction.AutoFix, true)]
    [InlineData(SafetyAction.RequireReview, false)]
    public async Task ActionType_ShouldDetermine_ProceedAbility(
        SafetyAction action,
        bool shouldAllowProceed)
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.KeywordRules.Clear();
        policy.KeywordRules.Add(new KeywordRule
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "flagged",
            MatchType = KeywordMatchType.WholeWord,
            Action = action,
            IsCaseSensitive = false
        });

        var content = "This is flagged content";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-action-type",
            content,
            policy,
            CancellationToken.None);

        // Assert
        if (shouldAllowProceed)
        {
            Assert.True(result.IsSafe || result.AllowWithDisclaimer || result.RequiresReview);
        }
        else
        {
            Assert.False(result.IsSafe);
        }
    }

    [Fact]
    public async Task OverallScore_ShouldDecrease_WithMoreViolations()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var contentWithOneViolation = "This has violence.";
        var contentWithMultipleViolations = "This has violence, drugs, explicit content, and hate speech.";

        // Act
        var singleViolationResult = await _safetyService.AnalyzeContentAsync(
            "test-score-1",
            contentWithOneViolation,
            policy,
            CancellationToken.None);

        var multipleViolationsResult = await _safetyService.AnalyzeContentAsync(
            "test-score-2",
            contentWithMultipleViolations,
            policy,
            CancellationToken.None);

        // Assert
        Assert.True(singleViolationResult.OverallSafetyScore > multipleViolationsResult.OverallSafetyScore);
    }

    [Fact]
    public async Task SuggestedFixes_ShouldBeProvided_ForAutoFixActions()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.KeywordRules.Clear();
        policy.KeywordRules.Add(new KeywordRule
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "inappropriate",
            MatchType = KeywordMatchType.WholeWord,
            Action = SafetyAction.AutoFix,
            Replacement = "appropriate",
            IsCaseSensitive = false
        });

        var content = "This has inappropriate content";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-suggested-fix",
            content,
            policy,
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.Violations);
        var violation = result.Violations[0];
        Assert.Equal("appropriate", violation.SuggestedFix);
        Assert.NotEmpty(result.SuggestedFixes);
    }

    [Fact]
    public async Task RecommendedDisclaimer_ShouldBeProvided_ForWarnings()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.KeywordRules.Clear();
        policy.KeywordRules.Add(new KeywordRule
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "warning-trigger",
            MatchType = KeywordMatchType.WholeWord,
            Action = SafetyAction.Warn,
            IsCaseSensitive = false
        });

        var content = "This has warning-trigger content";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-disclaimer",
            content,
            policy,
            CancellationToken.None);

        // Assert
        Assert.True(result.AllowWithDisclaimer);
        Assert.NotNull(result.RecommendedDisclaimer);
        Assert.NotEmpty(result.RecommendedDisclaimer);
    }

    [Fact]
    public async Task CategoryScores_ShouldBe_Calculated_ForAllCategories()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var content = "Video about violence, explicit content, drugs, and hate speech with graphic imagery.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-all-categories",
            content,
            policy,
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.CategoryScores);
        Assert.True(result.CategoryScores.Count >= 4);
        
        Assert.Contains(SafetyCategoryType.Violence, result.CategoryScores.Keys);
        Assert.Contains(SafetyCategoryType.SexualContent, result.CategoryScores.Keys);
        Assert.Contains(SafetyCategoryType.DrugAlcohol, result.CategoryScores.Keys);
        Assert.Contains(SafetyCategoryType.HateSpeech, result.CategoryScores.Keys);
    }

    [Fact]
    public async Task ViolationSeverity_ShouldBe_ProperlyScored()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var extremeContent = "Extremely explicit violent content with graphic imagery.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-severity",
            extremeContent,
            policy,
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.Violations);
        Assert.All(result.Violations, violation =>
        {
            Assert.True(violation.SeverityScore >= 0);
            Assert.True(violation.SeverityScore <= 10);
        });
        
        Assert.Contains(result.Violations, v => v.SeverityScore >= 5);
    }

    [Fact]
    public async Task MultipleKeywordRules_ShouldAll_BeEvaluated()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.KeywordRules.Clear();
        policy.KeywordRules.Add(new KeywordRule
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "word1",
            MatchType = KeywordMatchType.WholeWord,
            Action = SafetyAction.Block,
            IsCaseSensitive = false
        });
        policy.KeywordRules.Add(new KeywordRule
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "word2",
            MatchType = KeywordMatchType.WholeWord,
            Action = SafetyAction.Warn,
            IsCaseSensitive = false
        });
        policy.KeywordRules.Add(new KeywordRule
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "word3",
            MatchType = KeywordMatchType.WholeWord,
            Action = SafetyAction.AutoFix,
            IsCaseSensitive = false
        });

        var content = "This has word1 and word2 and word3";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-multiple-keywords",
            content,
            policy,
            CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Violations.Count);
        Assert.Contains(result.Violations, v => v.MatchedContent == "word1");
        Assert.Contains(result.Violations, v => v.MatchedContent == "word2");
        Assert.Contains(result.Violations, v => v.MatchedContent == "word3");
    }

    [Fact]
    public void PolicyPresets_ShouldHave_CorrectDefaultSettings()
    {
        // Act
        var unrestricted = SafetyPolicyPresets.GetUnrestrictedPolicy();
        var minimal = SafetyPolicyPresets.GetMinimalPolicy();
        var moderate = SafetyPolicyPresets.GetModeratePolicy();
        var strict = SafetyPolicyPresets.GetStrictPolicy();

        // Assert
        Assert.Equal(SafetyPolicyPreset.Unrestricted, unrestricted.Preset);
        Assert.False(unrestricted.IsEnabled);

        Assert.Equal(SafetyPolicyPreset.Minimal, minimal.Preset);
        Assert.True(minimal.IsEnabled);

        Assert.Equal(SafetyPolicyPreset.Moderate, moderate.Preset);
        Assert.True(moderate.IsEnabled);

        Assert.Equal(SafetyPolicyPreset.Strict, strict.Preset);
        Assert.True(strict.IsEnabled);
        
        Assert.True(moderate.AllowUserOverride);
        Assert.False(strict.AllowUserOverride);
    }
}
