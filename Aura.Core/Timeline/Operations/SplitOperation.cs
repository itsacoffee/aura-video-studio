using System;

namespace Aura.Core.Timeline.Operations;

/// <summary>
/// Splits a clip at a specified time, creating two clips
/// </summary>
public static class SplitOperation
{
    /// <summary>
    /// Split a clip at the specified timeline position
    /// </summary>
    /// <param name="timeline">The timeline model</param>
    /// <param name="clipId">ID of the clip to split</param>
    /// <param name="splitTime">Timeline time where to split (must be within the clip)</param>
    /// <returns>True if the split was successful</returns>
    public static bool Execute(TimelineModel timeline, string clipId, TimeSpan splitTime)
    {
        var clip = timeline.FindClip(clipId);
        if (clip == null) return false;

        var clipEnd = clip.TimelineStart + clip.Duration;
        if (splitTime <= clip.TimelineStart || splitTime >= clipEnd)
        {
            return false;
        }

        timeline.SaveState();

        var offsetInClip = splitTime - clip.TimelineStart;
        var newSourceIn = clip.SourceIn + offsetInClip;

        var firstClip = clip.WithSourceOut(newSourceIn);

        var secondClipId = $"{clipId}_split_{Guid.NewGuid():N}";
        var secondClip = new TimelineClip(
            id: secondClipId,
            sourcePath: clip.SourcePath,
            sourceIn: newSourceIn,
            sourceOut: clip.SourceOut,
            timelineStart: splitTime,
            trackId: clip.TrackId
        );

        timeline.RemoveClip(clipId);
        timeline.AddClip(clip.TrackId, firstClip);
        timeline.AddClip(clip.TrackId, secondClip);

        return true;
    }
}
