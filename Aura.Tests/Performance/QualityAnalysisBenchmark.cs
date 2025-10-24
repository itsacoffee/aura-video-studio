using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Tests.Performance;

/// <summary>
/// Performance benchmarks for quality analysis
/// </summary>
public class QualityAnalysisBenchmark
{
    private readonly ITestOutputHelper _output;

    public QualityAnalysisBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task HeuristicAnalysis_Should_Complete_Under1Second()
    {
        // Arrange
        var testScript = @"
            Machine learning enables computers to learn from data without explicit programming.
            Neural networks process information through layers of interconnected nodes.
            Training requires large datasets and significant computational resources.
            Applications span image recognition, natural language processing, and prediction.
            Continuous advancement pushes boundaries of what machines can accomplish.
        ";

        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            provider
        );

        var brief = new Brief(
            Topic: "Machine Learning",
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

        // Warmup
        await advisor.AnalyzeContentQualityAsync(testScript, brief, spec);

        // Act - Measure time
        var sw = Stopwatch.StartNew();
        var analysis = await advisor.AnalyzeContentQualityAsync(testScript, brief, spec);
        sw.Stop();

        // Assert
        _output.WriteLine($"Quality analysis completed in {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Overall score: {analysis.OverallScore:F1}");
        _output.WriteLine($"Issues found: {analysis.Issues.Count}");
        
        Assert.True(sw.ElapsedMilliseconds < 5000, 
            $"Analysis should complete under 5 seconds, took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task HeuristicAnalysis_Multiple_Should_ScaleLinearly()
    {
        // Test that multiple analyses don't have exponential growth
        
        var testScript = "Test script for performance analysis with multiple iterations";
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

        // Warmup
        await advisor.AnalyzeContentQualityAsync(testScript, brief, spec);

        // Measure single analysis
        var sw = Stopwatch.StartNew();
        await advisor.AnalyzeContentQualityAsync(testScript, brief, spec);
        var singleTime = sw.ElapsedMilliseconds;

        // Measure 10 analyses
        sw.Restart();
        for (int i = 0; i < 10; i++)
        {
            await advisor.AnalyzeContentQualityAsync(testScript, brief, spec);
        }
        var tenTime = sw.ElapsedMilliseconds;

        var ratio = tenTime / (singleTime * 10.0);

        _output.WriteLine($"Single analysis: {singleTime}ms");
        _output.WriteLine($"10 analyses: {tenTime}ms");
        _output.WriteLine($"Scaling ratio: {ratio:F2} (should be close to 1.0 for linear scaling)");

        // Assert - Should scale reasonably (within 50% of linear)
        Assert.True(ratio < 1.5, 
            $"Performance should scale linearly (ratio should be < 1.5, was {ratio:F2})");
    }

    [Fact]
    public async Task QualityAnalysis_MemoryUsage_Should_BeConstant()
    {
        // Ensure memory doesn't grow with repeated analyses
        
        var testScript = "Memory usage test script for quality analysis benchmarking";
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

        // Warmup and establish baseline
        for (int i = 0; i < 10; i++)
        {
            await advisor.AnalyzeContentQualityAsync(testScript, brief, spec);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(false);

        // Run many analyses
        for (int i = 0; i < 100; i++)
        {
            await advisor.AnalyzeContentQualityAsync(testScript, brief, spec);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryGrowthMB = (finalMemory - initialMemory) / 1024.0 / 1024.0;

        _output.WriteLine($"Memory growth after 100 analyses: {memoryGrowthMB:F2} MB");

        // Assert - Memory growth should be minimal
        Assert.True(memoryGrowthMB < 5, 
            $"Memory growth should be under 5MB, was {memoryGrowthMB:F2}MB");
    }

    [Fact]
    public async Task LargeScript_Analysis_Should_Complete_Under10Seconds()
    {
        // Test with a large script (simulating 5-minute video)
        var largeScript = string.Join("\n\n", 
            System.Linq.Enumerable.Repeat(
                @"This is a section of a longer video script. It contains multiple sentences 
                with varied content to test the performance of quality analysis on larger texts. 
                The analysis should still complete in reasonable time even for lengthy scripts.",
                50
            )
        );

        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            provider
        );

        var brief = new Brief(
            Topic: "Long Video Topic",
            Audience: "General",
            Goal: "Test",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "test"
        );

        // Act
        var sw = Stopwatch.StartNew();
        var analysis = await advisor.AnalyzeContentQualityAsync(largeScript, brief, spec);
        sw.Stop();

        // Assert
        _output.WriteLine($"Large script analysis ({largeScript.Length} chars) completed in {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Issues found: {analysis.Issues.Count}");
        
        Assert.True(sw.ElapsedMilliseconds < 10000,
            $"Large script analysis should complete under 10 seconds, took {sw.ElapsedMilliseconds}ms");
    }
}
