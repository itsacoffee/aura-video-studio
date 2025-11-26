using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Services;
using Aura.Core.Services.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for the unified Provider Health Dashboard endpoint.
/// Provides a single endpoint that returns all provider statuses including
/// health, quota, rate limits, and configuration status.
/// </summary>
[ApiController]
[Route("api/health-dashboard")]
public class HealthDashboardController : ControllerBase
{
    private readonly ILogger<HealthDashboardController> _logger;
    private readonly ProviderHealthMonitoringService _healthMonitoring;
    private readonly ProviderCircuitBreakerService _circuitBreaker;
    private readonly ProviderSettings _providerSettings;
    private readonly ISecureStorageService _secureStorage;

    public HealthDashboardController(
        ILogger<HealthDashboardController> logger,
        ProviderHealthMonitoringService healthMonitoring,
        ProviderCircuitBreakerService circuitBreaker,
        ProviderSettings providerSettings,
        ISecureStorageService secureStorage)
    {
        _logger = logger;
        _healthMonitoring = healthMonitoring;
        _circuitBreaker = circuitBreaker;
        _providerSettings = providerSettings;
        _secureStorage = secureStorage;
    }

    /// <summary>
    /// Get unified health dashboard with all provider statuses, quota info, and configuration status.
    /// This is the main endpoint for the Provider Health Dashboard feature.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ProviderHealthDashboardResponse>> GetDashboard(CancellationToken ct)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Fetching provider health dashboard, CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var providers = await BuildProviderStatusListAsync(ct).ConfigureAwait(false);

            var response = new ProviderHealthDashboardResponse
            {
                Providers = providers,
                Summary = BuildSummary(providers),
                Timestamp = DateTime.UtcNow,
                CorrelationId = correlationId
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching provider health dashboard, CorrelationId: {CorrelationId}", correlationId);
            return Problem(
                title: "Health Dashboard Error",
                detail: "An error occurred while fetching provider health data.",
                statusCode: 500,
                type: "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#health-dashboard-error",
                instance: correlationId);
        }
    }

    private async Task<List<ProviderDashboardStatus>> BuildProviderStatusListAsync(CancellationToken ct)
    {
        var providers = new List<ProviderDashboardStatus>();

        // Define all known providers with their categories and whether they need API keys
        var providerDefinitions = new[]
        {
            // LLM Providers
            new ProviderDefinition("OpenAI", "LLM", "Premium", true, "openai"),
            new ProviderDefinition("Anthropic", "LLM", "Premium", true, "anthropic"),
            new ProviderDefinition("Google", "LLM", "Premium", true, "gemini"),
            new ProviderDefinition("AzureOpenAI", "LLM", "Premium", true, "azure_openai"),
            new ProviderDefinition("Ollama", "LLM", "Free/Local", false, null),
            new ProviderDefinition("RuleBased", "LLM", "Free/Offline", false, null),

            // TTS Providers
            new ProviderDefinition("ElevenLabs", "TTS", "Premium", true, "elevenlabs"),
            new ProviderDefinition("PlayHT", "TTS", "Premium", true, "playht"),
            new ProviderDefinition("AzureSpeech", "TTS", "Premium", true, "azure_speech"),
            new ProviderDefinition("WindowsSAPI", "TTS", "Free/Local", false, null),
            new ProviderDefinition("Piper", "TTS", "Free/Offline", false, null),
            new ProviderDefinition("Mimic3", "TTS", "Free/Offline", false, null),

            // Image Providers
            new ProviderDefinition("StableDiffusion", "Image", "Free/Local", false, null),
            new ProviderDefinition("StabilityAI", "Image", "Premium", true, "stabilityai"),
            new ProviderDefinition("Pexels", "Image", "Free", true, "pexels"),
            new ProviderDefinition("Unsplash", "Image", "Free", true, "unsplash"),
            new ProviderDefinition("Pixabay", "Image", "Free", true, "pixabay"),
            new ProviderDefinition("Stock", "Image", "Free/Offline", false, null),
        };

        foreach (var def in providerDefinitions)
        {
            var status = await BuildProviderStatusAsync(def, ct).ConfigureAwait(false);
            providers.Add(status);
        }

        return providers;
    }

    private async Task<ProviderDashboardStatus> BuildProviderStatusAsync(ProviderDefinition def, CancellationToken ct)
    {
        var healthMetrics = _healthMonitoring.GetProviderHealth(def.Name);
        var circuitStatus = _circuitBreaker.GetStatus(def.Name);

        // Check if API key is configured
        var hasApiKey = false;
        if (def.RequiresApiKey && !string.IsNullOrEmpty(def.ApiKeyName))
        {
            hasApiKey = await _secureStorage.HasApiKeyAsync(def.ApiKeyName).ConfigureAwait(false);
        }

        // For providers that don't require API keys, consider them configured
        var isConfigured = !def.RequiresApiKey || hasApiKey;

        // Determine health status
        var healthStatus = DetermineHealthStatus(healthMetrics, circuitStatus, isConfigured, def.RequiresApiKey);

        // Get quota info for rate-limited providers
        var quotaInfo = GetQuotaInfo(def.Name, def.Tier);

        return new ProviderDashboardStatus
        {
            Name = def.Name,
            Category = def.Category,
            Tier = def.Tier,
            HealthStatus = healthStatus,
            IsConfigured = isConfigured,
            RequiresApiKey = def.RequiresApiKey,
            SuccessRate = healthMetrics?.SuccessRatePercent ?? 100.0,
            AverageLatencyMs = (healthMetrics?.AverageLatencySeconds ?? 0) * 1000,
            ConsecutiveFailures = healthMetrics?.ConsecutiveFailures ?? 0,
            CircuitState = circuitStatus.State.ToString(),
            LastError = null,
            LastCheckTime = healthMetrics?.LastUpdated ?? DateTime.MinValue,
            QuotaInfo = quotaInfo,
            ConfigureUrl = def.RequiresApiKey && !hasApiKey ? "/settings" : null
        };
    }

    private string DetermineHealthStatus(
        Aura.Core.Models.Providers.ProviderHealthMetrics? metrics,
        Aura.Core.Services.Providers.CircuitBreakerStatus circuitStatus,
        bool isConfigured,
        bool requiresApiKey)
    {
        // Not configured but requires API key
        if (requiresApiKey && !isConfigured)
        {
            return "not_configured";
        }

        // Circuit breaker is open
        if (circuitStatus.State == CircuitState.Open)
        {
            return "offline";
        }

        // Circuit breaker is half-open (testing)
        if (circuitStatus.State == CircuitState.HalfOpen)
        {
            return "degraded";
        }

        // No health metrics yet
        if (metrics == null)
        {
            return isConfigured ? "unknown" : "not_configured";
        }

        // Determine based on metrics
        if (metrics.ConsecutiveFailures >= 3)
        {
            return "offline";
        }

        if (metrics.SuccessRatePercent >= 95)
        {
            return "healthy";
        }

        if (metrics.SuccessRatePercent >= 80)
        {
            return "degraded";
        }

        return "offline";
    }

    private QuotaInfo? GetQuotaInfo(string providerName, string tier)
    {
        // Quota information for known rate-limited providers
        // In a production system, this would fetch actual quota data from provider APIs
        return providerName.ToLowerInvariant() switch
        {
            "pexels" => new QuotaInfo
            {
                RateLimitType = "hourly",
                LimitValue = 200,
                UsedValue = null, // Would need actual API call to get
                RemainingValue = null,
                ResetsAt = null,
                Description = "200 requests per hour"
            },
            "unsplash" => new QuotaInfo
            {
                RateLimitType = "hourly",
                LimitValue = 50,
                UsedValue = null,
                RemainingValue = null,
                ResetsAt = null,
                Description = "50 requests per hour (demo)"
            },
            "pixabay" => new QuotaInfo
            {
                RateLimitType = "hourly",
                LimitValue = 100,
                UsedValue = null,
                RemainingValue = null,
                ResetsAt = null,
                Description = "100 requests per hour"
            },
            "openai" => tier == "Premium" ? new QuotaInfo
            {
                RateLimitType = "tokens_per_minute",
                LimitValue = null,
                UsedValue = null,
                RemainingValue = null,
                ResetsAt = null,
                Description = "Rate limits vary by plan"
            } : null,
            "elevenlabs" => tier == "Premium" ? new QuotaInfo
            {
                RateLimitType = "characters_per_month",
                LimitValue = null,
                UsedValue = null,
                RemainingValue = null,
                ResetsAt = null,
                Description = "Character quota varies by plan"
            } : null,
            _ => null
        };
    }

    private ProviderDashboardSummary BuildSummary(List<ProviderDashboardStatus> providers)
    {
        return new ProviderDashboardSummary
        {
            TotalProviders = providers.Count,
            HealthyProviders = providers.Count(p => p.HealthStatus == "healthy"),
            DegradedProviders = providers.Count(p => p.HealthStatus == "degraded"),
            OfflineProviders = providers.Count(p => p.HealthStatus == "offline"),
            NotConfiguredProviders = providers.Count(p => p.HealthStatus == "not_configured"),
            UnknownProviders = providers.Count(p => p.HealthStatus == "unknown"),
            ByCategory = providers
                .GroupBy(p => p.Category)
                .ToDictionary(
                    g => g.Key,
                    g => new CategorySummary
                    {
                        Total = g.Count(),
                        Healthy = g.Count(p => p.HealthStatus == "healthy"),
                        Degraded = g.Count(p => p.HealthStatus == "degraded"),
                        Offline = g.Count(p => p.HealthStatus == "offline"),
                        NotConfigured = g.Count(p => p.HealthStatus == "not_configured")
                    })
        };
    }

    private record ProviderDefinition(
        string Name,
        string Category,
        string Tier,
        bool RequiresApiKey,
        string? ApiKeyName);
}

/// <summary>
/// Response for the unified provider health dashboard
/// </summary>
public class ProviderHealthDashboardResponse
{
    public List<ProviderDashboardStatus> Providers { get; init; } = new();
    public ProviderDashboardSummary Summary { get; init; } = new();
    public DateTime Timestamp { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Status of a single provider in the dashboard
/// </summary>
public class ProviderDashboardStatus
{
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public string Tier { get; init; } = "";
    public string HealthStatus { get; init; } = ""; // healthy, degraded, offline, not_configured, unknown
    public bool IsConfigured { get; init; }
    public bool RequiresApiKey { get; init; }
    public double SuccessRate { get; init; }
    public double AverageLatencyMs { get; init; }
    public int ConsecutiveFailures { get; init; }
    public string CircuitState { get; init; } = "";
    public string? LastError { get; init; }
    public DateTime LastCheckTime { get; init; }
    public QuotaInfo? QuotaInfo { get; init; }
    public string? ConfigureUrl { get; init; } // URL to configure if not configured
}

/// <summary>
/// Quota/rate limit information for a provider
/// </summary>
public class QuotaInfo
{
    public string RateLimitType { get; init; } = ""; // hourly, daily, monthly, tokens_per_minute, etc.
    public int? LimitValue { get; init; }
    public int? UsedValue { get; init; }
    public int? RemainingValue { get; init; }
    public DateTime? ResetsAt { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Summary of all providers in the dashboard
/// </summary>
public class ProviderDashboardSummary
{
    public int TotalProviders { get; init; }
    public int HealthyProviders { get; init; }
    public int DegradedProviders { get; init; }
    public int OfflineProviders { get; init; }
    public int NotConfiguredProviders { get; init; }
    public int UnknownProviders { get; init; }
    public Dictionary<string, CategorySummary> ByCategory { get; init; } = new();
}

/// <summary>
/// Summary for a specific category of providers
/// </summary>
public class CategorySummary
{
    public int Total { get; init; }
    public int Healthy { get; init; }
    public int Degraded { get; init; }
    public int Offline { get; init; }
    public int NotConfigured { get; init; }
}
