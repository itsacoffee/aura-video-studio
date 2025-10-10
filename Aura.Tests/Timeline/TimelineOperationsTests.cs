using System;
using System.Linq;
using Xunit;
using Aura.Core.Timeline;
using Aura.Core.Timeline.Operations;
using Aura.Core.Timeline.Overlays;

namespace Aura.Tests.Timeline;

public class TimelineOperationsTests
{
    [Fact]
    public void SplitOperation_Should_CreateTwoClips()
    {
        var timeline = new TimelineModel();
        var clip = new TimelineClip(
            id: "clip1",
            sourcePath: "/path/to/video.mp4",
            sourceIn: TimeSpan.FromSeconds(0),
            sourceOut: TimeSpan.FromSeconds(10),
            timelineStart: TimeSpan.FromSeconds(5),
            trackId: "V1"
        );
        timeline.AddClip("V1", clip);

        var splitTime = TimeSpan.FromSeconds(8);
        var result = SplitOperation.Execute(timeline, "clip1", splitTime);

        Assert.True(result);
        var track = timeline.Tracks.First(t => t.Id == "V1");
        Assert.Equal(2, track.Clips.Count);
        
        var firstClip = track.Clips.First();
        Assert.Equal(TimeSpan.FromSeconds(5), firstClip.TimelineStart);
        Assert.Equal(TimeSpan.FromSeconds(3), firstClip.Duration);

        var secondClip = track.Clips.Last();
        Assert.Equal(TimeSpan.FromSeconds(8), secondClip.TimelineStart);
        Assert.Equal(TimeSpan.FromSeconds(7), secondClip.Duration);
    }

    [Fact]
    public void SplitOperation_Should_FailOutsideClipBounds()
    {
        var timeline = new TimelineModel();
        var clip = new TimelineClip(
            id: "clip1",
            sourcePath: "/path/to/video.mp4",
            sourceIn: TimeSpan.FromSeconds(0),
            sourceOut: TimeSpan.FromSeconds(10),
            timelineStart: TimeSpan.FromSeconds(5),
            trackId: "V1"
        );
        timeline.AddClip("V1", clip);

        var result = SplitOperation.Execute(timeline, "clip1", TimeSpan.FromSeconds(20));

        Assert.False(result);
        var track = timeline.Tracks.First(t => t.Id == "V1");
        Assert.Single(track.Clips);
    }

    [Fact]
    public void RippleTrimOperation_Should_ShiftSubsequentClips()
    {
        var timeline = new TimelineModel();
        var clip1 = new TimelineClip("clip1", "/path/to/video.mp4", 
            TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0), "V1");
        var clip2 = new TimelineClip("clip2", "/path/to/video.mp4",
            TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), "V1");
        
        timeline.AddClip("V1", clip1);
        timeline.AddClip("V1", clip2);

        var result = RippleTrimOperation.Execute(timeline, "clip1", false, TimeSpan.FromSeconds(8));

        Assert.True(result);
        var track = timeline.Tracks.First(t => t.Id == "V1");
        var updatedClip1 = track.Clips.First(c => c.Id == "clip1");
        var updatedClip2 = track.Clips.First(c => c.Id == "clip2");

        Assert.Equal(TimeSpan.FromSeconds(8), updatedClip1.Duration);
        Assert.Equal(TimeSpan.FromSeconds(8), updatedClip2.TimelineStart);
    }

    [Fact]
    public void SlipOperation_Should_ChangeSourceWithoutTimelinePosition()
    {
        var timeline = new TimelineModel();
        var clip = new TimelineClip("clip1", "/path/to/video.mp4",
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(10), "V1");
        timeline.AddClip("V1", clip);

        var result = SlipOperation.Execute(timeline, "clip1", TimeSpan.FromSeconds(2));

        Assert.True(result);
        var track = timeline.Tracks.First(t => t.Id == "V1");
        var updatedClip = track.Clips.First();

        Assert.Equal(TimeSpan.FromSeconds(7), updatedClip.SourceIn);
        Assert.Equal(TimeSpan.FromSeconds(17), updatedClip.SourceOut);
        Assert.Equal(TimeSpan.FromSeconds(10), updatedClip.TimelineStart);
    }

    [Fact]
    public void SlideOperation_Should_MoveClipAndAdjustAdjacent()
    {
        var timeline = new TimelineModel();
        var clip1 = new TimelineClip("clip1", "/path/to/video.mp4",
            TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0), "V1");
        var clip2 = new TimelineClip("clip2", "/path/to/video.mp4",
            TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), "V1");
        var clip3 = new TimelineClip("clip3", "/path/to/video.mp4",
            TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), "V1");

        timeline.AddClip("V1", clip1);
        timeline.AddClip("V1", clip2);
        timeline.AddClip("V1", clip3);

        var result = SlideOperation.Execute(timeline, "clip2", TimeSpan.FromSeconds(5));

        Assert.True(result);
        var track = timeline.Tracks.First(t => t.Id == "V1");
        var updatedClip2 = track.Clips.First(c => c.Id == "clip2");
        
        Assert.Equal(TimeSpan.FromSeconds(10), updatedClip2.TimelineStart);
    }

    [Fact]
    public void RollOperation_Should_AdjustAdjacentClipBoundaries()
    {
        var timeline = new TimelineModel();
        var clip1 = new TimelineClip("clip1", "/path/to/video.mp4",
            TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0), "V1");
        var clip2 = new TimelineClip("clip2", "/path/to/video.mp4",
            TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), "V1");

        timeline.AddClip("V1", clip1);
        timeline.AddClip("V1", clip2);

        var result = RollOperation.Execute(timeline, "clip1", "clip2", TimeSpan.FromSeconds(2));

        Assert.True(result);
        var track = timeline.Tracks.First(t => t.Id == "V1");
        var updatedClip1 = track.Clips.First(c => c.Id == "clip1");
        var updatedClip2 = track.Clips.First(c => c.Id == "clip2");

        Assert.Equal(TimeSpan.FromSeconds(12), updatedClip1.Duration);
        Assert.Equal(TimeSpan.FromSeconds(12), updatedClip2.TimelineStart);
        Assert.Equal(TimeSpan.FromSeconds(8), updatedClip2.Duration);
    }

    [Fact]
    public void TimelineModel_UndoRedo_Should_RestoreState()
    {
        var timeline = new TimelineModel();
        var clip = new TimelineClip("clip1", "/path/to/video.mp4",
            TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0), "V1");
        timeline.AddClip("V1", clip);

        Assert.False(timeline.CanUndo);
        Assert.False(timeline.CanRedo);

        SplitOperation.Execute(timeline, "clip1", TimeSpan.FromSeconds(5));

        Assert.True(timeline.CanUndo);
        Assert.False(timeline.CanRedo);

        var track = timeline.Tracks.First(t => t.Id == "V1");
        Assert.Equal(2, track.Clips.Count);

        timeline.Undo();

        track = timeline.Tracks.First(t => t.Id == "V1");
        Assert.Single(track.Clips);
        Assert.False(timeline.CanUndo);
        Assert.True(timeline.CanRedo);

        timeline.Redo();

        track = timeline.Tracks.First(t => t.Id == "V1");
        Assert.Equal(2, track.Clips.Count);
        Assert.True(timeline.CanUndo);
        Assert.False(timeline.CanRedo);
    }

    [Fact]
    public void TimelineModel_ExportChapters_Should_FormatCorrectly()
    {
        var timeline = new TimelineModel();
        timeline.AddMarker(new ChapterMarker("m1", "Introduction", TimeSpan.FromSeconds(0)));
        timeline.AddMarker(new ChapterMarker("m2", "Main Content", TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(30)));
        timeline.AddMarker(new ChapterMarker("m3", "Conclusion", TimeSpan.FromHours(1) + TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(15)));

        var chapters = timeline.ExportChapters();

        Assert.Contains("0:00 Introduction", chapters);
        Assert.Contains("2:30 Main Content", chapters);
        Assert.Contains("1:05:15 Conclusion", chapters);
    }

    [Fact]
    public void TimelineModel_SnappingEnabled_Should_SnapToNearbyPoints()
    {
        var timeline = new TimelineModel { SnappingEnabled = true };
        var clip = new TimelineClip("clip1", "/path/to/video.mp4",
            TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), "V1");
        timeline.AddClip("V1", clip);

        var nearbyTime = TimeSpan.FromSeconds(5.05);
        var snappedTime = timeline.SnapTime(nearbyTime);

        Assert.Equal(TimeSpan.FromSeconds(5), snappedTime);
    }

    [Fact]
    public void TimelineModel_SnappingDisabled_Should_NotSnapToNearbyPoints()
    {
        var timeline = new TimelineModel { SnappingEnabled = false };
        var clip = new TimelineClip("clip1", "/path/to/video.mp4",
            TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), "V1");
        timeline.AddClip("V1", clip);

        var nearbyTime = TimeSpan.FromSeconds(5.05);
        var snappedTime = timeline.SnapTime(nearbyTime);

        Assert.Equal(nearbyTime, snappedTime);
    }
}
