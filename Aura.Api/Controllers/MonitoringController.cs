using Aura.Core.Monitoring;
using Microsoft.AspNetCore.Mvc;

namespace Aura.Api.Controllers;

/// <summary>
/// Monitoring and metrics endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly MetricsCollector _metrics;
    private readonly AlertingEngine _alerting;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        MetricsCollector metrics,
        AlertingEngine alerting,
        ILogger<MonitoringController> logger)
    {
        _metrics = metrics;
        _alerting = alerting;
        _logger = logger;
    }

    /// <summary>
    /// Get current metrics snapshot
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(MetricsSnapshot), StatusCodes.Status200OK)]
    public ActionResult<MetricsSnapshot> GetMetrics()
    {
        try
        {
            var snapshot = _metrics.GetSnapshot();
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics snapshot");
            return Problem(
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title: "Metrics Error",
                detail: "Failed to retrieve metrics",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Get current alert states
    /// </summary>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(Dictionary<string, AlertState>), StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, AlertState>> GetAlerts()
    {
        try
        {
            var states = _alerting.GetAlertStates();
            return Ok(states);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alert states");
            return Problem(
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title: "Alerts Error",
                detail: "Failed to retrieve alert states",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Get firing alerts only
    /// </summary>
    [HttpGet("alerts/firing")]
    [ProducesResponseType(typeof(Dictionary<string, AlertState>), StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, AlertState>> GetFiringAlerts()
    {
        try
        {
            var states = _alerting.GetAlertStates();
            var firing = states.Where(kvp => kvp.Value.Firing)
                               .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return Ok(firing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get firing alerts");
            return Problem(
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title: "Alerts Error",
                detail: "Failed to retrieve firing alerts",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Get specific metric value
    /// </summary>
    [HttpGet("metrics/gauge/{name}")]
    [ProducesResponseType(typeof(double), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<double> GetGaugeValue(string name)
    {
        try
        {
            var value = _metrics.GetGaugeValue(name);
            if (value == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Metric Not Found",
                    detail = $"Gauge metric '{name}' not found",
                    status = StatusCodes.Status404NotFound
                });
            }

            return Ok(value.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get gauge value for {MetricName}", name);
            return Problem(
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title: "Metrics Error",
                detail: "Failed to retrieve metric value",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Get histogram statistics for a metric
    /// </summary>
    [HttpGet("metrics/histogram/{name}")]
    [ProducesResponseType(typeof(HistogramStats), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<HistogramStats> GetHistogramStats(string name)
    {
        try
        {
            var stats = _metrics.GetHistogramStats(name);
            if (stats == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Metric Not Found",
                    detail = $"Histogram metric '{name}' not found",
                    status = StatusCodes.Status404NotFound
                });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get histogram stats for {MetricName}", name);
            return Problem(
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Metrics Error",
                detail: "Failed to retrieve histogram statistics",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Health check endpoint for synthetic monitoring
    /// </summary>
    [HttpGet("health/synthetic")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetSyntheticHealth()
    {
        try
        {
            // This endpoint is specifically for external synthetic monitoring tools
            var snapshot = _metrics.GetSnapshot();
            var alerts = _alerting.GetAlertStates();
            var firingCount = alerts.Count(kvp => kvp.Value.Firing);

            return Ok(new
            {
                status = firingCount > 0 ? "degraded" : "healthy",
                timestamp = DateTimeOffset.UtcNow,
                metrics = new
                {
                    gauges = snapshot.Gauges.Count,
                    counters = snapshot.Counters.Count,
                    histograms = snapshot.Histograms.Count
                },
                alerts = new
                {
                    total = alerts.Count,
                    firing = firingCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Synthetic health check failed");
            return Problem(
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title: "Health Check Error",
                detail: "Synthetic health check failed",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}
