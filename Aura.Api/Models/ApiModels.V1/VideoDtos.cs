using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Request for generating a new video
/// </summary>
public record VideoGenerationRequest(
    string Brief,
    string? VoiceId,
    string? Style,
    double DurationMinutes,
    VideoGenerationOptions? Options = null);

/// <summary>
/// Additional options for video generation
/// </summary>
public record VideoGenerationOptions(
    string? Audience = null,
    string? Goal = null,
    string? Tone = null,
    string? Language = null,
    string? Aspect = null,
    string? Pacing = null,
    string? Density = null,
    int? Width = null,
    int? Height = null,
    int? Fps = null,
    string? Codec = null,
    bool EnableHardwareAcceleration = true,
    DirectorPresetDto DirectorPreset = DirectorPresetDto.Documentary);

/// <summary>
/// Response from video generation initiation
/// </summary>
public record VideoGenerationResponse(
    string JobId,
    string Status,
    string? VideoUrl,
    DateTime CreatedAt,
    string CorrelationId);

/// <summary>
/// Progress update for video generation
/// </summary>
public record ProgressUpdate(
    int Percentage,
    string Stage,
    string Message,
    DateTime Timestamp,
    string? CurrentTask = null,
    TimeSpan? EstimatedTimeRemaining = null);

/// <summary>
/// Detailed video status information
/// </summary>
public record VideoStatus(
    string JobId,
    string Status,
    int ProgressPercentage,
    string CurrentStage,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? VideoUrl,
    string? ErrorMessage,
    List<string> ProcessingSteps,
    string CorrelationId);

/// <summary>
/// Video metadata information
/// </summary>
public record VideoMetadata(
    string JobId,
    string OutputPath,
    long FileSizeBytes,
    DateTime CreatedAt,
    DateTime CompletedAt,
    TimeSpan Duration,
    string Resolution,
    string Codec,
    int Fps,
    List<ArtifactInfo> Artifacts,
    string CorrelationId);

/// <summary>
/// Information about a generated artifact
/// </summary>
public record ArtifactInfo(
    string Name,
    string Path,
    string Type,
    long SizeBytes);

/// <summary>
/// Response from pipeline validation check
/// </summary>
public record PipelineValidationResponse(
    bool IsValid,
    List<string> Errors,
    DateTime Timestamp,
    string CorrelationId);

/// <summary>
/// Comprehensive preflight validation report with detailed status for each component.
/// </summary>
public record PipelinePreflightReportDto(
    bool Ok,
    DateTime Timestamp,
    int DurationMs,
    PipelineCheckResultDto FFmpeg,
    PipelineCheckResultDto Ollama,
    PipelineCheckResultDto TTS,
    PipelineCheckResultDto DiskSpace,
    PipelineCheckResultDto ImageProvider,
    List<string> Errors,
    List<string> Warnings);

/// <summary>
/// Result of an individual pipeline preflight check.
/// </summary>
public record PipelineCheckResultDto(
    bool Passed,
    bool Skipped,
    string Status,
    string? Details,
    string? SuggestedAction);

/// <summary>
/// AI Director preset styles for video aesthetics
/// </summary>
public enum DirectorPresetDto
{
    /// <summary>Steady, informative style with minimal motion</summary>
    Documentary,
    /// <summary>Fast-paced, dynamic style with quick cuts</summary>
    TikTokEnergy,
    /// <summary>Slow, dramatic style with emotional transitions</summary>
    Cinematic,
    /// <summary>Clean, professional style with subtle motion</summary>
    Corporate,
    /// <summary>Clear, focused style for comprehension</summary>
    Educational,
    /// <summary>Narrative-driven with emotion-matched pacing</summary>
    Storytelling,
    /// <summary>Manual control over all settings</summary>
    Custom
}

/// <summary>
/// Request for previewing AI Director decisions.
/// Uses existing SceneDto and BriefDto from Dtos.cs.
/// </summary>
public record PreviewDirectorRequest(
    List<SceneDto> Scenes,
    BriefDto Brief,
    DirectorPresetDto Preset);

/// <summary>
/// AI Director decisions response
/// </summary>
public record DirectorDecisionsResponse(
    List<SceneDirectionDto> SceneDirections,
    string OverallStyle,
    string EmotionalArc,
    string CorrelationId);

/// <summary>
/// Direction for a single scene
/// </summary>
public record SceneDirectionDto(
    int SceneIndex,
    string Motion,
    string InTransition,
    string OutTransition,
    double EmotionalIntensity,
    string VisualFocus,
    double SuggestedDurationSeconds,
    double KenBurnsIntensity);
