using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Director;

/// <summary>
/// Analyzes the emotional arc of video scenes using LLM to determine pacing and visual treatment.
/// </summary>
public class EmotionalArcAnalyzer
{
    private readonly ILogger<EmotionalArcAnalyzer> _logger;
    private readonly ILlmProvider? _llmProvider;

    public EmotionalArcAnalyzer(
        ILogger<EmotionalArcAnalyzer> logger,
        ILlmProvider? llmProvider = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Analyzes the emotional arc of scenes to determine intensity, emotion, and focus points.
    /// </summary>
    public async Task<EmotionalArcResult> AnalyzeAsync(
        IReadOnlyList<Scene> scenes,
        Brief brief,
        CancellationToken ct)
    {
        if (_llmProvider == null || scenes.Count == 0)
        {
            _logger.LogInformation("Using heuristic emotional analysis (LLM not available or no scenes)");
            return CreateHeuristicResult(scenes, brief);
        }

        try
        {
            var prompt = BuildEmotionalAnalysisPrompt(scenes, brief);
            var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            
            var result = ParseEmotionalArc(response, scenes.Count);
            if (result != null)
            {
                _logger.LogInformation("LLM emotional arc analysis completed: {Summary}", result.Summary);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM emotional arc analysis failed, falling back to heuristics");
        }

        return CreateHeuristicResult(scenes, brief);
    }

    private string BuildEmotionalAnalysisPrompt(IReadOnlyList<Scene> scenes, Brief brief)
    {
        var sceneDescriptions = string.Join("\n\n", scenes.Select((s, i) => 
            $"Scene {i + 1}: {s.Heading}\n{s.Script}"));

        return $@"Analyze the emotional arc of this video script. For each scene, identify:
1. Emotional intensity (0.0-1.0)
2. Primary emotion (excitement, calm, tension, joy, sadness, curiosity, urgency)
3. Whether this is a key point requiring visual emphasis (true/false)
4. The ideal visual focus point (center, left-third, right-third, top, bottom, subject)

Video goal: {brief.Goal ?? "inform and engage"}
Tone: {brief.Tone}
Audience: {brief.Audience ?? "general audience"}

Scenes:
{sceneDescriptions}

Respond in JSON format with this exact structure:
{{
  ""scenes"": [
    {{
      ""sceneIndex"": 0,
      ""intensity"": 0.5,
      ""emotion"": ""curiosity"",
      ""isKeyPoint"": false,
      ""focusPoint"": ""center""
    }}
  ],
  ""summary"": ""Brief description of overall emotional arc"",
  ""overallTone"": ""The dominant tone of the video""
}}";
    }

    private EmotionalArcResult? ParseEmotionalArc(string response, int expectedSceneCount)
    {
        try
        {
            // Extract JSON from response (handle markdown code blocks and nested JSON)
            var json = ExtractJsonFromResponse(response);
            
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogWarning("Could not find valid JSON in LLM response");
                return null;
            }
            
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var sceneEmotions = new List<SceneEmotion>();
            
            if (root.TryGetProperty("scenes", out var scenesArray))
            {
                foreach (var sceneElement in scenesArray.EnumerateArray())
                {
                    var intensity = sceneElement.TryGetProperty("intensity", out var intensityProp) 
                        ? intensityProp.GetDouble() 
                        : 0.5;
                    
                    var emotion = sceneElement.TryGetProperty("emotion", out var emotionProp) 
                        ? emotionProp.GetString() ?? "neutral"
                        : "neutral";
                    
                    var isKeyPoint = sceneElement.TryGetProperty("isKeyPoint", out var keyProp) 
                        && keyProp.GetBoolean();
                    
                    var focusPoint = sceneElement.TryGetProperty("focusPoint", out var focusProp) 
                        ? focusProp.GetString() ?? "center"
                        : "center";

                    sceneEmotions.Add(new SceneEmotion(intensity, emotion, isKeyPoint, focusPoint));
                }
            }

            // Pad with defaults if needed
            while (sceneEmotions.Count < expectedSceneCount)
            {
                sceneEmotions.Add(new SceneEmotion(0.5, "neutral", false, "center"));
            }

            var summary = root.TryGetProperty("summary", out var summaryProp) 
                ? summaryProp.GetString() ?? "Balanced emotional arc"
                : "Balanced emotional arc";
            
            var overallTone = root.TryGetProperty("overallTone", out var toneProp) 
                ? toneProp.GetString() ?? "informative"
                : "informative";

            return new EmotionalArcResult(sceneEmotions, summary, overallTone);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse emotional arc JSON");
            return null;
        }
    }

    /// <summary>
    /// Extracts JSON from an LLM response, handling markdown code blocks and nested structures.
    /// </summary>
    private static string? ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        // Try to find JSON inside markdown code blocks first
        var codeBlockStart = response.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (codeBlockStart >= 0)
        {
            var jsonStart = codeBlockStart + 7; // Skip "```json"
            var codeBlockEnd = response.IndexOf("```", jsonStart, StringComparison.Ordinal);
            if (codeBlockEnd > jsonStart)
            {
                var jsonCandidate = response.Substring(jsonStart, codeBlockEnd - jsonStart).Trim();
                if (IsValidJson(jsonCandidate))
                {
                    return jsonCandidate;
                }
            }
        }

        // Try generic code block
        codeBlockStart = response.IndexOf("```", StringComparison.Ordinal);
        if (codeBlockStart >= 0)
        {
            var jsonStart = response.IndexOf('\n', codeBlockStart);
            if (jsonStart >= 0)
            {
                jsonStart++; // Skip newline
                var codeBlockEnd = response.IndexOf("```", jsonStart, StringComparison.Ordinal);
                if (codeBlockEnd > jsonStart)
                {
                    var jsonCandidate = response.Substring(jsonStart, codeBlockEnd - jsonStart).Trim();
                    if (IsValidJson(jsonCandidate))
                    {
                        return jsonCandidate;
                    }
                }
            }
        }

        // Find balanced JSON object using brace matching
        var firstBrace = response.IndexOf('{');
        if (firstBrace < 0)
        {
            return null;
        }

        var depth = 0;
        var inString = false;
        var escape = false;
        
        for (int i = firstBrace; i < response.Length; i++)
        {
            var c = response[i];
            
            if (escape)
            {
                escape = false;
                continue;
            }
            
            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }
            
            if (c == '"')
            {
                inString = !inString;
                continue;
            }
            
            if (inString) continue;
            
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    var json = response.Substring(firstBrace, i - firstBrace + 1);
                    if (IsValidJson(json))
                    {
                        return json;
                    }
                    break;
                }
            }
        }

        return null;
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }

    private EmotionalArcResult CreateHeuristicResult(IReadOnlyList<Scene> scenes, Brief brief)
    {
        var sceneEmotions = new List<SceneEmotion>();
        
        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            
            // Determine intensity based on position in video
            double intensity;
            if (i == 0)
            {
                intensity = 0.6; // Opening hook
            }
            else if (i == scenes.Count - 1)
            {
                intensity = 0.7; // Strong close
            }
            else if (i == scenes.Count / 2)
            {
                intensity = 0.5; // Mid-point transition
            }
            else
            {
                intensity = 0.5 + (Math.Sin(i * Math.PI / scenes.Count) * 0.2);
            }

            // Analyze text for emotional indicators
            var text = scene.Script.ToLowerInvariant();
            var emotion = DetermineEmotionFromText(text, brief.Tone);
            
            // Key points are typically at beginning, transitions, and end
            var isKeyPoint = i == 0 || i == scenes.Count - 1 || 
                            text.Contains("important") || 
                            text.Contains("key") ||
                            text.Contains("remember");

            sceneEmotions.Add(new SceneEmotion(
                intensity,
                emotion,
                isKeyPoint,
                isKeyPoint ? "center" : "left-third"
            ));
        }

        var summary = $"Heuristic analysis for {scenes.Count} scenes with {brief.Tone} tone";
        return new EmotionalArcResult(sceneEmotions, summary, brief.Tone);
    }

    private static string DetermineEmotionFromText(string text, string defaultTone)
    {
        // Simple keyword-based emotion detection
        if (text.Contains("exciting") || text.Contains("amazing") || text.Contains("incredible"))
            return "excitement";
        if (text.Contains("calm") || text.Contains("peaceful") || text.Contains("relax"))
            return "calm";
        if (text.Contains("urgent") || text.Contains("critical") || text.Contains("immediately"))
            return "urgency";
        if (text.Contains("curious") || text.Contains("wonder") || text.Contains("discover"))
            return "curiosity";
        if (text.Contains("happy") || text.Contains("joy") || text.Contains("celebrate"))
            return "joy";
        if (text.Contains("challenge") || text.Contains("problem") || text.Contains("difficult"))
            return "tension";
        
        // Map default tone to emotion
        return defaultTone.ToLowerInvariant() switch
        {
            "energetic" or "exciting" => "excitement",
            "calm" or "peaceful" => "calm",
            "urgent" => "urgency",
            "curious" or "educational" => "curiosity",
            "happy" or "upbeat" => "joy",
            _ => "neutral"
        };
    }
}
