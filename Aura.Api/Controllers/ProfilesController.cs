using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Profiles;
using Aura.Core.Services.Profiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for user profile and preference management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
    private readonly ILogger<ProfilesController> _logger;
    private readonly ProfileService _profileService;

    public ProfilesController(
        ILogger<ProfilesController> logger,
        ProfileService profileService)
    {
        _logger = logger;
        _profileService = profileService;
    }

    /// <summary>
    /// Get all profiles for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserProfiles(
        string userId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new { error = "UserId is required" });
            }

            var profiles = await _profileService.GetUserProfilesAsync(userId, ct);
            
            var summaries = profiles.Select(p => new ProfileSummaryResponse(
                ProfileId: p.ProfileId,
                ProfileName: p.ProfileName,
                Description: p.Description,
                IsDefault: p.IsDefault,
                IsActive: p.IsActive,
                LastUsed: p.LastUsed,
                ContentType: null // Will be loaded separately if needed
            )).ToList();

            return Ok(new
            {
                success = true,
                profiles = summaries,
                count = summaries.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to retrieve profiles" });
        }
    }

    /// <summary>
    /// Create a new profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProfile(
        [FromBody] CreateProfileRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new { error = "UserId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.ProfileName))
            {
                return BadRequest(new { error = "ProfileName is required" });
            }

            var profile = await _profileService.CreateProfileAsync(request, ct);

            return Ok(new
            {
                success = true,
                profile = new
                {
                    profile.ProfileId,
                    profile.ProfileName,
                    profile.Description,
                    profile.IsDefault,
                    profile.IsActive,
                    profile.CreatedAt,
                    profile.LastUsed
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile for user {UserId}", request.UserId);
            return StatusCode(500, new { error = "Failed to create profile" });
        }
    }

    /// <summary>
    /// Get specific profile details
    /// </summary>
    [HttpGet("{profileId}")]
    public async Task<IActionResult> GetProfile(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var profile = await _profileService.GetProfileAsync(profileId, ct);
            if (profile == null)
            {
                return NotFound(new { error = $"Profile {profileId} not found" });
            }

            var preferences = await _profileService.GetPreferencesAsync(profileId, ct);

            return Ok(new
            {
                success = true,
                profile = new
                {
                    profile.ProfileId,
                    profile.ProfileName,
                    profile.Description,
                    profile.IsDefault,
                    profile.IsActive,
                    profile.CreatedAt,
                    profile.LastUsed,
                    profile.UpdatedAt
                },
                preferences = new
                {
                    preferences.ContentType,
                    preferences.Tone,
                    preferences.Visual,
                    preferences.Audio,
                    preferences.Editing,
                    preferences.Platform,
                    preferences.AIBehavior
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve profile" });
        }
    }

    /// <summary>
    /// Update profile metadata
    /// </summary>
    [HttpPut("{profileId}")]
    public async Task<IActionResult> UpdateProfile(
        string profileId,
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var updated = await _profileService.UpdateProfileAsync(profileId, request, ct);

            return Ok(new
            {
                success = true,
                profile = new
                {
                    updated.ProfileId,
                    updated.ProfileName,
                    updated.Description,
                    updated.UpdatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Profile {ProfileId} not found", profileId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to update profile" });
        }
    }

    /// <summary>
    /// Delete a profile
    /// </summary>
    [HttpDelete("{profileId}")]
    public async Task<IActionResult> DeleteProfile(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            await _profileService.DeleteProfileAsync(profileId, ct);

            return Ok(new
            {
                success = true,
                message = $"Profile {profileId} deleted successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete profile {ProfileId}", profileId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to delete profile" });
        }
    }

    /// <summary>
    /// Set a profile as active
    /// </summary>
    [HttpPost("{profileId}/activate")]
    public async Task<IActionResult> ActivateProfile(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var activated = await _profileService.ActivateProfileAsync(profileId, ct);

            return Ok(new
            {
                success = true,
                profile = new
                {
                    activated.ProfileId,
                    activated.ProfileName,
                    activated.IsActive,
                    activated.LastUsed
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot activate profile {ProfileId}", profileId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to activate profile" });
        }
    }

    /// <summary>
    /// Duplicate an existing profile
    /// </summary>
    [HttpPost("{profileId}/duplicate")]
    public async Task<IActionResult> DuplicateProfile(
        string profileId,
        [FromBody] DuplicateProfileRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.NewProfileName))
            {
                return BadRequest(new { error = "NewProfileName is required" });
            }

            var duplicated = await _profileService.DuplicateProfileAsync(
                profileId,
                request.NewProfileName,
                ct);

            return Ok(new
            {
                success = true,
                profile = new
                {
                    duplicated.ProfileId,
                    duplicated.ProfileName,
                    duplicated.Description,
                    duplicated.CreatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot duplicate profile {ProfileId}", profileId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to duplicate profile" });
        }
    }

    /// <summary>
    /// Get available profile templates
    /// </summary>
    [HttpGet("templates")]
    public IActionResult GetTemplates()
    {
        try
        {
            var templates = ProfileTemplateService.GetAllTemplates();

            return Ok(new
            {
                success = true,
                templates = templates.Select(t => new
                {
                    t.TemplateId,
                    t.Name,
                    t.Description,
                    t.Category
                }).ToList(),
                count = templates.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates");
            return StatusCode(500, new { error = "Failed to retrieve templates" });
        }
    }

    /// <summary>
    /// Create profile from template
    /// </summary>
    [HttpPost("from-template")]
    public async Task<IActionResult> CreateFromTemplate(
        [FromBody] CreateProfileRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new { error = "UserId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.ProfileName))
            {
                return BadRequest(new { error = "ProfileName is required" });
            }

            if (string.IsNullOrWhiteSpace(request.FromTemplateId))
            {
                return BadRequest(new { error = "FromTemplateId is required" });
            }

            var template = ProfileTemplateService.GetTemplate(request.FromTemplateId);
            if (template == null)
            {
                return NotFound(new { error = $"Template {request.FromTemplateId} not found" });
            }

            var profile = await _profileService.CreateProfileAsync(request, ct);

            return Ok(new
            {
                success = true,
                profile = new
                {
                    profile.ProfileId,
                    profile.ProfileName,
                    profile.Description,
                    profile.IsDefault,
                    profile.IsActive,
                    profile.CreatedAt
                },
                templateUsed = template.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile from template");
            return StatusCode(500, new { error = "Failed to create profile from template" });
        }
    }

    /// <summary>
    /// Update profile preferences
    /// </summary>
    [HttpPut("{profileId}/preferences")]
    public async Task<IActionResult> UpdatePreferences(
        string profileId,
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var updated = await _profileService.UpdatePreferencesAsync(profileId, request, ct);

            return Ok(new
            {
                success = true,
                preferences = new
                {
                    updated.ProfileId,
                    updated.ContentType,
                    updated.Tone,
                    updated.Visual,
                    updated.Audio,
                    updated.Editing,
                    updated.Platform,
                    updated.AIBehavior
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update preferences for profile {ProfileId}", profileId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to update preferences" });
        }
    }

    /// <summary>
    /// Record a user decision
    /// </summary>
    [HttpPost("{profileId}/decisions/record")]
    public async Task<IActionResult> RecordDecision(
        string profileId,
        [FromBody] RecordDecisionDto request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.SuggestionType))
            {
                return BadRequest(new { error = "SuggestionType is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Decision))
            {
                return BadRequest(new { error = "Decision is required" });
            }

            // Create the actual request with profileId
            var actualRequest = new Aura.Core.Models.Profiles.RecordDecisionRequest(
                ProfileId: profileId,
                SuggestionType: request.SuggestionType,
                Decision: request.Decision,
                Context: request.Context
            );
            await _profileService.RecordDecisionAsync(actualRequest, ct);

            return Ok(new
            {
                success = true,
                message = "Decision recorded successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording decision for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to record decision" });
        }
    }

    /// <summary>
    /// Get preference summary for a profile
    /// </summary>
    [HttpGet("{profileId}/preferences/summary")]
    public async Task<IActionResult> GetPreferencesSummary(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var preferences = await _profileService.GetPreferencesAsync(profileId, ct);
            var decisions = await _profileService.GetDecisionHistoryAsync(profileId, ct);

            // Calculate decision statistics
            var decisionStats = decisions
                .GroupBy(d => d.SuggestionType)
                .Select(g => new
                {
                    suggestionType = g.Key,
                    total = g.Count(),
                    accepted = g.Count(d => d.Decision == "accepted"),
                    rejected = g.Count(d => d.Decision == "rejected"),
                    modified = g.Count(d => d.Decision == "modified")
                })
                .ToList();

            return Ok(new
            {
                success = true,
                summary = new
                {
                    profileId,
                    preferences = new
                    {
                        preferences.ContentType,
                        tone = new
                        {
                            preferences.Tone?.Formality,
                            preferences.Tone?.Energy,
                            preferences.Tone?.PersonalityTraits
                        },
                        visual = new
                        {
                            preferences.Visual?.Aesthetic,
                            preferences.Visual?.ColorPalette
                        },
                        audio = new
                        {
                            preferences.Audio?.MusicGenres,
                            preferences.Audio?.MusicEnergy
                        },
                        editing = new
                        {
                            preferences.Editing?.Pacing,
                            preferences.Editing?.CutFrequency
                        },
                        platform = new
                        {
                            preferences.Platform?.PrimaryPlatform,
                            preferences.Platform?.AspectRatio
                        },
                        aiBehavior = new
                        {
                            preferences.AIBehavior?.AssistanceLevel,
                            preferences.AIBehavior?.CreativityLevel
                        }
                    },
                    decisionHistory = new
                    {
                        totalDecisions = decisions.Count,
                        byType = decisionStats
                    }
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot get preferences summary for profile {ProfileId}", profileId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences summary for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to get preferences summary" });
        }
    }
}

/// <summary>
/// Request to duplicate a profile
/// </summary>
public record DuplicateProfileRequest(string NewProfileName);

/// <summary>
/// DTO for recording a decision (without ProfileId since it comes from route)
/// </summary>
public record RecordDecisionDto(
    string SuggestionType,
    string Decision,
    Dictionary<string, object>? Context
);
