using System.Collections.Generic;
using System.Text.Json;
using Aura.Core.Models.Ollama;
using Xunit;

namespace Aura.Tests.Models.Ollama;

/// <summary>
/// Tests for Ollama tool calling models
/// </summary>
public class OllamaToolCallingModelsTests
{
    [Fact]
    public void OllamaToolDefinition_SerializesCorrectly()
    {
        var toolDef = new OllamaToolDefinition
        {
            Type = "function",
            Function = new OllamaFunctionDefinition
            {
                Name = "test_function",
                Description = "A test function",
                Parameters = new OllamaFunctionParameters
                {
                    Type = "object",
                    Properties = new Dictionary<string, OllamaPropertyDefinition>
                    {
                        ["param1"] = new OllamaPropertyDefinition
                        {
                            Type = "string",
                            Description = "First parameter"
                        }
                    },
                    Required = new List<string> { "param1" }
                }
            }
        };

        var json = JsonSerializer.Serialize(toolDef);
        var deserialized = JsonSerializer.Deserialize<OllamaToolDefinition>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("function", deserialized.Type);
        Assert.Equal("test_function", deserialized.Function.Name);
        Assert.Contains("param1", deserialized.Function.Parameters.Properties.Keys);
    }

    [Fact]
    public void OllamaToolCall_ParsesArgumentsAsTypedObject()
    {
        var toolCall = new OllamaToolCall
        {
            Function = new OllamaFunctionCall
            {
                Name = "get_research_data",
                Arguments = JsonSerializer.Serialize(new { topic = "quantum computing", depth = "detailed" })
            }
        };

        var args = toolCall.Function.ParseArgumentsAsDictionary();

        Assert.NotNull(args);
        Assert.Contains("topic", args.Keys);
        Assert.Contains("depth", args.Keys);
        Assert.Equal("quantum computing", args["topic"].GetString());
        Assert.Equal("detailed", args["depth"].GetString());
    }

    [Fact]
    public void OllamaToolCall_ParsesInvalidArgumentsReturnsNull()
    {
        var toolCall = new OllamaToolCall
        {
            Function = new OllamaFunctionCall
            {
                Name = "test",
                Arguments = "invalid json {"
            }
        };

        var args = toolCall.Function.ParseArgumentsAsDictionary();

        Assert.Null(args);
    }

    [Fact]
    public void OllamaToolCall_ParsesEmptyArgumentsReturnsNull()
    {
        var toolCall = new OllamaToolCall
        {
            Function = new OllamaFunctionCall
            {
                Name = "test",
                Arguments = ""
            }
        };

        var args = toolCall.Function.ParseArgumentsAsDictionary();

        Assert.Null(args);
    }

    [Fact]
    public void OllamaMessageWithToolCalls_SerializesCorrectly()
    {
        var message = new OllamaMessageWithToolCalls
        {
            Role = "assistant",
            Content = "I will call a tool",
            ToolCalls = new List<OllamaToolCall>
            {
                new OllamaToolCall
                {
                    Function = new OllamaFunctionCall
                    {
                        Name = "test_function",
                        Arguments = "{\"param\":\"value\"}"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(message);
        var deserialized = JsonSerializer.Deserialize<OllamaMessageWithToolCalls>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("assistant", deserialized.Role);
        Assert.NotNull(deserialized.ToolCalls);
        Assert.Single(deserialized.ToolCalls);
        Assert.Equal("test_function", deserialized.ToolCalls[0].Function.Name);
    }

    [Fact]
    public void OllamaFunctionParameters_SupportsEnumValues()
    {
        var param = new OllamaPropertyDefinition
        {
            Type = "string",
            Description = "A parameter with enum",
            Enum = new List<string> { "option1", "option2", "option3" }
        };

        Assert.NotNull(param.Enum);
        Assert.Equal(3, param.Enum.Count);
        Assert.Contains("option1", param.Enum);
    }

    [Fact]
    public void OllamaFunctionCall_ParsesComplexArguments()
    {
        var complexArgs = new
        {
            topic = "machine learning",
            depth = "detailed",
            metadata = new
            {
                source = "research",
                priority = 5
            }
        };

        var toolCall = new OllamaToolCall
        {
            Function = new OllamaFunctionCall
            {
                Name = "complex_function",
                Arguments = JsonSerializer.Serialize(complexArgs)
            }
        };

        var args = toolCall.Function.ParseArgumentsAsDictionary();

        Assert.NotNull(args);
        Assert.Contains("topic", args.Keys);
        Assert.Contains("metadata", args.Keys);
        
        var metadata = args["metadata"];
        Assert.Equal(JsonValueKind.Object, metadata.ValueKind);
    }
}
