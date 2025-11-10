using Aura.Core.Models.Assets;

namespace Aura.Tests.TestDataBuilders;

/// <summary>
/// Builder for creating test Asset instances
/// </summary>
public class AssetBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "test-asset";
    private AssetType _type = AssetType.Video;
    private string _filePath = "/path/to/asset.mp4";
    private long _fileSize = 1024 * 1024; // 1MB
    private double _duration = 10.0;
    private Dictionary<string, object> _metadata = new();
    private List<string> _tags = new();

    public AssetBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public AssetBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AssetBuilder WithType(AssetType type)
    {
        _type = type;
        return this;
    }

    public AssetBuilder WithFilePath(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    public AssetBuilder WithFileSize(long fileSize)
    {
        _fileSize = fileSize;
        return this;
    }

    public AssetBuilder WithDuration(double duration)
    {
        _duration = duration;
        return this;
    }

    public AssetBuilder WithMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    public AssetBuilder WithTag(string tag)
    {
        _tags.Add(tag);
        return this;
    }

    public AssetBuilder AsVideo()
    {
        _type = AssetType.Video;
        _filePath = "/path/to/video.mp4";
        return this;
    }

    public AssetBuilder AsAudio()
    {
        _type = AssetType.Audio;
        _filePath = "/path/to/audio.mp3";
        return this;
    }

    public AssetBuilder AsImage()
    {
        _type = AssetType.Image;
        _filePath = "/path/to/image.png";
        _duration = 0;
        return this;
    }

    public Asset Build()
    {
        return new Asset
        {
            Id = _id,
            Name = _name,
            Type = _type,
            FilePath = _filePath,
            FileSize = _fileSize,
            Duration = _duration,
            Metadata = _metadata,
            Tags = _tags
        };
    }
}

public enum AssetType
{
    Video,
    Audio,
    Image,
    Text
}
