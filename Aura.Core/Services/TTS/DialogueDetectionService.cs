using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.TTS;

/// <summary>
/// Detects dialogue, characters, and emotions in scripts using LLM analysis.
/// </summary>
public class DialogueDetectionService : IDialogueDetectionService
{
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<DialogueDetectionService> _logger;

    public DialogueDetectionService(
        ILlmProvider llmProvider,
        ILogger<DialogueDetectionService> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DialogueAnalysis> AnalyzeScriptAsync(
        string script,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return new DialogueAnalysis(
                Lines: Array.Empty<DialogueLine>(),
                Characters: Array.Empty<DetectedCharacter>(),
                HasMultipleCharacters: false);
        }

        _logger.LogInformation("Analyzing script for dialogue detection ({Length} characters)", script.Length);

        var prompt = BuildAnalysisPrompt(script);
        
        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            var analysis = ParseDialogueAnalysis(response, script);
            
            _logger.LogInformation(
                "Dialogue analysis complete: {LineCount} lines, {CharacterCount} characters",
                analysis.Lines.Count,
                analysis.Characters.Count);
            
            return analysis;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "LLM dialogue analysis failed, falling back to simple detection");
            return FallbackAnalysis(script);
        }
    }

    private static string BuildAnalysisPrompt(string script)
    {
        return $@"Analyze this script for dialogue and characters. 

For each segment, identify:
1. Whether it's narration, dialogue, quote, or internal thought
2. The character speaking (if dialogue)
3. The emotional tone

Script:
{script}

Return ONLY valid JSON with this structure (no markdown, no explanation):
{{
  ""lines"": [
    {{
      ""startIndex"": 0,
      ""endIndex"": 50,
      ""text"": ""The actual text segment"",
      ""character"": ""Narrator"",
      ""type"": ""narration"",
      ""emotion"": ""neutral""
    }}
  ],
  ""characters"": [
    {{
      ""name"": ""Narrator"",
      ""suggestedVoiceType"": ""male-mature"",
      ""lineCount"": 5
    }}
  ]
}}

Type values: narration, dialogue, quote, internal
Emotion values: neutral, excited, sad, angry, curious, calm (or null)
Voice type suggestions: male-young, male-mature, female-young, female-mature, neutral";
    }

    private DialogueAnalysis ParseDialogueAnalysis(string response, string originalScript)
    {
        try
        {
            // Strip markdown code blocks if present
            var jsonContent = response.Trim();
            if (jsonContent.StartsWith("```", StringComparison.Ordinal))
            {
                var startIndex = jsonContent.IndexOf('{');
                var endIndex = jsonContent.LastIndexOf('}');
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    jsonContent = jsonContent.Substring(startIndex, endIndex - startIndex + 1);
                }
            }

            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var lines = new List<DialogueLine>();
            var characters = new List<DetectedCharacter>();

            if (root.TryGetProperty("lines", out var linesElement))
            {
                foreach (var lineElement in linesElement.EnumerateArray())
                {
                    var startIndex = lineElement.TryGetProperty("startIndex", out var si) ? si.GetInt32() : 0;
                    var endIndex = lineElement.TryGetProperty("endIndex", out var ei) ? ei.GetInt32() : 0;
                    var text = lineElement.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
                    var character = lineElement.TryGetProperty("character", out var c) ? c.GetString() : null;
                    var typeStr = lineElement.TryGetProperty("type", out var tp) ? tp.GetString() ?? "narration" : "narration";
                    var emotionStr = lineElement.TryGetProperty("emotion", out var em) ? em.GetString() : null;

                    var dialogueType = ParseDialogueType(typeStr);
                    var emotion = ParseEmotion(emotionStr);

                    lines.Add(new DialogueLine(startIndex, endIndex, text, character, dialogueType, emotion));
                }
            }

            if (root.TryGetProperty("characters", out var charsElement))
            {
                foreach (var charElement in charsElement.EnumerateArray())
                {
                    var name = charElement.TryGetProperty("name", out var n) ? n.GetString() ?? "Unknown" : "Unknown";
                    var voiceType = charElement.TryGetProperty("suggestedVoiceType", out var vt) ? vt.GetString() ?? "neutral" : "neutral";
                    var lineCount = charElement.TryGetProperty("lineCount", out var lc) ? lc.GetInt32() : 0;

                    characters.Add(new DetectedCharacter(name, voiceType, lineCount));
                }
            }

            var hasMultiple = characters.Count > 1 || 
                              lines.Any(l => l.Type == DialogueType.Dialogue);

            return new DialogueAnalysis(lines, characters, hasMultiple);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response as JSON");
            return FallbackAnalysis(originalScript);
        }
    }

    private static DialogueType ParseDialogueType(string typeStr)
    {
        return typeStr?.ToLowerInvariant() switch
        {
            "dialogue" => DialogueType.Dialogue,
            "quote" => DialogueType.Quote,
            "internal" => DialogueType.InternalThought,
            _ => DialogueType.Narration
        };
    }

    private static EmotionHint? ParseEmotion(string? emotionStr)
    {
        if (string.IsNullOrWhiteSpace(emotionStr) || emotionStr.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return emotionStr.ToLowerInvariant() switch
        {
            "excited" => EmotionHint.Excited,
            "sad" => EmotionHint.Sad,
            "angry" => EmotionHint.Angry,
            "curious" => EmotionHint.Curious,
            "calm" => EmotionHint.Calm,
            "neutral" => EmotionHint.Neutral,
            _ => null
        };
    }

    private DialogueAnalysis FallbackAnalysis(string script)
    {
        // Simple fallback: treat entire script as narration
        var lines = new List<DialogueLine>
        {
            new(
                StartIndex: 0,
                EndIndex: script.Length,
                Text: script,
                CharacterName: "Narrator",
                Type: DialogueType.Narration,
                Emotion: null)
        };

        var characters = new List<DetectedCharacter>
        {
            new("Narrator", "neutral", 1)
        };

        return new DialogueAnalysis(lines, characters, HasMultipleCharacters: false);
    }
}
