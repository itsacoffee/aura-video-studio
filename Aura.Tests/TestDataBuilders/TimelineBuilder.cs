using Aura.Core.Models.Timeline;

namespace Aura.Tests.TestDataBuilders;

/// <summary>
/// Builder for creating test EditableTimeline instances with scenes
/// </summary>
public class TimelineBuilder
{
    private List<TimelineScene> _scenes = new();
    private string? _backgroundMusicPath;
    private SubtitleTrack _subtitles = new();

    public TimelineBuilder WithScene(TimelineScene scene)
    {
        _scenes.Add(scene);
        return this;
    }

    public TimelineBuilder WithBackgroundMusic(string path)
    {
        _backgroundMusicPath = path;
        return this;
    }

    public TimelineBuilder WithSubtitles(SubtitleTrack subtitles)
    {
        _subtitles = subtitles;
        return this;
    }

    public TimelineBuilder WithDefaultScene()
    {
        _scenes.Add(new TimelineScene(
            Index: 0,
            Heading: "Test Scene",
            Script: "This is a test scene script.",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5)
        ));
        return this;
    }

    public EditableTimeline Build()
    {
        return new EditableTimeline
        {
            Scenes = _scenes,
            BackgroundMusicPath = _backgroundMusicPath,
            Subtitles = _subtitles
        };
    }
}

/// <summary>
/// Builder for creating test TimelineScene instances
/// </summary>
public class SceneBuilder
{
    private int _index = 0;
    private string _heading = "Scene";
    private string _script = "This is a test scene.";
    private TimeSpan _start = TimeSpan.Zero;
    private TimeSpan _duration = TimeSpan.FromSeconds(5);
    private string? _narrationAudioPath;
    private List<TimelineAsset> _visualAssets = new();
    private string _transitionType = "None";
    private TimeSpan? _transitionDuration;

    public SceneBuilder WithIndex(int index)
    {
        _index = index;
        return this;
    }

    public SceneBuilder WithHeading(string heading)
    {
        _heading = heading;
        return this;
    }

    public SceneBuilder WithScript(string script)
    {
        _script = script;
        return this;
    }

    public SceneBuilder WithStart(TimeSpan start)
    {
        _start = start;
        return this;
    }

    public SceneBuilder WithDuration(TimeSpan duration)
    {
        _duration = duration;
        return this;
    }

    public SceneBuilder WithNarrationAudio(string audioPath)
    {
        _narrationAudioPath = audioPath;
        return this;
    }

    public SceneBuilder WithVisualAsset(TimelineAsset asset)
    {
        _visualAssets.Add(asset);
        return this;
    }

    public SceneBuilder WithTransition(string transitionType, TimeSpan? duration = null)
    {
        _transitionType = transitionType;
        _transitionDuration = duration;
        return this;
    }

    public TimelineScene Build()
    {
        return new TimelineScene(
            Index: _index,
            Heading: _heading,
            Script: _script,
            Start: _start,
            Duration: _duration,
            NarrationAudioPath: _narrationAudioPath,
            VisualAssets: _visualAssets,
            TransitionType: _transitionType,
            TransitionDuration: _transitionDuration
        );
    }
}

/// <summary>
/// Builder for creating test TimelineAsset instances
/// </summary>
public class TimelineAssetBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private AssetType _type = AssetType.Image;
    private string _filePath = "/test/asset.png";
    private TimeSpan _start = TimeSpan.Zero;
    private TimeSpan _duration = TimeSpan.FromSeconds(5);
    private Position _position = new(0, 0, 100, 100);
    private int _zIndex = 0;
    private double _opacity = 1.0;
    private EffectConfig? _effects;

    public TimelineAssetBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public TimelineAssetBuilder WithType(AssetType type)
    {
        _type = type;
        return this;
    }

    public TimelineAssetBuilder WithFilePath(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    public TimelineAssetBuilder WithStart(TimeSpan start)
    {
        _start = start;
        return this;
    }

    public TimelineAssetBuilder WithDuration(TimeSpan duration)
    {
        _duration = duration;
        return this;
    }

    public TimelineAssetBuilder WithPosition(Position position)
    {
        _position = position;
        return this;
    }

    public TimelineAssetBuilder WithZIndex(int zIndex)
    {
        _zIndex = zIndex;
        return this;
    }

    public TimelineAssetBuilder WithOpacity(double opacity)
    {
        _opacity = opacity;
        return this;
    }

    public TimelineAssetBuilder WithEffects(EffectConfig effects)
    {
        _effects = effects;
        return this;
    }

    public TimelineAsset Build()
    {
        return new TimelineAsset(
            Id: _id,
            Type: _type,
            FilePath: _filePath,
            Start: _start,
            Duration: _duration,
            Position: _position,
            ZIndex: _zIndex,
            Opacity: _opacity,
            Effects: _effects
        );
    }
}
