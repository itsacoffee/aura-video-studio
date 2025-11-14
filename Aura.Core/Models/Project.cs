using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Represents a saved video editing project
/// </summary>
public record Project
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Thumbnail { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastModifiedAt { get; init; } = DateTime.UtcNow;
    public double Duration { get; init; }
    public string? Author { get; init; }
    public List<string> Tags { get; init; } = new();
    public string ProjectData { get; init; } = string.Empty; // JSON serialized project file
    public int ClipCount { get; init; }
}

/// <summary>
/// Request to create or update a project
/// </summary>
public record SaveProjectRequest
{
    public string? Id { get; init; } // Null for new projects
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Thumbnail { get; init; }
    public string ProjectData { get; init; } = string.Empty; // JSON serialized project file
}

/// <summary>
/// Response when loading a project
/// </summary>
public record LoadProjectResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Thumbnail { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastModifiedAt { get; init; }
    public string ProjectData { get; init; } = string.Empty;
}

/// <summary>
/// Project list item for library view
/// </summary>
public record ProjectListItem
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Thumbnail { get; init; }
    public DateTime LastModifiedAt { get; init; }
    public double Duration { get; init; }
    public int ClipCount { get; init; }
}
