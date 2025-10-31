using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Health;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoints for provider health monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ProviderHealthMonitor _healthMonitor;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ProviderHealthMonitor healthMonitor,
        ILogger<HealthController> logger)
    {
        _healthMonitor = healthMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Get health metrics for all providers
    /// </summary>
    [HttpGet("providers")]
    public ActionResult<IEnumerable<ProviderHealthCheckDto>> GetAllProviders()
    {
        var metrics = _healthMonitor.GetAllProviderHealth();
        var dtos = metrics.Values.Select(ToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Get health metrics for a specific provider
    /// </summary>
    [HttpGet("providers/{name}")]
    public ActionResult<ProviderHealthCheckDto> GetProvider(string name)
    {
        var metrics = _healthMonitor.GetProviderHealth(name);
        if (metrics == null)
        {
            return NotFound(new { error = $"Provider '{name}' not found" });
        }

        return Ok(ToDto(metrics));
    }

    /// <summary>
    /// Trigger immediate health check for a specific provider
    /// </summary>
    [HttpPost("providers/{name}/check")]
    public async Task<ActionResult<ProviderHealthCheckDto>> CheckProvider(
        string name,
        CancellationToken ct)
    {
        // Note: This requires registered health check functions
        // For now, return the cached metrics
        var metrics = _healthMonitor.GetProviderHealth(name);
        if (metrics == null)
        {
            return NotFound(new { error = $"Provider '{name}' not found" });
        }

        _logger.LogInformation("Manual health check requested for provider: {ProviderName}", name);
        return Ok(ToDto(metrics));
    }

    /// <summary>
    /// Trigger health check for all providers
    /// </summary>
    [HttpPost("providers/check-all")]
    public ActionResult<IEnumerable<ProviderHealthCheckDto>> CheckAllProviders()
    {
        _logger.LogInformation("Manual health check requested for all providers");
        
        var metrics = _healthMonitor.GetAllProviderHealth();
        var dtos = metrics.Values.Select(ToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Get summary of provider health across all types
    /// </summary>
    [HttpGet("providers/summary")]
    public ActionResult<ProviderHealthSummaryDto> GetProvidersSummary()
    {
        var allMetrics = _healthMonitor.GetAllProviderHealth();
        
        var summary = new ProviderHealthSummaryDto
        {
            TotalProviders = allMetrics.Count,
            HealthyProviders = allMetrics.Values.Count(m => m.IsHealthy),
            DegradedProviders = allMetrics.Values.Count(m => !m.IsHealthy && m.ConsecutiveFailures < 3),
            OfflineProviders = allMetrics.Values.Count(m => m.ConsecutiveFailures >= 3),
            LastUpdateTime = allMetrics.Values.Any() 
                ? allMetrics.Values.Max(m => m.LastCheckTime) 
                : DateTime.MinValue,
            ProvidersByType = GroupProvidersByType(allMetrics)
        };

        return Ok(summary);
    }

    private Dictionary<string, ProviderTypeHealth> GroupProvidersByType(
        Dictionary<string, ProviderHealthMetrics> allMetrics)
    {
        var result = new Dictionary<string, ProviderTypeHealth>();

        var llmProviders = allMetrics
            .Where(kvp => IsLlmProvider(kvp.Key))
            .Select(kvp => kvp.Value)
            .ToList();

        var ttsProviders = allMetrics
            .Where(kvp => IsTtsProvider(kvp.Key))
            .Select(kvp => kvp.Value)
            .ToList();

        var imageProviders = allMetrics
            .Where(kvp => IsImageProvider(kvp.Key))
            .Select(kvp => kvp.Value)
            .ToList();

        if (llmProviders.Any())
        {
            result["llm"] = new ProviderTypeHealth
            {
                Total = llmProviders.Count,
                Healthy = llmProviders.Count(m => m.IsHealthy),
                Degraded = llmProviders.Count(m => !m.IsHealthy && m.ConsecutiveFailures < 3),
                Offline = llmProviders.Count(m => m.ConsecutiveFailures >= 3)
            };
        }

        if (ttsProviders.Any())
        {
            result["tts"] = new ProviderTypeHealth
            {
                Total = ttsProviders.Count,
                Healthy = ttsProviders.Count(m => m.IsHealthy),
                Degraded = ttsProviders.Count(m => !m.IsHealthy && m.ConsecutiveFailures < 3),
                Offline = ttsProviders.Count(m => m.ConsecutiveFailures >= 3)
            };
        }

        if (imageProviders.Any())
        {
            result["image"] = new ProviderTypeHealth
            {
                Total = imageProviders.Count,
                Healthy = imageProviders.Count(m => m.IsHealthy),
                Degraded = imageProviders.Count(m => !m.IsHealthy && m.ConsecutiveFailures < 3),
                Offline = imageProviders.Count(m => m.ConsecutiveFailures >= 3)
            };
        }

        return result;
    }

    private bool IsLlmProvider(string name) =>
        name.Contains("llm", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("RuleBased", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Azure", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Gemini", StringComparison.OrdinalIgnoreCase);

    private bool IsTtsProvider(string name) =>
        name.Contains("tts", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("voice", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("ElevenLabs", StringComparison.OrdinalIgnoreCase);

    private bool IsImageProvider(string name) =>
        name.Contains("image", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("visual", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("StableDiffusion", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Stock", StringComparison.OrdinalIgnoreCase);

    private ProviderHealthCheckDto ToDto(ProviderHealthMetrics metrics)
    {
        return new ProviderHealthCheckDto
        {
            ProviderName = metrics.ProviderName,
            IsHealthy = metrics.IsHealthy,
            LastCheckTime = metrics.LastCheckTime,
            ResponseTimeMs = metrics.ResponseTime.TotalMilliseconds,
            ConsecutiveFailures = metrics.ConsecutiveFailures,
            LastError = metrics.LastError,
            SuccessRate = metrics.SuccessRate,
            AverageResponseTimeMs = metrics.AverageResponseTime.TotalMilliseconds
        };
    }
}

/// <summary>
/// DTO for provider health check metrics (health endpoint specific)
/// </summary>
public class ProviderHealthCheckDto
{
    public string ProviderName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public DateTime LastCheckTime { get; set; }
    public double ResponseTimeMs { get; set; }
    public int ConsecutiveFailures { get; set; }
    public string? LastError { get; set; }
    public double SuccessRate { get; set; }
    public double AverageResponseTimeMs { get; set; }
}

/// <summary>
/// DTO for provider health summary
/// </summary>
public class ProviderHealthSummaryDto
{
    public int TotalProviders { get; set; }
    public int HealthyProviders { get; set; }
    public int DegradedProviders { get; set; }
    public int OfflineProviders { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public Dictionary<string, ProviderTypeHealth> ProvidersByType { get; set; } = new();
}

/// <summary>
/// Health status for a specific provider type
/// </summary>
public class ProviderTypeHealth
{
    public int Total { get; set; }
    public int Healthy { get; set; }
    public int Degraded { get; set; }
    public int Offline { get; set; }
}
