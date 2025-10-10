using System;
using System.Linq;

namespace Aura.Core.Timeline.Operations;

/// <summary>
/// Slides a clip along the timeline without changing its source in/out points,
/// adjusting adjacent clips to accommodate the move
/// </summary>
public static class SlideOperation
{
    /// <summary>
    /// Slide a clip to a new timeline position, adjusting adjacent clips
    /// </summary>
    /// <param name="timeline">The timeline model</param>
    /// <param name="clipId">ID of the clip to slide</param>
    /// <param name="delta">Time delta to slide (positive = forward, negative = backward)</param>
    /// <returns>True if the slide was successful</returns>
    public static bool Execute(TimelineModel timeline, string clipId, TimeSpan delta)
    {
        var clip = timeline.FindClip(clipId);
        if (clip == null) return false;

        var track = timeline.GetTrackForClip(clipId);
        if (track == null) return false;

        var newTimelineStart = clip.TimelineStart + delta;
        if (newTimelineStart < TimeSpan.Zero)
        {
            return false;
        }

        timeline.SaveState();

        var sortedClips = track.Clips
            .Where(c => c.Id != clipId)
            .OrderBy(c => c.TimelineStart)
            .ToList();

        var clipIndex = sortedClips.FindIndex(c => c.TimelineStart > clip.TimelineStart);
        
        if (delta > TimeSpan.Zero)
        {
            var clipsToAdjust = sortedClips
                .Where(c => c.TimelineStart >= clip.TimelineStart + clip.Duration && 
                           c.TimelineStart < newTimelineStart + clip.Duration)
                .ToList();

            foreach (var adjacentClip in clipsToAdjust)
            {
                var adjustedClip = adjacentClip.WithTimelineStart(adjacentClip.TimelineStart - clip.Duration);
                timeline.UpdateClip(adjustedClip);
            }
        }
        else if (delta < TimeSpan.Zero)
        {
            var clipsToAdjust = sortedClips
                .Where(c => c.TimelineStart + c.Duration > newTimelineStart && 
                           c.TimelineStart + c.Duration <= clip.TimelineStart)
                .ToList();

            foreach (var adjacentClip in clipsToAdjust)
            {
                var adjustedClip = adjacentClip.WithTimelineStart(adjacentClip.TimelineStart + clip.Duration);
                timeline.UpdateClip(adjustedClip);
            }
        }

        var updatedClip = clip.WithTimelineStart(newTimelineStart);
        timeline.UpdateClip(updatedClip);

        return true;
    }
}
