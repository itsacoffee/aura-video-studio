using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Tools;
using Aura.Core.Models.Ollama;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.AI.Tools;

/// <summary>
/// Tests for FactCheckTool
/// </summary>
public class FactCheckToolTests
{
    private readonly Mock<ILogger<FactCheckTool>> _loggerMock;
    private readonly FactCheckTool _tool;

    public FactCheckToolTests()
    {
        _loggerMock = new Mock<ILogger<FactCheckTool>>();
        _tool = new FactCheckTool(_loggerMock.Object);
    }

    [Fact]
    public void Name_ReturnsCorrectToolName()
    {
        Assert.Equal("verify_fact", _tool.Name);
    }

    [Fact]
    public void GetToolDefinition_ReturnsValidDefinition()
    {
        var definition = _tool.GetToolDefinition();

        Assert.NotNull(definition);
        Assert.Equal("function", definition.Type);
        Assert.Equal("verify_fact", definition.Function.Name);
        Assert.NotEmpty(definition.Function.Description);
        Assert.Equal("object", definition.Function.Parameters.Type);
        Assert.Contains("claim", definition.Function.Parameters.Properties.Keys);
        Assert.Contains("claim", definition.Function.Parameters.Required);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidClaim_ReturnsSuccessResult()
    {
        var arguments = JsonSerializer.Serialize(new { claim = "Quantum computers use qubits" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        Assert.NotNull(result);
        var parsedResult = JsonDocument.Parse(result);
        Assert.True(parsedResult.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("Quantum computers use qubits", parsedResult.RootElement.GetProperty("Claim").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WithQuantumClaim_ReturnsVerified()
    {
        var arguments = JsonSerializer.Serialize(new { claim = "Quantum computers use qubits for computation" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        Assert.True(parsedResult.RootElement.GetProperty("IsVerified").GetBoolean());
        Assert.True(parsedResult.RootElement.GetProperty("ConfidenceScore").GetDouble() > 0.9);
    }

    [Fact]
    public async Task ExecuteAsync_WithPythonClaim_ReturnsVerified()
    {
        var arguments = JsonSerializer.Serialize(new { claim = "Python was created in 1991" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        Assert.True(parsedResult.RootElement.GetProperty("IsVerified").GetBoolean());
        Assert.True(parsedResult.RootElement.GetProperty("ConfidenceScore").GetDouble() > 0.95);
    }

    [Fact]
    public async Task ExecuteAsync_WithFlatEarthClaim_ReturnsNotVerified()
    {
        var arguments = JsonSerializer.Serialize(new { claim = "The Earth is flat" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        Assert.False(parsedResult.RootElement.GetProperty("IsVerified").GetBoolean());
        Assert.True(parsedResult.RootElement.TryGetProperty("Correction", out _));
    }

    [Fact]
    public async Task ExecuteAsync_WithExaggeratedClaim_ReturnsNotVerified()
    {
        var arguments = JsonSerializer.Serialize(new { claim = "This product is 100% guaranteed to work always" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        Assert.False(parsedResult.RootElement.GetProperty("IsVerified").GetBoolean());
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingClaim_ReturnsError()
    {
        var arguments = JsonSerializer.Serialize(new { });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        Assert.False(parsedResult.RootElement.GetProperty("Success").GetBoolean());
        Assert.Contains("required", parsedResult.RootElement.GetProperty("Error").GetString()!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidJson_ReturnsError()
    {
        var arguments = "{ not valid json }";

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        Assert.False(parsedResult.RootElement.GetProperty("Success").GetBoolean());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsConfidenceScore()
    {
        var arguments = JsonSerializer.Serialize(new { claim = "AI can process data faster than humans" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        Assert.True(parsedResult.RootElement.GetProperty("ConfidenceScore").GetDouble() >= 0);
        Assert.True(parsedResult.RootElement.GetProperty("ConfidenceScore").GetDouble() <= 1);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsExplanation()
    {
        var arguments = JsonSerializer.Serialize(new { claim = "The sun is at the center of our solar system" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        var explanation = parsedResult.RootElement.GetProperty("Explanation").GetString();
        Assert.NotNull(explanation);
        Assert.NotEmpty(explanation);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSources()
    {
        var arguments = JsonSerializer.Serialize(new { claim = "Machine learning algorithms improve through experience" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        var sources = parsedResult.RootElement.GetProperty("Sources");
        Assert.True(sources.GetArrayLength() > 0);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_CompletesSuccessfully()
    {
        var arguments = JsonSerializer.Serialize(new { claim = "test claim" });
        using var cts = new CancellationTokenSource();

        var result = await _tool.ExecuteAsync(arguments, cts.Token);

        Assert.NotNull(result);
        var parsedResult = JsonDocument.Parse(result);
        Assert.True(parsedResult.RootElement.GetProperty("Success").GetBoolean());
    }
}
