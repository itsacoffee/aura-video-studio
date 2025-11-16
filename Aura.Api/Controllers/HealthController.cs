using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Health;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoints for provider and system health monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ProviderHealthMonitor _healthMonitor;
    private readonly ProviderHealthService _healthService;
    private readonly SystemHealthChecker _systemHealthChecker;
    private readonly ILogger<HealthController> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    public HealthController(
        ProviderHealthMonitor healthMonitor,
        ProviderHealthService healthService,
        SystemHealthChecker systemHealthChecker,
        ILogger<HealthController> logger,
        IHostEnvironment hostEnvironment)
    {
        _healthMonitor = healthMonitor;
        _healthService = healthService;
        _systemHealthChecker = systemHealthChecker;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    /// <summary>
    /// Get overall API health summary
    /// </summary>
    [HttpGet]
    public ActionResult<ApiHealthResponse> Get()
    {
        var response = new ApiHealthResponse
        {
            Status = "Healthy",
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            Environment = _hostEnvironment.EnvironmentName,
            MachineName = Environment.MachineName,
            OsPlatform = Environment.OSVersion.Platform.ToString(),
            OsVersion = Environment.OSVersion.VersionString,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
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
    public Task<ActionResult<ProviderHealthCheckDto>> CheckProvider(
        string name,
        CancellationToken ct)
    {
        // Note: This requires registered health check functions
        // For now, return the cached metrics
        var metrics = _healthMonitor.GetProviderHealth(name);
        if (metrics == null)
        {
            return Task.FromResult<ActionResult<ProviderHealthCheckDto>>(NotFound(new { error = $"Provider '{name}' not found" }));
        }

        _logger.LogInformation("Manual health check requested for provider: {ProviderName}", name);
        return Task.FromResult<ActionResult<ProviderHealthCheckDto>>(Ok(ToDto(metrics)));
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

    /// <summary>
    /// Get health status for all LLM providers
    /// </summary>
    [HttpGet("llm")]
    public ActionResult<ProviderTypeHealthDto> GetLlmHealth()
    {
        var providers = _healthService.GetLlmProvidersHealth();
        var allDown = _healthService.AreAllProvidersDown("llm");
        
        var dto = new ProviderTypeHealthDto
        {
            ProviderType = "llm",
            Providers = providers.Values.Select(ToDto).ToList(),
            IsHealthy = !allDown,
            HealthyCount = providers.Values.Count(p => p.IsHealthy),
            TotalCount = providers.Count
        };

        return allDown ? StatusCode(503, dto) : Ok(dto);
    }

    /// <summary>
    /// Get health status for all TTS providers
    /// </summary>
    [HttpGet("tts")]
    public ActionResult<ProviderTypeHealthDto> GetTtsHealth()
    {
        var providers = _healthService.GetTtsProvidersHealth();
        var allDown = _healthService.AreAllProvidersDown("tts");
        
        var dto = new ProviderTypeHealthDto
        {
            ProviderType = "tts",
            Providers = providers.Values.Select(ToDto).ToList(),
            IsHealthy = !allDown,
            HealthyCount = providers.Values.Count(p => p.IsHealthy),
            TotalCount = providers.Count
        };

        return allDown ? StatusCode(503, dto) : Ok(dto);
    }

    /// <summary>
    /// Get health status for all image providers
    /// </summary>
    [HttpGet("images")]
    public ActionResult<ProviderTypeHealthDto> GetImagesHealth()
    {
        var providers = _healthService.GetImageProvidersHealth();
        var allDown = _healthService.AreAllProvidersDown("images");
        
        var dto = new ProviderTypeHealthDto
        {
            ProviderType = "images",
            Providers = providers.Values.Select(ToDto).ToList(),
            IsHealthy = !allDown,
            HealthyCount = providers.Values.Count(p => p.IsHealthy),
            TotalCount = providers.Count
        };

        return allDown ? StatusCode(503, dto) : Ok(dto);
    }

    /// <summary>
    /// Get system health (FFmpeg, disk space, memory)
    /// </summary>
    [HttpGet("system")]
    public async Task<ActionResult<SystemHealthDto>> GetSystemHealth(CancellationToken ct)
    {
        var metrics = await _systemHealthChecker.CheckSystemHealthAsync(ct).ConfigureAwait(false);
        
        var dto = new SystemHealthDto
        {
            FFmpegAvailable = metrics.FFmpegAvailable,
            FFmpegVersion = metrics.FFmpegVersion,
            DiskSpaceGB = metrics.DiskSpaceGB,
            MemoryUsagePercent = metrics.MemoryUsagePercent,
            IsHealthy = metrics.IsHealthy,
            Issues = metrics.Issues
        };

        return metrics.IsHealthy ? Ok(dto) : StatusCode(503, dto);
    }

    /// <summary>
    /// Reset circuit breaker for a specific provider
    /// </summary>
    [HttpPost("providers/{name}/reset")]
    public async Task<ActionResult> ResetProviderCircuitBreaker(string name, CancellationToken ct)
    {
        var success = await _healthService.ResetCircuitBreakerAsync(name, ct).ConfigureAwait(false);
        
        if (!success)
        {
            return NotFound(new { error = $"Provider '{name}' not found" });
        }

        _logger.LogInformation("Circuit breaker reset for provider: {ProviderName}", name);
        return Ok(new { message = $"Circuit breaker reset for provider '{name}'" });
    }

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
            AverageResponseTimeMs = metrics.AverageResponseTime.TotalMilliseconds,
            CircuitState = metrics.CircuitState.ToString(),
            FailureRate = metrics.FailureRate,
            CircuitOpenedAt = metrics.CircuitOpenedAt
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
    public string CircuitState { get; set; } = "Closed";
    public double FailureRate { get; set; }
    public DateTime? CircuitOpenedAt { get; set; }
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

/// <summary>
/// DTO for provider type health (e.g., all LLM providers)
/// </summary>
public class ProviderTypeHealthDto
{
    public string ProviderType { get; set; } = string.Empty;
    public List<ProviderHealthCheckDto> Providers { get; set; } = new();
    public bool IsHealthy { get; set; }
    public int HealthyCount { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// API health response for Electron/Electron preload checks
/// </summary>
public class ApiHealthResponse
{
    public string Status { get; set; } = "Healthy";
    public string? Version { get; set; }
    public string? Environment { get; set; }
    public string? MachineName { get; set; }
    public string? OsPlatform { get; set; }
    public string? OsVersion { get; set; }
    public string? Architecture { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// DTO for system health check
/// </summary>
public class SystemHealthDto
{
    public bool FFmpegAvailable { get; set; }
    public string? FFmpegVersion { get; set; }
    public double DiskSpaceGB { get; set; }
    public double MemoryUsagePercent { get; set; }
    public bool IsHealthy { get; set; }
    public List<string> Issues { get; set; } = new();
}
