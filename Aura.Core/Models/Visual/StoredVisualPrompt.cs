using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Visual;

/// <summary>
/// Visual prompt stored in persistence layer with metadata and tracking
/// </summary>
public record StoredVisualPrompt
{
    /// <summary>
    /// Unique identifier for this visual prompt
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Script ID this prompt is associated with
    /// </summary>
    public string ScriptId { get; init; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracking generation runs
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Scene number (1-based)
    /// </summary>
    public int SceneNumber { get; init; }

    /// <summary>
    /// Scene heading/name
    /// </summary>
    public string SceneHeading { get; init; } = string.Empty;

    /// <summary>
    /// Detailed visual description for image generation
    /// </summary>
    public string DetailedPrompt { get; init; } = string.Empty;

    /// <summary>
    /// Camera angle description
    /// </summary>
    public string CameraAngle { get; init; } = string.Empty;

    /// <summary>
    /// Lighting description
    /// </summary>
    public string Lighting { get; init; } = string.Empty;

    /// <summary>
    /// Negative prompts (things to avoid)
    /// </summary>
    public List<string> NegativePrompts { get; init; } = new();

    /// <summary>
    /// Style keywords
    /// </summary>
    public string? StyleKeywords { get; init; }

    /// <summary>
    /// When this prompt was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When this prompt was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// URL to generated image if available
    /// </summary>
    public string? GeneratedImageUrl { get; init; }

    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Request to create a visual prompt
/// </summary>
public record CreateVisualPromptRequest(
    string ScriptId,
    string CorrelationId,
    int SceneNumber,
    string SceneHeading,
    string DetailedPrompt,
    string CameraAngle,
    string Lighting,
    List<string>? NegativePrompts = null,
    string? StyleKeywords = null
);

/// <summary>
/// Request to update a visual prompt
/// </summary>
public record UpdateVisualPromptRequest(
    string? DetailedPrompt = null,
    string? CameraAngle = null,
    string? Lighting = null,
    List<string>? NegativePrompts = null,
    string? StyleKeywords = null
);

