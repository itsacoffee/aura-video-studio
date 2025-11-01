using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models;
using Aura.Core.Models.Localization;
using Aura.Core.Providers;
using Aura.Core.Services.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Advanced multi-language translation with cultural localization
/// </summary>
[ApiController]
[Route("api/localization")]
public class LocalizationController : ControllerBase
{
    private readonly ILogger<LocalizationController> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly GlossaryManager _glossaryManager;
    private readonly ILoggerFactory _loggerFactory;

    public LocalizationController(
        ILogger<LocalizationController> logger,
        ILlmProvider llmProvider,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _loggerFactory = loggerFactory;
        
        var storageDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AuraVideoStudio",
            "Glossaries");
        _glossaryManager = new GlossaryManager(
            loggerFactory.CreateLogger<GlossaryManager>(),
            storageDir);
    }

    /// <summary>
    /// Translate script with cultural localization
    /// </summary>
    [HttpPost("translate")]
    [ProducesResponseType(typeof(TranslationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TranslationResultDto>> TranslateScript(
        [FromBody] TranslateScriptRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Translation request: {Source} → {Target}, CorrelationId: {CorrelationId}",
            request.SourceLanguage, request.TargetLanguage, HttpContext.TraceIdentifier);

        try
        {
            var translationService = new TranslationService(
                _loggerFactory.CreateLogger<TranslationService>(), 
                _llmProvider);

            var translationRequest = MapToTranslationRequest(request);
            var result = await translationService.TranslateAsync(translationRequest, cancellationToken);
            
            var dto = MapToTranslationResultDto(result);
            
            _logger.LogInformation("Translation completed in {Time:F2}s with quality {Quality:F1}",
                result.TranslationTimeSeconds, result.Quality.OverallScore);

            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid translation request");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Translation Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Translation Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Batch translate to multiple languages
    /// </summary>
    [HttpPost("translate/batch")]
    [ProducesResponseType(typeof(BatchTranslationResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BatchTranslationResultDto>> BatchTranslate(
        [FromBody] BatchTranslateRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Batch translation request: {Source} → [{Targets}]",
            request.SourceLanguage, string.Join(", ", request.TargetLanguages));

        try
        {
            var translationService = new TranslationService(
                _loggerFactory.CreateLogger<TranslationService>(), 
                _llmProvider);

            var batchRequest = MapToBatchTranslationRequest(request);
            var result = await translationService.BatchTranslateAsync(batchRequest, null, cancellationToken);
            
            var dto = MapToBatchTranslationResultDto(result);

            _logger.LogInformation("Batch translation completed: {Success}/{Total}",
                result.SuccessfulLanguages.Count, request.TargetLanguages.Count);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch translation failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Batch Translation Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Analyze cultural appropriateness of content
    /// </summary>
    [HttpPost("analyze-culture")]
    [ProducesResponseType(typeof(CulturalAnalysisResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CulturalAnalysisResultDto>> AnalyzeCulturalContent(
        [FromBody] Models.ApiModels.V1.CulturalAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cultural analysis request: {Language}/{Region}",
            request.TargetLanguage, request.TargetRegion);

        try
        {
            var translationService = new TranslationService(
                _loggerFactory.CreateLogger<TranslationService>(), 
                _llmProvider);

            var analysisRequest = new Core.Models.Localization.CulturalAnalysisRequest
            {
                TargetLanguage = request.TargetLanguage,
                TargetRegion = request.TargetRegion,
                Content = request.Content,
                AudienceProfileId = request.AudienceProfileId
            };

            var result = await translationService.AnalyzeCulturalContentAsync(analysisRequest, cancellationToken);
            
            var dto = MapToCulturalAnalysisResultDto(result);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cultural analysis failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Cultural Analysis Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get list of supported languages
    /// </summary>
    [HttpGet("languages")]
    [ProducesResponseType(typeof(List<LanguageInfoDto>), StatusCodes.Status200OK)]
    public ActionResult<List<LanguageInfoDto>> GetSupportedLanguages()
    {
        var languages = LanguageRegistry.GetAllLanguages();
        var dtos = languages.Select(MapToLanguageInfoDto).ToList();
        
        _logger.LogInformation("Returned {Count} supported languages", dtos.Count);
        return Ok(dtos);
    }

    /// <summary>
    /// Get language information
    /// </summary>
    [HttpGet("languages/{languageCode}")]
    [ProducesResponseType(typeof(LanguageInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<LanguageInfoDto> GetLanguage(string languageCode)
    {
        var language = LanguageRegistry.GetLanguage(languageCode);
        
        if (language == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Language Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Language {languageCode} is not supported",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }

        return Ok(MapToLanguageInfoDto(language));
    }

    /// <summary>
    /// Create new glossary
    /// </summary>
    [HttpPost("glossary")]
    [ProducesResponseType(typeof(ProjectGlossaryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjectGlossaryDto>> CreateGlossary(
        [FromBody] CreateGlossaryRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating glossary: {Name}", request.Name);

        try
        {
            var glossary = await _glossaryManager.CreateGlossaryAsync(
                request.Name,
                request.Description,
                cancellationToken);

            var dto = MapToProjectGlossaryDto(glossary);
            
            return CreatedAtAction(
                nameof(GetGlossary),
                new { glossaryId = glossary.Id },
                dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create glossary");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Glossary Creation Failed",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get glossary by ID
    /// </summary>
    [HttpGet("glossary/{glossaryId}")]
    [ProducesResponseType(typeof(ProjectGlossaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectGlossaryDto>> GetGlossary(
        string glossaryId,
        CancellationToken cancellationToken)
    {
        var glossary = await _glossaryManager.GetGlossaryAsync(glossaryId, cancellationToken);
        
        if (glossary == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Glossary Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Glossary {glossaryId} not found",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }

        return Ok(MapToProjectGlossaryDto(glossary));
    }

    /// <summary>
    /// List all glossaries
    /// </summary>
    [HttpGet("glossary")]
    [ProducesResponseType(typeof(List<ProjectGlossaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProjectGlossaryDto>>> ListGlossaries(
        CancellationToken cancellationToken)
    {
        var glossaries = await _glossaryManager.ListGlossariesAsync(cancellationToken);
        var dtos = glossaries.Select(MapToProjectGlossaryDto).ToList();
        
        return Ok(dtos);
    }

    /// <summary>
    /// Add entry to glossary
    /// </summary>
    [HttpPost("glossary/{glossaryId}/entries")]
    [ProducesResponseType(typeof(GlossaryEntryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<GlossaryEntryDto>> AddGlossaryEntry(
        string glossaryId,
        [FromBody] AddGlossaryEntryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _glossaryManager.AddEntryAsync(
                glossaryId,
                request.Term,
                request.Translations,
                request.Context,
                request.Industry,
                cancellationToken);

            var dto = MapToGlossaryEntryDto(entry);
            
            return CreatedAtAction(
                nameof(GetGlossary),
                new { glossaryId },
                dto);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Glossary Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Delete glossary
    /// </summary>
    [HttpDelete("glossary/{glossaryId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteGlossary(
        string glossaryId,
        CancellationToken cancellationToken)
    {
        await _glossaryManager.DeleteGlossaryAsync(glossaryId, cancellationToken);
        return NoContent();
    }

    private TranslationRequest MapToTranslationRequest(TranslateScriptRequest request)
    {
        return new TranslationRequest
        {
            SourceLanguage = request.SourceLanguage,
            TargetLanguage = request.TargetLanguage,
            SourceText = request.SourceText ?? string.Empty,
            ScriptLines = request.ScriptLines?.Select(MapToScriptLine).ToList() ?? new(),
            CulturalContext = request.CulturalContext != null ? MapToCulturalContext(request.CulturalContext) : null,
            Options = request.Options != null ? MapToTranslationOptions(request.Options) : new(),
            Glossary = request.Glossary ?? new(),
            AudienceProfileId = request.AudienceProfileId
        };
    }

    private BatchTranslationRequest MapToBatchTranslationRequest(BatchTranslateRequest request)
    {
        return new BatchTranslationRequest
        {
            SourceLanguage = request.SourceLanguage,
            TargetLanguages = request.TargetLanguages,
            SourceText = request.SourceText ?? string.Empty,
            ScriptLines = request.ScriptLines?.Select(MapToScriptLine).ToList() ?? new(),
            CulturalContext = request.CulturalContext != null ? MapToCulturalContext(request.CulturalContext) : null,
            Options = request.Options != null ? MapToTranslationOptions(request.Options) : new(),
            Glossary = request.Glossary ?? new()
        };
    }

    private ScriptLine MapToScriptLine(ScriptLineDto dto)
    {
        return new ScriptLine(
            SceneIndex: 0,
            Text: dto.Text,
            Start: TimeSpan.FromSeconds(dto.StartSeconds),
            Duration: TimeSpan.FromSeconds(dto.DurationSeconds)
        );
    }

    private Core.Models.Localization.CulturalContext MapToCulturalContext(CulturalContextDto dto)
    {
        return new Core.Models.Localization.CulturalContext
        {
            TargetRegion = dto.TargetRegion,
            TargetFormality = Enum.Parse<FormalityLevel>(dto.TargetFormality, true),
            PreferredStyle = Enum.Parse<Aura.Core.Models.Audience.CommunicationStyle>(dto.PreferredStyle, true),
            Sensitivities = dto.Sensitivities,
            TabooTopics = dto.TabooTopics,
            ContentRating = Enum.Parse<AgeRating>(dto.ContentRating, true)
        };
    }

    private TranslationOptions MapToTranslationOptions(TranslationOptionsDto dto)
    {
        return new TranslationOptions
        {
            Mode = Enum.Parse<TranslationMode>(dto.Mode, true),
            EnableBackTranslation = dto.EnableBackTranslation,
            EnableQualityScoring = dto.EnableQualityScoring,
            AdjustTimings = dto.AdjustTimings,
            MaxTimingVariance = dto.MaxTimingVariance,
            PreserveNames = dto.PreserveNames,
            PreserveBrands = dto.PreserveBrands,
            AdaptMeasurements = dto.AdaptMeasurements
        };
    }

    private TranslationResultDto MapToTranslationResultDto(TranslationResult result)
    {
        return new TranslationResultDto(
            result.SourceLanguage,
            result.TargetLanguage,
            result.SourceText,
            result.TranslatedText,
            result.TranslatedLines.Select(MapToTranslatedScriptLineDto).ToList(),
            MapToTranslationQualityDto(result.Quality),
            result.CulturalAdaptations.Select(MapToCulturalAdaptationDto).ToList(),
            MapToTimingAdjustmentDto(result.TimingAdjustment),
            result.VisualRecommendations.Select(MapToVisualLocalizationRecommendationDto).ToList(),
            result.TranslationTimeSeconds
        );
    }

    private TranslatedScriptLineDto MapToTranslatedScriptLineDto(TranslatedScriptLine line)
    {
        return new TranslatedScriptLineDto(
            line.SceneIndex,
            line.SourceText,
            line.TranslatedText,
            line.OriginalStartSeconds,
            line.OriginalDurationSeconds,
            line.AdjustedStartSeconds,
            line.AdjustedDurationSeconds,
            line.TimingVariance,
            line.AdaptationNotes
        );
    }

    private TranslationQualityDto MapToTranslationQualityDto(TranslationQuality quality)
    {
        return new TranslationQualityDto(
            quality.OverallScore,
            quality.FluencyScore,
            quality.AccuracyScore,
            quality.CulturalAppropriatenessScore,
            quality.TerminologyConsistencyScore,
            quality.BackTranslationScore,
            quality.BackTranslatedText,
            quality.Issues.Select(i => new QualityIssueDto(
                i.Severity.ToString(),
                i.Category,
                i.Description,
                i.Suggestion,
                i.LineNumber
            )).ToList()
        );
    }

    private CulturalAdaptationDto MapToCulturalAdaptationDto(CulturalAdaptation adaptation)
    {
        return new CulturalAdaptationDto(
            adaptation.Category,
            adaptation.SourcePhrase,
            adaptation.AdaptedPhrase,
            adaptation.Reasoning,
            adaptation.LineNumber
        );
    }

    private TimingAdjustmentDto MapToTimingAdjustmentDto(TimingAdjustment adjustment)
    {
        return new TimingAdjustmentDto(
            adjustment.OriginalTotalDuration,
            adjustment.AdjustedTotalDuration,
            adjustment.ExpansionFactor,
            adjustment.RequiresCompression,
            adjustment.CompressionSuggestions,
            adjustment.Warnings.Select(w => new TimingWarningDto(
                w.Severity.ToString(),
                w.Message,
                w.LineNumber
            )).ToList()
        );
    }

    private VisualLocalizationRecommendationDto MapToVisualLocalizationRecommendationDto(
        VisualLocalizationRecommendation rec)
    {
        return new VisualLocalizationRecommendationDto(
            rec.ElementType.ToString(),
            rec.Description,
            rec.Recommendation,
            rec.Priority.ToString(),
            rec.SceneIndex
        );
    }

    private BatchTranslationResultDto MapToBatchTranslationResultDto(BatchTranslationResult result)
    {
        return new BatchTranslationResultDto(
            result.SourceLanguage,
            result.Translations.ToDictionary(
                kvp => kvp.Key,
                kvp => MapToTranslationResultDto(kvp.Value)
            ),
            result.SuccessfulLanguages,
            result.FailedLanguages,
            result.TotalTimeSeconds
        );
    }

    private CulturalAnalysisResultDto MapToCulturalAnalysisResultDto(CulturalAnalysisResult result)
    {
        return new CulturalAnalysisResultDto(
            result.TargetLanguage,
            result.TargetRegion,
            result.CulturalSensitivityScore,
            result.Issues.Select(i => new CulturalIssueDto(
                i.Severity.ToString(),
                i.Category,
                i.Issue,
                i.Context,
                i.Suggestion
            )).ToList(),
            result.Recommendations.Select(r => new CulturalRecommendationDto(
                r.Category,
                r.Recommendation,
                r.Reasoning,
                r.Priority.ToString()
            )).ToList()
        );
    }

    private LanguageInfoDto MapToLanguageInfoDto(LanguageInfo language)
    {
        return new LanguageInfoDto(
            language.Code,
            language.Name,
            language.NativeName,
            language.Region,
            language.IsRightToLeft,
            language.DefaultFormality.ToString(),
            language.TypicalExpansionFactor
        );
    }

    private ProjectGlossaryDto MapToProjectGlossaryDto(ProjectGlossary glossary)
    {
        return new ProjectGlossaryDto(
            glossary.Id,
            glossary.Name,
            glossary.Description,
            glossary.Entries.Select(MapToGlossaryEntryDto).ToList(),
            glossary.CreatedAt,
            glossary.UpdatedAt
        );
    }

    private GlossaryEntryDto MapToGlossaryEntryDto(GlossaryEntry entry)
    {
        return new GlossaryEntryDto(
            entry.Id,
            entry.Term,
            entry.Translations,
            entry.Context,
            entry.Industry
        );
    }
}
