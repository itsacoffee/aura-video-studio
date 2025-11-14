using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.VoiceEnhancement;

/// <summary>
/// Main service for voice enhancement and processing
/// </summary>
public class VoiceProcessingService
{
    private readonly ILogger<VoiceProcessingService> _logger;
    private readonly NoiseReductionService _noiseReduction;
    private readonly EqualizeService _equalizer;
    private readonly ProsodyAdjustmentService _prosodyAdjustment;
    private readonly EmotionDetectionService _emotionDetection;
    private readonly string _tempDirectory;

    public VoiceProcessingService(
        ILogger<VoiceProcessingService> logger,
        NoiseReductionService noiseReduction,
        EqualizeService equalizer,
        ProsodyAdjustmentService prosodyAdjustment,
        EmotionDetectionService emotionDetection)
    {
        _logger = logger;
        _noiseReduction = noiseReduction;
        _equalizer = equalizer;
        _prosodyAdjustment = prosodyAdjustment;
        _emotionDetection = emotionDetection;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "VoiceEnhancement");

        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    /// <summary>
    /// Enhances voice audio with configured processing pipeline
    /// </summary>
    public async Task<VoiceEnhancementResult> EnhanceVoiceAsync(
        string inputPath,
        VoiceEnhancementConfig config,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting voice enhancement for: {InputPath}", inputPath);
        var sw = Stopwatch.StartNew();
        var messages = new List<string>();

        try
        {
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException($"Input file not found: {inputPath}");
            }

            string currentPath = inputPath;

            // Step 1: Noise Reduction
            if (config.EnableNoiseReduction)
            {
                _logger.LogDebug("Applying noise reduction (strength: {Strength})", config.NoiseReductionStrength);
                currentPath = await _noiseReduction.ReduceNoiseAsync(
                    currentPath,
                    config.NoiseReductionStrength,
                    ct).ConfigureAwait(false);
                messages.Add($"Noise reduction applied (strength: {config.NoiseReductionStrength:P0})");
            }

            // Step 2: Equalization
            if (config.EnableEqualization)
            {
                _logger.LogDebug("Applying equalization (preset: {Preset})", config.EqualizationPreset);
                currentPath = await _equalizer.ApplyEqualizationAsync(
                    currentPath,
                    config.EqualizationPreset,
                    ct).ConfigureAwait(false);
                messages.Add($"Equalization applied (preset: {config.EqualizationPreset})");
            }

            // Step 3: Prosody Adjustment
            if (config.EnableProsodyAdjustment && config.Prosody != null)
            {
                _logger.LogDebug("Applying prosody adjustment");
                currentPath = await _prosodyAdjustment.AdjustProsodyAsync(
                    currentPath,
                    config.Prosody,
                    ct).ConfigureAwait(false);
                messages.Add("Prosody adjustment applied");
            }

            // Step 4: Emotion Enhancement
            VoiceQualityMetrics? metrics = null;
            if (config.EnableEmotionEnhancement && config.TargetEmotion != null)
            {
                _logger.LogDebug("Detecting and enhancing emotion: {Emotion}", config.TargetEmotion.Emotion);
                var emotionResult = await _emotionDetection.DetectEmotionAsync(currentPath, ct).ConfigureAwait(false);
                
                // Calculate basic audio metrics
                var audioMetrics = await AnalyzeAudioMetricsAsync(currentPath, ct).ConfigureAwait(false);
                
                metrics = new VoiceQualityMetrics
                {
                    DetectedEmotion = emotionResult.Emotion,
                    EmotionConfidence = emotionResult.Confidence,
                    SignalToNoiseRatio = audioMetrics.SignalToNoiseRatio,
                    PeakLevel = audioMetrics.PeakLevel,
                    RmsLevel = audioMetrics.RmsLevel,
                    Lufs = audioMetrics.Lufs,
                    ClarityScore = audioMetrics.ClarityScore
                };

                messages.Add($"Emotion detected: {emotionResult.Emotion} (confidence: {emotionResult.Confidence:P0})");
            }

            sw.Stop();

            return new VoiceEnhancementResult
            {
                OutputPath = currentPath,
                ProcessingTimeMs = sw.ElapsedMilliseconds,
                QualityMetrics = metrics,
                Messages = messages.ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing voice");
            throw;
        }
    }

    /// <summary>
    /// Processes multiple audio files in batch
    /// </summary>
    public async Task<List<VoiceEnhancementResult>> BatchEnhanceAsync(
        IEnumerable<string> inputPaths,
        VoiceEnhancementConfig config,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var results = new List<VoiceEnhancementResult>();
        var pathsList = new List<string>(inputPaths);
        int total = pathsList.Count;
        int completed = 0;

        foreach (var path in pathsList)
        {
            ct.ThrowIfCancellationRequested();

            var result = await EnhanceVoiceAsync(path, config, ct).ConfigureAwait(false);
            results.Add(result);

            completed++;
            progress?.Report((int)((double)completed / total * 100));
        }

        return results;
    }

    /// <summary>
    /// Creates a processing pipeline with custom effects
    /// </summary>
    public VoiceProcessingPipeline CreatePipeline()
    {
        return new VoiceProcessingPipeline(_logger, _tempDirectory);
    }

    /// <summary>
    /// Analyzes voice quality without enhancement
    /// </summary>
    public async Task<VoiceQualityMetrics> AnalyzeQualityAsync(
        string inputPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing voice quality for: {InputPath}", inputPath);

        try
        {
            var emotionResult = await _emotionDetection.DetectEmotionAsync(inputPath, ct).ConfigureAwait(false);
            var audioMetrics = await AnalyzeAudioMetricsAsync(inputPath, ct).ConfigureAwait(false);

            return new VoiceQualityMetrics
            {
                DetectedEmotion = emotionResult.Emotion,
                EmotionConfidence = emotionResult.Confidence,
                SignalToNoiseRatio = audioMetrics.SignalToNoiseRatio,
                PeakLevel = audioMetrics.PeakLevel,
                RmsLevel = audioMetrics.RmsLevel,
                Lufs = audioMetrics.Lufs,
                ClarityScore = audioMetrics.ClarityScore
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing voice quality");
            throw;
        }
    }

    /// <summary>
    /// Analyzes basic audio metrics from a file
    /// </summary>
    private async Task<VoiceQualityMetrics> AnalyzeAudioMetricsAsync(
        string audioPath,
        CancellationToken ct = default)
    {
        // This is a simplified implementation
        // In a production system, this would use FFmpeg or a specialized audio library
        // to perform actual signal analysis
        
        await Task.Delay(10, ct).ConfigureAwait(false); // Simulate processing time

        // Calculate approximations based on file characteristics
        var fileInfo = new FileInfo(audioPath);
        var fileSize = fileInfo.Length;
        
        // Estimate SNR based on file size/bitrate (higher bitrate typically means better SNR)
        // This is a very rough approximation
        var estimatedSnr = Math.Min(45.0, 25.0 + (fileSize / 100000.0));
        
        // Return reasonable default metrics
        // In production, these would be calculated using actual audio analysis
        return new VoiceQualityMetrics
        {
            SignalToNoiseRatio = estimatedSnr,
            PeakLevel = -3.0,      // Typical safe headroom
            RmsLevel = -18.0,      // Typical speech RMS level
            Lufs = -14.0,          // YouTube standard loudness
            ClarityScore = 0.85    // Assumed good quality
        };
    }
}

/// <summary>
/// Modular voice processing pipeline
/// </summary>
public class VoiceProcessingPipeline
{
    private readonly ILogger _logger;
    private readonly string _tempDirectory;
    private readonly List<Func<string, CancellationToken, Task<string>>> _effects;

    public VoiceProcessingPipeline(ILogger logger, string tempDirectory)
    {
        _logger = logger;
        _tempDirectory = tempDirectory;
        _effects = new List<Func<string, CancellationToken, Task<string>>>();
    }

    /// <summary>
    /// Adds an effect to the pipeline
    /// </summary>
    public VoiceProcessingPipeline AddEffect(Func<string, CancellationToken, Task<string>> effect)
    {
        _effects.Add(effect);
        return this;
    }

    /// <summary>
    /// Executes the pipeline on an input file
    /// </summary>
    public async Task<string> ProcessAsync(string inputPath, CancellationToken ct = default)
    {
        string currentPath = inputPath;

        foreach (var effect in _effects)
        {
            ct.ThrowIfCancellationRequested();
            currentPath = await effect(currentPath, ct).ConfigureAwait(false);
        }

        return currentPath;
    }

    /// <summary>
    /// Gets the number of effects in the pipeline
    /// </summary>
    public int EffectCount => _effects.Count;
}
