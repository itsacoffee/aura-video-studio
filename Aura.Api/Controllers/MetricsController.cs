using Aura.Api.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller to expose performance metrics
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly PerformanceMetrics _metrics;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(PerformanceMetrics metrics, ILogger<MetricsController> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Get all performance metrics
    /// </summary>
    [HttpGet]
    public ActionResult<Dictionary<string, EndpointMetrics>> GetMetrics()
    {
        try
        {
            var metrics = _metrics.GetMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics");
            return StatusCode(500, new { error = "Failed to retrieve metrics" });
        }
    }

    /// <summary>
    /// Get metrics for a specific endpoint
    /// </summary>
    [HttpGet("{endpoint}")]
    public ActionResult<EndpointMetrics> GetEndpointMetrics(string endpoint)
    {
        try
        {
            // Decode the endpoint parameter (e.g., "GET:/api/jobs" might be URL encoded)
            var decodedEndpoint = Uri.UnescapeDataString(endpoint);
            
            var metrics = _metrics.GetEndpointMetrics(decodedEndpoint);
            
            if (metrics == null)
            {
                return NotFound(new { error = $"No metrics found for endpoint: {decodedEndpoint}" });
            }
            
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics for endpoint: {Endpoint}", endpoint);
            return StatusCode(500, new { error = "Failed to retrieve endpoint metrics" });
        }
    }

    /// <summary>
    /// Reset all metrics (useful for testing)
    /// </summary>
    [HttpPost("reset")]
    public ActionResult ResetMetrics()
    {
        try
        {
            _metrics.Reset();
            _logger.LogInformation("Metrics reset via API request");
            return Ok(new { message = "All metrics have been reset" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting metrics");
            return StatusCode(500, new { error = "Failed to reset metrics" });
        }
    }

    /// <summary>
    /// Reset metrics for a specific endpoint
    /// </summary>
    [HttpPost("reset/{endpoint}")]
    public ActionResult ResetEndpointMetrics(string endpoint)
    {
        try
        {
            var decodedEndpoint = Uri.UnescapeDataString(endpoint);
            _metrics.ResetEndpoint(decodedEndpoint);
            _logger.LogInformation("Metrics reset for endpoint: {Endpoint}", decodedEndpoint);
            return Ok(new { message = $"Metrics for {decodedEndpoint} have been reset" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting metrics for endpoint: {Endpoint}", endpoint);
            return StatusCode(500, new { error = "Failed to reset endpoint metrics" });
        }
    }
}
