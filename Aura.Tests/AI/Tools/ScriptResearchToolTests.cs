using System;
using System.Collections.Generic;
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
/// Tests for ScriptResearchTool
/// </summary>
public class ScriptResearchToolTests
{
    private readonly Mock<ILogger<ScriptResearchTool>> _loggerMock;
    private readonly ScriptResearchTool _tool;

    public ScriptResearchToolTests()
    {
        _loggerMock = new Mock<ILogger<ScriptResearchTool>>();
        _tool = new ScriptResearchTool(_loggerMock.Object);
    }

    [Fact]
    public void Name_ReturnsCorrectToolName()
    {
        Assert.Equal("get_research_data", _tool.Name);
    }

    [Fact]
    public void GetToolDefinition_ReturnsValidDefinition()
    {
        var definition = _tool.GetToolDefinition();

        Assert.NotNull(definition);
        Assert.Equal("function", definition.Type);
        Assert.Equal("get_research_data", definition.Function.Name);
        Assert.NotEmpty(definition.Function.Description);
        Assert.Equal("object", definition.Function.Parameters.Type);
        Assert.Contains("topic", definition.Function.Parameters.Properties.Keys);
        Assert.Contains("depth", definition.Function.Parameters.Properties.Keys);
        Assert.Contains("topic", definition.Function.Parameters.Required);
    }

    [Fact]
    public void GetToolDefinition_DepthParameterHasEnum()
    {
        var definition = _tool.GetToolDefinition();
        var depthParam = definition.Function.Parameters.Properties["depth"];

        Assert.NotNull(depthParam.Enum);
        Assert.Contains("basic", depthParam.Enum);
        Assert.Contains("detailed", depthParam.Enum);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidBasicArguments_ReturnsSuccessResult()
    {
        var arguments = JsonSerializer.Serialize(new { topic = "quantum computing", depth = "basic" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        Assert.NotNull(result);
        var parsedResult = JsonDocument.Parse(result);
        Assert.True(parsedResult.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("quantum computing", parsedResult.RootElement.GetProperty("Topic").GetString());
        Assert.Equal("basic", parsedResult.RootElement.GetProperty("Depth").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WithDetailedDepth_ReturnsMoreFacts()
    {
        var basicArgs = JsonSerializer.Serialize(new { topic = "AI", depth = "basic" });
        var detailedArgs = JsonSerializer.Serialize(new { topic = "AI", depth = "detailed" });

        var basicResult = await _tool.ExecuteAsync(basicArgs, CancellationToken.None);
        var detailedResult = await _tool.ExecuteAsync(detailedArgs, CancellationToken.None);

        var basicDoc = JsonDocument.Parse(basicResult);
        var detailedDoc = JsonDocument.Parse(detailedResult);

        var basicFacts = basicDoc.RootElement.GetProperty("KeyFacts").GetArrayLength();
        var detailedFacts = detailedDoc.RootElement.GetProperty("KeyFacts").GetArrayLength();

        Assert.True(detailedFacts > basicFacts);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutDepth_DefaultsToBasic()
    {
        var arguments = JsonSerializer.Serialize(new { topic = "test topic" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        Assert.True(parsedResult.RootElement.GetProperty("Success").GetBoolean());
        Assert.Equal("basic", parsedResult.RootElement.GetProperty("Depth").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingTopic_ReturnsError()
    {
        var arguments = JsonSerializer.Serialize(new { depth = "basic" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        Assert.False(parsedResult.RootElement.GetProperty("Success").GetBoolean());
        Assert.Contains("required", parsedResult.RootElement.GetProperty("Error").GetString()!, 
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidJson_ReturnsError()
    {
        var arguments = "{ invalid json }";

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        Assert.False(parsedResult.RootElement.GetProperty("Success").GetBoolean());
        Assert.Contains("Invalid", parsedResult.RootElement.GetProperty("Error").GetString()!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithQuantumTopic_ReturnsRelevantFacts()
    {
        var arguments = JsonSerializer.Serialize(new { topic = "quantum computing", depth = "detailed" });

        var result = await _tool.ExecuteAsync(arguments, CancellationToken.None);

        var parsedResult = JsonDocument.Parse(result);
        var keyFacts = parsedResult.RootElement.GetProperty("KeyFacts");
        
        Assert.True(keyFacts.GetArrayLength() > 0);
        
        var firstFact = keyFacts[0].GetString();
        Assert.Contains("qubit", firstFact!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_CompletesSuccessfully()
    {
        var arguments = JsonSerializer.Serialize(new { topic = "test", depth = "basic" });
        using var cts = new CancellationTokenSource();

        var result = await _tool.ExecuteAsync(arguments, cts.Token);

        Assert.NotNull(result);
        var parsedResult = JsonDocument.Parse(result);
        Assert.True(parsedResult.RootElement.GetProperty("Success").GetBoolean());
    }
}
