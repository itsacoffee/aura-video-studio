using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// High-level summary of system health status
/// </summary>
public record HealthSummaryResponse(
    string OverallStatus,
    bool IsReady,
    int TotalChecks,
    int PassedChecks,
    int WarningChecks,
    int FailedChecks,
    DateTimeOffset Timestamp);

/// <summary>
/// Detailed health check information with per-check results
/// </summary>
public record HealthDetailsResponse(
    string OverallStatus,
    bool IsReady,
    IReadOnlyList<HealthCheckDetail> Checks,
    DateTimeOffset Timestamp);

/// <summary>
/// Individual health check detail with remediation information
/// </summary>
public record HealthCheckDetail(
    string Id,
    string Name,
    string Category,
    string Status,
    bool IsRequired,
    string? Message,
    Dictionary<string, object>? Data,
    string? RemediationHint,
    IReadOnlyList<RemediationAction>? RemediationActions);

/// <summary>
/// Actionable remediation step for a failed health check
/// </summary>
public record RemediationAction(
    string Type,
    string Label,
    string Description,
    string? NavigateTo,
    string? ExternalUrl,
    Dictionary<string, string>? Parameters);

/// <summary>
/// Health check categories
/// </summary>
public static class HealthCheckCategory
{
    public const string System = "System";
    public const string Configuration = "Configuration";
    public const string LLM = "LLM";
    public const string TTS = "TTS";
    public const string Image = "Image";
    public const string Video = "Video";
}

/// <summary>
/// Health check status values
/// </summary>
public static class HealthCheckStatus
{
    public const string Pass = "pass";
    public const string Warning = "warning";
    public const string Fail = "fail";
}

/// <summary>
/// Remediation action types
/// </summary>
public static class RemediationActionType
{
    public const string OpenSettings = "open_settings";
    public const string Install = "install";
    public const string Configure = "configure";
    public const string Start = "start";
    public const string OpenHelp = "open_help";
    public const string SwitchProvider = "switch_provider";
}

/// <summary>
/// Canonical system health response with comprehensive status information
/// </summary>
public record SystemHealthResponse(
    bool BackendOnline,
    string Version,
    string OverallStatus,
    DatabaseHealth Database,
    FfmpegHealth Ffmpeg,
    ProvidersSummary ProvidersSummary,
    DateTimeOffset Timestamp,
    string? CorrelationId = null);

/// <summary>
/// Database health information
/// </summary>
public record DatabaseHealth(
    string Status,
    bool MigrationUpToDate,
    string? Message = null);

/// <summary>
/// FFmpeg health information
/// </summary>
public record FfmpegHealth(
    bool Installed,
    bool Valid,
    string? Version = null,
    string? Path = null,
    string? Message = null);

/// <summary>
/// Providers summary information
/// </summary>
public record ProvidersSummary(
    int TotalConfigured,
    int TotalReachable,
    string? Message = null);
