using System;
using System.Collections.Generic;
using System.Linq;

namespace Aura.Core.Timeline;

/// <summary>
/// Represents a timeline track (video or audio)
/// </summary>
public enum TrackType
{
    Video,
    Audio
}

/// <summary>
/// Represents a clip on the timeline with timecode ranges
/// </summary>
public record TimelineClip
{
    public string Id { get; init; }
    public string SourcePath { get; init; }
    public TimeSpan SourceIn { get; init; }
    public TimeSpan SourceOut { get; init; }
    public TimeSpan TimelineStart { get; init; }
    public TimeSpan Duration => SourceOut - SourceIn;
    public string TrackId { get; init; }

    public TimelineClip(string id, string sourcePath, TimeSpan sourceIn, TimeSpan sourceOut, TimeSpan timelineStart, string trackId)
    {
        Id = id;
        SourcePath = sourcePath;
        SourceIn = sourceIn;
        SourceOut = sourceOut;
        TimelineStart = timelineStart;
        TrackId = trackId;
    }

    public TimelineClip WithSourceIn(TimeSpan newIn) =>
        this with { SourceIn = newIn };

    public TimelineClip WithSourceOut(TimeSpan newOut) =>
        this with { SourceOut = newOut };

    public TimelineClip WithTimelineStart(TimeSpan newStart) =>
        this with { TimelineStart = newStart };
}

/// <summary>
/// Represents a timeline track
/// </summary>
public record Track
{
    public string Id { get; init; }
    public string Name { get; init; }
    public TrackType Type { get; init; }
    public List<TimelineClip> Clips { get; init; }

    public Track(string id, string name, TrackType type)
    {
        Id = id;
        Name = name;
        Type = type;
        Clips = new List<TimelineClip>();
    }

    public Track WithClips(List<TimelineClip> clips) =>
        new Track(Id, Name, Type) { Clips = clips };
}

/// <summary>
/// Represents a chapter marker on the timeline
/// </summary>
public record ChapterMarker(string Id, string Title, TimeSpan Time);

/// <summary>
/// Represents the state of the timeline for undo/redo
/// </summary>
public record TimelineState(List<Track> Tracks, List<ChapterMarker> Markers);

/// <summary>
/// Main timeline model with tracks V1, V2, A1, A2 and undo/redo support
/// </summary>
public class TimelineModel
{
    private readonly Stack<TimelineState> _undoStack = new();
    private readonly Stack<TimelineState> _redoStack = new();
    
    public List<Track> Tracks { get; private set; }
    public List<ChapterMarker> Markers { get; private set; }
    public bool SnappingEnabled { get; set; } = true;
    public TimeSpan SnapThreshold { get; set; } = TimeSpan.FromMilliseconds(100);

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public TimelineModel()
    {
        Tracks = new List<Track>
        {
            new Track("V1", "Video 1", TrackType.Video),
            new Track("V2", "Video 2", TrackType.Video),
            new Track("A1", "Audio 1", TrackType.Audio),
            new Track("A2", "Audio 2", TrackType.Audio)
        };
        Markers = new List<ChapterMarker>();
    }

    /// <summary>
    /// Save current state to undo stack before making changes
    /// </summary>
    public void SaveState()
    {
        var state = new TimelineState(
            Tracks.Select(t => t.WithClips(new List<TimelineClip>(t.Clips))).ToList(),
            new List<ChapterMarker>(Markers)
        );
        _undoStack.Push(state);
        _redoStack.Clear();
    }

    /// <summary>
    /// Undo the last operation
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;

        var currentState = new TimelineState(
            Tracks.Select(t => t.WithClips(new List<TimelineClip>(t.Clips))).ToList(),
            new List<ChapterMarker>(Markers)
        );
        _redoStack.Push(currentState);

        var previousState = _undoStack.Pop();
        Tracks = previousState.Tracks;
        Markers = previousState.Markers;
    }

    /// <summary>
    /// Redo the last undone operation
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;

        var currentState = new TimelineState(
            Tracks.Select(t => t.WithClips(new List<TimelineClip>(t.Clips))).ToList(),
            new List<ChapterMarker>(Markers)
        );
        _undoStack.Push(currentState);

        var nextState = _redoStack.Pop();
        Tracks = nextState.Tracks;
        Markers = nextState.Markers;
    }

    /// <summary>
    /// Get all clips across all tracks
    /// </summary>
    public IEnumerable<TimelineClip> GetAllClips()
    {
        return Tracks.SelectMany(t => t.Clips);
    }

    /// <summary>
    /// Find a clip by ID
    /// </summary>
    public TimelineClip? FindClip(string clipId)
    {
        return GetAllClips().FirstOrDefault(c => c.Id == clipId);
    }

    /// <summary>
    /// Get the track containing a specific clip
    /// </summary>
    public Track? GetTrackForClip(string clipId)
    {
        return Tracks.FirstOrDefault(t => t.Clips.Any(c => c.Id == clipId));
    }

    /// <summary>
    /// Update a clip in its track
    /// </summary>
    public void UpdateClip(TimelineClip updatedClip)
    {
        var track = GetTrackForClip(updatedClip.Id);
        if (track == null) return;

        var index = track.Clips.FindIndex(c => c.Id == updatedClip.Id);
        if (index >= 0)
        {
            track.Clips[index] = updatedClip;
        }
    }

    /// <summary>
    /// Add a clip to a track
    /// </summary>
    public void AddClip(string trackId, TimelineClip clip)
    {
        var track = Tracks.FirstOrDefault(t => t.Id == trackId);
        if (track != null)
        {
            track.Clips.Add(clip);
            SortTrackClips(track);
        }
    }

    /// <summary>
    /// Remove a clip from its track
    /// </summary>
    public void RemoveClip(string clipId)
    {
        var track = GetTrackForClip(clipId);
        if (track != null)
        {
            track.Clips.RemoveAll(c => c.Id == clipId);
        }
    }

    /// <summary>
    /// Sort clips in a track by timeline start time
    /// </summary>
    private void SortTrackClips(Track track)
    {
        track.Clips.Sort((a, b) => a.TimelineStart.CompareTo(b.TimelineStart));
    }

    /// <summary>
    /// Snap a time value to nearby clip boundaries if snapping is enabled
    /// </summary>
    public TimeSpan SnapTime(TimeSpan time)
    {
        if (!SnappingEnabled) return time;

        var allClips = GetAllClips().ToList();
        var allMarkers = Markers.ToList();

        var snapPoints = new List<TimeSpan> { TimeSpan.Zero };
        snapPoints.AddRange(allClips.Select(c => c.TimelineStart));
        snapPoints.AddRange(allClips.Select(c => c.TimelineStart + c.Duration));
        snapPoints.AddRange(allMarkers.Select(m => m.Time));

        var nearest = snapPoints
            .Select(p => new { Point = p, Distance = Math.Abs((p - time).TotalMilliseconds) })
            .Where(x => x.Distance <= SnapThreshold.TotalMilliseconds)
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        return nearest?.Point ?? time;
    }

    /// <summary>
    /// Add a chapter marker
    /// </summary>
    public void AddMarker(ChapterMarker marker)
    {
        Markers.Add(marker);
        Markers.Sort((a, b) => a.Time.CompareTo(b.Time));
    }

    /// <summary>
    /// Remove a chapter marker
    /// </summary>
    public void RemoveMarker(string markerId)
    {
        Markers.RemoveAll(m => m.Id == markerId);
    }

    /// <summary>
    /// Export chapters in YouTube format (time stamps with titles)
    /// </summary>
    public string ExportChapters()
    {
        var lines = new List<string>();
        foreach (var marker in Markers.OrderBy(m => m.Time))
        {
            var timeStr = FormatTimecode(marker.Time);
            lines.Add($"{timeStr} {marker.Title}");
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Format a TimeSpan as YouTube timecode (H:MM:SS or M:SS)
    /// </summary>
    private static string FormatTimecode(TimeSpan time)
    {
        if (time.Hours > 0)
        {
            return $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}";
        }
        return $"{time.Minutes}:{time.Seconds:D2}";
    }
}
