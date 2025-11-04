using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Routing;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Unit tests for LlmRouterService routing policy evaluation.
/// </summary>
public class LlmRouterServiceTests
{
    private readonly Mock<ILogger<LlmRouterService>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IRouterProviderFactory> _factoryMock;
    private readonly RoutingConfiguration _config;

    public LlmRouterServiceTests()
    {
        _loggerMock = new Mock<ILogger<LlmRouterService>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _factoryMock = new Mock<IRouterProviderFactory>();

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IRouterProviderFactory)))
            .Returns(_factoryMock.Object);

        _config = CreateDefaultConfig();
    }

    [Fact]
    public async Task SelectProviderAsync_ChoosesHighestPriorityHealthyProvider()
    {
        var options = Options.Create(_config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);

        var decision = await router.SelectProviderAsync(TaskType.Planning);

        Assert.NotNull(decision);
        Assert.Equal("OpenAI", decision.ProviderName);
        Assert.Equal("gpt-4o-mini", decision.ModelName);
        Assert.Equal(1, decision.Metadata.Rank);
    }

    [Fact]
    public async Task SelectProviderAsync_SkipsUnavailableProvider()
    {
        var options = Options.Create(_config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);

        await router.MarkProviderUnavailableAsync("OpenAI:gpt-4o-mini");

        var decision = await router.SelectProviderAsync(TaskType.Planning);

        Assert.NotNull(decision);
        Assert.NotEqual("OpenAI", decision.ProviderName);
        Assert.Equal(1, decision.Metadata.Rank);
    }

    [Fact]
    public async Task SelectProviderAsync_AppliesCircuitBreaker_AfterConsecutiveFailures()
    {
        var options = Options.Create(_config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);

        var providerKey = "OpenAI:gpt-4o-mini";
        
        for (int i = 0; i < 5; i++)
        {
            await router.RecordRequestAsync(providerKey, TaskType.Planning, success: false, latencyMs: 1000, cost: 0.01m);
        }

        var decision = await router.SelectProviderAsync(TaskType.Planning);

        Assert.NotEqual("OpenAI", decision.ProviderName);
        
        var healthStatus = await router.GetHealthStatusAsync();
        var openAiHealth = healthStatus.FirstOrDefault(h => h.ProviderName == providerKey);
        
        Assert.NotNull(openAiHealth);
        Assert.Equal(ProviderHealthState.Unavailable, openAiHealth.State);
        Assert.NotNull(openAiHealth.CircuitOpenedAt);
    }

    [Fact]
    public async Task SelectProviderAsync_RecoversProvider_AfterSuccessfulRequests()
    {
        var options = Options.Create(_config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);

        var providerKey = "OpenAI:gpt-4o-mini";
        
        for (int i = 0; i < 3; i++)
        {
            await router.RecordRequestAsync(providerKey, TaskType.Planning, success: false, latencyMs: 1000, cost: 0.01m);
        }

        for (int i = 0; i < 10; i++)
        {
            await router.RecordRequestAsync(providerKey, TaskType.Planning, success: true, latencyMs: 1000, cost: 0.01m);
        }

        var healthStatus = await router.GetHealthStatusAsync();
        var openAiHealth = healthStatus.FirstOrDefault(h => h.ProviderName == providerKey);
        
        Assert.NotNull(openAiHealth);
        Assert.Equal(ProviderHealthState.Healthy, openAiHealth.State);
        Assert.True(openAiHealth.SuccessRate >= 70);
    }

    [Fact]
    public async Task SelectProviderAsync_RespectsConstraints_MaxCost()
    {
        var config = CreateDefaultConfig();
        config.Policies.First(p => p.TaskType == TaskType.Planning).PreferredProviders
            .First(p => p.ProviderName == "OpenAI").CostPerRequest = 0.50m;

        var options = Options.Create(config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);

        var constraints = new RoutingConstraints(MaxCostPerRequest: 0.10m);
        var decision = await router.SelectProviderAsync(TaskType.Planning, constraints);

        Assert.NotNull(decision);
        Assert.NotEqual("OpenAI", decision.ProviderName);
    }

    [Fact]
    public async Task SelectProviderAsync_UsesGeneralFallback_WhenNoSpecificPolicy()
    {
        var options = Options.Create(_config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);

        var ssmlDecision = await router.SelectProviderAsync(TaskType.SsmlGeneration);
        
        Assert.NotNull(ssmlDecision);
        Assert.Equal("OpenAI", ssmlDecision.ProviderName);
    }

    [Fact]
    public async Task SelectProviderAsync_ThrowsWhenNoMatchingPolicy()
    {
        var config = new RoutingConfiguration
        {
            CircuitBreaker = new CircuitBreakerConfig(),
            HealthCheck = new HealthCheckConfig(),
            CostTracking = new CostTrackingConfig(),
            Policies = new List<RoutingPolicy>()
        };
        var options = Options.Create(config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);
        
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await router.SelectProviderAsync(TaskType.Planning));
    }

    [Fact]
    public async Task RecordRequestAsync_UpdatesMetrics()
    {
        var options = Options.Create(_config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);

        var providerKey = "OpenAI:gpt-4o-mini";
        
        await router.RecordRequestAsync(providerKey, TaskType.Planning, success: true, latencyMs: 1500, cost: 0.02m);
        await router.RecordRequestAsync(providerKey, TaskType.Planning, success: true, latencyMs: 2000, cost: 0.02m);
        await router.RecordRequestAsync(providerKey, TaskType.Planning, success: true, latencyMs: 2500, cost: 0.02m);

        var metrics = await router.GetMetricsAsync();
        var openAiMetrics = metrics.FirstOrDefault(m => m.ProviderName == "OpenAI" && m.ModelName == "gpt-4o-mini");

        Assert.NotNull(openAiMetrics);
        Assert.Equal(3, openAiMetrics.RequestCount);
        Assert.True(openAiMetrics.AverageLatencyMs > 0);
    }

    [Fact]
    public async Task GetHealthStatusAsync_ReturnsAllProviders()
    {
        var options = Options.Create(_config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);

        var healthStatus = await router.GetHealthStatusAsync();

        Assert.NotEmpty(healthStatus);
        Assert.Contains(healthStatus, h => h.ProviderName.Contains("OpenAI"));
        Assert.Contains(healthStatus, h => h.ProviderName.Contains("Ollama"));
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsAllProviders()
    {
        var options = Options.Create(_config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);

        var metrics = await router.GetMetricsAsync();

        Assert.NotEmpty(metrics);
        Assert.Contains(metrics, m => m.ProviderName == "OpenAI");
        Assert.Contains(metrics, m => m.ProviderName == "Ollama");
    }

    [Fact]
    public async Task ResetProviderHealthAsync_RestoresHealthyState()
    {
        var options = Options.Create(_config);
        var router = new LlmRouterService(_loggerMock.Object, options, _serviceProviderMock.Object);

        var providerKey = "OpenAI:gpt-4o-mini";
        await router.MarkProviderUnavailableAsync(providerKey);

        var healthBefore = await router.GetHealthStatusAsync();
        var openAiHealthBefore = healthBefore.FirstOrDefault(h => h.ProviderName == providerKey);
        Assert.Equal(ProviderHealthState.Unavailable, openAiHealthBefore?.State);

        await router.ResetProviderHealthAsync(providerKey);

        var healthAfter = await router.GetHealthStatusAsync();
        var openAiHealthAfter = healthAfter.FirstOrDefault(h => h.ProviderName == providerKey);
        Assert.Equal(ProviderHealthState.Healthy, openAiHealthAfter?.State);
    }

    [Fact]
    public void ProviderHealthStatus_CalculatesSuccessRate_Correctly()
    {
        var status = new ProviderHealthStatus
        {
            TotalRequests = 100,
            SuccessfulRequests = 80,
            FailedRequests = 20
        };

        Assert.Equal(80.0, status.SuccessRate);
    }

    [Fact]
    public void ProviderHealthStatus_HealthScore_ReflectsState()
    {
        var healthyStatus = new ProviderHealthStatus { State = ProviderHealthState.Healthy, TotalRequests = 100, SuccessfulRequests = 95 };
        var degradedStatus = new ProviderHealthStatus { State = ProviderHealthState.Degraded };
        var unavailableStatus = new ProviderHealthStatus { State = ProviderHealthState.Unavailable };

        Assert.True(healthyStatus.HealthScore >= 0.9);
        Assert.Equal(0.5, degradedStatus.HealthScore);
        Assert.Equal(0.0, unavailableStatus.HealthScore);
    }

    private RoutingConfiguration CreateDefaultConfig()
    {
        return new RoutingConfiguration
        {
            CircuitBreaker = new CircuitBreakerConfig
            {
                FailureThreshold = 5,
                OpenDurationSeconds = 60,
                SuccessRateThreshold = 80.0,
                MinimumThroughput = 10
            },
            HealthCheck = new HealthCheckConfig
            {
                IntervalSeconds = 300,
                TimeoutSeconds = 10,
                EnableBackgroundChecks = true
            },
            CostTracking = new CostTrackingConfig
            {
                MaxCostPerRequest = 0.50m,
                MaxCostPerHour = 10.00m,
                MaxCostPerDay = 50.00m,
                EnforceBudgetLimits = true
            },
            EnableFailover = true,
            EnableCostTracking = true,
            Policies = new List<RoutingPolicy>
            {
                new RoutingPolicy
                {
                    TaskType = TaskType.Planning,
                    PreferredProviders = new List<ProviderPreference>
                    {
                        new ProviderPreference
                        {
                            ProviderName = "OpenAI",
                            ModelName = "gpt-4o-mini",
                            Priority = 1,
                            QualityScore = 0.9,
                            CostPerRequest = 0.02m,
                            ExpectedLatencyMs = 3000,
                            ContextLength = 8192
                        },
                        new ProviderPreference
                        {
                            ProviderName = "Ollama",
                            ModelName = "llama3.1:8b-q4_k_m",
                            Priority = 2,
                            QualityScore = 0.75,
                            CostPerRequest = 0.0m,
                            ExpectedLatencyMs = 5000,
                            ContextLength = 8192
                        },
                        new ProviderPreference
                        {
                            ProviderName = "RuleBased",
                            ModelName = "default",
                            Priority = 3,
                            QualityScore = 0.5,
                            CostPerRequest = 0.0m,
                            ExpectedLatencyMs = 100,
                            ContextLength = 4096
                        }
                    },
                    DefaultConstraints = new RoutingConstraints(
                        RequiredContextLength: 8192,
                        MaxLatencyMs: 10000,
                        MaxCostPerRequest: 0.10m,
                        RequireDeterminism: false,
                        MinQualityScore: 0.7),
                    EnableFailover = true,
                    MaxFailoverAttempts = 3
                },
                new RoutingPolicy
                {
                    TaskType = TaskType.General,
                    PreferredProviders = new List<ProviderPreference>
                    {
                        new ProviderPreference
                        {
                            ProviderName = "OpenAI",
                            ModelName = "gpt-4o-mini",
                            Priority = 1,
                            QualityScore = 0.85,
                            CostPerRequest = 0.015m,
                            ExpectedLatencyMs = 3000,
                            ContextLength = 4096
                        },
                        new ProviderPreference
                        {
                            ProviderName = "Ollama",
                            ModelName = "llama3.1:8b-q4_k_m",
                            Priority = 2,
                            QualityScore = 0.7,
                            CostPerRequest = 0.0m,
                            ExpectedLatencyMs = 5000,
                            ContextLength = 4096
                        }
                    },
                    DefaultConstraints = new RoutingConstraints(
                        RequiredContextLength: 4096,
                        MaxLatencyMs: 30000,
                        MaxCostPerRequest: 0.10m,
                        RequireDeterminism: false,
                        MinQualityScore: 0.5),
                    EnableFailover = true,
                    MaxFailoverAttempts = 3
                }
            }
        };
    }
}
