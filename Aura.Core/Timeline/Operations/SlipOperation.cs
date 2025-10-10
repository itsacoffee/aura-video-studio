using System;

namespace Aura.Core.Timeline.Operations;

/// <summary>
/// Slips the source in/out points of a clip without changing its timeline position or duration
/// </summary>
public static class SlipOperation
{
    /// <summary>
    /// Slip a clip's source content by a delta amount
    /// </summary>
    /// <param name="timeline">The timeline model</param>
    /// <param name="clipId">ID of the clip to slip</param>
    /// <param name="delta">Time delta to slip the source (positive = forward, negative = backward)</param>
    /// <returns>True if the slip was successful</returns>
    public static bool Execute(TimelineModel timeline, string clipId, TimeSpan delta)
    {
        var clip = timeline.FindClip(clipId);
        if (clip == null) return false;

        timeline.SaveState();

        var newSourceIn = clip.SourceIn + delta;
        var newSourceOut = clip.SourceOut + delta;

        if (newSourceIn < TimeSpan.Zero)
        {
            return false;
        }

        var updatedClip = clip.WithSourceIn(newSourceIn).WithSourceOut(newSourceOut);
        timeline.UpdateClip(updatedClip);

        return true;
    }
}
