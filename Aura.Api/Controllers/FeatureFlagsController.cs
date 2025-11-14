using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Aura.Core.Services.FeatureFlags;
using System.ComponentModel.DataAnnotations;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing feature flags
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FeatureFlagsController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ILogger<FeatureFlagsController> _logger;
    
    public FeatureFlagsController(
        IFeatureFlagService featureFlagService,
        ILogger<FeatureFlagsController> logger)
    {
        _featureFlagService = featureFlagService;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all feature flags
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var flags = await _featureFlagService.GetAllFlagsAsync().ConfigureAwait(false);
        return Ok(flags);
    }
    
    /// <summary>
    /// Check if a feature is enabled
    /// </summary>
    [HttpGet("{featureName}/enabled")]
    public async Task<IActionResult> IsEnabled(string featureName)
    {
        var enabled = await _featureFlagService.IsEnabledAsync(featureName).ConfigureAwait(false);
        return Ok(new { featureName, enabled });
    }
    
    /// <summary>
    /// Check if a feature is enabled for a specific user
    /// </summary>
    [HttpGet("{featureName}/enabled/{userId}")]
    public async Task<IActionResult> IsEnabledForUser(string featureName, string userId)
    {
        var enabled = await _featureFlagService.IsEnabledForUserAsync(featureName, userId).ConfigureAwait(false);
        return Ok(new { featureName, userId, enabled });
    }
    
    /// <summary>
    /// Enable a feature flag (admin only)
    /// </summary>
    [HttpPost("{featureName}/enable")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Enable(string featureName)
    {
        await _featureFlagService.EnableFeatureAsync(featureName).ConfigureAwait(false);
        _logger.LogInformation("Feature {FeatureName} enabled by {User}", 
            featureName, User.Identity?.Name);
        return Ok();
    }
    
    /// <summary>
    /// Disable a feature flag (admin only)
    /// </summary>
    [HttpPost("{featureName}/disable")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Disable(string featureName)
    {
        await _featureFlagService.DisableFeatureAsync(featureName).ConfigureAwait(false);
        _logger.LogInformation("Feature {FeatureName} disabled by {User}", 
            featureName, User.Identity?.Name);
        return Ok();
    }
    
    /// <summary>
    /// Set rollout percentage (admin only)
    /// </summary>
    [HttpPost("{featureName}/rollout")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetRollout(
        string featureName, 
        [FromBody] SetRolloutRequest request)
    {
        await _featureFlagService.SetRolloutPercentageAsync(featureName, request.Percentage).ConfigureAwait(false);
        _logger.LogInformation("Feature {FeatureName} rollout set to {Percentage}% by {User}", 
            featureName, request.Percentage, User.Identity?.Name);
        return Ok();
    }
    
    /// <summary>
    /// Add user to allowlist (admin only)
    /// </summary>
    [HttpPost("{featureName}/allowlist")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddToAllowlist(
        string featureName, 
        [FromBody] AllowlistRequest request)
    {
        await _featureFlagService.AddUserToAllowlistAsync(featureName, request.UserId).ConfigureAwait(false);
        return Ok();
    }
    
    /// <summary>
    /// Remove user from allowlist (admin only)
    /// </summary>
    [HttpDelete("{featureName}/allowlist/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveFromAllowlist(string featureName, string userId)
    {
        await _featureFlagService.RemoveUserFromAllowlistAsync(featureName, userId).ConfigureAwait(false);
        return Ok();
    }
}

public class SetRolloutRequest
{
    [Range(0, 100)]
    public int Percentage { get; set; }
}

public class AllowlistRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
}
