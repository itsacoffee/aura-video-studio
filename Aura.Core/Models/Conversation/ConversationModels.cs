using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Conversation;

/// <summary>
/// Represents a single message in a conversation
/// </summary>
public record Message(
    string Role,           // "user", "assistant", "system"
    string Content,
    DateTime Timestamp,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Represents the complete conversation context for a project
/// </summary>
public record ConversationContext(
    string ProjectId,
    List<Message> Messages,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Represents metadata about the video being created
/// </summary>
public record VideoMetadata(
    string? ContentType,      // e.g., "Tutorial", "Marketing", "Entertainment"
    string? TargetPlatform,   // e.g., "YouTube", "TikTok", "Instagram"
    string? Audience,         // e.g., "Beginners", "Professionals"
    string? Tone,             // e.g., "Formal", "Casual", "Humorous"
    int? DurationSeconds,
    string[]? Keywords
);

/// <summary>
/// Represents an AI decision and the user's response
/// </summary>
public record AiDecision(
    string DecisionId,
    string Stage,             // e.g., "script", "visuals", "pacing"
    string Type,              // e.g., "suggestion", "recommendation", "question"
    string Suggestion,
    string UserAction,        // "accepted", "rejected", "modified"
    string? UserModification,
    DateTime Timestamp
);

/// <summary>
/// Represents the complete project context including conversation and decisions
/// </summary>
public record ProjectContext(
    string ProjectId,
    VideoMetadata? VideoMetadata,
    List<AiDecision> DecisionHistory,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Dictionary<string, object>? Metadata = null
);
