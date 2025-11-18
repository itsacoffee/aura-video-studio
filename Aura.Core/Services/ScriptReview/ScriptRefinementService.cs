using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ScriptReview;

/// <summary>
/// Service for interactive, user-directed script refinement
/// Allows natural language instructions for script modifications
/// </summary>
public class ScriptRefinementService
{
    private readonly ILogger<ScriptRefinementService> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;

    public ScriptRefinementService(
        ILogger<ScriptRefinementService> logger,
        ILlmProvider llmProvider,
        LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
    }

    /// <summary>
    /// Refine script based on natural language instruction from user
    /// </summary>
    public async Task<ScriptRefinementResponse> RefineScriptAsync(
        ScriptRefinementRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Refining script with instruction: {Instruction}",
            request.Instruction);

        if (string.IsNullOrWhiteSpace(request.CurrentScript))
        {
            throw new ArgumentException("Current script is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Instruction))
        {
            throw new ArgumentException("Refinement instruction is required", nameof(request));
        }

        var prompt = BuildRefinementPrompt(request);
        
        var brief = new Brief(
            Topic: prompt,
            Audience: request.Context?.Audience ?? "General",
            Goal: "Refine script based on user instruction",
            Tone: "Adaptive",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(request.Context?.TargetDurationSeconds ?? 60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Adaptive"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

        // Parse the LLM response
        var (revisedScript, diffSummary, riskNote) = ParseRefinementResponse(response, request);

        return new ScriptRefinementResponse(
            RevisedScript: revisedScript,
            DiffSummary: diffSummary,
            RiskNote: riskNote,
            GeneratedAt: DateTime.UtcNow
        );
    }

    private string BuildRefinementPrompt(ScriptRefinementRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert script editor helping refine a video script based on user feedback.");
        sb.AppendLine();
        sb.AppendLine("Current Script:");
        sb.AppendLine("---");
        sb.AppendLine(request.CurrentScript);
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"User's Instruction: {request.Instruction}");
        sb.AppendLine();

        if (request.Context != null)
        {
            if (!string.IsNullOrEmpty(request.Context.Audience))
            {
                sb.AppendLine($"Target Audience: {request.Context.Audience}");
            }
            if (!string.IsNullOrEmpty(request.Context.Goal))
            {
                sb.AppendLine($"Video Goal: {request.Context.Goal}");
            }
            if (request.Context.TargetDurationSeconds.HasValue)
            {
                sb.AppendLine($"Target Duration: {request.Context.TargetDurationSeconds} seconds");
            }
        }

        sb.AppendLine();
        sb.AppendLine("You must respond with ONLY valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"revisedScript\": \"The complete revised script in markdown format\",");
        sb.AppendLine("  \"diffSummary\": \"Clear summary of what changed and why (2-3 sentences)\",");
        sb.AppendLine("  \"riskNote\": \"Any potential issues or considerations (e.g., 'This makes the script 30 seconds longer' or 'This changes the tone significantly'). Empty string if no risks.\"");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Instructions:");
        sb.AppendLine("- Apply the user's instruction thoughtfully and completely");
        sb.AppendLine("- Maintain the overall structure and flow unless instructed otherwise");
        sb.AppendLine("- Keep the script's voice consistent");
        sb.AppendLine("- If the instruction would cause issues (duration changes, tone shifts, etc.), note them in riskNote");
        sb.AppendLine("- Provide a helpful diffSummary explaining what you changed");
        sb.AppendLine("- Return ONLY the JSON object, no additional text or markdown formatting");

        return sb.ToString();
    }

    private (string revisedScript, string diffSummary, string? riskNote) ParseRefinementResponse(
        string response,
        ScriptRefinementRequest request)
    {
        try
        {
            // Clean the response - remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            // Parse JSON response
            var jsonDoc = JsonDocument.Parse(cleanedResponse);
            var root = jsonDoc.RootElement;

            var revisedScript = root.GetProperty("revisedScript").GetString() ?? request.CurrentScript;
            var diffSummary = root.GetProperty("diffSummary").GetString() ?? "Script refined based on your instruction.";
            var riskNote = root.TryGetProperty("riskNote", out var riskElement) && !string.IsNullOrWhiteSpace(riskElement.GetString())
                ? riskElement.GetString()
                : null;

            return (revisedScript, diffSummary, riskNote);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response for script refinement, using fallback");
            
            // Fallback: Return original script with explanation
            return (
                request.CurrentScript,
                "Unable to parse refinement response. Please try rephrasing your instruction.",
                "The AI was unable to process your request. Please try again."
            );
        }
    }

    private async Task<string> GenerateWithLlmAsync(
        Brief brief,
        PlanSpec planSpec,
        CancellationToken ct)
    {
        // Use LLM provider directly for script refinement
        return await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Request for script refinement
/// </summary>
public record ScriptRefinementRequest(
    string CurrentScript,
    string Instruction,
    ScriptContext? Context = null
);

/// <summary>
/// Context information about the script being refined
/// </summary>
public record ScriptContext(
    string? Audience = null,
    string? Goal = null,
    int? TargetDurationSeconds = null,
    string? Platform = null
);

/// <summary>
/// Response from script refinement
/// </summary>
public record ScriptRefinementResponse(
    string RevisedScript,
    string DiffSummary,
    string? RiskNote,
    DateTime GeneratedAt
);
