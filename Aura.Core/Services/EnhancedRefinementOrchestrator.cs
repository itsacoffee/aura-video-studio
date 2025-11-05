using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Enhanced orchestrator using generator-critic-editor pattern with telemetry
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class EnhancedRefinementOrchestrator
{
    private readonly ILogger<EnhancedRefinementOrchestrator> _logger;
    private readonly ILlmProvider _generatorProvider;
    private readonly LlmStageAdapter? _stageAdapter;
    private readonly ICriticProvider _criticProvider;
    private readonly IEditorProvider _editorProvider;
    private readonly IntelligentContentAdvisor? _contentAdvisor;

    public EnhancedRefinementOrchestrator(
        ILogger<EnhancedRefinementOrchestrator> logger,
        ILlmProvider generatorProvider,
        ICriticProvider criticProvider,
        IEditorProvider editorProvider,
        IntelligentContentAdvisor? contentAdvisor = null,
        LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _generatorProvider = generatorProvider;
        _stageAdapter = stageAdapter;
        _criticProvider = criticProvider;
        _editorProvider = editorProvider;
        _contentAdvisor = contentAdvisor;
    }

    /// <summary>
    /// Perform multi-stage script refinement using generator-critic-editor pattern
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
            TotalPasses = 0,
            TotalCost = 0
        };

        var telemetry = config.EnableTelemetry ? new RefinementTelemetry() : null;
        var rubrics = RefinementRubricBuilder.GetDefaultRubrics();

        try
        {
            _logger.LogInformation(
                "Starting enhanced refinement: MaxPasses={MaxPasses}, QualityThreshold={Threshold}, CriticModel={Critic}, EditorModel={Editor}",
                config.MaxRefinementPasses, config.QualityThreshold, config.CriticModel ?? "same", config.EditorModel ?? "same");

            // Stage 1: Generate initial draft using generator
            _logger.LogInformation("Stage 1: Generating initial draft (Round 0)");
            var roundStopwatch = Stopwatch.StartNew();
            var currentScript = await GenerateInitialDraftAsync(brief, spec, ct);

            if (string.IsNullOrWhiteSpace(currentScript))
            {
                result.ErrorMessage = "Failed to generate initial draft";
                return result;
            }

            var generatorCost = EstimateTokenCost(currentScript.Length, isGenerator: true);
            result.TotalCost += generatorCost;

            // Stage 2: Assess initial quality using critic
            _logger.LogInformation("Stage 2: Critiquing initial draft (Round 0)");
            var initialCritique = await _criticProvider.CritiqueScriptAsync(
                currentScript, brief, spec, rubrics, null, ct);

            var criticCost = EstimateTokenCost(initialCritique.RawCritique.Length, isGenerator: false);
            result.TotalCost += criticCost;

            var initialMetrics = ConvertCritiqueToMetrics(initialCritique, 0);
            result.IterationMetrics.Add(initialMetrics);
            result.TotalPasses = 1;

            if (telemetry != null)
            {
                var roundTelemetry = new RoundTelemetry
                {
                    RoundNumber = 0,
                    BeforeMetrics = null,
                    AfterMetrics = initialMetrics,
                    Duration = roundStopwatch.Elapsed,
                    Cost = generatorCost + criticCost,
                    GeneratorModel = "generator",
                    CriticModel = config.CriticModel ?? "generator",
                    EditorModel = null,
                    SchemaValid = true,
                    WithinDurationConstraints = initialCritique.TimingAnalysis?.WithinAcceptableRange ?? true
                };
                telemetry.RoundData.Add(roundTelemetry);
                telemetry.CostByPhase["generation"] = generatorCost;
                telemetry.CostByPhase["critique"] = criticCost;
            }

            _logger.LogInformation(
                "Initial draft quality: Overall={Overall:F1}, Rubrics={Rubrics}",
                initialCritique.OverallScore,
                string.Join(", ", initialCritique.RubricScores.Select(kvp => $"{kvp.Key}={kvp.Value:F1}")));

            // Check if initial draft already meets threshold
            if (initialMetrics.MeetsThreshold(config.QualityThreshold))
            {
                _logger.LogInformation(
                    "Initial draft quality {Score:F1} meets threshold {Threshold:F1}, stopping refinement",
                    initialMetrics.OverallScore, config.QualityThreshold);
                result.FinalScript = currentScript;
                result.Success = true;
                result.StopReason = $"Initial draft met quality threshold ({initialMetrics.OverallScore:F1} >= {config.QualityThreshold:F1})";
                result.CritiqueSummary = BuildCritiqueSummary(initialCritique);
                result.Telemetry = telemetry;
                return result;
            }

            // Check budget before refinement
            if (config.MaxCostBudget.HasValue && result.TotalCost >= config.MaxCostBudget.Value)
            {
                _logger.LogWarning(
                    "Cost budget {Budget} exceeded after initial generation ({Cost:F4}), stopping",
                    config.MaxCostBudget.Value, result.TotalCost);
                result.FinalScript = currentScript;
                result.Success = true;
                result.StopReason = $"Cost budget exceeded ({result.TotalCost:F4} >= {config.MaxCostBudget.Value:F4})";
                result.CritiqueSummary = BuildCritiqueSummary(initialCritique);
                result.Telemetry = telemetry;
                return result;
            }

            // Stage 3: Iterative refinement using critic-editor loop
            ScriptQualityMetrics? previousMetrics = initialMetrics;
            CritiqueResult? previousCritique = initialCritique;

            for (int pass = 1; pass <= config.MaxRefinementPasses; pass++)
            {
                _logger.LogInformation("Starting refinement round {Round}/{MaxRounds}", pass, config.MaxRefinementPasses);
                roundStopwatch.Restart();

                var roundCost = 0.0;
                
                // Stage 3a: Editor applies changes based on critique
                _logger.LogInformation("Round {Round} - Stage 3a: Editor applying changes", pass);
                var editResult = await _editorProvider.EditScriptAsync(
                    currentScript, previousCritique, brief, spec, ct);

                if (!editResult.Success || string.IsNullOrWhiteSpace(editResult.EditedScript))
                {
                    _logger.LogWarning("Editor failed for round {Round}, stopping refinement", pass);
                    result.StopReason = $"Editor failed at round {pass}";
                    break;
                }

                var editorCost = EstimateTokenCost(editResult.EditedScript.Length, isGenerator: false);
                roundCost += editorCost;
                result.TotalCost += editorCost;

                currentScript = editResult.EditedScript;

                // Stage 3b: Schema validation
                var schemaValid = true;
                var withinDuration = true;
                if (config.EnableSchemaValidation && editResult.ValidationResult != null)
                {
                    schemaValid = editResult.ValidationResult.IsValid;
                    withinDuration = editResult.ValidationResult.MeetsDurationConstraints;

                    if (!schemaValid)
                    {
                        _logger.LogWarning(
                            "Round {Round} validation failed: {Errors}",
                            pass,
                            string.Join(", ", editResult.ValidationResult.Errors));
                    }

                    if (!withinDuration && editResult.ValidationResult.EstimatedDuration.HasValue && editResult.ValidationResult.TargetDuration.HasValue)
                    {
                        _logger.LogWarning(
                            "Round {Round} duration constraint violated: {Estimated} vs {Target}",
                            pass,
                            editResult.ValidationResult.EstimatedDuration.Value,
                            editResult.ValidationResult.TargetDuration.Value);
                    }
                }

                // Stage 3c: Critic evaluates revised script
                _logger.LogInformation("Round {Round} - Stage 3c: Critic evaluating revision", pass);
                var newCritique = await _criticProvider.CritiqueScriptAsync(
                    currentScript, brief, spec, rubrics, previousMetrics, ct);

                var newCriticCost = EstimateTokenCost(newCritique.RawCritique.Length, isGenerator: false);
                roundCost += newCriticCost;
                result.TotalCost += newCriticCost;

                var newMetrics = ConvertCritiqueToMetrics(newCritique, pass);
                result.IterationMetrics.Add(newMetrics);
                result.TotalPasses = pass + 1;

                var improvement = newMetrics.CalculateImprovement(previousMetrics);

                if (telemetry != null)
                {
                    var roundTelemetry = new RoundTelemetry
                    {
                        RoundNumber = pass,
                        BeforeMetrics = previousMetrics,
                        AfterMetrics = newMetrics,
                        Duration = roundStopwatch.Elapsed,
                        Cost = roundCost,
                        GeneratorModel = null,
                        CriticModel = config.CriticModel ?? "generator",
                        EditorModel = config.EditorModel ?? "generator",
                        SchemaValid = schemaValid,
                        WithinDurationConstraints = withinDuration
                    };
                    telemetry.RoundData.Add(roundTelemetry);
                    
                    if (!telemetry.CostByPhase.ContainsKey("editing"))
                    {
                        telemetry.CostByPhase["editing"] = 0;
                    }
                    telemetry.CostByPhase["editing"] += editorCost;
                    telemetry.CostByPhase["critique"] += newCriticCost;
                }

                _logger.LogInformation(
                    "Round {Round} quality: Overall={Overall:F1} (Δ{Delta:+F1}), Rubrics={Rubrics}",
                    pass,
                    newCritique.OverallScore, improvement.OverallDelta,
                    string.Join(", ", newCritique.RubricScores.Select(kvp => $"{kvp.Key}={kvp.Value:F1}")));

                previousMetrics = newMetrics;
                previousCritique = newCritique;

                // Check for early stopping conditions
                if (newMetrics.MeetsThreshold(config.QualityThreshold))
                {
                    _logger.LogInformation(
                        "Quality threshold met at round {Round} ({Score:F1} >= {Threshold:F1}), stopping refinement",
                        pass, newMetrics.OverallScore, config.QualityThreshold);
                    result.StopReason = $"Quality threshold met at round {pass} ({newMetrics.OverallScore:F1} >= {config.QualityThreshold:F1})";
                    break;
                }

                if (!improvement.HasMeaningfulImprovement() && pass < config.MaxRefinementPasses)
                {
                    _logger.LogInformation(
                        "Minimal improvement detected at round {Round} (Δ{Delta:F1}), stopping refinement",
                        pass, improvement.OverallDelta);
                    result.StopReason = $"Minimal improvement at round {pass} (Δ{improvement.OverallDelta:F1})";
                    break;
                }

                // Check cost budget
                if (config.MaxCostBudget.HasValue && result.TotalCost >= config.MaxCostBudget.Value)
                {
                    _logger.LogWarning(
                        "Cost budget {Budget} exceeded ({Cost:F4}), stopping refinement",
                        config.MaxCostBudget.Value, result.TotalCost);
                    result.StopReason = $"Cost budget exceeded ({result.TotalCost:F4} >= {config.MaxCostBudget.Value:F4})";
                    break;
                }

                if (pass == config.MaxRefinementPasses)
                {
                    result.StopReason = $"Maximum rounds reached ({config.MaxRefinementPasses})";
                }
            }

            result.FinalScript = currentScript;
            result.Success = true;
            result.CritiqueSummary = BuildCritiqueSummary(previousCritique!);

            // Calculate convergence statistics
            if (telemetry != null)
            {
                telemetry.Convergence = CalculateConvergenceStatistics(result.IterationMetrics);
                telemetry.ModelUsage = CalculateModelUsage(telemetry.RoundData);
                result.Telemetry = telemetry;
            }

            // Optional: Validate with IntelligentContentAdvisor
            if (config.EnableAdvisorValidation && _contentAdvisor != null)
            {
                _logger.LogInformation("Performing final validation with IntelligentContentAdvisor");
                try
                {
                    var advisorAnalysis = await _contentAdvisor.AnalyzeContentQualityAsync(
                        currentScript, brief, spec, ct);
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
            result.Telemetry = telemetry;
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
                    "Enhanced refinement complete: Duration={Duration:F1}s, Rounds={Rounds}, Cost=${Cost:F4}, TotalImprovement={Improvement:+F1}, Final={Final:F1}",
                    result.TotalDuration.TotalSeconds,
                    result.TotalPasses,
                    result.TotalCost,
                    totalImprovement.OverallDelta,
                    result.FinalMetrics?.OverallScore ?? 0);
            }
            else
            {
                _logger.LogInformation(
                    "Enhanced refinement complete: Duration={Duration:F1}s, Rounds={Rounds}, Cost=${Cost:F4}",
                    result.TotalDuration.TotalSeconds,
                    result.TotalPasses,
                    result.TotalCost);
            }
        }
    }

    private async Task<string> GenerateInitialDraftAsync(
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        try
        {
            var script = await GenerateWithLlmAsync(brief, spec, ct);
            return script ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate initial draft");
            return string.Empty;
        }
    }

    private async Task<string> GenerateWithLlmAsync(Brief brief, PlanSpec planSpec, CancellationToken ct)
    {
        if (_stageAdapter != null)
        {
            var result = await _stageAdapter.GenerateScriptAsync(brief, planSpec, "Free", false, ct);
            if (result.IsSuccess && result.Data != null) return result.Data;
            _logger.LogWarning("Orchestrator failed, using direct provider: {Error}", result.ErrorMessage);
        }
        return await _generatorProvider.DraftScriptAsync(brief, planSpec, ct);
    }

    private ScriptQualityMetrics ConvertCritiqueToMetrics(CritiqueResult critique, int iteration)
    {
        var metrics = new ScriptQualityMetrics
        {
            Iteration = iteration,
            AssessedAt = DateTime.UtcNow,
            OverallScore = critique.OverallScore
        };

        if (critique.RubricScores.TryGetValue("Clarity", out var clarity))
        {
            metrics.VisualClarity = clarity;
        }

        if (critique.RubricScores.TryGetValue("Coherence", out var coherence))
        {
            metrics.NarrativeCoherence = coherence;
        }

        if (critique.RubricScores.TryGetValue("Timing", out var timing))
        {
            metrics.PacingAppropriateness = timing;
        }

        if (critique.RubricScores.TryGetValue("Engagement", out var engagement))
        {
            metrics.EngagementPotential = engagement;
        }

        if (critique.RubricScores.TryGetValue("AudienceAlignment", out var audience))
        {
            metrics.AudienceAlignment = audience;
        }

        metrics.Issues = critique.Issues.Select(i => $"{i.Category}: {i.Description}").ToList();
        metrics.Strengths = critique.Strengths;
        metrics.Suggestions = critique.Suggestions.Select(s => $"{s.ChangeType} at {s.Target}: {s.Suggestion}").ToList();

        if (metrics.OverallScore == 0)
        {
            metrics.CalculateOverallScore();
        }

        return metrics;
    }

    private string BuildCritiqueSummary(CritiqueResult critique)
    {
        var summary = $"Overall Score: {critique.OverallScore:F1}/100\n\n";

        if (critique.Issues.Any())
        {
            summary += "Key Issues:\n";
            foreach (var issue in critique.Issues.Take(3))
            {
                summary += $"- [{issue.Severity}] {issue.Category}: {issue.Description}\n";
            }
            summary += "\n";
        }

        if (critique.Strengths.Any())
        {
            summary += "Strengths:\n";
            foreach (var strength in critique.Strengths.Take(3))
            {
                summary += $"- {strength}\n";
            }
            summary += "\n";
        }

        if (critique.TimingAnalysis != null)
        {
            summary += $"Timing: {critique.TimingAnalysis.WordCount} words " +
                      $"(target: {critique.TimingAnalysis.TargetWordCount}, " +
                      $"variance: {critique.TimingAnalysis.Variance:F1}%)\n";
        }

        return summary;
    }

    private ConvergenceStatistics CalculateConvergenceStatistics(List<ScriptQualityMetrics> metrics)
    {
        if (metrics.Count < 2)
        {
            return new ConvergenceStatistics
            {
                Converged = false,
                TotalImprovement = 0
            };
        }

        var improvements = new List<double>();
        for (int i = 1; i < metrics.Count; i++)
        {
            var improvement = metrics[i].OverallScore - metrics[i - 1].OverallScore;
            improvements.Add(improvement);
        }

        var avgImprovement = improvements.Average();
        var stdDev = Math.Sqrt(improvements.Average(v => Math.Pow(v - avgImprovement, 2)));

        var lastImprovement = improvements.Last();
        var converged = lastImprovement < 2.0 && improvements.Count > 1;
        int? convergenceRound = null;

        if (converged)
        {
            for (int i = improvements.Count - 1; i >= 0; i--)
            {
                if (improvements[i] >= 2.0)
                {
                    convergenceRound = i + 1;
                    break;
                }
            }
        }

        var totalImprovement = metrics.Last().OverallScore - metrics.First().OverallScore;
        var convergenceRate = totalImprovement / metrics.Count;

        return new ConvergenceStatistics
        {
            AverageImprovementPerRound = avgImprovement,
            ImprovementStdDev = stdDev,
            Converged = converged,
            ConvergenceRound = convergenceRound,
            ConvergenceRate = convergenceRate,
            TotalImprovement = totalImprovement
        };
    }

    private ModelUsageStats CalculateModelUsage(List<RoundTelemetry> rounds)
    {
        var stats = new ModelUsageStats
        {
            TotalApiCalls = rounds.Count * 2,
            RetryCount = 0
        };

        return stats;
    }

    private double EstimateTokenCost(int textLength, bool isGenerator)
    {
        var tokens = textLength / 4;
        var costPer1kTokens = isGenerator ? 0.002 : 0.001;
        return (tokens / 1000.0) * costPer1kTokens;
    }
}
