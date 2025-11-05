using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Aura.Core.Models.StockMedia;
using Aura.Core.Services.ContentSafety;
using Aura.Core.Services.StockMedia;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class StockMediaSafetyEnforcerTests
{
    private readonly StockMediaSafetyEnforcer _enforcer;
    private readonly ContentSafetyService _contentSafetyService;

    public StockMediaSafetyEnforcerTests()
    {
        var keywordManager = new KeywordListManager(NullLogger<KeywordListManager>.Instance);
        var topicManager = new TopicFilterManager(NullLogger<TopicFilterManager>.Instance);
        _contentSafetyService = new ContentSafetyService(
            NullLogger<ContentSafetyService>.Instance,
            keywordManager,
            topicManager);

        var filterService = new ContentSafetyFilterService(
            NullLogger<ContentSafetyFilterService>.Instance);

        _enforcer = new StockMediaSafetyEnforcer(
            NullLogger<StockMediaSafetyEnforcer>.Instance,
            _contentSafetyService,
            filterService);
    }

    [Fact]
    public async Task ValidateAndSanitizeQueryAsync_SafeQuery_ShouldReturnValid()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var query = "nature landscape photography";

        // Act
        var result = await _enforcer.ValidateAndSanitizeQueryAsync(query, policy, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(query, result.SanitizedQuery);
        Assert.Contains("safe", result.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAndSanitizeQueryAsync_UnsafeQuery_ShouldSanitize()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var query = "explicit violence action";

        // Act
        var result = await _enforcer.ValidateAndSanitizeQueryAsync(query, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.SanitizedQuery);
        
        if (string.IsNullOrEmpty(result.SanitizedQuery))
        {
            Assert.True(result.SanitizedQuery == "nature landscape", 
                "Empty queries should default to 'nature landscape'");
        }
        else
        {
            Assert.DoesNotContain("explicit", result.SanitizedQuery, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("violence", result.SanitizedQuery, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ValidateAndSanitizeQueryAsync_EmptyQuery_ShouldReturnInvalid()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var query = "";

        // Act
        var result = await _enforcer.ValidateAndSanitizeQueryAsync(query, policy, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("empty", result.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAndSanitizeQueryAsync_QueryTooLong_ShouldReturnInvalid()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var query = new string('a', 250);

        // Act
        var result = await _enforcer.ValidateAndSanitizeQueryAsync(query, policy, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("long", result.ValidationMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FilterResultsAsync_AllSafeResults_ShouldReturnAll()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var results = new List<StockMediaResult>
        {
            new StockMediaResult
            {
                Id = "1",
                Type = StockMediaType.Image,
                Provider = StockMediaProvider.Pexels,
                Metadata = new Dictionary<string, string> { ["description"] = "Beautiful landscape" }
            },
            new StockMediaResult
            {
                Id = "2",
                Type = StockMediaType.Image,
                Provider = StockMediaProvider.Unsplash,
                Metadata = new Dictionary<string, string> { ["description"] = "Nature photography" }
            }
        };

        // Act
        var filtered = await _enforcer.FilterResultsAsync(results, policy, CancellationToken.None);

        // Assert
        Assert.Equal(results.Count, filtered.Count);
    }

    [Fact]
    public async Task RecordSearchDecisionAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var query = "test query";
        var analysisResult = await _contentSafetyService.AnalyzeContentAsync(
            "test", query, policy, CancellationToken.None);

        // Act
        var auditLog = await _enforcer.RecordSearchDecisionAsync(
            query, policy, analysisResult, false, "testuser", CancellationToken.None);

        // Assert
        Assert.NotNull(auditLog);
        Assert.NotNull(auditLog.Id);
        Assert.Equal(policy.Id, auditLog.PolicyId);
        Assert.Equal("testuser", auditLog.UserId);
        Assert.Equal("StockMediaQuery", auditLog.ContentType);
    }

    [Fact]
    public async Task RecordSearchDecisionAsync_WithOverride_ShouldRecordOverriddenViolations()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var query = "test query";
        var analysisResult = new SafetyAnalysisResult
        {
            IsSafe = false,
            Violations = new List<SafetyViolation>
            {
                new SafetyViolation { Id = "v1", Category = SafetyCategoryType.Violence },
                new SafetyViolation { Id = "v2", Category = SafetyCategoryType.Profanity }
            }
        };

        // Act
        var auditLog = await _enforcer.RecordSearchDecisionAsync(
            query, policy, analysisResult, true, "testuser", CancellationToken.None);

        // Assert
        Assert.Equal(SafetyDecision.Approved, auditLog.Decision);
        Assert.Equal(2, auditLog.OverriddenViolations.Count);
    }

    [Fact]
    public async Task GetSafeSearchRecommendationAsync_SafeDescription_ShouldReturnSafeQuery()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var description = "Beautiful sunset over mountains";

        // Act
        var recommendation = await _enforcer.GetSafeSearchRecommendationAsync(
            description, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.IsSafe);
        Assert.NotNull(recommendation.RecommendedQuery);
        Assert.NotEmpty(recommendation.SafetyGuidelines);
    }

    [Fact]
    public async Task GetSafeSearchRecommendationAsync_UnsafeDescription_ShouldProvideAlternatives()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetStrictPolicy();
        var description = "Explicit violent scene";

        // Act
        var recommendation = await _enforcer.GetSafeSearchRecommendationAsync(
            description, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(recommendation);
        Assert.NotEmpty(recommendation.SafetyGuidelines);
    }

    [Fact]
    public async Task ValidateAndSanitizeQueryAsync_WithBlockedKeyword_ShouldProvideAlternatives()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        var query = "violent action scene";

        // Act
        var result = await _enforcer.ValidateAndSanitizeQueryAsync(query, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        if (!result.IsValid)
        {
            Assert.NotEmpty(result.Alternatives);
            Assert.All(result.Alternatives, alt => Assert.NotEmpty(alt));
        }
    }

    [Fact]
    public async Task GetSafeSearchRecommendationAsync_ShouldIncludeSuggestedFilters()
    {
        // Arrange
        var policy = SafetyPolicyPresets.GetModeratePolicy();
        policy.AgeSettings = new AgeAppropriatenessSettings
        {
            TargetRating = ContentRating.General
        };
        var description = "Family photos";

        // Act
        var recommendation = await _enforcer.GetSafeSearchRecommendationAsync(
            description, policy, CancellationToken.None);

        // Assert
        Assert.NotNull(recommendation.SuggestedFilters);
        Assert.True(recommendation.SuggestedFilters.ContainsKey("safeSearch"));
        Assert.Equal("enabled", recommendation.SuggestedFilters["safeSearch"]);
    }
}
