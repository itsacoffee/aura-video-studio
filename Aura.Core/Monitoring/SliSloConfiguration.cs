namespace Aura.Core.Monitoring;

/// <summary>
/// Service Level Indicator (SLI) and Service Level Objective (SLO) configuration
/// </summary>
public class SliSloConfiguration
{
    public List<ServiceLevelIndicator> Indicators { get; set; } = new();
    public List<ServiceLevelObjective> Objectives { get; set; } = new();
}

/// <summary>
/// Service Level Indicator - A measurable metric of service quality
/// </summary>
public class ServiceLevelIndicator
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SliType Type { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
    public SliAggregation Aggregation { get; set; }
    public TimeSpan MeasurementWindow { get; set; }
}

/// <summary>
/// Service Level Objective - Target for an SLI
/// </summary>
public class ServiceLevelObjective
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SliName { get; set; } = string.Empty;
    public SloOperator Operator { get; set; }
    public double TargetValue { get; set; }
    public TimeSpan EvaluationWindow { get; set; }
    public string Severity { get; set; } = "warning";
    public List<string> NotificationChannels { get; set; } = new();
}

public enum SliType
{
    Availability,      // % of successful requests
    Latency,          // Request duration
    ErrorRate,        // % of failed requests
    Throughput,       // Requests per second
    Saturation        // Resource utilization
}

public enum SliAggregation
{
    Average,
    Percentile50,
    Percentile90,
    Percentile95,
    Percentile99,
    Sum,
    Count,
    Rate
}

public enum SloOperator
{
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Equal
}

/// <summary>
/// Default SLI/SLO configuration for Aura
/// </summary>
public static class DefaultSliSlo
{
    public static SliSloConfiguration GetDefault()
    {
        return new SliSloConfiguration
        {
            Indicators = new List<ServiceLevelIndicator>
            {
                // Availability SLIs
                new ServiceLevelIndicator
                {
                    Name = "api_availability",
                    Description = "Percentage of successful API requests",
                    Type = SliType.Availability,
                    MetricName = "api.requests",
                    Aggregation = SliAggregation.Rate,
                    MeasurementWindow = TimeSpan.FromMinutes(5)
                },
                new ServiceLevelIndicator
                {
                    Name = "job_success_rate",
                    Description = "Percentage of successfully completed jobs",
                    Type = SliType.Availability,
                    MetricName = "jobs.completed",
                    Aggregation = SliAggregation.Rate,
                    MeasurementWindow = TimeSpan.FromMinutes(15)
                },

                // Latency SLIs
                new ServiceLevelIndicator
                {
                    Name = "api_latency_p95",
                    Description = "95th percentile API response time",
                    Type = SliType.Latency,
                    MetricName = "api.request_duration_ms",
                    Aggregation = SliAggregation.Percentile95,
                    MeasurementWindow = TimeSpan.FromMinutes(5)
                },
                new ServiceLevelIndicator
                {
                    Name = "job_processing_p90",
                    Description = "90th percentile job processing time",
                    Type = SliType.Latency,
                    MetricName = "jobs.duration_seconds",
                    Aggregation = SliAggregation.Percentile90,
                    MeasurementWindow = TimeSpan.FromMinutes(15)
                },

                // Error Rate SLIs
                new ServiceLevelIndicator
                {
                    Name = "api_error_rate",
                    Description = "Rate of 5xx errors per minute",
                    Type = SliType.ErrorRate,
                    MetricName = "api.errors.5xx",
                    Aggregation = SliAggregation.Rate,
                    MeasurementWindow = TimeSpan.FromMinutes(5)
                },

                // Saturation SLIs
                new ServiceLevelIndicator
                {
                    Name = "queue_depth",
                    Description = "Job queue backlog size",
                    Type = SliType.Saturation,
                    MetricName = "queue.depth",
                    Aggregation = SliAggregation.Average,
                    MeasurementWindow = TimeSpan.FromMinutes(5)
                },

                // Throughput SLIs
                new ServiceLevelIndicator
                {
                    Name = "api_throughput",
                    Description = "API requests per second",
                    Type = SliType.Throughput,
                    MetricName = "api.requests",
                    Aggregation = SliAggregation.Rate,
                    MeasurementWindow = TimeSpan.FromMinutes(1)
                }
            },

            Objectives = new List<ServiceLevelObjective>
            {
                // Availability SLOs
                new ServiceLevelObjective
                {
                    Name = "api_availability_target",
                    Description = "API should be available 99.9% of the time",
                    SliName = "api_availability",
                    Operator = SloOperator.GreaterThanOrEqual,
                    TargetValue = 99.9,
                    EvaluationWindow = TimeSpan.FromMinutes(5),
                    Severity = "critical",
                    NotificationChannels = new List<string> { "pagerduty", "slack" }
                },
                new ServiceLevelObjective
                {
                    Name = "job_success_rate_target",
                    Description = "Jobs should succeed 95% of the time",
                    SliName = "job_success_rate",
                    Operator = SloOperator.GreaterThanOrEqual,
                    TargetValue = 95.0,
                    EvaluationWindow = TimeSpan.FromMinutes(15),
                    Severity = "warning",
                    NotificationChannels = new List<string> { "slack" }
                },

                // Latency SLOs
                new ServiceLevelObjective
                {
                    Name = "api_latency_target",
                    Description = "95% of API requests should complete within 2 seconds",
                    SliName = "api_latency_p95",
                    Operator = SloOperator.LessThanOrEqual,
                    TargetValue = 2000, // ms
                    EvaluationWindow = TimeSpan.FromMinutes(5),
                    Severity = "warning",
                    NotificationChannels = new List<string> { "slack" }
                },
                new ServiceLevelObjective
                {
                    Name = "job_processing_target",
                    Description = "90% of jobs should complete within 300 seconds",
                    SliName = "job_processing_p90",
                    Operator = SloOperator.LessThanOrEqual,
                    TargetValue = 300, // seconds
                    EvaluationWindow = TimeSpan.FromMinutes(15),
                    Severity = "warning",
                    NotificationChannels = new List<string> { "slack" }
                },

                // Error Rate SLOs
                new ServiceLevelObjective
                {
                    Name = "error_rate_target",
                    Description = "Error rate should be below 1%",
                    SliName = "api_error_rate",
                    Operator = SloOperator.LessThanOrEqual,
                    TargetValue = 1.0, // percent
                    EvaluationWindow = TimeSpan.FromMinutes(5),
                    Severity = "critical",
                    NotificationChannels = new List<string> { "pagerduty", "slack" }
                },

                // Saturation SLOs
                new ServiceLevelObjective
                {
                    Name = "queue_depth_target",
                    Description = "Queue depth should stay below 100 jobs",
                    SliName = "queue_depth",
                    Operator = SloOperator.LessThanOrEqual,
                    TargetValue = 100,
                    EvaluationWindow = TimeSpan.FromMinutes(5),
                    Severity = "warning",
                    NotificationChannels = new List<string> { "slack" }
                }
            }
        };
    }
}
