using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Orchestrates multi-stage script refinement with Draft-Critique-Revise pattern
/// </summary>
public class ScriptRefinementOrchestrator
{
    private readonly ILogger<ScriptRefinementOrchestrator> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly IntelligentContentAdvisor? _contentAdvisor;

    public ScriptRefinementOrchestrator(
        ILogger<ScriptRefinementOrchestrator> logger,
        ILlmProvider llmProvider,
        IntelligentContentAdvisor? contentAdvisor = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _contentAdvisor = contentAdvisor;
    }

    /// <summary>
    /// Perform multi-stage script refinement with quality assessment
    /// </summary>
    public async Task<ScriptRefinementResult> RefineScriptAsync(
        Brief brief,
        PlanSpec spec,
        ScriptRefinementConfig config,
        CancellationToken ct = default)
    {
        config.Validate();

        var stopwatch = Stopwatch.StartNew();
        var result = new ScriptRefinementResult
        {
            Success = false,
            TotalPasses = 0
        };

        try
        {
            _logger.LogInformation(
                "Starting script refinement: MaxPasses={MaxPasses}, QualityThreshold={Threshold}",
                config.MaxRefinementPasses, config.QualityThreshold);

            // Stage 1: Generate initial draft
            _logger.LogInformation("Stage 1: Generating initial draft (Pass 0)");
            var currentScript = await GenerateInitialDraftAsync(brief, spec, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(currentScript))
            {
                result.ErrorMessage = "Failed to generate initial draft";
                return result;
            }

            // Stage 2: Assess initial quality
            _logger.LogInformation("Stage 2: Assessing initial draft quality");
            var initialMetrics = await AssessScriptQualityAsync(
                currentScript, brief, spec, 0, ct).ConfigureAwait(false);
            result.IterationMetrics.Add(initialMetrics);
            result.TotalPasses = 1;

            _logger.LogInformation(
                "Initial draft quality: Overall={Overall:F1}, Narrative={Narrative:F1}, Pacing={Pacing:F1}, Audience={Audience:F1}, Visual={Visual:F1}, Engagement={Engagement:F1}",
                initialMetrics.OverallScore,
                initialMetrics.NarrativeCoherence,
                initialMetrics.PacingAppropriateness,
                initialMetrics.AudienceAlignment,
                initialMetrics.VisualClarity,
                initialMetrics.EngagementPotential);

            // Check if initial draft already meets threshold
            if (initialMetrics.MeetsThreshold(config.QualityThreshold))
            {
                _logger.LogInformation(
                    "Initial draft quality {Score:F1} meets threshold {Threshold:F1}, stopping refinement",
                    initialMetrics.OverallScore, config.QualityThreshold);
                result.FinalScript = currentScript;
                result.Success = true;
                result.StopReason = $"Initial draft met quality threshold ({initialMetrics.OverallScore:F1} >= {config.QualityThreshold:F1})";
                return result;
            }

            // Stage 3: Iterative refinement
            ScriptQualityMetrics? previousMetrics = initialMetrics;

            for (int pass = 1; pass <= config.MaxRefinementPasses; pass++)
            {
                _logger.LogInformation("Starting refinement pass {Pass}/{MaxPasses}", pass, config.MaxRefinementPasses);

                // Stage 3a: Generate critique
                _logger.LogInformation("Pass {Pass} - Stage 3a: Generating critique", pass);
                var critique = await GenerateCritiqueAsync(
                    currentScript, brief, spec, previousMetrics, ct).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(critique))
                {
                    _logger.LogWarning("Critique generation failed for pass {Pass}, stopping refinement", pass);
                    result.StopReason = $"Critique generation failed at pass {pass}";
                    break;
                }

                // Stage 3b: Generate revised script
                _logger.LogInformation("Pass {Pass} - Stage 3b: Generating revised script", pass);
                var revisedScript = await GenerateRevisedScriptAsync(
                    currentScript, critique, brief, spec, ct).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(revisedScript))
                {
                    _logger.LogWarning("Revision generation failed for pass {Pass}, stopping refinement", pass);
                    result.StopReason = $"Revision generation failed at pass {pass}";
                    break;
                }

                // Stage 3c: Assess revised quality
                _logger.LogInformation("Pass {Pass} - Stage 3c: Assessing revised quality", pass);
                var revisedMetrics = await AssessScriptQualityAsync(
                    revisedScript, brief, spec, pass, ct).ConfigureAwait(false);
                result.IterationMetrics.Add(revisedMetrics);
                result.TotalPasses = pass + 1;

                var improvement = revisedMetrics.CalculateImprovement(previousMetrics);

                _logger.LogInformation(
                    "Pass {Pass} quality: Overall={Overall:F1} (Δ{Delta:+F1}), Narrative={Narrative:F1} (Δ{NDelta:+F1}), Pacing={Pacing:F1} (Δ{PDelta:+F1}), Audience={Audience:F1} (Δ{ADelta:+F1}), Visual={Visual:F1} (Δ{VDelta:+F1}), Engagement={Engagement:F1} (Δ{EDelta:+F1})",
                    pass,
                    revisedMetrics.OverallScore, improvement.OverallDelta,
                    revisedMetrics.NarrativeCoherence, improvement.NarrativeCoherenceDelta,
                    revisedMetrics.PacingAppropriateness, improvement.PacingDelta,
                    revisedMetrics.AudienceAlignment, improvement.AudienceDelta,
                    revisedMetrics.VisualClarity, improvement.VisualClarityDelta,
                    revisedMetrics.EngagementPotential, improvement.EngagementDelta);

                currentScript = revisedScript;
                previousMetrics = revisedMetrics;

                // Check for early stopping conditions
                if (revisedMetrics.MeetsThreshold(config.QualityThreshold))
                {
                    _logger.LogInformation(
                        "Quality threshold met at pass {Pass} ({Score:F1} >= {Threshold:F1}), stopping refinement",
                        pass, revisedMetrics.OverallScore, config.QualityThreshold);
                    result.StopReason = $"Quality threshold met at pass {pass} ({revisedMetrics.OverallScore:F1} >= {config.QualityThreshold:F1})";
                    break;
                }

                if (!improvement.HasMeaningfulImprovement() && pass < config.MaxRefinementPasses)
                {
                    _logger.LogInformation(
                        "Minimal improvement detected at pass {Pass} (Δ{Delta:F1}), stopping refinement",
                        pass, improvement.OverallDelta);
                    result.StopReason = $"Minimal improvement at pass {pass} (Δ{improvement.OverallDelta:F1})";
                    break;
                }

                if (pass == config.MaxRefinementPasses)
                {
                    result.StopReason = $"Maximum passes reached ({config.MaxRefinementPasses})";
                }
            }

            result.FinalScript = currentScript;
            result.Success = true;

            // Optional: Validate with IntelligentContentAdvisor
            if (config.EnableAdvisorValidation && _contentAdvisor != null)
            {
                _logger.LogInformation("Performing final validation with IntelligentContentAdvisor");
                try
                {
                    var advisorAnalysis = await _contentAdvisor.AnalyzeContentQualityAsync(
                        currentScript, brief, spec, ct).ConfigureAwait(false);
                    _logger.LogInformation(
                        "Advisor validation complete: OverallScore={Score:F1}, PassesThreshold={Passes}",
                        advisorAnalysis.OverallScore, advisorAnalysis.PassesQualityThreshold);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Advisor validation failed but refinement succeeded");
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script refinement failed with error");
            result.ErrorMessage = $"Refinement error: {ex.Message}";
            return result;
        }
        finally
        {
            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;

            var totalImprovement = result.GetTotalImprovement();
            if (totalImprovement != null)
            {
                _logger.LogInformation(
                    "Script refinement complete: Duration={Duration:F1}s, Passes={Passes}, TotalImprovement={Improvement:+F1}, Final={Final:F1}",
                    result.TotalDuration.TotalSeconds,
                    result.TotalPasses,
                    totalImprovement.OverallDelta,
                    result.FinalMetrics?.OverallScore ?? 0);
            }
            else
            {
                _logger.LogInformation(
                    "Script refinement complete: Duration={Duration:F1}s, Passes={Passes}",
                    result.TotalDuration.TotalSeconds,
                    result.TotalPasses);
            }
        }
    }

    /// <summary>
    /// Generate initial draft script
    /// </summary>
    private async Task<string> GenerateInitialDraftAsync(
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        try
        {
            var script = await _llmProvider.DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);
            return script ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate initial draft");
            return string.Empty;
        }
    }

    /// <summary>
    /// Generate critique of current script with structured quality assessment
    /// </summary>
    private async Task<string> GenerateCritiqueAsync(
        string script,
        Brief brief,
        PlanSpec spec,
        ScriptQualityMetrics currentMetrics,
        CancellationToken ct)
    {
        try
        {
            var critiquePrompt = BuildCritiquePrompt(script, brief, spec, currentMetrics);

            var critiqueBrief = new Brief(
                Topic: "Script Quality Critique",
                Audience: null,
                Goal: "Identify specific areas for improvement",
                Tone: "analytical",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var critiquePlanSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(1),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: critiquePrompt
            );

            var critique = await _llmProvider.DraftScriptAsync(critiqueBrief, critiquePlanSpec, ct).ConfigureAwait(false);
            return critique ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate critique");
            return string.Empty;
        }
    }

    /// <summary>
    /// Generate revised script incorporating critique feedback
    /// </summary>
    private async Task<string> GenerateRevisedScriptAsync(
        string originalScript,
        string critique,
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        try
        {
            var revisionPrompt = BuildRevisionPrompt(originalScript, critique, brief, spec);

            var revisionBrief = new Brief(
                Topic: brief.Topic,
                Audience: brief.Audience,
                Goal: brief.Goal,
                Tone: brief.Tone,
                Language: brief.Language,
                Aspect: brief.Aspect
            );

            var revisionPlanSpec = new PlanSpec(
                TargetDuration: spec.TargetDuration,
                Pacing: spec.Pacing,
                Density: spec.Density,
                Style: revisionPrompt
            );

            var revisedScript = await _llmProvider.DraftScriptAsync(revisionBrief, revisionPlanSpec, ct).ConfigureAwait(false);
            return revisedScript ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate revised script");
            return string.Empty;
        }
    }

    /// <summary>
    /// Assess script quality with structured metrics
    /// </summary>
    private async Task<ScriptQualityMetrics> AssessScriptQualityAsync(
        string script,
        Brief brief,
        PlanSpec spec,
        int iteration,
        CancellationToken ct)
    {
        try
        {
            var assessmentPrompt = BuildQualityAssessmentPrompt(script, brief, spec);

            var assessmentBrief = new Brief(
                Topic: "Script Quality Assessment",
                Audience: null,
                Goal: "Evaluate script quality across multiple dimensions",
                Tone: "analytical",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var assessmentPlanSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(1),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: assessmentPrompt
            );

            var assessment = await _llmProvider.DraftScriptAsync(assessmentBrief, assessmentPlanSpec, ct).ConfigureAwait(false);
            var metrics = ParseQualityAssessment(assessment, iteration);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assess script quality");
            return CreateDefaultMetrics(iteration);
        }
    }

    /// <summary>
    /// Build prompt for quality critique
    /// </summary>
    private string BuildCritiquePrompt(
        string script,
        Brief brief,
        PlanSpec spec,
        ScriptQualityMetrics currentMetrics)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("CRITIQUE THIS VIDEO SCRIPT:");
        sb.AppendLine();
        sb.AppendLine("CURRENT SCRIPT:");
        sb.AppendLine(script);
        sb.AppendLine();
        sb.AppendLine("CURRENT QUALITY ASSESSMENT:");
        sb.AppendLine($"- Overall Score: {currentMetrics.OverallScore:F1}/100");
        sb.AppendLine($"- Narrative Coherence: {currentMetrics.NarrativeCoherence:F1}/100");
        sb.AppendLine($"- Pacing Appropriateness: {currentMetrics.PacingAppropriateness:F1}/100");
        sb.AppendLine($"- Audience Alignment: {currentMetrics.AudienceAlignment:F1}/100");
        sb.AppendLine($"- Visual Clarity: {currentMetrics.VisualClarity:F1}/100");
        sb.AppendLine($"- Engagement Potential: {currentMetrics.EngagementPotential:F1}/100");
        sb.AppendLine();
        sb.AppendLine("EVALUATION CRITERIA:");
        sb.AppendLine();
        sb.AppendLine("1. NARRATIVE COHERENCE:");
        sb.AppendLine("   - Does the script have a clear beginning, middle, and end?");
        sb.AppendLine("   - Are transitions between ideas smooth and logical?");
        sb.AppendLine("   - Is there a compelling throughline that holds the narrative together?");
        sb.AppendLine();
        sb.AppendLine("2. PACING APPROPRIATENESS:");
        sb.AppendLine("   - Is the information density appropriate for the target duration?");
        sb.AppendLine("   - Are there natural rhythm variations (fast/slow moments)?");
        sb.AppendLine("   - Does the pacing match the intended tone and audience?");
        sb.AppendLine();
        sb.AppendLine("3. AUDIENCE ALIGNMENT:");
        sb.AppendLine($"   - Is the language level appropriate for: {brief.Audience ?? "general audience"}?");
        sb.AppendLine("   - Are examples and references relatable to the target audience?");
        sb.AppendLine("   - Does it address their likely questions and concerns?");
        sb.AppendLine();
        sb.AppendLine("4. VISUAL CLARITY:");
        sb.AppendLine("   - Can each scene be easily visualized or illustrated?");
        sb.AppendLine("   - Are there clear moments for B-roll, graphics, or demonstrations?");
        sb.AppendLine("   - Does the script support visual storytelling?");
        sb.AppendLine();
        sb.AppendLine("5. ENGAGEMENT POTENTIAL:");
        sb.AppendLine("   - Does the opening hook grab attention immediately?");
        sb.AppendLine("   - Are there pattern interrupts and surprises?");
        sb.AppendLine("   - Is there a clear value proposition for viewers?");
        sb.AppendLine();
        sb.AppendLine("PROVIDE CRITIQUE IN THIS FORMAT:");
        sb.AppendLine("ISSUES:");
        sb.AppendLine("- [List specific issues with examples]");
        sb.AppendLine();
        sb.AppendLine("STRENGTHS:");
        sb.AppendLine("- [List what works well]");
        sb.AppendLine();
        sb.AppendLine("SPECIFIC IMPROVEMENTS:");
        sb.AppendLine("- [Actionable suggestions with concrete examples]");
        
        return sb.ToString();
    }

    /// <summary>
    /// Build prompt for script revision
    /// </summary>
    private string BuildRevisionPrompt(
        string originalScript,
        string critique,
        Brief brief,
        PlanSpec spec)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("REVISE THIS VIDEO SCRIPT BASED ON CRITIQUE:");
        sb.AppendLine();
        sb.AppendLine("ORIGINAL SCRIPT:");
        sb.AppendLine(originalScript);
        sb.AppendLine();
        sb.AppendLine("CRITIQUE AND FEEDBACK:");
        sb.AppendLine(critique);
        sb.AppendLine();
        sb.AppendLine("REVISION INSTRUCTIONS:");
        sb.AppendLine("1. Address all issues identified in the critique");
        sb.AppendLine("2. Preserve the strengths mentioned");
        sb.AppendLine("3. Implement the specific improvements suggested");
        sb.AppendLine("4. Maintain the original topic, tone, and target duration");
        sb.AppendLine("5. Ensure the revised script flows naturally and sounds human");
        sb.AppendLine("6. Keep the same approximate length and structure");
        sb.AppendLine();
        sb.AppendLine("Generate the complete revised script now:");
        
        return sb.ToString();
    }

    /// <summary>
    /// Build prompt for quality assessment
    /// </summary>
    private string BuildQualityAssessmentPrompt(string script, Brief brief, PlanSpec spec)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("ASSESS THE QUALITY OF THIS VIDEO SCRIPT:");
        sb.AppendLine();
        sb.AppendLine("SCRIPT:");
        sb.AppendLine(script);
        sb.AppendLine();
        sb.AppendLine("CONTEXT:");
        sb.AppendLine($"- Topic: {brief.Topic}");
        sb.AppendLine($"- Audience: {brief.Audience ?? "general"}");
        sb.AppendLine($"- Tone: {brief.Tone}");
        sb.AppendLine($"- Duration: {spec.TargetDuration.TotalMinutes:F1} minutes");
        sb.AppendLine();
        sb.AppendLine("PROVIDE SCORES (0-100) FOR EACH DIMENSION:");
        sb.AppendLine();
        sb.AppendLine("NarrativeCoherence: [score 0-100]");
        sb.AppendLine("PacingAppropriateness: [score 0-100]");
        sb.AppendLine("AudienceAlignment: [score 0-100]");
        sb.AppendLine("VisualClarity: [score 0-100]");
        sb.AppendLine("EngagementPotential: [score 0-100]");
        sb.AppendLine();
        sb.AppendLine("ISSUES:");
        sb.AppendLine("- [List any issues, or 'None' if excellent]");
        sb.AppendLine();
        sb.AppendLine("SUGGESTIONS:");
        sb.AppendLine("- [List improvement suggestions, or 'None' if excellent]");
        sb.AppendLine();
        sb.AppendLine("STRENGTHS:");
        sb.AppendLine("- [List what works well]");
        
        return sb.ToString();
    }

    /// <summary>
    /// Parse quality assessment response into structured metrics
    /// </summary>
    private ScriptQualityMetrics ParseQualityAssessment(string assessment, int iteration)
    {
        var metrics = new ScriptQualityMetrics
        {
            Iteration = iteration,
            AssessedAt = DateTime.UtcNow
        };

        try
        {
            metrics.NarrativeCoherence = ExtractScore(assessment, "NarrativeCoherence") ?? 75.0;
            metrics.PacingAppropriateness = ExtractScore(assessment, "PacingAppropriateness") ?? 75.0;
            metrics.AudienceAlignment = ExtractScore(assessment, "AudienceAlignment") ?? 75.0;
            metrics.VisualClarity = ExtractScore(assessment, "VisualClarity") ?? 75.0;
            metrics.EngagementPotential = ExtractScore(assessment, "EngagementPotential") ?? 75.0;

            metrics.CalculateOverallScore();

            metrics.Issues = ExtractBulletPoints(assessment, "ISSUES");
            metrics.Suggestions = ExtractBulletPoints(assessment, "SUGGESTIONS");
            metrics.Strengths = ExtractBulletPoints(assessment, "STRENGTHS");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse quality assessment, using defaults");
            return CreateDefaultMetrics(iteration);
        }

        return metrics;
    }

    /// <summary>
    /// Extract score from assessment text
    /// </summary>
    private double? ExtractScore(string text, string metricName)
    {
        var pattern = $@"{metricName}:\s*(\d+(?:\.\d+)?)";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        
        if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
        {
            return Math.Clamp(score, 0, 100);
        }

        return null;
    }

    /// <summary>
    /// Extract bullet points from a section
    /// </summary>
    private List<string> ExtractBulletPoints(string text, string sectionHeader)
    {
        var items = new List<string>();
        
        try
        {
            var pattern = $@"{sectionHeader}:\s*(.*?)(?=\n[A-Z]+:|$)";
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            if (match.Success)
            {
                var content = match.Groups[1].Value;
                var bulletPattern = @"[-•*]\s*(.+?)(?=\n[-•*]|\n\n|$)";
                var bullets = Regex.Matches(content, bulletPattern, RegexOptions.Singleline);
                
                foreach (Match bullet in bullets)
                {
                    var item = bullet.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(item) && 
                        !item.Equals("None", StringComparison.OrdinalIgnoreCase))
                    {
                        items.Add(item);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract bullet points from section {Section}", sectionHeader);
        }

        return items;
    }

    /// <summary>
    /// Create default metrics when parsing fails
    /// </summary>
    private ScriptQualityMetrics CreateDefaultMetrics(int iteration)
    {
        var metrics = new ScriptQualityMetrics
        {
            NarrativeCoherence = 75.0,
            PacingAppropriateness = 75.0,
            AudienceAlignment = 75.0,
            VisualClarity = 75.0,
            EngagementPotential = 75.0,
            Iteration = iteration,
            AssessedAt = DateTime.UtcNow
        };

        metrics.CalculateOverallScore();
        return metrics;
    }
}
