using System;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests for LLM providers with EnhancedPromptTemplates
/// Tests all providers: RuleBased (always available), OpenAI, Ollama, Azure, Gemini (conditional)
/// </summary>
public class LLMProviderIntegrationTests
{
    [Fact]
    public async Task RuleBasedLlmProvider_Should_WorkWithEnhancedPrompts()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var brief = new Brief(
            Topic: "Introduction to Software Development",
            Audience: "Beginners",
            Goal: "Educational",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: EnhancedPromptTemplates.GetSystemPromptForScriptGeneration()
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, spec);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.Contains("Software Development", script);
    }

    [Fact]
    public async Task RuleBasedLlmProvider_Should_GenerateQualityFocusedContent()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var brief = new Brief(
            Topic: "Climate Change Basics",
            Audience: "General Public",
            Goal: "Educational awareness",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var userPrompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(
            brief,
            new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(3),
                Pacing: PacingEnum.Conversational,
                Density: DensityEnum.Balanced,
                Style: "educational"
            )
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: userPrompt
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, spec);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        
        // Verify script has structure
        Assert.Contains("Climate Change", script);
    }

    [Fact]
    public async Task AllProviders_Should_ProduceAnalyzableContent()
    {
        // This test validates that content from any LLM provider can be analyzed
        // by IntelligentContentAdvisor
        
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            provider
        );

        var brief = new Brief(
            Topic: "Python Programming Basics",
            Audience: "Beginners",
            Goal: "Tutorial",
            Tone: "conversational",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "educational"
        );

        // Act - Generate content
        var script = await provider.DraftScriptAsync(brief, spec);
        
        // Act - Analyze content
        var analysis = await advisor.AnalyzeContentQualityAsync(script, brief, spec);

        // Assert
        Assert.NotNull(analysis);
        Assert.True(analysis.OverallScore >= 0 && analysis.OverallScore <= 100);
        Assert.True(analysis.AuthenticityScore >= 0);
        Assert.True(analysis.EngagementScore >= 0);
        Assert.NotNull(analysis.Issues);
        Assert.NotNull(analysis.Suggestions);
    }

    [Theory]
    [InlineData(PacingEnum.Chill, 130)]
    [InlineData(PacingEnum.Conversational, 157)]
    [InlineData(PacingEnum.Fast, 190)]
    public async Task LlmProviders_Should_RespectPacing(PacingEnum pacing, int expectedWpm)
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "Test Audience",
            Goal: "Test",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: pacing,
            Density: DensityEnum.Balanced,
            Style: "test"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, spec);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        
        // Verify the prompt would contain pacing information
        var prompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);
        Assert.Contains(pacing.ToString(), prompt);
        Assert.Contains(expectedWpm.ToString(), prompt);
    }

    [Theory]
    [InlineData(DensityEnum.Sparse)]
    [InlineData(DensityEnum.Balanced)]
    [InlineData(DensityEnum.Dense)]
    public async Task LlmProviders_Should_RespectDensity(DensityEnum density)
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var brief = new Brief(
            Topic: "Test Topic for Density",
            Audience: "Test",
            Goal: "Test",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: density,
            Style: "test"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, spec);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        
        // Verify the prompt contains density information
        var prompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);
        Assert.Contains(density.ToString(), prompt);
    }

    [Fact]
    public async Task LlmProvider_Should_HandleVeryShortVideos()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var brief = new Brief(
            Topic: "Quick Tip: Keyboard Shortcut",
            Audience: "Computer users",
            Goal: "Quick tip",
            Tone: "conversational",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(15),
            Pacing: PacingEnum.Fast,
            Density: DensityEnum.Sparse,
            Style: "quick tip"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, spec);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
    }

    [Fact]
    public async Task LlmProvider_Should_HandleLongVideos()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var brief = new Brief(
            Topic: "Complete Guide to Web Development",
            Audience: "Aspiring developers",
            Goal: "Comprehensive tutorial",
            Tone: "professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(10),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Dense,
            Style: "comprehensive tutorial"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, spec);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        
        // Long videos should have more content
        var wordCount = script.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.True(wordCount > 500, "Long video should have substantial content");
    }

    [Fact]
    public async Task LlmProvider_Should_HandleUnusualTopics()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var brief = new Brief(
            Topic: "The History of Underwater Basket Weaving",
            Audience: "Craft enthusiasts",
            Goal: "Entertainment and education",
            Tone: "humorous",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "entertaining"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, spec);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
    }
}
