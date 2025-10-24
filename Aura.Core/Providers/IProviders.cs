using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;

namespace Aura.Core.Providers;

public interface ILlmProvider
{
    Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct);
    Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(
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