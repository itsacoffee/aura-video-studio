using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.VideoEffects;
using Aura.Core.Services.VideoEffects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for video effects management
/// </summary>
[ApiController]
[Route("api/video-effects")]
public class VideoEffectsController : ControllerBase
{
    private readonly IVideoEffectService _effectService;
    private readonly IEffectCacheService _cacheService;
    private readonly ILogger<VideoEffectsController> _logger;

    public VideoEffectsController(
        IVideoEffectService effectService,
        IEffectCacheService cacheService,
        ILogger<VideoEffectsController> logger)
    {
        _effectService = effectService ?? throw new ArgumentNullException(nameof(effectService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all available effect presets
    /// </summary>
    [HttpGet("presets")]
    [ProducesResponseType(typeof(List<EffectPreset>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPresets(
        [FromQuery] EffectCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var presets = await _effectService.GetPresetsAsync(category, cancellationToken).ConfigureAwait(false);
            return Ok(presets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effect presets");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to retrieve effect presets" });
        }
    }

    /// <summary>
    /// Get a specific preset by ID
    /// </summary>
    [HttpGet("presets/{id}")]
    [ProducesResponseType(typeof(EffectPreset), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreset(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var preset = await _effectService.GetPresetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (preset == null)
            {
                return NotFound(new { error = $"Preset '{id}' not found" });
            }

            return Ok(preset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preset {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to retrieve preset" });
        }
    }

    /// <summary>
    /// Save a custom effect preset
    /// </summary>
    [HttpPost("presets")]
    [ProducesResponseType(typeof(EffectPreset), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SavePreset(
        [FromBody] EffectPreset preset,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(preset.Name))
            {
                return BadRequest(new { error = "Preset name is required" });
            }

            var savedPreset = await _effectService.SavePresetAsync(preset, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetPreset), new { id = savedPreset.Id }, savedPreset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving preset");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to save preset" });
        }
    }

    /// <summary>
    /// Delete a custom preset
    /// </summary>
    [HttpDelete("presets/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePreset(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _effectService.DeletePresetAsync(id, cancellationToken).ConfigureAwait(false);
            if (!deleted)
            {
                return NotFound(new { error = $"Preset '{id}' not found or cannot be deleted" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting preset {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to delete preset" });
        }
    }

    /// <summary>
    /// Apply effects to a video
    /// </summary>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(ApplyEffectsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyEffects(
        [FromBody] ApplyEffectsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InputPath))
            {
                return BadRequest(new { error = "Input path is required" });
            }

            if (!System.IO.File.Exists(request.InputPath))
            {
                return BadRequest(new { error = "Input file does not exist" });
            }

            if (request.Effects == null || request.Effects.Count == 0)
            {
                return BadRequest(new { error = "At least one effect is required" });
            }

            // Generate output path if not provided
            var outputPath = request.OutputPath ?? Path.Combine(
                Path.GetTempPath(),
                $"video_effects_{Guid.NewGuid()}{Path.GetExtension(request.InputPath)}"
            );

            // Check cache first
            string? resultPath = null;
            if (request.UseCache)
            {
                var cacheKey = _cacheService.GenerateCacheKey(request.InputPath, request.Effects);
                resultPath = await _cacheService.GetCachedEffectAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            }

            // Apply effects if not in cache
            if (resultPath == null)
            {
                resultPath = await _effectService.ApplyEffectsAsync(
                    request.InputPath,
                    outputPath,
                    request.Effects,
                    progress =>
                    {
                        _logger.LogDebug("Effect application progress: {Progress}%", progress);
                    },
                    cancellationToken
                ).ConfigureAwait(false);

                // Cache the result
                if (request.UseCache)
                {
                    var cacheKey = _cacheService.GenerateCacheKey(request.InputPath, request.Effects);
                    await _cacheService.CacheEffectAsync(cacheKey, resultPath, cancellationToken).ConfigureAwait(false);
                }
            }

            return Ok(new ApplyEffectsResponse
            {
                OutputPath = resultPath,
                Success = true,
                FromCache = resultPath != outputPath
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying effects");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = $"Failed to apply effects: {ex.Message}" });
        }
    }

    /// <summary>
    /// Apply a preset to a video
    /// </summary>
    [HttpPost("apply-preset")]
    [ProducesResponseType(typeof(ApplyEffectsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyPreset(
        [FromBody] ApplyPresetRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InputPath))
            {
                return BadRequest(new { error = "Input path is required" });
            }

            if (!System.IO.File.Exists(request.InputPath))
            {
                return BadRequest(new { error = "Input file does not exist" });
            }

            if (string.IsNullOrWhiteSpace(request.PresetId))
            {
                return BadRequest(new { error = "Preset ID is required" });
            }

            // Generate output path if not provided
            var outputPath = request.OutputPath ?? Path.Combine(
                Path.GetTempPath(),
                $"video_preset_{Guid.NewGuid()}{Path.GetExtension(request.InputPath)}"
            );

            var resultPath = await _effectService.ApplyPresetAsync(
                request.InputPath,
                outputPath,
                request.PresetId,
                progress =>
                {
                    _logger.LogDebug("Preset application progress: {Progress}%", progress);
                },
                cancellationToken
            ).ConfigureAwait(false);

            return Ok(new ApplyEffectsResponse
            {
                OutputPath = resultPath,
                Success = true,
                FromCache = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying preset");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = $"Failed to apply preset: {ex.Message}" });
        }
    }

    /// <summary>
    /// Generate a preview for an effect
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(EffectPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GeneratePreview(
        [FromBody] EffectPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InputPath))
            {
                return BadRequest(new { error = "Input path is required" });
            }

            if (!System.IO.File.Exists(request.InputPath))
            {
                return BadRequest(new { error = "Input file does not exist" });
            }

            if (request.Effect == null)
            {
                return BadRequest(new { error = "Effect is required" });
            }

            var previewDuration = TimeSpan.FromSeconds(request.PreviewDurationSeconds);
            var previewPath = await _effectService.GenerateEffectPreviewAsync(
                request.InputPath,
                request.Effect,
                previewDuration,
                cancellationToken
            ).ConfigureAwait(false);

            return Ok(new EffectPreviewResponse
            {
                PreviewPath = previewPath,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating effect preview");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = $"Failed to generate preview: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get recommended effects for a video
    /// </summary>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(List<EffectPreset>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecommendations(
        [FromQuery] string videoPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(videoPath))
            {
                return BadRequest(new { error = "Video path is required" });
            }

            var recommendations = await _effectService.GetRecommendedEffectsAsync(videoPath, cancellationToken).ConfigureAwait(false);
            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effect recommendations");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to get recommendations" });
        }
    }

    /// <summary>
    /// Validate an effect
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    public IActionResult ValidateEffect([FromBody] VideoEffect effect)
    {
        try
        {
            var isValid = _effectService.ValidateEffect(effect, out var errorMessage);
            return Ok(new ValidationResponse
            {
                IsValid = isValid,
                ErrorMessage = errorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating effect");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to validate effect" });
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("cache/stats")]
    [ProducesResponseType(typeof(CacheStatistics), StatusCodes.Status200OK)]
    public IActionResult GetCacheStats()
    {
        try
        {
            var stats = _cacheService.GetStatistics();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to get cache statistics" });
        }
    }

    /// <summary>
    /// Clear effect cache
    /// </summary>
    [HttpDelete("cache")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCache(CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.ClearCacheAsync(cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "Failed to clear cache" });
        }
    }
}

// Request/Response DTOs
public record ApplyEffectsRequest
{
    public required string InputPath { get; init; }
    public string? OutputPath { get; init; }
    public required List<VideoEffect> Effects { get; init; }
    public bool UseCache { get; init; } = true;
}

public record ApplyPresetRequest
{
    public required string InputPath { get; init; }
    public string? OutputPath { get; init; }
    public required string PresetId { get; init; }
}

public record ApplyEffectsResponse
{
    public required string OutputPath { get; init; }
    public bool Success { get; init; }
    public bool FromCache { get; init; }
}

public record EffectPreviewRequest
{
    public required string InputPath { get; init; }
    public required VideoEffect Effect { get; init; }
    public double PreviewDurationSeconds { get; init; } = 5.0;
}

public record EffectPreviewResponse
{
    public required string PreviewPath { get; init; }
    public bool Success { get; init; }
}

public record ValidationResponse
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
}
