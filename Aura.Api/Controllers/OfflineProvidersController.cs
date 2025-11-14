using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing offline provider detection and availability
/// </summary>
[ApiController]
[Route("api/offline-providers")]
public class OfflineProvidersController : ControllerBase
{
    private readonly ILogger<OfflineProvidersController> _logger;
    private readonly OfflineProviderAvailabilityService _availabilityService;

    public OfflineProvidersController(
        ILogger<OfflineProvidersController> logger,
        OfflineProviderAvailabilityService availabilityService)
    {
        _logger = logger;
        _availabilityService = availabilityService;
    }

    /// <summary>
    /// Check availability of all offline providers
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Checking offline providers status, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            var status = await _availabilityService.CheckAllProvidersAsync(ct).ConfigureAwait(false);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking offline providers status, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return Problem("Failed to check offline providers status", statusCode: 500);
        }
    }

    /// <summary>
    /// Check if Piper TTS is available
    /// </summary>
    [HttpGet("piper")]
    public async Task<IActionResult> CheckPiper(CancellationToken ct)
    {
        try
        {
            var status = await _availabilityService.CheckPiperAsync(ct).ConfigureAwait(false);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Piper TTS status");
            return Problem("Failed to check Piper TTS status", statusCode: 500);
        }
    }

    /// <summary>
    /// Check if Mimic3 TTS is available
    /// </summary>
    [HttpGet("mimic3")]
    public async Task<IActionResult> CheckMimic3(CancellationToken ct)
    {
        try
        {
            var status = await _availabilityService.CheckMimic3Async(ct).ConfigureAwait(false);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Mimic3 TTS status");
            return Problem("Failed to check Mimic3 TTS status", statusCode: 500);
        }
    }

    /// <summary>
    /// Check if Ollama is available and get model recommendations
    /// </summary>
    [HttpGet("ollama")]
    public async Task<IActionResult> CheckOllama(CancellationToken ct)
    {
        try
        {
            var status = await _availabilityService.CheckOllamaAsync(ct).ConfigureAwait(false);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Ollama status");
            return Problem("Failed to check Ollama status", statusCode: 500);
        }
    }

    /// <summary>
    /// Check if Stable Diffusion WebUI is available
    /// </summary>
    [HttpGet("stable-diffusion")]
    public async Task<IActionResult> CheckStableDiffusion(CancellationToken ct)
    {
        try
        {
            var status = await _availabilityService.CheckStableDiffusionAsync(ct).ConfigureAwait(false);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Stable Diffusion status");
            return Problem("Failed to check Stable Diffusion status", statusCode: 500);
        }
    }

    /// <summary>
    /// Check if Windows TTS is available
    /// </summary>
    [HttpGet("windows-tts")]
    public async Task<IActionResult> CheckWindowsTts(CancellationToken ct)
    {
        try
        {
            var status = await _availabilityService.CheckWindowsTtsAsync(ct).ConfigureAwait(false);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Windows TTS status");
            return Problem("Failed to check Windows TTS status", statusCode: 500);
        }
    }

    /// <summary>
    /// Get machine-specific recommendations for offline providers based on hardware capabilities
    /// </summary>
    [HttpGet("recommendations")]
    public async Task<IActionResult> GetMachineRecommendations(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting machine-specific recommendations for offline providers, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            var recommendations = await _availabilityService.GetMachineRecommendationsAsync(ct).ConfigureAwait(false);

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting machine recommendations, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return Problem("Failed to get machine recommendations", statusCode: 500);
        }
    }
}
