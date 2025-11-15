using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// FFmpeg direct check candidate result with full diagnostic information
/// </summary>
public record FFmpegCheckCandidate(
    string Label,
    string? Path,
    bool Exists,
    bool ExecutionAttempted,
    int? ExitCode,
    bool TimedOut,
    string? RawVersionOutput,
    string? VersionParsed,
    bool Valid,
    string? Error);

/// <summary>
/// Overall FFmpeg direct check result
/// </summary>
public record FFmpegDirectCheckOverall(
    bool Installed,
    bool Valid,
    string? Source,
    string? ChosenPath,
    string? Version);

/// <summary>
/// Complete FFmpeg direct check response with all candidates checked
/// </summary>
public record FFmpegDirectCheckResponse(
    List<FFmpegCheckCandidate> Candidates,
    FFmpegDirectCheckOverall Overall,
    string CorrelationId);

/// <summary>
/// Simplified FFmpeg status response for normal UI
/// </summary>
public record FFmpegStatusResponse(
    bool Installed,
    bool Valid,
    string? Source,
    string? Version,
    string? Path,
    string? ErrorCode,
    string? ErrorMessage,
    string CorrelationId);

/// <summary>
/// FFmpeg install error response
/// </summary>
public record FFmpegInstallErrorResponse(
    bool Success,
    string ErrorCode,
    string Message,
    string? Details,
    string? HttpStatus,
    string? DownloadUrl,
    string CorrelationId);

/// <summary>
/// FFmpeg install success response
/// </summary>
public record FFmpegInstallSuccessResponse(
    bool Success,
    string Message,
    string Path,
    string? Version,
    DateTime InstalledAt,
    string CorrelationId);
