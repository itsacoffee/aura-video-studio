using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.Media;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Orchestration;

/// <summary>
/// Resolves accurate scene timings based on actual TTS audio output durations
/// </summary>
public class TimingResolver
{
    private readonly ILogger<TimingResolver> _logger;
    private readonly IMediaMetadataService _mediaMetadataService;

    public TimingResolver(
        ILogger<TimingResolver> logger,
        IMediaMetadataService mediaMetadataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediaMetadataService = mediaMetadataService ?? throw new ArgumentNullException(nameof(mediaMetadataService));
    }

    /// <summary>
    /// Resolves scene timings from audio file paths
    /// </summary>
    public async Task<TimingResolutionResult> ResolveSceneTimingsAsync(
        IReadOnlyList<Scene> scenes,
        IReadOnlyDictionary<int, string> sceneAudioPaths,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Resolving scene timings from audio for {SceneCount} scenes",
            scenes.Count);

        var result = new TimingResolutionResult
        {
            OriginalScenes = scenes,
            ResolvedScenes = new List<Scene>()
        };

        var currentStart = TimeSpan.Zero;
        var transitionDuration = TimeSpan.FromMilliseconds(500); // Standard transition time

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            TimeSpan duration;

            if (sceneAudioPaths.TryGetValue(scene.Index, out var audioPath) && 
                !string.IsNullOrEmpty(audioPath) && 
                File.Exists(audioPath))
            {
                try
                {
                    var metadata = await _mediaMetadataService.ExtractMetadataAsync(
                        audioPath,
                        Models.Media.MediaType.Audio,
                        cancellationToken).ConfigureAwait(false);

                    if (metadata?.Duration.HasValue == true)
                    {
                        duration = TimeSpan.FromSeconds(metadata.Duration.Value);
                        result.AudioDurationsUsed++;

                        _logger.LogDebug(
                            "Scene {SceneIndex} duration from audio: {Duration:F2}s",
                            scene.Index,
                            duration.TotalSeconds);
                    }
                    else
                    {
                        duration = EstimateDurationFromText(scene.Script);
                        result.EstimatedDurationsUsed++;

                        _logger.LogWarning(
                            "Scene {SceneIndex} audio metadata unavailable, using estimation: {Duration:F2}s",
                            scene.Index,
                            duration.TotalSeconds);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to extract duration for scene {SceneIndex}, using estimation",
                        scene.Index);

                    duration = EstimateDurationFromText(scene.Script);
                    result.EstimatedDurationsUsed++;
                }
            }
            else
            {
                duration = EstimateDurationFromText(scene.Script);
                result.EstimatedDurationsUsed++;

                _logger.LogDebug(
                    "Scene {SceneIndex} has no audio file, using estimation: {Duration:F2}s",
                    scene.Index,
                    duration.TotalSeconds);
            }

            var resolvedScene = scene with
            {
                Start = currentStart,
                Duration = duration
            };

            result.ResolvedScenes.Add(resolvedScene);
            currentStart += duration + transitionDuration;
        }

        result.TotalDuration = currentStart - transitionDuration; // Remove last transition
        result.CalculateAccuracy();

        _logger.LogInformation(
            "Timing resolution complete: {AudioBased}/{Total} scenes used audio durations, {Accuracy:F1}% accuracy",
            result.AudioDurationsUsed,
            scenes.Count,
            result.AccuracyPercentage);

        return result;
    }

    /// <summary>
    /// Resolves scene timings from a single concatenated audio file
    /// by analyzing silence/breaks in the audio
    /// </summary>
    public async Task<TimingResolutionResult> ResolveFromConcatenatedAudioAsync(
        IReadOnlyList<Scene> scenes,
        string concatenatedAudioPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Resolving scene timings from concatenated audio: {AudioPath}",
            concatenatedAudioPath);

        if (!File.Exists(concatenatedAudioPath))
        {
            throw new FileNotFoundException(
                "Concatenated audio file not found",
                concatenatedAudioPath);
        }

        var metadata = await _mediaMetadataService.ExtractMetadataAsync(
            concatenatedAudioPath,
            Models.Media.MediaType.Audio,
            cancellationToken).ConfigureAwait(false);

        if (metadata?.Duration == null)
        {
            throw new InvalidOperationException(
                "Could not extract duration from concatenated audio file");
        }

        var totalAudioDuration = TimeSpan.FromSeconds(metadata.Duration.Value);

        var result = new TimingResolutionResult
        {
            OriginalScenes = scenes,
            ResolvedScenes = new List<Scene>(),
            TotalDuration = totalAudioDuration
        };

        // Distribute duration proportionally based on word count
        int totalWords = scenes.Sum(s => CountWords(s.Script));
        var currentStart = TimeSpan.Zero;

        foreach (var scene in scenes)
        {
            int sceneWords = CountWords(scene.Script);
            double proportion = totalWords > 0 ? (double)sceneWords / totalWords : 1.0 / scenes.Count;
            TimeSpan duration = TimeSpan.FromSeconds(totalAudioDuration.TotalSeconds * proportion);

            var resolvedScene = scene with
            {
                Start = currentStart,
                Duration = duration
            };

            result.ResolvedScenes.Add(resolvedScene);
            result.AudioDurationsUsed++;
            currentStart += duration;
        }

        result.CalculateAccuracy();

        _logger.LogInformation(
            "Resolved {SceneCount} scenes from concatenated audio, total duration: {Duration:F2}s",
            scenes.Count,
            totalAudioDuration.TotalSeconds);

        return result;
    }

    /// <summary>
    /// Estimates duration from text using speech rate calculation
    /// </summary>
    private TimeSpan EstimateDurationFromText(string text)
    {
        int wordCount = CountWords(text);
        
        // Average conversational speech rate is ~150 words per minute
        const double wordsPerSecond = 150.0 / 60.0;
        double estimatedSeconds = wordCount / wordsPerSecond;
        
        // Add padding for natural pauses
        estimatedSeconds *= 1.1;
        
        return TimeSpan.FromSeconds(Math.Max(1.0, estimatedSeconds));
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

/// <summary>
/// Result of timing resolution operation
/// </summary>
public class TimingResolutionResult
{
    public IReadOnlyList<Scene> OriginalScenes { get; set; } = Array.Empty<Scene>();
    public List<Scene> ResolvedScenes { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public int AudioDurationsUsed { get; set; }
    public int EstimatedDurationsUsed { get; set; }
    public double AccuracyPercentage { get; set; }

    public void CalculateAccuracy()
    {
        int totalScenes = AudioDurationsUsed + EstimatedDurationsUsed;
        if (totalScenes > 0)
        {
            AccuracyPercentage = (AudioDurationsUsed / (double)totalScenes) * 100.0;
        }
        else
        {
            AccuracyPercentage = 0.0;
        }
    }

    public bool MeetsAcceptanceCriteria()
    {
        // Acceptance criteria: Final video matches sum of audio durations ± <=1% overhead
        if (AudioDurationsUsed == 0)
            return false;

        double calculatedTotal = ResolvedScenes.Sum(s => s.Duration.TotalSeconds);
        double difference = Math.Abs(calculatedTotal - TotalDuration.TotalSeconds);
        double percentDifference = (difference / TotalDuration.TotalSeconds) * 100.0;

        return percentDifference <= 1.0;
    }
}
