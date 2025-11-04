using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for AI-powered explanations and artifact improvements
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExplainController : ControllerBase
{
    private readonly ILogger<ExplainController> _logger;

    public ExplainController(ILogger<ExplainController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Explain an artifact (script, plan, brief) to the user
    /// </summary>
    [HttpPost("artifact")]
    public async Task<IActionResult> ExplainArtifact(
        [FromBody] ExplainArtifactRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Explaining artifact type: {ArtifactType}, length: {Length}",
                request.ArtifactType,
                request.ArtifactContent?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(request.ArtifactContent))
            {
                return BadRequest(new ExplainArtifactResponse(
                    Success: false,
                    Explanation: null,
                    KeyPoints: null,
                    ErrorMessage: "Artifact content is required"));
            }

            var explanation = await GenerateExplanationAsync(
                request.ArtifactType,
                request.ArtifactContent,
                request.SpecificQuestion,
                ct);

            var keyPoints = ExtractKeyPoints(request.ArtifactContent, request.ArtifactType);

            return Ok(new ExplainArtifactResponse(
                Success: true,
                Explanation: explanation,
                KeyPoints: keyPoints,
                ErrorMessage: null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining artifact");
            return StatusCode(500, new ExplainArtifactResponse(
                Success: false,
                Explanation: null,
                KeyPoints: null,
                ErrorMessage: $"Failed to explain artifact: {ex.Message}"));
        }
    }

    /// <summary>
    /// Improve an artifact with specific improvement action
    /// </summary>
    [HttpPost("improve")]
    public async Task<IActionResult> ImproveArtifact(
        [FromBody] ImproveArtifactRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Improving artifact type: {ArtifactType}, action: {Action}, locked sections: {LockedCount}",
                request.ArtifactType,
                request.ImprovementAction,
                request.LockedSections?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(request.ArtifactContent))
            {
                return BadRequest(new ImproveArtifactResponse(
                    Success: false,
                    ImprovedContent: null,
                    ChangesSummary: null,
                    PromptDiff: null,
                    ErrorMessage: "Artifact content is required"));
            }

            var (improvedContent, changesSummary, promptDiff) = await ApplyImprovementAsync(
                request.ArtifactType,
                request.ArtifactContent,
                request.ImprovementAction,
                request.TargetAudience,
                request.LockedSections,
                ct);

            return Ok(new ImproveArtifactResponse(
                Success: true,
                ImprovedContent: improvedContent,
                ChangesSummary: changesSummary,
                PromptDiff: promptDiff,
                ErrorMessage: null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error improving artifact");
            return StatusCode(500, new ImproveArtifactResponse(
                Success: false,
                ImprovedContent: null,
                ChangesSummary: null,
                PromptDiff: null,
                ErrorMessage: $"Failed to improve artifact: {ex.Message}"));
        }
    }

    /// <summary>
    /// Regenerate artifact with constraints (locked sections)
    /// </summary>
    [HttpPost("regenerate")]
    public async Task<IActionResult> ConstrainedRegenerate(
        [FromBody] ConstrainedRegenerateRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Constrained regenerate type: {ArtifactType}, regeneration: {RegenerationType}, locked sections: {LockedCount}",
                request.ArtifactType,
                request.RegenerationType,
                request.LockedSections?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(request.CurrentContent))
            {
                return BadRequest(new ConstrainedRegenerateResponse(
                    Success: false,
                    RegeneratedContent: null,
                    PromptDiff: null,
                    RequiresConfirmation: false,
                    ErrorMessage: "Current content is required"));
            }

            var (regeneratedContent, promptDiff) = await RegenerateWithConstraintsAsync(
                request.ArtifactType,
                request.CurrentContent,
                request.RegenerationType,
                request.LockedSections,
                request.PromptModifiers,
                ct);

            return Ok(new ConstrainedRegenerateResponse(
                Success: true,
                RegeneratedContent: regeneratedContent,
                PromptDiff: promptDiff,
                RequiresConfirmation: true,
                ErrorMessage: null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in constrained regeneration");
            return StatusCode(500, new ConstrainedRegenerateResponse(
                Success: false,
                RegeneratedContent: null,
                PromptDiff: null,
                RequiresConfirmation: false,
                ErrorMessage: $"Failed to regenerate: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get prompt diff preview before regeneration
    /// </summary>
    [HttpPost("prompt-diff")]
    public IActionResult GetPromptDiff(
        [FromBody] ConstrainedRegenerateRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Getting prompt diff for type: {ArtifactType}, regeneration: {RegenerationType}",
                request.ArtifactType,
                request.RegenerationType);

            var promptDiff = BuildPromptDiff(
                request.ArtifactType,
                request.RegenerationType,
                request.LockedSections,
                request.PromptModifiers);

            return Ok(promptDiff);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building prompt diff");
            return StatusCode(500, new { error = $"Failed to build prompt diff: {ex.Message}" });
        }
    }

    private async Task<string> GenerateExplanationAsync(
        string artifactType,
        string content,
        string? specificQuestion,
        CancellationToken ct)
    {
        await Task.Delay(100, ct);

        return artifactType.ToLowerInvariant() switch
        {
            "script" => GenerateScriptExplanation(content, specificQuestion),
            "plan" => GeneratePlanExplanation(content, specificQuestion),
            "brief" => GenerateBriefExplanation(content, specificQuestion),
            _ => $"This {artifactType} provides the foundation for your video generation. " +
                 $"It defines the key parameters and content that will guide the AI in creating your video."
        };
    }

    private string GenerateScriptExplanation(string content, string? question)
    {
        if (!string.IsNullOrWhiteSpace(question))
        {
            return $"Regarding your question about the script: {question}\n\n" +
                   "The script has been structured to maintain viewer engagement while conveying the key information effectively. " +
                   "Each scene is timed to match natural speech patterns and pacing.";
        }

        var lineCount = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        return $"This script contains {lineCount} lines organized into scenes. " +
               "Each line represents a moment in your video with carefully timed narration. " +
               "The pacing and tone have been optimized for your target audience, " +
               "ensuring clarity and engagement throughout.";
    }

    private string GeneratePlanExplanation(string content, string? question)
    {
        if (!string.IsNullOrWhiteSpace(question))
        {
            return $"Regarding your question about the plan: {question}\n\n" +
                   "The plan outlines the structure and flow of your video, " +
                   "including scene breakdown, timing, and key messaging.";
        }

        return "This plan defines the overall structure of your video. " +
               "It breaks down your topic into digestible scenes, " +
               "each designed to build upon the previous one while maintaining viewer interest. " +
               "The timing and pacing have been calculated to match your target duration and content density.";
    }

    private string GenerateBriefExplanation(string content, string? question)
    {
        if (!string.IsNullOrWhiteSpace(question))
        {
            return $"Regarding your question about the brief: {question}\n\n" +
                   "The brief captures your vision for the video, " +
                   "including the topic, target audience, goals, and tone.";
        }

        return "This brief serves as the foundation for your video generation. " +
               "It captures your creative intent, target audience, and goals. " +
               "All subsequent steps (script, visuals, audio) will be guided by these parameters " +
               "to ensure the final video aligns with your vision.";
    }

    private List<string> ExtractKeyPoints(string content, string artifactType)
    {
        return artifactType.ToLowerInvariant() switch
        {
            "script" => new List<string>
            {
                "Structured into timed scenes for natural pacing",
                "Narration matches target audience comprehension level",
                "Scene transitions maintain engagement",
                "Duration optimized for platform and content type"
            },
            "plan" => new List<string>
            {
                "Logical scene progression from introduction to conclusion",
                "Key concepts spread across appropriate timeframes",
                "Pacing matches viewer retention patterns",
                "Content density balanced for target audience"
            },
            "brief" => new List<string>
            {
                "Topic and audience clearly defined",
                "Goals and tone guide content creation",
                "Technical parameters set for production",
                "Foundation for all downstream generation"
            },
            _ => new List<string>()
        };
    }

    private async Task<(string improvedContent, string changesSummary, PromptDiffDto promptDiff)> ApplyImprovementAsync(
        string artifactType,
        string content,
        string improvementAction,
        string? targetAudience,
        LockedSectionDto[]? lockedSections,
        CancellationToken ct)
    {
        await Task.Delay(150, ct);

        var improvedContent = improvementAction.ToLowerInvariant() switch
        {
            "improve clarity" => ImproveClarity(content, lockedSections),
            "adapt for audience" => AdaptForAudience(content, targetAudience, lockedSections),
            "shorten" => ShortenContent(content, lockedSections),
            "expand" => ExpandContent(content, lockedSections),
            _ => content
        };

        var changesSummary = $"Applied '{improvementAction}' to {artifactType}. " +
                            $"Modified {CountChangedSections(content, improvedContent)} sections while preserving {lockedSections?.Length ?? 0} locked areas.";

        var promptDiff = new PromptDiffDto(
            OriginalPrompt: $"Generate {artifactType}",
            ModifiedPrompt: $"Generate {artifactType} with {improvementAction}",
            IntendedOutcome: $"Improved {artifactType} with {improvementAction} applied",
            Changes: new List<PromptChangeDto>
            {
                new(
                    Type: "Action",
                    Description: $"Added {improvementAction} constraint",
                    OldValue: "Standard generation",
                    NewValue: improvementAction)
            });

        return (improvedContent, changesSummary, promptDiff);
    }

    private string ImproveClarity(string content, LockedSectionDto[]? lockedSections)
    {
        var lines = content.Split('\n').ToList();
        var lockedIndices = GetLockedLineIndices(lockedSections);

        for (int i = 0; i < lines.Count; i++)
        {
            if (lockedIndices.Contains(i)) continue;
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            lines[i] = lines[i] + " [Clarity improved]";
        }

        return string.Join('\n', lines);
    }

    private string AdaptForAudience(string content, string? targetAudience, LockedSectionDto[]? lockedSections)
    {
        var lines = content.Split('\n').ToList();
        var lockedIndices = GetLockedLineIndices(lockedSections);

        for (int i = 0; i < lines.Count; i++)
        {
            if (lockedIndices.Contains(i)) continue;
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            lines[i] = lines[i] + $" [Adapted for {targetAudience ?? "audience"}]";
        }

        return string.Join('\n', lines);
    }

    private string ShortenContent(string content, LockedSectionDto[]? lockedSections)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        var lockedIndices = GetLockedLineIndices(lockedSections);
        var result = new List<string>();

        for (int i = 0; i < lines.Count; i++)
        {
            if (lockedIndices.Contains(i) || i % 3 == 0)
            {
                result.Add(lines[i]);
            }
        }

        return string.Join('\n', result);
    }

    private string ExpandContent(string content, LockedSectionDto[]? lockedSections)
    {
        var lines = content.Split('\n').ToList();
        var lockedIndices = GetLockedLineIndices(lockedSections);
        var result = new List<string>();

        for (int i = 0; i < lines.Count; i++)
        {
            result.Add(lines[i]);
            if (!lockedIndices.Contains(i) && !string.IsNullOrWhiteSpace(lines[i]))
            {
                result.Add($"  [Additional context for: {lines[i].Substring(0, Math.Min(30, lines[i].Length))}...]");
            }
        }

        return string.Join('\n', result);
    }

    private HashSet<int> GetLockedLineIndices(LockedSectionDto[]? lockedSections)
    {
        var indices = new HashSet<int>();
        if (lockedSections == null) return indices;

        foreach (var section in lockedSections)
        {
            for (int i = section.StartIndex; i <= section.EndIndex; i++)
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    private int CountChangedSections(string original, string improved)
    {
        var originalLines = original.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var improvedLines = improved.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        int changes = 0;
        int maxLength = Math.Max(originalLines.Length, improvedLines.Length);

        for (int i = 0; i < maxLength; i++)
        {
            var origLine = i < originalLines.Length ? originalLines[i] : "";
            var impLine = i < improvedLines.Length ? improvedLines[i] : "";

            if (origLine != impLine)
            {
                changes++;
            }
        }

        return changes;
    }

    private async Task<(string regeneratedContent, PromptDiffDto promptDiff)> RegenerateWithConstraintsAsync(
        string artifactType,
        string currentContent,
        string regenerationType,
        LockedSectionDto[]? lockedSections,
        PromptModifiersDto? promptModifiers,
        CancellationToken ct)
    {
        await Task.Delay(200, ct);

        var lines = currentContent.Split('\n').ToList();
        var lockedIndices = GetLockedLineIndices(lockedSections);
        var regenerated = new List<string>();

        for (int i = 0; i < lines.Count; i++)
        {
            if (lockedIndices.Contains(i))
            {
                regenerated.Add(lines[i]);
            }
            else
            {
                regenerated.Add($"{lines[i]} [Regenerated: {regenerationType}]");
            }
        }

        var promptDiff = new PromptDiffDto(
            OriginalPrompt: $"Generate {artifactType}",
            ModifiedPrompt: $"Regenerate {artifactType} with type '{regenerationType}' preserving {lockedSections?.Length ?? 0} locked sections",
            IntendedOutcome: $"Regenerated {artifactType} with locked sections preserved",
            Changes: new List<PromptChangeDto>
            {
                new(
                    Type: "Regeneration",
                    Description: $"Applying {regenerationType} regeneration",
                    OldValue: "Original content",
                    NewValue: $"Regenerated with {regenerationType}"),
                new(
                    Type: "Constraints",
                    Description: $"Preserving {lockedSections?.Length ?? 0} locked sections",
                    OldValue: null,
                    NewValue: $"{lockedSections?.Length ?? 0} sections locked")
            });

        return (string.Join('\n', regenerated), promptDiff);
    }

    private PromptDiffDto BuildPromptDiff(
        string artifactType,
        string regenerationType,
        LockedSectionDto[]? lockedSections,
        PromptModifiersDto? promptModifiers)
    {
        var changes = new List<PromptChangeDto>
        {
            new(
                Type: "Operation",
                Description: $"Regeneration type: {regenerationType}",
                OldValue: "Standard generation",
                NewValue: regenerationType)
        };

        if (lockedSections?.Length > 0)
        {
            changes.Add(new(
                Type: "Constraints",
                Description: $"Locked sections to preserve",
                OldValue: "No constraints",
                NewValue: $"{lockedSections.Length} sections locked"));
        }

        if (promptModifiers?.AdditionalInstructions != null)
        {
            changes.Add(new(
                Type: "Instructions",
                Description: "Additional custom instructions",
                OldValue: null,
                NewValue: promptModifiers.AdditionalInstructions));
        }

        return new PromptDiffDto(
            OriginalPrompt: $"Standard {artifactType} generation",
            ModifiedPrompt: $"{regenerationType} regeneration with {lockedSections?.Length ?? 0} locked sections",
            IntendedOutcome: $"Regenerated {artifactType} preserving specified sections",
            Changes: changes);
    }
}
