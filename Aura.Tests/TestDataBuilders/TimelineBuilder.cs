using Aura.Core.Models.Timeline;

namespace Aura.Tests.TestDataBuilders;

/// <summary>
/// Builder for creating test Timeline instances with tracks and clips
/// </summary>
public class TimelineBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private double _duration = 60.0;
    private List<TimelineTrack> _tracks = new();
    private List<TimelineMarker> _markers = new();
    private double _frameRate = 30.0;

    public TimelineBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public TimelineBuilder WithDuration(double duration)
    {
        _duration = duration;
        return this;
    }

    public TimelineBuilder WithFrameRate(double frameRate)
    {
        _frameRate = frameRate;
        return this;
    }

    public TimelineBuilder WithTrack(TimelineTrack track)
    {
        _tracks.Add(track);
        return this;
    }

    public TimelineBuilder WithMarker(TimelineMarker marker)
    {
        _markers.Add(marker);
        return this;
    }

    public TimelineBuilder WithDefaultVideoTrack()
    {
        _tracks.Add(new TrackBuilder()
            .WithType(TrackType.Video)
            .WithName("Video Track")
            .Build());
        return this;
    }

    public TimelineBuilder WithDefaultAudioTrack()
    {
        _tracks.Add(new TrackBuilder()
            .WithType(TrackType.Audio)
            .WithName("Audio Track")
            .Build());
        return this;
    }

    public Timeline Build()
    {
        return new Timeline
        {
            Id = _id,
            Duration = _duration,
            Tracks = _tracks,
            Markers = _markers,
            FrameRate = _frameRate
        };
    }
}

/// <summary>
/// Builder for creating test TimelineTrack instances
/// </summary>
public class TrackBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "Track";
    private TrackType _type = TrackType.Video;
    private List<TimelineClip> _clips = new();
    private bool _isLocked;
    private bool _isMuted;

    public TrackBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public TrackBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TrackBuilder WithType(TrackType type)
    {
        _type = type;
        return this;
    }

    public TrackBuilder WithClip(TimelineClip clip)
    {
        _clips.Add(clip);
        return this;
    }

    public TrackBuilder Locked()
    {
        _isLocked = true;
        return this;
    }

    public TrackBuilder Muted()
    {
        _isMuted = true;
        return this;
    }

    public TimelineTrack Build()
    {
        return new TimelineTrack
        {
            Id = _id,
            Name = _name,
            Type = _type,
            Clips = _clips,
            IsLocked = _isLocked,
            IsMuted = _isMuted
        };
    }
}

/// <summary>
/// Builder for creating test TimelineClip instances
/// </summary>
public class ClipBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _assetId = Guid.NewGuid().ToString();
    private double _timelineStart = 0.0;
    private double _duration = 5.0;
    private double _trimStart = 0.0;
    private double _trimEnd = 5.0;
    private List<Effect> _effects = new();

    public ClipBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public ClipBuilder WithAssetId(string assetId)
    {
        _assetId = assetId;
        return this;
    }

    public ClipBuilder AtTime(double timelineStart)
    {
        _timelineStart = timelineStart;
        return this;
    }

    public ClipBuilder WithDuration(double duration)
    {
        _duration = duration;
        _trimEnd = _trimStart + duration;
        return this;
    }

    public ClipBuilder WithTrim(double trimStart, double trimEnd)
    {
        _trimStart = trimStart;
        _trimEnd = trimEnd;
        _duration = trimEnd - trimStart;
        return this;
    }

    public ClipBuilder WithEffect(Effect effect)
    {
        _effects.Add(effect);
        return this;
    }

    public TimelineClip Build()
    {
        return new TimelineClip
        {
            Id = _id,
            AssetId = _assetId,
            TimelineStart = _timelineStart,
            Duration = _duration,
            TrimStart = _trimStart,
            TrimEnd = _trimEnd,
            Effects = _effects
        };
    }
}
