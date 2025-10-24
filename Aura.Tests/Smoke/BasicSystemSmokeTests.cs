using System;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Tests.Smoke;

/// <summary>
/// Quick smoke tests that run on every commit to verify basic functionality
/// </summary>
public class BasicSystemSmokeTests
{
    [Fact]
    public void EnhancedPromptTemplates_Should_Initialize()
    {
        // Quick check that prompt templates can be accessed
        var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
        Assert.NotNull(systemPrompt);
        Assert.NotEmpty(systemPrompt);
    }

    [Fact]
    public void EnhancedPromptTemplates_Should_GeneratePrompts()
    {
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

        var prompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);
        Assert.NotNull(prompt);
        Assert.NotEmpty(prompt);
    }

    [Fact]
    public async Task RuleBasedProvider_Should_Work()
    {
        // Verify local provider works without external dependencies
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var brief = new Brief(
            Topic: "Smoke Test",
            Audience: null,
            Goal: null,
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "test"
        );

        var script = await provider.DraftScriptAsync(brief, spec);
        Assert.NotNull(script);
        Assert.NotEmpty(script);
    }

    [Fact]
    public async Task QualitySystem_Should_Initialize()
    {
        // Verify quality system can be created
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            provider
        );

        Assert.NotNull(advisor);

        // Quick analysis test
        var brief = new Brief(
            Topic: "Test",
            Audience: null,
            Goal: null,
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(10),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "test"
        );

        var analysis = await advisor.AnalyzeContentQualityAsync("Test script", brief, spec);
        Assert.NotNull(analysis);
    }

    [Fact]
    public void Models_Should_Initialize()
    {
        // Verify core models can be created
        var brief = new Brief(
            Topic: "Test",
            Audience: "Test",
            Goal: "Test",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        Assert.NotNull(brief);
        Assert.Equal("Test", brief.Topic);
        Assert.Equal("en", brief.Language);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "test"
        );

        Assert.NotNull(spec);
        Assert.Equal(TimeSpan.FromMinutes(1), spec.TargetDuration);
    }

    [Fact]
    public void AllPromptTemplates_Should_BeAccessible()
    {
        // Verify all prompt template methods are accessible
        var scriptPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
        var visualPrompt = EnhancedPromptTemplates.GetSystemPromptForVisualSelection();
        var qualityPrompt = EnhancedPromptTemplates.GetSystemPromptForQualityValidation();

        Assert.NotNull(scriptPrompt);
        Assert.NotNull(visualPrompt);
        Assert.NotNull(qualityPrompt);

        Assert.NotEmpty(scriptPrompt);
        Assert.NotEmpty(visualPrompt);
        Assert.NotEmpty(qualityPrompt);
    }

    [Fact]
    public void NoRuntimeConfigurationErrors()
    {
        // This test passes if there are no runtime initialization errors
        // Simply creating these objects should not throw
        
        try
        {
            var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
            var advisor = new IntelligentContentAdvisor(
                NullLogger<IntelligentContentAdvisor>.Instance,
                provider
            );

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

            Assert.True(true, "No runtime configuration errors");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Runtime configuration error: {ex.Message}");
        }
    }
}
