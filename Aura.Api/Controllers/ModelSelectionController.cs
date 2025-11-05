using Aura.Core.AI.Adapters;
using Aura.Core.Services.ModelSelection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Controllers;

/// <summary>
/// API for managing model selection with explicit user control
/// </summary>
[ApiController]
[Route("api/models")]
public class ModelSelectionController : ControllerBase
{
    private readonly ILogger<ModelSelectionController> _logger;
    private readonly ModelSelectionService _selectionService;
    private readonly ModelCatalog _modelCatalog;

    public ModelSelectionController(
        ILogger<ModelSelectionController> logger,
        ModelSelectionService selectionService,
        ModelCatalog modelCatalog)
    {
        _logger = logger;
        _selectionService = selectionService;
        _modelCatalog = modelCatalog;
    }

    /// <summary>
    /// Get all available models with capabilities
    /// </summary>
    [HttpGet("available")]
    public IActionResult GetAvailableModels([FromQuery] string? provider = null)
    {
        try
        {
            _logger.LogInformation("Getting available models for provider: {Provider}", provider ?? "all");

            var providers = string.IsNullOrWhiteSpace(provider)
                ? new[] { "OpenAI", "Anthropic", "Gemini", "Azure", "Ollama" }
                : new[] { provider };

            var modelsByProvider = new Dictionary<string, List<ModelInfoDto>>();

            foreach (var prov in providers)
            {
                var models = _modelCatalog.GetAllModels(prov);
                var modelDtos = models.Select(m => new ModelInfoDto
                {
                    Provider = m.Provider,
                    ModelId = m.ModelId,
                    MaxTokens = m.MaxTokens,
                    ContextWindow = m.ContextWindow,
                    Aliases = m.Aliases?.ToList() ?? new List<string>(),
                    IsDeprecated = m.DeprecationDate.HasValue && m.DeprecationDate.Value <= DateTime.UtcNow,
                    DeprecationDate = m.DeprecationDate,
                    ReplacementModel = m.ReplacementModel
                }).ToList();

                if (modelDtos.Any())
                {
                    modelsByProvider[prov] = modelDtos;
                }
            }

            return Ok(new
            {
                providers = modelsByProvider,
                totalCount = modelsByProvider.Values.Sum(list => list.Count),
                correlationId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available models");
            return StatusCode(500, new
            {
                error = "Failed to retrieve available models",
                detail = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get current model selections (defaults, overrides, pins)
    /// </summary>
    [HttpGet("selection")]
    public async Task<IActionResult> GetSelections(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting model selections");

            var state = await _selectionService.GetAllSelectionsAsync(ct);

            return Ok(new
            {
                globalDefaults = state.GlobalDefaults.Select(s => new ModelSelectionDto
                {
                    Provider = s.Provider,
                    Stage = s.Stage,
                    ModelId = s.ModelId,
                    Scope = s.Scope.ToString(),
                    IsPinned = s.IsPinned,
                    SetBy = s.SetBy,
                    SetAt = s.SetAt,
                    Reason = s.Reason
                }),
                projectOverrides = state.ProjectOverrides.Select(s => new ModelSelectionDto
                {
                    Provider = s.Provider,
                    Stage = s.Stage,
                    ModelId = s.ModelId,
                    Scope = s.Scope.ToString(),
                    IsPinned = s.IsPinned,
                    SetBy = s.SetBy,
                    SetAt = s.SetAt,
                    Reason = s.Reason
                }),
                stageSelections = state.StageSelections.Select(s => new ModelSelectionDto
                {
                    Provider = s.Provider,
                    Stage = s.Stage,
                    ModelId = s.ModelId,
                    Scope = s.Scope.ToString(),
                    IsPinned = s.IsPinned,
                    SetBy = s.SetBy,
                    SetAt = s.SetAt,
                    Reason = s.Reason
                }),
                allowAutomaticFallback = state.AllowAutomaticFallback,
                correlationId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model selections");
            return StatusCode(500, new
            {
                error = "Failed to retrieve model selections",
                detail = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Set model selection with optional pin
    /// </summary>
    [HttpPost("selection")]
    public async Task<IActionResult> SetSelection(
        [FromBody] SetModelSelectionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.ModelId))
            {
                return BadRequest(new
                {
                    error = "Provider and ModelId are required",
                    correlationId = HttpContext.TraceIdentifier
                });
            }

            if (!Enum.TryParse<ModelSelectionScope>(request.Scope, true, out var scope))
            {
                return BadRequest(new
                {
                    error = $"Invalid scope: {request.Scope}. Must be: Global, Project, Stage, or Run",
                    correlationId = HttpContext.TraceIdentifier
                });
            }

            _logger.LogInformation(
                "Setting model selection: {Provider}/{Stage} -> {ModelId} (scope: {Scope}, pin: {Pin})",
                request.Provider, request.Stage, request.ModelId, scope, request.Pin);

            var result = await _selectionService.SetModelSelectionAsync(
                request.Provider,
                request.Stage,
                request.ModelId,
                scope,
                request.Pin,
                request.SetBy ?? "user",
                request.Reason,
                ct);

            if (!result.Applied)
            {
                return BadRequest(new
                {
                    applied = false,
                    reason = result.Reason,
                    recommended = result.Recommended,
                    correlationId = HttpContext.TraceIdentifier
                });
            }

            return Ok(new
            {
                applied = true,
                reason = result.Reason,
                deprecationWarning = result.DeprecationWarning,
                correlationId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set model selection");
            return StatusCode(500, new
            {
                error = "Failed to set model selection",
                detail = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Clear model selections by scope
    /// </summary>
    [HttpPost("selection/clear")]
    public async Task<IActionResult> ClearSelections(
        [FromBody] ClearModelSelectionsRequest request,
        CancellationToken ct = default)
    {
        try
        {
            ModelSelectionScope? scope = null;
            if (!string.IsNullOrWhiteSpace(request.Scope))
            {
                if (!Enum.TryParse<ModelSelectionScope>(request.Scope, true, out var parsedScope))
                {
                    return BadRequest(new
                    {
                        error = $"Invalid scope: {request.Scope}",
                        correlationId = HttpContext.TraceIdentifier
                    });
                }
                scope = parsedScope;
            }

            _logger.LogInformation(
                "Clearing selections: provider={Provider}, stage={Stage}, scope={Scope}",
                request.Provider, request.Stage, scope);

            await _selectionService.ClearSelectionsAsync(
                request.Provider,
                request.Stage,
                scope,
                ct);

            return Ok(new
            {
                success = true,
                message = "Selections cleared successfully",
                correlationId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear model selections");
            return StatusCode(500, new
            {
                error = "Failed to clear selections",
                detail = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Test a specific model with lightweight probe
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestModel(
        [FromBody] TestModelRequest request,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Provider) || 
                string.IsNullOrWhiteSpace(request.ModelId) ||
                string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new
                {
                    error = "Provider, ModelId, and ApiKey are required",
                    correlationId = HttpContext.TraceIdentifier
                });
            }

            _logger.LogInformation("Testing model {Provider}:{ModelId}", request.Provider, request.ModelId);

            var result = await _modelCatalog.TestModelAsync(
                request.Provider,
                request.ModelId,
                request.ApiKey,
                ct);

            return Ok(new
            {
                provider = result.Provider,
                modelId = result.ModelId,
                isAvailable = result.IsAvailable,
                isDeprecated = result.IsDeprecated,
                replacementModel = result.ReplacementModel,
                contextWindow = result.ContextWindow,
                maxTokens = result.MaxTokens,
                errorMessage = result.ErrorMessage,
                testedAt = result.TestedAt,
                correlationId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test model");
            return StatusCode(500, new
            {
                error = "Failed to test model",
                detail = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get audit log of model selection resolutions
    /// </summary>
    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int? limit = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting model selection audit log, limit: {Limit}", limit);

            var auditLog = await _selectionService.GetAuditLogAsync(limit, ct);

            return Ok(new
            {
                entries = auditLog.Select(a => new
                {
                    provider = a.Provider,
                    stage = a.Stage,
                    modelId = a.ModelId,
                    source = a.Source,
                    reasoning = a.Reasoning,
                    isPinned = a.IsPinned,
                    isBlocked = a.IsBlocked,
                    blockReason = a.BlockReason,
                    timestamp = a.Timestamp,
                    jobId = a.JobId
                }),
                totalCount = auditLog.Count,
                correlationId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit log");
            return StatusCode(500, new
            {
                error = "Failed to retrieve audit log",
                detail = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Explain model choice comparison between selected and recommended models
    /// </summary>
    [HttpPost("explain-choice")]
    public async Task<IActionResult> ExplainChoice(
        [FromBody] ExplainChoiceRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Explaining model choice: {Provider}/{Stage}, selected={Selected}",
                request.Provider, request.Stage, request.SelectedModelId);

            var explanation = await _selectionService.ExplainModelChoiceAsync(
                request.Provider,
                request.Stage,
                request.SelectedModelId,
                ct);

            return Ok(new
            {
                selectedModel = new
                {
                    modelId = explanation.SelectedModel.ModelId,
                    provider = explanation.SelectedModel.Provider,
                    maxTokens = explanation.SelectedModel.MaxTokens,
                    contextWindow = explanation.SelectedModel.ContextWindow,
                    isDeprecated = explanation.SelectedModel.IsDeprecated
                },
                recommendedModel = explanation.RecommendedModel != null ? new
                {
                    modelId = explanation.RecommendedModel.ModelId,
                    provider = explanation.RecommendedModel.Provider,
                    maxTokens = explanation.RecommendedModel.MaxTokens,
                    contextWindow = explanation.RecommendedModel.ContextWindow,
                    isDeprecated = explanation.RecommendedModel.IsDeprecated
                } : null,
                comparison = new
                {
                    selectedIsRecommended = explanation.SelectedIsRecommended,
                    reasoning = explanation.Reasoning,
                    tradeoffs = explanation.Tradeoffs,
                    suggestions = explanation.Suggestions
                },
                correlationId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to explain model choice");
            return StatusCode(500, new
            {
                error = "Failed to explain model choice",
                detail = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get deprecation status for models
    /// </summary>
    [HttpGet("deprecation-status")]
    public IActionResult GetDeprecationStatus([FromQuery] string? provider = null)
    {
        try
        {
            var providers = string.IsNullOrWhiteSpace(provider)
                ? new[] { "OpenAI", "Anthropic", "Gemini", "Azure", "Ollama" }
                : new[] { provider };

            var deprecatedModels = new List<DeprecationStatusDto>();

            foreach (var prov in providers)
            {
                var models = _modelCatalog.GetAllModels(prov);
                foreach (var model in models.Where(m => m.DeprecationDate.HasValue))
                {
                    deprecatedModels.Add(new DeprecationStatusDto
                    {
                        Provider = model.Provider,
                        ModelId = model.ModelId,
                        DeprecationDate = model.DeprecationDate!.Value,
                        IsCurrentlyDeprecated = model.DeprecationDate.Value <= DateTime.UtcNow,
                        ReplacementModel = model.ReplacementModel,
                        Message = model.DeprecationDate.Value <= DateTime.UtcNow
                            ? $"Model is deprecated. Use {model.ReplacementModel ?? "an alternative"} instead."
                            : $"Model will be deprecated on {model.DeprecationDate.Value:yyyy-MM-dd}."
                    });
                }
            }

            return Ok(new
            {
                deprecatedModels,
                totalCount = deprecatedModels.Count,
                correlationId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get deprecation status");
            return StatusCode(500, new
            {
                error = "Failed to get deprecation status",
                detail = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }
}

// DTOs
public class ModelInfoDto
{
    public required string Provider { get; set; }
    public required string ModelId { get; set; }
    public int MaxTokens { get; set; }
    public int ContextWindow { get; set; }
    public List<string> Aliases { get; set; } = new();
    public bool IsDeprecated { get; set; }
    public DateTime? DeprecationDate { get; set; }
    public string? ReplacementModel { get; set; }
}

public class ModelSelectionDto
{
    public required string Provider { get; set; }
    public required string Stage { get; set; }
    public required string ModelId { get; set; }
    public required string Scope { get; set; }
    public bool IsPinned { get; set; }
    public required string SetBy { get; set; }
    public DateTime SetAt { get; set; }
    public required string Reason { get; set; }
}

public class DeprecationStatusDto
{
    public required string Provider { get; set; }
    public required string ModelId { get; set; }
    public DateTime DeprecationDate { get; set; }
    public bool IsCurrentlyDeprecated { get; set; }
    public string? ReplacementModel { get; set; }
    public required string Message { get; set; }
}

public class SetModelSelectionRequest
{
    public required string Provider { get; set; }
    public string? Stage { get; set; }
    public required string ModelId { get; set; }
    public required string Scope { get; set; }
    public bool Pin { get; set; }
    public string? SetBy { get; set; }
    public string? Reason { get; set; }
}

public class ClearModelSelectionsRequest
{
    public string? Provider { get; set; }
    public string? Stage { get; set; }
    public string? Scope { get; set; }
}

public class TestModelRequest
{
    public required string Provider { get; set; }
    public required string ModelId { get; set; }
    public required string ApiKey { get; set; }
}

public class ExplainChoiceRequest
{
    public required string Provider { get; set; }
    public required string Stage { get; set; }
    public required string SelectedModelId { get; set; }
}
