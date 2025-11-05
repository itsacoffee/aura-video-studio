using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Aura.Core.Services.ContentSafety;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class SafetyRemediationServiceTests
{
    private readonly SafetyRemediationService _service;
    private readonly ContentSafetyService _contentSafetyService;

    public SafetyRemediationServiceTests()
    {
        var keywordManager = new KeywordListManager(NullLogger<KeywordListManager>.Instance);
        var topicManager = new TopicFilterManager(NullLogger<TopicFilterManager>.Instance);
        _contentSafetyService = new ContentSafetyService(
            NullLogger<ContentSafetyService>.Instance,
            keywordManager,
            topicManager);
        _service = new SafetyRemediationService(
            NullLogger<SafetyRemediationService>.Instance,
            _contentSafetyService);
    }

    [Fact]
    public async Task GenerateRemediationReportAsync_SafeContent_ShouldReturnPassReport()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var content = "Create a family-friendly cooking video.";
        var analysisResult = await _contentSafetyService.AnalyzeContentAsync(
            "test-1", content, policy, CancellationToken.None);

        // Act
        var report = await _service.GenerateRemediationReportAsync(
            "test-1", content, analysisResult, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(report);
        Assert.Contains("passed", report.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("proceed", report.RecommendedAction);
    }

    [Fact]
    public async Task GenerateRemediationReportAsync_UnsafeContent_ShouldProvideStrategies()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var content = "Create explicit content with violence.";
        var analysisResult = await _contentSafetyService.AnalyzeContentAsync(
            "test-2", content, policy, CancellationToken.None);

        // Act
        var report = await _service.GenerateRemediationReportAsync(
            "test-2", content, analysisResult, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.RemediationStrategies);
        Assert.NotEmpty(report.Alternatives);
        Assert.NotEmpty(report.UserOptions);
    }

    [Fact]
    public void ExplainSafetyBlock_WithBlockingViolations_ShouldProvideDetailedExplanation()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var analysisResult = new SafetyAnalysisResult
        {
            IsSafe = false,
            Violations = new System.Collections.Generic.List<SafetyViolation>
            {
                new SafetyViolation
                {
                    Category = SafetyCategoryType.Violence,
                    SeverityScore = 8,
                    Reason = "Content contains violent themes",
                    RecommendedAction = SafetyAction.Block,
                    MatchedContent = "violence"
                }
            }
        };

        // Act
        var explanation = _service.ExplainSafetyBlock(analysisResult, policy);

        // Assert
        Assert.NotNull(explanation);
        Assert.Contains("Safety Check Failed", explanation);
        Assert.Contains("Violence", explanation);
    }

    [Fact]
    public void ExplainSafetyBlock_WithUserOverride_ShouldMentionOverrideOption()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.AllowUserOverride = true;

        var analysisResult = new SafetyAnalysisResult
        {
            IsSafe = false,
            Violations = new System.Collections.Generic.List<SafetyViolation>
            {
                new SafetyViolation
                {
                    Category = SafetyCategoryType.Profanity,
                    SeverityScore = 6,
                    Reason = "Mild profanity detected",
                    RecommendedAction = SafetyAction.Block
                }
            }
        };

        // Act
        var explanation = _service.ExplainSafetyBlock(analysisResult, policy);

        // Assert
        Assert.Contains("override", explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Advanced Mode", explanation);
    }

    [Fact]
    public async Task SuggestModificationsAsync_ShouldReturnModifications()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var content = "Create explicit violent content.";
        var analysisResult = await _contentSafetyService.AnalyzeContentAsync(
            "test-3", content, policy, CancellationToken.None);

        // Act
        var modifications = await _service.SuggestModificationsAsync(
            content, analysisResult, CancellationToken.None);

        // Assert
        Assert.NotNull(modifications);
        if (analysisResult.Violations.Count > 0)
        {
            Assert.NotEmpty(modifications);
            Assert.All(modifications, m =>
            {
                Assert.NotNull(m.Description);
                Assert.NotNull(m.ModifiedText);
            });
        }
    }

    [Fact]
    public async Task GenerateRemediationReportAsync_WithAutoFixes_ShouldRecommendAutoFix()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.KeywordRules.Add(new KeywordRule
        {
            Keyword = "bad",
            Action = SafetyAction.AutoFix,
            Replacement = "appropriate",
            MatchType = KeywordMatchType.WholeWord
        });

        var content = "This content has a bad word.";
        var analysisResult = await _contentSafetyService.AnalyzeContentAsync(
            "test-4", content, policy, CancellationToken.None);

        // Act
        var report = await _service.GenerateRemediationReportAsync(
            "test-4", content, analysisResult, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(report);
        Assert.NotEmpty(report.RemediationStrategies);
        Assert.NotEmpty(report.UserOptions);
        
        var hasAutoFixViolations = analysisResult.Violations.Any(v => v.RecommendedAction == SafetyAction.AutoFix);
        if (hasAutoFixViolations)
        {
            Assert.Contains(report.UserOptions, o => o.Id == "apply-auto-fixes");
        }
    }

    [Fact]
    public async Task GenerateRemediationReportAsync_DetailedExplanation_ShouldIncludeCategoryScores()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var content = "Create content about violence and drugs.";
        var analysisResult = await _contentSafetyService.AnalyzeContentAsync(
            "test-5", content, policy, CancellationToken.None);

        // Act
        var report = await _service.GenerateRemediationReportAsync(
            "test-5", content, analysisResult, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(report.DetailedExplanation);
        Assert.Contains("Safety Analysis Results", report.DetailedExplanation);
        if (analysisResult.CategoryScores.Count > 0)
        {
            Assert.Contains("Category Scores", report.DetailedExplanation);
        }
    }

    [Fact]
    public async Task GenerateRemediationReportAsync_WithWarnings_ShouldIncludeWarnings()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var content = "Create content about controversial topics.";
        var analysisResult = await _contentSafetyService.AnalyzeContentAsync(
            "test-6", content, policy, CancellationToken.None);

        // Act
        var report = await _service.GenerateRemediationReportAsync(
            "test-6", content, analysisResult, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(report);
        if (analysisResult.Warnings.Count > 0)
        {
            Assert.Contains("Warnings", report.DetailedExplanation);
        }
    }
}
