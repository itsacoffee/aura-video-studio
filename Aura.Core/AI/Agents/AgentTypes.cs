using System;
using System.Collections.Generic;
using Aura.Core.Models;
using Aura.Core.Models.Generation;

namespace Aura.Core.AI.Agents;

/// <summary>
/// Wrapper for script text with parsed structure
/// </summary>
public record ScriptDocument(
    string RawText,
    Script? ParsedScript = null
)
{
    /// <summary>
    /// Parse scenes from raw text for agent processing
    /// </summary>
    public List<ScriptScene> Scenes => ParsedScript?.Scenes ?? new List<ScriptScene>();
}

/// <summary>
/// Visual prompt for a scene with detailed generation parameters
/// </summary>
public record VisualPrompt(
    int SceneNumber,
    string DetailedPrompt,
    string CameraAngle,
    string Lighting,
    string? NegativePrompt = null,
    string? StyleKeywords = null
);

/// <summary>
/// Request for script revision with feedback
/// </summary>
public record RevisionRequest(
    ScriptDocument CurrentScript,
    string? Feedback
);

/// <summary>
/// Approved script with visual prompts
/// </summary>
public record ApprovedScript(
    ScriptDocument Script,
    List<VisualPrompt>? VisualPrompts
);

/// <summary>
/// Result from agent orchestrator
/// </summary>
public record AgentOrchestratorResult(
    ScriptDocument Script,
    List<VisualPrompt> VisualPrompts,
    List<AgentIteration> Iterations,
    bool ApprovedByCritic
);

/// <summary>
/// Represents a single iteration in the agent workflow
/// </summary>
public record AgentIteration(
    int IterationNumber,
    ScriptDocument Script,
    List<VisualPrompt> VisualPrompts,
    string? CriticFeedback
);

