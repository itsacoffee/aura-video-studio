using Microsoft.Extensions.Logging;

namespace Aura.Core.Monitoring;

/// <summary>
/// Alert evaluation and notification engine
/// </summary>
public class AlertingEngine
{
    private readonly MetricsCollector _metrics;
    private readonly SliSloConfiguration _config;
    private readonly ILogger<AlertingEngine> _logger;
    private readonly Dictionary<string, AlertState> _alertStates = new();
    private readonly object _lock = new();

    public AlertingEngine(
        MetricsCollector metrics,
        SliSloConfiguration config,
        ILogger<AlertingEngine> logger)
    {
        _metrics = metrics;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Evaluate all SLOs and trigger alerts if violated
    /// </summary>
    public async Task<List<Alert>> EvaluateAsync(CancellationToken ct = default)
    {
        var alerts = new List<Alert>();

        foreach (var slo in _config.Objectives)
        {
            var alert = await EvaluateSloAsync(slo, ct).ConfigureAwait(false);
            if (alert != null)
            {
                alerts.Add(alert);
            }
        }

        return alerts;
    }

    /// <summary>
    /// Evaluate a single SLO
    /// </summary>
    private async Task<Alert?> EvaluateSloAsync(ServiceLevelObjective slo, CancellationToken ct)
    {
        await Task.CompletedTask;
        // Find the corresponding SLI
        var sli = _config.Indicators.FirstOrDefault(i => i.Name == slo.SliName);
        if (sli == null)
        {
            _logger.LogWarning("SLI {SliName} not found for SLO {SloName}", slo.SliName, slo.Name);
            return null;
        }

        // Get current metric value
        var currentValue = GetMetricValue(sli);
        if (currentValue == null)
        {
            _logger.LogDebug("No metric value available for SLI {SliName}", sli.Name);
            return null;
        }

        // Evaluate condition
        var violated = EvaluateCondition(currentValue.Value, slo.Operator, slo.TargetValue);

        lock (_lock)
        {
            // Get or create alert state
            if (!_alertStates.TryGetValue(slo.Name, out var state))
            {
                state = new AlertState
                {
                    SloName = slo.Name,
                    Firing = false,
                    ConsecutiveViolations = 0,
                    LastEvaluation = DateTimeOffset.UtcNow
                };
                _alertStates[slo.Name] = state;
            }

            state.LastEvaluation = DateTimeOffset.UtcNow;
            state.LastValue = currentValue.Value;

            if (violated)
            {
                state.ConsecutiveViolations++;
                
                // Fire alert if threshold met (prevent flapping)
                if (state.ConsecutiveViolations >= 3 && !state.Firing)
                {
                    state.Firing = true;
                    state.FiredAt = DateTimeOffset.UtcNow;

                    var alert = new Alert
                    {
                        Name = slo.Name,
                        Description = slo.Description,
                        Severity = slo.Severity,
                        SliName = sli.Name,
                        CurrentValue = currentValue.Value,
                        TargetValue = slo.TargetValue,
                        Operator = slo.Operator,
                        FiredAt = state.FiredAt.Value,
                        NotificationChannels = slo.NotificationChannels
                    };

                    _logger.LogWarning(
                        "Alert firing: {AlertName} - {Description}. Current: {Current}, Target: {Target}",
                        alert.Name, alert.Description, alert.CurrentValue, alert.TargetValue);

                    return alert;
                }
            }
            else
            {
                // Clear alert if condition resolved
                if (state.Firing)
                {
                    _logger.LogInformation(
                        "Alert resolved: {AlertName}. Current: {Current}, Target: {Target}",
                        slo.Name, currentValue.Value, slo.TargetValue);
                }

                state.Firing = false;
                state.ConsecutiveViolations = 0;
            }
        }

        return null;
    }

    /// <summary>
    /// Get current value for an SLI metric
    /// </summary>
    private double? GetMetricValue(ServiceLevelIndicator sli)
    {
        switch (sli.Aggregation)
        {
            case SliAggregation.Average:
                var stats = _metrics.GetHistogramStats(sli.MetricName, sli.Tags);
                return stats?.Mean;

            case SliAggregation.Percentile50:
                stats = _metrics.GetHistogramStats(sli.MetricName, sli.Tags);
                return stats?.P50;

            case SliAggregation.Percentile90:
                stats = _metrics.GetHistogramStats(sli.MetricName, sli.Tags);
                return stats?.P90;

            case SliAggregation.Percentile95:
                stats = _metrics.GetHistogramStats(sli.MetricName, sli.Tags);
                return stats?.P95;

            case SliAggregation.Percentile99:
                stats = _metrics.GetHistogramStats(sli.MetricName, sli.Tags);
                return stats?.P99;

            case SliAggregation.Sum:
                return _metrics.GetCounterValue(sli.MetricName, sli.Tags);

            case SliAggregation.Count:
                return _metrics.GetCounterValue(sli.MetricName, sli.Tags);

            case SliAggregation.Rate:
                // Calculate rate from counter
                var counter = _metrics.GetCounterValue(sli.MetricName, sli.Tags);
                if (counter.HasValue)
                {
                    // Simple rate calculation - in production, this would use time windows
                    return counter.Value / sli.MeasurementWindow.TotalMinutes;
                }
                return null;

            default:
                return _metrics.GetGaugeValue(sli.MetricName, sli.Tags);
        }
    }

    /// <summary>
    /// Evaluate if a condition is violated
    /// </summary>
    private bool EvaluateCondition(double currentValue, SloOperator op, double targetValue)
    {
        return op switch
        {
            SloOperator.GreaterThan => currentValue <= targetValue,
            SloOperator.GreaterThanOrEqual => currentValue < targetValue,
            SloOperator.LessThan => currentValue >= targetValue,
            SloOperator.LessThanOrEqual => currentValue > targetValue,
            SloOperator.Equal => Math.Abs(currentValue - targetValue) > 0.001,
            _ => false
        };
    }

    /// <summary>
    /// Get current state of all alerts
    /// </summary>
    public Dictionary<string, AlertState> GetAlertStates()
    {
        lock (_lock)
        {
            return new Dictionary<string, AlertState>(_alertStates);
        }
    }
}

public class Alert
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string SliName { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double TargetValue { get; set; }
    public SloOperator Operator { get; set; }
    public DateTimeOffset FiredAt { get; set; }
    public List<string> NotificationChannels { get; set; } = new();
}

public class AlertState
{
    public string SloName { get; set; } = string.Empty;
    public bool Firing { get; set; }
    public int ConsecutiveViolations { get; set; }
    public DateTimeOffset LastEvaluation { get; set; }
    public DateTimeOffset? FiredAt { get; set; }
    public double? LastValue { get; set; }
}
