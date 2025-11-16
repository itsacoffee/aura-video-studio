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
    string Mode,
    string? Error,
    string? ErrorCode,
    string? ErrorMessage,
    DateTime? LastValidatedAt,
    string? LastValidationResult,
    string CorrelationId);

/// <summary>
/// Response returned when forcing detection
/// </summary>
public record FFmpegDetectResponse(
    bool Success,
    bool Installed,
    bool Valid,
    string? Version,
    string? Path,
    string Source,
    string Mode,
    string Message,
    IReadOnlyList<string>? AttemptedPaths,
    string? Detail,
    string[]? HowToFix,
    string CorrelationId);

/// <summary>
/// Response returned when rescanning for FFmpeg
/// </summary>
public record FFmpegRescanResponse(
    bool Success,
    bool Installed,
    string? Version,
    string? Path,
    string? Source,
    bool Valid,
    string? Error,
    string Message,
    string CorrelationId);

/// <summary>
/// Response returned for path validation endpoints (set-path / use-existing)
/// </summary>
public record FFmpegPathValidationResponse(
    bool Success,
    string Message,
    bool Installed,
    bool Valid,
    string? Path,
    string? Version,
    string Source,
    string Mode,
    string CorrelationId,
    string? Title = null,
    string? Detail = null,
    string? ErrorCode = null,
    string[]? HowToFix = null,
    IReadOnlyList<string>? AttemptedPaths = null);

/// <summary>
/// FFmpeg install error response
/// </summary>
public record FFmpegInstallErrorResponse(
    bool Success,
    string Message,
    string? Title,
    string? Detail,
    string? ErrorCode,
    string? Type,
    string[]? HowToFix,
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
    string? Mode,
    string CorrelationId);
