using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Services;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.Examples;

/// <summary>
/// Example demonstrating the Script Refinement Pipeline
/// Shows how to generate a script with iterative quality improvements
/// </summary>
public class ScriptRefinementExample
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Aura Script Refinement Pipeline Example ===\n");

        // Setup
        var logger = NullLogger<ScriptRefinementOrchestrator>.Instance;
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var contentAdvisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            llmProvider
        );
        var orchestrator = new ScriptRefinementOrchestrator(logger, llmProvider, contentAdvisor);

        // Define the video brief
        var brief = new Brief(
            Topic: "The Future of Renewable Energy",
            Audience: "Environmentally conscious adults",
            Goal: "Educate and inspire action on clean energy",
            Tone: "optimistic yet realistic",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "educational documentary"
        );

        // Configure refinement
        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 2,
            QualityThreshold = 85.0,
            MinimumImprovement = 5.0,
            EnableAdvisorValidation = true,
            PassTimeout = TimeSpan.FromMinutes(2)
        };

        Console.WriteLine($"Brief: {brief.Topic}");
        Console.WriteLine($"Target: {spec.TargetDuration.TotalMinutes:F1} minutes, {brief.Tone} tone");
        Console.WriteLine($"Refinement: Up to {config.MaxRefinementPasses} passes, threshold {config.QualityThreshold}/100\n");
        Console.WriteLine("Starting refinement...\n");

        // Execute refinement
        var result = await orchestrator.RefineScriptAsync(brief, spec, config, CancellationToken.None);

        // Display results
        if (result.Success)
        {
            Console.WriteLine("✅ Refinement Successful!\n");

            Console.WriteLine($"Total Passes: {result.TotalPasses}");
            Console.WriteLine($"Duration: {result.TotalDuration.TotalSeconds:F1} seconds");
            Console.WriteLine($"Stop Reason: {result.StopReason}\n");

            // Show quality progression
            Console.WriteLine("Quality Progression:");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            Console.WriteLine(
                $"{"Iteration",-10} {"Overall",-10} {"Narrative",-12} {"Pacing",-10} {"Audience",-10} {"Visual",-10} {"Engage",-10}");
            Console.WriteLine("─────────────────────────────────────────────────────────────");

            foreach (var metrics in result.IterationMetrics)
            {
                Console.WriteLine(
                    $"{metrics.Iteration,-10} " +
                    $"{metrics.OverallScore,-10:F1} " +
                    $"{metrics.NarrativeCoherence,-12:F1} " +
                    $"{metrics.PacingAppropriateness,-10:F1} " +
                    $"{metrics.AudienceAlignment,-10:F1} " +
                    $"{metrics.VisualClarity,-10:F1} " +
                    $"{metrics.EngagementPotential,-10:F1}");
            }

            Console.WriteLine("─────────────────────────────────────────────────────────────\n");

            // Show improvement
            var improvement = result.GetTotalImprovement();
            if (improvement != null && result.TotalPasses > 1)
            {
                Console.WriteLine("Total Improvement:");
                Console.WriteLine($"  Overall: +{improvement.OverallDelta:F1} points");
                Console.WriteLine($"  Narrative Coherence: +{improvement.NarrativeCoherenceDelta:F1}");
                Console.WriteLine($"  Pacing: +{improvement.PacingDelta:F1}");
                Console.WriteLine($"  Audience Alignment: +{improvement.AudienceDelta:F1}");
                Console.WriteLine($"  Visual Clarity: +{improvement.VisualClarityDelta:F1}");
                Console.WriteLine($"  Engagement: +{improvement.EngagementDelta:F1}\n");
            }

            // Show final script excerpt
            Console.WriteLine("Final Script (first 500 characters):");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            var scriptExcerpt = result.FinalScript.Length > 500
                ? result.FinalScript.Substring(0, 500) + "..."
                : result.FinalScript;
            Console.WriteLine(scriptExcerpt);
            Console.WriteLine("─────────────────────────────────────────────────────────────\n");

            // Show issues and suggestions from final iteration
            if (result.FinalMetrics != null)
            {
                if (result.FinalMetrics.Issues.Count > 0)
                {
                    Console.WriteLine("Remaining Issues:");
                    foreach (var issue in result.FinalMetrics.Issues)
                    {
                        Console.WriteLine($"  • {issue}");
                    }

                    Console.WriteLine();
                }

                if (result.FinalMetrics.Strengths.Count > 0)
                {
                    Console.WriteLine("Strengths:");
                    foreach (var strength in result.FinalMetrics.Strengths)
                    {
                        Console.WriteLine($"  ✓ {strength}");
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine($"Final Quality Score: {result.FinalMetrics?.OverallScore:F1}/100");
        }
        else
        {
            Console.WriteLine($"❌ Refinement Failed: {result.ErrorMessage}");
        }

        Console.WriteLine("\n=== Example Complete ===");
    }
}
