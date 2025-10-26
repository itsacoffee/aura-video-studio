using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Categories for project templates
/// </summary>
public enum TemplateCategory
{
    YouTube,
    SocialMedia,
    Business,
    Creative
}

/// <summary>
/// Template for quick project creation with pre-built structure
/// </summary>
public record ProjectTemplate
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TemplateCategory Category { get; init; }
    public string SubCategory { get; init; } = string.Empty; // e.g., "Intro", "Outro", "Tutorial" for YouTube
    public string PreviewImage { get; init; } = string.Empty;
    public string PreviewVideo { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    public string TemplateData { get; init; } = string.Empty; // JSON serialized template structure
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public string Author { get; init; } = "System";
    public bool IsSystemTemplate { get; init; } = true;
    public bool IsCommunityTemplate { get; init; } = false;
    public int UsageCount { get; init; } = 0;
    public double Rating { get; init; } = 0.0;
    public int RatingCount { get; init; } = 0;
}

/// <summary>
/// Template data structure stored in JSON
/// </summary>
public record TemplateStructure
{
    public List<TemplateTrack> Tracks { get; init; } = new();
    public List<TemplatePlaceholder> Placeholders { get; init; } = new();
    public List<TemplateTextOverlay> TextOverlays { get; init; } = new();
    public List<TemplateTransition> Transitions { get; init; } = new();
    public List<TemplateEffect> Effects { get; init; } = new();
    public TemplateMusicTrack? MusicTrack { get; init; }
    public double Duration { get; init; }
    public TemplateSettings Settings { get; init; } = new();
}

/// <summary>
/// Track in template
/// </summary>
public record TemplateTrack
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Type { get; init; } = "video"; // video or audio
}

/// <summary>
/// Placeholder clip that user will replace
/// </summary>
public record TemplatePlaceholder
{
    public string Id { get; init; } = string.Empty;
    public string TrackId { get; init; } = string.Empty;
    public double StartTime { get; init; }
    public double Duration { get; init; }
    public string Type { get; init; } = "video"; // video, audio, or image
    public string PlaceholderText { get; init; } = "Drag media here";
    public string PreviewUrl { get; init; } = string.Empty;
}

/// <summary>
/// Text overlay in template
/// </summary>
public record TemplateTextOverlay
{
    public string Id { get; init; } = string.Empty;
    public string TrackId { get; init; } = string.Empty;
    public double StartTime { get; init; }
    public double Duration { get; init; }
    public string Text { get; init; } = string.Empty;
    public string Font { get; init; } = "Arial";
    public int FontSize { get; init; } = 48;
    public string Color { get; init; } = "#FFFFFF";
    public string Animation { get; init; } = "fadeIn"; // fadeIn, slideIn, typewriter, etc.
    public TemplatePosition Position { get; init; } = new();
}

/// <summary>
/// Position for text overlays
/// </summary>
public record TemplatePosition
{
    public double X { get; init; } = 0.5; // 0-1 normalized
    public double Y { get; init; } = 0.5;
    public string Alignment { get; init; } = "center"; // center, left, right
}

/// <summary>
/// Transition between clips
/// </summary>
public record TemplateTransition
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = "crossDissolve"; // crossDissolve, wipe, zoom, slide, fade
    public double Duration { get; init; } = 1.0;
    public string Direction { get; init; } = "left"; // for wipe and slide
    public double Position { get; init; }
}

/// <summary>
/// Effect applied to clip or track
/// </summary>
public record TemplateEffect
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty; // colorGrade, vignette, blur, etc.
    public Dictionary<string, object> Parameters { get; init; } = new();
    public string AppliesTo { get; init; } = string.Empty; // clip ID or track ID
}

/// <summary>
/// Music track in template
/// </summary>
public record TemplateMusicTrack
{
    public string TrackId { get; init; } = string.Empty;
    public string PlaceholderUrl { get; init; } = string.Empty;
    public double StartTime { get; init; } = 0;
    public double Duration { get; init; }
    public double Volume { get; init; } = 0.7;
    public bool FadeIn { get; init; } = true;
    public bool FadeOut { get; init; } = true;
}

/// <summary>
/// Template settings
/// </summary>
public record TemplateSettings
{
    public int Width { get; init; } = 1920;
    public int Height { get; init; } = 1080;
    public int FrameRate { get; init; } = 30;
    public string AspectRatio { get; init; } = "16:9";
}

/// <summary>
/// Effect preset definition
/// </summary>
public record EffectPreset
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty; // Cinematic, Retro, Dynamic, etc.
    public List<TemplateEffect> Effects { get; init; } = new();
    public string PreviewImage { get; init; } = string.Empty;
}

/// <summary>
/// Transition preset definition
/// </summary>
public record TransitionPreset
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public double DefaultDuration { get; init; } = 1.0;
    public string Direction { get; init; } = string.Empty;
    public string PreviewVideo { get; init; } = string.Empty;
}

/// <summary>
/// Title template for animated text
/// </summary>
public record TitleTemplate
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty; // Lower Third, End Credits, etc.
    public List<TemplateTextOverlay> TextLayers { get; init; } = new();
    public double Duration { get; init; } = 5.0;
    public string PreviewVideo { get; init; } = string.Empty;
}

/// <summary>
/// Request to create project from template
/// </summary>
public record CreateFromTemplateRequest
{
    public string TemplateId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
}

/// <summary>
/// Request to save current project as template
/// </summary>
public record SaveAsTemplateRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TemplateCategory Category { get; init; }
    public string SubCategory { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    public string ProjectData { get; init; } = string.Empty;
    public string PreviewImage { get; init; } = string.Empty;
}

/// <summary>
/// Template list item for library view
/// </summary>
public record TemplateListItem
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TemplateCategory Category { get; init; }
    public string SubCategory { get; init; } = string.Empty;
    public string PreviewImage { get; init; } = string.Empty;
    public string PreviewVideo { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    public int UsageCount { get; init; }
    public double Rating { get; init; }
    public bool IsSystemTemplate { get; init; }
    public bool IsCommunityTemplate { get; init; }
}
