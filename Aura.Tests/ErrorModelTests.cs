using System;
using System.Text.Json;
using Aura.Api.Models;
using Xunit;

namespace Aura.Tests;

public class ErrorModelTests
{
    [Fact]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Arrange & Act
        var error = new ErrorModel(
            type: "https://docs.aura.studio/errors/E500",
            title: "Internal Server Error",
            status: 500,
            detail: "Something went wrong",
            correlationId: "corr-123",
            traceId: "trace-456",
            errorCode: "E500",
            details: new { additionalInfo = "test" });

        // Assert
        Assert.Equal("https://docs.aura.studio/errors/E500", error.Type);
        Assert.Equal("Internal Server Error", error.Title);
        Assert.Equal(500, error.Status);
        Assert.Equal("Something went wrong", error.Detail);
        Assert.Equal("corr-123", error.CorrelationId);
        Assert.Equal("trace-456", error.TraceId);
        Assert.Equal("E500", error.ErrorCode);
        Assert.NotNull(error.Details);
        Assert.NotEqual(default(DateTime), error.Timestamp);
    }

    [Fact]
    public void NotFound_ShouldCreate404Error()
    {
        // Act
        var error = ErrorModel.NotFound("Resource not found", "corr-123", "trace-456");

        // Assert
        Assert.Equal("https://docs.aura.studio/errors/E404", error.Type);
        Assert.Equal("Not Found", error.Title);
        Assert.Equal(404, error.Status);
        Assert.Equal("Resource not found", error.Detail);
        Assert.Equal("corr-123", error.CorrelationId);
        Assert.Equal("trace-456", error.TraceId);
    }

    [Fact]
    public void BadRequest_ShouldCreate400Error()
    {
        // Act
        var error = ErrorModel.BadRequest(
            "Invalid input",
            "corr-123",
            "trace-456",
            new { field = "name", error = "required" });

        // Assert
        Assert.Equal("https://docs.aura.studio/errors/E400", error.Type);
        Assert.Equal("Bad Request", error.Title);
        Assert.Equal(400, error.Status);
        Assert.Equal("Invalid input", error.Detail);
        Assert.Equal("corr-123", error.CorrelationId);
        Assert.Equal("trace-456", error.TraceId);
        Assert.NotNull(error.Details);
    }

    [Fact]
    public void InternalServerError_ShouldCreate500Error()
    {
        // Act
        var error = ErrorModel.InternalServerError(
            "Server error occurred",
            "corr-123",
            "trace-456",
            "E500-DB");

        // Assert
        Assert.Equal("https://docs.aura.studio/errors/E500", error.Type);
        Assert.Equal("Internal Server Error", error.Title);
        Assert.Equal(500, error.Status);
        Assert.Equal("Server error occurred", error.Detail);
        Assert.Equal("corr-123", error.CorrelationId);
        Assert.Equal("trace-456", error.TraceId);
        Assert.Equal("E500-DB", error.ErrorCode);
    }

    [Fact]
    public void Serialization_ShouldProduceValidJson()
    {
        // Arrange
        var error = new ErrorModel(
            type: "https://docs.aura.studio/errors/E500",
            title: "Test Error",
            status: 500,
            detail: "Test detail",
            correlationId: "corr-123",
            traceId: "trace-456");

        // Act
        var json = JsonSerializer.Serialize(error);
        var deserialized = JsonSerializer.Deserialize<ErrorModel>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(error.Type, deserialized!.Type);
        Assert.Equal(error.Title, deserialized.Title);
        Assert.Equal(error.Status, deserialized.Status);
        Assert.Equal(error.Detail, deserialized.Detail);
        Assert.Equal(error.CorrelationId, deserialized.CorrelationId);
        Assert.Equal(error.TraceId, deserialized.TraceId);
    }

    [Fact]
    public void JsonPropertyNames_ShouldUseCamelCase()
    {
        // Arrange
        var error = new ErrorModel(
            type: "https://docs.aura.studio/errors/E500",
            title: "Test Error",
            status: 500,
            detail: "Test detail",
            correlationId: "corr-123",
            errorCode: "E500");

        // Act
        var json = JsonSerializer.Serialize(error);

        // Assert
        Assert.Contains("\"type\":", json);
        Assert.Contains("\"title\":", json);
        Assert.Contains("\"status\":", json);
        Assert.Contains("\"detail\":", json);
        Assert.Contains("\"correlationId\":", json);
        Assert.Contains("\"errorCode\":", json);
        Assert.Contains("\"timestamp\":", json);
    }

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var error = new ErrorModel();

        // Assert
        Assert.Equal("https://docs.aura.studio/errors/E500", error.Type);
        Assert.Equal("Internal Server Error", error.Title);
        Assert.Equal(500, error.Status);
        Assert.Equal(string.Empty, error.Detail);
        Assert.Equal(string.Empty, error.CorrelationId);
        Assert.NotEqual(default(DateTime), error.Timestamp);
    }
}
