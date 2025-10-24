using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Xunit;
using Xunit.Abstractions;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Tests.Performance;

/// <summary>
/// Performance benchmarks for prompt generation
/// </summary>
public class PromptGenerationBenchmark
{
    private readonly ITestOutputHelper _output;

    public PromptGenerationBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ScriptPromptGeneration_Should_BeUnder50ms()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Machine Learning Basics",
            Audience: "Beginners",
            Goal: "Education",
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

        // Act - Measure time
        var sw = Stopwatch.StartNew();
        
        for (int i = 0; i < 100; i++)
        {
            _ = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);
        }
        
        sw.Stop();
        
        var averageMs = sw.ElapsedMilliseconds / 100.0;

        // Assert
        _output.WriteLine($"Average script prompt generation time: {averageMs:F2}ms");
        Assert.True(averageMs < 50, $"Prompt generation should be under 50ms, was {averageMs:F2}ms");

        await Task.CompletedTask;
    }

    [Fact]
    public async Task VisualPromptGeneration_Should_BeUnder10ms()
    {
        // Act - Measure time
        var sw = Stopwatch.StartNew();
        
        for (int i = 0; i < 1000; i++)
        {
            _ = EnhancedPromptTemplates.BuildVisualSelectionPrompt(
                "Test Scene",
                "Test content",
                "professional",
                i
            );
        }
        
        sw.Stop();
        
        var averageMs = sw.ElapsedMilliseconds / 1000.0;

        // Assert
        _output.WriteLine($"Average visual prompt generation time: {averageMs:F2}ms");
        Assert.True(averageMs < 10, $"Visual prompt generation should be under 10ms, was {averageMs:F2}ms");

        await Task.CompletedTask;
    }

    [Fact]
    public async Task QualityPromptGeneration_Should_BeUnder10ms()
    {
        // Arrange
        var testScript = "Test script for quality validation prompt generation benchmark";

        // Act - Measure time
        var sw = Stopwatch.StartNew();
        
        for (int i = 0; i < 1000; i++)
        {
            _ = EnhancedPromptTemplates.BuildQualityValidationPrompt(testScript, "educational");
        }
        
        sw.Stop();
        
        var averageMs = sw.ElapsedMilliseconds / 1000.0;

        // Assert
        _output.WriteLine($"Average quality prompt generation time: {averageMs:F2}ms");
        Assert.True(averageMs < 10, $"Quality prompt generation should be under 10ms, was {averageMs:F2}ms");

        await Task.CompletedTask;
    }

    [Fact]
    public async Task SystemPromptRetrieval_Should_BeImmediate()
    {
        // Act - Measure time
        var sw = Stopwatch.StartNew();
        
        for (int i = 0; i < 10000; i++)
        {
            _ = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
            _ = EnhancedPromptTemplates.GetSystemPromptForVisualSelection();
            _ = EnhancedPromptTemplates.GetSystemPromptForQualityValidation();
        }
        
        sw.Stop();
        
        var averageMs = sw.ElapsedMilliseconds / 30000.0;

        // Assert
        _output.WriteLine($"Average system prompt retrieval time: {averageMs:F4}ms");
        Assert.True(averageMs < 1, $"System prompt retrieval should be under 1ms, was {averageMs:F4}ms");

        await Task.CompletedTask;
    }

    [Fact]
    public async Task PromptGeneration_MemoryUsage_Should_BeReasonable()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "Test Audience",
            Goal: "Test Goal",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "test"
        );

        // Act - Generate many prompts
        for (int i = 0; i < 1000; i++)
        {
            _ = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsedMB = (finalMemory - initialMemory) / 1024.0 / 1024.0;

        // Assert - Should not use excessive memory
        _output.WriteLine($"Memory used for 1000 prompt generations: {memoryUsedMB:F2} MB");
        Assert.True(memoryUsedMB < 10, $"Should use less than 10MB, used {memoryUsedMB:F2}MB");

        await Task.CompletedTask;
    }
}
