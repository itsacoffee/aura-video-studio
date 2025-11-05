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

/// <summary>
/// Integration tests for content safety pipeline
/// Tests unsafe prompts, remediation, and audit logging
/// </summary>
public class ContentSafetyIntegrationTests
{
    private readonly ContentSafetyService _safetyService;
    private readonly SafetyRemediationService _remediationService;
    private readonly LlmSafetyIntegrationService _llmSafetyService;
    private readonly KeywordListManager _keywordManager;
    private readonly TopicFilterManager _topicManager;

    public ContentSafetyIntegrationTests()
    {
        _keywordManager = new KeywordListManager(NullLogger<KeywordListManager>.Instance);
        _topicManager = new TopicFilterManager(NullLogger<TopicFilterManager>.Instance);
        _safetyService = new ContentSafetyService(
            NullLogger<ContentSafetyService>.Instance,
            _keywordManager,
            _topicManager);
        _remediationService = new SafetyRemediationService(
            NullLogger<SafetyRemediationService>.Instance,
            _safetyService);
        _llmSafetyService = new LlmSafetyIntegrationService(
            NullLogger<LlmSafetyIntegrationService>.Instance,
            _safetyService);
    }

    [Fact]
    public async Task UnsafePrompt_ShouldBeDetected_WithModeratePolicy()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var unsafePrompt = "Create a video showing explicit violence and graphic content with weapons and blood.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-unsafe-1",
            unsafePrompt,
            policy,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSafe);
        Assert.NotEmpty(result.Violations);
        Assert.Contains(result.Violations, v => v.Category == SafetyCategoryType.Violence);
        Assert.True(result.CategoryScores[SafetyCategoryType.Violence] > 0);
    }

    [Fact]
    public async Task UnsafePrompt_WithStrictPolicy_ShouldBlock()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var unsafePrompt = "Video about adult content and explicit material.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-unsafe-2",
            unsafePrompt,
            policy,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSafe);
        Assert.NotEmpty(result.Violations);
        Assert.Contains(result.Violations, v => v.RecommendedAction == SafetyAction.Block);
    }

    [Fact]
    public async Task SafePrompt_ShouldPass_WithAllPolicies()
    {
        // Arrange
        var safePrompt = "Create an educational video about healthy cooking recipes for families.";
        var policies = new[]
        {
            SafetyPolicyPresets.GetModeratePolicy(),
            SafetyPolicyPresets.GetStrictPolicy(),
            SafetyPolicyPresets.GetMinimalPolicy()
        };

        // Act & Assert
        foreach (var policy in policies)
        {
            var result = await _safetyService.AnalyzeContentAsync(
                $"test-safe-{policy.Name}",
                safePrompt,
                policy,
                CancellationToken.None);

            Assert.True(result.IsSafe, $"Safe prompt should pass {policy.Name} policy");
            Assert.Empty(result.Violations);
            Assert.True(result.OverallSafetyScore >= 80);
        }
    }

    [Fact]
    public async Task UnsafePrompt_Remediation_ShouldGenerateAlternatives()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var unsafePrompt = "Create a video about violence and fighting.";

        // Act
        var analysisResult = await _safetyService.AnalyzeContentAsync(
            "test-remediation-1",
            unsafePrompt,
            policy,
            CancellationToken.None);

        var report = await _remediationService.GenerateRemediationReportAsync(
            "test-remediation-1",
            unsafePrompt,
            analysisResult,
            policy,
            CancellationToken.None);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.RemediationStrategies);
        Assert.NotEmpty(report.Alternatives);
        Assert.NotEmpty(report.UserOptions);
        Assert.Contains(report.Summary, "Safety issues detected");
    }

    [Fact]
    public async Task UnsafePrompt_ExplainBlock_ShouldProvideUserFriendlyExplanation()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var unsafePrompt = "Video about explicit content and violence.";

        // Act
        var analysisResult = await _safetyService.AnalyzeContentAsync(
            "test-explain-1",
            unsafePrompt,
            policy,
            CancellationToken.None);

        var explanation = _remediationService.ExplainSafetyBlock(analysisResult, policy);

        // Assert
        Assert.NotEmpty(explanation);
        Assert.Contains("Safety Check Failed", explanation);
        Assert.Contains(policy.Name, explanation);
    }

    [Fact]
    public async Task KeywordRule_ShouldDetect_BlockedKeyword()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.KeywordRules.Add(new KeywordRule
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "banned-term",
            Action = SafetyAction.Block,
            MatchType = KeywordMatchType.WholeWord,
            IsCaseSensitive = false
        });
        
        var content = "This content includes banned-term in the script.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-keyword-1",
            content,
            policy,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSafe);
        Assert.NotEmpty(result.Violations);
        Assert.Contains(result.Violations, v => v.MatchedContent == "banned-term");
        Assert.Contains(result.Violations, v => v.RecommendedAction == SafetyAction.Block);
    }

    [Fact]
    public async Task LlmPromptValidation_UnsafePrompt_ShouldProvideModifiedVersion()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var unsafePrompt = "Generate script about violence and fighting.";

        // Act
        var validationResult = await _llmSafetyService.ValidatePromptAsync(
            unsafePrompt,
            policy,
            CancellationToken.None);

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.NotNull(validationResult.AnalysisResult);
        Assert.NotEmpty(validationResult.Explanation);
        
        if (validationResult.CanProceed)
        {
            Assert.NotNull(validationResult.ModifiedPrompt);
            Assert.NotEqual(unsafePrompt, validationResult.ModifiedPrompt);
        }
    }

    [Fact]
    public async Task LlmPromptValidation_SafePrompt_ShouldPass()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var safePrompt = "Generate script about healthy cooking and nutrition education.";

        // Act
        var validationResult = await _llmSafetyService.ValidatePromptAsync(
            safePrompt,
            policy,
            CancellationToken.None);

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.True(validationResult.CanProceed);
        Assert.Null(validationResult.ModifiedPrompt);
    }

    [Fact]
    public async Task SafeAlternatives_ShouldBeGenerated_ForUnsafeContent()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var unsafeContent = "Content with explicit violence.";

        // Act
        var analysisResult = await _safetyService.AnalyzeContentAsync(
            "test-alternatives-1",
            unsafeContent,
            policy,
            CancellationToken.None);

        var alternatives = await _llmSafetyService.SuggestSafeAlternativesAsync(
            unsafeContent,
            analysisResult,
            count: 3,
            CancellationToken.None);

        // Assert
        Assert.NotNull(alternatives);
        Assert.NotEmpty(alternatives);
        Assert.True(alternatives.Count <= 3);
        
        foreach (var alternative in alternatives)
        {
            Assert.NotEqual(unsafeContent, alternative);
            Assert.NotEmpty(alternative);
        }
    }

    [Fact]
    public async Task ContentModification_ShouldApply_SuggestedFixes()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.KeywordRules.Add(new KeywordRule
        {
            Id = Guid.NewGuid().ToString(),
            Keyword = "inappropriate",
            Action = SafetyAction.AutoFix,
            MatchType = KeywordMatchType.WholeWord,
            Replacement = "appropriate",
            IsCaseSensitive = false
        });
        
        var content = "This has inappropriate content.";

        // Act
        var analysisResult = await _safetyService.AnalyzeContentAsync(
            "test-modification-1",
            content,
            policy,
            CancellationToken.None);

        var modifications = await _remediationService.SuggestModificationsAsync(
            content,
            analysisResult,
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(modifications);
        Assert.Contains(modifications, m => m.OriginalText == "inappropriate");
        Assert.Contains(modifications, m => m.ModifiedText == "appropriate");
    }

    [Fact]
    public async Task PolicyEvaluation_MultipleCategoriesViolated_ShouldBeDetected()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var multiViolationContent = "Video about violence, explicit content, drugs, and hate speech.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-multi-violation-1",
            multiViolationContent,
            policy,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSafe);
        Assert.True(result.Violations.Count >= 3);
        Assert.Contains(result.CategoryScores, kvp => kvp.Value > 0);
        
        var violatedCategories = result.Violations.Select(v => v.Category).Distinct().ToList();
        Assert.True(violatedCategories.Count >= 3);
    }

    [Fact]
    public async Task OverrideCapability_WhenAllowed_ShouldBeIndicated()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.AllowUserOverride = true;
        
        var unsafePrompt = "Content with violence.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-override-1",
            unsafePrompt,
            policy,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSafe);
        Assert.All(result.Violations, v => Assert.True(v.CanOverride));
    }

    [Fact]
    public async Task OverrideCapability_WhenNotAllowed_ShouldBeIndicated()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        policy.AllowUserOverride = false;
        
        var unsafePrompt = "Content with violence.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-override-2",
            unsafePrompt,
            policy,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSafe);
        Assert.All(result.Violations, v => Assert.False(v.CanOverride));
    }

    [Fact]
    public async Task DisabledPolicy_ShouldSkipAnalysis()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.IsEnabled = false;
        
        var unsafePrompt = "Content with explicit violence and graphic material.";

        // Act
        var result = await _safetyService.AnalyzeContentAsync(
            "test-disabled-1",
            unsafePrompt,
            policy,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSafe);
        Assert.Empty(result.Violations);
        Assert.Equal(100, result.OverallSafetyScore);
    }

    [Fact]
    public async Task CategoryThreshold_HigherThreshold_ShouldAllowMoreContent()
    {
        // Arrange
        var lowThresholdPolicy = SafetyPolicyPresets.GetStrictPolicy();
        var highThresholdPolicy = SafetyPolicyPresets.GetMinimalPolicy();
        
        var borderlineContent = "Video discusses violence in historical context.";

        // Act
        var strictResult = await _safetyService.AnalyzeContentAsync(
            "test-threshold-1",
            borderlineContent,
            lowThresholdPolicy,
            CancellationToken.None);

        var minimalResult = await _safetyService.AnalyzeContentAsync(
            "test-threshold-2",
            borderlineContent,
            highThresholdPolicy,
            CancellationToken.None);

        // Assert
        Assert.True(strictResult.Violations.Count >= minimalResult.Violations.Count);
    }

    [Fact]
    public async Task RemediationReport_ShouldInclude_MultipleStrategies()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var unsafeContent = "Content with violence and explicit material.";

        // Act
        var analysisResult = await _safetyService.AnalyzeContentAsync(
            "test-strategies-1",
            unsafeContent,
            policy,
            CancellationToken.None);

        var report = await _remediationService.GenerateRemediationReportAsync(
            "test-strategies-1",
            unsafeContent,
            analysisResult,
            policy,
            CancellationToken.None);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.RemediationStrategies);
        Assert.True(report.RemediationStrategies.Count >= 2);
        
        Assert.All(report.RemediationStrategies, strategy =>
        {
            Assert.NotEmpty(strategy.Name);
            Assert.NotEmpty(strategy.Description);
            Assert.NotEmpty(strategy.Difficulty);
            Assert.True(strategy.SuccessLikelihood > 0);
            Assert.NotEmpty(strategy.Steps);
        });
    }

    [Fact]
    public async Task DetailedExplanation_ShouldContain_CategoryScores()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var content = "Video about violence and drugs.";

        // Act
        var analysisResult = await _safetyService.AnalyzeContentAsync(
            "test-detailed-1",
            content,
            policy,
            CancellationToken.None);

        var report = await _remediationService.GenerateRemediationReportAsync(
            "test-detailed-1",
            content,
            analysisResult,
            policy,
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(report.DetailedExplanation);
        Assert.Contains("Safety Analysis Results", report.DetailedExplanation);
        Assert.Contains("Overall Safety Score", report.DetailedExplanation);
        Assert.Contains("Category Scores", report.DetailedExplanation);
    }
}
