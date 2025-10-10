using System;
using System.Linq;

namespace Aura.Core.Timeline.Operations;

/// <summary>
/// Trims a clip and shifts all subsequent clips to close or open gaps
/// </summary>
public static class RippleTrimOperation
{
    /// <summary>
    /// Trim the in-point or out-point of a clip and ripple subsequent clips
    /// </summary>
    /// <param name="timeline">The timeline model</param>
    /// <param name="clipId">ID of the clip to trim</param>
    /// <param name="trimIn">If true, trim the in-point; otherwise trim the out-point</param>
    /// <param name="newTime">New timeline position for the trim point</param>
    /// <returns>True if the trim was successful</returns>
    public static bool Execute(TimelineModel timeline, string clipId, bool trimIn, TimeSpan newTime)
    {
        var clip = timeline.FindClip(clipId);
        if (clip == null) return false;

        var track = timeline.GetTrackForClip(clipId);
        if (track == null) return false;

        timeline.SaveState();

        TimeSpan delta;
        TimelineClip updatedClip;

        if (trimIn)
        {
            if (newTime >= clip.TimelineStart + clip.Duration)
            {
                return false;
            }

            delta = newTime - clip.TimelineStart;
            var newSourceIn = clip.SourceIn + delta;

            if (newSourceIn >= clip.SourceOut)
            {
                return false;
            }

            updatedClip = clip.WithSourceIn(newSourceIn).WithTimelineStart(newTime);
        }
        else
        {
            if (newTime <= clip.TimelineStart)
            {
                return false;
            }

            var newDuration = newTime - clip.TimelineStart;
            var newSourceOut = clip.SourceIn + newDuration;

            if (newSourceOut <= clip.SourceIn)
            {
                return false;
            }

            delta = newDuration - clip.Duration;
            updatedClip = clip.WithSourceOut(newSourceOut);
        }

        timeline.UpdateClip(updatedClip);

        var clipsToShift = track.Clips
            .Where(c => c.TimelineStart > clip.TimelineStart && c.Id != clipId)
            .OrderBy(c => c.TimelineStart)
            .ToList();

        foreach (var clipToShift in clipsToShift)
        {
            var shiftedClip = clipToShift.WithTimelineStart(clipToShift.TimelineStart + delta);
            timeline.UpdateClip(shiftedClip);
        }

        return true;
    }
}
