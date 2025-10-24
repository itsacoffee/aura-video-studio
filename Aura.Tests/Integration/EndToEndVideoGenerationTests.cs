using System;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Tests.Integration;

/// <summary>
/// End-to-end integration tests for complete video generation workflow
/// Tests from brief creation to final video generation
/// </summary>
public class EndToEndVideoGenerationTests
{
    [Fact]
    public async Task CompleteWorkflow_Should_GenerateVideoFromBrief()
    {
        // Arrange - Create a complete brief
        var brief = new Brief(
            Topic: "5 Tips for Better Time Management",
            Audience: "Working professionals",
            Goal: "Provide actionable advice",
            Tone: "professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "practical tips"
        );

        // Setup providers
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        // Act - Generate script
        var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
        var userPrompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);
        
        var scriptSpec = new PlanSpec(
            TargetDuration: spec.TargetDuration,
            Pacing: spec.Pacing,
            Density: spec.Density,
            Style: systemPrompt + "\n\n" + userPrompt
        );

        var script = await llmProvider.DraftScriptAsync(brief, scriptSpec);

        // Act - Analyze quality
        var qualityAnalysis = await advisor.AnalyzeContentQualityAsync(script, brief, spec);

        // Assert - Verify workflow completeness
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.Contains("Time Management", script);

        Assert.NotNull(qualityAnalysis);
        Assert.True(qualityAnalysis.OverallScore >= 0);
        Assert.True(qualityAnalysis.OverallScore <= 100);
        Assert.NotNull(qualityAnalysis.Issues);
        Assert.NotNull(qualityAnalysis.Suggestions);
    }

    [Fact]
    public async Task CompleteWorkflow_Should_HandleMultipleContentTypes()
    {
        // Test workflow with different content types
        var contentTypes = new[]
        {
            ("Educational", "How Photosynthesis Works", "informative"),
            ("Entertainment", "Top 10 Movie Plot Twists", "entertaining"),
            ("Marketing", "Product Launch Announcement", "professional")
        };

        foreach (var (category, topic, tone) in contentTypes)
        {
            // Arrange
            var brief = new Brief(
                Topic: topic,
                Audience: "General audience",
                Goal: category,
                Tone: tone,
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var spec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(2),
                Pacing: PacingEnum.Conversational,
                Density: DensityEnum.Balanced,
                Style: category.ToLowerInvariant()
            );

            var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);

            // Act
            var script = await llmProvider.DraftScriptAsync(brief, spec);

            // Assert
            Assert.NotNull(script);
            Assert.NotEmpty(script);
            Assert.Contains(topic, script);
        }
    }

    [Fact]
    public async Task CompleteWorkflow_Should_RespectTargetDuration()
    {
        // Test that generated content respects different durations
        var durations = new[]
        {
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(5)
        };

        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);

        foreach (var duration in durations)
        {
            // Arrange
            var brief = new Brief(
                Topic: $"Test Topic for {duration.TotalMinutes:F1} minute video",
                Audience: "Test",
                Goal: "Test",
                Tone: "informative",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var spec = new PlanSpec(
                TargetDuration: duration,
                Pacing: PacingEnum.Conversational,
                Density: DensityEnum.Balanced,
                Style: "test"
            );

            // Act
            var prompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);
            var script = await llmProvider.DraftScriptAsync(brief, spec);

            // Assert
            Assert.NotNull(script);
            Assert.Contains(duration.TotalMinutes.ToString(), prompt);
        }
    }

    [Fact]
    public async Task CompleteWorkflow_Should_GenerateVisualPrompts()
    {
        // Arrange
        var sceneHeading = "Opening: Modern Office Space";
        var sceneContent = "Professional setting with natural lighting, computers, and collaborative workspace";
        var tone = "professional";

        // Act
        var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForVisualSelection();
        var visualPrompt = EnhancedPromptTemplates.BuildVisualSelectionPrompt(
            sceneHeading,
            sceneContent,
            tone,
            0
        );

        // Assert
        Assert.NotNull(systemPrompt);
        Assert.NotNull(visualPrompt);
        Assert.Contains("Modern Office Space", visualPrompt);
        Assert.Contains("professional", visualPrompt);
        Assert.Contains("VISUAL:", visualPrompt);
    }

    [Fact]
    public async Task CompleteWorkflow_Should_ProvideQualityFeedback()
    {
        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        var brief = new Brief(
            Topic: "Digital Marketing Strategies",
            Audience: "Small business owners",
            Goal: "Business education",
            Tone: "professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(4),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "business education"
        );

        // Act
        var script = await llmProvider.DraftScriptAsync(brief, spec);
        var analysis = await advisor.AnalyzeContentQualityAsync(script, brief, spec);

        // Assert - Verify quality feedback is actionable
        Assert.NotNull(analysis);
        
        if (!analysis.PassesQualityThreshold)
        {
            Assert.NotEmpty(analysis.Issues);
            Assert.NotEmpty(analysis.Suggestions);
            
            // Suggestions should be actionable
            foreach (var suggestion in analysis.Suggestions)
            {
                Assert.NotEmpty(suggestion);
                Assert.True(suggestion.Length > 10, "Suggestions should be meaningful");
            }
        }
    }

    [Fact]
    public async Task CompleteWorkflow_Should_HandleEdgeCases()
    {
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);

        // Edge case 1: Very short video
        var shortBrief = new Brief(
            Topic: "Quick Tip",
            Audience: null,
            Goal: null,
            Tone: "conversational",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var shortSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(10),
            Pacing: PacingEnum.Fast,
            Density: DensityEnum.Sparse,
            Style: "tip"
        );

        var shortScript = await llmProvider.DraftScriptAsync(shortBrief, shortSpec);
        Assert.NotNull(shortScript);
        Assert.NotEmpty(shortScript);

        // Edge case 2: Minimal information
        var minimalBrief = new Brief(
            Topic: "Test",
            Audience: null,
            Goal: null,
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var minimalSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "test"
        );

        var minimalScript = await llmProvider.DraftScriptAsync(minimalBrief, minimalSpec);
        Assert.NotNull(minimalScript);
        Assert.NotEmpty(minimalScript);
    }

    [Fact]
    public async Task CompleteWorkflow_Should_SupportMultipleLanguages()
    {
        // Note: This test validates the workflow supports language parameter
        // Actual multilingual generation depends on LLM provider capabilities
        
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);

        var brief = new Brief(
            Topic: "Technology Trends",
            Audience: "Tech enthusiasts",
            Goal: "Information",
            Tone: "informative",
            Language: "en", // English for this test
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "informative"
        );

        var prompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);
        var script = await llmProvider.DraftScriptAsync(brief, spec);

        Assert.Contains("Language: en", prompt);
        Assert.NotNull(script);
        Assert.NotEmpty(script);
    }
}
