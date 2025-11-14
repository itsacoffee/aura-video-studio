using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Services.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for audio waveform generation
/// </summary>
[ApiController]
[Route("api/waveform")]
public class WaveformController : ControllerBase
{
    private readonly WaveformGenerator _waveformGenerator;
    private readonly ILogger<WaveformController> _logger;

    public WaveformController(
        WaveformGenerator waveformGenerator,
        ILogger<WaveformController> logger)
    {
        _waveformGenerator = waveformGenerator ?? throw new ArgumentNullException(nameof(waveformGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate waveform data for audio file
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(WaveformDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateWaveform(
        [FromBody] GenerateWaveformRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating waveform for {AudioPath}", request.AudioPath);

        try
        {
            float[] data;
            
            if (request.StartTime > 0 || request.EndTime > 0)
            {
                data = await _waveformGenerator.GenerateWaveformDataAsyncWithPriority(
                    request.AudioPath,
                    request.TargetSamples,
                    request.StartTime,
                    request.EndTime,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                data = await _waveformGenerator.GenerateWaveformDataAsync(
                    request.AudioPath,
                    request.TargetSamples,
                    cancellationToken).ConfigureAwait(false);
            }

            var response = new WaveformDataResponse(
                data,
                44100,
                0);

            return Ok(response);
        }
        catch (System.IO.FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Audio file not found: {AudioPath}", request.AudioPath);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating waveform for {AudioPath}", request.AudioPath);
            return StatusCode(500, new { error = "Failed to generate waveform", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate waveform image for audio file
    /// </summary>
    [HttpPost("image")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateWaveformImage(
        [FromQuery] string audioPath,
        [FromQuery] int width = 800,
        [FromQuery] int height = 100,
        [FromQuery] string trackType = "narration",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var imagePath = await _waveformGenerator.GenerateWaveformAsync(
                audioPath,
                width,
                height,
                trackType,
                cancellationToken).ConfigureAwait(false);

            var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath, cancellationToken).ConfigureAwait(false);
            return File(imageBytes, "image/png");
        }
        catch (System.IO.FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Audio file not found: {AudioPath}", audioPath);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating waveform image for {AudioPath}", audioPath);
            return StatusCode(500, new { error = "Failed to generate waveform image" });
        }
    }

    /// <summary>
    /// Clear waveform cache
    /// </summary>
    [HttpPost("clear-cache")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCache()
    {
        try
        {
            _logger.LogInformation("Clearing waveform cache");
            _waveformGenerator.ClearCache();
            await _waveformGenerator.ClearPersistentCacheAsync().ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing waveform cache");
            return StatusCode(500, new { error = "Failed to clear cache" });
        }
    }
}
