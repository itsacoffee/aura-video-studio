using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Health;

/// <summary>
/// Centralized service for provider health tracking with circuit breaker support
/// </summary>
public class ProviderHealthService
{
    private readonly ProviderHealthMonitor _healthMonitor;
    private readonly ILogger<ProviderHealthService> _logger;
    private readonly CircuitBreakerSettings _circuitBreakerSettings;

    public ProviderHealthService(
        ProviderHealthMonitor healthMonitor,
        ILogger<ProviderHealthService> logger,
        CircuitBreakerSettings circuitBreakerSettings)
    {
        _healthMonitor = healthMonitor;
        _logger = logger;
        _circuitBreakerSettings = circuitBreakerSettings;
    }

    /// <summary>
    /// Get health status for all LLM providers
    /// </summary>
    public Dictionary<string, ProviderHealthMetrics> GetLlmProvidersHealth()
    {
        var allHealth = _healthMonitor.GetAllProviderHealth();
        return allHealth
            .Where(kvp => IsLlmProvider(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Get health status for all TTS providers
    /// </summary>
    public Dictionary<string, ProviderHealthMetrics> GetTtsProvidersHealth()
    {
        var allHealth = _healthMonitor.GetAllProviderHealth();
        return allHealth
            .Where(kvp => IsTtsProvider(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Get health status for all image providers
    /// </summary>
    public Dictionary<string, ProviderHealthMetrics> GetImageProvidersHealth()
    {
        var allHealth = _healthMonitor.GetAllProviderHealth();
        return allHealth
            .Where(kvp => IsImageProvider(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Check if all providers of a type are unavailable
    /// </summary>
    public bool AreAllProvidersDown(string providerType)
    {
        Dictionary<string, ProviderHealthMetrics> providers = providerType.ToLowerInvariant() switch
        {
            "llm" => GetLlmProvidersHealth(),
            "tts" => GetTtsProvidersHealth(),
            "image" or "images" => GetImageProvidersHealth(),
            _ => new Dictionary<string, ProviderHealthMetrics>()
        };

        if (providers.Count == 0)
            return false;

        return providers.Values.All(p => !p.IsHealthy || p.CircuitState == CircuitBreakerState.Open);
    }

    /// <summary>
    /// Get the first healthy provider of a given type
    /// </summary>
    public string? GetFirstHealthyProvider(string providerType)
    {
        Dictionary<string, ProviderHealthMetrics> providers = providerType.ToLowerInvariant() switch
        {
            "llm" => GetLlmProvidersHealth(),
            "tts" => GetTtsProvidersHealth(),
            "image" or "images" => GetImageProvidersHealth(),
            _ => new Dictionary<string, ProviderHealthMetrics>()
        };

        return providers
            .Where(kvp => kvp.Value.IsHealthy && kvp.Value.CircuitState == CircuitBreakerState.Closed)
            .OrderBy(kvp => kvp.Value.ConsecutiveFailures)
            .ThenBy(kvp => kvp.Value.AverageResponseTime)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();
    }

    /// <summary>
    /// Test connection to a specific provider
    /// </summary>
    public async Task<ProviderHealthMetrics> TestProviderConnectionAsync(
        string providerName,
        Func<CancellationToken, Task<bool>> healthCheckFunc,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Manual health check triggered for provider: {ProviderName}", providerName);
        return await _healthMonitor.CheckProviderHealthAsync(providerName, healthCheckFunc, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reset circuit breaker for a provider
    /// </summary>
    public async Task<bool> ResetCircuitBreakerAsync(string providerName, CancellationToken ct = default)
    {
        var circuitBreaker = _healthMonitor.GetCircuitBreaker(providerName);
        if (circuitBreaker == null)
        {
            _logger.LogWarning("Cannot reset circuit breaker: provider {ProviderName} not found", providerName);
            return false;
        }

        await circuitBreaker.ResetAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Circuit breaker reset for provider: {ProviderName}", providerName);
        return true;
    }

    private bool IsLlmProvider(string name) =>
        name.Contains("llm", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("RuleBased", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Azure", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Gemini", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Anthropic", StringComparison.OrdinalIgnoreCase);

    private bool IsTtsProvider(string name) =>
        name.Contains("tts", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("voice", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("ElevenLabs", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("PlayHT", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Azure", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Piper", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Mimic3", StringComparison.OrdinalIgnoreCase);

    private bool IsImageProvider(string name) =>
        name.Contains("image", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("visual", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("StableDiffusion", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Stability", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Stock", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("LocalStock", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// System health metrics
/// </summary>
public record SystemHealthMetrics
{
    public bool FFmpegAvailable { get; init; }
    public string? FFmpegVersion { get; init; }
    public double DiskSpaceGB { get; init; }
    public double MemoryUsagePercent { get; init; }
    public bool IsHealthy { get; init; }
    public List<string> Issues { get; init; } = new();
}
