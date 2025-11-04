using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.AI.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for LLM router operations including provider selection, health status, and metrics.
/// </summary>
[ApiController]
[Route("api/router")]
[Produces("application/json")]
public class LlmRouterController : ControllerBase
{
    private readonly ILogger<LlmRouterController> _logger;
    private readonly ILlmRouterService _routerService;

    public LlmRouterController(
        ILogger<LlmRouterController> logger,
        ILlmRouterService routerService)
    {
        _logger = logger;
        _routerService = routerService;
    }

    /// <summary>
    /// Select the best provider for a given task type and constraints.
    /// </summary>
    [HttpPost("select")]
    [ProducesResponseType(typeof(RoutingDecisionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RoutingDecisionDto>> SelectProvider(
        [FromBody] SelectProviderRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Selecting provider for task type: {TaskType}", request.TaskType);

            if (!Enum.TryParse<TaskType>(request.TaskType, ignoreCase: true, out var taskType))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid task type",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"Task type '{request.TaskType}' is not valid. Valid values: {string.Join(", ", Enum.GetNames<TaskType>())}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var constraints = new RoutingConstraints(
                RequiredContextLength: request.RequiredContextLength ?? 4096,
                MaxLatencyMs: request.MaxLatencyMs ?? 30000,
                MaxCostPerRequest: request.MaxCostPerRequest ?? 0.10m,
                RequireDeterminism: request.RequireDeterminism ?? false,
                MinQualityScore: request.MinQualityScore ?? 0.7);

            var decision = await _routerService.SelectProviderAsync(taskType, constraints, ct);

            var dto = new RoutingDecisionDto(
                ProviderName: decision.ProviderName,
                ModelName: decision.ModelName,
                Reasoning: decision.Reasoning,
                DecisionTime: decision.DecisionTime,
                Metadata: new RoutingMetadataDto(
                    Rank: decision.Metadata.Rank,
                    HealthScore: decision.Metadata.HealthScore,
                    LatencyScore: decision.Metadata.LatencyScore,
                    CostScore: decision.Metadata.CostScore,
                    QualityScore: decision.Metadata.QualityScore,
                    OverallScore: decision.Metadata.OverallScore,
                    AlternativeProviders: decision.Metadata.AlternativeProviders));

            _logger.LogInformation(
                "Selected provider {Provider}:{Model} for {TaskType}. CorrelationId: {CorrelationId}",
                decision.ProviderName,
                decision.ModelName,
                request.TaskType,
                HttpContext.TraceIdentifier);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Provider selection failed for task type: {TaskType}", request.TaskType);
            return BadRequest(new ProblemDetails
            {
                Title = "Provider selection failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting provider for task type: {TaskType}", request.TaskType);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while selecting a provider",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get health status for all providers.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(RouterProviderHealthDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RouterProviderHealthDto[]>> GetHealthStatus(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching provider health status");

            var healthStatuses = await _routerService.GetHealthStatusAsync(ct);

            var dtos = healthStatuses.Select(h => new RouterProviderHealthDto(
                ProviderName: h.ProviderName,
                State: h.State.ToString(),
                LastCheckTime: h.LastCheckTime,
                ConsecutiveFailures: h.ConsecutiveFailures,
                TotalRequests: h.TotalRequests,
                SuccessfulRequests: h.SuccessfulRequests,
                FailedRequests: h.FailedRequests,
                SuccessRate: h.SuccessRate,
                AverageLatencyMs: h.AverageLatencyMs,
                HealthScore: h.HealthScore,
                CircuitOpenedAt: h.CircuitOpenedAt,
                CircuitResetInSeconds: h.CircuitResetIn.HasValue ? (int)h.CircuitResetIn.Value.TotalSeconds : null
            )).ToArray();

            _logger.LogInformation("Retrieved health status for {Count} providers", dtos.Length);

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching provider health status");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while fetching provider health status",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get performance metrics for all providers.
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(RouterProviderMetricsDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RouterProviderMetricsDto[]>> GetMetrics(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Fetching provider metrics");

            var metrics = await _routerService.GetMetricsAsync(ct);

            var dtos = metrics.Select(m => new RouterProviderMetricsDto(
                ProviderName: m.ProviderName,
                ModelName: m.ModelName,
                AverageLatencyMs: m.AverageLatencyMs,
                P95LatencyMs: m.P95LatencyMs,
                P99LatencyMs: m.P99LatencyMs,
                AverageCost: m.AverageCost,
                QualityScore: m.QualityScore,
                RequestCount: m.RequestCount,
                LastUpdated: m.LastUpdated
            )).ToArray();

            _logger.LogInformation("Retrieved metrics for {Count} providers", dtos.Length);

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching provider metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while fetching provider metrics",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Record the result of a provider request for metrics tracking.
    /// </summary>
    [HttpPost("record")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RecordRequest(
        [FromBody] RecordRequestRequest request,
        CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<TaskType>(request.TaskType, ignoreCase: true, out var taskType))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid task type",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"Task type '{request.TaskType}' is not valid",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            await _routerService.RecordRequestAsync(
                request.ProviderName,
                taskType,
                request.Success,
                request.LatencyMs,
                request.Cost,
                ct);

            _logger.LogInformation(
                "Recorded request for {Provider}: success={Success}, latency={Latency}ms, cost=${Cost}",
                request.ProviderName,
                request.Success,
                request.LatencyMs,
                request.Cost);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording request for provider: {Provider}", request.ProviderName);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while recording the request",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Manually mark a provider as unavailable.
    /// </summary>
    [HttpPost("mark-unavailable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkProviderUnavailable(
        [FromBody] MarkProviderUnavailableRequest request,
        CancellationToken ct)
    {
        try
        {
            TimeSpan? duration = request.DurationSeconds.HasValue 
                ? TimeSpan.FromSeconds(request.DurationSeconds.Value) 
                : null;

            await _routerService.MarkProviderUnavailableAsync(request.ProviderName, duration, ct);

            _logger.LogInformation(
                "Marked provider {Provider} as unavailable for {Duration}s",
                request.ProviderName,
                request.DurationSeconds ?? 60);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking provider unavailable: {Provider}", request.ProviderName);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while marking the provider unavailable",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Reset a provider to healthy state.
    /// </summary>
    [HttpPost("reset/{providerName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetProviderHealth(
        string providerName,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid provider name",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Provider name cannot be empty",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            await _routerService.ResetProviderHealthAsync(providerName, ct);

            _logger.LogInformation("Reset provider {Provider} to healthy state", providerName);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting provider health: {Provider}", providerName);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An error occurred while resetting provider health",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }
}
