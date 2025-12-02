using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing graphics and visual settings
/// </summary>
[ApiController]
[Route("api/graphics")]
public class GraphicsSettingsController : ControllerBase
{
    private readonly IGraphicsSettingsService _graphicsService;
    private readonly ILogger<GraphicsSettingsController> _logger;

    public GraphicsSettingsController(
        IGraphicsSettingsService graphicsService,
        ILogger<GraphicsSettingsController> logger)
    {
        _graphicsService = graphicsService;
        _logger = logger;
    }

    /// <summary>
    /// Get current graphics settings
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GraphicsSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GraphicsSettings>> GetSettings(CancellationToken ct)
    {
        try
        {
            var settings = await _graphicsService.GetSettingsAsync(ct).ConfigureAwait(false);
            return Ok(settings);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to get graphics settings");
            return StatusCode(500, new { error = "Failed to retrieve graphics settings" });
        }
    }

    /// <summary>
    /// Save graphics settings
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(GraphicsSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GraphicsSettings>> SaveSettings(
        [FromBody] GraphicsSettings settings,
        CancellationToken ct)
    {
        if (settings == null)
        {
            return BadRequest(new { error = "Settings cannot be null" });
        }

        try
        {
            var success = await _graphicsService.SaveSettingsAsync(settings, ct).ConfigureAwait(false);
            if (!success)
            {
                return StatusCode(500, new { error = "Failed to save settings" });
            }

            return Ok(settings);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to save graphics settings");
            return StatusCode(500, new { error = "Failed to save graphics settings" });
        }
    }

    /// <summary>
    /// Apply a performance profile preset
    /// </summary>
    [HttpPost("profile/{profile}")]
    [ProducesResponseType(typeof(GraphicsSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GraphicsSettings>> ApplyProfile(
        PerformanceProfile profile,
        CancellationToken ct)
    {
        try
        {
            var settings = await _graphicsService.ApplyProfileAsync(profile, ct).ConfigureAwait(false);
            return Ok(settings);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to apply performance profile {Profile}", profile);
            return StatusCode(500, new { error = "Failed to apply performance profile" });
        }
    }

    /// <summary>
    /// Detect optimal settings based on hardware
    /// </summary>
    [HttpPost("detect")]
    [ProducesResponseType(typeof(GraphicsSettings), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GraphicsSettings>> DetectOptimal(CancellationToken ct)
    {
        try
        {
            var settings = await _graphicsService.DetectOptimalSettingsAsync(ct).ConfigureAwait(false);
            return Ok(settings);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to detect optimal graphics settings");
            return StatusCode(500, new { error = "Failed to detect optimal settings" });
        }
    }

    /// <summary>
    /// Reset to default settings
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ResetToDefaults(CancellationToken ct)
    {
        try
        {
            var success = await _graphicsService.ResetToDefaultsAsync(ct).ConfigureAwait(false);
            if (!success)
            {
                return StatusCode(500, new { error = "Failed to reset settings" });
            }

            return Ok(new { success = true });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to reset graphics settings");
            return StatusCode(500, new { error = "Failed to reset graphics settings" });
        }
    }
}
