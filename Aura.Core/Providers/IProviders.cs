using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Visual;

namespace Aura.Core.Providers;

public interface ILlmProvider
{
    Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct);
    Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(
        string sceneText,
        string? previousSceneText,
        string videoGoal,
        CancellationToken ct);
    Task<VisualPromptResult?> GenerateVisualPromptAsync(
        string sceneText,
        string? previousSceneText,
        string videoTone,
        VisualStyle targetStyle,
        CancellationToken ct);
    Task<ContentComplexityAnalysisResult?> AnalyzeContentComplexityAsync(
        string sceneText,
        string? previousSceneText,
        string videoGoal,
        CancellationToken ct);
}

/// <summary>
/// Result of LLM-based scene analysis for pacing optimization
/// </summary>
public record SceneAnalysisResult(
    double Importance,
    double Complexity,
    double EmotionalIntensity,
    string InformationDensity,
    double OptimalDurationSeconds,
    string TransitionType,
    string Reasoning
);

/// <summary>
/// Result of LLM-based visual prompt generation
/// </summary>
public record VisualPromptResult(
    string DetailedDescription,
    string CompositionGuidelines,
    string LightingMood,
    string LightingDirection,
    string LightingQuality,
    string TimeOfDay,
    string[] ColorPalette,
    string ShotType,
    string CameraAngle,
    string DepthOfField,
    string[] StyleKeywords,
    string[] NegativeElements,
    string[] ContinuityElements,
    string Reasoning
);

/// <summary>
/// Result of LLM-based content complexity analysis for adaptive pacing
/// </summary>
public record ContentComplexityAnalysisResult(
    double OverallComplexityScore,
    double ConceptDifficulty,
    double TerminologyDensity,
    double PrerequisiteKnowledgeLevel,
    double MultiStepReasoningRequired,
    int NewConceptsIntroduced,
    double CognitiveProcessingTimeSeconds,
    double OptimalAttentionWindowSeconds,
    string DetailedBreakdown
);

public interface ITtsProvider
{
    Task<IReadOnlyList<string>> GetAvailableVoicesAsync();
    Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct);
}

public interface IImageProvider
{
    Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct);
}

public interface IVideoComposer
{
    Task<string> RenderAsync(Timeline timeline, RenderSpec spec, IProgress<RenderProgress> progress, CancellationToken ct);
}

public interface IStockProvider
{
    Task<IReadOnlyList<Asset>> SearchAsync(string query, int count, CancellationToken ct);
}

public record VisualSpec(string Style, Aspect Aspect, string[] Keywords);

public record Timeline(
    IReadOnlyList<Scene> Scenes,
    IReadOnlyDictionary<int, IReadOnlyList<Asset>> SceneAssets, 
    string NarrationPath, 
    string MusicPath,
    string? SubtitlesPath);