using Aura.Core.Models;

namespace Aura.Tests.TestDataBuilders;

/// <summary>
/// Builder for creating test Project instances
/// </summary>
public class ProjectBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = "Test Project";
    private string _description = "A test project";
    private string _ownerId = Guid.NewGuid().ToString();
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _updatedAt = DateTime.UtcNow;
    private ProjectStatus _status = ProjectStatus.Active;
    private Dictionary<string, object> _settings = new();
    private List<string> _tags = new();

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

    public ProjectBuilder WithOwnerId(string ownerId)
    {
        _ownerId = ownerId;
        return this;
    }

    public ProjectBuilder WithStatus(ProjectStatus status)
    {
        _status = status;
        return this;
    }

    public ProjectBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public ProjectBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public ProjectBuilder WithSetting(string key, object value)
    {
        _settings[key] = value;
        return this;
    }

    public ProjectBuilder WithTag(string tag)
    {
        _tags.Add(tag);
        return this;
    }

    public ProjectBuilder Archived()
    {
        _status = ProjectStatus.Archived;
        return this;
    }

    public Project Build()
    {
        return new Project
        {
            Id = _id,
            Name = _name,
            Description = _description,
            OwnerId = _ownerId,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            Status = _status,
            Settings = _settings,
            Tags = _tags
        };
    }
}

public enum ProjectStatus
{
    Active,
    Archived,
    Deleted
}
