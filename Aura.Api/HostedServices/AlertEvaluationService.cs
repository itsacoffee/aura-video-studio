using Aura.Api.Configuration;
using Aura.Core.Monitoring;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service to evaluate SLOs and trigger alerts
/// </summary>
public class AlertEvaluationService : BackgroundService
{
    private readonly AlertingEngine _alerting;
    private readonly ILogger<AlertEvaluationService> _logger;
    private readonly MonitoringOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    public AlertEvaluationService(
        AlertingEngine alerting,
        ILogger<AlertEvaluationService> logger,
        IOptions<MonitoringOptions> options,
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory)
    {
        _alerting = alerting;
        _logger = logger;
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Alert Evaluation Service started");

        var interval = TimeSpan.FromSeconds(_options.AlertEvaluationIntervalSeconds);

        // Wait a bit before starting evaluations to let metrics accumulate
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_options.EnableAlerting)
                {
                    await EvaluateAlertsAsync(stoppingToken).ConfigureAwait(false);
                }

                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating alerts");
            }
        }

        _logger.LogInformation("Alert Evaluation Service stopped");
    }

    private async Task EvaluateAlertsAsync(CancellationToken ct)
    {
        try
        {
            var alerts = await _alerting.EvaluateAsync(ct).ConfigureAwait(false);

            if (alerts.Count > 0)
            {
                _logger.LogWarning("Found {AlertCount} active alerts", alerts.Count);

                foreach (var alert in alerts)
                {
                    await SendAlertNotificationsAsync(alert, ct).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate alerts");
        }
    }

    private async Task SendAlertNotificationsAsync(Alert alert, CancellationToken ct)
    {
        try
        {
            _logger.LogWarning(
                "ALERT [{Severity}]: {Name} - {Description}. Current: {Current}, Target: {Target}",
                alert.Severity.ToUpper(), alert.Name, alert.Description,
                alert.CurrentValue, alert.TargetValue);

            // Send notifications to configured channels
            foreach (var channel in alert.NotificationChannels)
            {
                await SendToChannelAsync(channel, alert, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert notifications for {AlertName}", alert.Name);
        }
    }

    private async Task SendToChannelAsync(string channel, Alert alert, CancellationToken ct)
    {
        try
        {
            switch (channel.ToLower())
            {
                case "slack":
                    await SendSlackNotificationAsync(alert, ct).ConfigureAwait(false);
                    break;

                case "pagerduty":
                    await SendPagerDutyNotificationAsync(alert, ct).ConfigureAwait(false);
                    break;

                case "email":
                    await SendEmailNotificationAsync(alert, ct).ConfigureAwait(false);
                    break;

                case "webhook":
                    await SendWebhookNotificationAsync(alert, ct).ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning("Unknown notification channel: {Channel}", channel);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to {Channel}", channel);
        }
    }

    private async Task SendSlackNotificationAsync(Alert alert, CancellationToken ct)
    {
        var slackConfig = _options.NotificationChannels.Slack;
        if (slackConfig == null || !slackConfig.Enabled || string.IsNullOrEmpty(slackConfig.WebhookUrl))
        {
            _logger.LogDebug("Slack notifications not configured");
            return;
        }

        try
        {
            var color = alert.Severity == "critical" ? "danger" : "warning";
            var emoji = alert.Severity == "critical" ? "🚨" : "⚠️";

            var payload = new
            {
                channel = slackConfig.Channel,
                username = "Aura Monitoring",
                icon_emoji = ":chart_with_upwards_trend:",
                attachments = new[]
                {
                    new
                    {
                        color,
                        title = $"{emoji} {alert.Name}",
                        text = alert.Description,
                        fields = new[]
                        {
                            new { title = "Severity", value = alert.Severity.ToUpper(), @short = true },
                            new { title = "SLI", value = alert.SliName, @short = true },
                            new { title = "Current Value", value = $"{alert.CurrentValue:F2}", @short = true },
                            new { title = "Target Value", value = $"{alert.TargetValue:F2}", @short = true },
                            new { title = "Fired At", value = alert.FiredAt.ToString("yyyy-MM-dd HH:mm:ss UTC"), @short = false }
                        },
                        footer = "Aura Monitoring",
                        ts = ((DateTimeOffset)alert.FiredAt).ToUnixTimeSeconds()
                    }
                }
            };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            var response = await httpClient.PostAsJsonAsync(slackConfig.WebhookUrl, payload, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Slack notification sent for alert: {AlertName}", alert.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack notification");
        }
    }

    private async Task SendPagerDutyNotificationAsync(Alert alert, CancellationToken ct)
    {
        var pdConfig = _options.NotificationChannels.PagerDuty;
        if (pdConfig == null || !pdConfig.Enabled || string.IsNullOrEmpty(pdConfig.IntegrationKey))
        {
            _logger.LogDebug("PagerDuty notifications not configured");
            return;
        }

        // PagerDuty integration would go here
        _logger.LogInformation("PagerDuty notification triggered for alert: {AlertName}", alert.Name);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task SendEmailNotificationAsync(Alert alert, CancellationToken ct)
    {
        var emailConfig = _options.NotificationChannels.Email;
        if (emailConfig == null || !emailConfig.Enabled || string.IsNullOrEmpty(emailConfig.SmtpServer))
        {
            _logger.LogDebug("Email notifications not configured");
            return;
        }

        // Email sending would go here
        _logger.LogInformation("Email notification triggered for alert: {AlertName}", alert.Name);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task SendWebhookNotificationAsync(Alert alert, CancellationToken ct)
    {
        var webhookConfig = _options.NotificationChannels.Webhook;
        if (webhookConfig == null || !webhookConfig.Enabled || string.IsNullOrEmpty(webhookConfig.Url))
        {
            _logger.LogDebug("Webhook notifications not configured");
            return;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(webhookConfig.TimeoutSeconds);

            foreach (var header in webhookConfig.Headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var payload = new
            {
                alert = alert.Name,
                description = alert.Description,
                severity = alert.Severity,
                sli = alert.SliName,
                currentValue = alert.CurrentValue,
                targetValue = alert.TargetValue,
                firedAt = alert.FiredAt
            };

            var response = await httpClient.PostAsJsonAsync(webhookConfig.Url, payload, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Webhook notification sent for alert: {AlertName}", alert.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook notification");
        }
    }
}
