using System;
using System.Linq;

namespace Aura.Core.Timeline.Operations;

/// <summary>
/// Rolls the edit point between two adjacent clips, extending one and trimming the other
/// </summary>
public static class RollOperation
{
    /// <summary>
    /// Roll the edit point between two adjacent clips
    /// </summary>
    /// <param name="timeline">The timeline model</param>
    /// <param name="firstClipId">ID of the first clip (will be trimmed at out-point)</param>
    /// <param name="secondClipId">ID of the second clip (will be trimmed at in-point)</param>
    /// <param name="delta">Time delta to roll (positive = extend first/trim second, negative = trim first/extend second)</param>
    /// <returns>True if the roll was successful</returns>
    public static bool Execute(TimelineModel timeline, string firstClipId, string secondClipId, TimeSpan delta)
    {
        var firstClip = timeline.FindClip(firstClipId);
        var secondClip = timeline.FindClip(secondClipId);

        if (firstClip == null || secondClip == null) return false;

        var firstTrack = timeline.GetTrackForClip(firstClipId);
        var secondTrack = timeline.GetTrackForClip(secondClipId);

        if (firstTrack != secondTrack) return false;

        var firstEnd = firstClip.TimelineStart + firstClip.Duration;
        if (Math.Abs((firstEnd - secondClip.TimelineStart).TotalMilliseconds) > 1)
        {
            return false;
        }

        timeline.SaveState();

        var newFirstSourceOut = firstClip.SourceOut + delta;
        if (newFirstSourceOut <= firstClip.SourceIn || newFirstSourceOut > firstClip.SourceOut + TimeSpan.FromHours(24))
        {
            return false;
        }

        var newSecondSourceIn = secondClip.SourceIn + delta;
        var newSecondTimelineStart = secondClip.TimelineStart + delta;
        if (newSecondSourceIn < TimeSpan.Zero || newSecondSourceIn >= secondClip.SourceOut)
        {
            return false;
        }

        var updatedFirstClip = firstClip.WithSourceOut(newFirstSourceOut);
        var updatedSecondClip = secondClip
            .WithSourceIn(newSecondSourceIn)
            .WithTimelineStart(newSecondTimelineStart);

        timeline.UpdateClip(updatedFirstClip);
        timeline.UpdateClip(updatedSecondClip);

        return true;
    }
}
