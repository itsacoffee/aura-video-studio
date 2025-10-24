using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PacingServices;

/// <summary>
/// Service for analyzing emotional beats and arcs across video scenes
/// Uses LLM to detect emotional intensity and map emotional flow
/// </summary>
public class EmotionalBeatAnalyzer
{
    private readonly ILogger<EmotionalBeatAnalyzer> _logger;
    private readonly TimeSpan _llmTimeout = TimeSpan.FromSeconds(30);

    public EmotionalBeatAnalyzer(ILogger<EmotionalBeatAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes emotional beats for all scenes in a video
    /// </summary>
    public async Task<IReadOnlyList<EmotionalBeat>> AnalyzeEmotionalBeatsAsync(
        IReadOnlyList<Scene> scenes,
        ILlmProvider? llmProvider = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing emotional beats for {SceneCount} scenes", scenes.Count);

        var beats = new List<EmotionalBeat>();
        EmotionalBeat? previousBeat = null;

        for (int i = 0; i < scenes.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var scene = scenes[i];
            EmotionalBeat beat;

            if (llmProvider != null)
            {
                beat = await AnalyzeSceneEmotionalBeatAsync(scene, previousBeat, llmProvider, ct)
                    ?? CreateFallbackEmotionalBeat(scene, previousBeat);
            }
            else
            {
                beat = CreateFallbackEmotionalBeat(scene, previousBeat);
            }

            beats.Add(beat);
            previousBeat = beat;
        }

        // Identify peaks and valleys
        IdentifyPeaksAndValleys(beats);

        // Calculate arc positions
        CalculateArcPositions(beats);

        _logger.LogInformation("Identified {PeakCount} emotional peaks and {ValleyCount} valleys",
            beats.Count(b => b.IsPeak), beats.Count(b => b.IsValley));

        return beats;
    }

    /// <summary>
    /// Analyzes a single scene's emotional characteristics using LLM
    /// </summary>
    private async Task<EmotionalBeat?> AnalyzeSceneEmotionalBeatAsync(
        Scene scene,
        EmotionalBeat? previousBeat,
        ILlmProvider llmProvider,
        CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_llmTimeout);

            // Use scene analysis to get emotional data since ILlmProvider doesn't have CompleteAsync
            var analysisResult = await llmProvider.AnalyzeSceneImportanceAsync(
                scene.Script,
                null,
                "emotional analysis",
                cts.Token);

            if (analysisResult == null)
            {
                _logger.LogWarning("Empty response from LLM for scene {SceneIndex}", scene.Index);
                return null;
            }

            return new EmotionalBeat
            {
                SceneIndex = scene.Index,
                EmotionalIntensity = Math.Clamp(analysisResult.EmotionalIntensity, 0, 100),
                PrimaryEmotion = DetermineEmotionFromIntensity(analysisResult.EmotionalIntensity),
                EmotionalChange = previousBeat != null 
                    ? CalculateEmotionalChange(analysisResult.EmotionalIntensity, previousBeat.EmotionalIntensity)
                    : EmotionalChange.Stable,
                ViewerImpact = DetermineViewerImpact(analysisResult.EmotionalIntensity),
                RecommendedEmphasis = DetermineEmphasis(analysisResult.EmotionalIntensity, 
                    previousBeat != null 
                        ? CalculateEmotionalChange(analysisResult.EmotionalIntensity, previousBeat.EmotionalIntensity)
                        : EmotionalChange.Stable),
                IsPeak = false,
                IsValley = false,
                ArcPosition = 0.0,
                Analysis = $"LLM analysis: emotional intensity {analysisResult.EmotionalIntensity}",
                Timestamp = scene.Start
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze emotional beat for scene {SceneIndex}", scene.Index);
            return null;
        }
    }

    private string DetermineEmotionFromIntensity(double intensity)
    {
        return intensity switch
        {
            >= 80 => "excitement",
            >= 60 => "joy",
            >= 40 => "neutral",
            >= 20 => "calm",
            _ => "sadness"
        };
    }

    private string BuildEmotionalAnalysisPrompt(Scene scene, EmotionalBeat? previousBeat)
    {
        var previousEmotionInfo = previousBeat != null
            ? $"Previous emotion: {previousBeat.PrimaryEmotion}, intensity: {previousBeat.EmotionalIntensity}"
            : "No previous scene";

        return $@"Analyze this scene's emotional characteristics and return JSON with:
- emotionalIntensity (0-100): Overall emotional intensity level
- primaryEmotion (string): The main emotion (e.g., excitement, tension, calm, joy, sadness, surprise)
- emotionalChange (rising/falling/stable): Direction of emotional change
- viewerImpact (low/medium/high): Predicted impact on viewer
- recommendedEmphasis (more/same/less): Pacing emphasis recommendation

Scene: {scene.Script}
{previousEmotionInfo}

Return ONLY valid JSON in this exact format:
{{
  ""emotionalIntensity"": 75,
  ""primaryEmotion"": ""excitement"",
  ""emotionalChange"": ""rising"",
  ""viewerImpact"": ""high"",
  ""recommendedEmphasis"": ""more""
}}";
    }

    private EmotionalBeat? ParseEmotionalBeatResponse(string response, Scene scene)
    {
        try
        {
            // Extract JSON from response (handle markdown code blocks)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart == -1 || jsonEnd == -1 || jsonEnd <= jsonStart)
            {
                _logger.LogWarning("No valid JSON found in LLM response");
                return null;
            }

            var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var data = JsonSerializer.Deserialize<EmotionalBeatData>(json);

            if (data == null)
            {
                _logger.LogWarning("Failed to deserialize emotional beat data");
                return null;
            }

            return new EmotionalBeat
            {
                SceneIndex = scene.Index,
                EmotionalIntensity = Math.Clamp(data.EmotionalIntensity, 0, 100),
                PrimaryEmotion = data.PrimaryEmotion ?? "neutral",
                EmotionalChange = ParseEmotionalChange(data.EmotionalChange),
                ViewerImpact = ParseViewerImpact(data.ViewerImpact),
                RecommendedEmphasis = ParsePacingEmphasis(data.RecommendedEmphasis),
                IsPeak = false, // Will be set later
                IsValley = false, // Will be set later
                ArcPosition = 0.0, // Will be calculated later
                Analysis = $"LLM analysis: {data.PrimaryEmotion} with {data.ViewerImpact} impact",
                Timestamp = scene.Start
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing emotional beat response");
            return null;
        }
    }

    private EmotionalBeat CreateFallbackEmotionalBeat(Scene scene, EmotionalBeat? previousBeat)
    {
        // Heuristic-based emotional analysis
        var intensity = CalculateHeuristicIntensity(scene);
        var emotion = DetectPrimaryEmotion(scene);
        var change = CalculateEmotionalChange(intensity, previousBeat?.EmotionalIntensity ?? 50);
        var impact = DetermineViewerImpact(intensity);
        var emphasis = DetermineEmphasis(intensity, change);

        return new EmotionalBeat
        {
            SceneIndex = scene.Index,
            EmotionalIntensity = intensity,
            PrimaryEmotion = emotion,
            EmotionalChange = change,
            ViewerImpact = impact,
            RecommendedEmphasis = emphasis,
            IsPeak = false,
            IsValley = false,
            ArcPosition = 0.0,
            Analysis = "Heuristic analysis (LLM unavailable)",
            Timestamp = scene.Start
        };
    }

    private double CalculateHeuristicIntensity(Scene scene)
    {
        var baseIntensity = 50.0;
        var text = scene.Script.ToLowerInvariant();

        // High intensity words
        var highIntensityWords = new[] { "amazing", "incredible", "shocking", "urgent", "critical", "must", "now", "important" };
        var highCount = highIntensityWords.Count(w => text.Contains(w));
        baseIntensity += highCount * 10;

        // Exclamation marks
        var exclamationCount = scene.Script.Count(c => c == '!');
        baseIntensity += exclamationCount * 5;

        // Question marks (moderate intensity)
        var questionCount = scene.Script.Count(c => c == '?');
        baseIntensity += questionCount * 3;

        return Math.Clamp(baseIntensity, 0, 100);
    }

    private string DetectPrimaryEmotion(Scene scene)
    {
        var text = scene.Script.ToLowerInvariant();

        var emotionKeywords = new Dictionary<string, string[]>
        {
            ["excitement"] = new[] { "amazing", "incredible", "awesome", "fantastic", "wow" },
            ["tension"] = new[] { "urgent", "critical", "warning", "careful", "dangerous" },
            ["joy"] = new[] { "happy", "joy", "delightful", "wonderful", "love" },
            ["surprise"] = new[] { "surprise", "unexpected", "shocking", "believe" },
            ["calm"] = new[] { "calm", "peaceful", "relax", "gentle", "soft" },
            ["sadness"] = new[] { "sad", "unfortunately", "sorry", "loss", "difficult" }
        };

        foreach (var (emotion, keywords) in emotionKeywords)
        {
            if (keywords.Any(k => text.Contains(k)))
                return emotion;
        }

        return "neutral";
    }

    private EmotionalChange CalculateEmotionalChange(double currentIntensity, double previousIntensity)
    {
        var diff = currentIntensity - previousIntensity;
        
        if (diff > 10)
            return EmotionalChange.Rising;
        if (diff < -10)
            return EmotionalChange.Falling;
        
        return EmotionalChange.Stable;
    }

    private ViewerImpact DetermineViewerImpact(double intensity)
    {
        return intensity switch
        {
            >= 70 => ViewerImpact.High,
            >= 40 => ViewerImpact.Medium,
            _ => ViewerImpact.Low
        };
    }

    private PacingEmphasis DetermineEmphasis(double intensity, EmotionalChange change)
    {
        if (intensity >= 70 || change == EmotionalChange.Rising)
            return PacingEmphasis.More;
        if (intensity <= 30 || change == EmotionalChange.Falling)
            return PacingEmphasis.Less;
        
        return PacingEmphasis.Same;
    }

    private void IdentifyPeaksAndValleys(List<EmotionalBeat> beats)
    {
        if (beats.Count < 3)
            return;

        for (int i = 1; i < beats.Count - 1; i++)
        {
            var current = beats[i].EmotionalIntensity;
            var previous = beats[i - 1].EmotionalIntensity;
            var next = beats[i + 1].EmotionalIntensity;

            // Peak: higher than both neighbors by at least 15 points
            if (current > previous + 15 && current > next + 15)
            {
                beats[i] = beats[i] with { IsPeak = true };
            }

            // Valley: lower than both neighbors by at least 15 points
            if (current < previous - 15 && current < next - 15)
            {
                beats[i] = beats[i] with { IsValley = true };
            }
        }
    }

    private void CalculateArcPositions(List<EmotionalBeat> beats)
    {
        for (int i = 0; i < beats.Count; i++)
        {
            var position = beats.Count > 1 ? i / (double)(beats.Count - 1) : 0.0;
            beats[i] = beats[i] with { ArcPosition = position };
        }
    }

    private EmotionalChange ParseEmotionalChange(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "rising" => EmotionalChange.Rising,
            "falling" => EmotionalChange.Falling,
            _ => EmotionalChange.Stable
        };
    }

    private ViewerImpact ParseViewerImpact(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "high" => ViewerImpact.High,
            "low" => ViewerImpact.Low,
            _ => ViewerImpact.Medium
        };
    }

    private PacingEmphasis ParsePacingEmphasis(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "more" => PacingEmphasis.More,
            "less" => PacingEmphasis.Less,
            _ => PacingEmphasis.Same
        };
    }

    private class EmotionalBeatData
    {
        public double EmotionalIntensity { get; set; }
        public string? PrimaryEmotion { get; set; }
        public string? EmotionalChange { get; set; }
        public string? ViewerImpact { get; set; }
        public string? RecommendedEmphasis { get; set; }
    }
}
