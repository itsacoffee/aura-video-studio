namespace Aura.Api.Configuration;

/// <summary>
/// Configuration options for monitoring and alerting
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// Enable Application Insights integration
    /// </summary>
    public bool EnableApplicationInsights { get; set; } = true;

    /// <summary>
    /// Application Insights connection string
    /// </summary>
    public string ApplicationInsightsConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Application Insights instrumentation key (legacy)
    /// </summary>
    public string ApplicationInsightsInstrumentationKey { get; set; } = string.Empty;

    /// <summary>
    /// Enable custom metrics collection
    /// </summary>
    public bool EnableCustomMetrics { get; set; } = true;

    /// <summary>
    /// Enable business KPI tracking
    /// </summary>
    public bool EnableBusinessMetrics { get; set; } = true;

    /// <summary>
    /// Enable SLI/SLO monitoring
    /// </summary>
    public bool EnableSliSloMonitoring { get; set; } = true;

    /// <summary>
    /// SLO evaluation interval in seconds
    /// </summary>
    public int SloEvaluationIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Enable alerting
    /// </summary>
    public bool EnableAlerting { get; set; } = true;

    /// <summary>
    /// Alert evaluation interval in seconds
    /// </summary>
    public int AlertEvaluationIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Metrics export interval in seconds
    /// </summary>
    public int MetricsExportIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Enable synthetic monitoring
    /// </summary>
    public bool EnableSyntheticMonitoring { get; set; } = true;

    /// <summary>
    /// Synthetic health check interval in seconds
    /// </summary>
    public int SyntheticCheckIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Metric retention days
    /// </summary>
    public int MetricRetentionDays { get; set; } = 90;

    /// <summary>
    /// Enable anomaly detection
    /// </summary>
    public bool EnableAnomalyDetection { get; set; }

    /// <summary>
    /// Anomaly detection sensitivity (0.0 to 1.0)
    /// </summary>
    public double AnomalyDetectionSensitivity { get; set; } = 0.7;

    /// <summary>
    /// Log Analytics workspace ID
    /// </summary>
    public string LogAnalyticsWorkspaceId { get; set; } = string.Empty;

    /// <summary>
    /// Log Analytics shared key
    /// </summary>
    public string LogAnalyticsSharedKey { get; set; } = string.Empty;

    /// <summary>
    /// Notification channels configuration
    /// </summary>
    public NotificationChannelsOptions NotificationChannels { get; set; } = new();
}

public class NotificationChannelsOptions
{
    public SlackOptions? Slack { get; set; }
    public PagerDutyOptions? PagerDuty { get; set; }
    public EmailOptions? Email { get; set; }
    public WebhookOptions? Webhook { get; set; }
}

public class SlackOptions
{
    public bool Enabled { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string Channel { get; set; } = "#alerts";
    public List<string> MentionUsers { get; set; } = new();
}

public class PagerDutyOptions
{
    public bool Enabled { get; set; }
    public string IntegrationKey { get; set; } = string.Empty;
    public string ServiceKey { get; set; } = string.Empty;
}

public class EmailOptions
{
    public bool Enabled { get; set; }
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string FromAddress { get; set; } = string.Empty;
    public List<string> ToAddresses { get; set; } = new();
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class WebhookOptions
{
    public bool Enabled { get; set; }
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public int TimeoutSeconds { get; set; } = 30;
}
