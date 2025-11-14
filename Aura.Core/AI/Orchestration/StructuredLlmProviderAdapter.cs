using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Adapters;
using Aura.Core.AI.Validation;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Orchestration;

/// <summary>
/// Adapter that wraps LLM providers with structured output validation
/// </summary>
public class StructuredLlmProviderAdapter
{
    private readonly ILogger<StructuredLlmProviderAdapter> _logger;
    private readonly LlmOrchestrationService _orchestration;
    private readonly ILlmProvider _provider;
    private readonly LlmProviderAdapter? _adapter;

    public StructuredLlmProviderAdapter(
        ILogger<StructuredLlmProviderAdapter> logger,
        LlmOrchestrationService orchestration,
        ILlmProvider provider,
        LlmProviderAdapter? adapter = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestration = orchestration ?? throw new ArgumentNullException(nameof(orchestration));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _adapter = adapter;
    }

    /// <summary>
    /// Executes brief to plan generation with schema validation
    /// </summary>
    public async Task<OrchestrationStepResult<PlanSchema>> GeneratePlanAsync(
        Models.Brief brief,
        Models.PlanSpec planSpec,
        OrchestrationConfig? config = null,
        CancellationToken ct = default)
    {
        config ??= new OrchestrationConfig();
        
        var result = await _orchestration.ExecuteStepAsync<PlanSchema>(
            "brief_to_plan",
            async (repairPrompt, token) =>
            {
                var basePrompt = BuildPlanPrompt(brief, planSpec);
                var finalPrompt = string.IsNullOrEmpty(repairPrompt) ? basePrompt : repairPrompt;
                
                if (_adapter != null)
                {
                    finalPrompt = _adapter.OptimizeUserPrompt(finalPrompt, Adapters.LlmOperationType.Creative);
                }
                
                var response = await _provider.CompleteAsync(finalPrompt, token).ConfigureAwait(false);
                return ExtractJsonFromResponse(response);
            },
            config,
            GetProviderName(),
            GetModelName(),
            ct
        ).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Executes plan to scene breakdown with schema validation
    /// </summary>
    public async Task<OrchestrationStepResult<SceneBreakdownSchema>> GenerateScenesAsync(
        PlanSchema plan,
        Models.Brief brief,
        Models.PlanSpec planSpec,
        OrchestrationConfig? config = null,
        CancellationToken ct = default)
    {
        config ??= new OrchestrationConfig();
        
        var result = await _orchestration.ExecuteStepAsync<SceneBreakdownSchema>(
            "plan_to_scenes",
            async (repairPrompt, token) =>
            {
                var basePrompt = BuildSceneBreakdownPrompt(plan, brief, planSpec);
                var finalPrompt = string.IsNullOrEmpty(repairPrompt) ? basePrompt : repairPrompt;
                
                if (_adapter != null)
                {
                    finalPrompt = _adapter.OptimizeUserPrompt(finalPrompt, Adapters.LlmOperationType.Creative);
                }
                
                var response = await _provider.CompleteAsync(finalPrompt, token).ConfigureAwait(false);
                return ExtractJsonFromResponse(response);
            },
            config,
            GetProviderName(),
            GetModelName(),
            ct
        ).ConfigureAwait(false);

        return result;
    }

    private string BuildPlanPrompt(Models.Brief brief, Models.PlanSpec planSpec)
    {
        return $@"Generate a detailed plan for a video based on the following brief:

Topic: {brief.Topic}
Audience: {brief.Audience ?? "General"}
Goal: {brief.Goal ?? "Inform and engage"}
Tone: {brief.Tone}
Language: {brief.Language}
Target Duration: {planSpec.TargetDuration.TotalSeconds} seconds
Pacing: {planSpec.Pacing}
Density: {planSpec.Density}

Return a JSON object with this structure:
{{
  ""outline"": ""detailed outline (minimum 50 characters)"",
  ""sceneCount"": number (1-50),
  ""estimatedDurationSeconds"": number (5.0-3600.0),
  ""targetPacing"": ""slow|moderate|fast|dynamic"",
  ""contentDensity"": ""sparse|moderate|dense"",
  ""narrativeStructure"": ""description of structure (minimum 10 characters)"",
  ""keyMessages"": [""message 1"", ""message 2"", ...],
  ""schema_version"": ""1.0""
}}";
    }

    private string BuildSceneBreakdownPrompt(PlanSchema plan, Models.Brief brief, Models.PlanSpec planSpec)
    {
        return $@"Based on this plan, create a detailed scene breakdown:

Plan Outline: {plan.Outline}
Scene Count: {plan.SceneCount}
Target Pacing: {plan.TargetPacing}
Content Density: {plan.ContentDensity}
Key Messages: {string.Join(", ", plan.KeyMessages)}

Return a JSON object with this structure:
{{
  ""scenes"": [
    {{
      ""index"": 0,
      ""heading"": ""scene heading (minimum 3 characters)"",
      ""script"": ""scene script (minimum 10 characters)"",
      ""durationSeconds"": number (1.0-300.0),
      ""purpose"": ""purpose of this scene (minimum 10 characters)"",
      ""visualNotes"": ""optional visual notes"",
      ""transitionType"": ""cut|fade|dissolve|wipe""
    }},
    ...
  ],
  ""schema_version"": ""1.0""
}}";
    }

    private string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return string.Empty;
        }
        
        var trimmed = response.Trim();
        
        var jsonStart = trimmed.IndexOf('{');
        var jsonEnd = trimmed.LastIndexOf('}');
        
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return trimmed.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }
        
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            var start = trimmed.IndexOf('\n') + 1;
            var end = trimmed.LastIndexOf("```");
            if (end > start)
            {
                return trimmed.Substring(start, end - start).Trim();
            }
        }
        
        return trimmed;
    }

    private string GetProviderName()
    {
        return _adapter?.ProviderName ?? "Unknown";
    }

    private string GetModelName()
    {
        return "default";
    }
}
