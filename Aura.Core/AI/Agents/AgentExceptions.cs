using System;

namespace Aura.Core.AI.Agents;

/// <summary>
/// Thrown when an agent message is invalid
/// </summary>
public class InvalidAgentMessageException : Exception
{
    public AgentMessage? AgentMessage { get; }

    public InvalidAgentMessageException(string message) : base(message) { }

    public InvalidAgentMessageException(string message, AgentMessage agentMessage) 
        : base(message)
    {
        AgentMessage = agentMessage;
    }
}

/// <summary>
/// Thrown when an unknown agent is referenced
/// </summary>
public class UnknownAgentException : Exception
{
    public string AgentName { get; }

    public UnknownAgentException(string agentName)
        : base($"Unknown agent: {agentName}")
    {
        AgentName = agentName;
    }
}

/// <summary>
/// Thrown when an agent receives a message type it cannot handle
/// </summary>
public class UnknownMessageTypeException : Exception
{
    public string MessageType { get; }
    public string AgentName { get; }

    public UnknownMessageTypeException(string messageType, string agentName)
        : base($"Agent '{agentName}' cannot handle message type '{messageType}'")
    {
        MessageType = messageType;
        AgentName = agentName;
    }
}

/// <summary>
/// Thrown when agent processing times out
/// </summary>
public class AgentTimeoutException : Exception
{
    public string AgentName { get; }
    public TimeSpan Timeout { get; }

    public AgentTimeoutException(string agentName, TimeSpan timeout)
        : base($"Agent '{agentName}' timed out after {timeout.TotalSeconds:F1} seconds")
    {
        AgentName = agentName;
        Timeout = timeout;
    }
}

