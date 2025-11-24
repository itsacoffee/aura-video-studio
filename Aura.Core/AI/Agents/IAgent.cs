using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.AI.Agents;

/// <summary>
/// Interface for agents in the multi-agent script generation system
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Name of the agent (e.g., "Screenwriter", "VisualDirector", "Critic")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Process a message and return a response
    /// </summary>
    Task<AgentResponse> ProcessAsync(AgentMessage message, CancellationToken ct);
}

/// <summary>
/// Message passed between agents in the multi-agent system
/// </summary>
public record AgentMessage(
    string FromAgent,
    string ToAgent,
    string MessageType,
    object Payload,
    Dictionary<string, object>? Context = null
);

/// <summary>
/// Response from an agent after processing a message
/// </summary>
public record AgentResponse(
    bool Success,
    object? Result,
    string? FeedbackForRevision,
    bool RequiresRevision
);

