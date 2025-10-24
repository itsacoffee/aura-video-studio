using System;
using System.Threading;
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
/// Integration tests for AI Quality System
/// Tests EnhancedPromptTemplates with IntelligentContentAdvisor
/// </summary>
public class AIQualitySystemIntegrationTests
{
    [Fact]
    public async Task EnhancedPromptTemplates_Should_GenerateValidScriptPrompt()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Introduction to Machine Learning",
            Audience: "Beginners",
            Goal: "Educational tutorial",
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

        // Act
        var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
        var userPrompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);

        // Assert
        Assert.NotNull(systemPrompt);
        Assert.NotEmpty(systemPrompt);
        Assert.Contains("CORE PRINCIPLES", systemPrompt);
        Assert.Contains("CONTENT QUALITY STANDARDS", systemPrompt);
        Assert.Contains("AVOID AI DETECTION FLAGS", systemPrompt);

        Assert.NotNull(userPrompt);
        Assert.NotEmpty(userPrompt);
        Assert.Contains("Introduction to Machine Learning", userPrompt);
        Assert.Contains("Beginners", userPrompt);
        Assert.Contains("3.0 minutes", userPrompt);
        Assert.Contains("Conversational", userPrompt);
    }

    [Fact]
    public async Task IntelligentContentAdvisor_Should_DetectLowQualityContent()
    {
        // Arrange - Script with obvious quality issues
        var lowQualityScript = @"
            In today's video, we're going to delve into AI.
            It's important to note that AI is revolutionary.
            Firstly, AI can do many things.
            Secondly, it's a game changer.
            Thirdly, don't forget to like and subscribe!
        ";

        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        var brief = new Brief(
            Topic: "AI Technology",
            Audience: "General",
            Goal: "Educational",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "educational"
        );

        // Act
        var analysis = await advisor.AnalyzeContentQualityAsync(
            lowQualityScript,
            brief,
            spec
        );

        // Assert
        Assert.NotNull(analysis);
        Assert.True(analysis.OverallScore < 75, "Low quality script should score below 75");
        Assert.False(analysis.PassesQualityThreshold);
        Assert.NotEmpty(analysis.Issues);
        Assert.NotEmpty(analysis.Suggestions);
        
        // Should detect specific issues
        var issuesText = string.Join(" ", analysis.Issues);
        Assert.Contains("AI", issuesText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IntelligentContentAdvisor_Should_ApproveHighQualityContent()
    {
        // Arrange - High quality script
        var highQualityScript = @"
            Machine learning transforms how computers learn from experience.
            Instead of following rigid rules, ML systems discover patterns in data.
            
            Consider how you learned to ride a bike. You didn't memorize equations.
            You practiced, adjusted, and improved through trial and error.
            Machine learning works similarly - algorithms improve through experience.
            
            Three key approaches exist: supervised learning uses labeled examples,
            unsupervised learning finds hidden patterns, and reinforcement learning
            learns through rewards and consequences.
            
            Real applications include recommendation systems that suggest content you'll enjoy,
            medical diagnosis tools that identify diseases from scans, and voice assistants
            that understand natural speech.
            
            The technology continues evolving, making computers more capable
            while remaining tools that extend human capabilities.
        ";

        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );

        var brief = new Brief(
            Topic: "Machine Learning Introduction",
            Audience: "Beginners",
            Goal: "Educational",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "educational"
        );

        // Act
        var analysis = await advisor.AnalyzeContentQualityAsync(
            highQualityScript,
            brief,
            spec
        );

        // Assert
        Assert.NotNull(analysis);
        Assert.True(analysis.OverallScore >= 75, "High quality script should score 75 or above");
        Assert.True(analysis.PassesQualityThreshold);
        Assert.True(analysis.AuthenticityScore > 0);
        Assert.True(analysis.EngagementScore > 0);
    }

    [Fact]
    public void EnhancedPromptTemplates_Should_GenerateQualityValidationPrompt()
    {
        // Arrange
        var script = "Test script content for quality validation";
        var contentType = "educational";

        // Act
        var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForQualityValidation();
        var validationPrompt = EnhancedPromptTemplates.BuildQualityValidationPrompt(script, contentType);

        // Assert
        Assert.NotNull(systemPrompt);
        Assert.Contains("quality control expert", systemPrompt, StringComparison.OrdinalIgnoreCase);
        
        Assert.NotNull(validationPrompt);
        Assert.Contains("EDUCATIONAL", validationPrompt);
        Assert.Contains("Test script content", validationPrompt);
        Assert.Contains("EVALUATION CRITERIA", validationPrompt);
        Assert.Contains("Authenticity", validationPrompt);
        Assert.Contains("Engagement", validationPrompt);
    }

    [Fact]
    public void EnhancedPromptTemplates_Should_GenerateVisualSelectionPrompt()
    {
        // Arrange
        var sceneHeading = "Introduction Scene";
        var sceneContent = "Opening shot showing a modern workspace with technology";
        var tone = "professional";
        var sceneIndex = 0;

        // Act
        var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForVisualSelection();
        var visualPrompt = EnhancedPromptTemplates.BuildVisualSelectionPrompt(
            sceneHeading,
            sceneContent,
            tone,
            sceneIndex
        );

        // Assert
        Assert.NotNull(systemPrompt);
        Assert.Contains("visual director", systemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("VISUAL QUALITY PRINCIPLES", systemPrompt);
        
        Assert.NotNull(visualPrompt);
        Assert.Contains("Introduction Scene", visualPrompt);
        Assert.Contains("modern workspace", visualPrompt);
        Assert.Contains("professional", visualPrompt);
        Assert.Contains("SCENE 1", visualPrompt);
    }

    [Fact]
    public async Task IntelligentContentAdvisor_Should_HandleTimeout()
    {
        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
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
            TargetDuration: TimeSpan.FromSeconds(10),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "test"
        );

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(1)); // Very short timeout

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await advisor.AnalyzeContentQualityAsync(
                "Test script",
                brief,
                spec,
                cts.Token
            )
        );
    }

    [Theory]
    [InlineData("informative")]
    [InlineData("narrative")]
    [InlineData("humorous")]
    [InlineData("professional")]
    public async Task EnhancedPromptTemplates_Should_GeneratePromptForAllTones(string tone)
    {
        // Arrange
        var brief = new Brief(
            Topic: $"Test Topic for {tone} tone",
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
            Style: "test"
        );

        // Act
        var prompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);

        // Assert
        Assert.NotNull(prompt);
        Assert.NotEmpty(prompt);
        Assert.Contains(tone, prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SPECIFIC GUIDELINES", prompt);
    }
}
