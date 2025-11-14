using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Configuration;
using Aura.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing provider configuration profiles
/// </summary>
[ApiController]
[Route("api/provider-profiles")]
public class ProviderProfilesController : ControllerBase
{
    private readonly ILogger<ProviderProfilesController> _logger;
    private readonly ProviderProfileService _profileService;
    private readonly PreflightValidationService _validationService;
    private readonly IKeyStore _keyStore;

    public ProviderProfilesController(
        ILogger<ProviderProfilesController> logger,
        ProviderProfileService profileService,
        PreflightValidationService validationService,
        IKeyStore keyStore)
    {
        _logger = logger;
        _profileService = profileService;
        _validationService = validationService;
        _keyStore = keyStore;
    }

    /// <summary>
    /// Get all available provider profiles
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProfiles(CancellationToken ct)
    {
        try
        {
            var profiles = await _profileService.GetAllProfilesAsync(ct).ConfigureAwait(false);
            var dtos = profiles.Select(MapToDto).ToList();

            return Ok(new { profiles = dtos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider profiles");
            return Problem("Failed to get provider profiles", statusCode: 500);
        }
    }

    /// <summary>
    /// Get the currently active profile
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveProfile(CancellationToken ct)
    {
        try
        {
            var profile = await _profileService.GetActiveProfileAsync(ct).ConfigureAwait(false);
            var dto = MapToDto(profile);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active profile");
            return Problem("Failed to get active profile", statusCode: 500);
        }
    }

    /// <summary>
    /// Set the active provider profile
    /// </summary>
    [HttpPost("active")]
    public async Task<IActionResult> SetActiveProfile(
        [FromBody] SetActiveProfileRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProfileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var success = await _profileService.SetActiveProfileAsync(request.ProfileId, ct).ConfigureAwait(false);

            if (!success)
            {
                return NotFound(new { error = $"Profile {request.ProfileId} not found" });
            }

            var profile = await _profileService.GetActiveProfileAsync(ct).ConfigureAwait(false);
            
            _logger.LogInformation(
                "Active profile changed to {ProfileId}, CorrelationId: {CorrelationId}",
                request.ProfileId, HttpContext.TraceIdentifier);

            return Ok(new
            {
                success = true,
                message = $"Active profile changed to {profile.Name}",
                profile = MapToDto(profile)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active profile");
            return Problem("Failed to set active profile", statusCode: 500);
        }
    }

    /// <summary>
    /// Validate a provider profile
    /// </summary>
    [HttpPost("{profileId}/validate")]
    public async Task<IActionResult> ValidateProfile(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            var result = await _profileService.ValidateProfileAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new ProfileValidationResultDto(
                result.IsValid,
                result.Message,
                result.Errors,
                result.MissingKeys,
                result.Warnings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating profile {ProfileId}", profileId);
            return Problem($"Failed to validate profile {profileId}", statusCode: 500);
        }
    }

    /// <summary>
    /// Get recommended profile based on available API keys
    /// </summary>
    [HttpGet("recommend")]
    public async Task<IActionResult> GetRecommendedProfile(CancellationToken ct)
    {
        try
        {
            var recommendedProfile = await _profileService.GetRecommendedProfileAsync(ct).ConfigureAwait(false);
            var allKeys = _keyStore.GetAllKeys();
            var availableKeys = allKeys.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                                       .Select(kvp => kvp.Key)
                                       .ToList();

            var proMaxKeys = new List<string> { "openai", "elevenlabs", "stabilityai" };
            var missingKeys = proMaxKeys.Where(k => !availableKeys.Contains(k)).ToList();

            var reason = recommendedProfile.Tier switch
            {
                Core.Models.ProfileTier.ProMax => "All premium API keys are configured. Pro-Max profile provides the highest quality.",
                Core.Models.ProfileTier.BalancedMix => "OpenAI key is configured. Balanced Mix profile provides good quality at reasonable cost.",
                _ => "No premium API keys configured. Free-Only profile uses offline providers."
            };

            return Ok(new ProfileRecommendationDto(
                recommendedProfile.Id,
                recommendedProfile.Name,
                reason,
                availableKeys,
                missingKeys));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommended profile");
            return Problem("Failed to get recommended profile", statusCode: 500);
        }
    }

    /// <summary>
    /// Test a provider API key
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestProvider(
        [FromBody] TestProviderRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                return BadRequest(new { error = "Provider name is required" });
            }

            var maskedKey = request.ApiKey != null 
                ? SecretMaskingService.MaskApiKey(request.ApiKey) 
                : "[using stored key]";

            _logger.LogInformation(
                "Testing provider {Provider} with key {MaskedKey}, CorrelationId: {CorrelationId}",
                request.Provider, maskedKey, HttpContext.TraceIdentifier);

            var result = await _validationService.TestProviderAsync(
                request.Provider, 
                request.ApiKey, 
                ct).ConfigureAwait(false);

            return Ok(new ProviderTestResultDto(
                result.Provider,
                result.Success,
                result.Message,
                result.TestedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing provider {Provider}", request.Provider);
            return Problem($"Failed to test provider {request.Provider}", statusCode: 500);
        }
    }

    /// <summary>
    /// Save API keys
    /// </summary>
    [HttpPost("keys")]
    public async Task<IActionResult> SaveApiKeys(
        [FromBody] SaveApiKeysRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.Keys == null || request.Keys.Count == 0)
            {
                return BadRequest(new { error = "At least one API key is required" });
            }

            var maskedKeys = SecretMaskingService.MaskDictionary(request.Keys);
            _logger.LogInformation(
                "Saving {Count} API keys (masked: {MaskedKeys}), CorrelationId: {CorrelationId}",
                request.Keys.Count, 
                System.Text.Json.JsonSerializer.Serialize(maskedKeys),
                HttpContext.TraceIdentifier);

            await _keyStore.SaveKeysAsync(request.Keys, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                message = $"Saved {request.Keys.Count} API key(s)",
                savedKeys = request.Keys.Keys.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving API keys");
            return Problem("Failed to save API keys", statusCode: 500);
        }
    }

    /// <summary>
    /// Get stored API keys (masked for security)
    /// </summary>
    [HttpGet("keys")]
    public IActionResult GetApiKeys()
    {
        try
        {
            var allKeys = _keyStore.GetAllKeys();
            var maskedKeys = SecretMaskingService.MaskDictionary(allKeys);

            return Ok(new { keys = maskedKeys });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API keys");
            return Problem("Failed to get API keys", statusCode: 500);
        }
    }

    private static ProviderProfileDto MapToDto(Core.Models.ProviderProfile profile)
    {
        return new ProviderProfileDto(
            profile.Id,
            profile.Name,
            profile.Description,
            profile.Tier.ToString(),
            profile.Stages,
            profile.RequiredApiKeys,
            profile.UsageNotes,
            profile.LastValidatedAt);
    }
}
