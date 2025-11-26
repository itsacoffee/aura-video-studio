using System.Collections.Generic;
using System.Linq;

namespace Aura.Core.AI.Agents;

/// <summary>
/// Validation result for agent messages
/// </summary>
public record MessageValidationResult(
    bool IsValid,
    List<string> Errors
);

/// <summary>
/// Validates agent messages for correctness and completeness
/// </summary>
public class AgentMessageValidator
{
    /// <summary>
    /// Valid message types in the system
    /// </summary>
    private static readonly HashSet<string> ValidMessageTypes = new()
    {
        "GenerateScript",
        "ReviseScript",
        "GeneratePrompts",
        "Review"
    };

    /// <summary>
    /// Validates an agent message
    /// </summary>
    public MessageValidationResult Validate(AgentMessage message)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(message.FromAgent))
        {
            errors.Add("FromAgent is required and cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(message.ToAgent))
        {
            errors.Add("ToAgent is required and cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(message.MessageType))
        {
            errors.Add("MessageType is required and cannot be empty");
        }

        if (message.Payload == null)
        {
            errors.Add("Payload is required and cannot be null");
        }

        // Validate known message types
        if (!string.IsNullOrWhiteSpace(message.MessageType) && 
            !ValidMessageTypes.Contains(message.MessageType))
        {
            errors.Add(
                $"Unknown MessageType: {message.MessageType}. " +
                $"Valid types: {string.Join(", ", ValidMessageTypes)}");
        }

        return new MessageValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Validates an agent response
    /// </summary>
    public MessageValidationResult ValidateResponse(AgentResponse response)
    {
        var errors = new List<string>();

        if (response.Success && response.Result == null && !response.RequiresRevision)
        {
            errors.Add("Successful response must have a Result or RequiresRevision=true");
        }

        if (!response.Success && string.IsNullOrWhiteSpace(response.FeedbackForRevision))
        {
            errors.Add("Failed response should include FeedbackForRevision");
        }

        return new MessageValidationResult(errors.Count == 0, errors);
    }
}

