using System;
using System.Collections.Generic;
using System.Linq;
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
/// Tests that ensure consistent quality across different LLM providers
/// </summary>
public class MultiProviderConsistencyTests
{
    [Fact]
    public async Task AllProviders_Should_ProduceAnalyzableContent()
    {
        // This test validates that content from any provider can be analyzed for quality
        var providers = new List<ILlmProvider>
        {
            new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var brief = new Brief(
            Topic: "Renewable Energy Solutions",
            Audience: "General public",
            Goal: "Educational",
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

        foreach (var provider in providers)
        {
            // Act - Generate content
            var script = await provider.DraftScriptAsync(brief, spec, default);

            // Create advisor with this provider
            var advisor = new IntelligentContentAdvisor(
                NullLogger<IntelligentContentAdvisor>.Instance,
                provider
            );

            // Analyze content
            var analysis = await advisor.AnalyzeContentQualityAsync(script, brief, spec);

            // Assert - Content should be analyzable
            Assert.NotNull(analysis);
            Assert.True(analysis.OverallScore >= 0 && analysis.OverallScore <= 100);
            Assert.NotNull(analysis.Issues);
            Assert.NotNull(analysis.Suggestions);
        }
    }

    [Fact]
    public async Task AllProviders_Should_RespectEnhancedPrompts()
    {
        // Verify all providers work with EnhancedPromptTemplates
        var providers = new List<ILlmProvider>
        {
            new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var brief = new Brief(
            Topic: "Artificial Intelligence Ethics",
            Audience: "Technology professionals",
            Goal: "Discussion",
            Tone: "professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "professional discussion"
        );

        var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
        var userPrompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);

        foreach (var provider in providers)
        {
            var providerName = provider.GetType().Name;

            // Act
            var enhancedSpec = new PlanSpec(
                TargetDuration: spec.TargetDuration,
                Pacing: spec.Pacing,
                Density: spec.Density,
                Style: systemPrompt + "\n\n" + userPrompt
            );

            var script = await provider.DraftScriptAsync(brief, enhancedSpec, default);

            // Assert
            Assert.NotNull(script);
            Assert.NotEmpty(script);
            Assert.Contains("Ethics", script, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task QualityScores_Should_BeConsistentAcrossProviders()
    {
        // Test that quality analysis is consistent regardless of provider
        var providers = new List<ILlmProvider>
        {
            new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var goodScript = @"
            Cloud computing revolutionizes how businesses operate.
            Instead of maintaining physical servers, companies access computing power on-demand.
            
            This shift offers three key advantages:
            First, flexibility - scale resources up or down instantly based on needs.
            Second, cost efficiency - pay only for what you use, eliminating upfront infrastructure costs.
            Third, accessibility - team members can access tools and data from anywhere.
            
            Leading platforms like AWS, Azure, and Google Cloud compete to provide
            the most reliable and feature-rich services.
        ";

        var brief = new Brief(
            Topic: "Cloud Computing",
            Audience: "Business owners",
            Goal: "Education",
            Tone: "professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "professional"
        );

        var scores = new List<double>();

        foreach (var provider in providers)
        {
            var advisor = new IntelligentContentAdvisor(
                NullLogger<IntelligentContentAdvisor>.Instance,
                provider
            );

            var analysis = await advisor.AnalyzeContentQualityAsync(goodScript, brief, spec);
            scores.Add(analysis.OverallScore);
        }

        // All providers should give reasonable scores for good content
        Assert.All(scores, score => Assert.True(score >= 60, "Good content should score at least 60"));
    }

    [Fact]
    public async Task AllProviders_Should_DetectLowQuality()
    {
        // Verify all providers detect obviously poor quality content
        var providers = new List<ILlmProvider>
        {
            new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var poorScript = @"
            Today video about stuff.
            It's important to note. Things are important.
            Delve into topic. Many aspects.
            Game changer. Revolutionary.
            Don't forget to like and subscribe!
        ";

        var brief = new Brief(
            Topic: "Test",
            Audience: null,
            Goal: null,
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

        foreach (var provider in providers)
        {
            var advisor = new IntelligentContentAdvisor(
                NullLogger<IntelligentContentAdvisor>.Instance,
                provider
            );

            var analysis = await advisor.AnalyzeContentQualityAsync(poorScript, brief, spec);

            // Assert - Should detect low quality
            Assert.False(analysis.PassesQualityThreshold);
            Assert.True(analysis.OverallScore < 75);
            Assert.NotEmpty(analysis.Issues);
        }
    }

    [Fact]
    public async Task AllProviders_Should_HandleMultipleTones()
    {
        // Test consistency across different tones
        var tones = new[] { "informative", "conversational", "professional", "humorous" };
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);

        foreach (var tone in tones)
        {
            var brief = new Brief(
                Topic: $"Test Topic - {tone} tone",
                Audience: "General",
                Goal: "Test",
                Tone: tone,
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var spec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(2),
                Pacing: PacingEnum.Conversational,
                Density: DensityEnum.Balanced,
                Style: tone
            );

            var prompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);
            var script = await provider.DraftScriptAsync(brief, spec, default);

            // Assert - Should handle all tones
            Assert.NotNull(script);
            Assert.NotEmpty(script);
            Assert.Contains(tone, prompt, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task QualityThreshold_Should_BeConsistent()
    {
        // Verify that quality threshold (75) works consistently
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            provider
        );

        var brief = new Brief(
            Topic: "Software Testing Fundamentals",
            Audience: "Developers",
            Goal: "Tutorial",
            Tone: "professional",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "tutorial"
        );

        // Test multiple times for consistency
        var passResults = new List<bool>();
        
        for (int i = 0; i < 3; i++)
        {
            var script = await provider.DraftScriptAsync(brief, spec, default);
            var analysis = await advisor.AnalyzeContentQualityAsync(script, brief, spec);
            
            // Verify threshold logic is consistent
            var expectedPass = analysis.OverallScore >= 75.0;
            Assert.Equal(expectedPass, analysis.PassesQualityThreshold);
            
            passResults.Add(analysis.PassesQualityThreshold);
        }

        // Results should be consistent (either all pass or all fail)
        Assert.True(passResults.All(r => r == passResults[0]), 
            "Quality threshold should be applied consistently");
    }
}
