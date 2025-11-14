using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models.UserPreferences;
using Aura.Core.Services.UserPreferences;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoints for managing user preferences and customization
/// </summary>
[ApiController]
[Route("api/user-preferences")]
public class UserPreferencesController : ControllerBase
{
    private readonly ILogger<UserPreferencesController> _logger;
    private readonly UserPreferencesService _preferencesService;

    public UserPreferencesController(
        ILogger<UserPreferencesController> logger,
        UserPreferencesService preferencesService)
    {
        _logger = logger;
        _preferencesService = preferencesService;
    }

    // Custom Audience Profiles

    /// <summary>
    /// Get all custom audience profiles
    /// </summary>
    [HttpGet("audience-profiles")]
    [ProducesResponseType(typeof(List<CustomAudienceProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CustomAudienceProfileDto>>> GetCustomAudienceProfiles(CancellationToken ct)
    {
        _logger.LogInformation("Getting custom audience profiles");
        var profiles = await _preferencesService.GetCustomAudienceProfilesAsync(ct).ConfigureAwait(false);
        var dtos = profiles.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Get a custom audience profile by ID
    /// </summary>
    [HttpGet("audience-profiles/{id}")]
    [ProducesResponseType(typeof(CustomAudienceProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomAudienceProfileDto>> GetCustomAudienceProfile(string id, CancellationToken ct)
    {
        _logger.LogInformation("Getting custom audience profile {ProfileId}", id);
        var profile = await _preferencesService.GetCustomAudienceProfileAsync(id, ct).ConfigureAwait(false);
        
        if (profile == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Custom audience profile {id} does not exist"
            });
        }

        return Ok(MapToDto(profile));
    }

    /// <summary>
    /// Create a new custom audience profile
    /// </summary>
    [HttpPost("audience-profiles")]
    [ProducesResponseType(typeof(CustomAudienceProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomAudienceProfileDto>> CreateCustomAudienceProfile(
        [FromBody] CustomAudienceProfileDto dto,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating custom audience profile: {ProfileName}", dto.Name);
        
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Profile",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Profile name is required"
            });
        }

        var profile = MapFromDto(dto);
        profile.Id = Guid.NewGuid().ToString();
        profile.CreatedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        var saved = await _preferencesService.SaveCustomAudienceProfileAsync(profile, ct).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetCustomAudienceProfile), new { id = saved.Id }, MapToDto(saved));
    }

    /// <summary>
    /// Update a custom audience profile
    /// </summary>
    [HttpPut("audience-profiles/{id}")]
    [ProducesResponseType(typeof(CustomAudienceProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomAudienceProfileDto>> UpdateCustomAudienceProfile(
        string id,
        [FromBody] CustomAudienceProfileDto dto,
        CancellationToken ct)
    {
        _logger.LogInformation("Updating custom audience profile {ProfileId}", id);
        
        var existing = await _preferencesService.GetCustomAudienceProfileAsync(id, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Custom audience profile {id} does not exist"
            });
        }

        var profile = MapFromDto(dto);
        profile.Id = id;
        profile.CreatedAt = existing.CreatedAt;

        var saved = await _preferencesService.SaveCustomAudienceProfileAsync(profile, ct).ConfigureAwait(false);
        return Ok(MapToDto(saved));
    }

    /// <summary>
    /// Delete a custom audience profile
    /// </summary>
    [HttpDelete("audience-profiles/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomAudienceProfile(string id, CancellationToken ct)
    {
        _logger.LogInformation("Deleting custom audience profile {ProfileId}", id);
        
        var deleted = await _preferencesService.DeleteCustomAudienceProfileAsync(id, ct).ConfigureAwait(false);
        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Custom audience profile {id} does not exist"
            });
        }

        return NoContent();
    }

    // Content Filtering Policies

    /// <summary>
    /// Get all content filtering policies
    /// </summary>
    [HttpGet("filtering-policies")]
    [ProducesResponseType(typeof(List<ContentFilteringPolicyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ContentFilteringPolicyDto>>> GetContentFilteringPolicies(CancellationToken ct)
    {
        _logger.LogInformation("Getting content filtering policies");
        var policies = await _preferencesService.GetContentFilteringPoliciesAsync(ct).ConfigureAwait(false);
        var dtos = policies.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Get a content filtering policy by ID
    /// </summary>
    [HttpGet("filtering-policies/{id}")]
    [ProducesResponseType(typeof(ContentFilteringPolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentFilteringPolicyDto>> GetContentFilteringPolicy(string id, CancellationToken ct)
    {
        _logger.LogInformation("Getting content filtering policy {PolicyId}", id);
        var policy = await _preferencesService.GetContentFilteringPolicyAsync(id, ct).ConfigureAwait(false);
        
        if (policy == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Policy Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Content filtering policy {id} does not exist"
            });
        }

        return Ok(MapToDto(policy));
    }

    /// <summary>
    /// Create a new content filtering policy
    /// </summary>
    [HttpPost("filtering-policies")]
    [ProducesResponseType(typeof(ContentFilteringPolicyDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ContentFilteringPolicyDto>> CreateContentFilteringPolicy(
        [FromBody] ContentFilteringPolicyDto dto,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating content filtering policy: {PolicyName}", dto.Name);
        
        var policy = MapFromDto(dto);
        policy.Id = Guid.NewGuid().ToString();
        policy.CreatedAt = DateTime.UtcNow;
        policy.UpdatedAt = DateTime.UtcNow;

        var saved = await _preferencesService.SaveContentFilteringPolicyAsync(policy, ct).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetContentFilteringPolicy), new { id = saved.Id }, MapToDto(saved));
    }

    /// <summary>
    /// Update a content filtering policy
    /// </summary>
    [HttpPut("filtering-policies/{id}")]
    [ProducesResponseType(typeof(ContentFilteringPolicyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentFilteringPolicyDto>> UpdateContentFilteringPolicy(
        string id,
        [FromBody] ContentFilteringPolicyDto dto,
        CancellationToken ct)
    {
        _logger.LogInformation("Updating content filtering policy {PolicyId}", id);
        
        var existing = await _preferencesService.GetContentFilteringPolicyAsync(id, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Policy Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Content filtering policy {id} does not exist"
            });
        }

        var policy = MapFromDto(dto);
        policy.Id = id;
        policy.CreatedAt = existing.CreatedAt;

        var saved = await _preferencesService.SaveContentFilteringPolicyAsync(policy, ct).ConfigureAwait(false);
        return Ok(MapToDto(saved));
    }

    /// <summary>
    /// Delete a content filtering policy
    /// </summary>
    [HttpDelete("filtering-policies/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContentFilteringPolicy(string id, CancellationToken ct)
    {
        _logger.LogInformation("Deleting content filtering policy {PolicyId}", id);
        
        var deleted = await _preferencesService.DeleteContentFilteringPolicyAsync(id, ct).ConfigureAwait(false);
        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Policy Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Content filtering policy {id} does not exist"
            });
        }

        return NoContent();
    }

    // AI Behavior Settings

    /// <summary>
    /// Get all AI behavior settings
    /// </summary>
    [HttpGet("ai-behavior")]
    [ProducesResponseType(typeof(List<AIBehaviorSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AIBehaviorSettingsDto>>> GetAIBehaviorSettings(CancellationToken ct)
    {
        _logger.LogInformation("Getting AI behavior settings");
        var settings = await _preferencesService.GetAIBehaviorSettingsAsync(ct).ConfigureAwait(false);
        var dtos = settings.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Get an AI behavior setting by ID
    /// </summary>
    [HttpGet("ai-behavior/{id}")]
    [ProducesResponseType(typeof(AIBehaviorSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AIBehaviorSettingsDto>> GetAIBehaviorSetting(string id, CancellationToken ct)
    {
        _logger.LogInformation("Getting AI behavior setting {SettingId}", id);
        var setting = await _preferencesService.GetAIBehaviorSettingAsync(id, ct).ConfigureAwait(false);
        
        if (setting == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Setting Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"AI behavior setting {id} does not exist"
            });
        }

        return Ok(MapToDto(setting));
    }

    /// <summary>
    /// Create a new AI behavior setting
    /// </summary>
    [HttpPost("ai-behavior")]
    [ProducesResponseType(typeof(AIBehaviorSettingsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AIBehaviorSettingsDto>> CreateAIBehaviorSetting(
        [FromBody] AIBehaviorSettingsDto dto,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating AI behavior setting: {SettingName}", dto.Name);
        
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Setting",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Setting name is required"
            });
        }

        var setting = MapFromDto(dto);
        setting.Id = Guid.NewGuid().ToString();
        setting.CreatedAt = DateTime.UtcNow;
        setting.UpdatedAt = DateTime.UtcNow;

        var saved = await _preferencesService.SaveAIBehaviorSettingsAsync(setting, ct).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetAIBehaviorSetting), new { id = saved.Id }, MapToDto(saved));
    }

    /// <summary>
    /// Update an AI behavior setting
    /// </summary>
    [HttpPut("ai-behavior/{id}")]
    [ProducesResponseType(typeof(AIBehaviorSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AIBehaviorSettingsDto>> UpdateAIBehaviorSetting(
        string id,
        [FromBody] AIBehaviorSettingsDto dto,
        CancellationToken ct)
    {
        _logger.LogInformation("Updating AI behavior setting {SettingId}", id);
        
        var existing = await _preferencesService.GetAIBehaviorSettingAsync(id, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Setting Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"AI behavior setting {id} does not exist"
            });
        }

        var setting = MapFromDto(dto);
        setting.Id = id;
        setting.CreatedAt = existing.CreatedAt;

        var saved = await _preferencesService.SaveAIBehaviorSettingsAsync(setting, ct).ConfigureAwait(false);
        return Ok(MapToDto(saved));
    }

    /// <summary>
    /// Delete an AI behavior setting
    /// </summary>
    [HttpDelete("ai-behavior/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAIBehaviorSetting(string id, CancellationToken ct)
    {
        _logger.LogInformation("Deleting AI behavior setting {SettingId}", id);
        
        var deleted = await _preferencesService.DeleteAIBehaviorSettingsAsync(id, ct).ConfigureAwait(false);
        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Setting Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"AI behavior setting {id} does not exist"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Reset AI behavior setting to defaults
    /// </summary>
    [HttpPost("ai-behavior/{id}/reset")]
    [ProducesResponseType(typeof(AIBehaviorSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AIBehaviorSettingsDto>> ResetAIBehaviorSetting(string id, CancellationToken ct)
    {
        _logger.LogInformation("Resetting AI behavior setting {SettingId} to defaults", id);
        
        var existing = await _preferencesService.GetAIBehaviorSettingAsync(id, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Setting Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"AI behavior setting {id} does not exist"
            });
        }

        var defaultSettings = new AIBehaviorSettings
        {
            Id = id,
            Name = existing.Name,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            Description = existing.Description,
            IsDefault = existing.IsDefault
        };

        var saved = await _preferencesService.SaveAIBehaviorSettingsAsync(defaultSettings, ct).ConfigureAwait(false);
        return Ok(MapToDto(saved));
    }

    /// <summary>
    /// Get or create default AI behavior settings
    /// </summary>
    [HttpGet("ai-behavior/default")]
    [ProducesResponseType(typeof(AIBehaviorSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AIBehaviorSettingsDto>> GetOrCreateDefaultAIBehaviorSettings(CancellationToken ct)
    {
        _logger.LogInformation("Getting or creating default AI behavior settings");
        
        var defaultSettings = await _preferencesService.EnsureDefaultAIBehaviorSettingsAsync(ct).ConfigureAwait(false);
        return Ok(MapToDto(defaultSettings));
    }

    // Export/Import

    /// <summary>
    /// Export all user preferences
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(ExportPreferencesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExportPreferencesResponse>> ExportPreferences(CancellationToken ct)
    {
        _logger.LogInformation("Exporting user preferences");
        
        var json = await _preferencesService.ExportAllPreferencesAsync(ct).ConfigureAwait(false);
        var response = new ExportPreferencesResponse(json, DateTime.UtcNow, "1.0");
        
        return Ok(response);
    }

    /// <summary>
    /// Import user preferences
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportPreferences([FromBody] ImportPreferencesRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Importing user preferences");
        
        if (string.IsNullOrWhiteSpace(request.JsonData))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Import Data",
                Status = StatusCodes.Status400BadRequest,
                Detail = "JSON data is required for import"
            });
        }

        try
        {
            await _preferencesService.ImportPreferencesAsync(request.JsonData, ct).ConfigureAwait(false);
            return Ok(new { success = true, message = "Preferences imported successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing preferences");
            return BadRequest(new ProblemDetails
            {
                Title = "Import Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"Failed to import preferences: {ex.Message}"
            });
        }
    }

    // Mapping Methods

    private static CustomAudienceProfileDto MapToDto(CustomAudienceProfile profile)
    {
        return new CustomAudienceProfileDto(
            profile.Id,
            profile.Name,
            profile.BaseProfileId,
            profile.CreatedAt,
            profile.UpdatedAt,
            profile.IsCustom,
            profile.MinAge,
            profile.MaxAge,
            profile.EducationLevel,
            profile.EducationLevelDescription,
            profile.CulturalSensitivities,
            profile.TopicsToAvoid,
            profile.TopicsToEmphasize,
            profile.VocabularyLevel,
            profile.SentenceStructurePreference,
            profile.ReadingLevel,
            profile.ViolenceThreshold,
            profile.ProfanityThreshold,
            profile.SexualContentThreshold,
            profile.ControversialTopicsThreshold,
            profile.HumorStyle,
            profile.SarcasmLevel,
            profile.JokeTypes,
            profile.CulturalHumorPreferences,
            profile.FormalityLevel,
            profile.AttentionSpanSeconds,
            profile.PacingPreference,
            profile.InformationDensity,
            profile.TechnicalDepthTolerance,
            profile.JargonAcceptability,
            profile.FamiliarTechnicalTerms,
            profile.EmotionalTone,
            profile.EmotionalIntensity,
            profile.CtaAggressiveness,
            profile.CtaStyle,
            profile.BrandVoiceGuidelines,
            profile.BrandToneKeywords,
            profile.BrandPersonality,
            profile.Description,
            profile.Tags,
            profile.IsFavorite,
            profile.UsageCount,
            profile.LastUsedAt);
    }

    private static CustomAudienceProfile MapFromDto(CustomAudienceProfileDto dto)
    {
        return new CustomAudienceProfile
        {
            Id = dto.Id,
            Name = dto.Name,
            BaseProfileId = dto.BaseProfileId,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            IsCustom = dto.IsCustom,
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            EducationLevel = dto.EducationLevel,
            EducationLevelDescription = dto.EducationLevelDescription,
            CulturalSensitivities = dto.CulturalSensitivities,
            TopicsToAvoid = dto.TopicsToAvoid,
            TopicsToEmphasize = dto.TopicsToEmphasize,
            VocabularyLevel = dto.VocabularyLevel,
            SentenceStructurePreference = dto.SentenceStructurePreference,
            ReadingLevel = dto.ReadingLevel,
            ViolenceThreshold = dto.ViolenceThreshold,
            ProfanityThreshold = dto.ProfanityThreshold,
            SexualContentThreshold = dto.SexualContentThreshold,
            ControversialTopicsThreshold = dto.ControversialTopicsThreshold,
            HumorStyle = dto.HumorStyle,
            SarcasmLevel = dto.SarcasmLevel,
            JokeTypes = dto.JokeTypes,
            CulturalHumorPreferences = dto.CulturalHumorPreferences,
            FormalityLevel = dto.FormalityLevel,
            AttentionSpanSeconds = dto.AttentionSpanSeconds,
            PacingPreference = dto.PacingPreference,
            InformationDensity = dto.InformationDensity,
            TechnicalDepthTolerance = dto.TechnicalDepthTolerance,
            JargonAcceptability = dto.JargonAcceptability,
            FamiliarTechnicalTerms = dto.FamiliarTechnicalTerms,
            EmotionalTone = dto.EmotionalTone,
            EmotionalIntensity = dto.EmotionalIntensity,
            CtaAggressiveness = dto.CtaAggressiveness,
            CtaStyle = dto.CtaStyle,
            BrandVoiceGuidelines = dto.BrandVoiceGuidelines,
            BrandToneKeywords = dto.BrandToneKeywords,
            BrandPersonality = dto.BrandPersonality,
            Description = dto.Description,
            Tags = dto.Tags,
            IsFavorite = dto.IsFavorite,
            UsageCount = dto.UsageCount,
            LastUsedAt = dto.LastUsedAt
        };
    }

    private static ContentFilteringPolicyDto MapToDto(ContentFilteringPolicy policy)
    {
        return new ContentFilteringPolicyDto(
            policy.Id,
            policy.Name,
            policy.CreatedAt,
            policy.UpdatedAt,
            policy.FilteringEnabled,
            policy.AllowOverrideAll,
            policy.ProfanityFilter.ToString(),
            policy.CustomBannedWords,
            policy.CustomAllowedWords,
            policy.ViolenceThreshold,
            policy.BlockGraphicContent,
            policy.SexualContentThreshold,
            policy.BlockExplicitContent,
            policy.BannedTopics,
            policy.AllowedControversialTopics,
            policy.PoliticalContent.ToString(),
            policy.PoliticalContentGuidelines,
            policy.ReligiousContent.ToString(),
            policy.ReligiousContentGuidelines,
            policy.SubstanceReferences.ToString(),
            policy.BlockHateSpeech,
            policy.HateSpeechExceptions,
            policy.CopyrightPolicy.ToString(),
            policy.BlockedConcepts,
            policy.AllowedConcepts,
            policy.BlockedPeople,
            policy.AllowedPeople,
            policy.BlockedBrands,
            policy.AllowedBrands,
            policy.Description,
            policy.IsDefault,
            policy.UsageCount,
            policy.LastUsedAt);
    }

    private static ContentFilteringPolicy MapFromDto(ContentFilteringPolicyDto dto)
    {
        return new ContentFilteringPolicy
        {
            Id = dto.Id,
            Name = dto.Name,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            FilteringEnabled = dto.FilteringEnabled,
            AllowOverrideAll = dto.AllowOverrideAll,
            ProfanityFilter = Enum.Parse<ProfanityFilterLevel>(dto.ProfanityFilter, true),
            CustomBannedWords = dto.CustomBannedWords,
            CustomAllowedWords = dto.CustomAllowedWords,
            ViolenceThreshold = dto.ViolenceThreshold,
            BlockGraphicContent = dto.BlockGraphicContent,
            SexualContentThreshold = dto.SexualContentThreshold,
            BlockExplicitContent = dto.BlockExplicitContent,
            BannedTopics = dto.BannedTopics,
            AllowedControversialTopics = dto.AllowedControversialTopics,
            PoliticalContent = Enum.Parse<PoliticalContentPolicy>(dto.PoliticalContent, true),
            PoliticalContentGuidelines = dto.PoliticalContentGuidelines,
            ReligiousContent = Enum.Parse<ReligiousContentPolicy>(dto.ReligiousContent, true),
            ReligiousContentGuidelines = dto.ReligiousContentGuidelines,
            SubstanceReferences = Enum.Parse<SubstancePolicy>(dto.SubstanceReferences, true),
            BlockHateSpeech = dto.BlockHateSpeech,
            HateSpeechExceptions = dto.HateSpeechExceptions,
            CopyrightPolicy = Enum.Parse<CopyrightPolicy>(dto.CopyrightPolicy, true),
            BlockedConcepts = dto.BlockedConcepts,
            AllowedConcepts = dto.AllowedConcepts,
            BlockedPeople = dto.BlockedPeople,
            AllowedPeople = dto.AllowedPeople,
            BlockedBrands = dto.BlockedBrands,
            AllowedBrands = dto.AllowedBrands,
            Description = dto.Description,
            IsDefault = dto.IsDefault,
            UsageCount = dto.UsageCount,
            LastUsedAt = dto.LastUsedAt
        };
    }

    private static AIBehaviorSettingsDto MapToDto(AIBehaviorSettings settings)
    {
        return new AIBehaviorSettingsDto(
            settings.Id,
            settings.Name,
            settings.CreatedAt,
            settings.UpdatedAt,
            MapToDto(settings.ScriptGeneration),
            MapToDto(settings.SceneDescription),
            MapToDto(settings.ContentOptimization),
            MapToDto(settings.Translation),
            MapToDto(settings.QualityAnalysis),
            settings.CreativityVsAdherence,
            settings.EnableChainOfThought,
            settings.ShowPromptsBeforeSending,
            settings.Description,
            settings.IsDefault,
            settings.UsageCount,
            settings.LastUsedAt);
    }

    private static AIBehaviorSettings MapFromDto(AIBehaviorSettingsDto dto)
    {
        return new AIBehaviorSettings
        {
            Id = dto.Id,
            Name = dto.Name,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            ScriptGeneration = MapFromDto(dto.ScriptGeneration),
            SceneDescription = MapFromDto(dto.SceneDescription),
            ContentOptimization = MapFromDto(dto.ContentOptimization),
            Translation = MapFromDto(dto.Translation),
            QualityAnalysis = MapFromDto(dto.QualityAnalysis),
            CreativityVsAdherence = dto.CreativityVsAdherence,
            EnableChainOfThought = dto.EnableChainOfThought,
            ShowPromptsBeforeSending = dto.ShowPromptsBeforeSending,
            Description = dto.Description,
            IsDefault = dto.IsDefault,
            UsageCount = dto.UsageCount,
            LastUsedAt = dto.LastUsedAt
        };
    }

    private static LLMStageParametersDto MapToDto(LLMStageParameters parameters)
    {
        return new LLMStageParametersDto(
            parameters.StageName,
            parameters.Temperature,
            parameters.TopP,
            parameters.FrequencyPenalty,
            parameters.PresencePenalty,
            parameters.MaxTokens,
            parameters.CustomSystemPrompt,
            parameters.PreferredModel,
            parameters.StrictnessLevel);
    }

    private static LLMStageParameters MapFromDto(LLMStageParametersDto dto)
    {
        return new LLMStageParameters
        {
            StageName = dto.StageName,
            Temperature = dto.Temperature,
            TopP = dto.TopP,
            FrequencyPenalty = dto.FrequencyPenalty,
            PresencePenalty = dto.PresencePenalty,
            MaxTokens = dto.MaxTokens,
            CustomSystemPrompt = dto.CustomSystemPrompt,
            PreferredModel = dto.PreferredModel,
            StrictnessLevel = dto.StrictnessLevel
        };
    }
}
