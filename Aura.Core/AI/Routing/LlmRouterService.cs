using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.Core.AI.Routing;

/// <summary>
/// Router service that selects the best LLM provider based on task type,
/// health, latency, cost, and quality scoring with circuit breaker support.
/// </summary>
public class LlmRouterService : ILlmRouterService
{
    private readonly ILogger<LlmRouterService> _logger;
    private readonly RoutingConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    
    private readonly ConcurrentDictionary<string, ProviderHealthStatus> _healthStatus = new();
    private readonly ConcurrentDictionary<string, ProviderMetrics> _metrics = new();
    private readonly ConcurrentDictionary<string, ILlmProvider> _providerInstances = new();
    private readonly ConcurrentDictionary<string, List<double>> _latencyHistory = new();
    private readonly CostTracker _costTracker;

    public LlmRouterService(
        ILogger<LlmRouterService> logger,
        IOptions<RoutingConfiguration> config,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _costTracker = new CostTracker(_config.CostTracking);

        InitializeProviders();
    }

    private void InitializeProviders()
    {
        foreach (var policy in _config.Policies)
        {
            foreach (var provider in policy.PreferredProviders)
            {
                var key = $"{provider.ProviderName}:{provider.ModelName}";
                
                if (!_healthStatus.ContainsKey(key))
                {
                    _healthStatus[key] = new ProviderHealthStatus
                    {
                        ProviderName = key,
                        State = ProviderHealthState.Healthy,
                        LastCheckTime = DateTime.UtcNow
                    };
                }

                if (!_metrics.ContainsKey(key))
                {
                    _metrics[key] = new ProviderMetrics
                    {
                        ProviderName = provider.ProviderName,
                        ModelName = provider.ModelName,
                        AverageLatencyMs = provider.ExpectedLatencyMs,
                        AverageCost = provider.CostPerRequest,
                        QualityScore = provider.QualityScore,
                        LastUpdated = DateTime.UtcNow
                    };
                }

                if (!_latencyHistory.ContainsKey(key))
                {
                    _latencyHistory[key] = new List<double>();
                }
            }
        }
    }

    public async Task<RoutingDecision> SelectProviderAsync(
        TaskType taskType,
        RoutingConstraints? constraints = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Selecting provider for task type: {TaskType}", taskType);

        var policy = _config.Policies.FirstOrDefault(p => p.TaskType == taskType);
        if (policy == null)
        {
            _logger.LogWarning("No routing policy found for task type {TaskType}, using General fallback", taskType);
            policy = _config.Policies.FirstOrDefault(p => p.TaskType == TaskType.General);
            
            if (policy == null)
            {
                throw new InvalidOperationException($"No routing policy configured for task type {taskType} and no General fallback policy found");
            }
        }

        constraints ??= policy.DefaultConstraints;

        var candidates = await EvaluateCandidatesAsync(policy, constraints, ct);

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"No available providers for task type {taskType}");
        }

        var selected = candidates.First();
        var alternatives = candidates.Skip(1).Take(2).Select(c => c.ProviderName).ToArray();

        var decision = new RoutingDecision(
            ProviderName: selected.ProviderName,
            ModelName: selected.ModelName,
            Reasoning: BuildReasoning(selected, candidates),
            DecisionTime: DateTime.UtcNow,
            Metadata: new RoutingMetadata(
                Rank: 1,
                HealthScore: selected.HealthScore,
                LatencyScore: selected.LatencyScore,
                CostScore: selected.CostScore,
                QualityScore: selected.QualityScore,
                OverallScore: selected.OverallScore,
                AlternativeProviders: alternatives));

        _logger.LogInformation(
            "Selected provider {Provider}:{Model} for {TaskType}. Score: {Score:F2}. Reasoning: {Reasoning}",
            decision.ProviderName,
            decision.ModelName,
            taskType,
            decision.Metadata.OverallScore,
            decision.Reasoning);

        return decision;
    }

    private async Task<List<ProviderCandidate>> EvaluateCandidatesAsync(
        RoutingPolicy policy,
        RoutingConstraints constraints,
        CancellationToken ct)
    {
        var candidates = new List<ProviderCandidate>();

        foreach (var preference in policy.PreferredProviders.OrderBy(p => p.Priority))
        {
            var key = $"{preference.ProviderName}:{preference.ModelName}";
            
            if (!_healthStatus.TryGetValue(key, out var health))
            {
                continue;
            }

            if (health.State == ProviderHealthState.Unavailable)
            {
                if (health.CircuitOpenedAt.HasValue)
                {
                    var elapsed = DateTime.UtcNow - health.CircuitOpenedAt.Value;
                    if (elapsed < TimeSpan.FromSeconds(_config.CircuitBreaker.OpenDurationSeconds))
                    {
                        _logger.LogDebug("Provider {Provider} circuit is open, skipping", key);
                        continue;
                    }
                    
                    _logger.LogInformation("Provider {Provider} circuit timeout elapsed, attempting half-open", key);
                    health.State = ProviderHealthState.Degraded;
                }
            }

            if (!_metrics.TryGetValue(key, out var metrics))
            {
                continue;
            }

            if (_config.EnableCostTracking && constraints.MaxCostPerRequest < metrics.AverageCost)
            {
                _logger.LogDebug("Provider {Provider} exceeds cost constraint ({Cost} > {Max})",
                    key, metrics.AverageCost, constraints.MaxCostPerRequest);
                continue;
            }

            if (await _costTracker.WouldExceedBudgetAsync(metrics.AverageCost, ct))
            {
                _logger.LogWarning("Provider {Provider} would exceed budget limits, skipping", key);
                continue;
            }

            var healthScore = health.HealthScore;
            var latencyScore = CalculateLatencyScore(metrics.AverageLatencyMs, constraints.MaxLatencyMs);
            var costScore = CalculateCostScore(metrics.AverageCost, constraints.MaxCostPerRequest);
            var qualityScore = metrics.QualityScore;

            var overallScore = CalculateOverallScore(
                healthScore, latencyScore, costScore, qualityScore, preference.Priority);

            candidates.Add(new ProviderCandidate
            {
                ProviderName = preference.ProviderName,
                ModelName = preference.ModelName,
                Priority = preference.Priority,
                HealthScore = healthScore,
                LatencyScore = latencyScore,
                CostScore = costScore,
                QualityScore = qualityScore,
                OverallScore = overallScore
            });
        }

        return candidates.OrderByDescending(c => c.OverallScore).ToList();
    }

    private double CalculateLatencyScore(double actualLatencyMs, int maxLatencyMs)
    {
        if (actualLatencyMs <= maxLatencyMs * 0.5)
            return 1.0;
        
        if (actualLatencyMs >= maxLatencyMs)
            return 0.1;

        return 1.0 - ((actualLatencyMs - (maxLatencyMs * 0.5)) / (maxLatencyMs * 0.5));
    }

    private double CalculateCostScore(decimal actualCost, decimal maxCost)
    {
        if (actualCost <= 0)
            return 1.0;

        if (actualCost >= maxCost)
            return 0.1;

        var ratio = (double)(actualCost / maxCost);
        return 1.0 - ratio;
    }

    private double CalculateOverallScore(
        double healthScore,
        double latencyScore,
        double costScore,
        double qualityScore,
        int priority)
    {
        const double healthWeight = 0.4;
        const double latencyWeight = 0.2;
        const double costWeight = 0.2;
        const double qualityWeight = 0.2;

        var weightedScore = 
            (healthScore * healthWeight) +
            (latencyScore * latencyWeight) +
            (costScore * costWeight) +
            (qualityScore * qualityWeight);

        var priorityBonus = 1.0 / priority * 0.1;
        
        return Math.Min(1.0, weightedScore + priorityBonus);
    }

    private string BuildReasoning(ProviderCandidate selected, List<ProviderCandidate> allCandidates)
    {
        var reasons = new List<string>();

        if (selected.HealthScore >= 0.9)
            reasons.Add("excellent health");
        else if (selected.HealthScore >= 0.7)
            reasons.Add("good health");
        else
            reasons.Add("acceptable health");

        if (selected.LatencyScore >= 0.8)
            reasons.Add("low latency");
        else if (selected.LatencyScore >= 0.5)
            reasons.Add("moderate latency");

        if (selected.CostScore >= 0.8)
            reasons.Add("cost-effective");
        else if (selected.CostScore >= 0.5)
            reasons.Add("moderate cost");

        if (selected.QualityScore >= 0.8)
            reasons.Add("high quality");

        if (selected.Priority == 1)
            reasons.Add("highest priority");

        var reasoning = $"Selected due to {string.Join(", ", reasons)} (score: {selected.OverallScore:F2})";

        if (allCandidates.Count > 1)
        {
            var alternatives = string.Join(", ", allCandidates.Skip(1).Take(2).Select(c => $"{c.ProviderName} ({c.OverallScore:F2})"));
            reasoning += $". Alternatives: {alternatives}";
        }

        return reasoning;
    }

    public ILlmProvider GetProvider(RoutingDecision decision)
    {
        var key = $"{decision.ProviderName}:{decision.ModelName}";
        
        if (_providerInstances.TryGetValue(key, out var provider))
        {
            return provider;
        }

        provider = CreateProviderInstance(decision.ProviderName, decision.ModelName);
        _providerInstances[key] = provider;
        
        return provider;
    }

    private ILlmProvider CreateProviderInstance(string providerName, string modelName)
    {
        _logger.LogInformation("Creating provider instance for {Provider}:{Model}", providerName, modelName);

        var factory = _serviceProvider.GetService(typeof(IRouterProviderFactory)) as IRouterProviderFactory;
        
        if (factory == null)
        {
            throw new InvalidOperationException("IRouterProviderFactory not registered in DI container");
        }

        var provider = factory.Create(providerName, modelName);
        
        if (provider == null)
        {
            throw new InvalidOperationException($"Failed to create provider instance for {providerName}");
        }

        return provider;
    }

    public async Task<IReadOnlyList<ProviderHealthStatus>> GetHealthStatusAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return _healthStatus.Values.ToList();
    }

    public async Task<IReadOnlyList<ProviderMetrics>> GetMetricsAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return _metrics.Values.ToList();
    }

    public async Task RecordRequestAsync(
        string providerName,
        TaskType taskType,
        bool success,
        double latencyMs,
        decimal cost,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;

        var key = providerName;

        if (_healthStatus.TryGetValue(key, out var health))
        {
            health.TotalRequests++;
            health.LastCheckTime = DateTime.UtcNow;

            if (success)
            {
                health.SuccessfulRequests++;
                health.ConsecutiveFailures = 0;

                if (health.State == ProviderHealthState.Degraded && health.SuccessRate >= _config.CircuitBreaker.SuccessRateThreshold)
                {
                    _logger.LogInformation("Provider {Provider} recovered to healthy state", key);
                    health.State = ProviderHealthState.Healthy;
                    health.CircuitOpenedAt = null;
                }
            }
            else
            {
                health.FailedRequests++;
                health.ConsecutiveFailures++;

                if (health.ConsecutiveFailures >= _config.CircuitBreaker.FailureThreshold)
                {
                    _logger.LogWarning("Provider {Provider} circuit opened after {Failures} consecutive failures", 
                        key, health.ConsecutiveFailures);
                    health.State = ProviderHealthState.Unavailable;
                    health.CircuitOpenedAt = DateTime.UtcNow;
                    health.CircuitResetIn = TimeSpan.FromSeconds(_config.CircuitBreaker.OpenDurationSeconds);
                }
                else if (health.TotalRequests >= _config.CircuitBreaker.MinimumThroughput &&
                         health.SuccessRate < _config.CircuitBreaker.SuccessRateThreshold)
                {
                    _logger.LogWarning("Provider {Provider} degraded due to low success rate: {Rate:F1}%", 
                        key, health.SuccessRate);
                    health.State = ProviderHealthState.Degraded;
                }
            }
        }

        if (_metrics.TryGetValue(key, out var metrics))
        {
            metrics.RequestCount++;
            
            if (!_latencyHistory.TryGetValue(key, out var history))
            {
                history = new List<double>();
                _latencyHistory[key] = history;
            }

            history.Add(latencyMs);
            
            if (history.Count > 100)
            {
                history.RemoveAt(0);
            }

            metrics.AverageLatencyMs = history.Average();
            
            if (history.Count >= 20)
            {
                var sorted = history.OrderBy(l => l).ToList();
                metrics.P95LatencyMs = sorted[(int)(sorted.Count * 0.95)];
                metrics.P99LatencyMs = sorted[(int)(sorted.Count * 0.99)];
            }

            var totalCost = metrics.AverageCost * (metrics.RequestCount - 1) + cost;
            metrics.AverageCost = totalCost / metrics.RequestCount;
            metrics.LastUpdated = DateTime.UtcNow;
        }

        if (_config.EnableCostTracking)
        {
            await _costTracker.RecordCostAsync(cost, ct);
        }

        _logger.LogDebug(
            "Recorded request for {Provider}: success={Success}, latency={Latency}ms, cost=${Cost}",
            key, success, latencyMs, cost);
    }

    public async Task MarkProviderUnavailableAsync(string providerName, TimeSpan? duration = null, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        if (_healthStatus.TryGetValue(providerName, out var health))
        {
            _logger.LogWarning("Manually marking provider {Provider} as unavailable", providerName);
            health.State = ProviderHealthState.Unavailable;
            health.CircuitOpenedAt = DateTime.UtcNow;
            health.CircuitResetIn = duration ?? TimeSpan.FromSeconds(_config.CircuitBreaker.OpenDurationSeconds);
        }
    }

    public async Task ResetProviderHealthAsync(string providerName, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        if (_healthStatus.TryGetValue(providerName, out var health))
        {
            _logger.LogInformation("Resetting provider {Provider} to healthy state", providerName);
            health.State = ProviderHealthState.Healthy;
            health.ConsecutiveFailures = 0;
            health.CircuitOpenedAt = null;
            health.CircuitResetIn = null;
        }
    }

    private class ProviderCandidate
    {
        public string ProviderName { get; init; } = string.Empty;
        public string ModelName { get; init; } = string.Empty;
        public int Priority { get; init; }
        public double HealthScore { get; init; }
        public double LatencyScore { get; init; }
        public double CostScore { get; init; }
        public double QualityScore { get; init; }
        public double OverallScore { get; init; }
    }
}

/// <summary>
/// Tracks costs per request, hour, and day with budget enforcement.
/// </summary>
internal class CostTracker
{
    private readonly CostTrackingConfig _config;
    private readonly ConcurrentDictionary<DateTime, decimal> _hourlyCosts = new();
    private readonly ConcurrentDictionary<DateTime, decimal> _dailyCosts = new();

    public CostTracker(CostTrackingConfig config)
    {
        _config = config;
    }

    public async Task<bool> WouldExceedBudgetAsync(decimal cost, CancellationToken ct)
    {
        await Task.CompletedTask;

        if (!_config.EnforceBudgetLimits)
            return false;

        var now = DateTime.UtcNow;
        var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var currentDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

        CleanupOldEntries();

        var hourlyCost = _hourlyCosts.GetOrAdd(currentHour, 0m);
        var dailyCost = _dailyCosts.GetOrAdd(currentDay, 0m);

        return (hourlyCost + cost > _config.MaxCostPerHour) ||
               (dailyCost + cost > _config.MaxCostPerDay);
    }

    public async Task RecordCostAsync(decimal cost, CancellationToken ct)
    {
        await Task.CompletedTask;

        var now = DateTime.UtcNow;
        var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var currentDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

        _hourlyCosts.AddOrUpdate(currentHour, cost, (_, existing) => existing + cost);
        _dailyCosts.AddOrUpdate(currentDay, cost, (_, existing) => existing + cost);
    }

    private void CleanupOldEntries()
    {
        var cutoffHour = DateTime.UtcNow.AddHours(-24);
        var cutoffDay = DateTime.UtcNow.AddDays(-7);

        foreach (var key in _hourlyCosts.Keys.Where(k => k < cutoffHour).ToList())
        {
            _hourlyCosts.TryRemove(key, out _);
        }

        foreach (var key in _dailyCosts.Keys.Where(k => k < cutoffDay).ToList())
        {
            _dailyCosts.TryRemove(key, out _);
        }
    }
}
