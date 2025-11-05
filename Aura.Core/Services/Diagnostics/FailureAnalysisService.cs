using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Diagnostics;
using Aura.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Diagnostics;

/// <summary>
/// Service for AI-powered analysis of job failures with telemetry insights
/// </summary>
public class FailureAnalysisService
{
    private readonly ILogger<FailureAnalysisService> _logger;
    private readonly RunTelemetryCollector? _telemetryCollector;

    public FailureAnalysisService(
        ILogger<FailureAnalysisService> logger,
        RunTelemetryCollector? telemetryCollector = null)
    {
        _logger = logger;
        _telemetryCollector = telemetryCollector;
    }

    /// <summary>
    /// Analyze a failed job and provide root cause analysis with recommendations
    /// Integrates telemetry data for cost/latency anomaly detection
    /// </summary>
    public async Task<FailureAnalysis> AnalyzeFailureAsync(
        Job job,
        List<LogEntry>? logs = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing failure for job {JobId}", job.Id);

        await Task.CompletedTask;

        // Load telemetry for enhanced analysis
        RunTelemetryCollection? telemetry = null;
        TelemetryAnomalies? anomalies = null;
        
        if (_telemetryCollector != null)
        {
            telemetry = _telemetryCollector.LoadTelemetry(job.Id);
            if (telemetry != null)
            {
                anomalies = TelemetryAnomalyDetector.DetectAnomalies(telemetry);
                _logger.LogInformation("Telemetry loaded for job {JobId}: {RecordCount} records, {AnomalyCount} anomalies detected", 
                    job.Id, telemetry.Records.Count, 
                    anomalies.CostAnomalies.Count + anomalies.LatencyAnomalies.Count + anomalies.ProviderIssues.Count);
            }
        }

        // Analyze error patterns (now with telemetry context)
        var rootCauses = AnalyzeErrorPatterns(job, logs, telemetry, anomalies);
        
        // Sort by confidence
        rootCauses = rootCauses.OrderByDescending(rc => rc.Confidence).ToList();

        if (rootCauses.Count == 0)
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.Unknown,
                Description = "Unable to determine specific root cause from available information",
                Evidence = new List<string> { job.ErrorMessage ?? "No error message available" },
                Confidence = 30
            });
        }

        var primaryCause = rootCauses.First();
        var secondaryCauses = rootCauses.Skip(1).ToList();

        // Generate recommendations based on root causes (now with telemetry insights)
        var recommendations = GenerateRecommendations(primaryCause, secondaryCauses, job, telemetry, anomalies);

        // Generate summary (now with telemetry context)
        var summary = GenerateSummary(job, primaryCause, anomalies);

        // Get documentation links
        var docLinks = GetDocumentationLinks(primaryCause.Type);

        return new FailureAnalysis
        {
            JobId = job.Id,
            AnalyzedAt = DateTime.UtcNow,
            PrimaryRootCause = primaryCause,
            SecondaryRootCauses = secondaryCauses,
            RecommendedActions = recommendations,
            Summary = summary,
            DocumentationLinks = docLinks,
            ConfidenceScore = primaryCause.Confidence
        };
    }

    /// <summary>
    /// Analyze error patterns to identify root causes
    /// Enhanced with telemetry-based insights
    /// </summary>
    private List<RootCause> AnalyzeErrorPatterns(
        Job job, 
        List<LogEntry>? logs, 
        RunTelemetryCollection? telemetry = null,
        TelemetryAnomalies? anomalies = null)
    {
        var rootCauses = new List<RootCause>();
        var errorMessage = job.ErrorMessage?.ToLowerInvariant() ?? string.Empty;
        var errorCode = job.FailureDetails?.ErrorCode?.ToLowerInvariant() ?? string.Empty;

        // Check for provider issues from telemetry first (higher confidence)
        if (anomalies?.ProviderIssues != null && anomalies.ProviderIssues.Count > 0)
        {
            foreach (var issue in anomalies.ProviderIssues)
            {
                if (issue.IssueType == "HighErrorRate" && issue.Severity == AnomalySeverity.High)
                {
                    var rcType = issue.ErrorRate > 0.9 ? RootCauseType.ProviderUnavailable : RootCauseType.NetworkError;
                    rootCauses.Add(new RootCause
                    {
                        Type = rcType,
                        Description = $"{issue.Provider} provider showing {issue.ErrorRate * 100:F0}% error rate with {issue.ErrorCount} failures",
                        Evidence = issue.ErrorCodes.Take(3).ToList(),
                        Confidence = 92,
                        Stage = job.Stage,
                        Provider = issue.Provider
                    });
                }
            }
        }

        // Check for rate limit errors
        if (ContainsPattern(errorMessage, new[] { "rate limit", "429", "too many requests", "quota exceeded" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.RateLimit,
                Description = "API rate limit exceeded - Too many requests sent to the provider in a short time",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 90,
                Stage = job.Stage,
                Provider = ExtractProvider(errorMessage)
            });
        }

        // Check for invalid API key
        if (ContainsPattern(errorMessage, new[] { "invalid api key", "invalid key", "unauthorized", "401", "authentication failed" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.InvalidApiKey,
                Description = "Invalid or expired API key - The provided API key is not valid",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 95,
                Stage = job.Stage,
                Provider = ExtractProvider(errorMessage)
            });
        }

        // Check for missing API key
        if (ContainsPattern(errorMessage, new[] { "missing api key", "api key not found", "no api key", "api key required" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.MissingApiKey,
                Description = "Missing API key - No API key configured for the provider",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 98,
                Stage = job.Stage,
                Provider = ExtractProvider(errorMessage)
            });
        }

        // Check for network errors
        if (ContainsPattern(errorMessage, new[] { "network", "connection", "timeout", "unreachable", "dns", "socket" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.NetworkError,
                Description = "Network connectivity issue - Unable to reach the provider's API",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 85,
                Stage = job.Stage
            });
        }

        // Check for codec errors
        if (ContainsPattern(errorMessage, new[] { "codec", "encoder", "h264", "h265", "nvenc", "libx264" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.MissingCodec,
                Description = "Missing codec or encoder - Required video codec not available",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 92,
                Stage = job.Stage
            });
        }

        // Check for FFmpeg errors
        if (ContainsPattern(errorMessage, new[] { "ffmpeg not found", "ffmpeg", "cannot find ffmpeg" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.FFmpegNotFound,
                Description = "FFmpeg not found or not properly configured",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 95,
                Stage = job.Stage
            });
        }

        // Check for resource errors
        if (ContainsPattern(errorMessage, new[] { "out of memory", "insufficient", "disk space", "no space" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.InsufficientResources,
                Description = "Insufficient system resources - Not enough memory, disk space, or other resources",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 88,
                Stage = job.Stage
            });
        }

        // Check for timeout errors
        if (ContainsPattern(errorMessage, new[] { "timeout", "timed out", "took too long" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.Timeout,
                Description = "Operation timeout - The operation took too long to complete",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 85,
                Stage = job.Stage
            });
        }

        // Check for provider unavailable
        if (ContainsPattern(errorMessage, new[] { "service unavailable", "503", "502", "bad gateway", "provider down" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.ProviderUnavailable,
                Description = "Provider service unavailable - The provider's API is temporarily down",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 90,
                Stage = job.Stage,
                Provider = ExtractProvider(errorMessage)
            });
        }

        // Check for budget errors
        if (ContainsPattern(errorMessage, new[] { "budget", "quota", "limit exceeded", "insufficient funds" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.BudgetExceeded,
                Description = "Budget or quota exceeded - Spending limit reached",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 93,
                Stage = job.Stage
            });
        }

        // Check for file system errors
        if (ContainsPattern(errorMessage, new[] { "permission denied", "access denied", "file not found", "directory" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.FileSystemError,
                Description = "File system error - Permission denied, file not found, or disk space issue",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 87,
                Stage = job.Stage
            });
        }

        // Check for invalid input
        if (ContainsPattern(errorMessage, new[] { "invalid", "malformed", "bad request", "400" }))
        {
            rootCauses.Add(new RootCause
            {
                Type = RootCauseType.InvalidInput,
                Description = "Invalid input or configuration - The request contains invalid data",
                Evidence = new List<string> { job.ErrorMessage ?? string.Empty },
                Confidence = 80,
                Stage = job.Stage
            });
        }

        return rootCauses;
    }

    /// <summary>
    /// Generate recommended actions based on root causes
    /// Enhanced with telemetry-based insights
    /// </summary>
    private List<RecommendedAction> GenerateRecommendations(
        RootCause primaryCause, 
        List<RootCause> secondaryCauses,
        Job job,
        RunTelemetryCollection? telemetry = null,
        TelemetryAnomalies? anomalies = null)
    {
        var recommendations = new List<RecommendedAction>();

        // Add cost/latency anomaly warnings if present
        if (anomalies?.HasAnyAnomalies == true)
        {
            if (anomalies.CostAnomalies.Any(a => a.Severity == AnomalySeverity.High))
            {
                var highCostAnomaly = anomalies.CostAnomalies.First(a => a.Severity == AnomalySeverity.High);
                recommendations.Add(new RecommendedAction
                {
                    Priority = 0,
                    Title = "⚠️ High Cost Detected",
                    Description = highCostAnomaly.Description,
                    Steps = new List<string>
                    {
                        "Review the cost breakdown in the diagnostic bundle",
                        "Consider switching to a more cost-effective provider",
                        "Reduce batch size or video length if applicable"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 5,
                    Type = ActionType.Configuration
                });
            }

            if (anomalies.LatencyAnomalies.Any(a => a.Severity == AnomalySeverity.High))
            {
                var slowAnomaly = anomalies.LatencyAnomalies.First(a => a.Severity == AnomalySeverity.High);
                recommendations.Add(new RecommendedAction
                {
                    Priority = 0,
                    Title = "⚠️ Slow Performance Detected",
                    Description = slowAnomaly.Description,
                    Steps = new List<string>
                    {
                        "Check network connectivity to provider",
                        "Consider using a faster provider or model",
                        "Enable caching to avoid repeated slow operations"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 5,
                    Type = ActionType.Configuration
                });
            }
        }

        switch (primaryCause.Type)
        {
            case RootCauseType.RateLimit:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Wait and Retry",
                    Description = "Rate limits typically reset after a few minutes to an hour depending on the provider",
                    Steps = new List<string>
                    {
                        "Wait 5-10 minutes before retrying",
                        "Check your provider dashboard for rate limit details",
                        "Consider upgrading your provider tier for higher limits"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 10,
                    Type = ActionType.WaitAndRetry
                });
                recommendations.Add(new RecommendedAction
                {
                    Priority = 2,
                    Title = "Enable Request Caching",
                    Description = "Reduce API calls by enabling caching for repeated requests",
                    Steps = new List<string>
                    {
                        "Open Settings > Advanced",
                        "Enable LLM response caching",
                        "Set appropriate cache duration"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 2,
                    Type = ActionType.Configuration
                });
                break;

            case RootCauseType.InvalidApiKey:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Update API Key",
                    Description = "The current API key is invalid or expired. Generate a new key from your provider dashboard",
                    Steps = new List<string>
                    {
                        $"Go to Settings > Providers > {primaryCause.Provider ?? "Provider"}",
                        "Remove the current API key",
                        "Generate a new API key from your provider's website",
                        "Enter the new key and test the connection"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 5,
                    Type = ActionType.ApiKey
                });
                break;

            case RootCauseType.MissingApiKey:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Add API Key",
                    Description = $"Configure an API key for {primaryCause.Provider ?? "the provider"}",
                    Steps = new List<string>
                    {
                        $"Sign up for an account at {primaryCause.Provider ?? "the provider"}'s website",
                        "Generate an API key from your account dashboard",
                        "Go to Settings > Providers in Aura",
                        "Enter your API key and save"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 10,
                    Type = ActionType.ApiKey
                });
                break;

            case RootCauseType.NetworkError:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Check Internet Connection",
                    Description = "Verify your internet connection and firewall settings",
                    Steps = new List<string>
                    {
                        "Test your internet connection",
                        "Check if other websites are accessible",
                        "Temporarily disable firewall/antivirus and retry",
                        "Check proxy settings if applicable"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 5,
                    Type = ActionType.Network
                });
                break;

            case RootCauseType.MissingCodec:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Install Missing Codec",
                    Description = "Install the required video codec or use an alternative",
                    Steps = new List<string>
                    {
                        "Go to Settings > Render",
                        "Try switching to software encoding (libx264)",
                        "If using NVENC, ensure NVIDIA drivers are up to date",
                        "Verify FFmpeg installation includes required codecs"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 15,
                    Type = ActionType.Configuration
                });
                break;

            case RootCauseType.FFmpegNotFound:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Install or Configure FFmpeg",
                    Description = "FFmpeg is required for video rendering",
                    Steps = new List<string>
                    {
                        "Download FFmpeg from ffmpeg.org",
                        "Extract to a permanent location",
                        "Add FFmpeg to your system PATH",
                        "Restart Aura Video Studio",
                        "Alternatively, configure FFmpeg path in Settings"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 10,
                    Type = ActionType.Installation
                });
                break;

            case RootCauseType.InsufficientResources:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Free Up System Resources",
                    Description = "Close other applications and free up disk space",
                    Steps = new List<string>
                    {
                        "Close unnecessary applications",
                        "Free up disk space (at least 10GB recommended)",
                        "Check Task Manager for high memory usage",
                        "Try reducing video resolution or quality settings"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 10,
                    Type = ActionType.Resources
                });
                break;

            case RootCauseType.Timeout:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Adjust Timeout Settings",
                    Description = "Increase timeout limits or reduce job complexity",
                    Steps = new List<string>
                    {
                        "Try creating a shorter video",
                        "Reduce the number of scenes",
                        "Use faster providers or models",
                        "Check Settings > Advanced for timeout configuration"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 5,
                    Type = ActionType.Configuration
                });
                break;

            case RootCauseType.ProviderUnavailable:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Switch to Alternative Provider",
                    Description = "The current provider is temporarily unavailable",
                    Steps = new List<string>
                    {
                        "Try again in a few minutes",
                        "Check provider status page",
                        "Switch to an alternative provider in Settings",
                        "Enable automatic provider fallback"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 5,
                    Type = ActionType.ProviderSwitch
                });
                break;

            case RootCauseType.BudgetExceeded:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Adjust Budget Limits",
                    Description = "Increase budget limits or reduce usage",
                    Steps = new List<string>
                    {
                        "Go to Settings > Cost Tracking",
                        "Review current spending",
                        "Increase budget limit if appropriate",
                        "Switch to cheaper providers or models"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 5,
                    Type = ActionType.Configuration
                });
                break;

            case RootCauseType.FileSystemError:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Check File Permissions",
                    Description = "Verify file and directory permissions",
                    Steps = new List<string>
                    {
                        "Check that Aura has write permissions to output directory",
                        "Try running Aura as administrator (Windows)",
                        "Verify disk space is available",
                        "Check antivirus isn't blocking file operations"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 5,
                    Type = ActionType.Configuration
                });
                break;

            case RootCauseType.InvalidInput:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Review Input Configuration",
                    Description = "Check your brief, plan, or settings for invalid values",
                    Steps = new List<string>
                    {
                        "Review your creative brief for unusual characters",
                        "Check video duration is within acceptable range",
                        "Verify resolution and aspect ratio settings",
                        "Try with default settings first"
                    },
                    CanAutomate = false,
                    EstimatedMinutes = 5,
                    Type = ActionType.Configuration
                });
                break;

            default:
                recommendations.Add(new RecommendedAction
                {
                    Priority = 1,
                    Title = "Download Diagnostic Bundle",
                    Description = "Generate a detailed diagnostic report for troubleshooting",
                    Steps = new List<string>
                    {
                        "Click 'Download Diagnostics' button",
                        "Review the bundle contents",
                        "Share with support if needed"
                    },
                    CanAutomate = true,
                    EstimatedMinutes = 2,
                    Type = ActionType.Support
                });
                break;
        }

        // Always add retry option unless it's a configuration issue
        if (primaryCause.Type != RootCauseType.MissingApiKey &&
            primaryCause.Type != RootCauseType.InvalidApiKey &&
            primaryCause.Type != RootCauseType.FFmpegNotFound)
        {
            recommendations.Add(new RecommendedAction
            {
                Priority = recommendations.Count + 1,
                Title = "Retry Job",
                Description = "Retry the failed job after addressing the issue",
                Steps = new List<string>
                {
                    "Address the primary issue first",
                    "Click 'Retry' on the failed job",
                    "Monitor progress for any new errors"
                },
                CanAutomate = true,
                EstimatedMinutes = 1,
                Type = ActionType.Retry
            });
        }

        return recommendations;
    }

    /// <summary>
    /// Generate human-readable summary
    /// Enhanced with telemetry context
    /// </summary>
    private string GenerateSummary(Job job, RootCause primaryCause, TelemetryAnomalies? anomalies = null)
    {
        var stage = job.Stage ?? "Unknown";
        var causeDesc = primaryCause.Description;

        var summary = $"Job {job.Id} failed during the {stage} stage. {causeDesc}";

        // Add telemetry insights to summary
        if (anomalies?.HasAnyAnomalies == true)
        {
            var insights = new List<string>();
            
            if (anomalies.CostAnomalies.Count > 0)
            {
                insights.Add($"{anomalies.CostAnomalies.Count} cost {(anomalies.CostAnomalies.Count == 1 ? "anomaly" : "anomalies")} detected");
            }
            
            if (anomalies.LatencyAnomalies.Count > 0)
            {
                insights.Add($"{anomalies.LatencyAnomalies.Count} latency issue(s) found");
            }
            
            if (anomalies.ProviderIssues.Count > 0)
            {
                insights.Add($"{anomalies.ProviderIssues.Count} provider issue(s) identified");
            }

            if (insights.Count > 0)
            {
                summary += $" Telemetry analysis shows: {string.Join(", ", insights)}.";
            }
        }

        return summary;
    }

    /// <summary>
    /// Get documentation links for a root cause type
    /// </summary>
    private List<DocumentationLink> GetDocumentationLinks(RootCauseType causeType)
    {
        var links = new List<DocumentationLink>();

        switch (causeType)
        {
            case RootCauseType.RateLimit:
                links.Add(new DocumentationLink
                {
                    Title = "Managing API Rate Limits",
                    Url = "https://docs.aura.studio/troubleshooting/rate-limits",
                    Description = "Learn how to handle and avoid rate limit errors"
                });
                break;

            case RootCauseType.InvalidApiKey:
            case RootCauseType.MissingApiKey:
                links.Add(new DocumentationLink
                {
                    Title = "Configuring Provider API Keys",
                    Url = "https://docs.aura.studio/setup/api-keys",
                    Description = "Step-by-step guide for setting up provider API keys"
                });
                break;

            case RootCauseType.FFmpegNotFound:
            case RootCauseType.MissingCodec:
                links.Add(new DocumentationLink
                {
                    Title = "FFmpeg Installation Guide",
                    Url = "https://docs.aura.studio/setup/ffmpeg",
                    Description = "How to install and configure FFmpeg"
                });
                break;

            case RootCauseType.NetworkError:
                links.Add(new DocumentationLink
                {
                    Title = "Network Troubleshooting",
                    Url = "https://docs.aura.studio/troubleshooting/network",
                    Description = "Troubleshoot network and firewall issues"
                });
                break;

            case RootCauseType.InsufficientResources:
                links.Add(new DocumentationLink
                {
                    Title = "System Requirements",
                    Url = "https://docs.aura.studio/setup/requirements",
                    Description = "Check minimum and recommended system requirements"
                });
                break;

            case RootCauseType.BudgetExceeded:
                links.Add(new DocumentationLink
                {
                    Title = "Cost Management Guide",
                    Url = "https://docs.aura.studio/cost-tracking/budgets",
                    Description = "How to configure and manage budgets"
                });
                break;
        }

        // Always add general troubleshooting guide
        links.Add(new DocumentationLink
        {
            Title = "General Troubleshooting",
            Url = "https://docs.aura.studio/troubleshooting",
            Description = "Common issues and solutions"
        });

        return links;
    }

    /// <summary>
    /// Check if text contains any of the patterns
    /// </summary>
    private bool ContainsPattern(string text, string[] patterns)
    {
        return patterns.Any(pattern => text.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extract provider name from error message
    /// </summary>
    private string? ExtractProvider(string errorMessage)
    {
        var providers = new[] { "OpenAI", "Anthropic", "Gemini", "ElevenLabs", "PlayHT", "Stable Diffusion" };
        
        foreach (var provider in providers)
        {
            if (errorMessage.Contains(provider, StringComparison.OrdinalIgnoreCase))
            {
                return provider;
            }
        }

        return null;
    }
}
