using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models.PromptManagement;
using Aura.Core.Providers;
using Aura.Core.Services.PromptManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// API controller for advanced prompt template management
/// </summary>
[ApiController]
[Route("api/prompt-management")]
public class PromptManagementController : ControllerBase
{
    private readonly ILogger<PromptManagementController> _logger;
    private readonly PromptManagementService _promptService;
    private readonly PromptTestingService _testingService;
    private readonly PromptABTestingService _abTestingService;
    private readonly PromptAnalyticsService _analyticsService;
    private readonly ILlmProvider? _llmProvider;

    public PromptManagementController(
        ILogger<PromptManagementController> logger,
        PromptManagementService promptService,
        PromptTestingService testingService,
        PromptABTestingService abTestingService,
        PromptAnalyticsService analyticsService,
        ILlmProvider? llmProvider = null)
    {
        _logger = logger;
        _promptService = promptService;
        _testingService = testingService;
        _abTestingService = abTestingService;
        _analyticsService = analyticsService;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Create a new prompt template
    /// </summary>
    [HttpPost("templates")]
    [ProducesResponseType(typeof(PromptTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromptTemplateDto>> CreateTemplate(
        [FromBody] CreatePromptTemplateRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating prompt template: {Name}", request.Name);

        try
        {
            var template = MapToTemplate(request);
            var created = await _promptService.CreateTemplateAsync(template, "user", ct);

            return CreatedAtAction(
                nameof(GetTemplate),
                new { id = created.Id },
                MapToDto(created));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateProblemDetails("Validation Error", ex.Message, 400));
        }
    }

    /// <summary>
    /// Get a prompt template by ID
    /// </summary>
    [HttpGet("templates/{id}")]
    [ProducesResponseType(typeof(PromptTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromptTemplateDto>> GetTemplate(string id, CancellationToken ct)
    {
        var template = await _promptService.GetTemplateAsync(id, ct);
        if (template == null)
        {
            return NotFound(CreateProblemDetails("Not Found", $"Template {id} not found", 404));
        }

        return Ok(MapToDto(template));
    }

    /// <summary>
    /// List prompt templates with optional filtering
    /// </summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(List<PromptTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PromptTemplateDto>>> ListTemplates(
        [FromQuery] string? category = null,
        [FromQuery] string? stage = null,
        [FromQuery] string? source = null,
        [FromQuery] string? status = null,
        [FromQuery] string? createdBy = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var templates = await _promptService.ListTemplatesAsync(
            ParseEnum<PromptCategory>(category),
            ParseEnum<PipelineStage>(stage),
            ParseEnum<TemplateSource>(source),
            ParseEnum<TemplateStatus>(status),
            createdBy,
            searchTerm,
            skip,
            take,
            ct);

        return Ok(templates.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Update an existing prompt template
    /// </summary>
    [HttpPut("templates/{id}")]
    [ProducesResponseType(typeof(PromptTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromptTemplateDto>> UpdateTemplate(
        string id,
        [FromBody] UpdatePromptTemplateRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Updating prompt template {Id}", id);

        try
        {
            var updates = MapToTemplateFromUpdate(request);
            var updated = await _promptService.UpdateTemplateAsync(
                id, updates, "user", request.ChangeNotes, ct);

            return Ok(MapToDto(updated));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateProblemDetails("Validation Error", ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateProblemDetails("Invalid Operation", ex.Message, 400));
        }
    }

    /// <summary>
    /// Delete a prompt template
    /// </summary>
    [HttpDelete("templates/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(string id, CancellationToken ct)
    {
        _logger.LogInformation("Deleting prompt template {Id}", id);

        try
        {
            await _promptService.DeleteTemplateAsync(id, ct);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(CreateProblemDetails("Not Found", ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateProblemDetails("Invalid Operation", ex.Message, 400));
        }
    }

    /// <summary>
    /// Clone a prompt template
    /// </summary>
    [HttpPost("templates/{id}/clone")]
    [ProducesResponseType(typeof(PromptTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromptTemplateDto>> CloneTemplate(
        string id,
        [FromBody] CloneTemplateRequest? request,
        CancellationToken ct)
    {
        _logger.LogInformation("Cloning prompt template {Id}", id);

        try
        {
            var cloned = await _promptService.CloneTemplateAsync(id, "user", request?.NewName, ct);
            return CreatedAtAction(nameof(GetTemplate), new { id = cloned.Id }, MapToDto(cloned));
        }
        catch (ArgumentException ex)
        {
            return NotFound(CreateProblemDetails("Not Found", ex.Message, 404));
        }
    }

    /// <summary>
    /// Get version history for a template
    /// </summary>
    [HttpGet("templates/{id}/versions")]
    [ProducesResponseType(typeof(List<PromptTemplateVersionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PromptTemplateVersionDto>>> GetVersionHistory(
        string id,
        CancellationToken ct)
    {
        var versions = await _promptService.GetVersionHistoryAsync(id, ct);
        return Ok(versions.Select(MapToVersionDto).ToList());
    }

    /// <summary>
    /// Rollback template to a previous version
    /// </summary>
    [HttpPost("templates/{id}/rollback")]
    [ProducesResponseType(typeof(PromptTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromptTemplateDto>> RollbackTemplate(
        string id,
        [FromQuery] int targetVersion,
        CancellationToken ct)
    {
        _logger.LogInformation("Rolling back template {Id} to version {Version}", id, targetVersion);

        try
        {
            var rolled = await _promptService.RollbackTemplateAsync(id, targetVersion, "user", ct);
            return Ok(MapToDto(rolled));
        }
        catch (ArgumentException ex)
        {
            return NotFound(CreateProblemDetails("Not Found", ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateProblemDetails("Invalid Operation", ex.Message, 400));
        }
    }

    /// <summary>
    /// Test a prompt template with sample data
    /// </summary>
    [HttpPost("templates/{id}/test")]
    [ProducesResponseType(typeof(TestPromptResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TestPromptResultDto>> TestTemplate(
        string id,
        [FromBody] TestPromptRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Testing prompt template {Id}", id);

        if (_llmProvider == null)
        {
            return BadRequest(CreateProblemDetails(
                "Service Unavailable",
                "No LLM provider configured for testing",
                400));
        }

        var testRequest = new PromptTestRequest
        {
            TemplateId = id,
            TestVariables = request.TestVariables,
            UseLowTokenLimit = request.UseLowTokenLimit
        };

        var result = await _testingService.TestPromptAsync(testRequest, _llmProvider, ct);
        return Ok(MapToTestResultDto(result));
    }

    /// <summary>
    /// Validate template variable resolution without calling LLM
    /// </summary>
    [HttpPost("templates/{id}/validate-resolution")]
    [ProducesResponseType(typeof(TestPromptResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TestPromptResultDto>> ValidateResolution(
        string id,
        [FromBody] ValidateTemplateResolutionRequest request,
        CancellationToken ct)
    {
        var result = await _testingService.ValidatePromptResolutionAsync(id, request.TestVariables, ct);
        return Ok(MapToTestResultDto(result));
    }

    /// <summary>
    /// Resolve template variables and get the final prompt
    /// </summary>
    [HttpPost("templates/{id}/resolve")]
    [ProducesResponseType(typeof(ResolveTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResolveTemplateResponse>> ResolveTemplate(
        string id,
        [FromBody] ResolveTemplateRequest request,
        CancellationToken ct)
    {
        try
        {
            var options = new VariableResolverOptions
            {
                ThrowOnMissingRequired = request.ThrowOnMissing,
                SanitizeValues = request.SanitizeValues
            };

            var resolved = await _promptService.ResolveTemplateAsync(id, request.Variables, options, ct);

            var tokens = EstimateTokenCount(resolved);

            return Ok(new ResolveTemplateResponse(resolved, tokens));
        }
        catch (ArgumentException ex)
        {
            return NotFound(CreateProblemDetails("Not Found", ex.Message, 404));
        }
    }

    /// <summary>
    /// Record feedback on a template
    /// </summary>
    [HttpPost("templates/{id}/feedback")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordFeedback(
        string id,
        [FromBody] RecordFeedbackRequest request,
        CancellationToken ct)
    {
        await _promptService.RecordFeedbackAsync(
            id,
            request.ThumbsUp,
            request.QualityScore,
            request.GenerationTimeMs,
            request.TokenUsage,
            ct);

        return NoContent();
    }

    /// <summary>
    /// Get analytics for prompts
    /// </summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(PromptAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromptAnalyticsDto>> GetAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? category = null,
        [FromQuery] string? stage = null,
        [FromQuery] string? source = null,
        [FromQuery] string? createdBy = null,
        [FromQuery] int top = 10,
        CancellationToken ct = default)
    {
        var query = new PromptAnalyticsQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            Category = ParseEnum<PromptCategory>(category),
            Stage = ParseEnum<PipelineStage>(stage),
            Source = ParseEnum<TemplateSource>(source),
            CreatedBy = createdBy,
            Top = top
        };

        var analytics = await _analyticsService.GetAnalyticsAsync(query, ct);
        return Ok(MapToAnalyticsDto(analytics));
    }

    /// <summary>
    /// Create an A/B test
    /// </summary>
    [HttpPost("ab-tests")]
    [ProducesResponseType(typeof(ABTestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ABTestDto>> CreateABTest(
        [FromBody] CreatePromptABTestRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating A/B test: {Name}", request.Name);

        try
        {
            var test = await _abTestingService.CreateABTestAsync(
                request.Name,
                request.Description,
                request.TemplateIds,
                "user",
                ct);

            return CreatedAtAction(
                nameof(GetABTest),
                new { id = test.Id },
                MapToABTestDto(test));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateProblemDetails("Validation Error", ex.Message, 400));
        }
    }

    /// <summary>
    /// Get an A/B test
    /// </summary>
    [HttpGet("ab-tests/{id}")]
    [ProducesResponseType(typeof(ABTestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ABTestDto>> GetABTest(string id, CancellationToken ct)
    {
        var test = await _abTestingService.GetABTestResultsAsync(id, ct);
        if (test == null)
        {
            return NotFound(CreateProblemDetails("Not Found", $"A/B test {id} not found", 404));
        }

        return Ok(MapToABTestDto(test));
    }

    /// <summary>
    /// Run an A/B test
    /// </summary>
    [HttpPost("ab-tests/{id}/run")]
    [ProducesResponseType(typeof(ABTestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ABTestDto>> RunABTest(
        string id,
        [FromBody] RunABTestRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation("Running A/B test {Id}", id);

        if (_llmProvider == null)
        {
            return BadRequest(CreateProblemDetails(
                "Service Unavailable",
                "No LLM provider configured for A/B testing",
                400));
        }

        try
        {
            var test = await _abTestingService.RunABTestAsync(
                id,
                request.TestVariables,
                _llmProvider,
                request.Iterations,
                ct);

            return Ok(MapToABTestDto(test));
        }
        catch (ArgumentException ex)
        {
            return NotFound(CreateProblemDetails("Not Found", ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateProblemDetails("Invalid Operation", ex.Message, 400));
        }
    }

    /// <summary>
    /// Get A/B test summary statistics
    /// </summary>
    [HttpGet("ab-tests/{id}/summary")]
    [ProducesResponseType(typeof(Dictionary<string, ABTestSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Dictionary<string, ABTestSummaryDto>>> GetABTestSummary(
        string id,
        CancellationToken ct)
    {
        try
        {
            var summary = await _abTestingService.GetABTestSummaryAsync(id, ct);
            var dto = summary.ToDictionary(
                kvp => kvp.Key,
                kvp => MapToABTestSummaryDto(kvp.Value));

            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return NotFound(CreateProblemDetails("Not Found", ex.Message, 404));
        }
    }

    /// <summary>
    /// List all A/B tests
    /// </summary>
    [HttpGet("ab-tests")]
    [ProducesResponseType(typeof(List<ABTestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ABTestDto>>> ListABTests(
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var tests = await _abTestingService.ListABTestsAsync(ParseEnum<ABTestStatus>(status), ct);
        return Ok(tests.Select(MapToABTestDto).ToList());
    }

    /// <summary>
    /// Cancel a running A/B test
    /// </summary>
    [HttpPost("ab-tests/{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelABTest(string id, CancellationToken ct)
    {
        _logger.LogInformation("Cancelling A/B test {Id}", id);

        try
        {
            await _abTestingService.CancelABTestAsync(id, ct);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(CreateProblemDetails("Not Found", ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateProblemDetails("Invalid Operation", ex.Message, 400));
        }
    }

    private ProblemDetails CreateProblemDetails(string title, string detail, int status)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status,
            Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
        };
    }

    private T? ParseEnum<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return Enum.TryParse<T>(value, true, out var result) ? result : null;
    }

    private int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)(wordCount * 1.3);
    }

    private PromptTemplate MapToTemplate(CreatePromptTemplateRequest request)
    {
        return new PromptTemplate
        {
            Name = request.Name,
            Description = request.Description,
            PromptText = request.PromptText,
            Category = ParseEnum<PromptCategory>(request.Category) ?? PromptCategory.Custom,
            Stage = ParseEnum<PipelineStage>(request.Stage) ?? PipelineStage.Custom,
            TargetProvider = ParseEnum<TargetLlmProvider>(request.TargetProvider) ?? TargetLlmProvider.Any,
            Variables = request.Variables.Select(MapToVariable).ToList(),
            Tags = request.Tags
        };
    }

    private PromptTemplate MapToTemplateFromUpdate(UpdatePromptTemplateRequest request)
    {
        return new PromptTemplate
        {
            Name = request.Name,
            Description = request.Description,
            PromptText = request.PromptText,
            Variables = request.Variables.Select(MapToVariable).ToList(),
            Tags = request.Tags,
            Status = ParseEnum<TemplateStatus>(request.Status) ?? TemplateStatus.Active
        };
    }

    private PromptVariable MapToVariable(PromptVariableDto dto)
    {
        return new PromptVariable
        {
            Name = dto.Name,
            Description = dto.Description,
            Type = ParseEnum<VariableType>(dto.Type) ?? VariableType.String,
            Required = dto.Required,
            DefaultValue = dto.DefaultValue,
            ExampleValue = dto.ExampleValue,
            MinLength = dto.MinLength,
            MaxLength = dto.MaxLength,
            FormatPattern = dto.FormatPattern,
            AllowedValues = dto.AllowedValues
        };
    }

    private PromptTemplateDto MapToDto(PromptTemplate template)
    {
        return new PromptTemplateDto(
            template.Id,
            template.Name,
            template.Description,
            template.PromptText,
            template.Category.ToString(),
            template.Stage.ToString(),
            template.Source.ToString(),
            template.TargetProvider.ToString(),
            template.Status.ToString(),
            template.Variables.Select(MapToVariableDto).ToList(),
            template.Tags,
            template.CreatedBy,
            template.CreatedAt,
            template.ModifiedAt,
            template.ModifiedBy,
            template.Version,
            template.ParentTemplateId,
            template.IsDefault,
            MapToMetricsDto(template.Metrics));
    }

    private PromptVariableDto MapToVariableDto(PromptVariable variable)
    {
        return new PromptVariableDto(
            variable.Name,
            variable.Description,
            variable.Type.ToString(),
            variable.Required,
            variable.DefaultValue,
            variable.ExampleValue,
            variable.MinLength,
            variable.MaxLength,
            variable.FormatPattern,
            variable.AllowedValues);
    }

    private PromptPerformanceMetricsDto MapToMetricsDto(PromptPerformanceMetrics metrics)
    {
        return new PromptPerformanceMetricsDto(
            metrics.UsageCount,
            metrics.AverageQualityScore,
            metrics.AverageGenerationTimeMs,
            metrics.AverageTokenUsage,
            metrics.SuccessRate,
            metrics.ThumbsUpCount,
            metrics.ThumbsDownCount,
            metrics.LastUsedAt);
    }

    private PromptTemplateVersionDto MapToVersionDto(PromptTemplateVersion version)
    {
        return new PromptTemplateVersionDto(
            version.Id,
            version.TemplateId,
            version.VersionNumber,
            version.PromptText,
            version.ChangeNotes,
            version.ChangedBy,
            version.ChangedAt);
    }

    private TestPromptResultDto MapToTestResultDto(PromptTestResult result)
    {
        return new TestPromptResultDto(
            result.TemplateId,
            result.Success,
            result.GeneratedContent,
            result.ErrorMessage,
            result.GenerationTimeMs,
            result.TokensUsed,
            result.ExecutedAt,
            result.ResolvedPrompt);
    }

    private ABTestDto MapToABTestDto(PromptABTest test)
    {
        return new ABTestDto(
            test.Id,
            test.Name,
            test.Description,
            test.TemplateIds,
            test.Status.ToString(),
            test.StartedAt,
            test.CompletedAt,
            test.CreatedBy,
            test.CreatedAt,
            test.Results.Select(MapToABTestResultDto).ToList(),
            test.WinningTemplateId);
    }

    private ABTestResultDto MapToABTestResultDto(ABTestResult result)
    {
        return new ABTestResultDto(
            result.TemplateId,
            result.TemplateName,
            result.QualityScore,
            result.GenerationTimeMs,
            result.TokenUsage,
            result.Success,
            result.ExecutedAt);
    }

    private ABTestSummaryDto MapToABTestSummaryDto(ABTestSummary summary)
    {
        return new ABTestSummaryDto(
            summary.TemplateId,
            summary.TemplateName,
            summary.AverageQualityScore,
            summary.AverageGenerationTimeMs,
            summary.AverageTokenUsage,
            summary.SuccessRate,
            summary.TotalRuns);
    }

    private PromptAnalyticsDto MapToAnalyticsDto(PromptAnalytics analytics)
    {
        return new PromptAnalyticsDto(
            analytics.TotalTemplates,
            analytics.ActiveTemplates,
            analytics.TotalUsages,
            analytics.AverageQualityScore,
            analytics.AverageSuccessRate,
            analytics.TopPerformingTemplates.Select(MapToUsageStatsDto).ToList(),
            analytics.MostUsedTemplates.Select(MapToUsageStatsDto).ToList(),
            analytics.TemplatesByCategory.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
            analytics.AverageScoresByStage.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value));
    }

    private TemplateUsageStatsDto MapToUsageStatsDto(TemplateUsageStats stats)
    {
        return new TemplateUsageStatsDto(
            stats.TemplateId,
            stats.TemplateName,
            stats.UsageCount,
            stats.QualityScore,
            stats.SuccessRate,
            stats.TokenUsage);
    }
}
