using System;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Tests.Integration;

/// <summary>
/// Tests the content quality pipeline: generation → analysis → regeneration → validation
/// </summary>
public class ContentQualityPipelineTests
{
    [Fact]
    public async Task QualityPipeline_Should_IdentifyAndImproveContent()
    {
        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        var brief = new Brief(
            Topic: "Artificial Intelligence Applications",
            Audience: "General public",
            Goal: "Educational overview",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "educational"
        );

        // Act - Step 1: Generate initial content
        var initialScript = await llmProvider.DraftScriptAsync(brief, spec, default);
        
        // Act - Step 2: Analyze quality
        var analysis = await advisor.AnalyzeContentQualityAsync(initialScript, brief, spec);

        // Assert - Pipeline functionality
        Assert.NotNull(initialScript);
        Assert.NotNull(analysis);
        Assert.NotNull(analysis.Issues);
        Assert.NotNull(analysis.Suggestions);
        Assert.True(analysis.OverallScore >= 0 && analysis.OverallScore <= 100);

        // If quality is low, suggestions should be available for regeneration
        if (!analysis.PassesQualityThreshold)
        {
            Assert.NotEmpty(analysis.Suggestions);
            Assert.NotEmpty(analysis.Issues);
        }
    }

    [Fact]
    public async Task QualityPipeline_Should_DetectAIGeneratedPatterns()
    {
        // Arrange - Script with obvious AI patterns
        var problematicScript = @"
            In today's digital landscape, it's important to note that artificial intelligence 
            is revolutionizing various industries. Delving into this topic, we can see that 
            AI is a game-changer. Firstly, machine learning enables computers to learn. 
            Secondly, natural language processing helps computers understand text. 
            Moreover, it is essential to recognize that AI will continue to evolve.
            In conclusion, it's clear that AI is transforming our world.
        ";

        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        var brief = new Brief(
            Topic: "AI Overview",
            Audience: "General",
            Goal: "Education",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "educational"
        );

        // Act
        var analysis = await advisor.AnalyzeContentQualityAsync(problematicScript, brief, spec);

        // Assert - Should detect AI patterns
        Assert.NotNull(analysis);
        Assert.NotEmpty(analysis.Issues);
        
        var issuesText = string.Join(" ", analysis.Issues).ToLowerInvariant();
        Assert.True(
            issuesText.Contains("ai") || 
            issuesText.Contains("generic") || 
            issuesText.Contains("cliché") ||
            issuesText.Contains("repetitive"),
            "Should detect AI-generated patterns"
        );
    }

    [Fact]
    public async Task QualityPipeline_Should_ValidateQualityThreshold()
    {
        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        var brief = new Brief(
            Topic: "Web Development Fundamentals",
            Audience: "Beginners",
            Goal: "Tutorial",
            Tone: "conversational",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(4),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "tutorial"
        );

        // Act
        var script = await llmProvider.DraftScriptAsync(brief, spec, default);
        var analysis = await advisor.AnalyzeContentQualityAsync(script, brief, spec);

        // Assert - Quality threshold validation
        Assert.NotNull(analysis);
        
        // PassesQualityThreshold should match OverallScore >= 75
        var expectedPass = analysis.OverallScore >= 75.0;
        Assert.Equal(expectedPass, analysis.PassesQualityThreshold);
    }

    [Fact]
    public async Task QualityPipeline_Should_ProvideComponentScores()
    {
        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        var script = @"
            JavaScript has become essential for modern web development.
            It powers interactive features on virtually every website.
            
            Understanding JavaScript fundamentals opens doors to:
            Frontend frameworks like React and Vue
            Backend development with Node.js
            Mobile apps through React Native
            
            Start with variables, functions, and DOM manipulation.
            Build small projects to apply what you learn.
            Join coding communities for support and inspiration.
        ";

        var brief = new Brief(
            Topic: "JavaScript Basics",
            Audience: "Aspiring developers",
            Goal: "Tutorial",
            Tone: "conversational",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "tutorial"
        );

        // Act
        var analysis = await advisor.AnalyzeContentQualityAsync(script, brief, spec);

        // Assert - All component scores should be present
        Assert.NotNull(analysis);
        Assert.True(analysis.AuthenticityScore >= 0);
        Assert.True(analysis.EngagementScore >= 0);
        Assert.True(analysis.ValueScore >= 0);
        Assert.True(analysis.PacingScore >= 0);
        Assert.True(analysis.OriginalityScore >= 0);
        
        // Overall score should be related to component scores
        Assert.True(analysis.OverallScore >= 0);
        Assert.True(analysis.OverallScore <= 100);
    }

    [Fact]
    public async Task QualityPipeline_Should_DetectGenericPhrases()
    {
        // Arrange - Script with generic phrases
        var genericScript = @"
            In today's video, we're going to talk about productivity.
            Don't forget to like and subscribe!
            This is a game changer that will revolutionize your workflow.
            At the end of the day, it's all about being more productive.
            This cutting-edge approach will unlock the secrets to success.
        ";

        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        var brief = new Brief(
            Topic: "Productivity Tips",
            Audience: "Professionals",
            Goal: "Tips",
            Tone: "conversational",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "tips"
        );

        // Act
        var analysis = await advisor.AnalyzeContentQualityAsync(genericScript, brief, spec);

        // Assert
        Assert.NotNull(analysis);
        Assert.NotEmpty(analysis.Issues);
        Assert.True(analysis.OverallScore < 75, "Generic script should score below threshold");
    }

    [Fact]
    public async Task QualityPipeline_Should_IterativelyImprove()
    {
        // This test simulates iterative improvement workflow
        // In real implementation, this would regenerate based on suggestions

        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        var brief = new Brief(
            Topic: "Cloud Computing Basics",
            Audience: "Business owners",
            Goal: "Educational",
            Tone: "professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "professional education"
        );

        // Act - Iteration 1
        var script1 = await llmProvider.DraftScriptAsync(brief, spec, default);
        var analysis1 = await advisor.AnalyzeContentQualityAsync(script1, brief, spec);

        // If quality is low, we would use suggestions to regenerate
        // For this test, we'll just verify the feedback is actionable
        Assert.NotNull(analysis1);
        
        if (!analysis1.PassesQualityThreshold)
        {
            Assert.NotEmpty(analysis1.Suggestions);
            
            // Suggestions should be specific and actionable
            foreach (var suggestion in analysis1.Suggestions)
            {
                Assert.NotEmpty(suggestion);
                // Suggestions should not be too short to be meaningful
                Assert.True(suggestion.Length > 15);
            }
        }
    }

    [Fact]
    public async Task QualityPipeline_Should_PreventPoorContentProgression()
    {
        // Test that quality threshold prevents poor content from progressing

        // Arrange - Intentionally poor quality script
        var poorScript = @"
            In today's video. We talk about stuff.
            It's important to note. Things are revolutionary.
            Delve into this topic. Many aspects exist.
            Game changer for sure. Don't forget to subscribe.
        ";

        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "Test",
            Goal: "Test",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "test"
        );

        // Act
        var analysis = await advisor.AnalyzeContentQualityAsync(poorScript, brief, spec);

        // Assert - Poor content should not pass
        Assert.NotNull(analysis);
        Assert.False(analysis.PassesQualityThreshold, "Poor quality content should not pass threshold");
        Assert.True(analysis.OverallScore < 75, "Poor content should score below 75");
        Assert.NotEmpty(analysis.Issues);
    }
}
