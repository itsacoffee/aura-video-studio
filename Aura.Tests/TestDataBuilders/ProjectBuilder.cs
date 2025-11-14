using Aura.Core.Models;

namespace Aura.Tests.TestDataBuilders;

/// <summary>
/// Builder for creating test Project instances
/// </summary>
public class ProjectBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "Test Project";
    private string? _description = "A test project";
    private string? _thumbnail;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _lastModifiedAt = DateTime.UtcNow;
    private double _duration = 60.0;
    private string? _author = "Test Author";
    private List<string> _tags = new();
    private string _projectData = "{}";
    private int _clipCount = 0;

    public ProjectBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public ProjectBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProjectBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ProjectBuilder WithThumbnail(string thumbnail)
    {
        _thumbnail = thumbnail;
        return this;
    }

    public ProjectBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public ProjectBuilder WithLastModifiedAt(DateTime lastModifiedAt)
    {
        _lastModifiedAt = lastModifiedAt;
        return this;
    }

    public ProjectBuilder WithDuration(double duration)
    {
        _duration = duration;
        return this;
    }

    public ProjectBuilder WithAuthor(string author)
    {
        _author = author;
        return this;
    }

    public ProjectBuilder WithTag(string tag)
    {
        _tags.Add(tag);
        return this;
    }

    public ProjectBuilder WithProjectData(string projectData)
    {
        _projectData = projectData;
        return this;
    }

    public ProjectBuilder WithClipCount(int clipCount)
    {
        _clipCount = clipCount;
        return this;
    }

    public Project Build()
    {
        return new Project
        {
            Id = _id,
            Name = _name,
            Description = _description,
            Thumbnail = _thumbnail,
            CreatedAt = _createdAt,
            LastModifiedAt = _lastModifiedAt,
            Duration = _duration,
            Author = _author,
            Tags = _tags,
            ProjectData = _projectData,
            ClipCount = _clipCount
        };
    }
}
