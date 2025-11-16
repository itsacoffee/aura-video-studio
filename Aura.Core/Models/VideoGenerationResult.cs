using Aura.Core.Models.Timeline;
using ProviderTimeline = Aura.Core.Providers.Timeline;

namespace Aura.Core.Models;

/// <summary>
/// Represents the detailed output of the video generation pipeline, including the
/// rendered file and the timelines used for further editing.
/// </summary>
public record VideoGenerationResult(
    string OutputPath,
    ProviderTimeline? ProviderTimeline,
    EditableTimeline? EditableTimeline,
    string? NarrationPath,
    string? SubtitlesPath,
    string? CorrelationId = null);

