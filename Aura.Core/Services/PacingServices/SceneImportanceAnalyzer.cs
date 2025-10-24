using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PacingServices;

/// <summary>
/// Analyzes scene importance using LLM providers with fallback logic
/// </summary>
public class SceneImportanceAnalyzer
{
    private readonly ILogger<SceneImportanceAnalyzer> _logger;
    private readonly TimeSpan _llmTimeout = TimeSpan.FromSeconds(30);
    private readonly int _maxRetries = 2;

    public SceneImportanceAnalyzer(ILogger<SceneImportanceAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a single scene using LLM provider
    /// </summary>
    public async Task<SceneAnalysisData?> AnalyzeSceneAsync(
        ILlmProvider llmProvider,
        Scene scene,
        Scene? previousScene,
        string videoGoal,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Analyzing scene {SceneIndex} with LLM", scene.Index);

        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    _logger.LogDebug("Retry attempt {Attempt} for scene {SceneIndex}", attempt, scene.Index);
                    await Task.Delay(TimeSpan.FromSeconds(1 * attempt), ct);
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(_llmTimeout);

                var result = await llmProvider.AnalyzeSceneImportanceAsync(
                    scene.Script,
                    previousScene?.Script,
                    videoGoal,
                    cts.Token);

                if (result != null)
                {
                    _logger.LogDebug("Scene {SceneIndex} analysis complete: Importance={Importance}, Complexity={Complexity}",
                        scene.Index, result.Importance, result.Complexity);

                    return new SceneAnalysisData
                    {
                        SceneIndex = scene.Index,
                        Importance = Math.Clamp(result.Importance, 0, 100),
                        Complexity = Math.Clamp(result.Complexity, 0, 100),
                        EmotionalIntensity = Math.Clamp(result.EmotionalIntensity, 0, 100),
                        InformationDensity = ParseInformationDensity(result.InformationDensity),
                        OptimalDurationSeconds = Math.Max(3, result.OptimalDurationSeconds),
                        TransitionType = ParseTransitionType(result.TransitionType),
                        Reasoning = result.Reasoning ?? string.Empty,
                        AnalyzedWithLlm = true
                    };
                }

                _logger.LogWarning("LLM returned null result for scene {SceneIndex}", scene.Index);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Scene analysis cancelled for scene {SceneIndex}", scene.Index);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Scene analysis timed out for scene {SceneIndex} (attempt {Attempt})", 
                    scene.Index, attempt + 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing scene {SceneIndex} (attempt {Attempt})", 
                    scene.Index, attempt + 1);
            }
        }

        _logger.LogWarning("Failed to analyze scene {SceneIndex} with LLM after {Attempts} attempts", 
            scene.Index, _maxRetries);
        return null;
    }

    /// <summary>
    /// Analyzes multiple scenes in batch
    /// </summary>
    public async Task<IReadOnlyList<SceneAnalysisData>> AnalyzeScenesAsync(
        ILlmProvider llmProvider,
        IReadOnlyList<Scene> scenes,
        string videoGoal,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing {SceneCount} scenes with LLM provider", scenes.Count);

        var results = new List<SceneAnalysisData>();

        for (int i = 0; i < scenes.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var scene = scenes[i];
            var previousScene = i > 0 ? scenes[i - 1] : null;

            var analysis = await AnalyzeSceneAsync(llmProvider, scene, previousScene, videoGoal, ct);
            
            if (analysis != null)
            {
                results.Add(analysis);
            }
            else
            {
                // Add fallback heuristic analysis
                _logger.LogDebug("Using fallback heuristic analysis for scene {SceneIndex}", scene.Index);
                results.Add(CreateFallbackAnalysis(scene, scenes.Count));
            }
        }

        _logger.LogInformation("Scene analysis complete. {SuccessCount}/{TotalCount} scenes analyzed with LLM",
            results.Count(r => r.AnalyzedWithLlm), scenes.Count);

        return results;
    }

    /// <summary>
    /// Creates fallback analysis when LLM is unavailable
    /// </summary>
    private SceneAnalysisData CreateFallbackAnalysis(Scene scene, int totalScenes)
    {
        var wordCount = scene.Script.Split(new[] { ' ', '\t', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;

        // Heuristic importance based on position
        var importance = scene.Index switch
        {
            0 => 85.0, // Hook
            _ when scene.Index == totalScenes - 1 => 80.0, // Conclusion
            _ => 50.0 + (20.0 * (1.0 - Math.Abs(scene.Index / (double)totalScenes - 0.5)))
        };

        // Heuristic complexity based on word count
        var complexity = wordCount switch
        {
            < 30 => 30.0,
            < 70 => 50.0,
            < 120 => 70.0,
            _ => 85.0
        };

        // Heuristic emotional intensity
        var emotionalIntensity = 50.0;
        var emotionalWords = new[] { "amazing", "incredible", "important", "critical", "exciting" };
        var emotionalCount = emotionalWords.Count(word => 
            scene.Script.Contains(word, StringComparison.OrdinalIgnoreCase));
        emotionalIntensity += Math.Min(emotionalCount * 10, 30);

        // Information density based on word count
        var informationDensity = wordCount switch
        {
            < 50 => InformationDensity.Low,
            < 100 => InformationDensity.Medium,
            _ => InformationDensity.High
        };

        // Optimal duration: ~2.5 words per second
        var optimalDuration = wordCount / 2.5;

        return new SceneAnalysisData
        {
            SceneIndex = scene.Index,
            Importance = importance,
            Complexity = complexity,
            EmotionalIntensity = emotionalIntensity,
            InformationDensity = informationDensity,
            OptimalDurationSeconds = Math.Max(5, optimalDuration),
            TransitionType = scene.Index == 0 ? TransitionType.Cut : TransitionType.Fade,
            Reasoning = "Fallback heuristic analysis (LLM unavailable)",
            AnalyzedWithLlm = false
        };
    }

    private InformationDensity ParseInformationDensity(string density)
    {
        return density?.ToLowerInvariant() switch
        {
            "low" => InformationDensity.Low,
            "high" => InformationDensity.High,
            _ => InformationDensity.Medium
        };
    }

    private TransitionType ParseTransitionType(string transitionType)
    {
        return transitionType?.ToLowerInvariant() switch
        {
            "fade" => TransitionType.Fade,
            "dissolve" => TransitionType.Dissolve,
            _ => TransitionType.Cut
        };
    }
}

/// <summary>
/// Internal scene analysis data
/// </summary>
public class SceneAnalysisData
{
    public int SceneIndex { get; init; }
    public double Importance { get; init; }
    public double Complexity { get; init; }
    public double EmotionalIntensity { get; init; }
    public InformationDensity InformationDensity { get; init; }
    public double OptimalDurationSeconds { get; init; }
    public TransitionType TransitionType { get; init; }
    public string Reasoning { get; init; } = string.Empty;
    public bool AnalyzedWithLlm { get; init; }
}
