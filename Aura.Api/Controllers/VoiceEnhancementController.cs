using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;
using Aura.Core.Services.VoiceEnhancement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for voice enhancement and processing
/// </summary>
[ApiController]
[Route("api/voice-enhancement")]
public class VoiceEnhancementController : ControllerBase
{
    private readonly ILogger<VoiceEnhancementController> _logger;
    private readonly VoiceProcessingService _voiceProcessing;
    private readonly NoiseReductionService _noiseReduction;
    private readonly EqualizeService _equalizer;
    private readonly ProsodyAdjustmentService _prosody;
    private readonly EmotionDetectionService _emotionDetection;

    public VoiceEnhancementController(
        ILogger<VoiceEnhancementController> logger,
        VoiceProcessingService voiceProcessing,
        NoiseReductionService noiseReduction,
        EqualizeService equalizer,
        ProsodyAdjustmentService prosody,
        EmotionDetectionService emotionDetection)
    {
        _logger = logger;
        _voiceProcessing = voiceProcessing;
        _noiseReduction = noiseReduction;
        _equalizer = equalizer;
        _prosody = prosody;
        _emotionDetection = emotionDetection;
    }

    /// <summary>
    /// Enhances voice audio with specified configuration
    /// </summary>
    [HttpPost("enhance")]
    public async Task<IActionResult> EnhanceVoice(
        [FromBody] EnhanceVoiceRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InputPath))
            {
                return BadRequest(new { success = false, error = "Input path is required" });
            }

            _logger.LogInformation("Enhancing voice: {InputPath}", request.InputPath);

            var config = new VoiceEnhancementConfig
            {
                EnableNoiseReduction = request.EnableNoiseReduction,
                NoiseReductionStrength = request.NoiseReductionStrength,
                EnableEqualization = request.EnableEqualization,
                EqualizationPreset = request.EqualizationPreset,
                EnableProsodyAdjustment = request.EnableProsodyAdjustment,
                Prosody = request.Prosody,
                EnableEmotionEnhancement = request.EnableEmotionEnhancement,
                TargetEmotion = request.TargetEmotion
            };

            var result = await _voiceProcessing.EnhanceVoiceAsync(
                request.InputPath,
                config,
                ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                outputPath = result.OutputPath,
                processingTimeMs = result.ProcessingTimeMs,
                qualityMetrics = result.QualityMetrics,
                messages = result.Messages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing voice");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Analyzes voice quality without enhancement
    /// </summary>
    [HttpPost("analyze-quality")]
    public async Task<IActionResult> AnalyzeQuality(
        [FromBody] AnalyzeQualityRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InputPath))
            {
                return BadRequest(new { success = false, error = "Input path is required" });
            }

            _logger.LogInformation("Analyzing voice quality: {InputPath}", request.InputPath);

            var metrics = await _voiceProcessing.AnalyzeQualityAsync(request.InputPath, ct).ConfigureAwait(false);

            return Ok(new { success = true, metrics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing voice quality");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Detects emotion in voice audio
    /// </summary>
    [HttpPost("detect-emotion")]
    public async Task<IActionResult> DetectEmotion(
        [FromBody] DetectEmotionRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AudioPath))
            {
                return BadRequest(new { success = false, error = "Audio path is required" });
            }

            _logger.LogInformation("Detecting emotion: {AudioPath}", request.AudioPath);

            var result = await _emotionDetection.DetectEmotionAsync(request.AudioPath, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                emotion = result.Emotion.ToString(),
                confidence = result.Confidence,
                features = result.Features
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting emotion");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Batch enhances multiple audio files
    /// </summary>
    [HttpPost("batch-enhance")]
    public async Task<IActionResult> BatchEnhance(
        [FromBody] BatchEnhanceRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.InputPaths == null || request.InputPaths.Count == 0)
            {
                return BadRequest(new { success = false, error = "Input paths are required" });
            }

            _logger.LogInformation("Batch enhancing {Count} files", request.InputPaths.Count);

            var config = new VoiceEnhancementConfig
            {
                EnableNoiseReduction = request.EnableNoiseReduction,
                NoiseReductionStrength = request.NoiseReductionStrength,
                EnableEqualization = request.EnableEqualization,
                EqualizationPreset = request.EqualizationPreset,
                EnableProsodyAdjustment = request.EnableProsodyAdjustment,
                Prosody = request.Prosody,
                EnableEmotionEnhancement = request.EnableEmotionEnhancement,
                TargetEmotion = request.TargetEmotion
            };

            var progress = new Progress<int>(percent =>
            {
                _logger.LogDebug("Batch processing progress: {Percent}%", percent);
            });

            var results = await _voiceProcessing.BatchEnhanceAsync(
                request.InputPaths,
                config,
                progress,
                ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                results = results.Select(r => new
                {
                    outputPath = r.OutputPath,
                    processingTimeMs = r.ProcessingTimeMs,
                    messages = r.Messages
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch enhancement");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Applies noise reduction to audio
    /// </summary>
    [HttpPost("reduce-noise")]
    public async Task<IActionResult> ReduceNoise(
        [FromBody] ReduceNoiseRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InputPath))
            {
                return BadRequest(new { success = false, error = "Input path is required" });
            }

            _logger.LogInformation("Reducing noise: {InputPath}", request.InputPath);

            var outputPath = await _noiseReduction.ReduceNoiseAsync(
                request.InputPath,
                request.Strength,
                ct).ConfigureAwait(false);

            return Ok(new { success = true, outputPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reducing noise");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Applies equalization to audio
    /// </summary>
    [HttpPost("equalize")]
    public async Task<IActionResult> Equalize(
        [FromBody] EqualizationRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InputPath))
            {
                return BadRequest(new { success = false, error = "Input path is required" });
            }

            _logger.LogInformation("Applying equalization: {InputPath}", request.InputPath);

            var outputPath = await _equalizer.ApplyEqualizationAsync(
                request.InputPath,
                request.Preset,
                ct).ConfigureAwait(false);

            return Ok(new { success = true, outputPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying equalization");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Adjusts prosody (pitch, rate, etc.)
    /// </summary>
    [HttpPost("adjust-prosody")]
    public async Task<IActionResult> AdjustProsody(
        [FromBody] AdjustProsodyRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InputPath))
            {
                return BadRequest(new { success = false, error = "Input path is required" });
            }

            if (request.Settings == null)
            {
                return BadRequest(new { success = false, error = "Prosody settings are required" });
            }

            _logger.LogInformation("Adjusting prosody: {InputPath}", request.InputPath);

            var outputPath = await _prosody.AdjustProsodyAsync(
                request.InputPath,
                request.Settings,
                ct).ConfigureAwait(false);

            return Ok(new { success = true, outputPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting prosody");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

// Request models
public record EnhanceVoiceRequest
{
    [Required]
    public required string InputPath { get; init; }
    public bool EnableNoiseReduction { get; init; } = true;
    public double NoiseReductionStrength { get; init; } = 0.7;
    public bool EnableEqualization { get; init; } = true;
    public EqualizationPreset EqualizationPreset { get; init; } = EqualizationPreset.Balanced;
    public bool EnableProsodyAdjustment { get; init; }
    public ProsodySettings? Prosody { get; init; }
    public bool EnableEmotionEnhancement { get; init; }
    public EmotionTarget? TargetEmotion { get; init; }
}

public record AnalyzeQualityRequest
{
    [Required]
    public required string InputPath { get; init; }
}

public record DetectEmotionRequest
{
    [Required]
    public required string AudioPath { get; init; }
}

public record BatchEnhanceRequest
{
    [Required]
    public required List<string> InputPaths { get; init; }
    public bool EnableNoiseReduction { get; init; } = true;
    public double NoiseReductionStrength { get; init; } = 0.7;
    public bool EnableEqualization { get; init; } = true;
    public EqualizationPreset EqualizationPreset { get; init; } = EqualizationPreset.Balanced;
    public bool EnableProsodyAdjustment { get; init; }
    public ProsodySettings? Prosody { get; init; }
    public bool EnableEmotionEnhancement { get; init; }
    public EmotionTarget? TargetEmotion { get; init; }
}

public record ReduceNoiseRequest
{
    [Required]
    public required string InputPath { get; init; }
    public double Strength { get; init; } = 0.7;
}

public record EqualizationRequest
{
    [Required]
    public required string InputPath { get; init; }
    public EqualizationPreset Preset { get; init; } = EqualizationPreset.Balanced;
}

public record AdjustProsodyRequest
{
    [Required]
    public required string InputPath { get; init; }
    [Required]
    public required ProsodySettings Settings { get; init; }
}
