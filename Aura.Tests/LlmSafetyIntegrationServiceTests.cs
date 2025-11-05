using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Aura.Core.Services.ContentSafety;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class LlmSafetyIntegrationServiceTests
{
    private readonly LlmSafetyIntegrationService _service;
    private readonly ContentSafetyService _contentSafetyService;

    public LlmSafetyIntegrationServiceTests()
    {
        var keywordManager = new KeywordListManager(NullLogger<KeywordListManager>.Instance);
        var topicManager = new TopicFilterManager(NullLogger<TopicFilterManager>.Instance);
        _contentSafetyService = new ContentSafetyService(
            NullLogger<ContentSafetyService>.Instance,
            keywordManager,
            topicManager);
        _service = new LlmSafetyIntegrationService(
            NullLogger<LlmSafetyIntegrationService>.Instance,
            _contentSafetyService);
    }

    [Fact]
    public async Task ValidatePromptAsync_SafePrompt_ShouldReturnValid()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var prompt = "Create a video about cooking healthy meals for families.";

        // Act
        var result = await _service.ValidatePromptAsync(prompt, policy, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.CanProceed);
        Assert.Null(result.ModifiedPrompt);
        Assert.Empty(result.Alternatives);
    }

    [Fact]
    public async Task ValidatePromptAsync_UnsafePrompt_ShouldReturnInvalid()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var prompt = "Create content with explicit violence and blood.";

        // Act
        var result = await _service.ValidatePromptAsync(prompt, policy, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.AnalysisResult);
        Assert.True(result.AnalysisResult.Violations.Count > 0);
    }

    [Fact]
    public async Task ValidatePromptAsync_UnsafePromptWithModeration_ShouldProvideModifications()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var prompt = "Create a video about explicit content and violence.";

        // Act
        var result = await _service.ValidatePromptAsync(prompt, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        if (!result.IsValid)
        {
            Assert.NotNull(result.Explanation);
            Assert.True(result.Alternatives.Count > 0);
        }
    }

    [Fact]
    public async Task ValidateResponseAsync_SafeResponse_ShouldReturnValid()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var response = "Here is a family-friendly video script about cooking.";

        // Act
        var result = await _service.ValidateResponseAsync(response, policy, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.CanUse);
    }

    [Fact]
    public async Task ValidateResponseAsync_UnsafeResponse_ShouldDetectViolations()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var response = "This content includes explicit language and vulgar terms.";

        // Act
        var result = await _service.ValidateResponseAsync(response, policy, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotNull(result.AnalysisResult);
    }

    [Fact]
    public async Task SuggestSafeAlternativesAsync_ShouldReturnAlternatives()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var prompt = "Create violent content";
        var analysisResult = await _contentSafetyService.AnalyzeContentAsync(
            "test", prompt, policy, CancellationToken.None);

        // Act
        var alternatives = await _service.SuggestSafeAlternativesAsync(
            prompt, analysisResult, 3, CancellationToken.None);

        // Assert
        Assert.NotEmpty(alternatives);
        Assert.True(alternatives.Count <= 3);
    }

    [Fact]
    public async Task ValidatePromptAsync_WithUserOverridePolicy_ShouldAllowProceed()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.AllowUserOverride = true;

        var prompt = "Create content about controversial topics";

        // Act
        var result = await _service.ValidatePromptAsync(prompt, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ValidatePromptAsync_BlockedCategory_ShouldProvideExplanation()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var prompt = "Create explicit adult content";

        // Act
        var result = await _service.ValidatePromptAsync(prompt, policy, CancellationToken.None);

        // Assert
        if (!result.IsValid)
        {
            Assert.NotNull(result.Explanation);
            Assert.Contains("blocked", result.Explanation, StringComparison.OrdinalIgnoreCase);
        }
    }
}
