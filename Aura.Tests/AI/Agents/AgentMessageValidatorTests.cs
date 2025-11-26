using System;
using System.Linq;
using Aura.Core.AI.Agents;
using Aura.Core.Models;
using Xunit;

namespace Aura.Tests.AI.Agents;

public class AgentMessageValidatorTests
{
    private readonly AgentMessageValidator _validator = new();

    [Fact]
    public void Validate_ValidMessage_ReturnsSuccess()
    {
        // Arrange
        var message = new AgentMessage(
            FromAgent: "Orchestrator",
            ToAgent: "Screenwriter",
            MessageType: "GenerateScript",
            Payload: new Brief("Test", null, null, "professional", "en", Aspect.Widescreen16x9)
        );

        // Act
        var result = _validator.Validate(message);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NullFromAgent_ReturnsError()
    {
        // Arrange
        var message = new AgentMessage(
            FromAgent: null!,
            ToAgent: "Screenwriter",
            MessageType: "GenerateScript",
            Payload: new object()
        );

        // Act
        var result = _validator.Validate(message);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("FromAgent is required", result.Errors.First());
    }

    [Fact]
    public void Validate_EmptyFromAgent_ReturnsError()
    {
        // Arrange
        var message = new AgentMessage(
            FromAgent: "   ",
            ToAgent: "Screenwriter",
            MessageType: "GenerateScript",
            Payload: new object()
        );

        // Act
        var result = _validator.Validate(message);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("FromAgent is required", result.Errors.First());
    }

    [Fact]
    public void Validate_NullToAgent_ReturnsError()
    {
        // Arrange
        var message = new AgentMessage(
            FromAgent: "Orchestrator",
            ToAgent: null!,
            MessageType: "GenerateScript",
            Payload: new object()
        );

        // Act
        var result = _validator.Validate(message);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("ToAgent is required", result.Errors.First());
    }

    [Fact]
    public void Validate_NullMessageType_ReturnsError()
    {
        // Arrange
        var message = new AgentMessage(
            FromAgent: "Orchestrator",
            ToAgent: "Screenwriter",
            MessageType: null!,
            Payload: new object()
        );

        // Act
        var result = _validator.Validate(message);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("MessageType is required", result.Errors.First());
    }

    [Fact]
    public void Validate_NullPayload_ReturnsError()
    {
        // Arrange
        var message = new AgentMessage(
            FromAgent: "Orchestrator",
            ToAgent: "Screenwriter",
            MessageType: "GenerateScript",
            Payload: null!
        );

        // Act
        var result = _validator.Validate(message);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Payload is required", result.Errors.First());
    }

    [Fact]
    public void Validate_UnknownMessageType_ReturnsError()
    {
        // Arrange
        var message = new AgentMessage(
            FromAgent: "Orchestrator",
            ToAgent: "Screenwriter",
            MessageType: "InvalidType",
            Payload: new object()
        );

        // Act
        var result = _validator.Validate(message);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Unknown MessageType", result.Errors.First());
    }

    [Fact]
    public void Validate_ValidMessageTypes_AllAccepted()
    {
        // Arrange
        var validTypes = new[] { "GenerateScript", "ReviseScript", "GeneratePrompts", "Review" };

        foreach (var messageType in validTypes)
        {
            var message = new AgentMessage(
                FromAgent: "Orchestrator",
                ToAgent: "Screenwriter",
                MessageType: messageType,
                Payload: new object()
            );

            // Act
            var result = _validator.Validate(message);

            // Assert
            Assert.True(result.IsValid, $"Message type '{messageType}' should be valid");
        }
    }

    [Fact]
    public void ValidateResponse_SuccessfulResponseWithResult_ReturnsSuccess()
    {
        // Arrange
        var response = new AgentResponse(
            Success: true,
            Result: new object(),
            FeedbackForRevision: null,
            RequiresRevision: false
        );

        // Act
        var result = _validator.ValidateResponse(response);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateResponse_SuccessfulResponseWithRequiresRevision_ReturnsSuccess()
    {
        // Arrange
        var response = new AgentResponse(
            Success: true,
            Result: null,
            FeedbackForRevision: null,
            RequiresRevision: true
        );

        // Act
        var result = _validator.ValidateResponse(response);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateResponse_SuccessfulResponseWithoutResultOrRevision_ReturnsError()
    {
        // Arrange
        var response = new AgentResponse(
            Success: true,
            Result: null,
            FeedbackForRevision: null,
            RequiresRevision: false
        );

        // Act
        var result = _validator.ValidateResponse(response);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Successful response must have a Result", result.Errors.First());
    }

    [Fact]
    public void ValidateResponse_FailedResponseWithoutFeedback_ReturnsError()
    {
        // Arrange
        var response = new AgentResponse(
            Success: false,
            Result: null,
            FeedbackForRevision: null,
            RequiresRevision: false
        );

        // Act
        var result = _validator.ValidateResponse(response);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Failed response should include FeedbackForRevision", result.Errors.First());
    }

    [Fact]
    public void ValidateResponse_FailedResponseWithFeedback_ReturnsSuccess()
    {
        // Arrange
        var response = new AgentResponse(
            Success: false,
            Result: null,
            FeedbackForRevision: "Some feedback",
            RequiresRevision: true
        );

        // Act
        var result = _validator.ValidateResponse(response);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MultipleErrors_AllReturned()
    {
        // Arrange
        var message = new AgentMessage(
            FromAgent: null!,
            ToAgent: null!,
            MessageType: null!,
            Payload: null!
        );

        // Act
        var result = _validator.Validate(message);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Count);
        Assert.Contains("FromAgent is required", result.Errors);
        Assert.Contains("ToAgent is required", result.Errors);
        Assert.Contains("MessageType is required", result.Errors);
        Assert.Contains("Payload is required", result.Errors);
    }
}

