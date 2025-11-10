using Aura.Core.Monitoring;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests.Monitoring;

public class AlertingEngineTests
{
    private readonly ILogger<MetricsCollector> _metricsLogger;
    private readonly ILogger<AlertingEngine> _alertingLogger;
    private readonly MetricsCollector _metrics;

    public AlertingEngineTests()
    {
        var factory = new LoggerFactory();
        _metricsLogger = factory.CreateLogger<MetricsCollector>();
        _alertingLogger = factory.CreateLogger<AlertingEngine>();
        _metrics = new MetricsCollector(_metricsLogger);
    }

    [Fact]
    public async Task AlertingEngine_ShouldFireAlert_WhenSloViolated()
    {
        // Arrange
        var config = new SliSloConfiguration
        {
            Indicators = new List<ServiceLevelIndicator>
            {
                new ServiceLevelIndicator
                {
                    Name = "test_metric",
                    MetricName = "test_metric",
                    Aggregation = SliAggregation.Average,
                    MeasurementWindow = TimeSpan.FromMinutes(5)
                }
            },
            Objectives = new List<ServiceLevelObjective>
            {
                new ServiceLevelObjective
                {
                    Name = "test_slo",
                    SliName = "test_metric",
                    Operator = SloOperator.GreaterThanOrEqual,
                    TargetValue = 100,
                    EvaluationWindow = TimeSpan.FromMinutes(5),
                    Severity = "warning",
                    NotificationChannels = new List<string> { "slack" }
                }
            }
        };
        var alerting = new AlertingEngine(_metrics, config, _alertingLogger);

        // Act: Record value that violates SLO (need 100, recording 50)
        _metrics.RecordHistogram("test_metric", 50);
        
        // Evaluate multiple times to trigger flapping protection
        await alerting.EvaluateAsync();
        await alerting.EvaluateAsync();
        await alerting.EvaluateAsync();
        
        var alerts = await alerting.EvaluateAsync();

        // Assert
        Assert.NotEmpty(alerts);
        Assert.Equal("test_slo", alerts[0].Name);
        Assert.Equal("warning", alerts[0].Severity);
        Assert.Equal(50, alerts[0].CurrentValue);
        Assert.Equal(100, alerts[0].TargetValue);
    }

    [Fact]
    public async Task AlertingEngine_ShouldNotFireAlert_WhenSloMet()
    {
        // Arrange
        var config = new SliSloConfiguration
        {
            Indicators = new List<ServiceLevelIndicator>
            {
                new ServiceLevelIndicator
                {
                    Name = "test_metric",
                    MetricName = "test_metric",
                    Aggregation = SliAggregation.Average,
                    MeasurementWindow = TimeSpan.FromMinutes(5)
                }
            },
            Objectives = new List<ServiceLevelObjective>
            {
                new ServiceLevelObjective
                {
                    Name = "test_slo",
                    SliName = "test_metric",
                    Operator = SloOperator.GreaterThanOrEqual,
                    TargetValue = 100,
                    EvaluationWindow = TimeSpan.FromMinutes(5)
                }
            }
        };
        var alerting = new AlertingEngine(_metrics, config, _alertingLogger);

        // Act: Record value that meets SLO
        _metrics.RecordHistogram("test_metric", 150);
        var alerts = await alerting.EvaluateAsync();

        // Assert
        Assert.Empty(alerts);
    }

    [Fact]
    public async Task AlertingEngine_ShouldRequireConsecutiveViolations_BeforeFiring()
    {
        // Arrange
        var config = new SliSloConfiguration
        {
            Indicators = new List<ServiceLevelIndicator>
            {
                new ServiceLevelIndicator
                {
                    Name = "test_metric",
                    MetricName = "test_metric",
                    Aggregation = SliAggregation.Average,
                    MeasurementWindow = TimeSpan.FromMinutes(5)
                }
            },
            Objectives = new List<ServiceLevelObjective>
            {
                new ServiceLevelObjective
                {
                    Name = "test_slo",
                    SliName = "test_metric",
                    Operator = SloOperator.GreaterThanOrEqual,
                    TargetValue = 100,
                    EvaluationWindow = TimeSpan.FromMinutes(5)
                }
            }
        };
        var alerting = new AlertingEngine(_metrics, config, _alertingLogger);

        // Act & Assert
        _metrics.RecordHistogram("test_metric", 50);
        
        // First violation - should not fire
        var alerts1 = await alerting.EvaluateAsync();
        Assert.Empty(alerts1);

        // Second violation - should not fire
        var alerts2 = await alerting.EvaluateAsync();
        Assert.Empty(alerts2);

        // Third violation - should fire
        var alerts3 = await alerting.EvaluateAsync();
        Assert.NotEmpty(alerts3);
    }

    [Fact]
    public async Task AlertingEngine_ShouldAutoResolve_WhenMetricReturnsToNormal()
    {
        // Arrange
        var config = new SliSloConfiguration
        {
            Indicators = new List<ServiceLevelIndicator>
            {
                new ServiceLevelIndicator
                {
                    Name = "test_metric",
                    MetricName = "test_metric",
                    Aggregation = SliAggregation.Average,
                    MeasurementWindow = TimeSpan.FromMinutes(5)
                }
            },
            Objectives = new List<ServiceLevelObjective>
            {
                new ServiceLevelObjective
                {
                    Name = "test_slo",
                    SliName = "test_metric",
                    Operator = SloOperator.GreaterThanOrEqual,
                    TargetValue = 100,
                    EvaluationWindow = TimeSpan.FromMinutes(5)
                }
            }
        };
        var alerting = new AlertingEngine(_metrics, config, _alertingLogger);

        // Act: Violate SLO to fire alert
        _metrics.RecordHistogram("test_metric", 50);
        await alerting.EvaluateAsync();
        await alerting.EvaluateAsync();
        await alerting.EvaluateAsync();
        
        var states1 = alerting.GetAlertStates();
        Assert.True(states1["test_slo"].Firing);

        // Resolve: Metric returns to normal
        _metrics.RecordHistogram("test_metric", 150);
        await alerting.EvaluateAsync();
        
        var states2 = alerting.GetAlertStates();
        Assert.False(states2["test_slo"].Firing);
    }

    [Fact]
    public async Task AlertingEngine_ShouldEvaluateLessThanOperator()
    {
        // Arrange
        var config = new SliSloConfiguration
        {
            Indicators = new List<ServiceLevelIndicator>
            {
                new ServiceLevelIndicator
                {
                    Name = "error_rate",
                    MetricName = "error_rate",
                    Aggregation = SliAggregation.Average,
                    MeasurementWindow = TimeSpan.FromMinutes(5)
                }
            },
            Objectives = new List<ServiceLevelObjective>
            {
                new ServiceLevelObjective
                {
                    Name = "error_rate_slo",
                    SliName = "error_rate",
                    Operator = SloOperator.LessThanOrEqual,
                    TargetValue = 1.0, // Error rate should be <= 1%
                    EvaluationWindow = TimeSpan.FromMinutes(5)
                }
            }
        };
        var alerting = new AlertingEngine(_metrics, config, _alertingLogger);

        // Act: Record high error rate
        _metrics.RecordHistogram("error_rate", 5.0);
        await alerting.EvaluateAsync();
        await alerting.EvaluateAsync();
        await alerting.EvaluateAsync();
        var alerts = await alerting.EvaluateAsync();

        // Assert
        Assert.NotEmpty(alerts);
        Assert.Equal("error_rate_slo", alerts[0].Name);
    }
}
