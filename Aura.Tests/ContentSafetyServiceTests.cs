using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Aura.Core.Services.ContentSafety;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class ContentSafetyServiceTests
{
    private readonly ContentSafetyService _service;
    private readonly KeywordListManager _keywordManager;
    private readonly TopicFilterManager _topicManager;

    public ContentSafetyServiceTests()
    {
        _keywordManager = new KeywordListManager(NullLogger<KeywordListManager>.Instance);
        _topicManager = new TopicFilterManager(NullLogger<TopicFilterManager>.Instance);
        _service = new ContentSafetyService(
            NullLogger<ContentSafetyService>.Instance,
            _keywordManager,
            _topicManager);
    }

    [Fact]
    public async Task AnalyzeContent_CleanContent_ShouldReturnSafe()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var content = "This is a family-friendly video about cooking recipes.";

        // Act
        var result = await _service.AnalyzeContentAsync("test-1", content, policy, CancellationToken.None);

        // Assert
        Assert.True(result.IsSafe);
        Assert.Equal(100, result.OverallSafetyScore);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task AnalyzeContent_ProfanityContent_ShouldDetectViolation()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var content = "This content contains explicit language and vulgar terms.";

        // Act
        var result = await _service.AnalyzeContentAsync("test-2", content, policy, CancellationToken.None);

        // Assert
        Assert.False(result.IsSafe);
        Assert.True(result.Violations.Count > 0);
        Assert.Contains(result.Violations, v => v.Category == SafetyCategoryType.Profanity);
    }

    [Fact]
    public async Task AnalyzeContent_ViolenceContent_ShouldCalculateCorrectScore()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var content = "This video discusses violence, fighting, and weapons in detail.";

        // Act
        var result = await _service.AnalyzeContentAsync("test-3", content, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CategoryScores.ContainsKey(SafetyCategoryType.Violence));
        var violenceScore = result.CategoryScores[SafetyCategoryType.Violence];
        Assert.True(violenceScore > 0);
    }

    [Fact]
    public async Task AnalyzeContent_WithKeywordRules_ShouldDetectMatches()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.KeywordRules.Add(new KeywordRule
        {
            Keyword = "banned",
            Action = SafetyAction.Block,
            MatchType = KeywordMatchType.WholeWord
        });
        var content = "This content includes a banned word.";

        // Act
        var result = await _service.AnalyzeContentAsync("test-4", content, policy, CancellationToken.None);

        // Assert
        Assert.False(result.IsSafe);
        Assert.Contains(result.Violations, v => v.MatchedContent == "banned");
    }

    [Fact]
    public async Task AnalyzeContent_DisabledPolicy_ShouldSkipAnalysis()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        policy.IsEnabled = false;
        var content = "This has violence, explicit content, and everything else.";

        // Act
        var result = await _service.AnalyzeContentAsync("test-5", content, policy, CancellationToken.None);

        // Assert
        Assert.True(result.IsSafe);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task AnalyzeContent_HateSpeechContent_ShouldBlock()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var content = "This content promotes hate and discriminate against groups.";

        // Act
        var result = await _service.AnalyzeContentAsync("test-6", content, policy, CancellationToken.None);

        // Assert
        Assert.False(result.IsSafe);
        Assert.True(result.CategoryScores.ContainsKey(SafetyCategoryType.HateSpeech));
        Assert.True(result.CategoryScores[SafetyCategoryType.HateSpeech] > 0);
    }
}

public class KeywordListManagerTests
{
    private readonly KeywordListManager _manager;

    public KeywordListManagerTests()
    {
        _manager = new KeywordListManager(NullLogger<KeywordListManager>.Instance);
    }

    [Fact]
    public async Task FindMatches_WholeWord_ShouldMatchExactWords()
    {
        // Arrange
        var rule = new KeywordRule
        {
            Keyword = "test",
            MatchType = KeywordMatchType.WholeWord,
            IsCaseSensitive = false
        };
        var content = "This is a test of the system. Testing is important.";

        // Act
        var matches = await _manager.FindMatchesAsync(content, rule, CancellationToken.None);

        // Assert
        Assert.Single(matches);
        Assert.Equal("test", matches[0].MatchedText, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FindMatches_Substring_ShouldMatchPartialWords()
    {
        // Arrange
        var rule = new KeywordRule
        {
            Keyword = "test",
            MatchType = KeywordMatchType.Substring,
            IsCaseSensitive = false
        };
        var content = "This is a test. Testing is important. Latest update.";

        // Act
        var matches = await _manager.FindMatchesAsync(content, rule, CancellationToken.None);

        // Assert
        Assert.True(matches.Count >= 3);
    }

    [Fact]
    public async Task FindMatches_CaseSensitive_ShouldRespectCase()
    {
        // Arrange
        var rule = new KeywordRule
        {
            Keyword = "Test",
            MatchType = KeywordMatchType.WholeWord,
            IsCaseSensitive = true
        };
        var content = "This is a Test. But test in lowercase should not match.";

        // Act
        var matches = await _manager.FindMatchesAsync(content, rule, CancellationToken.None);

        // Assert
        Assert.Single(matches);
        Assert.Equal("Test", matches[0].MatchedText);
    }

    [Fact]
    public void ImportFromText_ShouldParseLines()
    {
        // Arrange
        var text = @"word1
word2
# comment
word3
// another comment
word4";

        // Act
        var rules = _manager.ImportFromText(text, SafetyAction.Warn);

        // Assert
        Assert.Equal(4, rules.Count);
        Assert.Contains(rules, r => r.Keyword == "word1");
        Assert.Contains(rules, r => r.Keyword == "word4");
        Assert.DoesNotContain(rules, r => r.Keyword.Contains("#"));
    }

    [Fact]
    public void GetStarterLists_ShouldReturnPredefinedLists()
    {
        // Act
        var lists = _manager.GetStarterLists();

        // Assert
        Assert.NotEmpty(lists);
        Assert.Contains("CommonProfanity", lists.Keys);
        Assert.Contains("ViolenceTerms", lists.Keys);
        Assert.All(lists.Values, list => Assert.NotEmpty(list));
    }
}

public class TopicFilterManagerTests
{
    private readonly TopicFilterManager _manager;

    public TopicFilterManagerTests()
    {
        _manager = new TopicFilterManager(NullLogger<TopicFilterManager>.Instance);
    }

    [Fact]
    public async Task DetectTopics_PoliticalContent_ShouldDetect()
    {
        // Arrange
        var content = "This video discusses politics, elections, and government policies.";

        // Act
        var topics = await _manager.DetectTopicsAsync(content, CancellationToken.None);

        // Assert
        Assert.NotEmpty(topics);
        Assert.Contains(topics, t => t.Topic == "Politics");
        Assert.All(topics, t => Assert.True(t.Confidence > 0 && t.Confidence <= 1));
    }

    [Fact]
    public async Task DetectTopics_ReligiousContent_ShouldDetect()
    {
        // Arrange
        var content = "Discussion about religion, faith, church, and prayer.";

        // Act
        var topics = await _manager.DetectTopicsAsync(content, CancellationToken.None);

        // Assert
        Assert.Contains(topics, t => t.Topic == "Religion");
    }

    [Fact]
    public async Task DetectTopics_NeutralContent_ShouldNotDetectSensitiveTopics()
    {
        // Arrange
        var content = "This is a video about cooking recipes and healthy eating.";

        // Act
        var topics = await _manager.DetectTopicsAsync(content, CancellationToken.None);

        // Assert
        var sensitiveTopics = new[] { "Politics", "Religion", "Violence", "Drugs" };
        Assert.DoesNotContain(topics, t => sensitiveTopics.Contains(t.Topic));
    }

    [Fact]
    public void ShouldBlockTopic_HighConfidence_ShouldBlock()
    {
        // Arrange
        var topic = new DetectedTopic
        {
            Topic = "Politics",
            Confidence = 0.9
        };
        var filters = new List<TopicFilter>
        {
            new TopicFilter
            {
                Topic = "Politics",
                IsBlocked = true,
                ConfidenceThreshold = 0.7
            }
        };

        // Act
        var shouldBlock = _manager.ShouldBlockTopic(topic, filters);

        // Assert
        Assert.True(shouldBlock);
    }

    [Fact]
    public void ShouldBlockTopic_LowConfidence_ShouldNotBlock()
    {
        // Arrange
        var topic = new DetectedTopic
        {
            Topic = "Politics",
            Confidence = 0.5
        };
        var filters = new List<TopicFilter>
        {
            new TopicFilter
            {
                Topic = "Politics",
                IsBlocked = true,
                ConfidenceThreshold = 0.7
            }
        };

        // Act
        var shouldBlock = _manager.ShouldBlockTopic(topic, filters);

        // Assert
        Assert.False(shouldBlock);
    }

    [Fact]
    public void GetCommonTopics_ShouldReturnList()
    {
        // Act
        var topics = _manager.GetCommonTopics();

        // Assert
        Assert.NotEmpty(topics);
        Assert.Contains("Politics", topics);
        Assert.Contains("Religion", topics);
        Assert.Contains("Violence", topics);
    }
}

public class SafetyPolicyPresetsTests
{
    [Fact]
    public void GetUnrestrictedPolicy_ShouldBeDisabled()
    {
        // Act
        var policy = SafetyPolicyPresets.GetUnrestrictedPolicy();

        // Assert
        Assert.False(policy.IsEnabled);
        Assert.Empty(policy.Categories);
        Assert.Equal(SafetyPolicyPreset.Unrestricted, policy.Preset);
    }

    [Fact]
    public void GetMinimalPolicy_ShouldOnlyBlockExtreme()
    {
        // Act
        var policy = SafetyPolicyPresets.GetMinimalPolicy();

        // Assert
        Assert.True(policy.IsEnabled);
        Assert.Contains(SafetyCategoryType.HateSpeech, policy.Categories.Keys);
        Assert.Equal(SafetyPolicyPreset.Minimal, policy.Preset);
    }

    [Fact]
    public void GetModeratePolicy_ShouldHaveBalancedSettings()
    {
        // Act
        var policy = SafetyPolicyPresets.GetModeratePolicy();

        // Assert
        Assert.True(policy.IsEnabled);
        Assert.True(policy.AllowUserOverride);
        Assert.Contains(SafetyCategoryType.Profanity, policy.Categories.Keys);
        Assert.Contains(SafetyCategoryType.Violence, policy.Categories.Keys);
        Assert.Equal(SafetyPolicyPreset.Moderate, policy.Preset);
    }

    [Fact]
    public void GetStrictPolicy_ShouldHaveRestrictiveSettings()
    {
        // Act
        var policy = SafetyPolicyPresets.GetStrictPolicy();

        // Assert
        Assert.True(policy.IsEnabled);
        Assert.False(policy.AllowUserOverride);
        Assert.All(policy.Categories.Values, cat => Assert.True(cat.Threshold <= 2));
        Assert.Equal(SafetyPolicyPreset.Strict, policy.Preset);
        Assert.NotNull(policy.AgeSettings);
        Assert.Equal(ContentRating.General, policy.AgeSettings.TargetRating);
    }

    [Fact]
    public void GetAllPresets_ShouldReturn4Presets()
    {
        // Act
        var presets = SafetyPolicyPresets.GetAllPresets();

        // Assert
        Assert.Equal(4, presets.Count);
        Assert.Contains(SafetyPolicyPreset.Unrestricted, presets.Keys);
        Assert.Contains(SafetyPolicyPreset.Minimal, presets.Keys);
        Assert.Contains(SafetyPolicyPreset.Moderate, presets.Keys);
        Assert.Contains(SafetyPolicyPreset.Strict, presets.Keys);
    }
}
