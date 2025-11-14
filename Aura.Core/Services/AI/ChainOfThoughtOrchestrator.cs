using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AI;

/// <summary>
/// Orchestrates chain-of-thought script generation with iterative refinement
/// Breaks generation into stages: Topic Analysis -> Outline -> Full Script
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class ChainOfThoughtOrchestrator
{
    private readonly ILogger<ChainOfThoughtOrchestrator> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;
    private readonly PromptCustomizationService _promptService;

    public ChainOfThoughtOrchestrator(
        ILogger<ChainOfThoughtOrchestrator> logger,
        ILlmProvider llmProvider,
        PromptCustomizationService promptService,
        LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
        _promptService = promptService;
    }

    /// <summary>
    /// Execute chain-of-thought generation for a specific stage
    /// </summary>
    public async Task<ChainOfThoughtResult> ExecuteStageAsync(
        ChainOfThoughtStage stage,
        Brief brief,
        PlanSpec spec,
        string? previousStageContent,
        CancellationToken ct)
    {
        _logger.LogInformation("Executing chain-of-thought stage: {Stage}", stage);

        var stagePrompt = BuildStagePrompt(stage, brief, spec, previousStageContent);
        
        var stageBrief = brief with { Topic = stagePrompt };

        var content = await GenerateWithLlmAsync(stageBrief, spec, ct).ConfigureAwait(false);

        var requiresReview = stage != ChainOfThoughtStage.TopicAnalysis;

        var suggestedEdits = GenerateSuggestedEdits(stage, content);

        return new ChainOfThoughtResult(
            Stage: stage,
            Content: content,
            RequiresUserReview: requiresReview,
            SuggestedEdits: suggestedEdits);
    }

    /// <summary>
    /// Build stage-specific prompt
    /// </summary>
    private string BuildStagePrompt(
        ChainOfThoughtStage stage,
        Brief brief,
        PlanSpec spec,
        string? previousContent)
    {
        var sb = new StringBuilder();

        switch (stage)
        {
            case ChainOfThoughtStage.TopicAnalysis:
                sb.AppendLine($"STAGE 1: TOPIC ANALYSIS");
                sb.AppendLine($"Analyze the following topic and provide strategic insights:");
                sb.AppendLine();
                sb.AppendLine($"Topic: {brief.Topic}");
                sb.AppendLine($"Audience: {brief.Audience}");
                sb.AppendLine($"Goal: {brief.Goal}");
                sb.AppendLine($"Tone: {brief.Tone}");
                sb.AppendLine();
                sb.AppendLine("Provide:");
                sb.AppendLine("1. Key themes and angles to explore");
                sb.AppendLine("2. Audience hooks and engagement strategies");
                sb.AppendLine("3. Potential challenges or pitfalls to avoid");
                sb.AppendLine("4. Unique insights or perspectives to include");
                sb.AppendLine("5. Recommended content structure approach");
                break;

            case ChainOfThoughtStage.Outline:
                sb.AppendLine($"STAGE 2: OUTLINE CREATION");
                sb.AppendLine($"Based on the topic analysis, create a detailed outline:");
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(previousContent))
                {
                    sb.AppendLine("TOPIC ANALYSIS:");
                    sb.AppendLine(previousContent);
                    sb.AppendLine();
                }
                sb.AppendLine($"Topic: {brief.Topic}");
                sb.AppendLine($"Duration: {spec.TargetDuration.TotalMinutes:F1} minutes");
                sb.AppendLine($"Pacing: {spec.Pacing}");
                sb.AppendLine();
                sb.AppendLine("Create an outline with:");
                sb.AppendLine("1. Hook (specific, not generic)");
                sb.AppendLine("2. Introduction with clear value proposition");
                sb.AppendLine("3. 3-5 main sections with descriptive headers");
                sb.AppendLine("4. Key points and examples for each section");
                sb.AppendLine("5. Conclusion with clear takeaway");
                sb.AppendLine("6. Suggested visual moments marked with [VISUAL: description]");
                break;

            case ChainOfThoughtStage.FullScript:
                sb.AppendLine($"STAGE 3: FULL SCRIPT");
                sb.AppendLine($"Expand the outline into a complete, production-ready script:");
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(previousContent))
                {
                    sb.AppendLine("APPROVED OUTLINE:");
                    sb.AppendLine(previousContent);
                    sb.AppendLine();
                }
                sb.AppendLine($"Write a natural, engaging script that:");
                sb.AppendLine("- Follows the outline structure");
                sb.AppendLine("- Uses conversational, authentic language");
                sb.AppendLine("- Includes specific examples and details");
                sb.AppendLine("- Varies sentence length and structure");
                sb.AppendLine("- Sounds great when read aloud");
                sb.AppendLine("- Maintains energy and momentum throughout");
                sb.AppendLine($"- Targets approximately {spec.TargetDuration.TotalMinutes:F1} minutes duration");
                break;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate suggested edits based on stage content
    /// </summary>
    private string? GenerateSuggestedEdits(ChainOfThoughtStage stage, string content)
    {
        switch (stage)
        {
            case ChainOfThoughtStage.TopicAnalysis:
                return "Review the analysis. Consider: Are there additional angles worth exploring? " +
                       "Any perspectives missing? Does this align with your vision?";

            case ChainOfThoughtStage.Outline:
                return "Review the outline structure. Consider: Does the flow make sense? " +
                       "Are sections in the right order? Any sections to add/remove/reorder? " +
                       "Are visual moments well-placed?";

            case ChainOfThoughtStage.FullScript:
                return "Review the complete script. Consider: Does it sound natural? " +
                       "Any sections that drag or rush? Are examples compelling? " +
                       "Does the hook grab attention? Is the conclusion satisfying?";

            default:
                return null;
        }
    }

    /// <summary>
    /// Execute complete chain-of-thought workflow
    /// This would typically be called with user review checkpoints between stages
    /// </summary>
    public async Task<string> ExecuteFullChainAsync(
        Brief brief,
        PlanSpec spec,
        Func<ChainOfThoughtResult, Task<string?>> userReviewCallback,
        CancellationToken ct)
    {
        _logger.LogInformation("Starting full chain-of-thought generation for topic: {Topic}", brief.Topic);

        var analysisResult = await ExecuteStageAsync(
            ChainOfThoughtStage.TopicAnalysis,
            brief,
            spec,
            null,
            ct).ConfigureAwait(false);

        _logger.LogInformation("Topic analysis complete, awaiting user review");
        var approvedAnalysis = await userReviewCallback(analysisResult).ConfigureAwait(false) ?? analysisResult.Content;

        var outlineResult = await ExecuteStageAsync(
            ChainOfThoughtStage.Outline,
            brief,
            spec,
            approvedAnalysis,
            ct).ConfigureAwait(false);

        _logger.LogInformation("Outline complete, awaiting user review");
        var approvedOutline = await userReviewCallback(outlineResult).ConfigureAwait(false) ?? outlineResult.Content;

        var scriptResult = await ExecuteStageAsync(
            ChainOfThoughtStage.FullScript,
            brief,
            spec,
            approvedOutline,
            ct).ConfigureAwait(false);

        _logger.LogInformation("Full script complete, awaiting final review");
        var finalScript = await userReviewCallback(scriptResult).ConfigureAwait(false) ?? scriptResult.Content;

        _logger.LogInformation("Chain-of-thought generation complete");
        return finalScript;
    }

    /// <summary>
    /// Helper method to execute LLM generation through unified orchestrator or fallback to direct provider
    /// </summary>
    private async Task<string> GenerateWithLlmAsync(
        Brief brief,
        PlanSpec planSpec,
        CancellationToken ct)
    {
        if (_stageAdapter != null)
        {
            var result = await _stageAdapter.GenerateScriptAsync(brief, planSpec, "Free", false, ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("Orchestrator generation failed, falling back to direct provider: {Error}", result.ErrorMessage);
                return await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
            }
            return result.Data;
        }
        else
        {
            return await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
        }
    }
}
