using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Content;

/// <summary>
/// Enhances video scripts with AI-powered improvements for coherence, engagement, clarity, and detail
/// </summary>
public class ScriptEnhancer
{
    private readonly ILogger<ScriptEnhancer> _logger;
    private readonly ILlmProvider _llmProvider;

    public ScriptEnhancer(ILogger<ScriptEnhancer> logger, ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Enhances a script based on the provided options
    /// </summary>
    public async Task<EnhancedScript> EnhanceScriptAsync(
        string originalScript, 
        EnhancementOptions options, 
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Enhancing script with options: Coherence={FixCoherence}, Engagement={IncreaseEngagement}, Clarity={ImproveClarity}, Details={AddDetails}",
            options.FixCoherence, options.IncreaseEngagement, options.ImproveClarity, options.AddDetails
        );

        try
        {
            // Build enhancement instructions
            var enhancementInstructions = BuildEnhancementInstructions(options);

            // Create enhancement prompt
            var prompt = $@"Enhance this video script to improve {enhancementInstructions}. 
Maintain the original structure and key points but make improvements where needed. 
Return the enhanced script with the same scene structure (# title, ## scene headings).

Original Script:
{originalScript}

Enhanced Script:";

            // Create a minimal brief and plan spec for the LLM call
            var brief = new Brief(
                Topic: "Script Enhancement",
                Audience: null,
                Goal: null,
                Tone: "professional",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(5),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: prompt
            );

            // Call LLM for enhancement
            var enhancedScript = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);

            // Generate diff
            var changes = GenerateDiff(originalScript, enhancedScript);

            // Generate improvement summary
            var summary = GenerateImprovementSummary(options, changes.Count);

            _logger.LogInformation("Script enhancement complete. Made {ChangeCount} changes", changes.Count);

            return new EnhancedScript(
                NewScript: enhancedScript,
                Changes: changes,
                ImprovementSummary: summary
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing script");
            
            // Return original script on error
            return new EnhancedScript(
                NewScript: originalScript,
                Changes: new List<DiffChange>(),
                ImprovementSummary: "Enhancement failed. Original script retained."
            );
        }
    }

    private string BuildEnhancementInstructions(EnhancementOptions options)
    {
        var instructions = new List<string>();

        if (options.FixCoherence)
        {
            instructions.Add("coherence (add transitions between scenes)");
        }
        if (options.IncreaseEngagement)
        {
            instructions.Add("engagement (add hooks, interesting facts, rhetorical questions)");
        }
        if (options.ImproveClarity)
        {
            instructions.Add("clarity (simplify complex sentences, add explanations)");
        }
        if (options.AddDetails)
        {
            instructions.Add("detail (expand with relevant context and examples)");
        }

        return instructions.Count > 0 
            ? string.Join(", ", instructions)
            : "overall quality";
    }

    private List<DiffChange> GenerateDiff(string original, string enhanced)
    {
        var changes = new List<DiffChange>();

        // Split by lines
        var originalLines = original.Split('\n').Select(l => l.Trim()).ToArray();
        var enhancedLines = enhanced.Split('\n').Select(l => l.Trim()).ToArray();

        // Simple line-by-line comparison
        var maxLines = Math.Max(originalLines.Length, enhancedLines.Length);
        
        for (int i = 0; i < maxLines; i++)
        {
            var origLine = i < originalLines.Length ? originalLines[i] : "";
            var enhLine = i < enhancedLines.Length ? enhancedLines[i] : "";

            if (origLine != enhLine)
            {
                if (string.IsNullOrEmpty(origLine))
                {
                    changes.Add(new DiffChange("added", i + 1, "", enhLine));
                }
                else if (string.IsNullOrEmpty(enhLine))
                {
                    changes.Add(new DiffChange("removed", i + 1, origLine, ""));
                }
                else
                {
                    changes.Add(new DiffChange("modified", i + 1, origLine, enhLine));
                }
            }
        }

        return changes;
    }

    private string GenerateImprovementSummary(EnhancementOptions options, int changeCount)
    {
        var improvements = new List<string>();

        if (options.FixCoherence)
        {
            improvements.Add("Added transitions to improve scene flow");
        }
        if (options.IncreaseEngagement)
        {
            improvements.Add("Enhanced engagement with hooks and interesting elements");
        }
        if (options.ImproveClarity)
        {
            improvements.Add("Simplified language for better clarity");
        }
        if (options.AddDetails)
        {
            improvements.Add("Expanded content with relevant details");
        }

        var summary = improvements.Count > 0
            ? string.Join(". ", improvements) + "."
            : "General improvements applied.";

        return $"{summary} Total changes: {changeCount}";
    }
}
