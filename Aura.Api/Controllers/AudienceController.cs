using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models.Audience;
using Aura.Core.Services.Audience;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoints for managing audience profiles
/// </summary>
[ApiController]
[Route("api/audience")]
public class AudienceController : ControllerBase
{
    private readonly ILogger<AudienceController> _logger;
    private readonly AudienceProfileStore _store;
    private readonly AudienceProfileValidator _validator;
    private readonly AudienceProfileConverter _converter;

    public AudienceController(
        ILogger<AudienceController> logger,
        AudienceProfileStore store,
        AudienceProfileValidator validator,
        AudienceProfileConverter converter)
    {
        _logger = logger;
        _store = store;
        _validator = validator;
        _converter = converter;
    }

    /// <summary>
    /// Get all audience profiles
    /// </summary>
    [HttpGet("profiles")]
    [ProducesResponseType(typeof(AudienceProfileListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AudienceProfileListResponse>> GetProfiles(
        [FromQuery] bool? templatesOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting audience profiles, page: {Page}, pageSize: {PageSize}, templatesOnly: {TemplatesOnly}",
            page, pageSize, templatesOnly);

        var skip = (page - 1) * pageSize;
        var profiles = await _store.GetAllAsync(templatesOnly, skip, pageSize, ct);
        var totalCount = await _store.GetCountAsync(templatesOnly, ct);

        var dtos = profiles.Select(MapToDto).ToList();

        return Ok(new AudienceProfileListResponse(dtos, totalCount, page, pageSize));
    }

    /// <summary>
    /// Get a specific audience profile by ID
    /// </summary>
    [HttpGet("profiles/{id}")]
    [ProducesResponseType(typeof(AudienceProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AudienceProfileResponse>> GetProfile(
        string id,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting audience profile {ProfileId}", id);

        var profile = await _store.GetByIdAsync(id, ct);
        
        if (profile == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Audience profile {id} does not exist"
            });
        }

        var validation = _validator.Validate(profile);
        var dto = MapToDto(profile);
        var validationDto = MapValidationToDto(validation);

        return Ok(new AudienceProfileResponse(dto, validationDto));
    }

    /// <summary>
    /// Create a new audience profile
    /// </summary>
    [HttpPost("profiles")]
    [ProducesResponseType(typeof(AudienceProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AudienceProfileResponse>> CreateProfile(
        [FromBody] CreateAudienceProfileRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating new audience profile: {ProfileName}", request.Profile.Name);

        var profile = MapFromDto(request.Profile);
        var validation = _validator.Validate(profile);

        if (!validation.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Profile has validation errors",
                Extensions = { ["errors"] = validation.Errors }
            });
        }

        var created = await _store.CreateAsync(profile, ct);
        var dto = MapToDto(created);
        var validationDto = MapValidationToDto(validation);

        return CreatedAtAction(
            nameof(GetProfile),
            new { id = created.Id },
            new AudienceProfileResponse(dto, validationDto));
    }

    /// <summary>
    /// Update an existing audience profile
    /// </summary>
    [HttpPut("profiles/{id}")]
    [ProducesResponseType(typeof(AudienceProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AudienceProfileResponse>> UpdateProfile(
        string id,
        [FromBody] UpdateAudienceProfileRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Updating audience profile {ProfileId}", id);

        var existing = await _store.GetByIdAsync(id, ct);
        if (existing == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Audience profile {id} does not exist"
            });
        }

        var profile = MapFromDto(request.Profile);
        profile.Id = id;

        var validation = _validator.Validate(profile);

        if (!validation.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Profile has validation errors",
                Extensions = { ["errors"] = validation.Errors }
            });
        }

        var updated = await _store.UpdateAsync(profile, ct);
        var dto = MapToDto(updated);
        var validationDto = MapValidationToDto(validation);

        return Ok(new AudienceProfileResponse(dto, validationDto));
    }

    /// <summary>
    /// Delete an audience profile
    /// </summary>
    [HttpDelete("profiles/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteProfile(string id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting audience profile {ProfileId}", id);

        var deleted = await _store.DeleteAsync(id, ct);

        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Audience profile {id} does not exist"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Get all template profiles
    /// </summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(AudienceProfileListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AudienceProfileListResponse>> GetTemplates(CancellationToken ct = default)
    {
        _logger.LogInformation("Getting audience profile templates");

        var templates = await _store.GetTemplatesAsync(ct);
        var dtos = templates.Select(MapToDto).ToList();

        return Ok(new AudienceProfileListResponse(dtos, dtos.Count, 1, dtos.Count));
    }

    /// <summary>
    /// Analyze script text and infer audience profile
    /// </summary>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AnalyzeAudienceResponse), StatusCodes.Status200OK)]
    public ActionResult<AnalyzeAudienceResponse> AnalyzeAudience(
        [FromBody] AnalyzeAudienceRequest request)
    {
        _logger.LogInformation("Analyzing script text to infer audience profile");

        var inferredString = InferAudienceFromScript(request.ScriptText);
        var profile = _converter.ConvertFromString(inferredString);
        var dto = MapToDto(profile);

        var reasoningFactors = BuildReasoningFactors(request.ScriptText, inferredString);

        return Ok(new AnalyzeAudienceResponse(
            InferredProfile: dto,
            ConfidenceScore: 0.75,
            ReasoningFactors: reasoningFactors));
    }

    /// <summary>
    /// Toggle favorite status for a profile
    /// </summary>
    [HttpPost("profiles/{id}/favorite")]
    [ProducesResponseType(typeof(AudienceProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AudienceProfileResponse>> ToggleFavorite(
        string id,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Toggling favorite for profile {ProfileId}", id);

        try
        {
            var profile = await _store.ToggleFavoriteAsync(id, ct);
            var dto = MapToDto(profile);
            var validation = _validator.Validate(profile);
            var validationDto = MapValidationToDto(validation);

            return Ok(new AudienceProfileResponse(dto, validationDto));
        }
        catch (InvalidOperationException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Audience profile {id} does not exist"
            });
        }
    }

    /// <summary>
    /// Get all favorite profiles
    /// </summary>
    [HttpGet("favorites")]
    [ProducesResponseType(typeof(AudienceProfileListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AudienceProfileListResponse>> GetFavorites(CancellationToken ct = default)
    {
        _logger.LogInformation("Getting favorite profiles");

        var profiles = await _store.GetFavoritesAsync(ct);
        var dtos = profiles.Select(MapToDto).ToList();

        return Ok(new AudienceProfileListResponse(dtos, dtos.Count, 1, dtos.Count));
    }

    /// <summary>
    /// Move profile to a folder
    /// </summary>
    [HttpPost("profiles/{id}/move")]
    [ProducesResponseType(typeof(AudienceProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AudienceProfileResponse>> MoveToFolder(
        string id,
        [FromBody] MoveToFolderRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Moving profile {ProfileId} to folder {FolderPath}", id, request.FolderPath ?? "(root)");

        try
        {
            var profile = await _store.MoveToFolderAsync(id, request.FolderPath, ct);
            var dto = MapToDto(profile);
            var validation = _validator.Validate(profile);
            var validationDto = MapValidationToDto(validation);

            return Ok(new AudienceProfileResponse(dto, validationDto));
        }
        catch (InvalidOperationException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Audience profile {id} does not exist"
            });
        }
    }

    /// <summary>
    /// Get profiles in a specific folder
    /// </summary>
    [HttpGet("folders/{*folderPath}")]
    [ProducesResponseType(typeof(AudienceProfileListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AudienceProfileListResponse>> GetProfilesByFolder(
        string? folderPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting profiles in folder: {FolderPath}", folderPath ?? "(root)");

        var profiles = await _store.GetByFolderAsync(folderPath, ct);
        var dtos = profiles.Select(MapToDto).ToList();

        return Ok(new AudienceProfileListResponse(dtos, dtos.Count, 1, dtos.Count));
    }

    /// <summary>
    /// Get all folder paths
    /// </summary>
    [HttpGet("folders")]
    [ProducesResponseType(typeof(FolderListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FolderListResponse>> GetFolders(CancellationToken ct = default)
    {
        _logger.LogInformation("Getting all folder paths");

        var folders = await _store.GetFoldersAsync(ct);
        return Ok(new FolderListResponse(folders));
    }

    /// <summary>
    /// Search profiles with advanced full-text search
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(AudienceProfileListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AudienceProfileListResponse>> SearchProfiles(
        [FromQuery] string query,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Searching profiles with query: {Query}", query);

        var profiles = await _store.SearchAsync(query, ct);
        var dtos = profiles.Select(MapToDto).ToList();

        return Ok(new AudienceProfileListResponse(dtos, dtos.Count, 1, dtos.Count));
    }

    /// <summary>
    /// Record profile usage for analytics
    /// </summary>
    [HttpPost("profiles/{id}/usage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RecordUsage(string id, CancellationToken ct = default)
    {
        _logger.LogInformation("Recording usage for profile {ProfileId}", id);

        await _store.RecordUsageAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Export profile to JSON
    /// </summary>
    [HttpGet("profiles/{id}/export")]
    [ProducesResponseType(typeof(ExportProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExportProfileResponse>> ExportProfile(
        string id,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting profile {ProfileId}", id);

        try
        {
            var json = await _store.ExportToJsonAsync(id, ct);
            return Ok(new ExportProfileResponse(json));
        }
        catch (InvalidOperationException)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Profile Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Audience profile {id} does not exist"
            });
        }
    }

    /// <summary>
    /// Import profile from JSON
    /// </summary>
    [HttpPost("profiles/import")]
    [ProducesResponseType(typeof(AudienceProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AudienceProfileResponse>> ImportProfile(
        [FromBody] ImportProfileRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Importing profile from JSON");

        try
        {
            var profile = await _store.ImportFromJsonAsync(request.Json, ct);
            var dto = MapToDto(profile);
            var validation = _validator.Validate(profile);
            var validationDto = MapValidationToDto(validation);

            return CreatedAtAction(
                nameof(GetProfile),
                new { id = profile.Id },
                new AudienceProfileResponse(dto, validationDto));
        }
        catch (Exception ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Import Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"Failed to import profile: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get recommended profiles based on topic and goal
    /// </summary>
    [HttpPost("recommend")]
    [ProducesResponseType(typeof(AudienceProfileListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AudienceProfileListResponse>> RecommendProfiles(
        [FromBody] RecommendProfilesRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting profile recommendations for topic: {Topic}", request.Topic);

        var profiles = await _store.GetRecommendedProfilesAsync(
            request.Topic,
            request.Goal,
            request.MaxResults ?? 5,
            ct);

        var dtos = profiles.Select(MapToDto).ToList();

        return Ok(new AudienceProfileListResponse(dtos, dtos.Count, 1, dtos.Count));
    }

    private AudienceProfileDto MapToDto(AudienceProfile profile)
    {
        return new AudienceProfileDto(
            Id: profile.Id,
            Name: profile.Name,
            Description: profile.Description,
            AgeRange: profile.AgeRange != null ? new AgeRangeDto(
                profile.AgeRange.MinAge,
                profile.AgeRange.MaxAge,
                profile.AgeRange.DisplayName,
                profile.AgeRange.ContentRating.ToString()) : null,
            EducationLevel: profile.EducationLevel?.ToString(),
            Profession: profile.Profession,
            Industry: profile.Industry,
            ExpertiseLevel: profile.ExpertiseLevel?.ToString(),
            IncomeBracket: profile.IncomeBracket?.ToString(),
            GeographicRegion: profile.GeographicRegion?.ToString(),
            LanguageFluency: profile.LanguageFluency != null ? new LanguageFluencyDto(
                profile.LanguageFluency.Language,
                profile.LanguageFluency.Level.ToString()) : null,
            Interests: profile.Interests,
            PainPoints: profile.PainPoints,
            Motivations: profile.Motivations,
            CulturalBackground: profile.CulturalBackground != null ? new CulturalBackgroundDto(
                profile.CulturalBackground.Sensitivities,
                profile.CulturalBackground.TabooTopics,
                profile.CulturalBackground.PreferredCommunicationStyle.ToString()) : null,
            PreferredLearningStyle: profile.PreferredLearningStyle?.ToString(),
            AttentionSpan: profile.AttentionSpan != null ? new AttentionSpanDto(
                profile.AttentionSpan.PreferredDuration.TotalMinutes,
                profile.AttentionSpan.DisplayName) : null,
            TechnicalComfort: profile.TechnicalComfort?.ToString(),
            AccessibilityNeeds: profile.AccessibilityNeeds != null ? new AccessibilityNeedsDto(
                profile.AccessibilityNeeds.RequiresCaptions,
                profile.AccessibilityNeeds.RequiresAudioDescriptions,
                profile.AccessibilityNeeds.RequiresHighContrast,
                profile.AccessibilityNeeds.RequiresSimplifiedLanguage,
                profile.AccessibilityNeeds.RequiresLargeText) : null,
            IsTemplate: profile.IsTemplate,
            Tags: profile.Tags,
            Version: profile.Version,
            CreatedAt: profile.CreatedAt,
            UpdatedAt: profile.UpdatedAt,
            IsFavorite: profile.IsFavorite,
            FolderPath: profile.FolderPath,
            UsageCount: profile.UsageCount,
            LastUsedAt: profile.LastUsedAt);
    }

    private AudienceProfile MapFromDto(AudienceProfileDto dto)
    {
        var profile = new AudienceProfile
        {
            Id = dto.Id ?? Guid.NewGuid().ToString(),
            Name = dto.Name,
            Description = dto.Description,
            Profession = dto.Profession,
            Industry = dto.Industry,
            Interests = dto.Interests ?? new List<string>(),
            PainPoints = dto.PainPoints ?? new List<string>(),
            Motivations = dto.Motivations ?? new List<string>(),
            IsTemplate = dto.IsTemplate,
            Tags = dto.Tags ?? new List<string>(),
            Version = dto.Version,
            IsFavorite = dto.IsFavorite,
            FolderPath = dto.FolderPath,
            UsageCount = dto.UsageCount,
            LastUsedAt = dto.LastUsedAt
        };

        if (dto.AgeRange != null)
        {
            profile.AgeRange = new AgeRange
            {
                MinAge = dto.AgeRange.MinAge,
                MaxAge = dto.AgeRange.MaxAge,
                DisplayName = dto.AgeRange.DisplayName,
                ContentRating = Enum.Parse<ContentRating>(dto.AgeRange.ContentRating)
            };
        }

        if (!string.IsNullOrWhiteSpace(dto.EducationLevel))
        {
            profile.EducationLevel = Enum.Parse<EducationLevel>(dto.EducationLevel);
        }

        if (!string.IsNullOrWhiteSpace(dto.ExpertiseLevel))
        {
            profile.ExpertiseLevel = Enum.Parse<ExpertiseLevel>(dto.ExpertiseLevel);
        }

        if (!string.IsNullOrWhiteSpace(dto.IncomeBracket))
        {
            profile.IncomeBracket = Enum.Parse<IncomeBracket>(dto.IncomeBracket);
        }

        if (!string.IsNullOrWhiteSpace(dto.GeographicRegion))
        {
            profile.GeographicRegion = Enum.Parse<GeographicRegion>(dto.GeographicRegion);
        }

        if (dto.LanguageFluency != null)
        {
            profile.LanguageFluency = new LanguageFluency
            {
                Language = dto.LanguageFluency.Language,
                Level = Enum.Parse<FluencyLevel>(dto.LanguageFluency.Level)
            };
        }

        if (dto.CulturalBackground != null)
        {
            profile.CulturalBackground = new CulturalBackground
            {
                Sensitivities = dto.CulturalBackground.Sensitivities ?? new List<string>(),
                TabooTopics = dto.CulturalBackground.TabooTopics ?? new List<string>(),
                PreferredCommunicationStyle = Enum.Parse<CommunicationStyle>(dto.CulturalBackground.PreferredCommunicationStyle)
            };
        }

        if (!string.IsNullOrWhiteSpace(dto.PreferredLearningStyle))
        {
            profile.PreferredLearningStyle = Enum.Parse<LearningStyle>(dto.PreferredLearningStyle);
        }

        if (dto.AttentionSpan != null)
        {
            profile.AttentionSpan = new AttentionSpan
            {
                PreferredDuration = TimeSpan.FromMinutes(dto.AttentionSpan.PreferredDurationMinutes),
                DisplayName = dto.AttentionSpan.DisplayName
            };
        }

        if (!string.IsNullOrWhiteSpace(dto.TechnicalComfort))
        {
            profile.TechnicalComfort = Enum.Parse<TechnicalComfort>(dto.TechnicalComfort);
        }

        if (dto.AccessibilityNeeds != null)
        {
            profile.AccessibilityNeeds = new AccessibilityNeeds
            {
                RequiresCaptions = dto.AccessibilityNeeds.RequiresCaptions,
                RequiresAudioDescriptions = dto.AccessibilityNeeds.RequiresAudioDescriptions,
                RequiresHighContrast = dto.AccessibilityNeeds.RequiresHighContrast,
                RequiresSimplifiedLanguage = dto.AccessibilityNeeds.RequiresSimplifiedLanguage,
                RequiresLargeText = dto.AccessibilityNeeds.RequiresLargeText
            };
        }

        return profile;
    }

    private Models.ApiModels.V1.ValidationResultDto MapValidationToDto(ValidationResult validation)
    {
        return new Models.ApiModels.V1.ValidationResultDto(
            IsValid: validation.IsValid,
            Errors: validation.Errors.Select(e => new Models.ApiModels.V1.ValidationIssueDto(
                e.Severity.ToString(),
                e.Field,
                e.Message,
                e.SuggestedFix)).ToList(),
            Warnings: validation.Warnings.Select(w => new Models.ApiModels.V1.ValidationIssueDto(
                w.Severity.ToString(),
                w.Field,
                w.Message,
                w.SuggestedFix)).ToList(),
            Infos: validation.Infos.Select(i => new Models.ApiModels.V1.ValidationIssueDto(
                i.Severity.ToString(),
                i.Field,
                i.Message,
                i.SuggestedFix)).ToList());
    }

    private string InferAudienceFromScript(string scriptText)
    {
        var lower = scriptText.ToLowerInvariant();

        if (lower.Contains("beginner") || lower.Contains("introduction") || lower.Contains("getting started"))
        {
            return "beginners";
        }

        if (lower.Contains("advanced") || lower.Contains("expert") || lower.Contains("professional"))
        {
            return "advanced professionals";
        }

        if (lower.Contains("business") || lower.Contains("corporate") || lower.Contains("enterprise"))
        {
            return "business professionals";
        }

        if (lower.Contains("student") || lower.Contains("learn") || lower.Contains("education"))
        {
            return "students";
        }

        return "general audience";
    }

    private List<string> BuildReasoningFactors(string scriptText, string inferredAudience)
    {
        var factors = new List<string>
        {
            $"Inferred audience: {inferredAudience}",
            $"Script length: {scriptText.Length} characters"
        };

        var lower = scriptText.ToLowerInvariant();

        if (lower.Contains("beginner") || lower.Contains("introduction"))
        {
            factors.Add("Contains beginner-oriented language");
        }

        if (lower.Contains("advanced") || lower.Contains("expert"))
        {
            factors.Add("Contains advanced terminology");
        }

        if (lower.Contains("business") || lower.Contains("corporate"))
        {
            factors.Add("Contains business context");
        }

        return factors;
    }
}
