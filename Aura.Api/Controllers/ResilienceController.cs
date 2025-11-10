using Aura.Core.Resilience;
using Aura.Core.Resilience.ErrorTracking;
using Aura.Core.Resilience.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace Aura.Api.Controllers;

/// <summary>
/// Provides endpoints for monitoring resilience health and circuit breaker states
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ResilienceController : ControllerBase
{
    private readonly ILogger<ResilienceController> _logger;
    private readonly ResilienceHealthMonitor _healthMonitor;
    private readonly CircuitBreakerStateManager _circuitBreakerManager;
    private readonly ErrorMetricsCollector _metricsCollector;

    public ResilienceController(
        ILogger<ResilienceController> logger,
        ResilienceHealthMonitor healthMonitor,
        CircuitBreakerStateManager circuitBreakerManager,
        ErrorMetricsCollector metricsCollector)
    {
        _logger = logger;
        _healthMonitor = healthMonitor;
        _circuitBreakerManager = circuitBreakerManager;
        _metricsCollector = metricsCollector;
    }

    /// <summary>
    /// Gets overall resilience health status
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ResilienceHealthReport), StatusCodes.Status200OK)]
    public ActionResult<ResilienceHealthReport> GetHealth()
    {
        var report = _healthMonitor.GetHealthReport();
        return Ok(report);
    }

    /// <summary>
    /// Gets all circuit breaker states
    /// </summary>
    [HttpGet("circuit-breakers")]
    [ProducesResponseType(typeof(Dictionary<string, CircuitBreakerState>), StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, CircuitBreakerState>> GetCircuitBreakers()
    {
        var states = _circuitBreakerManager.GetAllStates();
        return Ok(states);
    }

    /// <summary>
    /// Gets circuit breaker state for a specific service
    /// </summary>
    [HttpGet("circuit-breakers/{serviceName}")]
    [ProducesResponseType(typeof(CircuitBreakerState), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CircuitBreakerState> GetCircuitBreakerState(string serviceName)
    {
        var state = _circuitBreakerManager.GetState(serviceName);
        
        if (state == null)
        {
            return NotFound(new { message = $"No circuit breaker found for service '{serviceName}'" });
        }

        return Ok(state);
    }

    /// <summary>
    /// Gets error metrics for all services
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(Dictionary<string, ErrorMetrics>), StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, ErrorMetrics>> GetMetrics()
    {
        var metrics = _metricsCollector.GetAllMetrics();
        return Ok(metrics);
    }

    /// <summary>
    /// Gets error metrics for a specific service
    /// </summary>
    [HttpGet("metrics/{serviceName}")]
    [ProducesResponseType(typeof(ErrorMetrics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ErrorMetrics> GetServiceMetrics(string serviceName)
    {
        var metrics = _metricsCollector.GetMetrics(serviceName);
        
        if (metrics == null)
        {
            return NotFound(new { message = $"No metrics found for service '{serviceName}'" });
        }

        return Ok(metrics);
    }

    /// <summary>
    /// Gets recent errors across all services
    /// </summary>
    [HttpGet("errors/recent")]
    [ProducesResponseType(typeof(IEnumerable<ErrorEventDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ErrorEventDto>> GetRecentErrors([FromQuery] int count = 50)
    {
        if (count <= 0 || count > 1000)
        {
            return BadRequest(new { message = "Count must be between 1 and 1000" });
        }

        var errors = _metricsCollector.GetRecentErrors(count)
            .Select(e => new ErrorEventDto
            {
                ServiceName = e.ServiceName,
                Category = e.Category.ToString(),
                Message = e.Exception.Message,
                ExceptionType = e.Exception.GetType().Name,
                Timestamp = e.Timestamp,
                CorrelationId = e.CorrelationId
            });

        return Ok(errors);
    }

    /// <summary>
    /// Gets error rate for a service
    /// </summary>
    [HttpGet("metrics/{serviceName}/error-rate")]
    [ProducesResponseType(typeof(ErrorRateResponse), StatusCodes.Status200OK)]
    public ActionResult<ErrorRateResponse> GetErrorRate(
        string serviceName,
        [FromQuery] int windowMinutes = 5)
    {
        if (windowMinutes <= 0 || windowMinutes > 1440)
        {
            return BadRequest(new { message = "Window must be between 1 and 1440 minutes (24 hours)" });
        }

        var window = TimeSpan.FromMinutes(windowMinutes);
        var errorRate = _metricsCollector.GetErrorRate(serviceName, window);

        return Ok(new ErrorRateResponse
        {
            ServiceName = serviceName,
            ErrorsPerMinute = errorRate,
            WindowMinutes = windowMinutes
        });
    }

    /// <summary>
    /// Gets active health alerts
    /// </summary>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<HealthAlert>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<HealthAlert>> GetAlerts()
    {
        var alerts = _healthMonitor.GetActiveAlerts();
        return Ok(alerts);
    }

    /// <summary>
    /// Resets metrics for a specific service
    /// </summary>
    [HttpPost("metrics/{serviceName}/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult ResetMetrics(string serviceName)
    {
        _metricsCollector.ResetMetrics(serviceName);
        
        _logger.LogInformation(
            "Metrics reset for service {ServiceName} by user request",
            serviceName);

        return Ok(new { message = $"Metrics reset for service '{serviceName}'" });
    }
}

/// <summary>
/// DTO for error events
/// </summary>
public class ErrorEventDto
{
    public required string ServiceName { get; init; }
    public required string Category { get; init; }
    public required string Message { get; init; }
    public required string ExceptionType { get; init; }
    public required DateTime Timestamp { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Response for error rate queries
/// </summary>
public class ErrorRateResponse
{
    public required string ServiceName { get; init; }
    public required double ErrorsPerMinute { get; init; }
    public required int WindowMinutes { get; init; }
}
