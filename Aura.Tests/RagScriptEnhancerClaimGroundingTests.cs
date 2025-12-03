using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.RAG;
using Aura.Core.Services.RAG;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for RagScriptEnhancer claim grounding integration
/// </summary>
public class RagScriptEnhancerClaimGroundingTests
{
    private readonly Mock<ILogger<RagScriptEnhancer>> _loggerMock;
    private readonly Mock<RagContextBuilder> _ragContextBuilderMock;
    private readonly RagScriptEnhancer _enhancer;

    public RagScriptEnhancerClaimGroundingTests()
    {
        _loggerMock = new Mock<ILogger<RagScriptEnhancer>>();
        _ragContextBuilderMock = new Mock<RagContextBuilder>(
            Mock.Of<ILogger<RagContextBuilder>>(),
            Mock.Of<VectorIndex>(),
            Mock.Of<EmbeddingService>());

        _enhancer = new RagScriptEnhancer(
            _loggerMock.Object,
            _ragContextBuilderMock.Object);
    }

    [Fact]
    public async Task EnhanceBriefWithRagAsync_WithTightenClaimsEnabled_IncludesClaimGroundingInstructions()
    {
        // Arrange
        var brief = CreateBriefWithTightenClaims(true);
        var testContext = CreateTestRagContext();

        _ragContextBuilderMock
            .Setup(r => r.BuildContextAsync(It.IsAny<string>(), It.IsAny<RagConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testContext);

        // Act
        var (enhancedBrief, ragContext) = await _enhancer.EnhanceBriefWithRagAsync(brief, CancellationToken.None);

        // Assert
        Assert.NotNull(enhancedBrief.PromptModifiers);
        var instructions = enhancedBrief.PromptModifiers.AdditionalInstructions;

        // Verify claim grounding instructions are included
        Assert.Contains("# Claim Grounding Requirements", instructions);
        Assert.Contains("Only make claims that are supported by the reference material", instructions);
        Assert.Contains("Always cite your sources inline", instructions);
        Assert.Contains("Distinguish between facts and opinions", instructions);
        Assert.Contains("Avoid these unsupported claim patterns", instructions);
        Assert.Contains("When in doubt, use hedging language", instructions);
    }

    [Fact]
    public async Task EnhanceBriefWithRagAsync_WithTightenClaimsDisabled_DoesNotIncludeClaimGroundingInstructions()
    {
        // Arrange
        var brief = CreateBriefWithTightenClaims(false);
        var testContext = CreateTestRagContext();

        _ragContextBuilderMock
            .Setup(r => r.BuildContextAsync(It.IsAny<string>(), It.IsAny<RagConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testContext);

        // Act
        var (enhancedBrief, ragContext) = await _enhancer.EnhanceBriefWithRagAsync(brief, CancellationToken.None);

        // Assert
        Assert.NotNull(enhancedBrief.PromptModifiers);
        var instructions = enhancedBrief.PromptModifiers.AdditionalInstructions;

        // Verify claim grounding instructions are NOT included
        Assert.DoesNotContain("# Claim Grounding Requirements", instructions);
        Assert.DoesNotContain("Only make claims that are supported by the reference material", instructions);

        // But regular RAG instructions should still be present
        Assert.Contains("# Reference Material", instructions);
        Assert.Contains("# Citations", instructions);
    }

    [Fact]
    public async Task EnhanceBriefWithRagAsync_WithTightenClaims_IncludesUnsupportedClaimPatterns()
    {
        // Arrange
        var brief = CreateBriefWithTightenClaims(true);
        var testContext = CreateTestRagContext();

        _ragContextBuilderMock
            .Setup(r => r.BuildContextAsync(It.IsAny<string>(), It.IsAny<RagConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testContext);

        // Act
        var (enhancedBrief, _) = await _enhancer.EnhanceBriefWithRagAsync(brief, CancellationToken.None);

        // Assert
        var instructions = enhancedBrief.PromptModifiers!.AdditionalInstructions;

        // Verify unsupported claim patterns are listed
        Assert.Contains("Research shows", instructions);
        Assert.Contains("Studies have found", instructions);
        Assert.Contains("percent of", instructions);
        Assert.Contains("According to experts", instructions);
    }

    [Fact]
    public async Task EnhanceBriefWithRagAsync_WithTightenClaims_IncludesHedgingLanguageGuidance()
    {
        // Arrange
        var brief = CreateBriefWithTightenClaims(true);
        var testContext = CreateTestRagContext();

        _ragContextBuilderMock
            .Setup(r => r.BuildContextAsync(It.IsAny<string>(), It.IsAny<RagConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testContext);

        // Act
        var (enhancedBrief, _) = await _enhancer.EnhanceBriefWithRagAsync(brief, CancellationToken.None);

        // Assert
        var instructions = enhancedBrief.PromptModifiers!.AdditionalInstructions;

        // Verify hedging language guidance is included
        Assert.Contains("This may", instructions);
        Assert.Contains("instead of", instructions);
        Assert.Contains("Some suggest", instructions);
    }

    [Fact]
    public async Task EnhanceBriefWithRagAsync_PreservesExistingPromptModifiers()
    {
        // Arrange
        var existingModifiers = new PromptModifiers(
            AdditionalInstructions: "Existing custom instructions here.",
            ExampleStyle: "Formal",
            EnableChainOfThought: true,
            PromptVersion: "v2");

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Educate",
            Tone: "Professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9,
            RagConfiguration: new RagConfiguration(
                Enabled: true,
                TopK: 5,
                TightenClaims: true),
            PromptModifiers: existingModifiers);

        var testContext = CreateTestRagContext();

        _ragContextBuilderMock
            .Setup(r => r.BuildContextAsync(It.IsAny<string>(), It.IsAny<RagConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testContext);

        // Act
        var (enhancedBrief, _) = await _enhancer.EnhanceBriefWithRagAsync(brief, CancellationToken.None);

        // Assert
        Assert.NotNull(enhancedBrief.PromptModifiers);

        // Existing instructions should be preserved
        Assert.Contains("Existing custom instructions here", enhancedBrief.PromptModifiers.AdditionalInstructions);

        // New instructions should also be present
        Assert.Contains("# Claim Grounding Requirements", enhancedBrief.PromptModifiers.AdditionalInstructions);

        // Other modifiers should be preserved
        Assert.Equal("Formal", enhancedBrief.PromptModifiers.ExampleStyle);
        Assert.True(enhancedBrief.PromptModifiers.EnableChainOfThought);
        Assert.Equal("v2", enhancedBrief.PromptModifiers.PromptVersion);
    }

    [Fact]
    public async Task TightenClaimsAsync_StillWorksForPostHocValidation()
    {
        // Arrange - This tests that the original post-hoc validation still works
        var script = "Research shows that users prefer simplicity [Citation 1]. Many people believe dark mode is easier on the eyes.";
        var ragContext = CreateTestRagContext();

        // Act
        var (enhancedScript, warnings) = await _enhancer.TightenClaimsAsync(script, ragContext, CancellationToken.None);

        // Assert
        Assert.NotNull(enhancedScript);
        Assert.NotNull(warnings);
        // The script should be returned (post-hoc validation doesn't modify it much currently)
        Assert.Equal(script, enhancedScript);
    }

    [Fact]
    public async Task TightenClaimsAsync_DetectsUncitedFactualClaims()
    {
        // Arrange
        var script = "Studies have found that exercise is beneficial. Research indicates that sleep is important. Users love the product [Citation 1].";
        var ragContext = CreateTestRagContext();

        // Act
        var (_, warnings) = await _enhancer.TightenClaimsAsync(script, ragContext, CancellationToken.None);

        // Assert
        Assert.NotEmpty(warnings);
        // Should detect uncited claims with factual keywords
        Assert.Contains(warnings, w => w.Contains("uncited") || w.Contains("Studies have found"));
    }

    [Fact]
    public async Task EnhanceBriefWithRagAsync_WithNullRagConfiguration_SkipsEnhancement()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Educate",
            Tone: "Professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9,
            RagConfiguration: null);

        // Act
        var (enhancedBrief, ragContext) = await _enhancer.EnhanceBriefWithRagAsync(brief, CancellationToken.None);

        // Assert
        Assert.Equal(brief, enhancedBrief);
        Assert.Null(ragContext);

        // Should not call the context builder
        _ragContextBuilderMock.Verify(
            r => r.BuildContextAsync(It.IsAny<string>(), It.IsAny<RagConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Brief CreateBriefWithTightenClaims(bool tightenClaims)
    {
        return new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Educate",
            Tone: "Professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9,
            RagConfiguration: new RagConfiguration(
                Enabled: true,
                TopK: 5,
                MinimumScore: 0.7f,
                MaxContextTokens: 2000,
                IncludeCitations: true,
                TightenClaims: tightenClaims));
    }

    private static RagContext CreateTestRagContext()
    {
        return new RagContext
        {
            Query = "Test Topic",
            Chunks = new List<ContextChunk>
            {
                new ContextChunk
                {
                    Content = "Test content about the topic with factual information.",
                    Source = "test-document.pdf",
                    Section = "Introduction",
                    PageNumber = 1,
                    RelevanceScore = 0.9f,
                    CitationNumber = 1
                },
                new ContextChunk
                {
                    Content = "Additional reference material with more details.",
                    Source = "reference-guide.pdf",
                    Section = "Details",
                    PageNumber = 5,
                    RelevanceScore = 0.85f,
                    CitationNumber = 2
                }
            },
            Citations = new List<Citation>
            {
                new Citation
                {
                    Number = 1,
                    Source = "test-document.pdf",
                    Section = "Introduction",
                    PageNumber = 1
                },
                new Citation
                {
                    Number = 2,
                    Source = "reference-guide.pdf",
                    Section = "Details",
                    PageNumber = 5
                }
            },
            FormattedContext = "# Reference Material\n\nTest content about the topic.",
            TotalTokens = 100
        };
    }
}
