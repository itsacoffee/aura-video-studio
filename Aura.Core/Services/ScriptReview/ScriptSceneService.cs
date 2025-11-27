using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ScriptReview;

/// <summary>
/// Service for modifying individual script scenes using AI
/// Supports regeneration, expansion, shortening, and B-Roll suggestion generation
/// </summary>
public class ScriptSceneService
{
    private readonly ILogger<ScriptSceneService> _logger;
    private readonly ILlmProvider _llmProvider;

    public ScriptSceneService(
        ILogger<ScriptSceneService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Regenerate a single scene with optional context from surrounding scenes
    /// </summary>
    public async Task<SceneResult> RegenerateSceneAsync(
        string jobId,
        int sceneIndex,
        string? brief,
        string? currentSceneText,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Regenerating scene {SceneIndex} for job {JobId}",
            sceneIndex, jobId);

        try
        {
            var prompt = BuildRegeneratePrompt(sceneIndex, brief, currentSceneText);
            
            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(15),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "Engaging"
            );

            var generatedText = await _llmProvider.DraftScriptAsync(
                new Brief(
                    Topic: prompt,
                    Audience: "General",
                    Goal: "Regenerate scene with fresh content",
                    Tone: "Adaptive",
                    Language: "en-US",
                    Aspect: Aspect.Widescreen16x9
                ),
                planSpec,
                ct).ConfigureAwait(false);

            return new SceneResult(
                Success: true,
                Text: generatedText.Trim(),
                Error: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate scene {SceneIndex}", sceneIndex);
            return new SceneResult(
                Success: false,
                Text: null,
                Error: ex.Message
            );
        }
    }

    /// <summary>
    /// Expand a scene by a target factor (e.g., 1.5 = 50% longer)
    /// </summary>
    public async Task<SceneResult> ExpandSceneAsync(
        string jobId,
        int sceneIndex,
        string currentSceneText,
        double targetExpansion,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Expanding scene {SceneIndex} by {Expansion}x for job {JobId}",
            sceneIndex, targetExpansion, jobId);

        try
        {
            var prompt = BuildExpandPrompt(sceneIndex, currentSceneText, targetExpansion);
            
            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(30),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "Detailed"
            );

            var generatedText = await _llmProvider.DraftScriptAsync(
                new Brief(
                    Topic: prompt,
                    Audience: "General",
                    Goal: "Expand scene with more detail",
                    Tone: "Adaptive",
                    Language: "en-US",
                    Aspect: Aspect.Widescreen16x9
                ),
                planSpec,
                ct).ConfigureAwait(false);

            return new SceneResult(
                Success: true,
                Text: generatedText.Trim(),
                Error: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expand scene {SceneIndex}", sceneIndex);
            return new SceneResult(
                Success: false,
                Text: null,
                Error: ex.Message
            );
        }
    }

    /// <summary>
    /// Shorten a scene by a target factor (e.g., 0.7 = 30% shorter)
    /// </summary>
    public async Task<SceneResult> ShortenSceneAsync(
        string jobId,
        int sceneIndex,
        string currentSceneText,
        double targetReduction,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Shortening scene {SceneIndex} to {Reduction}x for job {JobId}",
            sceneIndex, targetReduction, jobId);

        try
        {
            var prompt = BuildShortenPrompt(sceneIndex, currentSceneText, targetReduction);
            
            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(10),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "Concise"
            );

            var generatedText = await _llmProvider.DraftScriptAsync(
                new Brief(
                    Topic: prompt,
                    Audience: "General",
                    Goal: "Shorten scene while preserving meaning",
                    Tone: "Adaptive",
                    Language: "en-US",
                    Aspect: Aspect.Widescreen16x9
                ),
                planSpec,
                ct).ConfigureAwait(false);

            return new SceneResult(
                Success: true,
                Text: generatedText.Trim(),
                Error: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shorten scene {SceneIndex}", sceneIndex);
            return new SceneResult(
                Success: false,
                Text: null,
                Error: ex.Message
            );
        }
    }

    /// <summary>
    /// Generate B-Roll visual suggestions for a scene
    /// </summary>
    public async Task<BRollResult> GenerateBRollSuggestionsAsync(
        string jobId,
        int sceneIndex,
        string sceneText,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Generating B-Roll suggestions for scene {SceneIndex} in job {JobId}",
            sceneIndex, jobId);

        try
        {
            var prompt = BuildBRollPrompt(sceneIndex, sceneText);
            
            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(10),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "Visual"
            );

            var generatedText = await _llmProvider.DraftScriptAsync(
                new Brief(
                    Topic: prompt,
                    Audience: "General",
                    Goal: "Generate B-Roll visual suggestions",
                    Tone: "Creative",
                    Language: "en-US",
                    Aspect: Aspect.Widescreen16x9
                ),
                planSpec,
                ct).ConfigureAwait(false);

            var suggestions = ParseBRollSuggestions(generatedText);

            return new BRollResult(
                Success: true,
                Suggestions: suggestions,
                Error: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate B-Roll suggestions for scene {SceneIndex}", sceneIndex);
            return new BRollResult(
                Success: false,
                Suggestions: new List<string>(),
                Error: ex.Message
            );
        }
    }

    private static string BuildRegeneratePrompt(int sceneIndex, string? brief, string? currentText)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a video script writer. Regenerate the following scene with fresh, engaging content.");
        sb.AppendLine();
        sb.AppendLine($"Scene {sceneIndex + 1}:");
        
        if (!string.IsNullOrEmpty(currentText))
        {
            sb.AppendLine($"Current content: {currentText}");
        }
        
        if (!string.IsNullOrEmpty(brief))
        {
            sb.AppendLine($"Brief/Topic: {brief}");
        }
        
        sb.AppendLine();
        sb.AppendLine("Write a new version that is:");
        sb.AppendLine("- Engaging and different from the previous version");
        sb.AppendLine("- Similar in length to the original");
        sb.AppendLine("- Maintains the same topic/subject matter");
        sb.AppendLine();
        sb.AppendLine("Return ONLY the new scene narration text, no additional formatting or explanation.");
        
        return sb.ToString();
    }

    private static string BuildExpandPrompt(int sceneIndex, string currentText, double targetExpansion)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a video script writer. Expand the following scene to add more detail and depth.");
        sb.AppendLine();
        sb.AppendLine($"Scene {sceneIndex + 1} (current text):");
        sb.AppendLine(currentText);
        sb.AppendLine();
        sb.AppendLine($"Target: Make this scene approximately {(targetExpansion * 100) - 100:F0}% longer.");
        sb.AppendLine();
        sb.AppendLine("Expansion guidelines:");
        sb.AppendLine("- Add more detail, examples, or context");
        sb.AppendLine("- Maintain the original tone and message");
        sb.AppendLine("- Ensure smooth flow and coherence");
        sb.AppendLine("- Keep the expanded content relevant");
        sb.AppendLine();
        sb.AppendLine("Return ONLY the expanded scene narration text, no additional formatting or explanation.");
        
        return sb.ToString();
    }

    private static string BuildShortenPrompt(int sceneIndex, string currentText, double targetReduction)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a video script writer. Condense the following scene while preserving its core message.");
        sb.AppendLine();
        sb.AppendLine($"Scene {sceneIndex + 1} (current text):");
        sb.AppendLine(currentText);
        sb.AppendLine();
        sb.AppendLine($"Target: Make this scene approximately {(1 - targetReduction) * 100:F0}% shorter.");
        sb.AppendLine();
        sb.AppendLine("Shortening guidelines:");
        sb.AppendLine("- Remove redundant words and phrases");
        sb.AppendLine("- Keep the essential message and key points");
        sb.AppendLine("- Maintain clarity and coherence");
        sb.AppendLine("- Preserve the original tone");
        sb.AppendLine();
        sb.AppendLine("Return ONLY the shortened scene narration text, no additional formatting or explanation.");
        
        return sb.ToString();
    }

    private static string BuildBRollPrompt(int sceneIndex, string sceneText)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a video production expert. Generate 5 creative B-Roll visual suggestions for the following scene.");
        sb.AppendLine();
        sb.AppendLine($"Scene {sceneIndex + 1}:");
        sb.AppendLine(sceneText);
        sb.AppendLine();
        sb.AppendLine("Generate 5 specific, actionable B-Roll suggestions that:");
        sb.AppendLine("- Visually complement the narration");
        sb.AppendLine("- Are diverse (mix of footage types: stock video, graphics, animations)");
        sb.AppendLine("- Are practical to source or create");
        sb.AppendLine("- Add visual interest and engagement");
        sb.AppendLine();
        sb.AppendLine("Format: Return each suggestion on a new line, numbered 1-5. No additional explanation.");
        
        return sb.ToString();
    }

    private static List<string> ParseBRollSuggestions(string generatedText)
    {
        var suggestions = new List<string>();
        var lines = generatedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;
            
            // Remove common numbering patterns (1., 2., etc. or 1), 2), etc.)
            var cleanedLine = System.Text.RegularExpressions.Regex.Replace(
                trimmedLine,
                @"^\d+[\.\)]\s*",
                string.Empty
            ).Trim();
            
            if (!string.IsNullOrWhiteSpace(cleanedLine))
            {
                suggestions.Add(cleanedLine);
            }
            
            // Limit to 5 suggestions
            if (suggestions.Count >= 5)
                break;
        }
        
        return suggestions;
    }
}

/// <summary>
/// Result of a scene modification operation
/// </summary>
public record SceneResult(
    bool Success,
    string? Text,
    string? Error
);

/// <summary>
/// Result of B-Roll suggestion generation
/// </summary>
public record BRollResult(
    bool Success,
    List<string> Suggestions,
    string? Error
);
