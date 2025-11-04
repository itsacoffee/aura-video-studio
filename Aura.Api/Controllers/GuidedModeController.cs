using System;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for guided mode configuration and telemetry
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GuidedModeController : ControllerBase
{
    private readonly ILogger<GuidedModeController> _logger;

    public GuidedModeController(ILogger<GuidedModeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get current guided mode configuration
    /// </summary>
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        try
        {
            _logger.LogInformation("Getting guided mode configuration");

            var config = new GuidedModeConfigDto(
                Enabled: true,
                ExperienceLevel: "beginner",
                ShowTooltips: true,
                ShowWhyLinks: true,
                RequirePromptDiffConfirmation: true);

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting guided mode config");
            return StatusCode(500, new { error = "Failed to get configuration" });
        }
    }

    /// <summary>
    /// Update guided mode configuration
    /// </summary>
    [HttpPost("config")]
    public IActionResult UpdateConfig([FromBody] GuidedModeConfigDto config)
    {
        try
        {
            _logger.LogInformation(
                "Updating guided mode config: Enabled={Enabled}, Level={Level}",
                config.Enabled,
                config.ExperienceLevel);

            return Ok(new { success = true, config });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating guided mode config");
            return StatusCode(500, new { error = "Failed to update configuration" });
        }
    }

    /// <summary>
    /// Track guided mode feature usage telemetry
    /// </summary>
    [HttpPost("telemetry")]
    public IActionResult TrackTelemetry([FromBody] GuidedModeTelemetryDto telemetry)
    {
        try
        {
            _logger.LogInformation(
                "Guided mode telemetry: Feature={Feature}, Type={Type}, Duration={Duration}ms, Success={Success}",
                telemetry.FeatureUsed,
                telemetry.ArtifactType,
                telemetry.DurationMs,
                telemetry.Success);

            if (telemetry.FeedbackRating != null)
            {
                _logger.LogInformation(
                    "User feedback for {Feature}: {Rating}",
                    telemetry.FeatureUsed,
                    telemetry.FeedbackRating);
            }

            return Ok(new { success = true, message = "Telemetry recorded" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking guided mode telemetry");
            return StatusCode(500, new { error = "Failed to track telemetry" });
        }
    }

    /// <summary>
    /// Get default configuration for experience level
    /// </summary>
    [HttpGet("defaults/{experienceLevel}")]
    public IActionResult GetDefaults(string experienceLevel)
    {
        try
        {
            _logger.LogInformation("Getting defaults for experience level: {Level}", experienceLevel);

            var config = experienceLevel.ToLowerInvariant() switch
            {
                "beginner" => new GuidedModeConfigDto(
                    Enabled: true,
                    ExperienceLevel: "beginner",
                    ShowTooltips: true,
                    ShowWhyLinks: true,
                    RequirePromptDiffConfirmation: true),
                "intermediate" => new GuidedModeConfigDto(
                    Enabled: true,
                    ExperienceLevel: "intermediate",
                    ShowTooltips: false,
                    ShowWhyLinks: true,
                    RequirePromptDiffConfirmation: true),
                "advanced" => new GuidedModeConfigDto(
                    Enabled: false,
                    ExperienceLevel: "advanced",
                    ShowTooltips: false,
                    ShowWhyLinks: false,
                    RequirePromptDiffConfirmation: false),
                _ => new GuidedModeConfigDto(
                    Enabled: true,
                    ExperienceLevel: "beginner",
                    ShowTooltips: true,
                    ShowWhyLinks: true,
                    RequirePromptDiffConfirmation: true)
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting defaults");
            return StatusCode(500, new { error = "Failed to get defaults" });
        }
    }
}
